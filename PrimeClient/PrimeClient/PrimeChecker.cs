using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PrimeClient
{
    class PrimeChecker
    {
        //Also compare with Mersen number
        public static bool IsPrime(ulong number)
        {
            if (number == 2)
                return true;

            var random = new Random();

            for (int i = 0; i < 300; i++)
            {
                ulong a = ((ulong)random.Next() % (number - 2)) + 2;

                if (Gcd(a, number) != 1)
                    return false;

                if (Pows(a, number - 1, number) != 1)
                    return false;
            }

            return true;
        }

        private static ulong Gcd(ulong a, ulong b)
        {
            if (b == 0)
                return a;
            return Gcd(b, a % b);
        }

        private static ulong Mul(ulong a, ulong b, ulong m)
        {
            if (b == 1)
                return a;
            if (b % 2 == 0)
            {
                ulong t = Mul(a, b / 2, m);
                return (2 * t) % m;
            }
            return (Mul(a, b - 1, m) + a) % m;
        }


        //this shit can be faster use binary represesntaion Luke
        public static ulong Pows(ulong a, ulong b, ulong m)
        {
            if (b == 0)
                return 1;
            if (b % 2 == 0)
            {
                ulong t = Pows(a, b / 2, m);
                return Mul(t, t, m) % m;
            }
            return (Mul(Pows(a, b - 1, m), a, m)) % m;
        }
    }
}
