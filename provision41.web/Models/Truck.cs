using System.ComponentModel.DataAnnotations;
namespace provision41.web.Models;

public class Truck
{
    [Key]
    public string Id { get; set; } // Same as QR serial (id from querystring)

    public string CompanyName { get; set; }
    public string TruckNumber { get; set; }
    public int MaxCapacity { get; set; }
    public string Type { get; set; } // "C&D", "VEG", or "HAZMAT"

    public List<DumpLog> DumpLogs { get; set; }
}
