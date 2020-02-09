using System.Collections.Generic;
using System.Reflection;

namespace ModLoader
{
    public class AssemblyComparer : IComparer<Assembly>
    {
        public int Compare(Assembly a, Assembly b)
        {
            var a_deps = a.GetReferencedAssemblies();
            var b_deps = b.GetReferencedAssemblies();
            if(a_deps.Length > 0)
            {
                for(int i = 0; i < a_deps.Length; i++)
                {
                    var dep = a_deps[i];
                    // if a depends on b, then a is "greater" than b.
                    if(b.GetName().Name == dep.Name) return 1;
                }
            }
            if(b_deps.Length > 0)
            {
                for (int i = 0; i < b_deps.Length; i++)
                {
                    var dep = b_deps[i];
                    // if b depends on a, then a is "less" than b.
                    if (a.GetName().Name == dep.Name) return -1;
                }
            }
            return 0;
        }
    }
}