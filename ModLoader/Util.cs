using System;
using System.IO;
using System.Reflection;

namespace ModLoader
{
    public static class Util
    {
        public static Assembly PreLoadAssembly(string file)
        {
            var fileName = Path.GetFileName(file);
            ModLoader.Logger.Info($"Trying to pre-Load assembly {fileName}.");
            if (File.Exists(file))
            {
                ModLoader.Logger.Info($"found {fileName}, pre-loading...");
                try
                {
                    var assembly = Assembly.ReflectionOnlyLoadFrom(file);
                    ModLoader.Logger.Info($"Successfully pre-loaded assembly {fileName}.");
                    return assembly;
                }
                catch (Exception ex)
                {
                    ModLoader.Logger.Debug($"Pre-Loading assembly {fileName} failed!");
                    ModLoader.Logger.Debug(ex.Message);
                    ModLoader.Logger.Debug(ex.StackTrace);
                }
            }
            else ModLoader.Logger.Info($"Cannot find {fileName}, ignoring this mod.");
            return null;
        }
        public static Assembly LoadAssembly(Assembly asm)
        {
            if (asm != null)
            {
                try
                {
                    ModLoader.Logger.Info($"Trying to load assembly {asm.FullName}");
                    var loaded = Assembly.LoadFrom(asm.Location);
                    ModLoader.Logger.Info($"Successfully loaded assembly {asm.FullName}.");
                    return loaded;
                }
                catch (Exception ex)
                {
                    ModLoader.Logger.Debug($"loading assembly {asm.FullName} failed!");
                    ModLoader.Logger.Debug(ex.Message);
                    ModLoader.Logger.Debug(ex.StackTrace);
                }
            }
            return null;
        }
        public static void Call(Assembly asm, string type, string method = "Main")
        {
            if (asm != null)
            {
                try
                {
                    var name = asm.GetName().Name;
                    ModLoader.Logger.Info($"calling the method {method} of {type} in {asm.FullName}...");
                    asm.GetType(type)?.GetMethod(method)?.Invoke(null, null);
                    ModLoader.Logger.Info($"Successfully called method {method} of {type} in {asm.FullName}.");
                }
                catch (ArgumentException ae)
                {
                    ModLoader.Logger.Debug(ae.Message);
                }
                catch (TargetInvocationException tie)
                {
                    ModLoader.Logger.Debug($"Failed to invoke {method} of {type} in {asm.FullName}!");
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