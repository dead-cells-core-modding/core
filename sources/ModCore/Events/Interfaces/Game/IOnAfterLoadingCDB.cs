using dc;

namespace ModCore.Events.Interfaces.Game
{
    ///<summary>
    /// An event fired after the game attempts to load cdb
    ///</summary>
    [Event]
    public interface IOnAfterLoadingCDB
    {
        /// <summary>
        /// Raised after the game has loaded the CDB.
        /// </summary>
        /// <param name="cdb">The loaded CDB data.</param>
        public void OnAfterLoadingCDB( _Data_ cdb );
    }
}
