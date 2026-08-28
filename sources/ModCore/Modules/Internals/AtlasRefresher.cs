using dc;
using dc.libs.heaps.slib;
using dc.libs.heaps.slib.assets;
using Hashlink.Proxy.Objects;
using ModCore.Events.Interfaces;
using ModCore.Events.Interfaces.Game;
using ModCore.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ModCore.Modules.Internals
{
    [CoreModule(CoreModuleAttribute.CoreModuleKind.Normal)]
    internal class AtlasRefresher : CoreModule<AtlasRefresher>,
        IOnUpdatedPakRecord
    {
        private record class CachedAtlasId(string Name);
        private static readonly Dictionary<string, CachedAtlasId> cachedAtlas = new()
        {
            ["atlas/ui.atlas"] = new("ui"),
            ["atlas/achievements.atlas"] = new("achievements"),
            ["atlas/fxCommon.atlas"] = new("fx"),
            ["atlas/fxEnemy.atlas"] = new("fxEnemy"),
            ["atlas/fxWeapon.atlas"] = new("fxWeapon"),
            ["atlas/fxDisplace.atlas"] = new("fxDisplace"),
            ["atlas/gameElements.atlas"] = new("gameElements")
        };

        void IOnUpdatedPakRecord.OnUpdatedPakRecord( string path )
        {
            if (!Assets.Class.initDone || !Core.Config.Value.AutoRefreshFxAtlas)
            {
                return;
            }
            if (cachedAtlas.TryGetValue(path, out var id))
            {
                var newSprLib = Atlas.Class.load(path.AsHaxeString(), null, null, null);

                var obj = (HashlinkObject)Assets.Class.HashlinkObj;
                var sprLib = (SpriteLib?)(dynamic?) obj.GetFieldValue(id.Name);
                if (sprLib != null)
                {
                    sprLib.children = newSprLib.children;
                    sprLib.groups = newSprLib.groups;
                    sprLib.normalPages = newSprLib.normalPages;
                    sprLib.currentGroup = newSprLib.currentGroup;
                    sprLib.defaultCenterX = newSprLib.defaultCenterX;
                    sprLib.defaultCenterY = newSprLib.defaultCenterY;
                    sprLib.gridX = newSprLib.gridX;
                    sprLib.gridY = newSprLib.gridY;
                    sprLib.pages = newSprLib.pages;
                }
                obj.SetFieldValue(id.Name, newSprLib);
            }
            else if (path == "atlas/lore.atlas")
            {
                Assets.Class.dynamicAtlasByAtlasId.set(new DynamicLoadAtlas.Lore(), null);
            }
        }
    }
}
