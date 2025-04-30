using dc;
using dc.en;
using dc.pr;
using Hashlink.Marshaling;
using Hashlink.Proxy.Clousre;
using Hashlink.Proxy.DynamicAccess;
using Hashlink.Proxy.Objects;
using HaxeProxy.Runtime;
using ModCore.Events;
using ModCore.Events.Interfaces.Game;
using ModCore.Events.Interfaces.Game.Hero;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Modules.AdvancedModules
{
    [AdvancedModule]
    public class Game : AdvancedModule<Game>,
        IOnBeforeGameInit,
        IOnFrameUpdate
    {
        private bool Hook_ServerApi_canSaveScore( HashlinkClosure orig, HashlinkObject self )
        {
            return false;
        }
        public Hero? HeroInstance
        {
            get; private set;
        }
        private object? Hook_hero_init( HashlinkClosure orig, HashlinkObject self )
        {
            HeroInstance = self.AsHaxe<Hero>();
            EventSystem.BroadcastEvent<IOnHeroInit>();
            return orig.DynamicInvoke(self);
        }
        private object? Hook_hero_dispose( HashlinkClosure orig, HashlinkObject self )
        {
            EventSystem.BroadcastEvent<IOnHeroUpdate>();
            HeroInstance = null;
            return orig.DynamicInvoke(self);
        }
        private void Hook_LogoSplash_update( HashlinkClosure orig, HashlinkObject self )
        {
            var s = self.AsHaxe<LogoSplashscreen>();

            Assets.Class.preInit();
            s.secondLogo = true;
            s.ready = true;
            self.AsDynamic().next(null);
        }

        private object? Hook_TitleMenu_addMenu( HashlinkClosure orig, HashlinkObject self,
            HashlinkObject str, HashlinkClosure cb, HashlinkObject help, object? isEnabled,
            object? color )
        {
            var s = self.AsHaxe<TitleScreen>();
            var menuItems = s.menuItems;
            if (menuItems.length == 3 && s.isMainMenu)
            {
                orig.DynamicInvoke(
                    self, GetText.Instance.GetString("About Core Modding"), () =>
                    {
                        Logger.Information("Open https://github.com/dead-cells-core-modding/core");
                        Process.Start(new ProcessStartInfo()
                        {
                            UseShellExecute = true,
                            FileName = "https://github.com/dead-cells-core-modding/core"
                        });
                    }, null, null, null
                    );
            }
            return orig.DynamicInvoke(self, str, cb, help, isEnabled, color);
        }
        void IOnBeforeGameInit.OnBeforeGameInit()
        {
            HashlinkHooks.Instance.CreateHook("en.Hero", "init", Hook_hero_init).Enable();
            HashlinkHooks.Instance.CreateHook("en.Hero", "dispose", Hook_hero_dispose).Enable();

            HashlinkHooks.Instance.CreateHook("pr.TitleScreen", "addMenu", Hook_TitleMenu_addMenu).Enable();

            HashlinkHooks.Instance.CreateHook("tool.$ServerApi", "canSaveScore", Hook_ServerApi_canSaveScore).Enable();

            if (Core.Config.Value.SkipLogoSplash)
            {
                HashlinkHooks.Instance.CreateHook("pr.LogoSplashscreen", "update", Hook_LogoSplash_update).Enable();
            }
        }

        void IOnFrameUpdate.OnFrameUpdate( double dt )
        {
            if (HeroInstance != null)
            {
                EventSystem.BroadcastEvent<IOnHeroUpdate, double>(dt);
            }
        }
    }
}
