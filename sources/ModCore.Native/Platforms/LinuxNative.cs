extern alias iced;
using Hashlink;
using iced::Iced.Intel;
using static iced::Iced.Intel.AssemblerRegisters;
using System.Runtime.Versioning;
using ModCore.Native;
using System.Runtime.InteropServices;
using ModCore.Storage;

[SupportedOSPlatform("linux")]
internal unsafe partial class LinuxNative : Native
{
    public override bool TryLoadLibrary( string path, out nint handle )
    {
        if(NativeLibrary.TryLoad(path, out handle))
        {
            return true;
        }else if(NativeLibrary.TryLoad(path + ".so", out handle))        {
            return true;
        }
        else if(NativeLibrary.TryLoad(FolderInfo.CurrentNativeRoot.GetFilePath(path), out handle))
        {
            return true;
        }
        else if(NativeLibrary.TryLoad(FolderInfo.CurrentNativeRoot.GetFilePath(path + ".so"), out handle))
        {
            return true;
        }
        return false;
    }

    protected override void InitializeAsm()
    {
        //base.InitializeAsm();
    }

    public override int AllocTls()
    {
        throw new NotImplementedException();
    }

    public override unsafe void FixThreadCurrentStackFrame( HL_thread_info* t )
    {
        throw new NotImplementedException();
    }

    public override nint GetCurrentThreadStackBase()
    {
        throw new NotImplementedException();
    }

    public override ReadOnlySpan<byte> GetHlbootDataFromExe( string exePath )
    {
        throw new NotImplementedException();
    }

    public override nint GetTlsValue( int index )
    {
        throw new NotImplementedException();
    }

    public override void MakePageWritable( nint ptr, out int old )
    {
        throw new NotImplementedException();
    }

    public override void RestorePageProtect( nint ptr, int val )
    {
        throw new NotImplementedException();
    }

    public override void SetTlsValue( int index, nint val )
    {
        throw new NotImplementedException();
    }

    protected override void AsmGetTlsDataPtrRax<T>( Assembler c, ref T offset )
    {
        throw new NotImplementedException();
    }

    protected override void Generate_asm_cs_hl_store_context( Assembler c )
    {
        throw new NotImplementedException();
    }

    protected override void Generate_asm_hl2cs_store_return_ptr( Assembler c )
    {
        throw new NotImplementedException();
    }

    protected override void Generate_asm_hl2cs_throw_exception( Assembler c )
    {
        throw new NotImplementedException();
    }

    protected override void Generate_asm_hook_break_on_trap_Entry( Assembler c )
    {
        throw new NotImplementedException();
    }

    public override bool IsBadPtr( nint ptr )
    {
        throw new NotImplementedException();
    }
}
