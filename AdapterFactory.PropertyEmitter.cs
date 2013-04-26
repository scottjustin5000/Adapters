using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Adapters
{
    partial class AdapterFactory
    {
        private sealed class PropertyEmitter : MemberEmitter<PropertyInfo, PropertyBuilder>
        {
            public PropertyEmitter(TypeBuilder typeBuilder, FieldInfo implField)
                : base(typeBuilder, implField) { }

            public override PropertyBuilder Emit(PropertyInfo member)
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
                var methodEmitter = new MethodEmitter(TypeBuilder, ImplField);

                if (getMethod != null)
                {
                    var getMethodBuilder = methodEmitter.Emit(getMethod);
                    propertyBuilder.SetGetMethod(getMethodBuilder);
                }

                if (setMethod != null)
                {
                    var setMethodBuilder = methodEmitter.Emit(setMethod);
                    propertyBuilder.SetSetMethod(setMethodBuilder);
                }

                return propertyBuilder;
            }
        }
    }
}
