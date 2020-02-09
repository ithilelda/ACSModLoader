using System;

namespace ModLoader
{
    [AttributeUsage(AttributeTargets.Class)]
    public class PreLoaderPatcherAttribute : Attribute
    {
        public string Target;
        public PreLoaderPatcherAttribute(string target) { Target = target; }
    }
}