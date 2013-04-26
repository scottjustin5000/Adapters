using System;
using System.Collections.Generic;
using System.Linq;

namespace Adapters
{
    internal class TypeComparer : IEqualityComparer<IEnumerable<Type>>
    {
        static TypeComparer ()
        {
            Default = new TypeComparer();
        }

        public static TypeComparer Default
        {
            get;
            private set;
        }

        public bool Equals (IEnumerable<Type> first, IEnumerable<Type> second)
        {
            var firstArray = first.ToArray();
            var secondArray = second.ToArray();

            bool equals = false;

            if (firstArray.Length == secondArray.Length)
            {
                equals = true;

                for (int i = 0; i < firstArray.Length; i++)
                {
                    if (firstArray[i] != secondArray[i])
                    {
                        equals = false;
                        break;
                    }
                }
            }

            return equals;
        }

        public bool Assignable (IEnumerable<Type> first, IEnumerable<Type> second)
        {
            var firstArray = first.ToArray();
            var secondArray = second.ToArray();

            bool assignable = false;

            if (firstArray.Length == secondArray.Length)
            {
                assignable = true;

                for (int i = 0; i < firstArray.Length; i++)
                {
                    if (!firstArray[i].IsAssignableFrom(secondArray[i]))
                    {
                        assignable = false;
                        break;
                    }
                }
            }

            return assignable;
        }

        public int GetHashCode (IEnumerable<Type> types)
        {
            return types.GetHashCode();
        }
    }
}
