using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace provision41.web.Models
{
    public class DumpLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int TruckId { get; set; }

        public int CurrentCapacity { get; set; }

        [Required]
        public string Type { get; set; } = "";

        public string? Comments { get; set; }

        public DateTime Timestamp { get; set; }
    }
}
