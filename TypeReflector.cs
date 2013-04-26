using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Adapters
{
    internal sealed class TypeReflector
    {
        private const BindingFlags BINDING_FLAGS = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy;

        public TypeReflector (Type type)
        {
            ReflectedType = type;
        }

        public Type ReflectedType
        {
            get;
            private set;
        }

        public FieldInfo GetField (string fieldName)
        {
            return ReflectedType.GetField(fieldName, BINDING_FLAGS);
        }

        public PropertyInfo GetProperty (string propertyName)
        {
            return ReflectedType.GetProperty(propertyName, BINDING_FLAGS);
        }

        public ConstructorInfo GetConstructor (IEnumerable<Type> argTypes)
        {
            var comparer = new TypeComparer();

            return ReflectedType.GetConstructors(BINDING_FLAGS)
                         .Where(c => comparer.Equals(c.GetParameters()
                                                      .Select(p => p.ParameterType), argTypes))
                         .Union(ReflectedType.GetConstructors(BINDING_FLAGS)
                                      .Where(c => comparer.Assignable(c.GetParameters()
                                                                       .Select(p => p.ParameterType), argTypes)))
                         .FirstOrDefault();
        }

        public MethodInfo GetMethod (string methodName)
        {
            return GetMethod(methodName, Type.EmptyTypes);
        }

        public MethodInfo GetMethod (string methodName, IEnumerable<Type> argTypes)
        {
            var matches = ReflectedType.GetMethods(BINDING_FLAGS)
                                .Where(m => m.Name.Equals(methodName))
                                .ToArray();

            var comparer = new TypeComparer();

            return matches.Count() == 1
                   && !argTypes.Any()
                   ? matches.First()
                   : matches.Where(m => comparer.Equals(m.GetParameters()
                                                         .Select(p => p.ParameterType), argTypes))
                            .Union(matches.Where(m => comparer.Assignable(m.GetParameters()
                                                                           .Select(p => p.ParameterType), argTypes)))
                            .FirstOrDefault();
        }

        public PropertyInfo GetIndexer (IEnumerable<Type> argTypes)
        {
            var matches = ReflectedType.GetProperties(BINDING_FLAGS)
                              .Where(p => p.Name.Equals("Item"));

            var comparer = new TypeComparer();

            return matches.Where(m => comparer.Equals(m.GetIndexParameters()
                                                       .Select(p => p.ParameterType), argTypes))
                          .Union(matches.Where(m => comparer.Assignable(m.GetIndexParameters()
                                                                         .Select(p => p.ParameterType), argTypes)))
                          .FirstOrDefault();
        }

        public EventInfo GetEvent (string eventName)
        {
            return ReflectedType.GetEvent(eventName, BINDING_FLAGS);
        }
    }
}

