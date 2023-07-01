using Opc.UaFx;
using Opc.UaFx.Server;

namespace HDA.OPCUA.Server
{
    public class NodeHistorian : IOpcNodeHistoryProvider
    {
        #region ---------- Private fields ----------

        private bool _autoUpdateHistory;

        #endregion

        #region ---------- Public constructors ----------

        public NodeHistorian(OpcNodeManager owner, OpcVariableNode node)
            : base()
        {
            Owner = owner ?? throw new ArgumentNullException($"Argument {nameof(owner)} is null"); 
            Node = node ?? throw new ArgumentNullException($"Argument {nameof(node)} is null");

            Node.AccessLevel |= OpcAccessLevel.HistoryReadOrWrite;
            Node.UserAccessLevel |= OpcAccessLevel.HistoryReadOrWrite;
            Node.IsHistorizing = true;

            History = new HistoryService(node.Id.ToString());
        }

        #endregion

        #region ---------- Public properties ----------

        public bool AutoUpdateHistory
        {
            get
            {
                return _autoUpdateHistory;
            }

            set
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

        public HistoryService History
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
                        result.Update(OpcStatusCode.GoodEntryInserted);
                    }
                }
                else
                {
                    result.Update(OpcStatusCode.BadTypeMismatch);
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

            int index = 0;

            foreach (var time in times)
            {
                var result = results[index++];

                if (History.Contains(time))
                {                  
                    History.RemoveAt(time);
                }
                else
                {
                    result.Update(OpcStatusCode.BadNoEntryExists);
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

            for (int index = 0; index < values.Count; index++)
            {
                var timestamp = OpcHistoryValue.Create(values[index]).Timestamp;
                var result = results[index];

                if (History.Contains(timestamp))
                {                  
                    History.RemoveAt(timestamp);
                }
                else
                {
                    result.Update(OpcStatusCode.BadNoEntryExists);
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

            var values = History.Enumerate(startTime, endTime).ToArray();
            History.RemoveRange(startTime, endTime);

            for (int index = 0; index < values.Length; index++)
            {
                results.Add(OpcStatusCode.Good);
            }

            return results;
        }

        public IEnumerable<OpcHistoryValue> ReadHistory(
                OpcContext context,
                DateTime? startTime,
                DateTime? endTime,
                OpcReadHistoryOptions options)
        {

            return History
                    .Enumerate(startTime, endTime)
                    .ToArray();
        }

        public OpcStatusCollection ReplaceHistory(
                OpcContext context,
                OpcHistoryModificationInfo modificationInfo,
                OpcValueCollection values)
        {
            var results = OpcStatusCollection.Create(OpcStatusCode.Good, values.Count);

            var expectedDataTypeId = Node.DataTypeId;

            for (int index = 0; index < values.Count; index++)
            {
                var result = results[index];
                var value = OpcHistoryValue.Create(values[index]);

                if (value.DataTypeId == expectedDataTypeId)
                {
                    if (History.Contains(value.Timestamp))
                    {
                        History.Update(value);
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

            return results;
        }

        public OpcStatusCollection UpdateHistory(
                OpcContext context,
                OpcHistoryModificationInfo modificationInfo,
                OpcValueCollection values)
        {
            var results = OpcStatusCollection.Create(OpcStatusCode.Good, values.Count);

            var expectedDataTypeId = Node.DataTypeId;

            for (int index = 0; index < values.Count; index++)
            {
                var result = results[index];
                var value = OpcHistoryValue.Create(values[index]);

                if (value.DataTypeId == expectedDataTypeId)
                {
                    if (History.Contains(value.Timestamp))
                    {
                        History.Update(value);
                        result.Update(OpcStatusCode.GoodEntryReplaced);
                    }
                    else
                    {
                        History.Add(value);
                        result.Update(OpcStatusCode.GoodEntryInserted);
                    }
                }
                else
                {
                    result.Update(OpcStatusCode.BadTypeMismatch);
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
                    History.Update(value);
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
