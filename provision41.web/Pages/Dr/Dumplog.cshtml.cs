using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using provision41.web.Data; 
using provision41.web.Models;

namespace Provision41Web.Pages.Dr;

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
    public string TruckId { get; set; } = "";

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

    public IActionResult OnGet()
    {
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            Console.WriteLine($"🚛 POST received: {CompanyName}, {TruckId}, {MaxCapacity}, {CurrentCapacity}, {Type}");

            if (!ModelState.IsValid)
            {
                Console.WriteLine("❌ ModelState is invalid!");
                foreach (var kvp in ModelState)
                {
                    foreach (var err in kvp.Value.Errors)
                    {
                        Console.WriteLine($"- {kvp.Key}: {err.ErrorMessage}");
                    }
                }
                return Page();
            }

            var log = new DumpLog
            {
                CompanyName = CompanyName,
                TruckId = TruckId,
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
