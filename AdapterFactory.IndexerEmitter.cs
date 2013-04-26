using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Adapters
{
    partial class AdapterFactory
    {
        private sealed class IndexerEmitter : MemberEmitter<PropertyInfo, PropertyBuilder>
        {
            public IndexerEmitter(TypeBuilder typeBuilder, FieldInfo implField)
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

                var indexerBuilder = TypeBuilder.DefineProperty(member.Name, member.Attributes, callingConvention, member.PropertyType, member.GetIndexParameters()
                                                                                                                                                .Select(p => p.ParameterType)
                                                                                                                                                .ToArray());
                //var indexParams = member.GetIndexParameters();

                //if (getMethod != null)
                //{
                //    var methodBuilder = AdapterEmitter.CreateMethod(TypeBuilder, getMethod);
                //    var methodEmitter = methodBuilder.GetILGenerator();
                //    ImplProxy.ImplIndexerGet(methodEmitter, ImplField, indexParams, member.PropertyType);
                //    indexerBuilder.SetGetMethod(methodBuilder);
                //}

                //if (setMethod != null)
                //{
                //    var methodBuilder = AdapterEmitter.CreateMethod(TypeBuilder, setMethod);
                //    var methodEmitter = methodBuilder.GetILGenerator();
                //    ImplProxy.ImplIndexerSet(methodEmitter, ImplField, indexParams, setMethod.ReturnParameter);
                //    indexerBuilder.SetSetMethod(methodBuilder);
                //}

                var methodEmitter = new MethodEmitter(TypeBuilder, ImplField);

                if (getMethod != null)
                {
                    var methodBuilder = methodEmitter.Emit(getMethod);
                    indexerBuilder.SetGetMethod(methodBuilder);
                }

                if (setMethod != null)
                {
                    var methodBuilder = methodEmitter.Emit(setMethod);
                    indexerBuilder.SetSetMethod(methodBuilder);
                }

                return indexerBuilder;
            }
        }
    }
}
