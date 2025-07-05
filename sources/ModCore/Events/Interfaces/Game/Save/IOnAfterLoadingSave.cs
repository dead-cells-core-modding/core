using dc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Events.Interfaces.Game.Save
{
    [Event]
    public interface IOnAfterLoadingSave
    {
        void OnAfterLoadingSave( User data );
    }
}
