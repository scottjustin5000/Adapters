
using System.Reflection;
using System.Reflection.Emit;

namespace Adapters
{
    partial class AdapterProxy
    {
        private abstract class MemberEmitter<TMember>
        {
            protected MemberEmitter (TypeBuilder typeBuilder, FieldInfo implField, FieldInfo proxyField)
            {
                TypeBuilder = typeBuilder;
                ImplField = implField;
                ProxyField = proxyField;

            }

            protected TypeBuilder TypeBuilder
            {
                get;
                private set;
            }

            protected FieldInfo ImplField
            {
                get;
                private set;
            }

            protected FieldInfo ProxyField
            {
                get;
                private set;
            }

            public abstract void Emit (TMember member);

            protected LocalBuilder ImplArguments (ILGenerator implBody, ParameterInfo[] argParams)
            {
                var argsLocal = implBody.DeclareLocal(typeof(object[]));

                // var args = new object[n];
                implBody.Emit(OpCodes.Ldc_I4, argParams.Length);
                implBody.Emit(OpCodes.Newarr, typeof(object));
                implBody.Emit(OpCodes.Stloc, argsLocal);

                foreach (var argParam in argParams)
                {
                    // args[x] = ...
                    implBody.Emit(OpCodes.Ldloc, argsLocal);
                    implBody.Emit(OpCodes.Ldc_I4, argParam.Position);
                    implBody.Emit(OpCodes.Ldarg, argParam.Position + 1);

                    if (argParam.ParameterType.IsValueType)
                    {
                        implBody.Emit(OpCodes.Box, argParam.ParameterType);
                    }

                    implBody.Emit(OpCodes.Stelem_Ref);
                }

                return argsLocal;
            }
        }
    }
}
