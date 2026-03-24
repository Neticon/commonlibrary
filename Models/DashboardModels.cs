using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibrary.Models
{
    internal class DashboardModels
    {
    }

    public class DashboardPayload
    {
        public string? period { get; set; }
        public string? mode { get; set; }
        public Guid? venue_id { get; set; }
        public Guid? tenant_id { get; set; }
        public string? bracket {  get; set; }
        public string? date { get; set; }
    }

    public class DashboardKeyMetrics
    {
        public string mode { get; set; }
        public List<Row> rows { get; set; }
        public string entity { get; set; }
        public string period { get; set; }
        public string schema { get; set; }
        public bool success { get; set; }
        public string request_id { get; set; }
        public int scoped_booking_count { get; set; }
        public int customer_rating_count { get; set; }
        public int appointee_rating_count { get; set; }
        public double average_customer_rating { get; set; }
        public int average_appointee_rating { get; set; }
    }
    public class Metrics
    {
        public int booked { get; set; }
        public int cancelled { get; set; }
        public int rescheduled { get; set; }
        public int pct_result_lead { get; set; }
        public int qualified_views { get; set; }
        public int block_selections { get; set; }
        public double pct_customer_rated { get; set; }
        public int pct_result_prospect { get; set; }
        public int pct_result_conversion { get; set; }
    }

    public class Row
    {
        public string name { get; set; }
        public Metrics metrics { get; set; }
    }
}
