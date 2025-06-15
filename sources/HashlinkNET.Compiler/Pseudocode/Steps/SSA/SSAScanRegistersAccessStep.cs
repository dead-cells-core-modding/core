using HashlinkNET.Compiler.Pseudocode.Data;
using HashlinkNET.Compiler.Pseudocode.IR;
using HashlinkNET.Compiler.Pseudocode.IR.SSA;
using HashlinkNET.Compiler.Steps;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Pseudocode.Steps.SSA
{
    internal class SSAScanRegistersAccessStep : CompileStep
    {
        public override void Execute( IDataContainer container )
        {
            var gdata = container.GetGlobalData<FuncEmitGlobalData>();

            foreach (var v in gdata.IRBasicBlocks)
            {
                var rad = v.registerAccessData = new RegisterAccessData(gdata.Registers.Count);
                rad.ssaRegisters = new SSARegisterData[gdata.Registers.Count];
                for (int i = 0; i < v.flatIR!.Length; i++)
                {
                    var ir = v.flatIR[i];
                    if (ir.IR is IR_LoadLocalReg lcr && lcr.src != null)
                    {
                        var index = lcr.src.Index;

                        var ssaReg = rad.ssaRegisters[index] ??= new()
                        {
                            reg = lcr.src
                        };
                        ir.IR = new IR_SSA_Load(ssaReg);
                        ssaReg.loadAccess.Add(ir);

                        if (!rad.firstWriteReg[index])
                        {
                            rad.firstReadReg[index] = true;
                        }
                        rad.readReg[index] = true;
                        rad.regAccess.Add(i);
                    }
                    else if (ir.IR is IR_SetLocalReg slr && slr.dst != null)
                    {
                        var index = slr.dst.Index;

                        var ssaReg = rad.ssaRegisters[index] = new()
                        {
                            reg = slr.dst
                        };


                        ir.IR = ssaReg.ir_save = new IR_SSA_Save(ssaReg, slr.src, slr.assign);


                        if (!rad.firstReadReg[index])
                        {
                            rad.firstWriteReg[index] = true;
                        }
                        rad.writeReg[index] = true;
                        rad.regAccess.Add(i);
                    }
                }
                rad.requireReg.Or(rad.firstReadReg);
            }

            Queue<IRBasicBlockData> queue = [];

            while (queue.TryDequeue(out var bb))
            {
                var rad = bb.registerAccessData!;
                foreach (var v in bb.parents)
                {
                    var trad = v.registerAccessData!;
                    var old = (BitArray) trad.requireReg.Clone();
                    var req = (BitArray) rad.requireReg.Clone();
                    req.Not()
                        .Xor(trad.writeReg)
                        .Not()
                        .And(rad.requireReg)
                        ;

                    trad.requireReg.Or(req);
                    if (old.Xor(trad.requireReg).HasAnySet()) //Not Equal
                    {
                        queue.Enqueue(v);
                    }
                }
            }

        }
    }
}
