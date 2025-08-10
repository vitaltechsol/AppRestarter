using System.Runtime.Serialization;

namespace AppRestarter
{
    [DataContract]
    public class ApplicationDetails
    {
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string ProcessName { get; set; }
        [DataMember]
        public string? RestartPath { get; set; }
        [DataMember]
        public string? ClientIP { get; set; }
        [DataMember]
        public bool AutoStart { get; set; }
        [DataMember]
        public int AutoStartDelayInSeconds { get; set; } = 0;
        [DataMember]
        public bool NoKill { get; set; } = false;

    }

    public class AppSettings
    {
        public string Schema { get; set; } = "x.x.x";
        public int WebPort { get; set; } = 8080;
        public int AppPort { get; set; } = 2024;
        public bool AutoStartWithWindows { get; set; } = true;
    }
}
