using System;

namespace ModLoader
{
    [AttributeUsage(AttributeTargets.Class)]
    public class PreLoaderPatchAttribute : Attribute
    {
        public string Target;
        public PreLoaderPatchAttribute(string target) { Target = target; }
    }
}