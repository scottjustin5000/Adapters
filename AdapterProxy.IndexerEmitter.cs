using System;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;

namespace Adapters
{
    partial class AdapterProxy
    {
        private sealed class IndexerEmitter : MemberEmitter<PropertyInfo>
        {
            public IndexerEmitter (TypeBuilder typeBuilder, FieldInfo implField, FieldInfo proxyField)
                : base(typeBuilder, implField, proxyField) { }

            public override void Emit (PropertyInfo member)
            {
                var getMethod = member.CanRead
                                ? member.GetGetMethod()
                                : null;

                var setMethod = member.CanWrite
                                ? member.GetSetMethod()
                                : null;

                var callingConvention = getMethod != null
                                        ? getMethod.CallingConvention
                                        : setMethod.CallingConvention;

                var indexerBuilder = TypeBuilder.DefineProperty(member.Name, member.Attributes, callingConvention, member.PropertyType, member.GetIndexParameters()
                                                                                                                                                .Select(p => p.ParameterType)
                                                                                                                                                .ToArray());
                var indexParams = member.GetIndexParameters();

                if (getMethod != null)
                {
                    var methodBuilder = AdapterEmitter.CreateMethod(TypeBuilder, getMethod);
                    ImplGet(methodBuilder, indexParams, member.PropertyType);
                    indexerBuilder.SetGetMethod(methodBuilder);
                }

                if (setMethod != null)
                {
                    var methodBuilder = AdapterEmitter.CreateMethod(TypeBuilder, setMethod);
                    ImplSet(methodBuilder, indexParams, setMethod.ReturnParameter);
                    indexerBuilder.SetSetMethod(methodBuilder);
                }
            }

            private void ImplGet (MethodBuilder methodImpl, ParameterInfo[] indexParams, Type returnType)
            {
                var emitter = methodImpl.GetILGenerator();
                var argsLocal = ImplArguments(emitter, indexParams);

                emitter.Emit(OpCodes.Ldarg_0);
                emitter.Emit(OpCodes.Ldfld, ProxyField);
                emitter.Emit(OpCodes.Ldarg_0);
                emitter.Emit(OpCodes.Ldfld, ImplField);
                //emitter.Emit(OpCodes.Castclass, typeof(object));
                emitter.Emit(OpCodes.Ldloc, argsLocal);

                var reflector = new TypeReflector(ProxyField.FieldType);
                var method = reflector.GetMethod("Get", new Type[] { typeof(object), typeof(object[]) });
                //var method = reflector.GetMethod("Get", new Type[] { typeof(object[]) });
                emitter.Emit(OpCodes.Callvirt, method);

                if (returnType.IsValueType)
                {
                    emitter.Emit(OpCodes.Unbox_Any, returnType);
                }
            }

            private void ImplSet (MethodBuilder methodImpl, ParameterInfo[] indexParams, ParameterInfo valueParam)
            {
                var emitter = methodImpl.GetILGenerator();
                var valueLocal = emitter.DeclareLocal(typeof(object));
                var argsLocal = ImplArguments(emitter, indexParams);

                emitter.Emit(OpCodes.Ldloc, valueLocal);
                emitter.Emit(OpCodes.Ldarg, indexParams.Length + 1);

                if (valueParam.ParameterType.IsValueType)
                {
                    emitter.Emit(OpCodes.Box, valueParam.ParameterType);
                }

                emitter.Emit(OpCodes.Stloc, valueLocal);

                emitter.Emit(OpCodes.Ldarg_0);
                emitter.Emit(OpCodes.Ldfld, ProxyField);
                emitter.Emit(OpCodes.Ldarg_0);
                emitter.Emit(OpCodes.Ldfld, ImplField);
                //emitter.Emit(OpCodes.Castclass, typeof(object));
                emitter.Emit(OpCodes.Ldloc, argsLocal);
                emitter.Emit(OpCodes.Ldloc, valueLocal);

                var reflector = new TypeReflector(ProxyField.FieldType);
                var method = reflector.GetMethod("Set", new Type[] { typeof(object), typeof(object[]), typeof(object) });
                //var method = reflector.GetMethod("Set", new Type[] { typeof(object[]), typeof(object) });
                emitter.Emit(OpCodes.Callvirt, method);
            }
        }
    }
}