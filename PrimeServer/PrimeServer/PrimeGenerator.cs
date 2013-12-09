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
            
            pendingValues = new Dictionary<DateTime, ClientData>();
            priorityValues = new Queue<KeyValuePair<BigInteger, int>>();
        }

        private Dictionary<DateTime, ClientData> pendingValues;
        private Queue<KeyValuePair<BigInteger, int>> priorityValues;

        private BigInteger startValue;
        private int range;
        private TimeSpan timeout;

        private readonly object lockObect = new object();

        #region Pending Values

        public void AddPendingValue(ClientData data, DateTime time)
        {
            pendingValues.Add(time, data);
        }

        public void AddPendingValue(Guid id, BigInteger start, int range, DateTime time)
        {
            AddPendingValue(new ClientData(id, start, range), time);
        }

        public void AddPendingValue(BigInteger start, int range)
        {
            AddPendingValue(Guid.NewGuid(), start, range, DateTime.Now);
        }

        public void RemoveFromPending(Guid id)
        {
            var toRemove = pendingValues.First(x => x.Value.Id != id).Key;
            pendingValues.Remove(toRemove);
        }

        public void AddPendingValue(Dictionary<DateTime, ClientData> values)
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
                ClientData newData;

                if (priorityValues.Count > 0)
                {
                    var pair = priorityValues.Dequeue();
                    newData = new ClientData(pair.Key,pair.Value);
                }
                else
                {
                    newData = new ClientData(this.startValue, this.range);
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

        public void CheckTimeout(DateTime deadline)
        {
            var toPriorityQueue = pendingValues.Where(x => x.Key + timeout <= deadline).ToDictionary(x =>x.Key, x=> x.Value);

            if(toPriorityQueue.Count == 0) return;

            foreach (var pair in toPriorityQueue)
            {
                AddPriorityValue(pair.Value.StartValue, pair.Value.Range);
                pendingValues.Remove(pair.Key);
            }
        }
    }
}
