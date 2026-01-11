using System.Dynamic;
using System.Reflection;

namespace Hashlink.Proxy
{
    partial class HashlinkObj : DynamicObject
    {
        private static readonly MethodInfo iextenddata_getdata = typeof(IExtraData).GetMethod(nameof(IExtraData.GetData))!;

        public override bool TryGetMember( GetMemberBinder binder, out object? result )
        {
            return base.TryGetMember(binder, out result);
        }

        public override bool TryConvert( ConvertBinder binder, out object? result )
        {
            if (binder.Type.IsAssignableTo(typeof(HashlinkObj)))
            {
                result = this;
                return true;
            }
            if (binder.Type == typeof(string))
            {
                result = ToString();
                return true;
            }
            if (binder.Type.IsAssignableTo(typeof(IExtraDataItem)))
            {
                var m = iextenddata_getdata.MakeGenericMethod(binder.Type);
                result = m.Invoke(this, null);
                return true;
            }
            return base.TryConvert(binder, out result);
        }
    }
}
