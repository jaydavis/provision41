using System;

namespace provision41.web.Models
{
    public class DumpLog
    {
        public int Id { get; set; }

        public string? CompanyName { get; set; }

        public string? TruckId { get; set; }

        public int MaxCapacity { get; set; }

        public int CurrentCapacity { get; set; }

        public string? Type { get; set; }

        public string? Comments { get; set; }

        public DateTime Timestamp { get; set; }
    }
}
