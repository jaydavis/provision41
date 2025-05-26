using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace provision41.web.Models;

public class DumpLog
{
    [Key]
    public int Id { get; set; }
    public string TruckId { get; set; }

    [ForeignKey("TruckId")]
    public Truck Truck { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public int CurrentCapacity { get; set; }
    public string Comments { get; set; }
    public string PhotoUrlsJson { get; set; }
}
