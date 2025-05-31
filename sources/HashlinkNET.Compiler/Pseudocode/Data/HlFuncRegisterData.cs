using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static HashlinkNET.Compiler.Pseudocode.Data.HlFuncRegisterData;

namespace HashlinkNET.Compiler.Pseudocode.Data
{
    record class HlFuncRegisterData(
        RegisterKind Kind,
        int Index
        )
    {
        public ParameterDefinition? Parameter
        {
            get; set;
        }
        public VariableDefinition? Variable
        {
            get; set;
        }
        public TypeReference RegisterType => Variable?.VariableType ?? Parameter!.ParameterType;
        public enum RegisterKind
        {
            LocalVar,
            Parameter
        }
    }
}
