namespace CommonLibrary.Domain
{
    [AttributeUsage(AttributeTargets.Property)]
    public class EncryptAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Property)]
    public class IgnoreForSerializationAttribute : Attribute
    {
    }
}
