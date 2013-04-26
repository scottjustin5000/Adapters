using System;


namespace Adapters
{
    public abstract partial class AdapterProxy : IAdapterProxy
    {
        //private object Instance;
        //private TypeReflector reflector;

        //protected DynamicProxy (object Instance)
        //{
        //    if (Instance == null)
        //    {
        //        throw new ArgumentNullException("Instance");
        //    }

        //    Instance = Instance;
        //    reflector = new TypeReflector(Instance.GetType());
        //}

        //public object instance
        //{
        //    get;
        //    set;
        //}

        public virtual object Invoke (object instance, string method, object[] arguments)
        {
            var type = instance.GetType();
            var reflector = new TypeReflector(type);
            var argTypes = Type.GetTypeArray(arguments);
            var methodInfo = reflector.GetMethod(method, argTypes);

            if (methodInfo == null)
            {
                throw new MissingMemberException(String.Format("Method \"{0}\" with the specified signature not found for type [{1}, {2}]", method, type.Name, type.Assembly.GetName().Name));
            }

            return methodInfo.Invoke(instance, arguments);
        }

        public virtual object Get (object instance, string property)
        {
            var type = instance.GetType();
            var reflector = new TypeReflector(type);
            var propertyInfo = reflector.GetProperty(property);

            if (propertyInfo == null)
            {
                throw new MissingMemberException(String.Format("Property \"{0}\" not found for type [{1}, {2}]", property, type.Name, type.Assembly.GetName().Name));
            }

            return propertyInfo.GetValue(instance, null);
        }

        public virtual void Set (object instance, string property, object value)
        {
            var type = instance.GetType();
            var reflector = new TypeReflector(type);
            var propertyInfo = reflector.GetProperty(property);

            if (propertyInfo == null)
            {
                throw new MissingMemberException(String.Format("Property \"{0}\" not found for type [{1}, {2}]", property, type.Name, type.Assembly.GetName().Name));
            }

            propertyInfo.SetValue(instance, value, null);
        }

        public virtual object Get (object instance, object[] args)
        {
            var type = instance.GetType();
            var reflector = new TypeReflector(type);
            var argTypes = Type.GetTypeArray(args);
            var indexer = reflector.GetIndexer(argTypes);

            if (indexer == null)
            {
                throw new MissingMemberException(String.Format("Indexer with the specified signature not found for type [{0}, {1}]", type.Name, type.Assembly.GetName().Name));
            }

            return indexer.GetValue(instance, args);
        }

        public virtual void Set (object instance, object[] args, object value)
        {
            var type = instance.GetType();
            var reflector = new TypeReflector(type);
            var argTypes = Type.GetTypeArray(args);
            var indexer = reflector.GetIndexer(argTypes);

            if (indexer == null)
            {
                throw new MissingMemberException(String.Format("Indexer with the specified signature not found for type [{0}, {1}]", type.Name, type.Assembly.GetName().Name));
            }

            indexer.SetValue(instance, value, args);
        }

        public virtual void Hook (object instance, string eventName, Delegate handler)
        {
            var type = instance.GetType();
            var reflector = new TypeReflector(type);
            var eventInfo = reflector.GetEvent(eventName);

            if (eventInfo == null)
            {
                throw new MissingMemberException(String.Format("Event \"{0}\" not found for type [{1}, {2}]", eventName, type.Name, type.Assembly.GetName().Name));
            }

            eventInfo.AddEventHandler(instance, handler);
        }

        public virtual void Unhook (object instance, string eventName, Delegate handler)
        {
            var type = instance.GetType();
            var reflector = new TypeReflector(type);
            var eventInfo = reflector.GetEvent(eventName);

            if (eventInfo == null)
            {
                throw new MissingMemberException(String.Format("Event \"{0}\" not found for type [{1}, {2}]", eventName, type.Name, type.Assembly.GetName().Name));
            }

            eventInfo.RemoveEventHandler(instance, handler);
        }
    }
}
