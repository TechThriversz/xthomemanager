namespace XTHomeManager.API.Data
{
    public class FamilyMemberDto
    {
        public string Name { get; set; } = string.Empty;
        public DateTime Birthday { get; set; }
        public string Relation { get; set; } = string.Empty;

        // FIX: Accept JSON string, not List<int>
        public string? ParentIdsJson { get; set; }
        public string? SpouseIdsJson { get; set; }

        public bool IsDeceased { get; set; }
        public DateTime? DeathDate { get; set; }
        public string? BornPlace { get; set; }
        public string? DiedPlace { get; set; }
        public IFormFile? Image { get; set; }
    }
}
