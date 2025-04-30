
using HashlinkNET.Bytecode;
using HashlinkNET.Compiler;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Runtime.CompilerServices;

using var asm = AssemblyDefinition.CreateAssembly(new("GameProxy", new()), "GameProxy", ModuleKind.Dll);
var compiler = new HashlinkCompiler(
    HlCode.FromBytes(File.ReadAllBytes(args[0])), asm, new()
    {
        AllowParalle = true
    });

compiler.Compile();

var m = asm.MainModule;

asm.CustomAttributes.Clear();
asm.CustomAttributes.Add(new(
    m.ImportReference(
        typeof(ReferenceAssemblyAttribute).GetConstructors().First()
    )
    )
{

});

foreach(var v in m.Types.ToArray())
{
    if(!v.IsPublic)
    {
        m.Types.Remove(v);
    }
    else
    {
        CleanupType(v);
    }
}

static void CleanupType(TypeDefinition v)
{
    v.CustomAttributes.Clear();
    foreach (var me in v.Methods.ToArray())
    {
        me.CustomAttributes.Clear();
        if (!me.IsPublic)
        {
            v.Methods.Remove(me);
        }
        else if(me.Body != null)
        {
            me.Body = new(me);
            var il = me.Body.GetILProcessor();
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Throw);
        }
    }
    foreach (var f in v.Fields.ToArray())
    {
        f.CustomAttributes.Clear();
        if (!f.IsPublic)
        {
            v.Fields.Remove(f);
        }
    }
    foreach (var nt in v.NestedTypes.ToArray())
    {
        nt.CustomAttributes.Clear();
        if (!nt.IsPublic && !nt.IsNestedPublic)
        {
            v.NestedTypes.Remove(nt);
        }
        else
        {
            CleanupType(nt);
        }
    }
}

asm.Write(args[1]);
asm.Dispose();
