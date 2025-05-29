using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using provision41.web.Data;
using provision41.web.ViewModels;
using provision41.web.Helpers;
using Azure.Security.KeyVault.Secrets;
using Azure.Identity;
using Azure.Storage.Blobs;
using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace provision41.web.Pages
{
    public class ReportModel : PageModel
    {
        private readonly AppDbContext _context;
        public ReportModel(AppDbContext context) => _context = context;

        public List<DumpLogReportViewModel> ReportEntries { get; set; } = [];
        public List<WeightSummary> WeightSummaries { get; set; } = [];
        public List<int> TruckIdOptions { get; set; } = [];
        public List<string> TypeOptions { get; set; } = [];
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

        [BindProperty(SupportsGet = true)] public int? TruckIdFilter { get; set; }
        [BindProperty(SupportsGet = true)] public string? TypeFilter { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? StartDate { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? EndDate { get; set; }
        [BindProperty(SupportsGet = true)] public string? Export { get; set; }
        [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;

        public async Task<IActionResult> OnGetAsync()
        {
            var kvName = Environment.GetEnvironmentVariable("KeyVaultName");
            var secretClient = new SecretClient(new Uri($"https://{kvName}.vault.azure.net/"), new DefaultAzureCredential());
            var accountName = (await secretClient.GetSecretAsync("blobstorageaccountname")).Value.Value;
            var blobClient = new BlobServiceClient(new Uri($"https://{accountName}.blob.core.windows.net"), new DefaultAzureCredential());
            var container = blobClient.GetBlobContainerClient("uploads");

            TruckIdOptions = await _context.Trucks.Select(t => t.Id).Distinct().OrderBy(id => id).ToListAsync();
            TypeOptions = await _context.DumpLogs.Select(dl => dl.Type).Where(t => !string.IsNullOrWhiteSpace(t)).Distinct().OrderBy(t => t).ToListAsync();

            var query = _context.DumpLogs.Join(_context.Trucks,
                dl => dl.TruckId,
                t => t.Id,
                (dl, t) => new
                {
                    dl.Id,
                    dl.Timestamp,
                    TruckId = t.Id,
                    t.CompanyName,
                    t.CompanyTruckId,
                    t.MaxCapacity,
                    dl.Type,
                    dl.CurrentCapacity
                }).AsQueryable();

            if (TruckIdFilter.HasValue) query = query.Where(x => x.TruckId == TruckIdFilter);
            if (!string.IsNullOrWhiteSpace(TypeFilter)) query = query.Where(x => x.Type == TypeFilter);
            if (StartDate.HasValue) query = query.Where(x => x.Timestamp >= StartDate);
            if (EndDate.HasValue) query = query.Where(x => x.Timestamp <= EndDate);

            var joined = await query.OrderByDescending(x => x.Timestamp).ToListAsync();
            TotalCount = joined.Count;

            var allEntries = new List<DumpLogReportViewModel>();
            foreach (var entry in joined)
            {
                bool hasImages = false;
                await foreach (var _ in container.GetBlobsAsync(prefix: $"log-{entry.Id}/"))
                {
                    hasImages = true;
                    break;
                }

                allEntries.Add(new DumpLogReportViewModel
                {
                    DumpLogId = entry.Id,
                    Date = entry.Timestamp,
                    TruckId = entry.TruckId,
                    CompanyName = entry.CompanyName ?? string.Empty,
                    CompanyTruckId = entry.CompanyTruckId ?? string.Empty,
                    MaxCapacity = entry.MaxCapacity,
                    Type = entry.Type,
                    ActualCapacity = entry.CurrentCapacity,
                    HasImages = hasImages
                });
            }

            WeightSummaries = allEntries
                .GroupBy(x => x.Type)
                .Select(g => new WeightSummary
                {
                    Type = g.Key,
                    TotalWeight = g.Sum(x => x.MaxCapacity * x.ActualCapacity / 100.0)
                }).ToList();

            if (Export?.ToLowerInvariant() == "csv")
                return File(ReportExportHelper.GenerateCsv(allEntries, WeightSummaries), "text/csv", "dumplog-report.csv");

            if (Export?.ToLowerInvariant() == "pdf")
                return File(ReportExportHelper.GeneratePdf(allEntries, WeightSummaries), "application/pdf", "dumplog-report.pdf");

            ReportEntries = allEntries
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            return Page();
        }
    }
}
