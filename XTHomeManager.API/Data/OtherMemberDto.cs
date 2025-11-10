namespace XTHomeManager.API.Data
{
    public class OtherMemberDto
    {
        public string Name { get; set; } = string.Empty;
        public string Relation { get; set; } = string.Empty;
        public DateTime Birthday { get; set; }
        public bool IsDeceased { get; set; }
        public DateTime? DeathDate { get; set; }
        public string? BornPlace { get; set; }
        public string? DiedPlace { get; set; }
        public IFormFile? Image { get; set; }
    }
}
