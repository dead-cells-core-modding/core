using Hashlink.Proxy;
using HaxeProxy.Runtime;
using System.Dynamic;

namespace ModCore.Utilities
{

    /// <summary>
    /// Provides utility methods and extensions for working with extra data associated with Hashlink objects.
    /// </summary>
    /// <remarks>This static class offers mechanisms to attach and access additional metadata on HashlinkObj
    /// instances. It is intended for advanced scenarios where dynamic, per-object data storage is required. Thread
    /// safety is not guaranteed; callers should ensure appropriate synchronization if accessing extra data from
    /// multiple threads.</remarks>
    public static class ExtraDataUtils
    {
        private class ExtraDataContainer : IExtraDataItem
        {
            public readonly ExpandoObject container = [];
            static object IExtraDataItem.Create( HashlinkObj obj )
            {
                obj.MarkStateful();
                return new ExtraDataContainer();
            }
        }

        extension( HashlinkObj obj )
        {
            /// <summary>
            /// Gets the metadata associated with the current object.
            /// </summary>
            /// <remarks>The returned value provides access to additional data or properties that are
            /// not part of the object's primary schema. The structure and contents of the metadata may vary depending
            /// on the context in which the object is used.</remarks>
            public dynamic Meta => ((IExtraData)obj).GetData<ExtraDataContainer>().container;
        }

        extension( HaxeProxyBase obj )
        {
            /// <summary>
            /// Gets the metadata associated with the current object.
            /// </summary>
            /// <remarks>The structure and contents of the metadata are dynamic and may vary depending
            /// on the source object. Consumers should verify the expected properties and types at runtime.</remarks>
            public dynamic Meta => obj.HashlinkObj.Meta;
        }


    }
}
