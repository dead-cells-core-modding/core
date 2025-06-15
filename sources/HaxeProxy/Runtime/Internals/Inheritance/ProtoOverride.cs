using Hashlink;
using Hashlink.Reflection.Members.Object;
using Hashlink.Reflection.Types;
using Hashlink.UnsafeUtilities;
using Hashlink.Wrapper.Callbacks;
using ModCore.Collections;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace HaxeProxy.Runtime.Internals.Inheritance
{
    internal unsafe class ProtoOverride
    {
        private readonly HlCallback callback;
        private readonly DynamicMethod virtualRouter;

        public ProtoOverride( 
            HashlinkObjectProto proto, 
            HL_type* type,
            MethodInfo vmethod)
        {
            if (!proto.IsVirtual)
            {
                throw new InvalidOperationException();
            }

            Type[] ptypes = [typeof(object), typeof(HaxeProxyBase),.. vmethod.GetParameters().Select(x => x.ParameterType)];

            virtualRouter = new("<VirtualRouter>+" + vmethod.GetID(),
                vmethod.ReturnType, ptypes);

            {
                var ilp = virtualRouter.GetILGenerator();

                for (int i = 1; i < ptypes.Length; i++)
                {
                    ilp.Emit(OpCodes.Ldarg, i);
                }
                ilp.Emit(OpCodes.Callvirt, vmethod);
                ilp.Emit(OpCodes.Ret);
            }

            callback = HlCallbackFactory.GetHlCallback(proto.Function.FuncType);
            callback.Target = virtualRouter.CreateAnonymousDelegate(vmethod)
                                            .CreateAdaptDelegate();

            {

                var nt = type;
                nt->vobj_proto[proto.ProtoIndex] = (void*)callback.NativePointer;
                var o = nt->data.obj;

                var method_index = -HashlinkNative.obj_resolve_field(o,
                        proto.HashedName)->field_index-1;
                o->rt->methods[method_index] = (void*)callback.NativePointer;
            }
        }
    }
}
