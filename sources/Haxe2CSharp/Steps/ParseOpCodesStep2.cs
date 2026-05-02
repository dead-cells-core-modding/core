using HashlinkNET.Bytecode;
using HashlinkNET.Bytecode.OpCodeParser;
using HashlinkNET.Compiler;
using HashlinkNET.Compiler.Data;
using HashlinkNET.Compiler.Pseudocode.Data;
using HashlinkNET.Compiler.Pseudocode.IR;
using HashlinkNET.Compiler.Pseudocode.Steps;
using HashlinkNET.Compiler.Steps;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Text;

namespace Haxe2CSharp.Steps
{
    internal class ParseOpCodesStep2 : ParseOpCodesStep
    {

        protected override void ParseOpCode( HlOpcode code, int index, IRBasicBlockData irbb )
        {
            var hlc = gdata2.Code;
            var c = code.Kind;
            var op = HlOpCodes.OpCodes[(int)code.Kind];
            var opc = op.OpCode;

            if (
                (opc >= HlOpcodeKind.Mov && opc <= HlOpcodeKind.Null) ||
                (opc >= HlOpcodeKind.Add && opc <= HlOpcodeKind.Xor) ||
                (opc >= HlOpcodeKind.Neg && opc <= HlOpcodeKind.Decr) ||
                (opc >= HlOpcodeKind.ToDyn && opc <= HlOpcodeKind.ToVirtual) ||
                (opc == HlOpcodeKind.UnsafeCast) ||
                (opc == HlOpcodeKind.GetMem || opc == HlOpcodeKind.GetI8 || opc == HlOpcodeKind.GetI16) ||
                (opc == HlOpcodeKind.SetMem || opc == HlOpcodeKind.SetI8 || opc == HlOpcodeKind.SetI16) ||
                (opc == HlOpcodeKind.Ret) ||
                (opc >= HlOpcodeKind.JTrue && opc <= HlOpcodeKind.JAlways) ||
                (opc >= HlOpcodeKind.Ref && opc <= HlOpcodeKind.Setref) ||
                (opc == HlOpcodeKind.Switch)
                )
            {

                base.ParseOpCode(code, index, irbb);
            }
        }
    }
}
