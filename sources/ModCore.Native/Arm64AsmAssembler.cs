namespace ModCore.Native
{
    /// <summary>
    /// AArch64 (ARM64) assembly builder backed by TCC (TinyCC).
    /// Inherits from <see cref="AsmAssembler"/> and overrides instruction methods
    /// with AArch64 GNU as syntax equivalents.
    /// </summary>
    internal class Arm64AsmAssembler : AsmAssembler
    {
        // ── Register name constants (AArch64: no % prefix) ───────────────

        public new static class R
        {
            // ── General-purpose registers (64-bit) ────────────────────────
            public const string x0  = "x0";
            public const string x1  = "x1";
            public const string x2  = "x2";
            public const string x3  = "x3";
            public const string x4  = "x4";
            public const string x5  = "x5";
            public const string x6  = "x6";
            public const string x7  = "x7";
            public const string x8  = "x8";
            public const string x9  = "x9";
            public const string x10 = "x10";
            public const string x11 = "x11";
            public const string x12 = "x12";
            public const string x13 = "x13";
            public const string x14 = "x14";
            public const string x15 = "x15";
            public const string x16 = "x16";
            public const string x17 = "x17";
            public const string x18 = "x18";
            public const string x19 = "x19";
            public const string x20 = "x20";
            public const string x21 = "x21";
            public const string x22 = "x22";
            public const string x23 = "x23";
            public const string x24 = "x24";
            public const string x25 = "x25";
            public const string x26 = "x26";
            public const string x27 = "x27";
            public const string x28 = "x28";
            public const string x29 = "x29";
            public const string x30 = "x30";

            // ── Aliases ───────────────────────────────────────────────────
            public const string sp = "sp";
            public const string fp = "x29";
            public const string lr = "x30";
            public const string xzr = "xzr";

            // ── 32-bit variants (w0–w30) ──────────────────────────────────
            public const string w0  = "w0";
            public const string w1  = "w1";
            public const string w2  = "w2";
            public const string w3  = "w3";
            public const string w4  = "w4";
            public const string w5  = "w5";
            public const string w6  = "w6";
            public const string w7  = "w7";
            public const string w8  = "w8";
            public const string w9  = "w9";
            public const string w10 = "w10";
            public const string w11 = "w11";
            public const string w12 = "w12";
            public const string w13 = "w13";
            public const string w14 = "w14";
            public const string w15 = "w15";
            public const string w16 = "w16";
            public const string w17 = "w17";
            public const string w18 = "w18";
            public const string w19 = "w19";
            public const string w20 = "w20";
            public const string w21 = "w21";
            public const string w22 = "w22";
            public const string w23 = "w23";
            public const string w24 = "w24";
            public const string w25 = "w25";
            public const string w26 = "w26";
            public const string w27 = "w27";
            public const string w28 = "w28";
            public const string w29 = "w29";
            public const string w30 = "w30";

            // ── FP/SIMD registers (64-bit double-precision) ───────────────
            public const string d0  = "d0";
            public const string d1  = "d1";
            public const string d2  = "d2";
            public const string d3  = "d3";
            public const string d4  = "d4";
            public const string d5  = "d5";
            public const string d6  = "d6";
            public const string d7  = "d7";
            public const string d8  = "d8";
            public const string d9  = "d9";
            public const string d10 = "d10";
            public const string d11 = "d11";
            public const string d12 = "d12";
            public const string d13 = "d13";
            public const string d14 = "d14";
            public const string d15 = "d15";
            public const string d16 = "d16";
            public const string d17 = "d17";
            public const string d18 = "d18";
            public const string d19 = "d19";
            public const string d20 = "d20";
            public const string d21 = "d21";
            public const string d22 = "d22";
            public const string d23 = "d23";
            public const string d24 = "d24";
            public const string d25 = "d25";
            public const string d26 = "d26";
            public const string d27 = "d27";
            public const string d28 = "d28";
            public const string d29 = "d29";
            public const string d30 = "d30";
            public const string d31 = "d31";

            // ── SIMD/vector registers (128-bit, alias for Q-form) ─────────
            public const string v0  = "v0";
            public const string v1  = "v1";
            public const string v2  = "v2";
            public const string v3  = "v3";
            public const string v4  = "v4";
            public const string v5  = "v5";
            public const string v6  = "v6";
            public const string v7  = "v7";
            public const string v8  = "v8";
            public const string v9  = "v9";
            public const string v10 = "v10";
            public const string v11 = "v11";
            public const string v12 = "v12";
            public const string v13 = "v13";
            public const string v14 = "v14";
            public const string v15 = "v15";
            public const string v16 = "v16";
            public const string v17 = "v17";
            public const string v18 = "v18";
            public const string v19 = "v19";
            public const string v20 = "v20";
            public const string v21 = "v21";
            public const string v22 = "v22";
            public const string v23 = "v23";
            public const string v24 = "v24";
            public const string v25 = "v25";
            public const string v26 = "v26";
            public const string v27 = "v27";
            public const string v28 = "v28";
            public const string v29 = "v29";
            public const string v30 = "v30";
            public const string v31 = "v31";
        }

        // ── Construction ─────────────────────────────────────────────────

        public Arm64AsmAssembler()
            : base(skipTccInit: false)
        {
        }

        // ── AddLine override (no trailing semicolon for AArch64 asm) ─────

        public override void AddLine(string line)
        {
            lines.Add(line);
        }

        // ── Overridden instruction methods (AArch64 equivalents) ──────────

        /// <summary>mov {dst}, {src} — register-to-register move.</summary>
        public override void mov_rr(string dstReg, string srcReg)
            => AddLine($"mov {dstReg}, {srcReg}");

        /// <summary>ldr {dst}, [{srcBase}, #{off}] — load from memory.</summary>
        public override void mov_mr(string dstReg, string srcBase, int off = 0)
            => AddLine($"ldr {dstReg}, [{srcBase}, #{off}]");

        /// <summary>str {src}, [{dstBase}, #{off}] — store to memory.</summary>
        public override void mov_rm(string srcReg, string dstBase, int off = 0)
            => AddLine($"str {srcReg}, [{dstBase}, #{off}]");

        /// <summary>Load 64-bit immediate via movz + movk sequence.</summary>
        public override void mov_imm(string dstReg, long value)
        {
            var v = (ulong)value;
            ushort h0 = (ushort)(v & 0xFFFF);
            ushort h1 = (ushort)((v >> 16) & 0xFFFF);
            ushort h2 = (ushort)((v >> 32) & 0xFFFF);
            ushort h3 = (ushort)((v >> 48) & 0xFFFF);

            AddLine($"movz {dstReg}, #0x{h0:x4}");

            bool needH1 = h1 != 0 || (h2 != 0 || h3 != 0);
            bool needH2 = h2 != 0 || h3 != 0;
            bool needH3 = h3 != 0;

            if (needH1)
                AddLine($"movk {dstReg}, #0x{h1:x4}, lsl #16");
            if (needH2)
                AddLine($"movk {dstReg}, #0x{h2:x4}, lsl #32");
            if (needH3)
                AddLine($"movk {dstReg}, #0x{h3:x4}, lsl #48");
        }

        /// <summary>stp {reg}, xzr, [sp, #-16]! — push with 16-byte alignment.</summary>
        public override void push(string reg)
            => AddLine($"stp {reg}, xzr, [sp, #-16]!");

        /// <summary>ldp {reg}, xzr, [sp], #16 — pop with 16-byte alignment.</summary>
        public override void pop(string reg)
            => AddLine($"ldp {reg}, xzr, [sp], #16");

        /// <summary>ret — return from subroutine (branches to x30/lr).</summary>
        public override void ret()
            => AddLine("ret");

        /// <summary>blr {reg} — branch with link to register (function call).</summary>
        public override void call_r(string reg)
            => AddLine($"blr {reg}");

        /// <summary>Load function pointer from memory then blr.</summary>
        public override void call_m(string baseReg, int off = 0)
        {
            AddLine($"ldr x16, [{baseReg}, #{off}]");
            AddLine("blr x16");
        }

        /// <summary>b {label} — unconditional branch.</summary>
        public override void jmp_label(string label)
            => AddLine($"b {label}");

        /// <summary>Load target from memory then br.</summary>
        public override void jmp_m(string baseReg, int off = 0)
        {
            AddLine($"ldr x16, [{baseReg}, #{off}]");
            AddLine("br x16");
        }

        /// <summary>add {dst}, {srcBase}, #{off} — compute address.</summary>
        public override void lea(string dstReg, string srcBase, int off)
            => AddLine($"add {dstReg}, {srcBase}, #{off}");

        // ── Arithmetic ──────────────────────────────────────────────────

        /// <summary>add {reg}, {reg}, #{imm} — add immediate.</summary>
        public override void add(string reg, int imm)
            => AddLine($"add {reg}, {reg}, #{imm}");

        /// <summary>sub {reg}, {reg}, #{imm} — subtract immediate.</summary>
        public override void sub(string reg, int imm)
            => AddLine($"sub {reg}, {reg}, #{imm}");

        /// <summary>and {reg}, {reg}, #{imm} — bitwise AND with immediate.</summary>
        public override void and(string reg, int imm)
            => AddLine($"and {reg}, {reg}, #{imm}");

        // ── Comparison ──────────────────────────────────────────────────

        /// <summary>cmp {reg}, #{imm} — compare register with immediate.</summary>
        public override void cmp_ri(string reg, int imm)
            => AddLine($"cmp {reg}, #{imm}");

        /// <summary>cmp {reg1}, {reg2} — compare register with register.</summary>
        public override void cmp_rr(string reg1, string reg2)
            => AddLine($"cmp {reg1}, {reg2}");

        // ── Conditional branches ────────────────────────────────────────

        /// <summary>b.eq {label} — branch if equal (zero flag set).</summary>
        public override void je(string label)
            => AddLine($"b.eq {label}");

        /// <summary>b.lt {label} — branch if less than (signed).</summary>
        public override void jl(string label)
            => AddLine($"b.lt {label}");

        /// <summary>b.ne {label} — branch if not equal (zero flag clear).</summary>
        public override void jne(string label)
            => AddLine($"b.ne {label}");

        // ── Breakpoint ──────────────────────────────────────────────────

        /// <summary>brk #0 — software breakpoint.</summary>
        public override void int3()
            => AddLine("brk #0");

        // ── GS segment (x86-specific, not available on AArch64) ─────────

        /// <summary>Not supported on AArch64 — no GS segment register.</summary>
        public override void mov_gs(string dstReg, int off)
            => throw new NotSupportedException("GS segment access is not available on AArch64.");

        /// <summary>Not supported on AArch64 — no GS segment register.</summary>
        public override void mov_gs_r(string dstReg, string baseReg, int off)
            => throw new NotSupportedException("GS segment access is not available on AArch64.");

        // ── Stack with immediates ───────────────────────────────────────

        /// <summary>Push 32-bit immediate via movz/movk to x16 then stp.</summary>
        public override void push_imm(int value)
        {
            uint v = (uint)value;
            ushort lo = (ushort)(v & 0xFFFF);
            ushort hi = (ushort)((v >> 16) & 0xFFFF);

            AddLine($"movz x16, #0x{lo:x4}");
            if (hi != 0)
                AddLine($"movk x16, #0x{hi:x4}, lsl #16");
            AddLine("stp x16, xzr, [sp, #-16]!");
        }

        /// <summary>Load from memory then push to stack.</summary>
        public override void push_m(string baseReg, int off = 0)
        {
            AddLine($"ldr x16, [{baseReg}, #{off}]");
            AddLine("stp x16, xzr, [sp, #-16]!");
        }

        // ── FPU / SSE (x86-specific — mapped or unsupported) ────────────

        /// <summary>Not supported on AArch64 — x86 MXCSR control.</summary>
        public override void ldmxcsr(string baseReg, int off)
            => throw new NotSupportedException("ldmxcsr is not available on AArch64.");

        /// <summary>Not supported on AArch64 — x86 FPU control word.</summary>
        public override void fldcw(string baseReg, int off)
            => throw new NotSupportedException("fldcw is not available on AArch64.");

        /// <summary>ldr {dreg}, [{baseReg}, #{off}] — load double into FP/SIMD register.</summary>
        public override void movsd_rm(string xmmReg, string baseReg, int off)
        {
            // Convert AT&T %xmmN → AArch64 dN
            var dreg = xmmReg.Replace("%xmm", "d");
            AddLine($"ldr {dreg}, [{baseReg}, #{off}]");
        }

        // ── AArch64-specific helpers (not overrides) ─────────────────────

        /// <summary>mov {dst}, {src} — register-to-register move (convenience method).</summary>
        public void mov(string src, string dst)
            => AddLine($"mov {dst}, {src}");

        /// <summary>movz {reg}, #{imm}, lsl #{shift} — move wide with zero; shift in {0, 16, 32, 48}.</summary>
        public void movz(string reg, ushort imm, int shift = 0)
            => AddLine($"movz {reg}, #{imm}, lsl #{shift}");

        /// <summary>movk {reg}, #{imm}, lsl #{shift} — move wide with keep; shift in {0, 16, 32, 48}.</summary>
        public void movk(string reg, ushort imm, int shift = 0)
            => AddLine($"movk {reg}, #{imm}, lsl #{shift}");

        /// <summary>add {dst}, {a}, {b} — register + register.</summary>
        public void add(string dst, string a, string b)
            => AddLine($"add {dst}, {a}, {b}");

        /// <summary>sub {dst}, {a}, {b} — register - register.</summary>
        public void sub(string dst, string a, string b)
            => AddLine($"sub {dst}, {a}, {b}");

        /// <summary>ldr {dst}, [{base}, #{offset}] — load 64-bit from memory.</summary>
        public void ldr(string dst, string @base, int offset = 0)
            => AddLine($"ldr {dst}, [{@base}, #{offset}]");

        /// <summary>str {src}, [{base}, #{offset}] — store 64-bit to memory.</summary>
        public void str(string src, string @base, int offset = 0)
            => AddLine($"str {src}, [{@base}, #{offset}]");

        /// <summary>ldp {r1}, {r2}, [{base}, #{offset}] — load pair of 64-bit registers.</summary>
        public void ldp(string r1, string r2, string @base, int offset = 0)
            => AddLine($"ldp {r1}, {r2}, [{@base}, #{offset}]");

        /// <summary>stp {r1}, {r2}, [{base}, #{offset}]! — store pair with pre-index write-back.</summary>
        public void stp(string r1, string r2, string @base, int offset, int nbytes)
            => AddLine($"stp {r1}, {r2}, [{@base}, #{offset}]!");

        /// <summary>blr {reg} — branch with link to register (function call).</summary>
        public void blr(string reg)
            => AddLine($"blr {reg}");

        /// <summary>br {reg} — unconditional branch to register (tail call / jump).</summary>
        public void br(string reg)
            => AddLine($"br {reg}");

        /// <summary>cbnz {reg}, {label} — compare and branch if non-zero.</summary>
        public void cbnz(string reg, string label)
            => AddLine($"cbnz {reg}, {label}");

        /// <summary>Define a named label at the current position.</summary>
        public void define_label(string name)
            => AddLine($"{name}:");

        /// <summary>Return the label name for use as an operand in branch/call instructions.</summary>
        public static string reference_label(string name)
            => name;
    }
}
