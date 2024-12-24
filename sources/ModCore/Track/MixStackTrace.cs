using Hashlink;
using ModCore.Hashlink;
using ModCore.Modules;
using ModCore.Track;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Track
{
    public unsafe class MixStackTrace : StackTrace
    {
        private StackTrace innerTrace;

        private List<StackFrame> frames = [];

        public MixStackTrace(int skipFrames = 0, bool needFileInfo = false)
        {
            Core.ThrowIfNotMainThread();

            innerTrace = new(skipFrames + 1, needFileInfo);

            frames.Capacity = innerTrace.FrameCount;

            CollectInfo();
        }

        public override int FrameCount => frames.Count;
        public override StackFrame? GetFrame(int index)
        {
            return frames[index];
        }
        public override StackFrame[] GetFrames()
        {
            return [.. frames];
        }

        private void CollectInfo()
        {
            var t = HashlinkNative.hl_get_thread();
            void** buf = stackalloc void*[512];
            var ebpCount = Native.mcn_load_stacktrace(buf, 512, t->stack_top);

            var curTransition = MixTrace.current;
            var managedId = 0;

            for (int i = 0; i < ebpCount; i++)
            {
                var ebp = (nint)buf[i];
                if (curTransition != null)
                {
                    if (ebp >= curTransition.esp ||
                        (ebp >= curTransition.ebp && curTransition.ebp > 0))
                    {
                        //Enter managed code
                        bool enter = false;
                        while (managedId < innerTrace.FrameCount)
                        {
                            var frame = innerTrace.GetFrame(managedId++)!;
                            if(!enter)
                            {
                                if(frame.GetMethod()?.GetCustomAttribute<WillCallHL>() == null)
                                {
                                    continue;
                                }
                            }
                            enter = true;
                            frames.Add(frame);
                            if (frame.GetMethod()?.GetCustomAttribute<CallFromHLOnly>() != null)
                            {
                                break;
                            }
                            //Do something to exit
                        }
                        curTransition = curTransition.next;
                    }
                }
                var eip = ((void**)ebp)[1];
                ebp = *(nint*)ebp;

                var m = HashlinkVM.Instance.Context->m;


                if (!Native.module_resolve_pos(m, eip, out var fidx, out var fpos))
                {
                    //Unknown frame
                    continue;
                }

                var func = m->code->functions + fidx;
                var debug_addr = func->debug + ((fpos & 0xFFFF) * 2);

                string funcName;

                if (func->obj != null)
                {
                    funcName = $"{HashlinkUtils.GetString(func->obj->name)}.{HashlinkUtils.GetString(func->field)}";
                }
                else
                {
                    //funcName = $"fun${fidx}";
                    continue;
                }

                frames.Add(new HLStackFrame()
                {
                    Pointer = (nint)eip,
                    FuncName = funcName,
                    FileName = HashlinkUtils.GetString(m->code->debugfiles[debug_addr[0]],
                        m->code->debugfiles_lens[debug_addr[0]], Encoding.UTF8),
                    FileLine = debug_addr[1],
                });
            }
            while(managedId < innerTrace.FrameCount)
            {
                frames.Add(innerTrace.GetFrame(managedId++)!);
            }
        }
    }
}
