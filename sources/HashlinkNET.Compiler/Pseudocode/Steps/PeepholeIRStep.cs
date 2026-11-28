using HashlinkNET.Compiler.Pseudocode.Data;
using HashlinkNET.Compiler.Pseudocode.IR;
using HashlinkNET.Compiler.Pseudocode.IR.Object;
using HashlinkNET.Compiler.Pseudocode.IR.Ref;
using HashlinkNET.Compiler.Steps;
using HashlinkNET.Compiler.Utils;
using Mono.Cecil;

namespace HashlinkNET.Compiler.Pseudocode.Steps
{
    /// <summary>
    /// Dataflow cleanup that makes the emitted code more readable:
    /// <list type="bullet">
    /// <item>intra-block copy propagation of register moves (<c>dst = src</c>),</item>
    /// <item>removal of self-moves (<c>r = r</c>),</item>
    /// <item>dead-store elimination driven by whole-function liveness over the
    /// basic-block graph.</item>
    /// </list>
    /// Registers whose address is taken are never optimized because they can be
    /// observed or modified through the reference.
    /// </summary>
    internal class PeepholeIRStep : CompileStep
    {
        public override void Execute( IDataContainer container )
        {
            var gdata = container.GetGlobalData<FuncEmitGlobalData>();
            var blocks = gdata.IRBasicBlocks;
            if (blocks.Count == 0 || gdata.Registers.Count == 0)
            {
                return;
            }

            var exposed = new HashSet<HlFuncRegisterData>(ReferenceEqualityComparer.Instance);
            foreach (var bb in blocks)
            {
                foreach (var stmt in bb.ir)
                {
                    CollectExposed(stmt, exposed);
                }
            }

            var aliases = new Dictionary<HlFuncRegisterData, HlFuncRegisterData>(
                ReferenceEqualityComparer.Instance);
            var constants = new Dictionary<HlFuncRegisterData, IRResult>(
                ReferenceEqualityComparer.Instance);
            foreach (var bb in blocks)
            {
                aliases.Clear();
                constants.Clear();
                PropagateCopies(bb, aliases, constants, exposed);
            }

            foreach (var bb in blocks)
            {
                RemoveSelfMoves(bb);
            }

            // Exception-handler edges are not part of the normal CFG, so
            // whole-function analysis would be unsound for functions with
            // traps. Keep the local (per-block) cleanups but skip the global
            // passes.
            if (gdata.TrapRegions.Count == 0)
            {
                EliminateDeadStores(gdata, blocks, exposed);
                InlineFieldChains(blocks, exposed);
            }

            // Block-local scheduling: move pure assignments closer to their
            // first use to shorten live ranges. Only simple loads/constants
            // (which cannot throw) are reordered, so this is safe even inside
            // protected regions.
            foreach (var bb in blocks)
            {
                ReorderPureAssignments(bb, exposed);
            }
        }

        private static void ReorderPureAssignments( IRBasicBlockData bb,
            HashSet<HlFuncRegisterData> exposed )
        {
            var list = bb.ir;
            if (list.Count < 2 || list.Count > 2048)
            {
                return;
            }

            var changed = true;
            var guard = 0;
            while (changed && guard++ < 64)
            {
                changed = false;
                for (var i = 0; i + 1 < list.Count; i++)
                {
                    if (!TryGetSimpleAssign(list[i], out var dst, out var source) ||
                        exposed.Contains(dst))
                    {
                        continue;
                    }
                    if (!TryGetSimpleAssign(list[i + 1], out var nextDst, out var nextSource) ||
                        exposed.Contains(nextDst) ||
                        ReferenceEquals(nextDst, dst))
                    {
                        continue;
                    }
                    // The following statement must not consume the moved value,
                    // and must not invalidate the value's source register.
                    if ((nextSource is not null && ReferenceEquals(nextSource, dst)) ||
                        (source is not null && ReferenceEquals(nextDst, source)))
                    {
                        continue;
                    }

                    (list[i], list[i + 1]) = (list[i + 1], list[i]);
                    changed = true;
                }
            }
        }

        private static bool TryGetSimpleAssign( IRResult stmt,
            out HlFuncRegisterData dst, out HlFuncRegisterData? source )
        {
            if (stmt.IR is IR_SetLocalReg set && set.dst is not null)
            {
                if (set.src.IR is IR_LoadLocalReg load && load.src is not null)
                {
                    dst = set.dst;
                    source = load.src;
                    return true;
                }
                if (set.src.IR is IR_LoadConst)
                {
                    dst = set.dst;
                    source = null;
                    return true;
                }
            }
            dst = null!;
            source = null;
            return false;
        }

        private static void PropagateCopies( IRBasicBlockData bb,
            Dictionary<HlFuncRegisterData, HlFuncRegisterData> aliases,
            Dictionary<HlFuncRegisterData, IRResult> constants,
            HashSet<HlFuncRegisterData> exposed )
        {
            var list = bb.ir;
            for (var i = 0; i < list.Count; i++)
            {
                var stmt = list[i];
                if (stmt.IR is null)
                {
                    continue;
                }

                RewriteLoads(stmt, aliases, constants, exposed);

                if (stmt.IR is not IR_SetLocalReg set || set.dst is null)
                {
                    continue;
                }

                var dst = set.dst;

                // The destination now holds a new value: every alias/constant
                // that refers to it is stale.
                Invalidate(aliases, dst);
                constants.Remove(dst);

                if (exposed.Contains(dst))
                {
                    continue;
                }

                if (set.src.IR is IR_LoadLocalReg load &&
                    load.src is not null &&
                    !ReferenceEquals(load.src, dst) &&
                    !exposed.Contains(load.src) &&
                    dst.RegisterType.IsSameType(load.src.RegisterType))
                {
                    aliases[dst] = Resolve(aliases, load.src);
                }
                else if (set.src.IR is IR_LoadConst constant &&
                    IsPropagatableConstant(constant, dst.RegisterType))
                {
                    constants[dst] = set.src;
                }
                else if (set.src.IR is IR_Cast cast &&
                    cast.type.IsSameType(dst.RegisterType) &&
                    cast.type.IsNullable(out var nullableUnderlying) &&
                    cast.src.IR is IR_LoadConst inner &&
                    !cast.src.HasSideEffects &&
                    MatchesScalar(inner.value, nullableUnderlying))
                {
                    // T -> Nullable<T> constant: propagate the whole cast so the
                    // temporary local can be removed.
                    constants[dst] = set.src;
                }
            }
        }

        private static bool MatchesScalar( object? value, TypeReference type )
        {
            return value switch
            {
                int => type.FullName == "System.Int32",
                long => type.FullName == "System.Int64",
                float => type.FullName == "System.Single",
                double => type.FullName == "System.Double",
                bool => type.FullName == "System.Boolean",
                _ => false
            };
        }

        private static bool IsPropagatableConstant( IR_LoadConst constant, TypeReference registerType )
        {
            if (constant.value is null)
            {
                // A null constant can be propagated when it was created with the
                // same declared type as the register (references, nullable value
                // types and raw pointers are all covered).
                return constant.targetType is not null &&
                    constant.targetType.IsSameType(registerType);
            }
            return MatchesScalar(constant.value, registerType);
        }

        private static void RemoveSelfMoves( IRBasicBlockData bb )
        {
            foreach (var stmt in bb.ir)
            {
                if (stmt.IR is IR_SetLocalReg set &&
                    set.dst is not null &&
                    set.src.IR is IR_LoadLocalReg load &&
                    ReferenceEquals(load.src, set.dst))
                {
                    stmt.IR = null;
                }
            }
        }

        private sealed class UseSite
        {
            public required IRResult Wrapper;
            public int Block;
            public int Index;
        }

        /// <summary>
        /// Inlines adjacent, single-use, pure field reads so that a chain such
        /// as <c>r1 = obj.a; r2 = r1.b; r3 = r2.c</c> becomes a single nested
        /// access instead of a temporary per step.
        /// </summary>
        private static void InlineFieldChains( List<IRBasicBlockData> blocks,
            HashSet<HlFuncRegisterData> exposed )
        {
            var useCount = new Dictionary<HlFuncRegisterData, int>(
                ReferenceEqualityComparer.Instance);
            var useSite = new Dictionary<HlFuncRegisterData, UseSite>(
                ReferenceEqualityComparer.Instance);

            for (var b = 0; b < blocks.Count; b++)
            {
                var list = blocks[b].ir;
                for (var s = 0; s < list.Count; s++)
                {
                    CollectUses(list[s], b, s, useCount, useSite);
                }
            }

            for (var b = 0; b < blocks.Count; b++)
            {
                var list = blocks[b].ir;
                for (var s = 0; s + 1 < list.Count; s++)
                {
                    if (list[s].IR is not IR_SetLocalReg set ||
                        set.dst is null ||
                        exposed.Contains(set.dst) ||
                        set.src.IR is not IR_GetField field ||
                        set.src.HasSideEffects ||
                        ContainsRef(set.src))
                    {
                        continue;
                    }

                    var dst = set.dst;
                    if (!useCount.TryGetValue(dst, out var count) ||
                        count != 1 ||
                        !useSite.TryGetValue(dst, out var site) ||
                        site.Block != b ||
                        site.Index != s + 1 ||
                        site.Wrapper.IR is not IR_LoadLocalReg use ||
                        !ReferenceEquals(use.src, dst))
                    {
                        continue;
                    }

                    // The store inserted no cast only when the field type is the
                    // register type; otherwise inlining would drop the cast.
                    if (!field.field.PropertyType.IsSameType(dst.RegisterType))
                    {
                        continue;
                    }

                    // The consumer must be free of side effects so that
                    // deferring the field read to it cannot observe mutation.
                    var next = list[s + 1].IR;
                    if (next is null)
                    {
                        continue;
                    }
                    if (next is IR_SetLocalReg nextSet)
                    {
                        if (nextSet.src.HasSideEffects || ContainsRef(nextSet.src))
                        {
                            continue;
                        }
                    }
                    else if (next.HasSideEffects || ContainsRef(next))
                    {
                        continue;
                    }

                    site.Wrapper.IR = set.src.IR;
                    list[s].IR = null;
                }
            }
        }

        private static void CollectUses( IRResult? result, int block, int index,
            Dictionary<HlFuncRegisterData, int> useCount,
            Dictionary<HlFuncRegisterData, UseSite> useSite )
        {
            if (result?.IR is not IRBase ir)
            {
                return;
            }
            if (ir is IR_LoadLocalReg load)
            {
                if (load.src is not null)
                {
                    var count = useCount.GetValueOrDefault(load.src) + 1;
                    useCount[load.src] = count;
                    if (count == 1)
                    {
                        useSite[load.src] = new UseSite
                        {
                            Wrapper = result,
                            Block = block,
                            Index = index
                        };
                    }
                    else
                    {
                        useSite.Remove(load.src);
                    }
                }
                return;
            }
            if (ir is IR_Ref)
            {
                return;
            }
            foreach (var value in ir.Values)
            {
                CollectUses(value, block, index, useCount, useSite);
            }
        }

        private static bool ContainsRef( IRResult? result )
        {
            if (result?.IR is not IRBase ir)
            {
                return false;
            }
            if (ir is IR_Ref)
            {
                return true;
            }
            foreach (var value in ir.Values)
            {
                if (ContainsRef(value))
                {
                    return true;
                }
            }
            return false;
        }

        private static void EliminateDeadStores( FuncEmitGlobalData gdata,
            List<IRBasicBlockData> blocks, HashSet<HlFuncRegisterData> exposed )
        {
            var regCount = gdata.Registers.Count;
            var n = blocks.Count;

            var blockIndex = new Dictionary<IRBasicBlockData, int>(n, ReferenceEqualityComparer.Instance);
            for (var i = 0; i < n; i++)
            {
                blockIndex[blocks[i]] = i;
            }

            var use = new BitSet[n];
            var def = new BitSet[n];
            var liveIn = new BitSet[n];
            var liveOut = new BitSet[n];
            var preds = new List<int>[n];

            for (var i = 0; i < n; i++)
            {
                use[i] = new BitSet(regCount);
                def[i] = new BitSet(regCount);
                liveIn[i] = new BitSet(regCount);
                liveOut[i] = new BitSet(regCount);
                preds[i] = [];
            }

            for (var i = 0; i < n; i++)
            {
                var bb = blocks[i];
                foreach (var stmt in bb.ir)
                {
                    CollectUseDef(stmt, use[i], def[i]);
                }
                foreach (var transition in bb.transitions)
                {
                    if (blockIndex.TryGetValue(transition.Target, out var target))
                    {
                        preds[target].Add(i);
                    }
                }
            }

            // Iterative liveness (worklist). liveIn = use | (liveOut & ~def).
            var queue = new Queue<int>(n);
            var inQueue = new bool[n];
            for (var i = 0; i < n; i++)
            {
                queue.Enqueue(i);
                inQueue[i] = true;
            }
            var scratch = new BitSet(regCount);
            while (queue.Count > 0)
            {
                var i = queue.Dequeue();
                inQueue[i] = false;

                var @out = liveOut[i];
                @out.Clear();
                foreach (var transition in blocks[i].transitions)
                {
                    if (blockIndex.TryGetValue(transition.Target, out var target))
                    {
                        @out.Or(liveIn[target]);
                    }
                }

                scratch.CopyFrom(@out);
                scratch.AndNot(def[i]);
                scratch.Or(use[i]);

                if (!liveIn[i].SetEquals(scratch))
                {
                    liveIn[i].CopyFrom(scratch);
                    foreach (var p in preds[i])
                    {
                        if (!inQueue[p])
                        {
                            inQueue[p] = true;
                            queue.Enqueue(p);
                        }
                    }
                }
            }

            // Publish live-out sets for later IL-level optimizations.
            var liveIndices = new List<int>();
            for (var i = 0; i < n; i++)
            {
                liveIndices.Clear();
                liveOut[i].CollectInto(liveIndices);
                var liveSet = new HashSet<HlFuncRegisterData>(
                    ReferenceEqualityComparer.Instance);
                foreach (var index in liveIndices)
                {
                    if ((uint)index < (uint)gdata.Registers.Count)
                    {
                        var reg = gdata.Registers[index];
                        if (reg is not null)
                        {
                            liveSet.Add(reg);
                        }
                    }
                }
                blocks[i].liveOutRegs = liveSet;
            }

            // Backward pass over each block, removing stores that are dead at
            // their program point under the whole-function liveness.
            var live = new BitSet(regCount);
            foreach (var bb in blocks)
            {
                live.CopyFrom(liveOut[blockIndex[bb]]);
                var list = bb.ir;
                for (var i = list.Count - 1; i >= 0; i--)
                {
                    var ir = list[i].IR;
                    if (ir is null)
                    {
                        continue;
                    }

                    if (ir is IR_SetLocalReg set)
                    {
                        if (set.dst is not null &&
                            !exposed.Contains(set.dst) &&
                            !live.Get(set.dst.Index) &&
                            !set.src.HasSideEffects)
                        {
                            list[i].IR = null;
                        }

                        if (set.dst is not null)
                        {
                            live.Clear(set.dst.Index);
                        }
                        AddReads(set.src, live);
                    }
                    else
                    {
                        AddReads(ir, live);
                    }
                }
            }
        }

        private static void CollectUseDef( IRResult? result, BitSet use, BitSet def )
        {
            if (result?.IR is not IRBase ir)
            {
                return;
            }
            if (ir is IR_LoadLocalReg load)
            {
                if (load.src is not null && !def.Get(load.src.Index))
                {
                    use.Set(load.src.Index);
                }
                return;
            }
            if (ir is IR_Ref rf)
            {
                if (rf.reg is not null && !def.Get(rf.reg.Index))
                {
                    use.Set(rf.reg.Index);
                }
                return;
            }
            if (ir is IR_SetLocalReg set)
            {
                // Reads in the value happen before the write.
                CollectUseDef(set.src, use, def);
                if (set.dst is not null)
                {
                    def.Set(set.dst.Index);
                }
                return;
            }
            foreach (var value in ir.Values)
            {
                CollectUseDef(value, use, def);
            }
        }

        private static void AddReads( IRResult? result, BitSet live )
        {
            if (result?.IR is not IRBase ir)
            {
                return;
            }
            if (ir is IR_LoadLocalReg load)
            {
                if (load.src is not null)
                {
                    live.Set(load.src.Index);
                }
                return;
            }
            if (ir is IR_Ref rf)
            {
                if (rf.reg is not null)
                {
                    live.Set(rf.reg.Index);
                }
                return;
            }
            foreach (var value in ir.Values)
            {
                AddReads(value, live);
            }
        }

        private static void RewriteLoads( IRResult? result,
            Dictionary<HlFuncRegisterData, HlFuncRegisterData> aliases,
            Dictionary<HlFuncRegisterData, IRResult> constants,
            HashSet<HlFuncRegisterData> exposed )
        {
            if (result?.IR is not IRBase ir)
            {
                return;
            }
            if (ir is IR_LoadLocalReg load)
            {
                if (load.src is not null && !exposed.Contains(load.src))
                {
                    if (constants.TryGetValue(load.src, out var constant) &&
                        constant.IR is not null)
                    {
                        result.IR = constant.IR;
                        return;
                    }
                    var resolved = Resolve(aliases, load.src);
                    if (!ReferenceEquals(resolved, load.src))
                    {
                        load.src = resolved;
                    }
                }
                return;
            }
            if (ir is IR_Ref)
            {
                return;
            }
            foreach (var value in ir.Values)
            {
                RewriteLoads(value, aliases, constants, exposed);
            }
        }

        private static void CollectExposed( IRResult? result,
            HashSet<HlFuncRegisterData> exposed )
        {
            if (result?.IR is not IRBase ir)
            {
                return;
            }
            if (ir is IR_Ref rf)
            {
                if (rf.reg is not null)
                {
                    exposed.Add(rf.reg);
                }
                return;
            }
            foreach (var value in ir.Values)
            {
                CollectExposed(value, exposed);
            }
        }

        private static void Invalidate(
            Dictionary<HlFuncRegisterData, HlFuncRegisterData> aliases,
            HlFuncRegisterData reg )
        {
            if (aliases.Count == 0)
            {
                return;
            }
            List<HlFuncRegisterData>? stale = null;
            foreach (var pair in aliases)
            {
                if (ReferenceEquals(pair.Key, reg) || ReferenceEquals(pair.Value, reg))
                {
                    (stale ??= []).Add(pair.Key);
                }
            }
            if (stale is not null)
            {
                foreach (var key in stale)
                {
                    aliases.Remove(key);
                }
            }
        }

        private static HlFuncRegisterData Resolve(
            Dictionary<HlFuncRegisterData, HlFuncRegisterData> aliases,
            HlFuncRegisterData reg )
        {
            var guard = 0;
            while (aliases.TryGetValue(reg, out var next) &&
                !ReferenceEquals(next, reg) && guard++ < 64)
            {
                reg = next;
            }
            return reg;
        }

        private sealed class BitSet
        {
            private readonly ulong[] words;

            public BitSet( int bits )
            {
                words = new ulong[(bits + 63) >> 6];
            }

            public bool Get( int index )
            {
                return (words[index >> 6] & (1UL << (index & 63))) != 0;
            }

            public void Set( int index )
            {
                words[index >> 6] |= 1UL << (index & 63);
            }

            public void Clear( int index )
            {
                words[index >> 6] &= ~(1UL << (index & 63));
            }

            public void Clear()
            {
                Array.Clear(words);
            }

            public void CollectInto( List<int> destination )
            {
                for (var w = 0; w < words.Length; w++)
                {
                    var bits = words[w];
                    while (bits != 0)
                    {
                        var bit = System.Numerics.BitOperations.TrailingZeroCount(bits);
                        destination.Add((w << 6) + bit);
                        bits &= bits - 1;
                    }
                }
            }

            public void CopyFrom( BitSet other )
            {
                Array.Copy(other.words, words, words.Length);
            }

            public void Or( BitSet other )
            {
                for (var i = 0; i < words.Length; i++)
                {
                    words[i] |= other.words[i];
                }
            }

            public void AndNot( BitSet other )
            {
                for (var i = 0; i < words.Length; i++)
                {
                    words[i] &= ~other.words[i];
                }
            }

            public bool SetEquals( BitSet other )
            {
                for (var i = 0; i < words.Length; i++)
                {
                    if (words[i] != other.words[i])
                    {
                        return false;
                    }
                }
                return true;
            }
        }
    }
}
