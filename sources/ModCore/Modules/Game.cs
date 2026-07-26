using dc;
using dc.en;
using dc.h3d.pass;
using dc.hl;
using dc.hl.types;
using dc.hxd.fs;
using dc.hxd.res;
using dc.hxsl;
using dc.libs;
using dc.pr;
using dc.tool;
using dc.ui;
using Hashlink;
using Hashlink.Marshaling;
using Hashlink.Proxy.Objects;
using Hashlink.Reflection.Types;
using Hashlink.Virtuals;
using HaxeProxy.Events;
using HaxeProxy.Runtime;
using HaxeProxy.Runtime.Internals.Inheritance;
using ModCore.Events;
using ModCore.Events.Interfaces;
using ModCore.Events.Interfaces.Game;
using ModCore.Events.Interfaces.Game.Hero;
using ModCore.Events.Interfaces.Game.Save;
using ModCore.Modules.Platforms;
using ModCore.Native;
using ModCore.Utilities;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ModCore.Modules
{
    /// <summary>
    /// 
    /// </summary>
    [CoreModule(CoreModuleAttribute.CoreModuleKind.Normal)]
    public class Game : CoreModule<Game>,
        IOnBeforeGameInit,
        IOnFrameUpdate,
        IOnAdvancedModuleInitializing
    {
        private static readonly ConcurrentQueue<(SendOrPostCallback, object?)> queues = [];
        class SyncContext : SynchronizationContext
        {
            public override void Post( SendOrPostCallback d, object? state )
            {
                queues.Enqueue((d, state));
            }
            public override void Send( SendOrPostCallback d, object? state )
            {
                if (Core.InMainThread)
                {
                    d(state);
                    return;
                }
                EventWaitHandle wait = new(false, EventResetMode.ManualReset);
                Exception? exception = null;
                queues.Enqueue((state =>
                {
                    try
                    {
                        d(state);
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                    }
                    wait.Set();
                }, state));
                wait.WaitOne();
                wait.Dispose();

                if (exception != null)
                {
                    throw new AggregateException("An exception occurred while executing a callback in the synchronization context.", exception);
                }
            }
        }
        /// <summary>
        /// Gets the synchronization context used to marshal execution to the appropriate thread or context.
        /// </summary>
        public static SynchronizationContext SynchronizationContext { get; } = new SyncContext();
        /// <inheritdoc/>
        public override int Priority => ModulePriorities.Game;

        /// <summary>
        /// Get an instance of Hero
        /// </summary>
        public Hero? HeroInstance {
            get; private set;
        }


        void IOnBeforeGameInit.OnBeforeGameInit()
        {
            Hook_Hero.init += Hook_Hero_init;
            Hook_Hero.dispose += Hook_Hero_dispose;
            Hook__ServerApi.canSaveScore += Hook__ServerApi_canSaveScore;

            if (!Debugger.IsAttached)
            {
                Hook__ErrorHandler.init += Hook__ErrorHandler_init;
            }

            if (Core.Config.Value.SkipLogoSplash)
            {
                Hook__LogoSplashscreen.__constructor__ += Hook_LogoSplashscreen_onConstructor;
                Hook_LogoSplashscreen.onResize += Hook_LogoSplashscreen_onResize;
                Hook_Main.onSecondFrame += Hook_Main_onSecondFrame;
            }
        }
        private void Hook_LogoSplashscreen_onConstructor( Hook__LogoSplashscreen.orig___constructor__ orig,
             LogoSplashscreen arg1, Ref<double> delay )
        {
            orig(arg1, delay);
            arg1.logoMT.alpha = 0;
            arg1.logoEvilEmpire.alpha = 0;
        }

        private unsafe void Hook__ErrorHandler_init( Hook__ErrorHandler.orig_init orig )
        {
            dc.hxd.System.Class.reportError = err =>
            {
                nint ptr = 0;
                HashlinkMarshal.WriteData(&ptr, err, HashlinkMarshal.Module.KnownTypes.Dynamic);
                throw new HashlinkError(ptr);
            };
        }

        private void Hook_LogoSplashscreen_onResize( Hook_LogoSplashscreen.orig_onResize orig, LogoSplashscreen self )
        {
            self.logoMT.alpha = 0;
            self.logoEvilEmpire.alpha = 0;
        }

        private void Hook_Main_onSecondFrame( Hook_Main.orig_onSecondFrame orig, Main self )
        {
            orig(self);
            Assets.Class.preInit();
            var lss = (LogoSplashscreen)self.curProcess;
            lss.nextProcess();

        }

        private bool Hook__ServerApi_canSaveScore( Hook__ServerApi.orig_canSaveScore orig )
        {
            return false;
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
            System.Threading.SynchronizationContext.SetSynchronizationContext(SynchronizationContext);

            Hook__Type.createEmptyInstance += Hook__Type_createEmptyInstance;


            Hook_TitleScreen.setMiscTexts += Hook_TitleScreen_setMiscTexts;
            Hook__Boot.main += Hook__Boot_main;

            Hook_Boot.init += Hook_Boot_init1;
            Hook_Boot.endInit += Hook_Boot_endInit1;
            Hook_Boot.update += Hook_Boot_update1;
            Hook_Boot.mainLoop += Hook_Boot_mainLoop;
            Hook_Boot.forceRender += Hook_Boot_forceRender;
            Hook_Boot.render += Hook_Boot_render;

            Hook__Achievements.setAchievement += Hook__Achievements_setAchievement;
            Hook__Achievements.hasAchievement += Hook__Achievements_hasAchievement;

            Hook_HUD.postUpdate += Hook_HUD_postUpdate;

            Hook__Save.delete += Hook__Save_delete;
            Hook__Save.copy += Hook__Save_copy;
            Hook__Save.tryLoad += Hook__Save_tryLoad;
            Hook__Save.save += Hook__Save_save;

            Hook_GlslOut.run += Hook_GlslOut_run;

            Hook__Data.loadFrom += Hook__Data_loadFrom;

            Hook__Sys.getPath += Hook__Sys_getPath;

            try
            {
                HashlinkHooks.Instance.CreateHook("$Data", "loadJson", Hook__Data_loadJson, true);
            }
            catch (Exception)
            {
            }
        }

        private void Hook_HUD_postUpdate( Hook_HUD.orig_postUpdate orig, HUD self )
        {
            orig(self);

            var bmp = self.bmpMod;
            if (bmp != null)
            {
                var gm = dc.pr.Game.Class.ME;
                if (gm.user != null)
                {
                    bmp.set_visible(true);
                }
            }
        }

        private bool Hook__Achievements_hasAchievement( Hook__Achievements.orig_hasAchievement orig, dc.achievements.EAchievement id )
        {
            return false;
        }

        private void Hook__Achievements_setAchievement( Hook__Achievements.orig_setAchievement orig,
            dc.achievements.EAchievement id, Ref<bool> showLog )
        {

        }

        private nint Hook__Sys_getPath( Hook__Sys.orig_getPath orig, dc.String s )
        {
            var p = s.ToString();
            if (string.IsNullOrWhiteSpace(p))
            {
                return orig(s);
            }
            var str = System.IO.Path.GetFullPath(s.ToString());
            return orig(str.AsHaxeString());
        }

        private dc.String Hook_GlslOut_run( Hook_GlslOut.orig_run orig, GlslOut self, Hashlink.Virtuals.virtual_funs_name_vars_ s )
        {
            self.glES = null;
            return orig(self, s);
        }

        private unsafe object Hook__Type_createEmptyInstance( Hook__Type.orig_createEmptyInstance orig, dc.hl.Class c )
        {
            var ct = HashlinkMarshal.GetHashlinkType((HL_type*)c.__type__);

            var result = EventSystem.BroadcastEvent<IOnHashlinkCreateEmptyInstance, HashlinkType, object>(ct);

            return result.HasValue ? result.Value : orig(c);
        }

        private void Hook_Boot_render( Hook_Boot.orig_render orig, Boot self, dc.h3d.Engine e )
        {
            orig(self, e);
            FlushSyncTasks();
        }

        private void Hook_Boot_forceRender( Hook_Boot.orig_forceRender orig, Boot self )
        {
            orig(self);
            FlushSyncTasks();
        }

        private unsafe void Hook__Data_loadJson( Action<dc.String, nint> orig, dc.String json, nint allowReload )
        {
            orig(json, allowReload);
            EventSystem.BroadcastEvent<IOnAfterLoadingCDB, _Data_>(Data.Class);
        }

        private unsafe void Hook__Data_loadFrom( Hook__Data.orig_loadFrom orig, dc.String path, Ref<bool> allowReload )
        {
            orig(path, allowReload);
            EventSystem.BroadcastEvent<IOnAfterLoadingCDB, _Data_>(Data.Class);
        }

        private void FlushSyncTasks()
        {
            while (queues.TryDequeue(out var req))
            {
                try
                {
                    req.Item1(req.Item2);
                }
                catch (Exception ex)
                {
                    Logger.Fatal(ex, "An exception occurred during task execution.");
                    Debugger.BreakForUserUnhandledException(ex);
                    throw;
                }
            }
        }

        private void Hook_Boot_mainLoop( Hook_Boot.orig_mainLoop orig, Boot self )
        {
            try
            {
                SynchronizationContext.SetSynchronizationContext(SynchronizationContext);
                FlushSyncTasks();
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
            string dccmVer = $"DCCM(v{typeof(Core).Assembly.GetName().Version})";
            if(Native.Native.Current.RunOnHLC)
            {
                dccmVer += "(HLC)";
            }
            self.build.set_text($"{dccmVer} - {self.build.text}".AsHaxeString());

            if (!Core.Config.Value.DCCMWarningPopup)
            {
                var popup = new ModalPopUp(Ref<bool>.In(true), null);
                popup.text(GetText.Instance.GetString("Dead Cells Core Modding enabled, achievements and rankings disabled.").AsHaxeString(), null, default);
                popup.text(GetText.Instance.GetString("This window will not reappear after closing.").AsHaxeString(), null, default);
                popup.onClose = () =>
                {
                    Core.Config.Value.DCCMWarningPopup = true;
                    Core.Config.Save();
                };
            }

            Logger.Information(self.build.text.ToString());
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
            if (GameInfo.GameVersion != GameInfo.DCCMVersion.Major)
            {
                Logger.Warning("The target game version for DCCM is {A}, not the {B}", GameInfo.DCCMVersion.Major,
                    GameInfo.GameVersion);
            }

            Logger.Information("Game version: v{ver} ({date}) - {hash}", 
                GameInfo.GameVersion, 
                GameInfo.BuildDate,
                GitVersion.Class.HASH.ToString());

            if (GameInfo.Platform == GameInfo.PlatformKind.Steam)
            {
                try
                {
                    _ = new SteamPlatformModule();
                }
                catch (Exception ex)
                {
                    Logger.Warning(ex, "Failed to initialize SteamPlatformModule");
                }
            }

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
            orig(self);

            self.engine.window.window.set_title("Dead Cells with Core Modding".AsHaxeString());

            EventSystem.BroadcastEvent<IOnGameInit>();

        }
    }
}
