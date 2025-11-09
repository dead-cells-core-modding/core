using dc;
using dc.tool.mod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Events.Interfaces.Game
{
    ///<summary>
    /// An event fired after the game attempts to load cdb
    ///</summary>
    [Event]
    public interface IOnAfterLoadingCDB
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cdb"></param>
        public void OnAfterLoadingCDB( _Data cdb );
    }
}
