namespace ModCore.Events.Interfaces.Game.Hero
{
    /// <summary>
    /// An event is triggered when the hero is destroyed.
    /// </summary>
    [Event]
    public interface IOnHeroDispose
    {
        /// <summary>
        /// 
        /// </summary>
        void OnHeroDispose();
    }
}
