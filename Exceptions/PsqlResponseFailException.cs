namespace CommonLibrary.Exceptions
{
    public class PsqlResponseFailException : Exception
    {
        public int StatusCode = 204;

        public PsqlResponseFailException(string message) : base(message)
        {
        }
    }
}
