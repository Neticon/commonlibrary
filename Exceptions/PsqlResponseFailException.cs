﻿using System.Net;

namespace CommonLibrary.Exceptions
{
    public class PsqlResponseFailException : Exception
    {
        public int StatusCode = 400;

        public PsqlResponseFailException(string message): base(message)
        {
        }
    }
}
