namespace ServicePortal.Application.Models
{
    public class S3FileMetadata
    {
        public string Key { get; set; } = default!;
        public long? Size { get; set; }
        public DateTime? LastModified { get; set; }
    }
}
