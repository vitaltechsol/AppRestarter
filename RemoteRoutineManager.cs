using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using AppRestarter.Models;

namespace AppRestarter
{
    public static class RemoteRoutineManager
    {
        private static readonly string RemoteRoutinesFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "remote-routines.xml");

        public static List<RemoteRoutineReference> LoadRemoteRoutines()
        {
            if (!File.Exists(RemoteRoutinesFilePath))
            {
                return new List<RemoteRoutineReference>();
            }

            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<RemoteRoutineReference>));
                using (StreamReader reader = new StreamReader(RemoteRoutinesFilePath))
                {
                    return (List<RemoteRoutineReference>)serializer.Deserialize(reader);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading remote routines: {ex.Message}");
                return new List<RemoteRoutineReference>();
            }
        }

        public static void SaveRemoteRoutines(List<RemoteRoutineReference> remoteRoutines)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<RemoteRoutineReference>));
                using (StreamWriter writer = new StreamWriter(RemoteRoutinesFilePath))
                {
                    serializer.Serialize(writer, remoteRoutines);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving remote routines: {ex.Message}");
            }
        }
    }
}
