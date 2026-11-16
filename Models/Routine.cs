using System;
using System.Collections.Generic;

namespace AppRestarter.Models
{
    public class Routine
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
        public List<RoutineStep> Steps { get; set; } = new List<RoutineStep>();
    }

    public class RemoteRoutineReference
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>UUID of the PcInfo entry on this machine.</summary>
        public string PcId { get; set; }

        /// <summary>Cached PC name for display when the remote PC is offline.</summary>
        public string CachedPcName { get; set; }

        /// <summary>Deprecated: raw IP stored by older versions. Used only during migration to populate PcId.</summary>
        public string RemoteHost { get; set; }

        /// <summary>Deprecated: port stored by older versions. Port is now always resolved from AppSettings.WebPort.</summary>
        public int RemotePort { get; set; } = 8090;

        public string RemoteRoutineId { get; set; }
        public string CachedName { get; set; } // Cached for display
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }

    public class RoutineStep
    {
        public string Description { get; set; }
        public List<WaitCondition> WaitConditions { get; set; } = new List<WaitCondition>();
        public RoutineAction Action { get; set; }
    }

    public class WaitCondition
    {
        public WaitType Type { get; set; }
        public int WaitTimeSeconds { get; set; }
        public string AppId { get; set; }
        public int AppStartTimeoutSeconds { get; set; } // 0 means 'never' or stop routine

        public string Summary
        {
            get
            {
                switch (Type)
                {
                    case WaitType.TimeDelay:
                        return $"Wait {WaitTimeSeconds} seconds";
                    case WaitType.AppRunning:
                        return $"Wait for '{AppId}' to be running (timeout: {AppStartTimeoutSeconds}s)";
                    default:
                        return "Unknown wait";
                }
            }
        }
    }

    public enum WaitType
    {
        TimeDelay,
        AppRunning
    }

    public class RoutineAction
    {
        public ActionType Type { get; set; }
        public string TargetId { get; set; }
        public TargetType TargetType { get; set; }

        // For KeyBoard shortcuts
        public string Keys { get; set; }

        // For Clicks
        public int ClickX { get; set; }
        public int ClickY { get; set; }
        public bool DoubleClick { get; set; }
    }

    public enum ActionType
    {
        Start,
        Restart,
        Stop,
        KeyboardShortcut,
        ClickArea,
        Minimize
    }

    public enum TargetType
    {
        App,
        PC,
        Group
    }

    /// <summary>
    /// A reference to an app registered on a remote PC's app registry.
    /// Stored locally in remote-apps.xml. The remote PC is identified by PcId (UUID of a PcInfo).
    /// CachedProcessName and CachedRestartPath are stored as a TCP fallback while the remote PC
    /// is running an older version that does not yet support /apps/control.
    /// </summary>
    public class RemoteAppReference
    {
        /// <summary>Local unique ID for this reference.</summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>UUID of the PcInfo entry that hosts this app.</summary>
        public string PcId { get; set; }

        /// <summary>Cached PC display name for use when the remote PC is offline.</summary>
        public string CachedPcName { get; set; }

        /// <summary>UUID of the app as registered in the remote PC's app registry. Null while pending resolution.</summary>
        public string RemoteAppId { get; set; }

        /// <summary>Cached app display name.</summary>
        public string CachedName { get; set; }

        /// <summary>Cached process name — used as TCP fallback payload.</summary>
        public string CachedProcessName { get; set; }

        /// <summary>Cached restart path — used as TCP fallback payload.</summary>
        public string CachedRestartPath { get; set; }

        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }
}
