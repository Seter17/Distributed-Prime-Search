using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace PrimeServer
{
    public static class Utilities
    {
        public static bool Connected(this Socket s)
        {
            var polled = s.Poll(1000, SelectMode.SelectRead);
            var rested = (s.Available == 0);
            return !(polled & rested);
        }
    }
}
