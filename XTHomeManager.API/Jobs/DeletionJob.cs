// Jobs/DeletionJob.cs
using Hangfire;
using Microsoft.EntityFrameworkCore;
using XTHomeManager.API.Data;

namespace XTHomeManager.API.Jobs
{
    [AutomaticRetry(Attempts = 3)]
    public class DeletionJob
    {
        private readonly AppDbContext _context;

        public DeletionJob(AppDbContext context)
        {
            _context = context;
        }

        public async Task Execute()
        {
            var cutoff = DateTime.UtcNow;
            var requests = await _context.UserDeletionRequests
                .Where(r => r.Status == "Approved" && r.DeletionScheduledAt <= cutoff)
                .Include(r => r.User)
                .ToListAsync();

            foreach (var request in requests)
            {
                var userId = request.UserId;

                var recordIds = await _context.Records
                    .Where(r => r.UserId == userId)
                    .Select(r => r.Id)
                    .ToListAsync();

                var user = request.User;
                _context.Users.Remove(user);

                await _context.Settings.Where(s => s.UserId == userId).ExecuteDeleteAsync();
                await _context.Passwords.Where(p => p.UserId == userId).ExecuteDeleteAsync();
                await _context.FamilyMembers.Where(f => f.UserId == userId).ExecuteDeleteAsync();

                if (recordIds.Any())
                {
                    await _context.MilkEntries.Where(m => recordIds.Contains(m.RecordId)).ExecuteDeleteAsync();
                    await _context.RentEntries.Where(r => recordIds.Contains(r.RecordId)).ExecuteDeleteAsync();
                    await _context.ElectricityBills.Where(e => recordIds.Contains(e.RecordId)).ExecuteDeleteAsync();
                }

                await _context.Records.Where(r => r.UserId == userId).ExecuteDeleteAsync();
                await _context.ProUpgradeRequests.Where(p => p.UserId == userId).ExecuteDeleteAsync();

                if (!string.IsNullOrEmpty(user.ImagePath))
                {
                    var fileKey = user.ImagePath.Split('/').Last();
                    // await r2Client.DeleteObjectAsync("bucket", fileKey);
                }

                _context.UserDeletionRequests.Remove(request);
            }

            await _context.SaveChangesAsync();
        }
    }
}