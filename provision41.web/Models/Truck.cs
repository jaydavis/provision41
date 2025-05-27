using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Truck
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }

    public string CompanyName { get; set; } = "";
    public string CompanyTruckId { get; set; } = "";
    public int MaxCapacity { get; set; }
}
