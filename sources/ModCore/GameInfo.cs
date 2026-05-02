using dc;

namespace ModCore
{
    /// <summary>
    /// 
    /// </summary>
    public static class GameInfo
    {
        /// <summary>
        /// Specifies the current platform types for the game.
        /// </summary>
        public enum PlatformKind
        {
            /// <summary>
            /// Represents an unknown or unspecified value.
            /// </summary>
            Unknown,

            /// <summary>
            /// Represents Steam
            /// </summary>
            Steam
        }
        /// <summary>
        /// Gets the current version number of the game.
        /// </summary>
        public static int GameVersion => Main.Class.GAME_VERSION;

        /// <summary>
        /// Gets the version of the DCCM.
        /// </summary>
        public static Version DCCMVersion {
            get;
        } = typeof(GameInfo).Assembly.GetName().Version!;

        /// <summary>
        /// Gets the current platform on which the game is running.
        /// </summary>
        public static PlatformKind Platform {
            get; internal set;
        } = PlatformKind.Unknown;

        /// <summary>
        /// Gets the computed HLBoot hash value as a byte array.
        /// </summary>
        public static byte[] HlbootHash {
            get; internal set;
        } = [];
    }
}
