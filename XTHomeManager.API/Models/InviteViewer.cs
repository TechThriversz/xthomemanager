namespace XTHomeManager.API.Models
{
   
    public class InvitedViewerDto
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
        public List<RecordDto> Records { get; set; }
    }

    public class RecordDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public bool Accepted { get; set; }
        public bool IsAccepted { get; set; }
    }
}
