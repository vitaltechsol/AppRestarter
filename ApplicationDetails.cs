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
        public bool KillProcess { get; set; } = false;
        [DataMember]
        public bool NoWarn { get; set; } = false;
        [DataMember]
        public bool StartMinimized { get; set; } = false;
        [DataMember]
        public string? GroupName { get; set; }



    }

    public class AppSettings
    {
        public string Schema { get; set; } = "1.2.0";
        public int WebPort { get; set; } = 8090;
        public int AppPort { get; set; } = 2024;
        public bool AutoStartWithWindows { get; set; } = true;
        public bool StartMinimized { get; set; } = false;   
    }
}
