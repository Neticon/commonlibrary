namespace CommonLibrary.Models
{
    public class VenuePublicationState
    {
        public Guid venue_id { get; set; }
        public VenuePublicationStatus status { get; set; }
        public string Message { get; set; }
        public string Step { get; set; }
    }

    public enum VenuePublicationStatus
    {
        idle,
        busy,
        error
    }
}
