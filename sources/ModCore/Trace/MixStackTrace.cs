using Hashlink;
using Hashlink.Trace;
using ModCore.Modules;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace ModCore.Trace
{
    public unsafe class MixStackTrace : StackTrace
    {
        private readonly StackTrace innerTrace;

        private readonly List<StackFrame> frames = [];

        public MixStackTrace( StackTrace trace )
        {
            innerTrace = trace;

            frames.Capacity = innerTrace.FrameCount;

            CollectInfo();
        }
        public MixStackTrace( int skipFrames = 0, bool needFileInfo = false ) : this(new(skipFrames + 1, needFileInfo))
        {
        }

        public override int FrameCount => frames.Count;
        public override StackFrame? GetFrame( int index )
        {
            return frames[index];
        }
        public override StackFrame[] GetFrames()
        {
            return [.. frames];
        }
        private static string GetFunctionName( HL_function* func )
        {
            return func->obj != null
                ? $"{new string(func->obj->name)}.{new string(func->field.field)}@{func->findex}"
                : func->field.@ref != null ? GetFunctionName(func->field.@ref) : $"fun${func->findex}";
        }
        private void CollectInfo()
        {
            var t = hl_get_thread();
            var buf = stackalloc HLU_stack_frame[512];
            var ebpCount = mcn_load_stacktrace(buf, 512, t->stack_top);

            var curTransition = MixTrace.current;
            var managedId = 0;

            var jitStart = (nint)HashlinkVM.Instance.Context->m->jit_code;
            var jitEnd = jitStart + HashlinkVM.Instance.Context->m->codesize;

            var symBuf = stackalloc char[512];
            var modNameBuf = stackalloc char[512];

            for (var i = 0; i < ebpCount; i++)
            {
                var ebp = buf[i].esp;
                var eip = buf[i].eip;
                if (curTransition != null)
                {
                    if (ebp >= curTransition.esp ||
                        (ebp >= curTransition.ebp && curTransition.ebp > 0))
                    {
                        //Enter managed code
                        var enter = false;
                        while (managedId < innerTrace.FrameCount)
                        {
                            var frame = innerTrace.GetFrame(managedId++)!;
                            var method = frame.GetMethod();
                            if (!enter)
                            {
                                if (method?.GetCustomAttribute<WillCallHL>() == null)
                                {
                                    continue;
                                }
                            }
                            enter = true;
                            if (Core.Config.Value.DetailedStackTrace || (
                                method?.GetCustomAttribute<StackTraceHiddenAttribute>() == null &&
                                frame.HasSource())
                                )
                            {
                                frames.Add(frame);
                            }
                            if (
                                method?.GetCustomAttribute<CallFromHLOnly>() != null)
                            {
                                break;
                            }
                            //Do something to exit
                        }
                        curTransition = curTransition.prev;
                    }
                }
      

                var m = HashlinkVM.Instance.Context->m;

                if (eip < jitStart || eip > jitEnd)
                {
                    goto UNKNOWN_FUNC;
                }

                if (!module_resolve_pos(m, (void*)eip, out var fidx, out var fpos))
                {
                    goto UNKNOWN_FUNC;
                }

                goto HL_JIT;

                UNKNOWN_FUNC:
                if (Core.Config.Value.DetailedStackTrace)
                {
                    continue;
                }

                var modNameLen = 512;
                if (mcn_get_sym((void*)eip,
                    symBuf,
                    out var symNameLen,
                    modNameBuf,
                    ref modNameLen,
                    out var fileName,
                    out var line) &&
                    (fileName != null || line != 0))
                {

                    frames.Add(new NativeStackFrame()
                    {
                        Pointer = eip,
                        FileLine = line,
                        FileName = Marshal.PtrToStringUTF8((nint)fileName),
                        ModuleName = modNameLen > 0 ? Path.GetFileName(new string(modNameBuf)) : null,
                        FuncName = new(symBuf),
                    });

                }


                continue;

                HL_JIT:

                var func = m->code->functions + fidx;
                var debug_addr = func->debug + ((fpos & 0xFFFF) * 2);

                var funcName = GetFunctionName(func);

                frames.Add(new HLStackFrame()
                {
                    Pointer = eip,
                    FuncName = funcName,
                    FileName = Encoding.UTF8.GetString(m->code->debugfiles[debug_addr[0]],
                        m->code->debugfiles_lens[debug_addr[0]]),
                    FileLine = debug_addr[1],
                });
            }
            while (managedId < innerTrace.FrameCount)
            {
                frames.Add(innerTrace.GetFrame(managedId++)!);
            }
        }
        public override string ToString()
        {
            StringBuilder sb = new();
            foreach (var v in frames)
            {
                sb.Append(" at ");
                sb.Append(v.GetDisplayName());
                sb.Append('\n');
            }
            return sb.ToString();
        }
    }
}
