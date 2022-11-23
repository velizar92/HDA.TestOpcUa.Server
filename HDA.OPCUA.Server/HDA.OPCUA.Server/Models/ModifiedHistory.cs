using System.Data;

namespace HDA.OPCUA.Server.Models
{
    public class ModifiedHistory
    {
        public int Id { get; set; }
        public string? NodeId { get; set; }
        public DateTime TimeStamp { get; set; }
        public long TimeStampValue { get; set; }
        public string Value { get; set; }
        public long StatusCode { get; set; }
        public DateTime ModificationTime { get; set; }
        public long ModificationTimeValue { get; set; }
        public byte ModificationType { get; set; }
        public string? ModificationUserName { get; set; }
    }

}
