using System;
using System.Reflection.Emit;
using System.Reflection;

namespace Adapters
{
    partial class AdapterProxy
    {
        private sealed class EventEmitter : MemberEmitter<EventInfo>
        {
            public EventEmitter (TypeBuilder typeBuilder, FieldInfo implField, FieldInfo proxyField)
                : base(typeBuilder, implField, proxyField) { }

            public override void Emit (EventInfo member)
            {
                var eventBuilder = TypeBuilder.DefineEvent(member.Name, member.Attributes, member.EventHandlerType);

                var addMethod = member.GetAddMethod();
                var removeMethod = member.GetRemoveMethod();

                // for examples, assume the following:
                // var proxy = new DynamicProxy(typeof(MyProxy));
                // var inst = (MyProxy)proxy.Create(ref);
                if (addMethod != null)
                {
                    // call example:
                    // inst.MyEvent += new MyEventHandler(Instance_MyEvent);
                    //
                    // translate to:
                    // proxy.AddEvent(instance, "MyEvent", new MyEventHandler(Instance_MyEvent));
                    var methodBuilder = AdapterEmitter.CreateMethod(TypeBuilder, addMethod);
                    ImplCall(methodBuilder, member.Name, "Hook");
                    eventBuilder.SetAddOnMethod(methodBuilder);
                }

                if (removeMethod != null)
                {
                    var methodBuilder = AdapterEmitter.CreateMethod(TypeBuilder, removeMethod);
                    ImplCall(methodBuilder, member.Name, "Unhook");
                    eventBuilder.SetRemoveOnMethod(methodBuilder);
                }
            }

            private void ImplCall (MethodBuilder methodImpl, string eventName, string proxyMethod)
            {
                var emitter = methodImpl.GetILGenerator();

                emitter.Emit(OpCodes.Ldarg_0);
                emitter.Emit(OpCodes.Ldfld, ProxyField);
                emitter.Emit(OpCodes.Ldarg_0);
                emitter.Emit(OpCodes.Ldfld, ImplField);
                //emitter.Emit(OpCodes.Castclass, typeof(object));
                emitter.Emit(OpCodes.Ldstr, eventName);
                emitter.Emit(OpCodes.Ldarg_1);

                var reflector = new TypeReflector(ProxyField.FieldType);
                var method = reflector.GetMethod(proxyMethod, new Type[] { typeof(object), typeof(string), typeof(Delegate) });
                //var method = reflector.GetMethod(proxyMethod, new Type[] { typeof(string), typeof(Delegate) });
                emitter.Emit(OpCodes.Callvirt, method);
            }
        }
    }
}