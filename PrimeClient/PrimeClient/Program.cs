using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PrimeClient
{
    class Program
    {
        static void Main(string[] args)
        {
            bool val = PrimeChecker.IsPrime(8191);

            Client.Connected += (sender, s) => Console.WriteLine(s);
            Client.Sent += (sender, s) => Console.WriteLine(String.Format("Sent \"{0}\" request",s));
            Client.DataRecieved += (sender, s) => Console.WriteLine(String.Format("Recieved message: \"{0}\"", s));

            Client.Instance.RequestNumbers();

            var key = Console.ReadKey();
            if (key.Key == ConsoleKey.Escape)
            {
                Client.Instance.Disconnect();
            }
        }
    }
}
