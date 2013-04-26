using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Adapters
{
    partial class AdapterProxy
    {
        internal class ProxyEmitter : AdapterEmitter
        {
            public ProxyEmitter (TypeBuilder typeBuilder, Type instanceType)
                : base(typeBuilder, instanceType) 
            {
                ProxyField = typeBuilder.DefineField("__Proxy_", typeof(IAdapterProxy), FieldAttributes.Private);
            }

            protected FieldInfo ProxyField
            {
                get;
                private set;
            }

            public override void EmitMethod (MethodInfo methodInfo)
            {
                var emitter = new MethodEmitter(TypeBuilder, ImplField, ProxyField);
                emitter.Emit(methodInfo);
            }

            public override void EmitProperty (PropertyInfo propertyInfo)
            {
                var emitter = new PropertyEmitter(TypeBuilder, ImplField, ProxyField);
                emitter.Emit(propertyInfo);
            }

            public override void EmitIndexer (PropertyInfo indexerInfo)
            {
                var emitter = new IndexerEmitter(TypeBuilder, ImplField, ProxyField);
                emitter.Emit(indexerInfo);
            }

            public override void EmitEvent (EventInfo eventInfo)
            {
                var emitter = new EventEmitter(TypeBuilder, ImplField, ProxyField);
                emitter.Emit(eventInfo);
            }
        }
    }
}
