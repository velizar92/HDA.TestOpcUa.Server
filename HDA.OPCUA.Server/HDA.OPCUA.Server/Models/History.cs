using System.Data;

namespace HDA.OPCUA.Server.Models
{
    public class History
    {
        public int Id { get; set; }
        public string NodeId { get; set; }
        public DateTime TimeStamp { get; set; }
        public long TimeStampValue { get; set; }
        public string Value { get; set; }
        public long StatusCode { get; set; }
    }
}
