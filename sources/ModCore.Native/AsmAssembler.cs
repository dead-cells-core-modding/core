using ModCore.Storage;
using System.Text;

namespace ModCore.Native
{
    /// <summary>
    /// AT&T-syntax x86-64 assembly builder backed by TCC (TinyCC).
    /// Accumulates asm lines and compiles them into executable memory via <see cref="TCCCompiler"/>.
    /// </summary>
    internal class AsmAssembler : IDisposable
    {
        protected readonly TCCCompiler compiler = new();

        public TCCCompiler Compiler => compiler;

        protected readonly List<string> lines = [];
        protected bool compiled;
        protected int _labelCounter;
        protected string? _pendingForwardLabel;
        protected int _anonForwardCounter;

        // ── Register name constants (AT&T syntax: %prefix) ──────────────

        public static class R
        {
            public const string rax = "%rax";
            public const string rcx = "%rcx";
            public const string rdx = "%rdx";
            public const string rbx = "%rbx";
            public const string rsp = "%rsp";
            public const string rbp = "%rbp";
            public const string rsi = "%rsi";
            public const string rdi = "%rdi";
            public const string r8  = "%r8";
            public const string r9  = "%r9";
            public const string r10 = "%r10";
            public const string r11 = "%r11";
            public const string r12 = "%r12";
            public const string r13 = "%r13";
            public const string r14 = "%r14";
            public const string r15 = "%r15";

            public const string xmm0  = "%xmm0";
            public const string xmm1  = "%xmm1";
            public const string xmm2  = "%xmm2";
            public const string xmm3  = "%xmm3";
            public const string xmm4  = "%xmm4";
            public const string xmm5  = "%xmm5";
            public const string xmm6  = "%xmm6";
            public const string xmm7  = "%xmm7";
            public const string xmm8  = "%xmm8";
            public const string xmm9  = "%xmm9";
            public const string xmm10 = "%xmm10";
            public const string xmm11 = "%xmm11";
            public const string xmm12 = "%xmm12";
            public const string xmm13 = "%xmm13";
            public const string xmm14 = "%xmm14";
            public const string xmm15 = "%xmm15";
        }

        // ── Symbol definition ───────────────────────────────────────────

        public void DefineGlobalSymbol(string name)
        {
            lines.Add("#$#" + name);
        }

        /// <summary>Append a raw AT&T-syntax asm line (without trailing semicolon — added automatically).</summary>
        public virtual void AddLine(string line)
        {
            lines.Add(line + ";");
        }

        /// <summary>Alias for <see cref="AddLine"/>.</summary>
        public void L(string line) => AddLine(line);

        // ── Label management ────────────────────────────────────────────

        /// <summary>Create a unique named label. Call <see cref="Label"/> to place it.</summary>
        public string CreateLabel() => $".L{_labelCounter++}";

        /// <summary>Place a named label at the current position.</summary>
        public void Label(ref string name) => AddLine($"{name}:");

        /// <summary>
        /// Returns the pending forward anonymous label.
        /// All calls to <see cref="F"/> between two <see cref="AnonymousLabel"/> calls
        /// return the same label — they all jump forward to the same target.
        /// </summary>
        public string F => _pendingForwardLabel ??= $".L{_labelCounter++}_F{_anonForwardCounter++}";

        /// <summary>
        /// Place the pending forward anonymous label (the target for preceding <see cref="F"/> jumps).
        /// </summary>
        public void AnonymousLabel()
        {
            if (_pendingForwardLabel != null)
            {
                AddLine($"{_pendingForwardLabel}:");
                _pendingForwardLabel = null;
            }
        }

        // ── Conditional jumps ───────────────────────────────────────────

        public virtual void je(string label)  => AddLine($"je {label}");
        public virtual void jl(string label)  => AddLine($"jl {label}");
        public virtual void jne(string label) => AddLine($"jne {label}");

        // ── Unconditional jump ──────────────────────────────────────────

        public virtual void jmp_label(string label) => AddLine($"jmp {label}");

        /// <summary>jmp *off(%baseReg)</summary>
        public virtual void jmp_m(string baseReg, int off = 0) => AddLine($"jmp *{off}({baseReg})");

        // ── Call / Return ───────────────────────────────────────────────

        /// <summary>call *%reg</summary>
        public virtual void call_r(string reg) => AddLine($"call *{reg}");

        /// <summary>call *off(%baseReg)</summary>
        public virtual void call_m(string baseReg, int off = 0) => AddLine($"call *{off}({baseReg})");

        public virtual void ret() => AddLine("ret");

        // ── Data movement ───────────────────────────────────────────────

        /// <summary>movq %srcReg, %dstReg</summary>
        public virtual void mov_rr(string dstReg, string srcReg) => AddLine($"movq {srcReg}, {dstReg}");

        /// <summary>movq off(%srcBase), %dstReg  (load from memory)</summary>
        public virtual void mov_mr(string dstReg, string srcBase, int off = 0) => AddLine($"movq {off}({srcBase}), {dstReg}");

        /// <summary>movq %srcReg, off(%dstBase)  (store to memory)</summary>
        public virtual void mov_rm(string srcReg, string dstBase, int off = 0) => AddLine($"movq {srcReg}, {off}({dstBase})");

        /// <summary>movq $imm64, %dstReg  (load 64-bit immediate)</summary>
        public virtual void mov_imm(string dstReg, long value) => AddLine($"movq ${value}, {dstReg}");

        /// <summary>movq %gs:off, %dstReg  (Windows TLS via GS segment)</summary>
        public virtual void mov_gs(string dstReg, int off) => AddLine($"movq %gs:{off}, {dstReg}");

        /// <summary>movq %gs:off(%baseReg), %dstReg  (indirect GS)</summary>
        public virtual void mov_gs_r(string dstReg, string baseReg, int off) => AddLine($"movq %gs:{off}({baseReg}), {dstReg}");

        // ── Stack ───────────────────────────────────────────────────────

        public virtual void push(string reg) => AddLine($"push {reg}");
        public virtual void pop(string reg)  => AddLine($"pop {reg}");
        public virtual void push_imm(int value) => AddLine($"push ${value}");
        public virtual void push_m(string baseReg, int off = 0) => AddLine($"push {off}({baseReg})");

        // ── Address computation ─────────────────────────────────────────

        /// <summary>lea off(%srcBase), %dstReg</summary>
        public virtual void lea(string dstReg, string srcBase, int off) => AddLine($"lea {off}({srcBase}), {dstReg}");

        // ── Arithmetic ──────────────────────────────────────────────────

        public virtual void add(string reg, int imm) => AddLine($"add ${imm}, {reg}");
        public virtual void sub(string reg, int imm) => AddLine($"sub ${imm}, {reg}");
        public virtual void and(string reg, int imm) => AddLine($"and ${imm}, {reg}");

        // ── Comparison ──────────────────────────────────────────────────

        /// <summary>cmp $imm, %reg</summary>
        public virtual void cmp_ri(string reg, int imm) => AddLine($"cmp ${imm}, {reg}");

        /// <summary>cmp %reg2, %reg1</summary>
        public virtual void cmp_rr(string reg1, string reg2) => AddLine($"cmp {reg2}, {reg1}");

        // ── Breakpoint ──────────────────────────────────────────────────

        public virtual void int3() => AddLine("int3");

        // ── FPU / SSE ───────────────────────────────────────────────────

        /// <summary>ldmxcsr off(%baseReg)</summary>
        public virtual void ldmxcsr(string baseReg, int off) => AddLine($"ldmxcsr {off}({baseReg})");

        /// <summary>fldcw off(%baseReg)</summary>
        public virtual void fldcw(string baseReg, int off) => AddLine($"fldcw {off}({baseReg})");

        /// <summary>movq off(%baseReg), %xmmReg  (64-bit load to low XMM, zero-extends — TCC-compatible replacement for movsd)</summary>
        public virtual void movsd_rm(string xmmReg, string baseReg, int off) => AddLine($"movsd {off}({baseReg}), {xmmReg}");

        // ── Compilation ─────────────────────────────────────────────────

        public AsmAssembler() : this(false)
        {
        }

        protected AsmAssembler(bool skipTccInit)
        {
            if (!skipTccInit)
            {
                compiler.AddOptions("-nostdlib");
                compiler.SetOutputType(TCCCompiler.OutputType.MEMORY);

                var incRoot = Path.GetFullPath(Path.Combine(FolderInfo.CurrentNativeRoot.FullPath, "tinycc"));
                compiler.AddIncludePath(incRoot, true);
                compiler.AddLibraryPath(incRoot);
            }
        }

        public nint GetSymbol(string name)
        {
            if (!compiled)
                throw new InvalidOperationException($"{GetType().Name}: must call Compile() before GetSymbol().");
            return Compiler.GetSymbol(name);
        }

        public void Compile()
        {
            if (compiled)
                throw new InvalidOperationException($"{GetType().Name}: already compiled.");

            compiled = true;

            var decl = new StringBuilder();
            var asm = new StringBuilder();

            asm.AppendLine(".text;");

            foreach (var v in lines)
            {
                if (v.StartsWith("#$#"))
                {
                    var name = v[3..];
                    asm.AppendLine($"{name}:");
                    decl.AppendLine($"void {name}();");
                    continue;
                }
                asm.AppendLine(v);
            }

            var sb = new StringBuilder();
            sb.AppendLine(decl.ToString());
            sb.AppendLine($"asm({ToLiteral(asm.ToString())});");

            if (compiler.AddString(sb.ToString()) == -1)
                throw new InvalidOperationException($"{GetType().Name}: TCC failed to compile asm.");

            if (compiler.Relocate() < 0)
                throw new InvalidOperationException($"{GetType().Name}: TCC failed to relocate asm.");
        }

        public void Dispose()
        {
            ((IDisposable)compiler).Dispose();
        }

        // ── String literal escaping for C inline asm ────────────────────

        private static string ToLiteral(string input)
        {
            var sb = new StringBuilder(input.Length + 2);
            sb.Append('"');
            foreach (char c in input)
            {
                switch (c)
                {
                    case '\\': sb.Append(@"\\"); break;
                    case '\"': sb.Append("\\\""); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\0': sb.Append("\\0"); break;
                    case '\a': sb.Append("\\a"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\v': sb.Append("\\v"); break;
                    default:
                        if (c < 0x20)
                            sb.Append($"\\u{(int)c:x4}");
                        else
                            sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }
    }
}
