using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Events.Interfaces.Game
{
    public interface IOnGameExit : ICallOnceEvent<IOnGameExit>
    {
        public void OnGameExit();
    }
}
