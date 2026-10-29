using dc.haxe.io;
using dc.hxd.snd;
using dc.hxd.snd.openal;
using dc.ui;
using ModCore.Events;
using ModCore.Events.Interfaces;
using ModCore.Events.Interfaces.VM;
using ModCore.Storage;
using System;
using System.Collections.Generic;
using System.Text;
using Path = System.IO.Path;

namespace ModCore.Modules.Internals
{
    [CoreModule(CoreModuleAttribute.CoreModuleKind.Preload)]
    internal class LegacySFX : CoreModule<LegacySFX>,
        IOnCoreModuleInitializing,
        IOnAdvancedModuleInitializing
    {
        private static class HashlinkHooks
        {
            private const double MonoTrim = 0.846; // -1.45 dB

            private static readonly Dictionary<int, int> bufferChannels = new();
            private static readonly Dictionary<int, int> sourceChannels = new();
            internal static void Init()
            {
                Hook_Driver.setBufferData += Hook_setBufferData;
                Hook_Driver.queueBuffer += Hook_queueBuffer;
                Hook_Driver.setSourceVolume += Hook_setSourceVolume;
                Hook_Driver.createSource += Hook_createSource;
            }

            private static void Hook_setBufferData(
            Hook_Driver.orig_setBufferData orig,
            Driver self,
            BufferHandle buffer,
            Bytes data,
            int size,
            SampleFormat format,
            int channelCount,
            int samplingRate )
            {
                bufferChannels[buffer.inst] = channelCount;
                orig(self, buffer, data, size, format, channelCount, samplingRate);
            }

            private static void Hook_queueBuffer(
                Hook_Driver.orig_queueBuffer orig,
                Driver self,
                SourceHandle source,
                BufferHandle buffer,
                int sampleStart,
                bool endOfStream )
            {
                if (bufferChannels.TryGetValue(buffer.inst, out var ch))
                    sourceChannels[source.inst] = ch;

                orig(self, source, buffer, sampleStart, endOfStream);
            }

            private static void Hook_setSourceVolume(
                Hook_Driver.orig_setSourceVolume orig,
                Driver self,
                SourceHandle source,
                double value )
            {
                if (sourceChannels.TryGetValue(source.inst, out var ch) && ch == 1)
                    value *= MonoTrim;

                orig(self, source, value);
            }

            private static SourceHandle Hook_createSource(
                Hook_Driver.orig_createSource orig,
                Driver self )
            {
                var source = orig(self);
                sourceChannels.Remove(source.inst);
                return source;
            }
        }
        private const string ALSOFT_INI_CONTENT = """
[general]
resampler = linear
volume-adjust = 0
output-limiter = false
dither = false

[reverb]
boost = 6
""";
        void IOnCoreModuleInitializing.OnCoreModuleInitializing()
        {
            if (!Core.Config.Value.UseOldSFX)
            {
                return;
            }

            var alsoftini = Path.GetFullPath(FolderInfo.Cache.GetFilePath("alsoft.ini"));
            File.WriteAllText(alsoftini, ALSOFT_INI_CONTENT);

            Environment.SetEnvironmentVariable("ALSOFT_CONF", alsoftini);
        }

        void IOnAdvancedModuleInitializing.OnAdvancedModuleInitializing()
        {
            if (!Core.Config.Value.UseOldSFX)
            {
                return;
            }

            HashlinkHooks.Init();
        }
    }
}
