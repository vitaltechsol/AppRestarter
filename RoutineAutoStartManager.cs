using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace AppRestarter
{
    /// <summary>
    /// Manages local client-side auto-start settings for routines.
    /// This is separate from the routine data itself so auto-start preferences
    /// are client-specific and not shared when routines are imported.
    /// </summary>
    public static class RoutineAutoStartManager
    {
        private static readonly string SettingsFilePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, 
            "routine-autostart-settings.xml");

        public static Dictionary<string, bool> LoadAutoStartSettings()
        {
            var settings = new Dictionary<string, bool>();

            if (!File.Exists(SettingsFilePath))
            {
                return settings;
            }

            try
            {
                var doc = XDocument.Load(SettingsFilePath);
                var root = doc.Element("RoutineAutoStartSettings");

                if (root != null)
                {
                    foreach (var routine in root.Elements("Routine"))
                    {
                        var id = routine.Element("Id")?.Value;
                        var autoStart = bool.TryParse(routine.Element("AutoStart")?.Value, out var value) && value;

                        if (!string.IsNullOrEmpty(id))
                        {
                            settings[id] = autoStart;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading routine auto-start settings: {ex.Message}");
            }

            return settings;
        }

        public static void SaveAutoStartSettings(Dictionary<string, bool> settings)
        {
            try
            {
                var root = new XElement("RoutineAutoStartSettings");

                foreach (var kvp in settings.Where(s => s.Value)) // Only save enabled ones
                {
                    root.Add(new XElement("Routine",
                        new XElement("Id", kvp.Key),
                        new XElement("AutoStart", kvp.Value)
                    ));
                }

                var doc = new XDocument(root);
                doc.Save(SettingsFilePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving routine auto-start settings: {ex.Message}");
            }
        }

        public static bool GetAutoStart(string routineId)
        {
            var settings = LoadAutoStartSettings();
            return settings.TryGetValue(routineId, out var autoStart) && autoStart;
        }

        public static void SetAutoStart(string routineId, bool autoStart)
        {
            var settings = LoadAutoStartSettings();

            if (autoStart)
            {
                settings[routineId] = true;
            }
            else
            {
                settings.Remove(routineId);
            }

            SaveAutoStartSettings(settings);
        }
    }
}
