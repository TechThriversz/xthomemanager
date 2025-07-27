using System;
using System.ComponentModel.DataAnnotations;

namespace XTHomeManager.API.Models
{
    public class MilkEntry
    {
        public int Id { get; set; }

        [Required]
        public int RecordId { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public decimal QuantityLiters { get; set; }

        [Required]
        public decimal TotalCost { get; set; }

        [Required]
        public string AdminId { get; set; }

        public string? Status { get; set; }

        public Record? Record { get; set; }
    }
}