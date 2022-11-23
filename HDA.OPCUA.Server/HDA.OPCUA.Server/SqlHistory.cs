using Opc.UaFx;

namespace HDA.OPCUA.Server
{
    public class SqlHistory<T> where T : OpcHistoryValue
    {
        
        private readonly OpcHistoryRepository<T> _repository;

        public T this[DateTime timestamp]
        {
            get => _repository.Read(timestamp);
        }


        private SqlHistory(OpcHistoryRepository<T> repository)
            : base()
        {
            _repository = repository;
        }

       
        public static SqlHistory<T> Create(OpcNodeId nodeId)
        {
            if (typeof(T) != typeof(OpcHistoryValue) && typeof(T) != typeof(OpcModifiedHistoryValue))
            {
                throw new ArgumentException();
            }
                   
            return new SqlHistory<T>(new OpcHistoryRepository<T>(nodeId));
        }

        public void Add(T value)
        {
            if (!_repository.Create(value))
            {
                throw new ArgumentException(string.Format(
                        "An item with the timestamp '{0}' does already exist.",
                        value.Timestamp));
            }
        }

        public bool Contains(DateTime timestamp)
        {
            return _repository.Exists(timestamp);
        }

        public IEnumerable<T> Enumerate(DateTime? startTime, DateTime? endTime)
        {
            AdjustRange(ref startTime, ref endTime);

            foreach (var value in _repository.Read(startTime, endTime))
            {
                yield return value;
            }               
        }

        public bool IsEmpty()
        {
            return !Enumerate(startTime: null, endTime: null).Any();
        }

        public void RemoveAt(DateTime timestamp)
        {
            if (!_repository.Delete(timestamp))
            {
                throw new ArgumentOutOfRangeException(string.Format(
                        "The timestamp '{0}' is out of the history range.",
                        timestamp));
            }
        }

        public void RemoveRange(DateTime? startTime, DateTime? endTime)
        {
            AdjustRange(ref startTime, ref endTime);
            _repository.Delete(startTime, endTime);
        }

        public void Replace(T value)
        {
            if (!_repository.Update(value))
            {
                throw new ArgumentException(string.Format(
                        "An item with the timestamp '{0}' does not exist.",
                        value.Timestamp));
            }
        }


        #region ---------- Private static methods ----------

        private static void AdjustRange(ref DateTime? startTime, ref DateTime? endTime)
        {
            if (startTime == DateTime.MinValue || startTime == DateTime.MaxValue)
            {
                startTime = null;
            }         

            if (endTime == DateTime.MinValue || endTime == DateTime.MaxValue)
            {
                endTime = null;
            }
                
            if (startTime > endTime)
            {
                var tempTime = startTime;

                startTime = endTime;
                endTime = tempTime;
            }
        }

        #endregion
    }
}
