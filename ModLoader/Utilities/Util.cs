using System;
using System.IO;
using System.Reflection;

namespace ModLoader.Utilities
{
    static class Util
    {
        public static Assembly PreLoadAssembly(string file)
        {
            var fileName = Path.GetFileName(file);
            try
            {
                ModLoader.Logger.Debug($"Pre-Loading: {fileName}");
                var assembly = Assembly.ReflectionOnlyLoadFrom(file);
                return assembly;
            }
            catch (Exception ex)
            {
                ModLoader.Logger.Debug($"Pre-Loading assembly {fileName} failed!");
                ModLoader.Logger.Debug(ex.Message);
                ModLoader.Logger.Debug(ex.StackTrace);
            }
            return null;
        }
        public static Assembly LoadAssembly(Assembly asm)
        {
            if (asm != null)
            {
                try
                {
                    ModLoader.Logger.Debug($"Loading: {asm.FullName}");
                    var loaded = Assembly.LoadFrom(asm.Location);
                    return loaded;
                }
                catch (Exception ex)
                {
                    ModLoader.Logger.Debug($"loading assembly {asm.GetName()} failed!");
                    ModLoader.Logger.Debug(ex.Message);
                    ModLoader.Logger.Debug(ex.StackTrace);
                }
            }
            return null;
        }
        public static void Call(Assembly asm, string method)
        {
            if (asm != null)
            {
                try
                {
                    var name = asm.GetName().Name;
                    ModLoader.Logger.Debug($"calling the {method} method for {name}...");
                    asm.GetType($"{name}.{name}")?.GetMethod(method)?.Invoke(null, null);
                }
                catch (ArgumentException ae)
                {
                    ModLoader.Logger.Debug(ae.Message);
                }
                catch (TargetInvocationException tie)
                {
                    ModLoader.Logger.Debug($"invocation of {method} in {asm.FullName} failed!");
                    var ie = tie.InnerException;
                    ModLoader.Logger.Debug(ie.Message);
                    ModLoader.Logger.Debug(ie.StackTrace);
                }
                catch (Exception e)
                {
                    ModLoader.Logger.Debug(e.Message);
                    ModLoader.Logger.Debug(e.StackTrace);
                }
            }
        }
    }
}