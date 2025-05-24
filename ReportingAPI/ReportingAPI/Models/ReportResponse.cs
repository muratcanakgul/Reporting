using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ReportingAPI.Models
{
    public class ReportResponse
    {
        public List<Dictionary<string, object>> Data { get; set; }
        public int TotalCount { get; set; }
    }
}
