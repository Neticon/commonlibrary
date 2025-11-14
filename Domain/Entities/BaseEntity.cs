namespace CommonLibrary.Domain.Entities
{
    public class BaseEntity
    {
        [IgnoreForSerialization]
        public virtual string _table { get; }
        [IgnoreForSerialization]
        public virtual string _schema { get; }
        public virtual List<string> _encryptedFields {  get; }

        public static (string _table, string _schema) GetMeta<T>() where T : BaseEntity, new()
        {
            var entity = new T();
            return (entity._table, entity._schema);
        }

        public static List<string> GetEncryptedFields<T>() where T : BaseEntity, new()
        {
            var entity = new T();
            return (entity._encryptedFields);
        }
    }
}
