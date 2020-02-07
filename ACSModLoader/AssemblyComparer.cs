using System.Collections.Generic;
using System.Reflection;
using System;

namespace ModLoader
{
    public class AssemblyComparer : IComparer<Assembly>
    {
        public int Compare(Assembly a, Assembly b)
        {
            var a_deps = Attribute.GetCustomAttributes(a, typeof(ModRequire));
            var b_deps = Attribute.GetCustomAttributes(b, typeof(ModRequire));
            if(a_deps.Length > 0)
            {
                for(int i = 0; i < a_deps.Length; i++)
                {
                    var dep = a_deps[i] as ModRequire;
                    bool version_match = string.IsNullOrEmpty(dep.Version) ? true : dep.Version == b.GetName().Version.ToString();
                    // if a depends on b, then a is "greater" than b.
                    if(dep.Dependency == b.GetName().Name && version_match) return 1;
                }
            }
            if(b_deps.Length > 0)
            {
                for (int i = 0; i < b_deps.Length; i++)
                {
                    var dep = b_deps[i] as ModRequire;
                    bool version_match = string.IsNullOrEmpty(dep.Version) ? true : dep.Version == a.GetName().Version.ToString();
                    // if b depends on a, then a is "less" than b.
                    if (dep.Dependency == a.GetName().Name && version_match) return -1;
                }
            }
            return 0;
        }
    }
}