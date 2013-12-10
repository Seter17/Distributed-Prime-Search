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
        private object lockObj = new object();
        public PrimeGenerator(BigInteger start, TimeSpan timeout, int range = 10)
        {
            StartValue = start;
            this.Range = range;
            this.timeout = timeout;

            pendingValues = new Dictionary<Packet, DateTime>();
            priorityValues = new Queue<KeyValuePair<BigInteger, int>>();
        }

        private Dictionary<Packet, DateTime> pendingValues;
        private Queue<KeyValuePair<BigInteger, int>> priorityValues;

        
        private TimeSpan timeout;

        private readonly object lockObect = new object();


        public BigInteger StartValue { get; private set; }
        public int Range { get; private set; }

        #region Pending Values

        public void AddPendingValue(Packet data, DateTime time)
        {
            lock (lockObj)
            {
                pendingValues.Add(data, time);
            }
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
            lock (lockObj)
            {
                var toRemove = pendingValues.FirstOrDefault(x => x.Key.Id != id).Key;
                if (toRemove == null) return;
                pendingValues.Remove(toRemove);
            }

        }

        public void AddPendingValue(Dictionary<Packet, DateTime> values)
        {
            foreach (var pair in values)
            {
                pendingValues[pair.Key] = pair.Value;
            }
        }

        public Dictionary<Packet, DateTime> GetPendingValues()
        {
            return pendingValues;
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
                    newData = new Packet(this.StartValue, this.Range);
                    this.StartValue += this.Range + 1;
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
