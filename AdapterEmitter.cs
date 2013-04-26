using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Adapters
{
    internal abstract class AdapterEmitter
    {
        public static MethodBuilder CreateMethod(TypeBuilder typeBuilder, MethodInfo method)
        {
            var scope = method.IsPublic
                        ? MethodAttributes.Public
                        : MethodAttributes.Family;

            var parameters = method.GetParameters();

            var parameterTypes = parameters.Select(p => p.ParameterType)
                                           .ToArray();

            var methodBuilder = typeBuilder.DefineMethod(method.Name, scope | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig, method.CallingConvention, method.ReturnType, parameterTypes);

            foreach (var parameter in parameters)
            {
                methodBuilder.DefineParameter(parameter.Position + 1, parameter.Attributes, parameter.Name);
            }

            typeBuilder.DefineMethodOverride(methodBuilder, method);

            return methodBuilder;
        }

        protected AdapterEmitter(TypeBuilder typeBuilder, Type instanceType)
        {
            TypeBuilder = typeBuilder;
            ImplField = typeBuilder.DefineField("__Inst_", instanceType, FieldAttributes.Private);
        }

        public TypeBuilder TypeBuilder
        {
            get;
            private set;
        }

        public FieldInfo ImplField
        {
            get;
            private set;
        }

        public void EmitConstructor()
        {
            var builder = TypeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[] { ImplField.FieldType });
            var emitter = builder.GetILGenerator();

            emitter.Emit(OpCodes.Ldarg_0);
            emitter.Emit(OpCodes.Ldarg_1);
            emitter.Emit(OpCodes.Stfld, ImplField);
            emitter.Emit(OpCodes.Ret);
        }

        public abstract void EmitMethod(MethodInfo methodInfo);
        public abstract void EmitProperty(PropertyInfo propertyInfo);
        public abstract void EmitIndexer(PropertyInfo indexerInfo);
        public abstract void EmitEvent(EventInfo eventInfo);
    }
}
