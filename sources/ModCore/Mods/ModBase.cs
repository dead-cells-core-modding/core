using ModCore.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Mods
{
    /// <summary>
    /// Base class for all mods
    /// </summary>
    /// <param name="info"></param>
    public class ModBase(ModInfo info) : Module
    {
        /// <summary>
        /// Metadata about the mod
        /// </summary>
        public ModInfo Info { get; } = info;

        /// <summary>
        /// Called when the mod is being initialized
        /// </summary>
        public virtual void Initialize()
        {

        }
    }
}
