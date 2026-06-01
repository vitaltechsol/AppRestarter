using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using AppRestarter.Models;

namespace AppRestarter
{
    public static class RoutineManager
    {
        private static readonly string RoutinesFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "routines.xml");

        public static List<Routine> LoadRoutines()
        {
            if (!File.Exists(RoutinesFilePath))
            {
                return new List<Routine>();
            }

            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<Routine>));
                using (StreamReader reader = new StreamReader(RoutinesFilePath))
                {
                    return (List<Routine>)serializer.Deserialize(reader);
                }
            }
            catch (Exception ex)
            {
                // Handle or log error
                Console.WriteLine($"Error loading routines: {ex.Message}");
                return new List<Routine>();
            }
        }

        public static void SaveRoutines(List<Routine> routines)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<Routine>));
                using (StreamWriter writer = new StreamWriter(RoutinesFilePath))
                {
                    serializer.Serialize(writer, routines);
                }
            }
            catch (Exception ex)
            {
                 // Handle or log error
                 Console.WriteLine($"Error saving routines: {ex.Message}");
            }
        }
    }
}
