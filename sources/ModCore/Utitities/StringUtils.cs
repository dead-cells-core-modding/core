using System;
using System.Collections.Generic;
using System.Text;

namespace ModCore.Utitities
{
    /// <summary>
    /// Provides utility methods and operators for comparing and working with dc.String instances.
    /// </summary>
    /// <remarks>StringUtils offers static methods and operator overloads to facilitate equality and
    /// inequality comparisons of dc.String objects based on their string content. All members are static and can be
    /// used without instantiating the class.</remarks>
    public static class StringUtils
    {
        extension(dc.String)
        {
            /// <summary>
            /// Determines whether two dc.String instances have equal string content.
            /// </summary>
            /// <remarks>Comparison is based on the result of ToString() for each dc.String instance.
            /// Null instances are supported and treated as having empty string content.</remarks>
            /// <param name="a">The first dc.String instance to compare.</param>
            /// <param name="b">The second dc.String instance to compare.</param>
            /// <returns>true if the string content of both instances is equal; otherwise, false.</returns>
            public static bool operator ==( dc.String a, dc.String b )
            {
                return a.ToString() == b.ToString();
            }
            /// <summary>
            /// Determines whether two dc.String instances have different values.
            /// </summary>
            /// <remarks>Comparison is based on the string representations of the dc.String
            /// instances.</remarks>
            /// <param name="a">The first dc.String to compare.</param>
            /// <param name="b">The second dc.String to compare.</param>
            /// <returns>true if the values of a and b are not equal; otherwise, false.</returns>
            public static bool operator !=( dc.String a, dc.String b )
            {
                return a.ToString() != b.ToString();
            }
        }
    }
}
