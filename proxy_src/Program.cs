using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FLOpenServerProxy
{
    class Program
    {
        static void Main(string[] args)
        {
            bool silent = false;
            if (args.Length > 0 && args[0] == "-s")
                silent = true;

            new FLServerProxyListener(silent);
            while (true) System.Threading.Thread.Sleep(0);
        }
    }
}
