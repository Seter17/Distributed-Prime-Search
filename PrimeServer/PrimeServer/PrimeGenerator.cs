using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace PrimeServer
{
    public class PrimeGenerator
    {
        public PrimeGenerator(BigInteger start, TimeSpan timeout, int range = 10)
        {
            startValue = start;
            this.range = range;
            this.timeout = timeout;

            pendingValues = new Dictionary<Packet, DateTime>();
            priorityValues = new Queue<KeyValuePair<BigInteger, int>>();
        }

        private Dictionary<Packet, DateTime> pendingValues;
        private Queue<KeyValuePair<BigInteger, int>> priorityValues;

        private BigInteger startValue;
        private int range;
        private TimeSpan timeout;

        private readonly object lockObect = new object();

        #region Pending Values

        public void AddPendingValue(Packet data, DateTime time)
        {
            pendingValues.Add(data, time);
        }

        public void AddPendingValue(Guid id, BigInteger start, int range, DateTime time)
        {
            AddPendingValue(new Packet(id, start, range), time);
        }

        public void AddPendingValue(BigInteger start, int range)
        {
            AddPendingValue(Guid.NewGuid(), start, range, DateTime.Now);
        }

        public void RemoveFromPending(Guid id)
        {
            var toRemove = pendingValues.First(x => x.Key.Id != id).Key;
            pendingValues.Remove(toRemove);
        }

        public void AddPendingValue(Dictionary<Packet, DateTime> values)
        {
            foreach (var pair in values)
            {
                pendingValues[pair.Key] = pair.Value;
            }
        }

        
        #endregion

        #region Prioriy Values

        //TODO:: check priority values for crossing intervals (could be possible when range is changing)
        private void AddPriorityValue(BigInteger value, int range)
        {
            priorityValues.Enqueue(new KeyValuePair<BigInteger, int>(value, range));
        }

        #endregion

        public string GenerateNewValueMessage()
        {
            lock (lockObect)
            {
                Packet newData;

                if (priorityValues.Count > 0)
                {
                    var pair = priorityValues.Dequeue();
                    newData = new Packet(pair.Key,pair.Value);
                }
                else
                {
                    newData = new Packet(this.startValue, this.range);
                    this.startValue += this.range + 1;
                }
                
                this.AddPendingValue(newData, DateTime.Now);
                return newData.ToString();
            }

        }

        public void CheckTimeout()
        {
            CheckTimeout(DateTime.Now);
        }

        private void CheckTimeout(DateTime deadline)
        {
            var toPriorityQueue = pendingValues.Where(x => x.Value + timeout <= deadline).ToDictionary(x =>x.Key, x=> x.Value);

            if(toPriorityQueue.Count == 0) return;

            foreach (var pair in toPriorityQueue)
            {
                AddPriorityValue(pair.Key.StartValue, pair.Key.Range);
                pendingValues.Remove(pair.Key);
            }
        }
    }
}
