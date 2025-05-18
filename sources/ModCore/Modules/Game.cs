using dc;
using dc.en;
using dc.pr;
using dc.tool;
using Hashlink.Marshaling;
using Hashlink.Proxy.Clousre;
using Hashlink.Proxy.DynamicAccess;
using Hashlink.Proxy.Objects;
using Hashlink.Virtuals;
using HaxeProxy.Runtime;
using ModCore.Events;
using ModCore.Events.Interfaces;
using ModCore.Events.Interfaces.Game;
using ModCore.Events.Interfaces.Game.Hero;
using ModCore.Events.Interfaces.VM;
using ModCore.Utitities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Modules
{
    [CoreModule(CoreModuleAttribute.CoreModuleKind.Preload)]
    public class Game : CoreModule<Game>,
        IOnBeforeGameInit,
        IOnFrameUpdate,
        IOnAdvancedModuleInitializing
    {
        public override int Priority => ModulePriorities.Game;
        private bool Hook_ServerApi_canSaveScore( HashlinkClosure orig, HashlinkObject self )
        {
            return false;
        }
        public Hero? HeroInstance
        {
            get; private set;
        }

        private void Hook_Boot_main( HashlinkClosure orig )
        {
            EventSystem.BroadcastEvent<IOnBeforeGameInit>();
            orig.DynamicInvoke();
        }
        void IOnBeforeGameInit.OnBeforeGameInit()
        {
            Hook_Hero.init += Hook_Hero_init;
            Hook_Hero.dispose += Hook_Hero_dispose;
            Hook_TitleScreen.addMenu += Hook_TitleScreen_addMenu;

            HashlinkHooks.Instance.CreateHook("tool.$ServerApi", "canSaveScore", Hook_ServerApi_canSaveScore).Enable();

            if (Core.Config.Value.SkipLogoSplash)
            {
                Hook_LogoSplashscreen.update += Hook_LogoSplashscreen_update;
            }
        }

        private void Hook_LogoSplashscreen_update( Hook_LogoSplashscreen.orig_update orig, LogoSplashscreen self )
        {
            var s = self;

            Assets.Class.preInit();
            s.secondLogo = true;
            s.ready = true;
            self.next(null);
        }

        private virtual_cb_help_inter_isEnable_t__0 Hook_TitleScreen_addMenu( Hook_TitleScreen.orig_addMenu orig, TitleScreen self, dc.String str, HlAction cb, dc.String help, bool? isEnable, Ref<int> color )
        {
            var s = self;
            var menuItems = s.menuItems;
            if (menuItems.length == 3 && s.isMainMenu)
            {
                orig(
                    self, GetText.Instance.GetString("About Core Modding").AsHaxeString(), () =>
                    {
                        Logger.Information("Open https://github.com/dead-cells-core-modding/core");
                        Process.Start(new ProcessStartInfo()
                        {
                            UseShellExecute = true,
                            FileName = "https://github.com/dead-cells-core-modding/core"
                        });
                    }, null, null, default
                    );
            }

            return orig(self, str, cb, help, isEnable, color);
        }

        private void Hook_Hero_dispose( Hook_Hero.orig_dispose orig, Hero self )
        {
            EventSystem.BroadcastEvent<IOnHeroUpdate>();
            HeroInstance = null;
            orig(self);
        }

        private void Hook_Hero_init( Hook_Hero.orig_init orig, Hero self )
        {
            HeroInstance = self;
            EventSystem.BroadcastEvent<IOnHeroInit>();
            orig(self);
        }

        void IOnFrameUpdate.OnFrameUpdate( double dt )
        {
            if (HeroInstance != null)
            {
                EventSystem.BroadcastEvent<IOnHeroUpdate, double>(dt);
            }
        }

        void IOnAdvancedModuleInitializing.OnAdvancedModuleInitializing()
        {
            HashlinkHooks.Instance.CreateHook("$Boot", "main", Hook_Boot_main).Enable();
            Hook_Boot.init += Hook_Boot_init1;
            Hook_Boot.endInit += Hook_Boot_endInit1;
            Hook_Boot.update += Hook_Boot_update1;
        }

        private void Hook_Boot_update1( Hook_Boot.orig_update orig, Boot self, double dt )
        {
            orig(self, dt);
            EventSystem.BroadcastEvent<IOnFrameUpdate, double>(dt);
        }

        private void Hook_Boot_endInit1( Hook_Boot.orig_endInit orig, Boot self )
        {
            orig(self);
            EventSystem.BroadcastEvent<IOnGameEndInit>();
        }

        private void Hook_Boot_init1( Hook_Boot.orig_init orig, Boot self )
        {
            var win = self.engine.window.window;

            orig(self);

            win.set_title("Dead Cells with Core Modding".AsHaxeString());

            EventSystem.BroadcastEvent<IOnGameInit>();
            
        }
    }
}
