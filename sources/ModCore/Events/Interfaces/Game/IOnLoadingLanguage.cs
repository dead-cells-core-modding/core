using dc;
using System;
using System.Collections.Generic;
using System.Text;

namespace ModCore.Events.Interfaces.Game
{
    /// <summary>
    ///  An event is triggered when load language
    /// </summary>
    [Event()]
    public interface IOnLoadingLanguage
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="lang"></param>
        public void OnLoadingLanguage( string lang );
    }
}
