using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using provision41.web.Data;
using provision41.web.Models;
using Microsoft.EntityFrameworkCore;

namespace Provision41Web.Pages.Dr;

[ValidateAntiForgeryToken]
public class DumplogModel : PageModel
{
    private readonly AppDbContext _context;

    public DumplogModel(AppDbContext context)
    {
        _context = context;
    }

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    public string Timestamp { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm");

    [BindProperty]
    public string CompanyName { get; set; } = "";

    [BindProperty]
    public string CompanyTruckId { get; set; } = "";

    [BindProperty]
    public int MaxCapacity { get; set; }

    [BindProperty]
    public int CurrentCapacity { get; set; }

    [BindProperty]
    public string Type { get; set; } = "";

    [BindProperty]
    public string Comments { get; set; } = "";

    [BindProperty]
    public IFormFileCollection? UploadedFiles { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        Console.WriteLine($"🔍 OnGetAsync called with Id={Id}");

        if (Id <= 0)
        {
            Console.WriteLine("❌ Invalid or missing Id.");
            return Page(); // Invalid ID, just return the page
        }

        var truck = await _context.Trucks.FindAsync(Id);

        if (truck != null)
        {
            Console.WriteLine("✅ Truck found. Populating form.");
            Id = Id;
            CompanyName = truck.CompanyName ?? "";
            CompanyTruckId = truck.CompanyTruckId ?? "";
            MaxCapacity = truck.MaxCapacity;
        }
        else
        {
            Console.WriteLine("⚠️ No truck found with that Id.");
        }

        Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            Console.WriteLine($"🚛 POST received: Id={Id}, CompanyName={CompanyName}, MaxCapacity={MaxCapacity}, CurrentCapacity={CurrentCapacity}, Type={Type}");

            if (!ModelState.IsValid)
            {
                Console.WriteLine("❌ ModelState is invalid!");
                foreach (var kvp in ModelState)
                    foreach (var err in kvp.Value.Errors)
                        Console.WriteLine($"- {kvp.Key}: {err.ErrorMessage}");
                return Page();
            }

            // Check if truck exists by ID
            var truck = await _context.Trucks.FindAsync(Id);

            // If truck doesn't exist, create it
            if (truck == null)
            {
                truck = new Truck
                {
                    Id = Id, 
                    CompanyName = CompanyName,
                    CompanyTruckId = CompanyTruckId,
                    MaxCapacity = MaxCapacity
                };
                _context.Trucks.Add(truck);
                await _context.SaveChangesAsync();
            }

            // Save the dump log
            var log = new DumpLog
            {
                CompanyName = CompanyName ?? "",
                TruckId = truck.Id,
                MaxCapacity = MaxCapacity,
                CurrentCapacity = CurrentCapacity,
                Type = Type,
                Comments = Comments,
                Timestamp = DateTime.Now
            };

            _context.DumpLogs.Add(log);
            await _context.SaveChangesAsync();

            Console.WriteLine("✅ DumpLog successfully saved.");
            return RedirectToPage("/Index");
        }
        catch (Exception ex)
        {
            Console.WriteLine("🔥 EXCEPTION during OnPostAsync:");
            Console.WriteLine(ex.ToString());
            throw;
        }
    }
}
