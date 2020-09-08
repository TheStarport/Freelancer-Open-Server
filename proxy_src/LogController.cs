using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FLOpenServerProxy
{
    public interface LogController
    {
        void AddLog(string message);
        void AddLogDebug(string message);
    }
}
