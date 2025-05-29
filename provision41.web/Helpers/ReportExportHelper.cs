using provision41.web.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text;

namespace provision41.web.Helpers
{
    public static class ReportExportHelper
    {
        public static byte[] GenerateCsv(List<DumpLogReportViewModel> entries, List<WeightSummary> summaries)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Date,Time,TruckId,CompanyName,CompanyTruckId,MaxCapacity,Type,% of Load,Weight,HasImages");

            foreach (var item in entries)
            {
                var weight = item.MaxCapacity * item.ActualCapacity / 100.0;
                sb.AppendLine($"{item.Date:yyyy-MM-dd},{item.Time},{item.TruckId},\"{item.CompanyName}\",\"{item.CompanyTruckId}\",{item.MaxCapacity},{item.Type},{item.ActualCapacity},{weight:N2},{(item.HasImages ? "Yes" : "No")}");
            }

            sb.AppendLine();
            sb.AppendLine("Summary by Type");
            sb.AppendLine("Type,Total Weight");

            foreach (var summary in summaries)
            {
                sb.AppendLine($"{summary.Type},{summary.TotalWeight:N2}");
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        public static byte[] GeneratePdf(List<DumpLogReportViewModel> entries, List<WeightSummary> summaries)
        {
            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Header().Text("Dump Log Report").FontSize(20).Bold().AlignCenter();

                    page.Content().Column(col =>
                    {
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
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
                                header.Cell().Element(CellStyle).Text("% of Load");
                                header.Cell().Element(CellStyle).Text("Weight");
                            });

                            foreach (var item in entries)
                            {
                                var weight = item.MaxCapacity * item.ActualCapacity / 100.0;

                                table.Cell().Element(CellStyle).Text(item.Date.ToShortDateString());
                                table.Cell().Element(CellStyle).Text(item.Time);
                                table.Cell().Element(CellStyle).Text(item.TruckId.ToString());
                                table.Cell().Element(CellStyle).Text(item.CompanyName);
                                table.Cell().Element(CellStyle).Text(item.CompanyTruckId);
                                table.Cell().Element(CellStyle).Text(item.MaxCapacity.ToString());
                                table.Cell().Element(CellStyle).Text(item.Type);
                                table.Cell().Element(CellStyle).Text(item.ActualCapacity.ToString());
                                table.Cell().Element(CellStyle).Text(weight.ToString("N2"));
                            }
                        });

                        col.Item().PaddingTop(25).Text("Summary by Type").Bold().FontSize(14);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.ConstantColumn(100);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("Type");
                                header.Cell().Element(CellStyle).Text("Total Weight");
                            });

                            foreach (var summary in summaries)
                            {
                                table.Cell().Element(CellStyle).Text(summary.Type);
                                table.Cell().Element(CellStyle).Text(summary.TotalWeight.ToString("N2"));
                            }
                        });
                    });
                });
            });

            using var ms = new MemoryStream();
            doc.GeneratePdf(ms);
            return ms.ToArray();
        }

        private static IContainer CellStyle(IContainer container) =>
            container.PaddingVertical(4).PaddingHorizontal(2).BorderBottom(1).BorderColor("#CCC");
    }
}
