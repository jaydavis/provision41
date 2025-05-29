namespace provision41.web.ViewModels
{
    public class DumpLogReportViewModel
    {
        public DateTime Date { get; set; }
        public string Time => Date.ToString("hh:mm tt");
        public int TruckId { get; set; }
        public string CompanyName { get; set; } = "";
        public string CompanyTruckId { get; set; } = "";
        public int MaxCapacity { get; set; }
        public string Type { get; set; } = "";
        public int ActualCapacity { get; set; }
        public double Percentage => MaxCapacity == 0 ? 0 : Math.Round((double)ActualCapacity / MaxCapacity * 100, 2);
        public bool HasImages { get; set; } = false;
        public int DumpLogId { get; set; }
        public class WeightSummary
        {
            public string Type { get; set; } = "";
            public double TotalWeight { get; set; }
        }        
    }
}
