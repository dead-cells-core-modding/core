namespace ModCore.Events.Interfaces.Game.Save
{
    /// <summary>
    /// An event triggered when a save file is saved.
    /// </summary>
    [Event]
    public interface IOnAfterSavingSave
    {
        /// <summary>
        /// 
        /// </summary>
        void OnAfterSavingSave();
    }
}
