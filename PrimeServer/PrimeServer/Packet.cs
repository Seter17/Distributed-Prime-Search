using System;
using System.Numerics;

namespace PrimeServer
{
    public class Packet
    {
        public Packet(Guid id, BigInteger startValue, int range  = 10)
        {
            Id = id;
            StartValue = startValue;
            Range = range;
        }

        public Packet(BigInteger startValue, int range) : this(Guid.NewGuid(), startValue, range) { }

        public Guid Id
        {
            get;
            private set;
        }

        public BigInteger StartValue { get; private set; }
        public int Range { get; private set; }

        public override string ToString()
        {
            return String.Format("{0} {1} {2}", Id, StartValue, Range);
        }
    }
}