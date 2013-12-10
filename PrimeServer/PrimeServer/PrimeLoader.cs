using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace PrimeServer
{
    public class PrimeLoader
    {
        private string primeDataFilePath, pendingValuesFilePath;
        private TextWriter sw;
        private object lockObj = new object();

        public PrimeLoader(string primeDataFilePath, string pendingValuesFilePath)
        {
            this.primeDataFilePath = primeDataFilePath;
            this.pendingValuesFilePath = pendingValuesFilePath;

            sw = new StreamWriter(primeDataFilePath, true);
        }

        public void SavePrimeData(List<BigInteger> data)
        {
            data.ForEach(integer => TextWriter.Synchronized(sw).WriteLine(integer.ToString()));
            sw.Flush();
        }

        public void SavePendingValues(Dictionary<Packet, DateTime> pendingValues)
        {
            
        }

        public Dictionary<Packet, DateTime> LoadPendingValues(out BigInteger startValue, out int range)
        {
            startValue = BigInteger.Zero;
            range = 10;
            return new Dictionary<Packet, DateTime>();
        }
    }
}
