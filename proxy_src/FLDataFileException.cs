using System;
using System.Collections.Generic;
using System.Text;

namespace FLOpenServerProxy
{
    class FLDataFileException : Exception
    {
        public FLDataFileException(string msg) : base(msg) { }
    }
}
