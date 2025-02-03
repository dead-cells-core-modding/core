using ModCore.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Mods
{
    public class ModBase(ModInfo info) : Module
    {
        public ModInfo Info { get; } = info;

        public virtual void Initialize()
        {

        }
    }
}
