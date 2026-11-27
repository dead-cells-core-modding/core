namespace ModCore.Events.Interfaces.Game.Hero
{
    /// <summary>
    /// An event is triggered every frame when the hero exists.
    /// </summary>
    [Event]
    public interface IOnHeroUpdate
    {
        /// <summary>
        /// Raised once per frame while the hero exists.
        /// </summary>
        /// <param name="dt">The elapsed time since the previous frame, in seconds.</param>
        void OnHeroUpdate( double dt );
    }
}
