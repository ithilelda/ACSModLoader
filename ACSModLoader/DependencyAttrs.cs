using System;

namespace ModLoader
{
    public class ModRequire : Attribute
    {
        public string Dependency;
        public string Version;
        
        public ModRequire(string dep)
        {
            Dependency = dep ?? throw new ArgumentNullException("dep");
        }
        public ModRequire(string dep, string ver)
        {
            Dependency = dep ?? throw new ArgumentNullException("dep");
            Version = ver;
        }
    }
}