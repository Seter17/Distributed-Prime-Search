using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace PrimeClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Client.Connected += (sender, s) => Console.WriteLine(s.data);
            Client.Sent += (sender, s) => Console.WriteLine(String.Format("Sent \"{0}\" request",s.data));
            Client.DataRecieved += (sender, s) => Console.WriteLine(String.Format("Recieved message: \"{0}\"", s.data));
            Client.Exception += (sender, ex) => WriteException(ex.data.Message);

            try
            {
                var ip = ConfigurationManager.AppSettings["ServerIp"];
                int port = Int32.Parse(ConfigurationManager.AppSettings["ServerPort"]);
                Client.Instance.SetConnectionDetails(ip,port);
            }
            catch (Exception ex)
            {
                WriteException("Configuration data loading failed");
            }


            Client.Instance.RequestNumbers();

            var key = Console.ReadKey();
            if (key.Key == ConsoleKey.Escape)
            {
                Client.Instance.Disconnect();
            }
        }

        private static void WriteException(string s)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(s);
            Console.ResetColor();
        }
    }
}
