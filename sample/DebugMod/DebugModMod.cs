using Hashlink.Marshaling;
using Hashlink.Proxy.Clousre;
using Hashlink.Proxy.DynamicAccess;
using Hashlink.Proxy.Objects;
using Hashlink.Reflection.Types;
using HaxeProxy.Runtime;
using ModCore.Events.Interfaces.Game;
using ModCore.Mods;
using ModCore.Modules;
using ModCore.Utitities;

namespace DebugMod
{
    public class DebugModMod(ModInfo info) : ModBase(info),
        IOnBeforeGameInit
    {
        private void Hook_Console_ctor(HashlinkClosure orig, HashlinkObject self)
        {
            orig.DynamicInvoke(self);
            var s = self.AsHaxe<dc.ui.Console>();
            var ss = dc.ui.Console.Class;
            ss.HIDE_UI = "FDMM_HIDE_UI".AsHaxeString();
            ss.HIDE_DEBUG = "FDMM_HIDE_DEBUG".AsHaxeString();
            ss.HIDE_CONSOLE = "FDMM_HIDE_CONSOLE".AsHaxeString();
            s.activateDebug();
        }
        void IOnBeforeGameInit.OnBeforeGameInit()
        {
            var hh = HashlinkHooks.Instance;

            dc.h2d.Hook_Console.handleCommand += Hook_Console_handleCommand1;
            dc.ui.Hook_Console.log += Hook_Console_log1;

            hh.CreateHook("ui.$Console", "__constructor__", Hook_Console_ctor).Enable();
        }

        private void Hook_Console_log1(dc.ui.Hook_Console.orig_log orig, dc.ui.Console self, 
            dc.String text, int? color)
        {
            Logger.Information(text.ToString() ?? "");
            var ct = (HashlinkObjectType)self.HashlinkObj.Type;
            ct.Super!.FindProto("log")!.Function.DynamicInvoke(self, text, color);
        }

        private void Hook_Console_handleCommand1(dc.h2d.Hook_Console.orig_handleCommand orig, 
            dc.h2d.Console self, dc.String command)
        {
            Logger.Information("Handle Command: {cmd}", command);
            orig(self, command);
        }
    }
}
