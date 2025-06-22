using dc;
using dc.haxe.io;
using dc.hl;
using dc.hxbit;
using dc.hxsl;
using Hashlink.Proxy;
using Hashlink.Reflection.Types;
using HaxeProxy.Events;
using HaxeProxy.Runtime;
using HaxeProxy.Runtime.Internals.Inheritance;
using ModCore.Events.Interfaces;
using ModCore.Serialization;
using ModCore.Serialization.Converters;
using ModCore.Storage;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Modules.Internals
{
    [CoreModule(CoreModuleAttribute.CoreModuleKind.Normal)]
    internal unsafe class HxbitModule : CoreModule<HxbitModule>,
        IOnAdvancedModuleInitializing
    {
        public const ulong MAGIC_NUMBER_EXTRA_DATA = 0x004443434D435344;
        public static readonly int CURRENT_VERSION = 1;

        private Class? Hook_resolveClass( Func<dc.String, Class?> orig, dc.String str )
        {
            var result = orig(str);
            if (result == null)
            {
                var type = System.Type.GetType(str.ToString(), false);
                if (type != null)
                {
                    return HaxeProxyUtils.GetClass<Class>(type);
                }
            }
            return result;
        }
        private Func<JsonSerializerSettings>? oldSettingsFactory;
        void IOnAdvancedModuleInitializing.OnAdvancedModuleInitializing()
        {
            HashlinkHooks.Instance.CreateHook("$Type", "resolveClass", Hook_resolveClass);

            oldSettingsFactory = JsonConvert.DefaultSettings;
            JsonConvert.DefaultSettings = SerializerSettingsFactory;

            Hook_Serializer.beginSave += Hook_Serializer_beginSave;
            Hook_Serializer.endSave += Hook_Serializer_endSave;
            Hook_Serializer.addObjRef += Hook_Serializer_addObjRef;
        }

        private JsonSerializerSettings SerializerSettingsFactory()
        {
            var result = oldSettingsFactory?.Invoke() ?? new();
            result.Converters.Add(new HxbitConverter());
            return result;
        }

        private void Hook_Serializer_beginSave( Hook_Serializer.orig_beginSave orig, Serializer self )
        {
            SerializeContext.PushContext(new(self));
            orig(self);
        }


        private void ThrowFatalException( object obj )
        {
            Logger.Error("Unknown serialized object({type}): {string}", obj.GetType().AssemblyQualifiedName, obj.ToString());
            throw new InvalidProgramException("Saved something that shouldn't be saved, now crashes the game to avoid damaging the save. Please check the log for details. Object:"
                + obj.ToString());
        }

        private void Hook_Serializer_addObjRef( Hook_Serializer.orig_addObjRef orig, Serializer self, 
            Hashlink.Virtuals.virtual___uid_getCLID_getSerializeSchema_serialize_unserialize_unserializeInit_ s )
        {
            var ctx = SerializeContext.current;
            if (ctx == null)
            {
                orig(self, s);
                return;
            }
            
            if (self.refs == null ||
                !self.refs.exists(s.__uid))
            {
                var val = s.AsObject<HaxeObject>();
                if (val.HashlinkObj.Type is CustomHaxeType.ReflectType)
                {
                    if (val is not IHxbitSerializable)
                    {
                        ThrowFatalException(val);
                    }
                    ctx.AddItem(val);
                }
            }

            orig(self, s);
        }

        private void AppendBuffer( BytesBuffer buffer, ReadOnlySpan<byte> bytes )
        {
            fixed (byte* ptr = bytes)
            {
                var p = ptr;
                buffer.__add((nint)ptr, 0, bytes.Length);
            }
        }

        private Bytes Hook_Serializer_endSave( Hook_Serializer.orig_endSave orig, Serializer self,
            Ref<int> savePosition )
        {
            var ctx = SerializeContext.current;
            if (ctx == null)
            {
                return orig(self, savePosition);
            }
            if (ctx.Serializer != self)
            {
                throw new InvalidOperationException("beginSave and endSave are called a mismatch of times");
            }
            try
            {
                if (ctx.HasData)
                {
                    var prev = self.@out;
                    self.@out = ctx.hxbitBuffer;

                    ctx.SerializeData();

                    self.@out = prev;

                    var old = orig(self, savePosition);

                    var buffer = new BytesBuffer();

                    buffer.__add(old.b, 0, old.length);

                    var str = JsonConvert.SerializeObject(ctx.Finish());
                    var buf = new byte[System.Text.Encoding.UTF8.GetByteCount(str) + 16];
                    fixed (byte* ptr = buf)
                    {
                        var p = ptr;
                        *((ulong*)p++) = MAGIC_NUMBER_EXTRA_DATA;
                        *((int*)p++) = CURRENT_VERSION;
                        *((int*)p++) = buf.Length;
                        System.Text.Encoding.UTF8.GetBytes(str, new Span<byte>(p, buf.Length));

                        buffer.__add((nint)ptr, 0, buf.Length);
                    }
                    buffer.__add(ctx.hxbitBuffer.b, 0, ctx.hxbitBuffer.size);

                    return buffer.getBytes();
                }
                return orig(self, savePosition);
            }
            finally
            {
                SerializeContext.PopContext();
            }
        }
    }
}
