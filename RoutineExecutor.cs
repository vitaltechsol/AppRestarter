using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppRestarter.Models;

namespace AppRestarter
{
    public class RoutineExecutor
    {
        private readonly List<ApplicationDetails> _applications;

        public RoutineExecutor(List<ApplicationDetails> applications)
        {
            _applications = applications;
        }

        public async Task ExecuteRoutineAsync(Routine routine, CancellationToken cancellationToken = default)
        {
            foreach (var step in routine.Steps)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // 1. Process Wait Conditions
                foreach (var wait in step.WaitConditions)
                {
                    await ProcessWaitConditionAsync(wait, cancellationToken);
                }

                // 2. Execute Action
                if (step.Action != null)
                {
                    await ExecuteActionAsync(step.Action, cancellationToken);
                }
            }
        }

        private async Task ProcessWaitConditionAsync(WaitCondition wait, CancellationToken cancellationToken)
        {
             if (wait.Type == WaitType.TimeDelay)
             {
                 await Task.Delay(wait.WaitTimeSeconds * 1000, cancellationToken);
             }
             else if (wait.Type == WaitType.AppRunning)
             {
                 var app = _applications.FirstOrDefault(a => a.Name == wait.AppId);
                 if (app != null)
                 {
                     int checkIntervalMs = 1000;
                     int totalWaitedMs = 0;
                     int timeoutMs = wait.AppStartTimeoutSeconds * 1000;

                     while (true)
                     {
                         cancellationToken.ThrowIfCancellationRequested();

                         // Check if process is running
                         bool isRunning = IsAppRunning(app);
                         if (isRunning)
                         {
                             break;
                         }

                         if (timeoutMs > 0 && totalWaitedMs >= timeoutMs)
                         {
                             throw new TimeoutException($"Timed out waiting for app {wait.AppId} to run.");
                         }

                         await Task.Delay(checkIntervalMs, cancellationToken);
                         totalWaitedMs += checkIntervalMs;
                     }
                 }
             }
        }

        private bool IsAppRunning(ApplicationDetails app)
        {
            return ProcessTerminator.IsRunning(app);
        }

        private async Task ExecuteActionAsync(RoutineAction action, CancellationToken cancellationToken)
        {
            if (action.TargetType == TargetType.App)
            {
                 var app = _applications.FirstOrDefault(a => a.Name == action.TargetId);
                 if (app != null)
                 {
                     switch (action.Type)
                     {
                         case ActionType.Start:
                             if (!string.IsNullOrWhiteSpace(app.RestartPath))
                             {
                                 var startInfo = new System.Diagnostics.ProcessStartInfo
                                 {
                                     FileName = app.RestartPath,
                                     WorkingDirectory = System.IO.Path.GetDirectoryName(app.RestartPath),
                                     UseShellExecute = true,
                                 };
                                 if (app.StartMinimized)
                                     startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Minimized;
                                 System.Diagnostics.Process.Start(startInfo);
                             }
                             break;
                         case ActionType.Stop:
                             await ProcessTerminator.StopAsync(app, log: null);
                             break;
                         case ActionType.Restart:
                             await ProcessTerminator.StopAsync(app, log: null);
                             await Task.Delay(2000, cancellationToken);
                             if (!string.IsNullOrWhiteSpace(app.RestartPath))
                             {
                                 var startInfo = new System.Diagnostics.ProcessStartInfo
                                 {
                                     FileName = app.RestartPath,
                                     WorkingDirectory = System.IO.Path.GetDirectoryName(app.RestartPath),
                                     UseShellExecute = true,
                                 };
                                 if (app.StartMinimized)
                                     startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Minimized;
                                 System.Diagnostics.Process.Start(startInfo);
                             }
                             break;
                         case ActionType.KeyboardShortcut:
                             // TODO: Implementation for sending keys
                             break;
                         case ActionType.ClickArea:
                             // TODO: Implementation for clicking
                             break;
                     }
                 }
            }
            // TODO: Implementation for PC and Group targets
        }
    }
}
