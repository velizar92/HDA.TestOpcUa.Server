using HDA.OPCUA.Server.Data;
using HDA.OPCUA.Server.Models;
using Opc.UaFx;

namespace HDA.OPCUA.Server
{
    public class HistoryService
    {
        private readonly string _nodeId;
        private readonly OpcDbContext _dbContext;

        public HistoryService(string nodeId)
        {
            _nodeId = nodeId;
            _dbContext = new();
        }

        public OpcHistoryValue? this[DateTime timestamp]
        {
            get => Read(timestamp);
        }


        public bool Add(OpcHistoryValue value)
        {
            if (Contains(value.Timestamp) == false)
            {
                _dbContext.History.Add(new History
                {
                    NodeId = _nodeId,
                    TimeStamp = value.Timestamp,
                    TimeStampValue = value.Timestamp.Ticks,
                    Value = value.Value.ToString(),
                    StatusCode = (long)value.Status.Code
                });

                _dbContext.SaveChanges();
                return true;
            }

            return false;
        }


        public bool RemoveAt(DateTime timestamp)
        {
            var record = _dbContext.History
                .FirstOrDefault(e => e.NodeId == _nodeId && e.TimeStamp == timestamp);

            if (record != null)
            {
                _dbContext.Remove(record);
                _dbContext.SaveChanges();
                return true;
            }

            return false;

        }

        public bool RemoveRange(DateTime? startTime, DateTime? endTime)
        {
            AdjustRange(ref startTime, ref endTime);

            var record = _dbContext.History
               .FirstOrDefault(e => e.NodeId == _nodeId && e.TimeStamp >= startTime && e.TimeStamp <= endTime);

            if (record != null)
            {
                _dbContext.Remove(record);
                _dbContext.SaveChanges();
                return true;
            }

            return false;
        }

        public bool Contains(DateTime timestamp)
        {
            var item = _dbContext.History.FirstOrDefault(e => e.NodeId == _nodeId && e.TimeStamp == timestamp);

            if (item != null)
            {
                return true;
            }

            return false;
        }

        public OpcHistoryValue? Read(DateTime timestamp)
        {
            var result = _dbContext.History
                .FirstOrDefault(e => e.NodeId == _nodeId && e.TimeStamp == timestamp);

            if (result != null)
            {
                return (OpcHistoryValue)(object)result;
            }

            return default;
        }

        public IEnumerable<OpcHistoryValue> Read(DateTime? startTime, DateTime? endTime)
        {
            var opcHistoryRecords = _dbContext.History
               .Where(e => e.NodeId == _nodeId && e.TimeStamp >= startTime && e.TimeStamp <= endTime)
               .Select(x => new OpcHistoryValue(x.Value, x.TimeStamp))
               .ToList();

            return opcHistoryRecords;
        }

        public bool Update(OpcHistoryValue value)
        {
            var record = _dbContext.History.FirstOrDefault(e => e.NodeId == _nodeId);

            if (record == null)
            {
                return false;
            }

            record.TimeStamp = value.Timestamp;
            record.TimeStampValue = value.Timestamp.Ticks;
            record.Value = value.Value.ToString();
            record.StatusCode = (long)value.Status.Code;

            _dbContext.SaveChanges();
            return true;
        }

        public IEnumerable<OpcHistoryValue> Enumerate(DateTime? startTime, DateTime? endTime)
        {
            AdjustRange(ref startTime, ref endTime);

            var values = Read(startTime, endTime);

            foreach (var value in values)
            {
                yield return value;
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
