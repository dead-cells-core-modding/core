namespace ModCore.Utilities
{
    /// <summary>
    /// Provides utility methods for working with color values.
    /// </summary>
    public static class ColorUtils
    {
        /// <summary>
        /// Packs the specified red, green, blue, and alpha color components into a single 32-bit integer in RGBA order.
        /// </summary>
        /// <remarks>The returned integer can be used for efficient color storage or transmission where a
        /// single value is required to represent all four color channels.</remarks>
        /// <param name="r">The red component of the color. Represents the most significant byte in the packed value.</param>
        /// <param name="g">The green component of the color.</param>
        /// <param name="b">The blue component of the color.</param>
        /// <returns>A 32-bit integer containing the packed RGBA color value, with the red component in the highest byte and the
        /// alpha component in the lowest byte.</returns>
        public static int PackColor( byte r, byte g, byte b )
        {
            return (r << 16) | (g << 8) | (b << 0);
        }
    }
}
