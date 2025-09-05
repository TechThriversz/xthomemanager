using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace XTHomeManager.API.Models
{
    public class Record
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        public string Type { get; set; }

        // Make UserId optional; set server-side
        public string UserId { get; set; }

        [JsonIgnore]
        public User User { get; set; }

        // Make Viewers optional; managed by context

        [JsonIgnore]
        public ICollection<RecordViewer> Viewers
        {
            get; set;
        }
    }

    public class CreateRecordDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        public string Type { get; set; }
    }


}