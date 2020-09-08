using System;

namespace FLServer
{
    internal class FLDataFileException : Exception
    {
        public FLDataFileException(string msg) : base(msg)
        {
        }
    }
}