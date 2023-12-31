﻿using System.Runtime.Serialization;

namespace AppRestarter
{
    [DataContract]
    internal class ApplicationDetails
    {
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string ProcessName { get; set; }
        [DataMember]
        public string? RestartPath { get; set; }
        [DataMember]
        public string? ClientIP { get; set; }

    }
}
