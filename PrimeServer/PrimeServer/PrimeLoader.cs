using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace PrimeServer
{
    public class PrimeLoader
    {
        private string pendingValuesFilePath;
        private TextWriter sw;
        private object lockObj = new object();

        public PrimeLoader(string primeDataFilePath, string pendingValuesFilePath)
        {

            this.pendingValuesFilePath = pendingValuesFilePath;

            if(sw != null)
                sw.Close();

            sw = new StreamWriter(primeDataFilePath, true);
        }

        public void SavePrimeData(List<BigInteger> data)
        {
            data.ForEach(integer => TextWriter.Synchronized(sw).WriteLine(integer.ToString()));
            sw.Flush();
        }

        public void SavePendingValues(Dictionary<Packet, DateTime> pendingValues, BigInteger startValue, int range)
        {
            try
            {
                var doc = new XElement("Pednings", new XAttribute("range", range), new XAttribute("start", startValue));
                foreach (var pendingValue in pendingValues)
                {
                    var pending = new XElement("Pending");

                    var id = new XElement("Id", pendingValue.Key.Id);
                    var start = new XElement("Start", pendingValue.Key.StartValue);
                    var rangeNode = new XElement("Range", pendingValue.Key.Range);
                    var time = new XElement("Time", pendingValue.Value);

                    pending.Add(id, start, rangeNode, time);

                    doc.Add(pending);
                }

                doc.Save(pendingValuesFilePath);
            }
            catch (Exception ex)
            {
                var str = ex.Message;
            }

        }

        public Dictionary<Packet, DateTime> LoadPendingValues(out BigInteger startValue, out int range)
        {
            var pendingValues = new Dictionary<Packet, DateTime>();

            try
            {
                var doc = XElement.Load(pendingValuesFilePath);

                startValue = BigInteger.Parse(doc.Attribute("start").Value);
                range = Int32.Parse(doc.Attribute("range").Value);

                if (doc.HasElements)
                {
                    foreach (var element in doc.Elements("Pending"))
                    {
                        var id = Guid.Parse(element.Element("Id").Value);
                        var start = BigInteger.Parse(element.Element("Start").Value);
                        var rangeNode = Int32.Parse(element.Element("Range").Value);
                        var time = DateTime.Parse(element.Element("Time").Value);

                        var packet = new Packet(id, start, rangeNode);
                        pendingValues.Add(packet,time);
                    }
                }
            }
            catch (Exception)
            {
                startValue = BigInteger.Zero;
                range = 10;
            }

            return pendingValues;
        }

        public void Close()
        {
            sw.Close();
        }
    }
}
