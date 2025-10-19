using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Events.Interfaces.Mods
{
    /// <summary>
    /// An event triggered when the mods loader searches for a valid mod type.
    /// </summary>
    [Event(true)]
    public interface IOnRegisterModsType
    {
        /// <summary>
        /// Add a new mod type to the mods loader
        /// </summary>
        /// <param name="typeName"></param>
        /// <param name="infoType"></param>
        public delegate void AddModType(string typeName, Type infoType);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="add"></param>
        void OnRegisterModsType(AddModType add);
    }
}
