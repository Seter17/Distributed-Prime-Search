using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Numerics;
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

        public static string EraseEnding(this string s)
        {
            var index = s.IndexOf("<EOF>", System.StringComparison.Ordinal);
            return index != -1
                ? s.Remove(s.IndexOf("<EOF>", System.StringComparison.Ordinal))
                : s;
        }

        public static string ToBinaryString(this BigInteger bigint)
        {
            var bytes = bigint.ToByteArray();
            var idx = bytes.Length - 1;

            // Create a StringBuilder having appropriate capacity.
            var base2 = new StringBuilder(bytes.Length * 8);

            // Convert first byte to binary.
            var binary = Convert.ToString(bytes[idx], 2);

            // Ensure leading zero exists if value is positive.
            if (binary[0] != '0' && bigint.Sign == 1)
            {
                base2.Append('0');
            }

            // Append binary string to StringBuilder.
            base2.Append(binary);

            // Convert remaining bytes adding leading zeros.
            for (idx--; idx >= 0; idx--)
            {
                base2.Append(Convert.ToString(bytes[idx], 2).PadLeft(8, '0'));
            }

            return base2.ToString();
        }
    }
}
