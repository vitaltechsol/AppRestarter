using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using AppRestarter.Models;

namespace AppRestarter
{
    public static class RemoteAppManager
    {
        private static readonly string RemoteAppsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "remote-apps.xml");

        public static List<RemoteAppReference> LoadRemoteApps()
        {
            if (!File.Exists(RemoteAppsFilePath))
                return new List<RemoteAppReference>();

            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<RemoteAppReference>));
                using (StreamReader reader = new StreamReader(RemoteAppsFilePath))
                {
                    return (List<RemoteAppReference>)serializer.Deserialize(reader);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading remote apps: {ex.Message}");
                return new List<RemoteAppReference>();
            }
        }

        public static void SaveRemoteApps(List<RemoteAppReference> remoteApps)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<RemoteAppReference>));
                using (StreamWriter writer = new StreamWriter(RemoteAppsFilePath))
                {
                    serializer.Serialize(writer, remoteApps);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving remote apps: {ex.Message}");
            }
        }
    }
}
