using System;
using System.Linq;
using ModLoader;
using Mono.Cecil;
using Mono.Cecil.Cil;


namespace MLTest
{
    [PreLoaderPatch("Assembly-CSharp")]
    public static class MLTest
    {
        public static void Patch(AssemblyDefinition asm)
        {
            var luamgr = asm.MainModule.Types.First(t => t.FullName == "XiaWorld.LuaMgr");
            var init = luamgr.Methods.First(m => m.Name == "Init");
            var processor = init.Body.GetILProcessor();
            var last = processor.Body.Instructions.Last();
            var writeline = asm.MainModule.ImportReference(typeof(Console).GetMethod("WriteLine", new Type[] {typeof(string)}));
            processor.InsertBefore(last, processor.Create(OpCodes.Ldstr, "MLTest in Action"));
            processor.InsertBefore(last, processor.Create(OpCodes.Call, writeline));
        }
    }
}
