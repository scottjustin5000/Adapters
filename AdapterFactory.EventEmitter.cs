using System.Reflection;
using System.Reflection.Emit;

namespace Adapters
{
    partial class AdapterFactory
    {
        private sealed class EventEmitter : MemberEmitter<EventInfo, EventBuilder>
        {
            public EventEmitter(TypeBuilder typeBuilder, FieldInfo implField)
                : base(typeBuilder, implField)
            {
            }

            public override EventBuilder Emit(EventInfo member)
            {
                var eventBuilder = TypeBuilder.DefineEvent(member.Name, member.Attributes, member.EventHandlerType);

                var addMethod = member.GetAddMethod();
                var removeMethod = member.GetRemoveMethod();

                //if (addMethod != null)
                //{
                //    var methodBuilder = AdapterEmitter.CreateMethod(TypeBuilder, addMethod);
                //    var methodEmitter = methodBuilder.GetILGenerator();
                //    ImplProxy.ImplEventAdd(methodEmitter, ImplField, member.Name, member.EventHandlerType);
                //    eventBuilder.SetAddOnMethod(methodBuilder);
                //}

                //if (removeMethod != null)
                //{
                //    var methodBuilder = AdapterEmitter.CreateMethod(TypeBuilder, removeMethod);
                //    var methodEmitter = methodBuilder.GetILGenerator();
                //    ImplProxy.ImplEventRemove(methodEmitter, ImplField, member.Name, member.EventHandlerType);
                //    eventBuilder.SetRemoveOnMethod(methodBuilder);
                //}

                var methodEmitter = new MethodEmitter(TypeBuilder, ImplField);

                if (addMethod != null)
                {
                    var methodBuilder = methodEmitter.Emit(addMethod);
                    eventBuilder.SetAddOnMethod(methodBuilder);
                }

                if (removeMethod != null)
                {
                    var methodBuilder = methodEmitter.Emit(removeMethod);
                    eventBuilder.SetRemoveOnMethod(methodBuilder);
                }

                return eventBuilder;
            }
        }
    }
}
