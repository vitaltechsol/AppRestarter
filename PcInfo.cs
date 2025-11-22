using System.Runtime.Serialization;

namespace AppRestarter
{
    [DataContract]
    public class PcInfo
    {
        [DataMember]
        public string Name { get; set; } = "";

        [DataMember]
        public string IP { get; set; } = "";
    }
}