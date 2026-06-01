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

    public class RoutineStep
    {
        public List<WaitCondition> WaitConditions { get; set; } = new List<WaitCondition>();
        public RoutineAction Action { get; set; }
    }

    public class WaitCondition
    {
        public WaitType Type { get; set; }
        public int WaitTimeSeconds { get; set; }
        public string AppId { get; set; }
        public int AppStartTimeoutSeconds { get; set; } // 0 means 'never' or stop routine
    }

    public enum WaitType
    {
        None,
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
}
