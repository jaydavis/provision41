using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using provision41.web.Models;
using provision41.web.Data;

namespace provision41.web.Pages;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    [BindProperty]
    public Truck Truck { get; set; }

    [BindProperty]
    public DumpLog DumpLog { get; set; }

    public bool IsKnownTruck { get; set; }

    public void OnGet()
    {
        Truck = _db.Trucks.FirstOrDefault(t => t.Id == Id);
        IsKnownTruck = Truck != null;

        if (!IsKnownTruck)
        {
            Truck = new Truck { Id = Id };
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!_db.Trucks.Any(t => t.Id == Truck.Id))
        {
            _db.Trucks.Add(Truck);
        }

        DumpLog.TruckId = Truck.Id;
        _db.DumpLogs.Add(DumpLog);

        await _db.SaveChangesAsync();

        return RedirectToPage("/Success");
    }
}
