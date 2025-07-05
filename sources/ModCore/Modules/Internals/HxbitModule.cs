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
using System.IO.Hashing;
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
        public const ulong MAGIC_NUMBER_EXTRA_DATA = 0x004443434D435300;
        public static readonly int CURRENT_VERSION = 3;

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

            Hook_Serializer.beginLoad += Hook_Serializer_beginLoad;
            Hook_Serializer.endLoad += Hook_Serializer_endLoad;
        }

        private void Hook_Serializer_endLoad( Hook_Serializer.orig_endLoad orig, Serializer self )
        {
            var ctx = DeserializeContext.current;
            if (ctx == null ||
                ctx.Serializer != self)
            {
                orig(self);
                return;
            }
            try
            {
                var cur = (byte*)self.input.b + self.inPos;
                var end = (byte*)self.input.b + self.input.length;

                if (*((ulong*)cur) != MAGIC_NUMBER_EXTRA_DATA)
                {
                    //No extra data
                    orig(self);
                    return;
                }
                cur += sizeof(ulong);
                var ver = Read<int>(ref cur);
                if (ver != CURRENT_VERSION)
                {
                    Logger.Warning("Version number mismatch: expected {A} instead of {B}.", CURRENT_VERSION, ver);
                    Logger.Warning("Skip extra data loading, save data may be corrupted after saving.");
                    orig(self);
                    return;
                }
                var size = Read<int>(ref cur) - 16;
                var str = System.Text.Encoding.UTF8.GetString(cur, size);
                var data = JsonConvert.DeserializeObject<SerializeContext.Data>(
                    str
                    );

                Debug.Assert(data != null);

                cur = cur + size;

                var checksum = Crc64.HashToUInt64(new ReadOnlySpan<byte>(
                    cur, data.extraHxObjSize
                ));
                if (checksum != data.extraHxObjChecksum)
                {
                    throw new InvalidOperationException("Save is corrupted.");
                }

                self.setInput(new((nint)cur, data.extraHxObjSize), 0);

                ctx.Begin(data);

                orig(self);
            }
            finally
            {
                DeserializeContext.PopContext();
            }
        }

        private void Hook_Serializer_beginLoad( Hook_Serializer.orig_beginLoad orig, Serializer self, 
            Bytes bytes, Ref<int> position )
        {
            DeserializeContext.PushContext(new(self));
            orig(self, bytes, position);
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
        private static void Write<T>( ref byte* ptr, T value ) where T : unmanaged
        {
            *(T*)ptr = value;
            ptr += sizeof(T);
        }
        private static T Read<T>( ref byte* ptr ) where T : unmanaged
        {
            var result = *(T*)ptr;
            ptr += sizeof(T);
            return result;
        }

        private Bytes Hook_Serializer_endSave( Hook_Serializer.orig_endSave orig, Serializer self,
            Ref<int> savePosition )
        {
            var ctx = SerializeContext.current;
            if (ctx == null ||
                ctx.Serializer != self)
            {
                return orig(self, savePosition);
            }
            try
            {
                if (ctx.HasData)
                {
                    var prev = self.@out;
                    ctx.hxbitBuffer.pos = 0;
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
                        Write(ref p, MAGIC_NUMBER_EXTRA_DATA);
                        Write(ref p, CURRENT_VERSION);
                        Write(ref p, buf.Length);

                        System.Text.Encoding.UTF8.GetBytes(str, new Span<byte>(p, buf.Length));

                        buffer.__add((nint)ptr, 0, buf.Length);
                    }
                    buffer.__add(ctx.hxbitBuffer.b, 0, ctx.hxbitBuffer.pos);

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
