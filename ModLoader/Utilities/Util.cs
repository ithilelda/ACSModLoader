using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Serilog;
using HarmonyLib;

namespace ModLoader.Utilities
{
    static class Util
    {
        public static Assembly PreLoadAssembly(string file)
        {
            var fileName = Path.GetFileName(file);
            // we exclude errogenous libraries that may be a problem.
            if (!(fileName.ToLower() == "0harmony") && !(fileName.ToLower().Contains("ModLoader")))
            {
                try
                {
                    Log.Debug($"[ModLoader] Pre-Loading: {fileName}");
                    var assembly = Assembly.ReflectionOnlyLoadFrom(file);
                    return assembly;
                }
                catch (Exception ex)
                {
                    Log.Debug($"[ModLoader] Pre-Loading assembly {fileName} failed!");
                    Log.Debug(ex.Message);
                    Log.Debug(ex.StackTrace);
                }
            }
            return null;
        }
        public static Assembly LoadAssembly(Assembly asm)
        {
            if (asm != null)
            {
                try
                {
                    Log.Debug($"[ModLoader] Loading: {asm.FullName}");
                    var loaded = Assembly.LoadFrom(asm.Location);
                    return loaded;
                }
                catch (Exception ex)
                {
                    Log.Debug($"[ModLoader] loading assembly {asm.GetName()} failed!");
                    Log.Debug(ex.Message);
                    Log.Debug(ex.StackTrace);
                }
            }
            return null;
        }
        public static bool ApplyHarmony(Assembly asm, string name)
        {
            if(asm != null)
            {
                var harmony_name = string.IsNullOrEmpty(name) ? asm.FullName : name;
                try
                {
                    Log.Debug($"[ModLoader] Applying harmony patch: {harmony_name}");
                    var harmonyInstance = new Harmony(harmony_name);
                    harmonyInstance?.PatchAll(asm);
                    Log.Debug($"[ModLoader] Applying patch {harmony_name} succeeded!");
                    return true;
                }
                catch (Exception ex)
                {
                    Log.Debug($"[ModLoader] Patching harmony mod {harmony_name} failed!");
                    Log.Debug(ex.Message);
                    Log.Debug(ex.StackTrace);
                }
            }
            return false;
        }
        public static void Call(Assembly asm, string method)
        {
            if (asm != null)
            {
                try
                {
                    var name = asm.GetName().Name;
                    Log.Debug($"[ModLoader] calling the {method} method for {name}...");
                    asm.GetType($"{name}.{name}")?.GetMethod(method)?.Invoke(null, null);
                }
                catch (ArgumentException ae)
                {
                    Log.Debug(ae.Message);
                }
                catch (TargetInvocationException tie)
                {
                    Log.Debug($"[ModLoader] invocation of {method} in {asm.FullName} failed!");
                    var ie = tie.InnerException;
                    Log.Debug(ie.Message);
                    Log.Debug(ie.StackTrace);
                }
                catch (Exception e)
                {
                    Log.Debug(e.Message);
                    Log.Debug(e.StackTrace);
                }
            }
        }
    }
}