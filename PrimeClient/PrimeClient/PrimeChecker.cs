using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PrimeServer;

namespace PrimeClient
{
    class PrimeChecker
    {

        public static void CheckRange(BigInteger startValue, int range, ref List<BigInteger> primes)
        {
            //test interval [startValue; startValue + range] for prime numbers
            var result = primes;
            var tasks = new Task[range + 1];
            for (var i = 0; i <= range; ++i)
            {
                var incremet =  (BigInteger)i;

                tasks[i] = Task.Factory.StartNew(() =>
                                      {
                                          var valueToTest = startValue + incremet;
                                          if (IsPrime(valueToTest))
                                              result.Add(valueToTest);
                                      });
            }

            Task.WaitAll(tasks, 1000);

            primes = result;
        }

        //Also we should compare with Mersen number
        public static bool IsPrime(BigInteger number)
        {
            if (number == 2)
                return true;

            var random = new Random();

            for (int i = 0; i < 300; i++)
            {
                BigInteger a = ((BigInteger)random.Next() % (number - 2)) + 2;

                if (Gcd(a, number) != 1)
                    return false;

                if (Pows(a, number - 1, number) != 1)
                    return false;
            }

            return true;
        }

        private static BigInteger Gcd(BigInteger a, BigInteger b)
        {
            if (b == 0)
                return a;
            return Gcd(b, a % b);
        }

        private static BigInteger Mul(BigInteger a, BigInteger b, BigInteger m)
        {
            if (b == 1)
                return a;
            if (b%2 != 0) return (Mul(a, b - 1, m) + a)%m;
            var t = Mul(a, b / 2, m);
            return (2 * t) % m;
        }


        public static BigInteger Pows(BigInteger a, BigInteger b, BigInteger m)
        {
            var binaryString = b.ToBinaryString();

            BigInteger factor = a;
            BigInteger result = 1;

            for (var i = binaryString.Length - 1; i >= 0; i--)
            {
                factor = Mul(factor, factor, m);
                if (binaryString[i].Equals('1'))
                {
                    result = Mul(factor, result, m);
                }
            }

            return result;
        }
    }
}
