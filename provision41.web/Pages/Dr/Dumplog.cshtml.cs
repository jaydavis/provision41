using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Provision41Web.Pages.Dr;

public class DumplogModel : PageModel
{
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
        // TODO: Save data to a JSON file or DB
        // TODO: Upload UploadedFiles to Azure Blob

        // Temporary logging to console
        Console.WriteLine($"Submitted: {CompanyName}, {TruckId}, Type={Type}");

        return RedirectToPage("/Dr/Dumplog", new { id = this.Id });
    }
}
