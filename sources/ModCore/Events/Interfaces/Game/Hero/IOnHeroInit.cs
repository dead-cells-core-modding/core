namespace ModCore.Events.Interfaces.Game.Hero
{
    /// <summary>
    /// An event is triggered when the hero is being initialized.
    /// </summary>
    [Event]
    public interface IOnHeroInit
    {
        /// <summary>
        /// Raised when the hero is being initialized.
        /// </summary>
        void OnHeroInit();
    }
}
