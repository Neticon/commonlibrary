namespace CommonLibrary.Models.API
{
    public class DeleteVenueModel
    {
        public DeleteVenueModelData data { get; set; }
        public DeleteVenueModelFilter filters { get; set; }

    }

    public class DeleteVenueModelData
    {
        public bool is_deleted { get; set; }
    }

    public class DeleteVenueModelFilter
    {
        public string venue_id { get; set; }
        public string tenant_id { get; set; }
    }
}
