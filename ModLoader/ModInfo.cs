using System;
using System.Reflection;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ModLoader
{
    public class ModInfo
    {
        public string Name = "";
        public string DisplayName = "";
        public string Author = "";
        public string Desc = "";
        public string AssemblyFile = "";
        public string EntranceType = "";
        public string EntranceMethod = "";
        [JsonIgnore]
        public Assembly LoadedAssembly;
    }
}
