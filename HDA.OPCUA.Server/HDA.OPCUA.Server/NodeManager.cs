using Opc.UaFx.Server;
using Opc.UaFx;
using HDA.OPCUA.Server.Data;
using Microsoft.EntityFrameworkCore;
using Opc.Ua;

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

            CreateDatabaseIfNotCreated();

            _positionHistorian = new NodeHistorian(
                    this, new OpcDataVariableNode<int>(rootNode, "Position", TryParse(GetLastOpcValue("s=RootNode/Position"))));

            _drawingNumberHistorian = new NodeHistorian(
                   this, new OpcDataVariableNode<string>(rootNode, "DrawingNumber", GetLastOpcValue("s=RootNode/DrawingNumber")));

            _positionHistorian.AutoUpdateHistory = true;
            _drawingNumberHistorian.AutoUpdateHistory = true;


            return new IOpcNode[] { rootNode };
        }


        protected override IOpcNodeHistoryProvider RetrieveNodeHistoryProvider(IOpcNode node)
        {
            if (_positionHistorian.Node == node)
            {
                return _positionHistorian;
            }
                
            if (_drawingNumberHistorian.Node == node)
            {
                return _drawingNumberHistorian;
            }        

            return base.RetrieveNodeHistoryProvider(node);
        }


        private void CreateDatabaseIfNotCreated()
        {
            _dbContext.Database.EnsureCreated();
        }


        private string GetLastOpcValue(string nodeId)
        {
            var nodeValue = _dbContext.History
                    .OrderByDescending(x => x.Id)
                    .FirstOrDefault(x => x.NodeId == nodeId);

            if (nodeValue != null)
            {
                return nodeValue.Value;
            }

            return string.Empty;

        }

        private int TryParse(string value)
        {
            try
            {
                return int.Parse(value);
            }
            catch (FormatException)
            {
                return 0;
            }    
        }
    }
}
