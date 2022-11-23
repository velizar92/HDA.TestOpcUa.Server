using Opc.UaFx.Server;
using Opc.UaFx;
using HDA.OPCUA.Server.Data;

namespace HDA.OPCUA.Server
{
    public class NodeManager : OpcNodeManager
    {
        private NodeHistorian _positionHistorian;
        private NodeHistorian _drawingNumberHistorian;
        private OpcDbContext _dbContext = new();

        public NodeManager()
             : base("http://myserver/")
        {
        }

        protected override IEnumerable<IOpcNode> CreateNodes(OpcNodeReferenceCollection references)
        {
            OpcFolderNode rootNode = new OpcFolderNode("RootNode");

            references.Add(rootNode, OpcObjectTypes.ObjectsFolder);

            _positionHistorian = new NodeHistorian(
                    this, new OpcDataVariableNode<int>(rootNode, "Position", -1));

            _drawingNumberHistorian = new NodeHistorian(
                   this, new OpcDataVariableNode<string>(rootNode, "DrawingNumber", string.Empty));

            _positionHistorian.AutoUpdateHistory = true;
            _drawingNumberHistorian.AutoUpdateHistory = true;

            CreateDatabaseIfNotCreated();

            return new IOpcNode[] { rootNode };
        }


        protected override IOpcNodeHistoryProvider RetrieveNodeHistoryProvider(IOpcNode node)
        {
            if (_positionHistorian.Node == node)
                return _positionHistorian;

            if (_drawingNumberHistorian.Node == node)
                return _drawingNumberHistorian;

            return base.RetrieveNodeHistoryProvider(node);
        }

        private void CreateDatabaseIfNotCreated()
        {
            _dbContext.Database.EnsureCreated();
        }


    }
}
