using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using XTHomeManager.API.Data;
using XTHomeManager.API.Models;

namespace XTHomeManager.API.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("api/[controller]")]
    [ApiController]
    public class BillsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BillsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetBills()
        {
            var userId = User.FindFirst("AdminId")?.Value;
            var bills = await _context.ElectricityBills
                .Where(b => b.AdminId == userId)
                .ToListAsync();
            return Ok(bills);
        }

        [HttpPost]
        public async Task<IActionResult> CreateBill([FromBody] ElectricityBill bill)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            bill.AdminId = User.FindFirst("AdminId")?.Value;
            _context.ElectricityBills.Add(bill);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetBills), new { id = bill.Id }, bill);
        }
    }
}