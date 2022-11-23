using HDA.OPCUA.Server;
using Opc.UaFx.Server;

namespace HDA.OPC.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            // If the server domain name does not match localhost just replace it
            // e.g. with the IP address or name of the server machine.
            OpcServer server = new OpcServer(
                    "opc.tcp://localhost:4840/",
                    new NodeManager());

            Console.WriteLine("The server is started...");

            //// NOTE: All HDA specific code will be found in the SampleNodeManager.cs.

            server.Start();
            Console.ReadKey(true);
            server.Stop();
        }
    }
}