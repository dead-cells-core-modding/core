using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Events.Interfaces.Mods
{
    [Event(true)]
    public interface IOnRegisterModsType
    {
        public delegate void AddModType(string typeName, Type infoType);
        void OnRegisterModsType(AddModType add);
    }
}
