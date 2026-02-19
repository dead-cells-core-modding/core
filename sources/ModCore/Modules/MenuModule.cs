using dc.ui;
using HaxeProxy.Runtime;
using ModCore.Events;
using ModCore.Events.Interfaces.Game;
using ModCore.Menu;
using ModCore.Mods;
using ModCore.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

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
        private Dictionary<IModMenu, CustomMenu>? customMenus;

        private readonly MainMenu _mainMenu = new();

        private static readonly string STEAMWORKSHOP_PATH_STR = "workshop\\content\\588650\\".Replace('\\', Path.DirectorySeparatorChar);

        /// <summary>
        /// Gets the options section associated with the main menu of the DCCM.
        /// </summary>
        public OptionsSection DCCMMainMenu
        {
            get
            {

                TryScanMenuMods();
                return customMenus[_mainMenu];
            }
        }

        private class CustomMenu : OptionsSection.S_Main
        {
            public OptionsSection? PrevSection
            {
                get; set;
            }
            public required IModMenu MenuMod
            {
                get; init;
            }

            public void BuildMenu( Options options )
            {
                MenuMod.BuildMenu(options);
            }
        }

        private class MainMenu : IModMenu
        {
            public void BuildMenu( Options options )
            {
                Instance.TryScanMenuMods();

                options.title.set_text(GetString("CORE MODDING"));

                options.createScroller(1);

                var flow = options.scrollerFlow;

                int offX = 5;

                options.addSimpleWidget(GetString("About Core Modding"), 
                    $"DCCM Version: {Core.Version}".AsHaxeString(), () =>
                {
                    Logger.Information("Open https://dead-cells-core-modding.github.io/docs/docs/");
                    System.Diagnostics.Process.Start(new ProcessStartInfo()
                    {
                        UseShellExecute = true,
                        FileName = "https://dead-cells-core-modding.github.io/docs/docs/"
                    });
                }, Ref<int>.From(ref offX), flow);

                options.addSeparator(GetString("Loaded Mods"), flow);

                foreach (var mod in EventSystem.FindReceivers<ModBase>())
                {
                    if (mod is not IModMenu
                        && mod is not IModMenuProvider)
                    {
                        HlAction onClick = static () => { };

                        var mi = mod.Info;

                        Uri? uri = null;

                        if (!string.IsNullOrEmpty(mi.RepositoryUrl))
                        {
                            uri = new(mi.RepositoryUrl);
                        }
                        else
                        {
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
                        }

                        if (uri != null)
                        {
                            onClick = () =>
                            {
                                System.Diagnostics.Process.Start(new ProcessStartInfo()
                                {
                                    UseShellExecute = true,
                                    FileName = uri.ToString()
                                });
                            };
                        }

                        options.addSimpleWidget(GetString(mod.GetDisplayName()),
                            $"Version: {mod.Info.Version}".AsHaxeString(), onClick, Ref<int>.From(ref offX), flow);

                        continue;
                    }

                    if (mod is not IModMenu mm)
                    {
                        mm = ((IModMenuProvider)mod).GetModMenu();
                    }

                    var cm = Instance.customMenus[mm];

                    var name = mm.GetName();
                    if (string.IsNullOrEmpty(name))
                    {
                        continue;
                    }

                    options.addSimpleWidget(GetString(name), GetString(mm.GetSubText()), () =>
                    {
                        options.setSection(cm);
                    }, Ref<int>.From(ref offX), flow);
                }
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
        }

        private bool Hook_Options_isOnAModSection( Hook_Options.orig_isOnAModSection orig, Options self )
        {
            if (self.curSection is CustomMenu)
            {
                return false;
            }
            return orig(self);
        }

        [MemberNotNull(nameof(customMenus))]
        private void TryScanMenuMods()
        {
            if (customMenus != null)
            {
                return;
            }
            customMenus = [];

            IModMenu[] menus = [
                .. EventSystem.FindReceivers<IModMenu>(),
                _mainMenu
                ];


            foreach (var v in menus)
            {
                var cm = new CustomMenu()
                {
                    MenuMod = v
                };
                customMenus.Add(v, cm);
            }
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

            TryScanMenuMods();



            if (self.curSection is OptionsSection.S_Main)
            {
                int offX = 5;
                self.addSimpleWidget(GetString("Core Modding"), null, () =>
                {
                    Logger.Information("Enter core modding menu");

                    SetSection(_mainMenu);
                }, Ref<int>.From(ref offX), null);

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
            TryScanMenuMods();

            Options.Class.ME.setSection(customMenus[menu]);
        }
    }
}
