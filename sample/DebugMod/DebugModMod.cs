using Hashlink.Marshaling;
using Hashlink.Proxy.Clousre;
using Hashlink.Proxy.Objects;
using Hashlink.Reflection.Types;
using Haxe.Marshaling;
using ModCore.Events.Interfaces.Game;
using ModCore.Mods;
using ModCore.Modules;

namespace DebugMod
{
    public class DebugModMod(ModInfo info) : ModBase(info),
        IOnBeforeGameInit
    {

        private void Hook_Console_ctor(HashlinkClosure orig, HashlinkObject self)
        {
            orig.DynamicInvoke(self);
            var s = self.AsHaxe().Dynamic;
            s.activateDebug();
        }
        private void Hook_Console_handleCommand(HashlinkClosure orig, HashlinkObject self,
            HashlinkString command)
        {
            Logger.Information("Handle Command: {cmd}", command.Value);
            orig.DynamicInvoke(self, command);
        }
        private void Hook_Console_log(HashlinkClosure orig, HashlinkObject self,
            HashlinkString text, object color)
        {
            Logger.Information(text.ToString() ?? "");
            var ct = (HashlinkObjectType)self.Type;
            ct.Super!.FindProto("log")!.Function.DynamicInvoke(self, text, color);
        }
        void IOnBeforeGameInit.OnBeforeGameInit()
        {
            var hh = HashlinkHooks.Instance;

            hh.CreateHook("ui.Console", "log", Hook_Console_log);
            hh.CreateHook("ui.Console", "handleCommand", Hook_Console_handleCommand).Enable();
            hh.CreateHook("ui.$Console", "__constructor__", Hook_Console_ctor).Enable();
        }
    }
}
