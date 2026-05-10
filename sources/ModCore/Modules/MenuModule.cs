using dc;
using dc.h2d;
using dc.hl.types;
using dc.ui;
using dc.ui.pause;
using Hashlink.Proxy.Clousre;
using Hashlink.Proxy.Values;
using Hashlink.Reflection.Types;
using Hashlink.Virtuals;
using HaxeProxy.Runtime;
using ModCore.Events;
using ModCore.Events.Interfaces.Game;
using ModCore.Events.Interfaces.Game.Menu;
using ModCore.Menu;
using ModCore.Mods;
using ModCore.Utilities;
using Steamworks;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using static dc.hxd.PixelFormat;
using Hook_Options = dc.ui.Hook_Options;
using Options = dc.ui.Options;

namespace ModCore.Modules
{

    /// <summary>
    /// Manages the application's menu system, enabling integration and display of custom mod menus within the main menu
    /// interface.
    /// </summary>
    /// <remarks>MenuModule implements the IOnGameInit interface to initialize menu-related event handlers
    /// during game startup. It supports dynamic discovery and loading of mod menus, providing a structured approach for
    /// navigating between the main menu and custom mod menus. This module is intended for use in modding scenarios
    /// where additional menu sections need to be registered and displayed alongside the core menu options.</remarks>
    [CoreModule(CoreModuleAttribute.CoreModuleKind.Normal)]
    public class MenuModule : CoreModule<MenuModule>,
        IOnGameInit
    {
        private static readonly Dictionary<IModMenu, CustomMenu> customMenus = [];

        private readonly MainMenu _mainMenu = new();

        private static readonly string STEAMWORKSHOP_PATH_STR = "workshop\\content\\588650\\".Replace('\\', Path.DirectorySeparatorChar);

        /// <summary>
        /// Gets the options section associated with the main menu of the DCCM.
        /// </summary>
        public OptionsSection DCCMMainMenu {
            get {
                return GetCustomMenu(_mainMenu);
            }
        }

        private class CustomMenu : OptionsSection.S_Main
        {
            public OptionsSection? PrevSection {
                get; set;
            }
            public required IModMenu MenuMod {
                get; init;
            }

            public void BuildMenu( Options options )
            {
                MenuMod.BuildMenu(options);
            }
        }

        private class MainMenu : IModMenu
        {
            private static void OpenURL( string url )
            {
                Logger.Information("Open {url}", url);

                if (GameInfo.Platform == GameInfo.PlatformKind.Steam)
                {

                    bool shouldOverlay = url.StartsWith("https://steam");

                    if (SteamUtils.IsSteamInBigPictureMode())
                    {
                        shouldOverlay = true;
                    }

                    if (shouldOverlay && SteamUtils.IsOverlayEnabled())
                    {
                        SteamFriends.ActivateGameOverlayToWebPage(url);
                        return;
                    }
                }

                System.Diagnostics.Process.Start(new ProcessStartInfo()
                {
                    UseShellExecute = true,
                    FileName = url
                });
            }
            public void BuildMenu( Options options )
            {
                options.title.set_text(GetString("CORE MODDING"));

                options.createScroller(1);

                var flow = options.scrollerFlow;

                var w = options.addSimpleWidget(GetString("About Core Modding"),
                    $"DCCM Version: {Core.Version}".AsHaxeString(), () =>
                {
                    OpenURL("https://dead-cells-core-modding.github.io/docs/docs/");
                }, Ref<int>.In(5), flow);

                w.set_horizontalAlign(new FlowAlign.Right());

                if (Environment.GetEnvironmentVariable("DCCM_STEAMWORKSHOP_ENABLED") == "true")
                {
                    options.addSimpleWidget(GetString("Get mods"),
                    GetString("Get mods from the Steam Workshop"), () =>
                   {
                       OpenURL("https://steamcommunity.com/workshop/browse/?appid=588650&searchtext=%5BDCCM%5D");
                   }, Ref<int>.In(5), flow);
                }


                {

                    bool hasSep = false;

                    foreach (var mod in EventSystem.FindReceivers<ModBase>())
                    {
                        if (mod is not IModMenu mm)
                        {
                            if (mod is IModMenuProvider mp)
                            {
                                mm = mp.GetModMenu();
                            }
                            else
                            {
                                continue;
                            }
                        }

                        var cm = GetCustomMenu(mm);

                        var name = mm.GetName();
                        if (string.IsNullOrEmpty(name))
                        {
                            continue;
                        }

                        if (!hasSep)
                        {
                            hasSep = true;
                            options.addSeparator("".AsHaxeString(), flow);
                        }

                        options.addSimpleWidget(GetString(name), GetString(mm.GetSubText()), () =>
                        {
                            options.setSection(cm);
                        }, Ref<int>.In(5), flow);
                    }
                }

                options.addSeparator(GetString("Loaded Mods"), flow);

                {
                    foreach (var mod in EventSystem.FindReceivers<ModBase>())
                    {
                        HlAction onClick = static () => { };

                        var mi = mod.Info;

                        Uri? uri = null;

                        {
                            //Parse steam workshop
                            var p = mi.ModRoot.FullPath;
                            var idx = p.LastIndexOf(STEAMWORKSHOP_PATH_STR);
                            if (idx != -1)
                            {
                                var startIdx = idx + STEAMWORKSHOP_PATH_STR.Length;

                                var swidStr = p[startIdx..];
                                if (long.TryParse(swidStr, out var swid))
                                {
                                    uri = new($"https://steamcommunity.com/sharedfiles/filedetails/?id={swid}");
                                }
                            }
                        }

                        if (uri == null)
                        {
                            if (!string.IsNullOrEmpty(mi.RepositoryUrl))
                            {
                                uri = new(mi.RepositoryUrl);
                            }
                        }

                        if (uri != null)
                        {
                            onClick = () =>
                            {
                                OpenURL(uri.ToString());
                            };
                        }

                        options.addSimpleWidget(GetString(mod.GetDisplayName()),
                            $"Version: {mod.Info.Version}".AsHaxeString(), onClick, Ref<int>.In(5), flow);

                        continue;
                    }

                }

                //Foot

                options.addSeparator("".AsHaxeString(), flow);

                options.addSimpleWidget(GetString("Join Discord"), null, () =>
                {
                    OpenURL("https://discord.gg/qH5gw7hwx7");
                }, Ref<int>.In(0), flow);

                options.addSimpleWidget(GetString("Buy Me a Coffee"), GetString("Your support helps us keep maintaining it.Thank you!"), () =>
                {
                    OpenURL("https://afdian.com/a/FrostyTwilight");
                }, Ref<int>.In(0), flow);
            }

            public string GetName()
            {
                return "";
            }
        }


        [return: NotNullIfNotNull(nameof(str))]
        private static dc.String? GetString( string? str )
        {
            return str == null ? null : GetText.Instance.GetString(str).AsHaxeString();
        }
        void IOnGameInit.OnGameInit()
        {
            Hook_Options.isOnAModSection += Hook_Options_isOnAModSection;
            Hook_Options.buildCurSection += Hook_Options_buildCurSection;
            Hook_OptionsBase.setSection += Hook_OptionsBase_setSection;
            Hook__DefaultPause.__constructor__ += Hook_DefaultPause__constructor__;
            Hook__RichterPause.__constructor__ += Hook__RichterPause__constructor__;
        }


        private void Hook__RichterPause__constructor__( Hook__RichterPause.orig___constructor__ orig, RichterPause arg1 )
        {
            orig(arg1);
            EventSystem.BroadcastEvent<IOnAfterPauseMenuBuild, Pause>(arg1);
        }

        private void Hook_DefaultPause__constructor__( Hook__DefaultPause.orig___constructor__ orig, DefaultPause arg1 )
        {
            orig(arg1);
            EventSystem.BroadcastEvent<IOnAfterPauseMenuBuild, Pause>(arg1);
        }

        private void OpenPauseCoreMenu( Pause pause )
        {
            pause.root.set_visible(false);
            var opt = new Options(pause, null, null);
            SetSection(_mainMenu);
            opt.killOnBack = true;
        }

        /// <summary>
        /// Insert a custom button at the bottom of the pause menu and bind a callback to it.
        /// </summary>
        /// <param name="buttonText"></param>
        /// <param name="onClick"></param>
        /// <param name="pause"></param>
        /// <param name="index">Insertion position index (offset relative to the existing bottom button)</param>
        public void AddCustomButtonToPause( string buttonText, Action<Pause> onClick, Pause pause, int index )
        {
            dc.ui.Text text = Assets.Class.makeText(GetString(buttonText), null, true, null);

            text.set_textAlign(new Align.Center());

            var options = (ArrayObj) ((dynamic)pause).options;
            var botMenu = (Flow)((dynamic)pause).botMenu;

            Interactive? templateInteractive = null;

            foreach (virtual_cb_inter_t_? v in options)
            {
                var inter = v?.inter;
                if (inter == null)
                {
                    continue;
                }
                
                if(inter.onClick != null && inter.onOver != null)
                {
                    var clickCL = (HashlinkClosure)((dynamic)inter.HashlinkObj).onClick;


                    //Find native closure
                    if (clickCL?.BindingThis == null)
                    {
                        continue;
                    }

                    templateInteractive = inter;
                    break;
                }
            }

            double width = text.get_textWidth();
            double height = text.get_textHeight();

            Interactive interactive = new(width, height, text, null)
            {
                onClick = _ => { },
                onOver = _ => { }
            };

            var entry = new virtual_cb_inter_t_
            {
                t = text,
                inter = interactive,
                cb = () => onClick(pause)
            };

            if (templateInteractive != null)
            {
                interactive.onClick = templateInteractive.onClick;

                static HashlinkClosure NewArrowClosure(HashlinkClosure cl, virtual_cb_inter_t_ entry)
                {
                    Debug.Assert(cl.BindingThis != null);

                    var arrowCtx = (HashlinkEnum) cl.BindingThis;

                    var newCtx = new HashlinkEnum((HashlinkEnumType) arrowCtx.Type, 0);
                    newCtx[0] = arrowCtx[0];
                    newCtx[1] = entry;
                    return new HashlinkClosure((HashlinkFuncType) cl.Type, cl.FunctionPtr, newCtx.HashlinkPointer);
                }

                //Unsafe
                interactive.onOver = (dynamic)NewArrowClosure((HashlinkClosure)((dynamic)templateInteractive.HashlinkObj).onOver, entry);
            }

           

            botMenu.addChildAt(text, index + 1);
            options.insertDyn(index, entry);

            pause.onResize();
        }

        private bool Hook_Options_isOnAModSection( Hook_Options.orig_isOnAModSection orig, Options self )
        {
            if (self.curSection is CustomMenu)
            {
                return false;
            }
            return orig(self);
        }

        private static CustomMenu GetCustomMenu( IModMenu mm )
        {
            if (customMenus.TryGetValue(mm, out var cm))
            {
                return cm;
            }
            cm = new CustomMenu()
            {
                MenuMod = mm
            };
            cm.HashlinkObj.MarkStateful();
            customMenus.Add(mm, cm);
            return cm;
        }

        private void Hook_OptionsBase_setSection( Hook_OptionsBase.orig_setSection orig, OptionsBase self, OptionsSection s )
        {
            if (s is CustomMenu cm)
            {
                cm.PrevSection = self.curSection;
            }
            orig(self, s);
        }

        private void Hook_Options_buildCurSection( Hook_Options.orig_buildCurSection orig, Options self )
        {
            if (self.curSection is CustomMenu cm)
            {
                cm.BuildMenu(self);
                return;
            }

            if (self.curSection is OptionsSection.S_Main)
            {
                self.addSimpleWidget(GetString("Core Modding"), null, () =>
                {
                    Logger.Information("Enter core modding menu");

                    SetSection(_mainMenu);
                }, Ref<int>.In(5), null);

                orig(self);
            }
            else
            {
                orig(self);
            }
        }

        /// <summary>
        /// Sets the specified mod menu section as the active section.
        /// </summary>
        /// <remarks>This method scans for available menu mods before setting the section. Ensure that the
        /// provided menu is properly initialized and exists in the customMenus dictionary to avoid runtime
        /// errors.</remarks>
        /// <param name="menu">The mod menu to activate. This parameter must not be null and should correspond to a valid entry in the
        /// customMenus collection.</param>
        public void SetSection( IModMenu menu )
        {
            Options.Class.ME.setSection(GetCustomMenu(menu));
        }
    }
}
