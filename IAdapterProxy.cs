using System;


namespace Adapters
{
    public interface IAdapterProxy
    {
        //object instance { get; set; }
        object Invoke (object instance, string method, object[] args);
        object Get (object instance, string property);
        void Set (object instance, string property, object value);
        object Get (object instance, object[] args);
        void Set (object instance, object[] args, object value);
        void Hook (object instance, string eventName, Delegate handler);
        void Unhook (object instance, string eventName, Delegate handler);
    }
}
