using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace PrimeServer
{
    public class PrimeLoader
    {
        private string primeDataFilePath, pendingValuesFilePath;

        public PrimeLoader(string primeDataFilePath, string pendingValuesFilePath)
        {
            this.primeDataFilePath = primeDataFilePath;
            this.pendingValuesFilePath = pendingValuesFilePath;
        }

        public void SavePrimeData(List<BigInteger> data)
        { }

        public void SavePendingValues(Dictionary<Packet, DateTime> pendingValues)
        { }

        public Dictionary<Packet, DateTime> LoadPendingValues(out BigInteger startValue, out int range)
        {
            startValue = BigInteger.Zero;
            range = 10;
            return new Dictionary<Packet, DateTime>();
        }
    }
}
