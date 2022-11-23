using HDA.OPCUA.Server.Data;
using HDA.OPCUA.Server.Models;
using Opc.UaFx;
using System.Data;

namespace HDA.OPCUA.Server
{
    public class OpcHistoryRepository<T> where T : OpcHistoryValue
    {
        private readonly string _nodeId;
        private readonly OpcDbContext _dbContext;
        private readonly object _syncRoot;

        public OpcHistoryRepository(OpcNodeId nodeId)
        {
            _nodeId = nodeId.ToString();
            _syncRoot = new();
            _dbContext = new();
        }

        private static bool IsModifiedHistory
        {
            get => typeof(T) == typeof(OpcModifiedHistoryValue);
        }


        public bool Create(T value)
        {
            lock (_syncRoot)
            {
                if (!Exists(value.Timestamp))
                {
                    _dbContext.History.Add(new History
                    {
                        NodeId = _nodeId,
                        TimeStamp = value.Timestamp,
                        TimeStampValue = value.Timestamp.Ticks,
                        Value = value.Value.ToString(),
                        StatusCode = (long)value.Status.Code
                    });

                    if (value is OpcModifiedHistoryValue modifiedValue)
                    {
                        _dbContext.ModifiedHistory.Add(new ModifiedHistory
                        {
                            ModificationTime = modifiedValue.ModificationTime,
                            ModificationTimeValue = modifiedValue.ModificationTime.Ticks,
                            ModificationType = (byte)modifiedValue.ModificationType,
                            ModificationUserName = modifiedValue.ModificationUserName,
                        });
                    }

                    _dbContext.SaveChanges();
                    return true;
                }
            }

            return false;
        }


        public bool Delete(DateTime timestamp)
        {
            lock (_syncRoot)
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
        }

        public bool Delete(DateTime? startTime, DateTime? endTime)
        {
            lock (_syncRoot)
            {
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
        }

        public bool Exists(DateTime timestamp)
        {
            lock (_syncRoot)
            {
                var item = _dbContext.History.FirstOrDefault(e => e.NodeId == _nodeId && e.TimeStamp == timestamp);
                if (item != null)
                {
                    return true;
                }
            }

            return false;
        }

        public T Read(DateTime timestamp)
        {
            lock (_syncRoot)
            {
                var result = _dbContext.History
                    .FirstOrDefault(e => e.NodeId == _nodeId && e.TimeStamp == timestamp);

                if (result != null)
                {
                    return (T)(object)result;
                }
            }

            return default;
        }

        public IEnumerable<T> Read(DateTime? startTime, DateTime? endTime)
        {
            lock (_syncRoot)
            {
                var opcHistoryRecords = _dbContext.History
                   .Where(e => e.NodeId == _nodeId && e.TimeStamp >= startTime && e.TimeStamp <= endTime)
                   .Select(x => new OpcHistoryValue(x.Value, x.TimeStamp))
                   .ToList();

                return (IEnumerable<T>)opcHistoryRecords;
            }
        }

        public bool Update(T value)
        {
            lock (_syncRoot)
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

                if (value is OpcModifiedHistoryValue modifiedValue)
                {
                    var modifiedRecord = _dbContext.ModifiedHistory.FirstOrDefault(e => e.NodeId == _nodeId);

                    if (record == null)
                    {
                        return false;
                    }

                    modifiedRecord.ModificationTime = modifiedValue.ModificationTime;
                    modifiedRecord.ModificationTimeValue = modifiedValue.ModificationTime.Ticks;
                    modifiedRecord.ModificationType = (byte)modifiedValue.ModificationType;
                    modifiedRecord.ModificationUserName = modifiedValue.ModificationUserName;
                }

                _dbContext.SaveChanges();
                return true;

            }
        }


    }
}
