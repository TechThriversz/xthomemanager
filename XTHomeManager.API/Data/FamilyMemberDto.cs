namespace XTHomeManager.API.Data
{
    public class FamilyMemberDto
    {
        public string Name { get; set; } = string.Empty;
        public DateTime Birthday { get; set; }
        public string Relation { get; set; } = string.Empty;
        public List<int>? ParentIds { get; set; }
        public List<int>? SpouseIds { get; set; }
        public bool IsDeceased { get; set; }
        public DateTime? DeathDate { get; set; }

        // MAKE THESE NULLABLE
        public string? BornPlace { get; set; }
        public string? DiedPlace { get; set; }

        public IFormFile? Image { get; set; }
    }
}
