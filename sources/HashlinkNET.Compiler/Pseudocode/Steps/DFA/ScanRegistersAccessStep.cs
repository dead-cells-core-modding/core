using HashlinkNET.Compiler.Pseudocode.Data;
using HashlinkNET.Compiler.Pseudocode.Data.DFA;
using HashlinkNET.Compiler.Pseudocode.IR;
using HashlinkNET.Compiler.Steps;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Pseudocode.Steps.DFA
{
    internal class ScanRegistersAccessStep : CompileStep
    {
        public override void Execute( IDataContainer container )
        {
            var gdata = container.GetGlobalData<FuncEmitGlobalData>();

            Queue<IRBasicBlockData> queue = [];
            foreach (var v in gdata.IRBasicBlocks)
            {
                queue.Enqueue( v );

                var rad = v.registerAccessData = new RegisterAccessData(gdata.Registers.Count);
                rad.ssaRegisters = new SSARegisterData[gdata.Registers.Count];
                for (var i = 0; i < v.flatIR!.Length; i++)
                {
                    var ir = v.flatIR[i];
                    if (ir.IR is IR_LoadLocalReg lcr && lcr.src != null)
                    {
                        var index = lcr.src.Index;

                        var ssaReg = rad.ssaRegisters[index] ??= new()
                        {
                            reg = lcr.src
                        };

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

                        rad.ssaRegisters[index] = new()
                        {
                            reg = slr.dst
                        };


                        if (!rad.firstReadReg[index])
                        {
                            rad.firstWriteReg[index] = true;
                        }
                        rad.writeReg[index] = true;
                        rad.regAccess.Add(i);
                    }
                }
                rad.requireBySelfAndChildrenReg.Or(rad.firstReadReg);
            }

            

            while (queue.TryDequeue(out var bb))
            {
                var rad = bb.registerAccessData!;
                foreach (var v in bb.parents)
                {
                    var trad = v.registerAccessData!;
                    var old = (BitArray) trad.requireBySelfAndChildrenReg.Clone();
                    var req = (BitArray) rad.requireBySelfAndChildrenReg.Clone();
                    trad.exposedReg.Or(req);
                    req.And(trad.writeReg.Not());
                    trad.writeReg.Not();

                    trad.requireBySelfAndChildrenReg.Or(req);
                    if (old.Xor(trad.requireBySelfAndChildrenReg).HasAnySet()) //Not Equal
                    {
                        queue.Enqueue(v);
                    }
                }
            }

            foreach (var v in gdata.IRBasicBlocks)
            {
                var rad = v.registerAccessData!;
                foreach (var sr in rad.ssaRegisters)
                {
                    if (sr != null &&
                        sr.reg != null)
                    {
                        sr.isLast = true;
                        sr.crossBB = rad.exposedReg[sr.reg.Index];
                    }
                }
            }
        }
    }
}
