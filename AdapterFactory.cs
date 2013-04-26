using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Adapters
{
    /// <summary>
    /// Provides the ability to adapt objects to other types.
    /// </summary>
    /// <example>
    /// <code>
    /// // -----------------------------------------------------------------
    /// // Create adapter without a proxy
    /// // -----------------------------------------------------------------
    /// 
    /// // assume IMyStream defines a Read(byte[], int, int) method and a Length
    /// // property and Fabricon.DataStream is a COM object that defines a Read 
    /// // method and Length property with the same signatures.
    /// var factory = new AdapterFactory(typeof(IMyStream));
    ///
    /// // assume dataStream is an instance of type Fabricon.DataStream.
    /// var stream = (IMyStream)factory.CreateAdapter(dataStream);
    /// var bytes = new byte[stream.Length];
    /// stream.Read(bytes, 0, bytes.Length);
    /// 
    /// // -----------------------------------------------------------------
    /// // Create adapter with a proxy
    /// // -----------------------------------------------------------------
    /// 
    /// // assume IMyStream defines a Read(byte[], int, int) method and a Length
    /// // property and Fabricon.DataStream is a COM object that defines a Read 
    /// // method and Length property with the same signatures.
    /// var factory = new AdapterFactory(typeof(IMyStream));
    /// 
    /// // add a proxy to intercept all of the calls to the adapter. Assume
    /// // MyProxy is a type you have defined that derives from AdapterProxy
    /// // or implements IAdapterProxy.
    /// factory.AdapterProxy = new MyProxy();
    /// 
    /// // assume dataStream is an instance of type Fabricon.DataStream.
    /// var stream = (IMyStream)factory.CreateAdapter(dataStream);
    /// 
    /// // the call to stream.Length will be channeled through the AdapterProxy
    /// // Get(object, string) method. If MyProxy overrides this method, then
    /// // custom action can be performed. Otherwise, the AdapterProxy class
    /// // will call the underlying Length property on the dataStream instance
    /// // using reflection.
    /// var bytes = new byte[stream.Length];
    /// 
    /// // the call to stream.Read will be channeled through the AdapterProxy
    /// // Invoke(object, string, object[]) method. If MyProxy overrides this 
    /// // method, then custom action can be performed. Otherwise, the AdapterProxy 
    /// // class will call the underlying Read method on the dataStream instance
    /// // using reflection.
    /// stream.Read(bytes, 0, bytes.Length);
    /// </code>
    /// </example>
    public partial class AdapterFactory
    {
        private static AssemblyBuilder s_DynamicAssemby;
        private static IDictionary<Type, ModuleBuilder> s_DynamicModules;

        static AdapterFactory()
        {
            s_DynamicAssemby = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("fxadapt"), AssemblyBuilderAccess.RunAndSave);
            s_DynamicModules = new Dictionary<Type, ModuleBuilder>();
        }

        private ModuleBuilder m_DynamicModule;
        private Type m_ProxyType;

        /// <summary>
        /// Creates and returns a new instance of type <see cref="AdapterFactory"/>.
        /// </summary>
        public AdapterFactory(Type proxyType)
        {
            if (proxyType == null)
            {
                throw new ArgumentNullException("adapterType");
            }

            if (proxyType.IsValueType)
            {
                throw new ArgumentException(String.Format("An adapter for type [{0}, {1}] cannot be created because is is a value type.", proxyType.FullName, proxyType.Assembly.GetName().Name));
            }

            if (proxyType.IsClass
                && proxyType.IsSealed)
            {
                throw new ArgumentException(String.Format("An adapter for type [{0}, {1}] cannot be created because the class is sealed.", proxyType.FullName, proxyType.Assembly.GetName().Name));
            }

            m_ProxyType = proxyType;
            m_DynamicModule = GetAdapterModule(proxyType);
        }

        /// <summary>
        /// Gets/sets a proxy to use for the generated adapters.
        /// </summary>
        public IAdapterProxy AdapterProxy
        {
            get;
            set;
        }

        public void SaveAssembly(string fileName)
        {
            s_DynamicAssemby.Save(fileName);
        }

        /// <summary>
        /// Creates an adapter to the specified instance.
        /// </summary>
        public object CreateAdapter(object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            var adapterType = GetAdapterType(value);

            var instance = Activator.CreateInstance(adapterType, value);

            if (adapterType.Name.EndsWith("Proxy"))
            {
                var proxyField = adapterType.GetField("__Proxy_", BindingFlags.NonPublic | BindingFlags.Instance);
                proxyField.SetValue(instance, AdapterProxy);
            }

            return instance;
        }

        ///// <summary>
        ///// Creates an adapter to the specified instance that calls through the specified proxy.
        ///// </summary>
        //public object CreateAdapter (object value, IAdapterProxy proxy)
        //{
        //    if (value == null)
        //    {
        //        throw new ArgumentNullException("value");
        //    }

        //    var adapterType = GetAdapterType(value);

        //    var instance = Activator.CreateInstance(adapterType, value);

        //    if (adapterType.Name.EndsWith("Proxy"))
        //    {
        //        var proxyField = adapterType.GetField("__Proxy_", BindingFlags.NonPublic | BindingFlags.Instance);
        //        proxyField.SetValue(instance, AdapterProxy);
        //    }

        //    return instance;
        //}

        /// <summary>
        /// Gets the proxy type for the specified instance.
        /// </summary>
        public Type GetAdapterType(object instance)
        {
            var instanceType = instance.GetType();

            var typeName = AdapterProxy != null
                           ? String.Concat(instanceType.FullName, "Proxy")
                           : String.Concat(instanceType.FullName, "Adapter");

            var type = m_DynamicModule.GetType(typeName);

            if (type == null)
            {
                var typeBuilder = m_ProxyType.IsClass
                                  ? m_DynamicModule.DefineType(typeName, TypeAttributes.Public | TypeAttributes.Class, m_ProxyType)
                                  : m_DynamicModule.DefineType(typeName, TypeAttributes.Public | TypeAttributes.Class, typeof(object), new Type[] { m_ProxyType });

                var emitter = AdapterProxy != null
                              ? (AdapterEmitter)new AdapterProxy.ProxyEmitter(typeBuilder, instanceType)
                              : (AdapterEmitter)new DirectEmitter(typeBuilder, instanceType);

                emitter.EmitConstructor();

                EmitAdapterMethods(m_ProxyType, emitter);
                EmitAdapterProperties(m_ProxyType, emitter);
                EmitAdapterEvents(m_ProxyType, emitter);

                type = typeBuilder.CreateType();
            }

            return type;
        }

        private static ModuleBuilder GetAdapterModule(Type adapterType)
        {
            if (!s_DynamicModules.ContainsKey(adapterType))
            {
                lock (s_DynamicModules)
                {
                    if (!s_DynamicModules.ContainsKey(adapterType))
                    {
                        var assemblyName = s_DynamicAssemby.GetName().Name;
                        var dynamicModule = s_DynamicAssemby.DefineDynamicModule(adapterType.FullName, String.Concat(assemblyName, ".dll"));
                        s_DynamicModules.Add(adapterType, dynamicModule);
                    }
                }
            }

            return s_DynamicModules[adapterType];
        }

        //private static void EmitDebuggerDisplay (TypeBuilder typeBuilder, Type proxyType, Type implType)
        //{
        //    var constructor = typeof(DebuggerDisplayAttribute).GetConstructor(new Type[] { typeof(string) });
        //    var arguments = new object[] { String.Format("DynamicProxy :: {0} => {1}", implType.FullName, proxyType.FullName) };
        //    var debuggerDisplay = new CustomAttributeBuilder(constructor, arguments);
        //    typeBuilder.SetCustomAttribute(debuggerDisplay);
        //}

        private static void EmitAdapterMethods(Type adapterType, AdapterEmitter emitter)
        {
            var methods = adapterType.IsClass
                          ? adapterType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic)
                                       .Where(m => CanOverrideMethod(m))
                          : adapterType.GetMethods()
                                       .Where(m => !m.IsSpecialName);

            foreach (var method in methods)
            {
                emitter.EmitMethod(method);
            }
        }

        private static void EmitAdapterProperties(Type adapterType, AdapterEmitter emitter)
        {
            var properties = adapterType.IsClass
                             ? adapterType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic)
                                          .Where(p => p.CanRead
                                                      ? CanOverrideMethod(p.GetGetMethod())
                                                      : p.CanWrite
                                                        ? CanOverrideMethod(p.GetSetMethod())
                                                        : false)
                                          .ToArray()
                             : adapterType.GetProperties();

            if (properties.Length != 0)
            {
                foreach (var property in properties)
                {
                    if (property.GetIndexParameters().Any())
                    {
                        emitter.EmitIndexer(property);
                    }
                    else
                    {
                        emitter.EmitProperty(property);
                    }
                }
            }
        }

        private static void EmitAdapterEvents(Type adapterType, AdapterEmitter emitter)
        {
            var events = adapterType.GetEvents();

            if (events.Length != 0)
            {
                foreach (var evt in events)
                {
                    emitter.EmitEvent(evt);
                }
            }
        }

        private static bool CanOverrideMethod(MethodInfo method)
        {
            return method != null
                   && !method.IsPrivate
                   && (method.IsAbstract
                       || method.IsVirtual);
        }
    }

    /// <summary>
    /// Provides the ability to adapt objects to other types.
    /// </summary>
    /// <example>
    /// <code>
    /// // -----------------------------------------------------------------
    /// // Create adapter without a proxy
    /// // -----------------------------------------------------------------
    /// 
    /// // assume IMyStream defines a Read(byte[], int, int) method and a Length
    /// // property and Fabricon.DataStream is a COM object that defines a Read 
    /// // method and Length property with the same signatures.
    /// var factory = new AdapterFactory{IMyStream}();
    ///
    /// // assume dataStream is an instance of type Fabricon.DataStream.
    /// var stream = factory.CreateAdapter(dataStream);
    /// var bytes = new byte[stream.Length];
    /// stream.Read(bytes, 0, bytes.Length);
    /// 
    /// // -----------------------------------------------------------------
    /// // Create adapter with a proxy
    /// // -----------------------------------------------------------------
    /// 
    /// // assume IMyStream defines a Read(byte[], int, int) method and a Length
    /// // property and Fabricon.DataStream is a COM object that defines a Read 
    /// // method and Length property with the same signatures.
    /// var factory = new AdapterFactory{IMyStream}();
    /// 
    /// // add a proxy to intercept all of the calls to the adapter. Assume
    /// // MyProxy is a type you have defined that derives from AdapterProxy
    /// // or implements IAdapterProxy.
    /// factory.AdapterProxy = new MyProxy();
    /// 
    /// // assume dataStream is an instance of type Fabricon.DataStream.
    /// var stream = factory.CreateAdapter(dataStream);
    /// 
    /// // the call to stream.Length will be channeled through the AdapterProxy
    /// // Get(object, string) method. If MyProxy overrides this method, then
    /// // custom action can be performed. Otherwise, the AdapterProxy class
    /// // will call the underlying Length property on the dataStream instance
    /// // using reflection.
    /// var bytes = new byte[stream.Length];
    /// 
    /// // the call to stream.Read will be channeled through the AdapterProxy
    /// // Invoke(object, string, object[]) method. If MyProxy overrides this 
    /// // method, then custom action can be performed. Otherwise, the AdapterProxy 
    /// // class will call the underlying Read method on the dataStream instance
    /// // using reflection.
    /// stream.Read(bytes, 0, bytes.Length);
    /// </code>
    /// </example>
    public class AdapterFactory<T>
        : AdapterFactory
    {
        /// <summary>
        /// Creates and returns a new instance of type <see cref="DynamicAdapter{T}"/>.
        /// </summary>
        public AdapterFactory()
            : base(typeof(T)) { }

        /// <summary>
        /// Creates a proxy to the specified instance.
        /// </summary>
        public new T CreateAdapter(object value)
        {
            return (T)base.CreateAdapter(value);
        }
    }
}
