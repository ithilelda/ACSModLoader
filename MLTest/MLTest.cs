using ModLoader;
using Mono.Cecil;


namespace MLTest
{
    [PreLoaderPatch("Assembly-CSharp")]
    public static class MLTest
    {
        public static void Patch(AssemblyDefinition asm)
        {
            
        }
    }
}
