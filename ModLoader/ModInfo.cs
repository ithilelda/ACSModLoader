using System;
using System.Reflection;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace ModLoader
{
    [Serializable]
    public class ModInfo
    {
        public string Name = "";
        public string DisplayName = "";
        public string Author = "";
        public string Desc = "";
        public string AssemblyFile = "";
        public string EntranceType = "";
        public string EntranceMethod = "";
        [XmlIgnore]
        public Assembly LoadedAssembly;
    }
}
