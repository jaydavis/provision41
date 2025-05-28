using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using provision41.web.Data;
using provision41.web.ViewModels;
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
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public List<int> TruckIdOptions { get; set; } = [];
        public List<string> TypeOptions { get; set; } = [];

        [BindProperty(SupportsGet = true)]
        public int? TruckIdFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? TypeFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? StartDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? EndDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Export { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;


        public async Task<IActionResult> OnGetAsync()
        {
            // Load blob container
            var kvName = Environment.GetEnvironmentVariable("KeyVaultName");
            var secretClient = new SecretClient(new Uri($"https://{kvName}.vault.azure.net/"), new DefaultAzureCredential());
            var accountName = (await secretClient.GetSecretAsync("blobstorageaccountname")).Value.Value;

            var blobServiceClient = new BlobServiceClient(new Uri($"https://{accountName}.blob.core.windows.net"), new DefaultAzureCredential());
            var containerClient = blobServiceClient.GetBlobContainerClient("uploads");

            TruckIdOptions = await _context.Trucks
                .Select(t => t.Id)
                .Distinct()
                .OrderBy(id => id)
                .ToListAsync();

            TypeOptions = await _context.DumpLogs
                .Select(dl => dl.Type)
                .Where(type => type != null && type != "")
                .Distinct()
                .OrderBy(type => type)
                .ToListAsync();

            // Build query with filters
            var query = _context.DumpLogs
                .Join(_context.Trucks,
                    dl => dl.TruckId,
                    t => t.Id,
                    (dl, t) => new
                    {
                        DumpLogId = dl.Id,
                        dl.Timestamp,
                        TruckId = t.Id,
                        t.CompanyName,
                        t.CompanyTruckId,
                        t.MaxCapacity,
                        dl.Type,
                        dl.CurrentCapacity
                    })
                .AsQueryable();

            if (TruckIdFilter.HasValue)
                query = query.Where(x => x.TruckId == TruckIdFilter.Value);

            if (!string.IsNullOrWhiteSpace(TypeFilter))
                query = query.Where(x => x.Type == TypeFilter);

            if (StartDate.HasValue)
                query = query.Where(x => x.Timestamp >= StartDate.Value);

            if (EndDate.HasValue)
                query = query.Where(x => x.Timestamp <= EndDate.Value);

            var joined = await query
                .OrderByDescending(x => x.Timestamp)
                .ToListAsync();

            TotalCount = joined.Count;

            // CSV export (before pagination)
            if (Export?.ToLowerInvariant() == "csv")
            {
                var allEntries = new List<DumpLogReportViewModel>();

                foreach (var entry in joined)
                {
                    bool hasImages = false;
                    await foreach (var blob in containerClient.GetBlobsAsync(prefix: $"log-{entry.DumpLogId}/"))
                    {
                        hasImages = true;
                        break;
                    }

                    allEntries.Add(new DumpLogReportViewModel
                    {
                        DumpLogId = entry.DumpLogId,
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

                var csv = new StringBuilder();
                csv.AppendLine("Date,Time,TruckId,CompanyName,CompanyTruckId,MaxCapacity,Type,ActualCapacity,HasImages");

                foreach (var item in allEntries)
                {
                    csv.AppendLine($"{item.Date:yyyy-MM-dd},{item.Time},{item.TruckId},\"{item.CompanyName}\",\"{item.CompanyTruckId}\",{item.MaxCapacity},{item.Type},{item.ActualCapacity},{item.HasImages}");
                }

                var bytes = Encoding.UTF8.GetBytes(csv.ToString());
                return File(bytes, "text/csv", "dumplog-report.csv");
            }

            if (Export?.ToLowerInvariant() == "pdf")
            {
                var allEntries = new List<DumpLogReportViewModel>();

                foreach (var entry in joined)
                {
                    bool hasImages = false;
                    await foreach (var blob in containerClient.GetBlobsAsync(prefix: $"log-{entry.DumpLogId}/"))
                    {
                        hasImages = true;
                        break;
                    }

                    allEntries.Add(new DumpLogReportViewModel
                    {
                        DumpLogId = entry.DumpLogId,
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

                var pdf = QuestPDF.Fluent.Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Margin(30);
                        page.Header().Text("Dump Log Report").FontSize(20).Bold().AlignCenter();
                        page.Content().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(1); // Date
                                columns.RelativeColumn(1); // Time
                                columns.RelativeColumn(1); // Truck ID
                                columns.RelativeColumn(2); // Company Name
                                columns.RelativeColumn(2); // Company Truck ID
                                columns.RelativeColumn(1); // Max Capacity
                                columns.RelativeColumn(1); // Type
                                columns.RelativeColumn(1); // Actual
                                columns.RelativeColumn(1); // Has Images
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("Date");
                                header.Cell().Element(CellStyle).Text("Time");
                                header.Cell().Element(CellStyle).Text("Truck ID");
                                header.Cell().Element(CellStyle).Text("Company Name");
                                header.Cell().Element(CellStyle).Text("Company Truck ID");
                                header.Cell().Element(CellStyle).Text("Max Capacity");
                                header.Cell().Element(CellStyle).Text("Type");
                                header.Cell().Element(CellStyle).Text("Actual");
                                header.Cell().Element(CellStyle).Text("Has Images");
                            });

                            foreach (var item in allEntries)
                            {
                                table.Cell().Element(CellStyle).Text(item.Date.ToShortDateString());
                                table.Cell().Element(CellStyle).Text(item.Time);
                                table.Cell().Element(CellStyle).Text(item.TruckId.ToString());
                                table.Cell().Element(CellStyle).Text(item.CompanyName);
                                table.Cell().Element(CellStyle).Text(item.CompanyTruckId);
                                table.Cell().Element(CellStyle).Text(item.MaxCapacity.ToString());
                                table.Cell().Element(CellStyle).Text(item.Type);
                                table.Cell().Element(CellStyle).Text(item.ActualCapacity.ToString());
                                table.Cell().Element(CellStyle).Text(item.HasImages ? "Yes" : "No");
                            }

                            IContainer CellStyle(IContainer container) =>
                                container.PaddingVertical(4).PaddingHorizontal(2).BorderBottom(1).BorderColor("#CCC");
                        });
                    });
                });

                using var ms = new MemoryStream();
                pdf.GeneratePdf(ms);
                return File(ms.ToArray(), "application/pdf", "dumplog-report.pdf");
            }

            // Pagination
            var paged = joined
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            ReportEntries = new();

            foreach (var entry in paged)
            {
                bool hasImages = false;
                await foreach (var blob in containerClient.GetBlobsAsync(prefix: $"log-{entry.DumpLogId}/"))
                {
                    hasImages = true;
                    break;
                }

                ReportEntries.Add(new DumpLogReportViewModel
                {
                    DumpLogId = entry.DumpLogId,
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

            return Page();
        }
    }
}
