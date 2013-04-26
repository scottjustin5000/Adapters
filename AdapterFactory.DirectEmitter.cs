using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Adapters
{
    partial class AdapterFactory
    {
        internal class DirectEmitter : AdapterEmitter
        {
            public DirectEmitter(TypeBuilder typeBuilder, Type instanceType)
                : base(typeBuilder, instanceType) { }

            public override void EmitMethod(MethodInfo methodInfo)
            {
                var emitter = new MethodEmitter(TypeBuilder, ImplField);
                emitter.Emit(methodInfo);
            }

            public override void EmitProperty(PropertyInfo propertyInfo)
            {
                var emitter = new PropertyEmitter(TypeBuilder, ImplField);
                emitter.Emit(propertyInfo);
            }

            public override void EmitIndexer(PropertyInfo indexerInfo)
            {
                var emitter = new IndexerEmitter(TypeBuilder, ImplField);
                emitter.Emit(indexerInfo);
            }

            public override void EmitEvent(EventInfo eventInfo)
            {
                var emitter = new EventEmitter(TypeBuilder, ImplField);
                emitter.Emit(eventInfo);
            }
        }
    }
}
