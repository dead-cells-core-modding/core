using dc;
using dc.haxe.io;
using dc.hxd;
using dc.libs.data;
using HaxeProxy.Runtime;
using ModCore.Events;
using ModCore.Events.Interfaces;
using ModCore.Events.Interfaces.Game;
using ModCore.Utilities;
using ModCore.Utitities;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

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
        /// Retrieves a formatted string by applying the specified parameters to the input template.
        /// </summary>
        /// <param name="str">The string template to format. This string may contain placeholders that correspond to keys in the <paramref
        /// name="params"/> dictionary.</param>
        /// <param name="params">A dictionary containing key-value pairs to substitute into the template. Each key should match a placeholder
        /// in the template string.</param>
        /// <returns>A string resulting from replacing placeholders in the template with the corresponding values from the
        /// dictionary.</returns>
        public string GetString( string str, IDictionary<string, string> @params )
        {
            var s = str.AsHaxeString();
            dynamic param = new HaxeDynObj();
            foreach (var v in @params)
            {
                param[v.Key] = v.Value.AsHaxeString();
            }
            return Lang.Class.t.get(s, param).ToString()!;
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

            RegisterMod("dccm-core");
        }

        private bool LoadResByNames([NotNullWhen(true)] out Bytes? data, params ReadOnlySpan<string> names )
        {
            foreach (var name in names)
            {
                try
                {
                    data = Res.Class.load(name.AsHaxeString()).entry.getBytes();
                    return true;
                }
                catch (Exception)
                {

                }
            }
            data = null;
            return false;
        }
        private static string GetMoPathString( string lang, string? ns, string name )
        {
            if (string.IsNullOrEmpty(ns)) {
                return $"lang/{name}.{lang}.mo";
            }
            return $"{ns}/lang/{name}.{lang}.mo";
        }
        private void Hook_GetText_readMo( Hook_GetText.orig_readMo orig, GT self, dc.haxe.io.Bytes r )
        {
            orig(self, r);
            var curLang = Lang.Class.LANG.ToString();
            CultureInfo.CurrentUICulture = CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(curLang);

            foreach (var v in registeredLangName)
            {
                if (!LoadResByNames(out var data,
                    GetMoPathString(curLang, v, "main"),
                    GetMoPathString(curLang, v, v),
                    GetMoPathString(curLang, null, v),
                    GetMoPathString("en", v, "main"),
                    GetMoPathString("en", v, v),
                    GetMoPathString("en", null, v)
                    ))
                {
                    continue;
                }

                var texts = new MoReader(data).parse();
                foreach (var k in texts.keys())
                {
                    self.texts.set(k, texts.get(k));
                }
            }

            EventSystem.BroadcastEvent<IOnLoadingLanguage, string>(Lang.Class.LANG.ToString());
        }
    }
}
