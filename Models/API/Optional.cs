public struct Optional<T>
{
    public bool HasValue { get; set; }
    public T? Value { get; set; }

    public Optional(T? value)
    {
        HasValue = true;
        Value = value;
    }
}
