using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using provision41.web.Data;
using provision41.web.Models;
using Azure.Storage.Blobs;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Core;


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
    public string? CompanyName { get; set; }

    [BindProperty]
    public string? CompanyTruckId { get; set; }

    [BindProperty]
    public int MaxCapacity { get; set; }

    [BindProperty]
    public int CurrentCapacity { get; set; }

    [BindProperty]
    public string Type { get; set; } = "";

    [BindProperty]
    public string? Comments { get; set; }

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
                    CompanyName = string.IsNullOrWhiteSpace(CompanyName) ? "Not Provided" : CompanyName,
                    CompanyTruckId = string.IsNullOrWhiteSpace(CompanyTruckId) ? "Not Provided" : CompanyTruckId,
                    MaxCapacity = MaxCapacity
                };
                _context.Trucks.Add(truck);
            }
            else
            {
                // Update existing truck details
                truck.CompanyName = string.IsNullOrWhiteSpace(CompanyName) ? "Not Provided" : CompanyName;
                truck.CompanyTruckId = string.IsNullOrWhiteSpace(CompanyTruckId) ? "Not Provided" : CompanyTruckId;
                truck.MaxCapacity = MaxCapacity;
                
                _context.Trucks.Update(truck);
            }
                
            await _context.SaveChangesAsync();

            // Save the dump log
            var log = new DumpLog
            {
                TruckId = truck.Id,
                CurrentCapacity = CurrentCapacity,
                Type = Type,
                Comments = Comments,
                Timestamp = DateTime.Now
            };

            _context.DumpLogs.Add(log);
            await _context.SaveChangesAsync();

            if (UploadedFiles is { Count: > 0 })
            {
                // Get Key Vault name from environment variable or WebApp settings
                var kvName = Environment.GetEnvironmentVariable("KeyVaultName");
                if (string.IsNullOrWhiteSpace(kvName))
                    throw new Exception("KeyVaultName environment variable not set");

                var kvUri = $"https://{kvName}.vault.azure.net/";
                var client = new SecretClient(new Uri(kvUri), new DefaultAzureCredential());

                var storageAccountSecret = await client.GetSecretAsync("blobstorageaccountname");
                var storageAccountName = storageAccountSecret.Value.Value;

                var blobServiceClient = new BlobServiceClient(
                    new Uri($"https://{storageAccountName}.blob.core.windows.net"),
                    new DefaultAzureCredential());

                var containerClient = blobServiceClient.GetBlobContainerClient("uploads");

                foreach (var file in UploadedFiles)
                {
                    if (file.Length > 0)
                    {
                        var blobName = $"log-{log.Id}/{Path.GetFileName(file.FileName)}";
                        var blobClient = containerClient.GetBlobClient(blobName);
                        using var stream = file.OpenReadStream();
                        await blobClient.UploadAsync(stream, overwrite: true);
                    }
                }
            }

            Console.WriteLine("✅ DumpLog successfully saved.");
            return RedirectToPage("/Dr/DumpLogResult", new { id = log.Id });

        }
        catch (Exception ex)
        {
            Console.WriteLine("🔥 EXCEPTION during OnPostAsync:");
            Console.WriteLine(ex.ToString());
            throw;
        }
    }
}
