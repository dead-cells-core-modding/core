using HashlinkNET.Bytecode;
using HashlinkNET.Compiler.Data;
using HashlinkNET.Compiler.Utils;
using HaxeProxy.Runtime;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace HashlinkNET.Compiler.Steps.Preprocessor.Fun
{
    internal class GenerateAltClosureDefStep : ForeachHlTypeCompileStep
    {
        private record class AltClosureName( int ArgCount, bool HasReturnValue, long RefMark );
        private TypeDefinition GenerateDelegate( ModuleDefinition module,
            RuntimeImports rdata,
            AltClosureName name)
        {
            var hasRet = name.HasReturnValue;
            var refMark = name.RefMark;
            var argCount = name.ArgCount;

            var ts = module.TypeSystem;
            var type = new TypeDefinition("HaxeProxy.Runtime.Alt", !hasRet ?
                $"AltHlAction_{refMark:X}`{argCount}": $"AltHlFunc_{refMark:X}`{argCount + 1}", TypeAttributes.Class | TypeAttributes.Public
                | TypeAttributes.Sealed)
            {
                BaseType = rdata.delegateBaseType
            };
            type.CustomAttributes.Add(new(rdata.attrAlt));
            TypeReference retType;
            if (!hasRet)
            {
                retType = module.TypeSystem.Void;
            }
            else
            {
                var pd = new GenericParameter("TRet", type)
                {
                    AllowByRefLikeConstraint = false
                };
                type.GenericParameters.Add(pd);
                retType = pd;
            }

            var argTypes = new TypeReference[argCount];

            for (var i = 0; i < argCount; i++)
            {
                var pd = new GenericParameter("TArg" + (i + 1), type)
                {
                    AllowByRefLikeConstraint = false,
                };
                type.GenericParameters.Add(pd);
                argTypes[i] = pd;

                if ((refMark & (1L << i)) != 0)
                {
                    var rt = new ByReferenceType(argTypes[i]);
                    argTypes[i] = rt;

                    pd.HasNotNullableValueTypeConstraint = true;

                    Debug.Assert(argTypes[i] is ByReferenceType);
                }
            }

            Debug.Assert(argTypes.Any(x => x is ByReferenceType));

            var paramsArray = argTypes.Select(x => new ParameterDefinition(x)).ToArray();

            type.Methods.Add(new MethodDefinition(".ctor", MethodAttributes.Public, ts.Void)
            {
                Parameters =
                {
                    new(ts.Object),
                    new(ts.IntPtr)
                },
                HasThis = true,
                IsVirtual = false,
                IsRuntimeSpecialName = true,
                IsSpecialName = true,
                IsHideBySig = true,
                IsRuntime = true,
                Body = null
            });
            type.Methods.Add(new MethodDefinition("EndInvoke", MethodAttributes.Public, retType)
            {
                Parameters =
                {
                    new(rdata.IAsyncResultType),
                },
                HasThis = true,
                IsVirtual = true,
                IsHideBySig = true,
                IsRuntime = true,
                IsNewSlot = true,
                Body = null
            });
            var invoke = new MethodDefinition("Invoke", MethodAttributes.Public, retType)
            {
                HasThis = true,
                IsVirtual = true,
                IsHideBySig = true,
                IsRuntime = true,
                IsNewSlot = true,
                Body = null
            };
            type.Methods.Add(invoke);
            var beginInvoke = new MethodDefinition("BeginInvoke", MethodAttributes.Public, rdata.IAsyncResultType)
            {
                HasThis = true,
                IsVirtual = true,
                IsHideBySig = true,
                IsRuntime = true,
                IsNewSlot = true,
                Body = null
            };
            type.Methods.Add(beginInvoke);
            foreach (var v in argTypes)
            {
                invoke.Parameters.Add(new(v));
                beginInvoke.Parameters.Add(new(v));
            }
            beginInvoke.Parameters.Add(new(rdata.AsyncCallbackType));
            beginInvoke.Parameters.Add(new(ts.Object));


            return type;
        }

        private AltClosureName? GetAltName( HlTypeWithFun type )
        {
            var fun = type.FunctionDescription;
            long refMark = 0;

            for (int i = 0; i < fun.Arguments.Length; i++)
            {
                var arg = fun.Arguments[i];
                if (arg.Value.Kind == HlTypeKind.Ref)
                {
                    refMark |= 1L << i;
                }
            }

            if (refMark == 0)
            {
                return null;
            }

            return new(fun.Arguments.Length,
                fun.ReturnType.Value.Kind != HlTypeKind.Void, refMark);
        }

        public override bool Filter( HlType type )
        {
            return type.Kind == HlTypeKind.Fun;
        }

        private readonly ConcurrentDictionary<AltClosureName, TypeReference> baseAltClosure = [];

        public override void Execute( IDataContainer container, HlCode code, GlobalData gdata, 
            RuntimeImports rdata, HlType type )
        {
            if (type is not HlTypeWithFun tf)
            {
                return;
            }
            var name = GetAltName(tf);
            if (name == null)
            {
                return;
            }

            var b = baseAltClosure.GetOrAdd(name, _ =>
            {
                var gd = GenerateDelegate(gdata.Module, rdata, name);
                return gd;
            });

            var mainType = (GenericInstanceType) container.GetTypeRef(type);
            var altType = new GenericInstanceType(b);

            foreach (var v in mainType.GenericArguments)
            {
                if (v is GenericInstanceType gt &&
                    gt.ElementType.FullName == typeof(Ref<>).FullName)
                {
                    var vt = gt.GenericArguments[0];
                    altType.GenericArguments.Add(vt);

                    Debug.Assert(vt.IsPrimitive);
                }
                else
                {
                    altType.GenericArguments.Add(v);
                }
            }

            var cd = container.GetData<ClosureClassData>(type);
            cd.AltTypeReference = altType;
        }

        protected override void PostProcessing( IDataContainer container )
        {
            var gdata = container.GetGlobalData<GlobalData>();

            foreach ((var _, var td) in baseAltClosure)
            {
                gdata.Module.Types.Add((TypeDefinition) td);    
            }
            base.PostProcessing(container);
        }
    }
}
