using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Events.Collections
{
    internal sealed class EventReceiverList : IEnumerable<IEventReceiver>
    {
        class NodeDataBlock(int count)
        {
            public class BlockItem
            {
                public required IEventReceiver Receiver { get; set; }
                public required int Version { get; set; }
            }
            public int Count { get; } = count;
            public BlockItem?[] Items { get; } = new BlockItem[count];
            public NodeDataBlock? NextBlock { get; set; }
        }
        class Node
        {
            public Node()
            {
                LowestNode = this;
            }
            public Node? Parent { get; set; }
            public Node? Left { get; set; }
            public Node? Right { get; set; }
            public required int Priority { get; init; }
            public Node LowestNode { get; set; }
            public NodeDataBlock Data { get; } = new(2);
            
        }

        public int Version { get; private set; } = 0;

        private readonly Node root = new()
        {
            Priority = 0
        };

        private static IEnumerator<Node> ForeachAllNode(Node root)
        {
            if(root.Left == null && root.Right == null)
            {
                yield return root;
                yield break;
            }
            var cur = root.LowestNode;
            while (cur != root.Parent)
            {
                if(cur == null)
                {
                    yield break;
                }
                yield return cur;

                if(cur.Right != null)
                {
                    var en = ForeachAllNode(cur.Right);
                    while(en.MoveNext())
                    {
                        yield return en.Current;
                    }
                }

                cur = cur.Parent;
            }
        }

        private Node GetOrAddNode(int priority)
        {
            var curNode = root;
            Node? newNode;
            while(true)
            {
                if(curNode.Priority == priority)
                {
                    return curNode;
                }
                if(curNode.Priority > priority)
                {
                    var node = curNode.Left;
                    if(node != null)
                    {
                        curNode = node;
                        continue;
                    }
                    newNode = new()
                    {
                        Priority = priority,
                        Parent = curNode,
                    };
                    curNode.Left = newNode;
                    break;
                }
                else
                {
                    var node = curNode.Right;
                    if (node != null)
                    {
                        curNode = node;
                        continue;
                    }
                    newNode = new()
                    {
                        Priority = priority,
                        Parent = curNode,
                    };
                    curNode.Right = newNode;
                    break;
                }
            }
            while(curNode != null)
            {
                if(curNode.LowestNode.Priority >= priority)
                {
                    curNode.LowestNode = newNode;
                }
                else
                {
                    break;
                }
                curNode = curNode.Parent;
            }
            return newNode;
        }

        public IEnumerator<IEventReceiver> GetEnumerator()
        {
            var en = ForeachAllNode(root);
            var curVer = Version;
            while(en.MoveNext())
            {
                var node = en.Current;
                var curBlock = node.Data;
                while(curBlock != null)
                {
                    for(int i = 0; i < curBlock.Count; i++)
                    {
                        var block = curBlock.Items[i];
                        if(block != null)
                        {
                            if(block.Version <= curVer)
                            {
                                yield return block.Receiver;
                            }
                        }

                    }
                    curBlock = curBlock.NextBlock;
                }
            }
        }

        public void Add(IEventReceiver receiver)
        {
            var node = GetOrAddNode(receiver.Priority);
            var curBlock = node.Data;
            var lastBlock = curBlock;
            while (curBlock != null)
            {
                for (int i = 0; i < curBlock.Count; i++)
                {
                    ref var rec = ref curBlock.Items[i];
                    if(rec == receiver)
                    {
                        return;
                    }
                    if (rec == null)
                    {
                        rec = new()
                        {
                            Receiver = receiver,
                            Version = Version
                        };
                        return;
                    }
                }
                lastBlock = curBlock;
                curBlock = curBlock.NextBlock;
            }
            lastBlock.NextBlock = curBlock = new(lastBlock.Count * 2);
            curBlock.Items[0] = new()
            {
                Receiver = receiver,
                Version = Version
            };
        }
        public void Remove(IEventReceiver receiver)
        {
            var node = GetOrAddNode(receiver.Priority);
            var curBlock = node.Data;
            while (curBlock != null)
            {
                for (int i = 0; i < curBlock.Count; i++)
                {
                    ref var rec = ref curBlock.Items[i];
                    if (rec == receiver)
                    {
                        rec = null;
                        return;
                    }
                }
                curBlock = curBlock.NextBlock;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
