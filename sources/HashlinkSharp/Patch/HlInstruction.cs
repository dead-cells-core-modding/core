using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Patch
{
    public class HlInstruction
    {
        public HlOpCode OpCode
        {
            get; set;
        } = HlOpCodes.ONop;
        public object[] Operands
        {
            get; set;
        } = [];
        internal int Index
        {
            get; set;
        }

        public HlInstruction()
        {
        
        }
        public HlInstruction( HlOpCode opcode, params object[] operands )
        {
            OpCode = opcode;
            Operands = operands;
        }
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"OP {Index:x}: ");
            sb.Append( OpCode );
            sb.Append(':');
            foreach (var v in Operands)
            {
                sb.Append(" , ");
                if (v is string str)
                {
                    sb.Append('"');
                    sb.Append(str);
                    sb.Append('"');
                } 
                else if (v is HlInstruction ti)
                {
                    sb.Append(" {");
                    sb.Append(ti.ToString());
                    sb.Append('}');
                }
                else
                {
                    sb.Append(v);
                }
                    
            }
            return sb.ToString();
        }
    }
}
