using System.Runtime.Serialization;

namespace AppRestarter
{
    [DataContract]
    public class PcInfo
    {
        [DataMember]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [DataMember]
        public string Name { get; set; } = "";

        [DataMember]
        public string IP { get; set; } = "";

        [DataMember]
        public bool Enabled { get; set; } = true;
    }
}