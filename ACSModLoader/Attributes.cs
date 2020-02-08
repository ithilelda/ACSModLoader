using System;

namespace ModLoader
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class RequireAttribute : Attribute
    {
        public string Dependency;
        public string Version;
        
        public RequireAttribute(string dep)
        {
            Dependency = dep ?? throw new ArgumentNullException("dep");
        }
        public RequireAttribute(string dep, string ver)
        {
            Dependency = dep ?? throw new ArgumentNullException("dep");
            Version = ver;
        }
    }
}