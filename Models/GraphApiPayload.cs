namespace CommonLibrary.Models
{
    public class GraphApiPayload
    {
        public object data { get; set; }
        public object filters { get; set; }
    }

    public class EntityPayload<T>
    {
        public T? data { get; set; }
        public object? filters { get; set; }
    }
}
