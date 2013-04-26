using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Adapters
{
    partial class AdapterFactory
    {
        private sealed class MethodEmitter : MemberEmitter<MethodInfo, MethodBuilder>
        {
            public MethodEmitter(TypeBuilder typeBuilder, FieldInfo implField)
                : base(typeBuilder, implField) { }

            public override MethodBuilder Emit(MethodInfo member)
            {
                var methodBuilder = AdapterEmitter.CreateMethod(TypeBuilder, member);
                var emitter = methodBuilder.GetILGenerator();

                var hasReturn = member.ReturnType != typeof(void);

                var returnLocal = hasReturn
                                    ? emitter.DeclareLocal(member.ReturnType)
                                    : null;

                var parameters = member.GetParameters();
                var parameterTypes = parameters.Select(p => p.ParameterType);

                var methods = ImplField.FieldType.GetMethods()
                                                    .Where(m => m.Name.Equals(member.Name));

                var typeComparer = new TypeComparer();

                var exactMethods = methods.Where(m => typeComparer.Equals(parameterTypes, m.GetParameters()
                                                                                            .Select(p => p.ParameterType)));

                var matchMethods = methods.Where(m => typeComparer.Assignable(parameterTypes, m.GetParameters()
                                                                                                .Select(p => p.ParameterType)));

                var implMethod = exactMethods.Union(matchMethods)
                                                .FirstOrDefault();

                if (implMethod != null)
                {
                    SafeImpl(emitter, parameters, ImplField, implMethod);
                }
                else
                {
                    UnsafeImpl(emitter, parameters, ImplField, member.Name, returnLocal);
                }

                if (hasReturn)
                {
                    emitter.Emit(OpCodes.Stloc, returnLocal);
                    emitter.Emit(OpCodes.Ldloc, returnLocal);
                }

                emitter.Emit(OpCodes.Ret);

                return methodBuilder;
            }

            private void SafeImpl(ILGenerator methodBody, ParameterInfo[] methodArgs, FieldInfo implField, MethodInfo methodImpl)
            {
                methodBody.Emit(OpCodes.Ldarg_0);
                methodBody.Emit(OpCodes.Ldfld, implField);

                foreach (var parameter in methodArgs)
                {
                    methodBody.Emit(OpCodes.Ldarg, parameter.Position + 1);
                }

                methodBody.Emit(OpCodes.Callvirt, methodImpl);
            }

            private void UnsafeImpl(ILGenerator methodBody, ParameterInfo[] methodArgs, FieldInfo implField, string methodName, LocalBuilder result)
            {
                var methodLocal = methodBody.DeclareLocal(typeof(MethodInfo));
                var typesLocal = methodBody.DeclareLocal(typeof(Type[]));
                var argsLocal = methodBody.DeclareLocal(typeof(Object[]));

                var getTypeFromHandleMethod = typeof(Type).GetMethod("GetTypeFromHandle", new Type[] { typeof(RuntimeTypeHandle) });

                methodBody.Emit(OpCodes.Ldtoken, implField.FieldType);
                methodBody.Emit(OpCodes.Call, getTypeFromHandleMethod);

                // var method = __ProxyImpl_.GetType().GetMethod("...", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, types, null);
                var getMethodMethod = typeof(Type).GetMethod("GetMethod", new Type[] { typeof(String), typeof(BindingFlags), typeof(Binder), typeof(Type[]), typeof(ParameterModifier[]) });
                methodBody.Emit(OpCodes.Ldstr, methodName);
                methodBody.Emit(OpCodes.Ldc_I4, (int)(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance));
                methodBody.Emit(OpCodes.Ldnull);

                // var types = new Type[] { typeof(first), typeof(second), ... };
                methodBody.Emit(OpCodes.Ldc_I4, methodArgs.Length);
                methodBody.Emit(OpCodes.Newarr, typeof(Type));
                methodBody.Emit(OpCodes.Stloc, typesLocal);

                foreach (var parameter in methodArgs)
                {
                    methodBody.Emit(OpCodes.Ldloc, typesLocal);
                    methodBody.Emit(OpCodes.Ldc_I4, parameter.Position);
                    methodBody.Emit(OpCodes.Ldtoken, parameter.ParameterType);
                    methodBody.Emit(OpCodes.Call, getTypeFromHandleMethod);
                    methodBody.Emit(OpCodes.Stelem_Ref);
                }

                methodBody.Emit(OpCodes.Ldloc, typesLocal);
                methodBody.Emit(OpCodes.Ldnull);
                methodBody.Emit(OpCodes.Callvirt, getMethodMethod);
                methodBody.Emit(OpCodes.Stloc, methodLocal);
                methodBody.Emit(OpCodes.Ldloc, methodLocal);

                // method.Invoke(this.__ProxyImpl_, args);
                methodBody.Emit(OpCodes.Ldarg_0);
                methodBody.Emit(OpCodes.Ldfld, implField);

                // var args = new object[2];
                methodBody.Emit(OpCodes.Ldc_I4, methodArgs.Length);
                methodBody.Emit(OpCodes.Newarr, typeof(Object));
                methodBody.Emit(OpCodes.Stloc, argsLocal);

                foreach (var parameter in methodArgs)
                {
                    methodBody.Emit(OpCodes.Ldloc, argsLocal);
                    methodBody.Emit(OpCodes.Ldc_I4, parameter.Position);
                    methodBody.Emit(OpCodes.Ldarg, parameter.Position + 1);

                    if (parameter.ParameterType.IsValueType)
                    {
                        methodBody.Emit(OpCodes.Box, parameter.ParameterType);
                    }

                    methodBody.Emit(OpCodes.Stelem_Ref);
                }

                methodBody.Emit(OpCodes.Ldloc, argsLocal);

                var invokeMethod = typeof(MethodBase).GetMethod("Invoke", new Type[] { typeof(Object), typeof(Object[]) });
                methodBody.Emit(OpCodes.Callvirt, invokeMethod);

                if (result != null
                    && result.LocalType.IsValueType)
                {
                    methodBody.Emit(OpCodes.Unbox_Any, result.LocalType);
                }
            }
        }
    }
}
