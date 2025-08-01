
using HashlinkNET.Bytecode;
using HashlinkNET.Compiler;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Diagnostics;
using System.Reflection;
using System.Runtime;
using System.Runtime.CompilerServices;

static void Run(string[] args){
    using var asm = AssemblyDefinition.CreateAssembly(new("GameProxy", new()), "GameProxy", ModuleKind.Dll);
    var config = new CompileConfig()
    {
        AllowParalle = true,
        GeneratePseudocode = false
    };

#if DEBUG
    config.AllowParalle = true;
    config.GeneratePseudocode = true;
    config.GenerateBytecodeMapping = true;
#endif

    var compiler = new HashlinkCompiler(
        HlCode.FromBytes(File.ReadAllBytes(args[0])), asm, config);

    Stopwatch stopwatch = new();

    compiler.OnAfterRunStep += Compiler_OnAfterRunStep;
    compiler.OnBeforeRunStep += Compiler_OnBeforeRunStep;

    void Compiler_OnBeforeRunStep(IDataContainer arg1, HashlinkNET.Compiler.Steps.CompileStep arg2)
    {
        Console.WriteLine($"Running Compile Step: {arg2.GetType().Name}");
        stopwatch.Restart();
    }

    void Compiler_OnAfterRunStep(IDataContainer arg1, HashlinkNET.Compiler.Steps.CompileStep arg2)
    {
        stopwatch.Stop();
        Console.WriteLine($"{arg2.GetType().Name} completed, time: {stopwatch.Elapsed}");
    }

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

#if !DEBUG

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

#endif

    static void CleanupType(TypeDefinition v)
    {
        //v.CustomAttributes.Clear();
        foreach (var me in v.Methods.ToArray())
        {
            //me.CustomAttributes.Clear();
            if (!me.IsPublic)
            {
                v.Methods.Remove(me);
            }
            else if (me.Body != null)
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

    var mscorlibRef = asm.MainModule.AssemblyReferences.First(x => x.Name == "mscorlib");
    var corelibRef = asm.MainModule.AssemblyReferences.FirstOrDefault(x => x.Name == "System.Private.CoreLib");
    if (corelibRef != null)
    {
        corelibRef.Culture = mscorlibRef.Culture;
        corelibRef.Version = mscorlibRef.Version;
        corelibRef.Attributes = mscorlibRef.Attributes;
        corelibRef.MetadataToken = mscorlibRef.MetadataToken;
        corelibRef.PublicKeyToken = mscorlibRef.PublicKeyToken;
        corelibRef.PublicKey = mscorlibRef.PublicKey;
        corelibRef.Hash = mscorlibRef.Hash;
        corelibRef.HashAlgorithm = mscorlibRef.HashAlgorithm;
        corelibRef.HasPublicKey = mscorlibRef.HasPublicKey;
        corelibRef.IsRetargetable = mscorlibRef.IsRetargetable;
        corelibRef.IsWindowsRuntime = mscorlibRef.IsWindowsRuntime;
        corelibRef.Name = mscorlibRef.Name;
    }

    using var pdbFile = new FileStream(Path.ChangeExtension(args[1], "pdb"),
        FileMode.Create, FileAccess.Write);
    asm.Write(args[1], new()
    {
        SymbolWriterProvider = new PortablePdbWriterProvider(),
        SymbolStream = pdbFile
    });
    File.WriteAllBytes(Path.ChangeExtension(args[1], "bcm.bin"),
        compiler.BytecodeMappingData.Write());
}

Run(args); 

Console.WriteLine("Done.");
