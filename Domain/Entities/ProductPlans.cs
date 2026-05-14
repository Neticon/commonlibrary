namespace CommonLibrary.Domain.Entities
{
    public class ProductPlans : BaseEntity
    {
        public override string _schema => "help_desk";
        public override string _table => "product_plans";
        public string? plan_id { get; set; } 
        public string? name { get; set; } 
        public int? venue_limit { get; set; }
        public int? user_limit { get; set; }
        public int? block_limit { get; set; }
        public int? retention { get; set; }
        public int? simultaneous_limit { get; set; }
        public int? service_limit { get; set; }
    }
}
