using System;
using System.Reflection.Emit;
using System.Reflection;

namespace Adapters
{
    partial class AdapterProxy
    {
        private sealed class MethodEmitter : MemberEmitter<MethodInfo>
        {
            public MethodEmitter (TypeBuilder typeBuilder, FieldInfo implField, FieldInfo proxyField)
                : base(typeBuilder, implField, proxyField) { }

            public override void Emit (MethodInfo member)
            {
                var methodBuilder = AdapterEmitter.CreateMethod(TypeBuilder, member);
                var emitter = methodBuilder.GetILGenerator();

                var hasReturn = member.ReturnType != typeof(void);

                var returnLocal = hasReturn
                                    ? emitter.DeclareLocal(member.ReturnType)
                                    : null;

                var parameters = member.GetParameters();

                var argsLocal = ImplArguments(emitter, parameters);

                emitter.Emit(OpCodes.Ldarg_0);
                emitter.Emit(OpCodes.Ldfld, ProxyField);
                emitter.Emit(OpCodes.Ldarg_0);
                emitter.Emit(OpCodes.Ldfld, ImplField);
                //emitter.Emit(OpCodes.Castclass, typeof(object));
                emitter.Emit(OpCodes.Ldstr, member.Name);
                emitter.Emit(OpCodes.Ldloc, argsLocal);

                var reflector = new TypeReflector(ProxyField.FieldType);
                var methodImpl = reflector.GetMethod("Invoke", new Type[] { typeof(object), typeof(string), typeof(object[]) });
                //var methodImpl = reflector.GetMethod("Invoke", new Type[] { typeof(string), typeof(object[]) });
                emitter.Emit(OpCodes.Callvirt, methodImpl);

                if (hasReturn)
                {
                    if (member.ReturnType.IsValueType)
                    {
                        emitter.Emit(OpCodes.Unbox_Any, member.ReturnType);
                    }

                    emitter.Emit(OpCodes.Stloc, returnLocal);
                    emitter.Emit(OpCodes.Ldloc, returnLocal);
                }

                emitter.Emit(OpCodes.Ret);
            }
        }
    }
}