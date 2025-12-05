namespace CommonLibrary.Domain.Entities
{
    public class Event : BaseEntity
    {
        [IgnoreForSerialization]
        public override string _table => "events";
        [IgnoreForSerialization]
        public override string _schema => "help_desk";
    }
}
