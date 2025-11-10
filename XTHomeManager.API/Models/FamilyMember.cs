namespace XTHomeManager.API.Models
{

    public class FamilyMember
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateTime Birthday { get; set; }
        public string Relation { get; set; } = string.Empty;
        public string ParentIds { get; set; } = "[]";
        public string SpouseIds { get; set; } = "[]";
        public bool IsDeceased { get; set; } = false;
        public DateTime? DeathDate { get; set; }
        public string? BornPlace { get; set; }
        public string? DiedPlace { get; set; }
        public string? ImagePath { get; set; } 
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
