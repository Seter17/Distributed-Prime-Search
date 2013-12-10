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
            Console.WriteLine(val);
            Console.ReadKey();
        }
    }
}
