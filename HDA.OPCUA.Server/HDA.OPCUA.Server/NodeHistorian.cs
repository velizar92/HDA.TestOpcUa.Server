using Opc.UaFx;
using Opc.UaFx.Server;

namespace HDA.OPCUA.Server
{
    public class NodeHistorian : IOpcNodeHistoryProvider
    {
        #region ---------- Private readonly fields ----------

        private readonly object _syncRoot;

        #endregion

        #region ---------- Private fields ----------

        private bool _autoUpdateHistory;

        #endregion

        #region ---------- Public constructors ----------

        public NodeHistorian(OpcNodeManager owner, OpcVariableNode node)
            : base()
        {
            Owner = owner;
            Node = node;

            Node.AccessLevel |= OpcAccessLevel.HistoryReadOrWrite;
            Node.UserAccessLevel |= OpcAccessLevel.HistoryReadOrWrite;
            Node.IsHistorizing = true;

            _syncRoot = new object();

            History = SqlHistory<OpcHistoryValue>
                     .Create(node.Id);

            ModifiedHistory = SqlHistory<OpcModifiedHistoryValue>
                     .Create(node.Id);
        }

        #endregion

        #region ---------- Public properties ----------

        public bool AutoUpdateHistory
        {
            get
            {
                lock (_syncRoot)
                    return _autoUpdateHistory;
            }

            set
            {
                lock (_syncRoot)
                {
                    if (_autoUpdateHistory != value)
                    {
                        _autoUpdateHistory = value;

                        if (_autoUpdateHistory)
                        {
                            Node.BeforeApplyChanges
                                    += HandleNodeBeforeApplyChanges;
                        }
                        else
                        {
                            Node.BeforeApplyChanges
                                    -= HandleNodeBeforeApplyChanges;
                        }
                    }
                }
            }
        }

        public SqlHistory<OpcHistoryValue> History
        {
            get;
        }

        public SqlHistory<OpcModifiedHistoryValue> ModifiedHistory
        {
            get;
        }

        public OpcVariableNode Node
        {
            get;
        }

        public OpcNodeManager Owner
        {
            get;
        }

        #endregion

        #region ---------- Public methods ----------

        public OpcStatusCollection CreateHistory(
                OpcContext context,
                OpcHistoryModificationInfo modificationInfo,
                OpcValueCollection values)
        {
            var results = OpcStatusCollection.Create(OpcStatusCode.Good, values.Count);

            lock (_syncRoot)
            {
                var expectedDataType = Node.DataTypeId;

                for (int index = 0; index < values.Count; index++)
                {
                    var result = results[index];
                    var value = OpcHistoryValue.Create(values[index]);

                    if (value.DataTypeId == expectedDataType)
                    {
                        if (History.Contains(value.Timestamp))
                        {
                            result.Update(OpcStatusCode.BadEntryExists);
                        }
                        else
                        {
                            History.Add(value);

                            var modifiedValue = value.CreateModified(modificationInfo);
                            ModifiedHistory.Add(modifiedValue);

                            result.Update(OpcStatusCode.GoodEntryInserted);
                        }
                    }
                    else
                    {
                        result.Update(OpcStatusCode.BadTypeMismatch);
                    }
                }
            }

            return results;
        }

        public OpcStatusCollection DeleteHistory(
                OpcContext context,
                OpcHistoryModificationInfo modificationInfo,
                IEnumerable<DateTime> times)
        {
            var results = OpcStatusCollection.Create(OpcStatusCode.Good, times.Count());

            lock (_syncRoot)
            {
                int index = 0;

                foreach (var time in times)
                {
                    var result = results[index++];

                    if (History.Contains(time))
                    {
                        var value = History[time];
                        History.RemoveAt(time);

                        var modifiedValue = value.CreateModified(modificationInfo);
                        ModifiedHistory.Add(modifiedValue);
                    }
                    else
                    {
                        result.Update(OpcStatusCode.BadNoEntryExists);
                    }
                }
            }

            return results;
        }

        public OpcStatusCollection DeleteHistory(
                OpcContext context,
                OpcHistoryModificationInfo modificationInfo,
                OpcValueCollection values)
        {
            var results = OpcStatusCollection.Create(OpcStatusCode.Good, values.Count);

            lock (_syncRoot)
            {
                for (int index = 0; index < values.Count; index++)
                {
                    var timestamp = OpcHistoryValue.Create(values[index]).Timestamp;
                    var result = results[index];

                    if (History.Contains(timestamp))
                    {
                        var value = History[timestamp];
                        History.RemoveAt(timestamp);

                        var modifiedValue = value.CreateModified(modificationInfo);
                        ModifiedHistory.Add(modifiedValue);
                    }
                    else
                    {
                        result.Update(OpcStatusCode.BadNoEntryExists);
                    }
                }
            }

            return results;
        }

        public OpcStatusCollection DeleteHistory(
                OpcContext context,
                OpcHistoryModificationInfo modificationInfo,
                DateTime? startTime,
                DateTime? endTime,
                OpcDeleteHistoryOptions options)
        {
            var results = new OpcStatusCollection();

            lock (_syncRoot)
            {
                if (options.HasFlag(OpcDeleteHistoryOptions.Modified))
                {
                    ModifiedHistory.RemoveRange(startTime, endTime);
                }
                else
                {
                    var values = History.Enumerate(startTime, endTime).ToArray();
                    History.RemoveRange(startTime, endTime);

                    for (int index = 0; index < values.Length; index++)
                    {
                        var value = values[index];
                        ModifiedHistory.Add(value.CreateModified(modificationInfo));

                        results.Add(OpcStatusCode.Good);
                    }
                }
            }

            return results;
        }

        public IEnumerable<OpcHistoryValue> ReadHistory(
                OpcContext context,
                DateTime? startTime,
                DateTime? endTime,
                OpcReadHistoryOptions options)
        {
            lock (_syncRoot)
            {
                if (options.HasFlag(OpcReadHistoryOptions.Modified))
                {
                    return ModifiedHistory
                            .Enumerate(startTime, endTime)
                            .Cast<OpcHistoryValue>()
                            .ToArray();
                }

                return History
                        .Enumerate(startTime, endTime)
                        .ToArray();
            }
        }

        public OpcStatusCollection ReplaceHistory(
                OpcContext context,
                OpcHistoryModificationInfo modificationInfo,
                OpcValueCollection values)
        {
            var results = OpcStatusCollection.Create(OpcStatusCode.Good, values.Count);

            lock (_syncRoot)
            {
                var expectedDataTypeId = Node.DataTypeId;

                for (int index = 0; index < values.Count; index++)
                {
                    var result = results[index];
                    var value = OpcHistoryValue.Create(values[index]);

                    if (value.DataTypeId == expectedDataTypeId)
                    {
                        if (History.Contains(value.Timestamp))
                        {
                            var oldValue = History[value.Timestamp];
                            History.Replace(value);

                            var modifiedValue = oldValue.CreateModified(modificationInfo);
                            ModifiedHistory.Add(modifiedValue);

                            result.Update(OpcStatusCode.GoodEntryReplaced);
                        }
                        else
                        {
                            result.Update(OpcStatusCode.BadNoEntryExists);
                        }
                    }
                    else
                    {
                        result.Update(OpcStatusCode.BadTypeMismatch);
                    }
                }
            }

            return results;
        }

        public OpcStatusCollection UpdateHistory(
                OpcContext context,
                OpcHistoryModificationInfo modificationInfo,
                OpcValueCollection values)
        {
            var results = OpcStatusCollection.Create(OpcStatusCode.Good, values.Count);

            lock (_syncRoot)
            {
                var expectedDataTypeId = Node.DataTypeId;

                for (int index = 0; index < values.Count; index++)
                {
                    var result = results[index];
                    var value = OpcHistoryValue.Create(values[index]);

                    if (value.DataTypeId == expectedDataTypeId)
                    {
                        if (History.Contains(value.Timestamp))
                        {
                            var oldValue = History[value.Timestamp];
                            History.Replace(value);

                            var modifiedValue = oldValue.CreateModified(modificationInfo);
                            ModifiedHistory.Add(modifiedValue);

                            result.Update(OpcStatusCode.GoodEntryReplaced);
                        }
                        else
                        {
                            History.Add(value);

                            var modifiedValue = value.CreateModified(modificationInfo);
                            ModifiedHistory.Add(modifiedValue);

                            result.Update(OpcStatusCode.GoodEntryInserted);
                        }
                    }
                    else
                    {
                        result.Update(OpcStatusCode.BadTypeMismatch);
                    }
                }
            }

            return results;
        }

        #endregion

        #region ---------- Private methods ----------

        private void HandleNodeBeforeApplyChanges(object sender, OpcNodeChangesEventArgs e)
        {
            var timestamp = Node.Timestamp;

            if (timestamp != null && e.IsChangeOf(OpcNodeChanges.Value))
            {
                var value = new OpcHistoryValue(Node.Value, timestamp.Value);

                if (History.Contains(value.Timestamp))
                {
                    History.Replace(value);
                }
                else
                {
                    History.Add(value);
                }
                   
            }
        }

        #endregion
    }
}
