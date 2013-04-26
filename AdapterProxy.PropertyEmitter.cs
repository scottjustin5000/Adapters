using System;
using System.Reflection.Emit;
using System.Reflection;

namespace Adapters
{
    partial class AdapterProxy
    {
        private sealed class PropertyEmitter : MemberEmitter<PropertyInfo>
        {
            public PropertyEmitter (TypeBuilder typeBuilder, FieldInfo implField, FieldInfo proxyField)
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

                var propertyBuilder = TypeBuilder.DefineProperty(member.Name, member.Attributes, callingConvention, member.PropertyType, Type.EmptyTypes);
                    
                if (getMethod != null)
                {
                    var methodImpl = AdapterEmitter.CreateMethod(TypeBuilder, getMethod);
                    ImplGet(getMethod, methodImpl);
                    propertyBuilder.SetGetMethod(methodImpl);
                }

                if (setMethod != null)
                {
                    var methodImpl = AdapterEmitter.CreateMethod(TypeBuilder, setMethod);
                    ImplSet(setMethod, methodImpl);
                    propertyBuilder.SetSetMethod(methodImpl);
                }
            }

            private void ImplGet (MethodInfo methodInfo, MethodBuilder methodImpl)
            {
                var emitter = methodImpl.GetILGenerator();

                //var returnLocal = implBody.DeclareLocal(propertyType);
                emitter.Emit(OpCodes.Ldarg_0);
                emitter.Emit(OpCodes.Ldfld, ProxyField);
                emitter.Emit(OpCodes.Ldarg_0);
                emitter.Emit(OpCodes.Ldfld, ImplField);
                //emitter.Emit(OpCodes.Castclass, typeof(object));

                var reflector = new TypeReflector(ProxyField.FieldType);
                var method = reflector.GetMethod("Get", new Type[] { typeof(object), typeof(string) });
                //var method = reflector.GetMethod("Get", new Type[] { typeof(string) });
                emitter.Emit(OpCodes.Callvirt, method);

                if (methodInfo.ReturnType.IsValueType)
                {
                    emitter.Emit(OpCodes.Unbox_Any, methodInfo.ReturnType);
                }
            }

            private void ImplSet (MethodInfo methodInfo, MethodBuilder methodImpl)
            {
                var emitter = methodImpl.GetILGenerator();

                emitter.Emit(OpCodes.Ldarg_0);
                emitter.Emit(OpCodes.Ldfld, ProxyField);
                emitter.Emit(OpCodes.Ldarg_0);
                emitter.Emit(OpCodes.Ldfld, ImplField);
                //emitter.Emit(OpCodes.Castclass, typeof(object));
                emitter.Emit(OpCodes.Ldarg_1);

                if (methodInfo.ReturnParameter.ParameterType.IsValueType)
                {
                    emitter.Emit(OpCodes.Box, methodInfo.ReturnParameter.ParameterType);
                }

                var reflector = new TypeReflector(ProxyField.FieldType);
                var method = reflector.GetMethod("Set", new Type[] { typeof(object), typeof(string), typeof(object) });
                //var method = reflector.GetMethod("Set", new Type[] { typeof(string), typeof(object) });
                emitter.Emit(OpCodes.Callvirt, method);
            }
        }
    }
}