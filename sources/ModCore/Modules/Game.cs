using dc;
using dc.en;
using dc.haxe;
using dc.hxd;
using dc.pr;
using dc.spine;
using dc.tool;
using dc.ui.icon;
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
using ModCore.Events.Interfaces.Game.Save;
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
    [CoreModule(CoreModuleAttribute.CoreModuleKind.Normal)]
    public class Game : CoreModule<Game>,
        IOnBeforeGameInit,
        IOnFrameUpdate,
        IOnAdvancedModuleInitializing
    {
        public override int Priority => ModulePriorities.Game;
        public Hero? HeroInstance
        {
            get; private set;
        }

        void IOnBeforeGameInit.OnBeforeGameInit()
        {
            Hook_Hero.init += Hook_Hero_init;
            Hook_Hero.dispose += Hook_Hero_dispose;
            Hook_TitleScreen.addMenu += Hook_TitleScreen_addMenu;

            Hook__ServerApi.canSaveScore += Hook__ServerApi_canSaveScore;

            if (Core.Config.Value.SkipLogoSplash)
            {
                Hook_LogoSplashscreen.update += Hook_LogoSplashscreen_update;
            }
        }

        private bool Hook__ServerApi_canSaveScore( Hook__ServerApi.orig_canSaveScore orig )
        {
            return false;
        }

        private void Hook_LogoSplashscreen_update( Hook_LogoSplashscreen.orig_update orig, LogoSplashscreen self )
        {
            var s = self;

            Assets.Class.preInit();
            s.secondLogo = true;
            s.ready = true;
            self.next(null);
        }

        private virtual_cb_help_inter_isEnable_t_<bool> Hook_TitleScreen_addMenu( Hook_TitleScreen.orig_addMenu orig, TitleScreen self, dc.String str, HlAction cb, dc.String help, bool? isEnable, Ref<int> color )
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
            EventSystem.BroadcastEvent<IOnHeroDispose>();
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
            Hook_TitleScreen.setMiscTexts += Hook_TitleScreen_setMiscTexts;
            Hook__Boot.main += Hook__Boot_main;
            Hook_Boot.init += Hook_Boot_init1;
            Hook_Boot.endInit += Hook_Boot_endInit1;
            Hook_Boot.update += Hook_Boot_update1;
            Hook_Boot.mainLoop += Hook_Boot_mainLoop;

            Hook__Save.delete += Hook__Save_delete;
            Hook__Save.copy += Hook__Save_copy;
            Hook__Save.tryLoad += Hook__Save_tryLoad;
            Hook__Save.save += Hook__Save_save;
        }

        private void Hook_Boot_mainLoop( Hook_Boot.orig_mainLoop orig, Boot self )
        {
            try
            {
                orig(self);
            }
            catch (Exception ex)
            {
                Debugger.BreakForUserUnhandledException(ex);
                throw;
            }
        }

        private void Hook_TitleScreen_setMiscTexts( Hook_TitleScreen.orig_setMiscTexts orig, 
            TitleScreen self )
        {
            orig(self);
            self.build.set_text($"DCCM(v{typeof(Core).Assembly.GetName().Version}) - {self.build.text}".AsHaxeString());
        }

        private void Hook__Save_save( Hook__Save.orig_save orig, User u, bool onlyGameData )
        {
            EventSystem.BroadcastEvent<IOnSaveConfig>();
            EventSystem.BroadcastEvent<IOnBeforeSavingSave, IOnBeforeSavingSave.EventData>(new(u, onlyGameData));
            orig(u, onlyGameData);
            EventSystem.BroadcastEvent<IOnAfterSavingSave>();
        }

        private User Hook__Save_tryLoad( Hook__Save.orig_tryLoad orig )
        {
            EventSystem.BroadcastEvent<IOnSaveConfig>();
            EventSystem.BroadcastEvent<IOnBeforeLoadingSave>();
            var data = orig();
            EventSystem.BroadcastEvent<IOnAfterLoadingSave, User>(data);
            return data;
        }

        private void Hook__Save_copy( Hook__Save.orig_copy orig, int slotFrom, int slotTo )
        {
            EventSystem.BroadcastEvent<IOnCopySave, IOnCopySave.EventData>(new(slotFrom, slotTo));
            orig(slotFrom, slotTo);
        }

        private void Hook__Save_delete( Hook__Save.orig_delete orig, int? slot )
        {
            EventSystem.BroadcastEvent<IOnDeleteSave, int?>(slot);
            orig(slot);
        }

        private void Hook__Boot_main( Hook__Boot.orig_main orig )
        {
            EventSystem.BroadcastEvent<IOnBeforeGameInit>();
            orig();
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
            dynamic win = self.engine.window;

            orig(self);

            win.window.set_title("Dead Cells with Core Modding".AsHaxeString());

            EventSystem.BroadcastEvent<IOnGameInit>();
            
        }
    }
}
