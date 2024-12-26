using Hashlink;
using ModCore.Hashlink;
using ModCore.Modules.Events;
using ModCore.Track;
using SDL2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static SDL2.SDL;

namespace ModCore.Modules
{
    [CoreModule]
    public unsafe class Game : CoreModule<Game>, IOnModCoreInjected, IOnBeforeGameStartup
    {
        private HashlinkHook hhook = null!;
        public override int Priority => ModulePriorities.Game;

        public nint MainWindowPtr { get; private set; }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void UpdateD(nint self);
        private object? Hook_Boot_update(HashlinkFunc orig, HashlinkObject self)
        {
            var rself = (HashlinkObject) HashlinkUtils.GetGlobal("$Boot").Dynamic.ME;

            var log = HashlinkUtils.GetGlobal("haxe.$Log");

            var htrace = log.Dynamic.trace;
            var ho = (HashlinkObject)htrace;
            var vt = ho.HashlinkType->data.func->args[1];
            var arg2 = new HashlinkObject(vt);
            arg2.Dynamic.className = "&%Test1";
            arg2.Dynamic.fileName = "Test2";
            arg2.Dynamic.methodName = "Test3";
            arg2.Dynamic.lineNumber = 114514;
            
            htrace(
                    "Hello, World!!!!!!!" + HashlinkUtils.GetTypeString(vt),
                arg2);

            //o((nint)self.HashlinkValue.ptr);
            Logger.Information("AA {a} {b} {c}", (string)arg2.Dynamic.className, (string)arg2.Dynamic.fileName, arg2.Dynamic.lineNumber);
            return orig.Call(self);
        }

        void IOnBeforeGameStartup.OnBeforeGameStartup()
        {
            hhook.CreateHook(HashlinkUtils.FindFunction("Boot", "endInit"), Hook_Boot_update);
        }

        void IOnModCoreInjected.OnModCoreInjected()
        {
            hhook = HashlinkHook.Instance;

        }
    }
}
