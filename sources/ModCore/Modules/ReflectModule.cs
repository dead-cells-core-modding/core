using dc;
using dc.hscript;
using Hashlink.Marshaling;
using Hashlink.Proxy;
using Hashlink.UnsafeUtilities;
using HaxeProxy.Runtime;
using ModCore.Events.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace ModCore.Modules
{
    [CoreModule(CoreModuleAttribute.CoreModuleKind.Normal)]
    internal class ReflectModule : CoreModule<ReflectModule>,
        IOnAdvancedModuleInitializing
    {
        private static readonly MethodInfo MI_castObject = typeof(UtilityDelegates).GetMethod(nameof(UtilityDelegates.CastObject), BindingFlags.Static | BindingFlags.NonPublic)!;
        private static readonly ConcurrentDictionary<System.Type, Func<object, object>> castDel = [];
        void IOnAdvancedModuleInitializing.OnAdvancedModuleInitializing()
        {
            Hook__Reflect.setField += Hook__Reflect_setField;
            Hook__Reflect.hasField += Hook__Reflect_hasField;
        }

        private bool Hook__Reflect_hasField( Hook__Reflect.orig_hasField orig, object o, dc.String field )
        {
            try
            {
                return orig(o, field);
            }
            catch (HashlinkError)
            {

                if (o is HashlinkObj ho)
                {
                    o = ho.AsHaxe();
                }

                var ot = o.GetType();
                return ot.GetField(field.ToString(), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) != null;
            }
        }

        private static Func<object, object> GetCastDel( System.Type toType )
        {
            return castDel.GetOrAdd(toType, (System.Type key) =>
            {
               
                var p = Expression.Parameter(typeof(object));
                return Expression.Lambda<Func<object, object>>
                    (Expression.Call(MI_castObject.MakeGenericMethod(toType), p),
                    p).Compile();
                ;
            });
        }

        private void Hook__Reflect_setField( Hook__Reflect.orig_setField orig, object o, dc.String field, object value )
        {
            try
            {
                orig(o, field, value);
            }
            catch (HashlinkError)
            {
                if (o is HashlinkObj ho)
                {
                    o = ho.AsHaxe();
                }

                var ot = o.GetType();
                var f = ot.GetField(field.ToString(), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) ??
                    throw new MissingFieldException(ot.FullName, field.ToString());

                f.SetValue(o, GetCastDel(f.FieldType)(value));
            }
        }
    }
}
