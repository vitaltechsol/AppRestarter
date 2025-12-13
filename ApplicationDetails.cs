using System.Runtime.Serialization;

namespace AppRestarter
{

    public enum RemoteActionType
    {
        AppControl,
        PcRestart,
        PcShutdown,
        AppStatusBatch
    }

    [DataContract]
    public class ApplicationDetails
    {
        [DataMember]
        public RemoteActionType ActionType { get; set; } = RemoteActionType.AppControl;

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
        public bool StopRequested { get; set; } = false;
        [DataMember]
        public bool StartRequested { get; set; } = true;
        [DataMember]
        public bool NoWarn { get; set; } = false;
        [DataMember]
        public bool StartMinimized { get; set; } = false;
        [DataMember]
        public string? GroupName { get; set; }
        [DataMember] 
        public string MachineName { get; set; }
        [DataMember]
        public List<ApplicationDetails> StatusBatchApps { get; set; }

    }

    /// <summary>
    /// DTO for batched app status requests.
    /// This is used only for STATUS polling; all existing per-app control
    /// (start/stop, restart) continues to use ApplicationDetails.
    /// </summary>
    [DataContract]
    public class AppStatusBatchRequest
    {
        [DataMember]
        public RemoteActionType ActionType { get; set; }

        [DataMember]
        public List<ApplicationDetails> Apps { get; set; }

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
