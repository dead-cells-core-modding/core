using dc;
using dc.haxe.io;
using dc.hxd;
using dc.libs.data;
using Hashlink.Marshaling;
using Hashlink.Proxy.DynamicAccess;
using ModCore.Events;
using ModCore.Events.Interfaces;
using ModCore.Events.Interfaces.Game;
using ModCore.Storage;
using ModCore.Utitities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GT = dc.libs.data.GetText;

namespace ModCore.Modules
{
    /// <summary>
    /// 
    /// </summary>
    [CoreModule(CoreModuleAttribute.CoreModuleKind.Normal)]
    public class GetText : CoreModule<GetText>,
        IOnAdvancedModuleInitializing
    {
        private readonly List<string> registeredLangName = [];
        ///<inheritdoc/>
        public override int Priority => ModulePriorities.Game;
        /// <summary>
        /// Get localized strings
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public string GetString( string str )
        {
            var s = str.AsHaxeString();
            return Lang.Class.t.get(s, null).ToString()!;
        }

        /// <summary>
        /// Registers a mod by its name and adds it to the list of registered language names.
        /// </summary>
        /// <param name="name">The name of the mod to register. Cannot be null or empty.</param>
        /// <returns>The name of the registered mod.</returns>
        public void RegisterMod( string name )
        {
            registeredLangName.Add(name);
        }

        void IOnAdvancedModuleInitializing.OnAdvancedModuleInitializing()
        {
            Hook_GetText.readMo += Hook_GetText_readMo;
        }

        private void Hook_GetText_readMo( Hook_GetText.orig_readMo orig, GT self, dc.haxe.io.Bytes r )
        {
            orig(self, r);

            foreach (var v in registeredLangName)
            {
                var basePath = v + "/lang/main.";
                var curPath = basePath + Lang.Class.LANG.ToString() + ".mo";
                Bytes? bytes = null;
                try
                {
                    bytes = Res.Class.load(curPath.AsHaxeString()).entry.getBytes();
                } catch (Exception)
                {
                    try
                    {
                        bytes = Res.Class.load((basePath + "en.mo").AsHaxeString()).entry.getBytes();
                    }
                    catch (Exception)
                    {
                    }
                }
                if (bytes == null)
                {
                    continue;
                }
                self.readNextMo(bytes);
            }

            EventSystem.BroadcastEvent<IOnLoadingLanguage, string>(Lang.Class.LANG.ToString());
        }
    }
}
