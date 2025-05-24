using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ReportingAPI.Models
{
    public class ReportRequest
    {
        public string ReportType { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 100;
    }
    public class ReportFilter
    {
        public string Field { get; set; }      // Ör: "ProductName", "CustomerName", "SaleDate"
        public string Operator { get; set; }   // Ör: "=", "LIKE", ">", "<", ">=", "<="
        public object Value { get; set; }      // Değeri (ör. "Laptop", "2024-01-01")
    }
    public class FilteredReportRequest
    {
        public string ReportType { get; set; }
        public List<ReportFilter> Filters { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 100;
    }
}
