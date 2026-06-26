using dc;
using dc.cdb;
using dc.hxd;
using dc.hxd.fmt.pak;
using dc.hxd.res;
using GameRes.Core.Cdb;
using Hashlink.Marshaling;
using Hashlink.Reflection.Types;
using HaxeProxy.Runtime;
using ModCore.Events.Interfaces;
using ModCore.Utilities;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace ModCore.Modules
{
    /// <summary>
    /// 
    /// </summary>
    [CoreModule(CoreModuleAttribute.CoreModuleKind.Normal)]
    public class CDBManager : CoreModule<CDBManager>,
        IOnAdvancedModuleInitializing
    {
        private string? overrideJsonData = null;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="jsonData"></param>
        public unsafe void LoadJsonData( string jsonData )
        {
            try
            {
                overrideJsonData = jsonData;
                dc.Data.Class.loadFrom("CDBManager_Override".AsHaxeString(), Ref<bool>.In(false));
            }
            finally
            {
                overrideJsonData = null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string GetAlteredCDB()
        {
            if (Core.Config.Value.UseGameCDBManager)
            {
                if (HashlinkMarshal.Module.TryGetTypeByName("tool.mod.CDBManager", out var gCDBManagerType))
                {
                    var inst = ((dynamic)((HashlinkObjectType)gCDBManagerType).GlobalValue).instance;
                    return inst.getAlteredCDB();
                }
            }

            Loader loader = Res.Class.get_loader();
            dc.String jsonStr = loader.loadCache("data.cdb".AsHaxeString(), Resource.Class).entry.getBytes().toString();
            if (!FsPak.Instance.FileSystem.exists("data.cdb_".AsHaxeString()))
            {
                return jsonStr.ToString();
            }
            var cdb = CdbFile.ReadFrom(jsonStr.ToString());

            Dictionary<string, string> overrideJson = [];

            var rootEntry = (PakEntry) FsPak.Instance.FileSystem.get("data.cdb_".AsHaxeString());
            Dictionary<string, PakEntry> sheetEntries = [];

            foreach (PakEntry child in rootEntry.subs)
            {
                if (child.get_isDirectory())
                {
                    sheetEntries.Add(child.name.ToString(), child);
                }
            }

            foreach ((var sheetName, var entry) in sheetEntries)
            {
                var osheet = cdb.Sheets.FirstOrDefault(x => x.Name == sheetName);
                if (osheet == null)
                {
                    continue;
                }
                foreach (PakEntry child in entry.subs)
                {
                    if (child.get_isDirectory())
                    {
                        continue;
                    }
                    var json = JToken.Parse(child.getText().ToString());
                    var name = Path.GetFileNameWithoutExtension(child.name.ToString());

                    var sepName = json["__separator_group_Name"]?.ToString();

                    var separator = osheet.Separators.FirstOrDefault(x => x.Name == sepName) ?? osheet.Separators[0];
                    var line = osheet.Separators.SelectMany(x => x.Lines).FirstOrDefault(x => x.Name == name);

                    if (line == null)
                    {
                        line = new CdbLine()
                        {
                            Separator = separator,
                            Value = (JObject)json
                        };
                    }
                    else
                    {
                        line.Value = (JObject)json;
                        line.Separator.Lines.Remove(line);
                    }
                    separator.Lines.Add(line);
                }
            }

            var root = new JObject();
            cdb.WriteTo(root);
            return root.ToString();
        }

        void IOnAdvancedModuleInitializing.OnAdvancedModuleInitializing()
        {
            Hook__MultifileLoadSave.readFile += Hook__MultifileLoadSave_readFile;
        }

        private dc.String Hook__MultifileLoadSave_readFile( Hook__MultifileLoadSave.orig_readFile orig, dc.String fullPath )
        {
            if (string.IsNullOrEmpty(overrideJsonData))
            {
                return orig(fullPath);
            }
            return overrideJsonData.AsHaxeString();
        }
    }
}
