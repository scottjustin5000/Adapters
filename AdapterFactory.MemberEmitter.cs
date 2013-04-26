using System.Reflection;
using System.Reflection.Emit;

namespace Adapters
{
    partial class AdapterFactory
    {
        private abstract class MemberEmitter<TMember, TBuilder>
        {
            protected MemberEmitter(TypeBuilder typeBuilder, FieldInfo implField)
            {
                TypeBuilder = typeBuilder;
                ImplField = implField;
            }

            protected TypeBuilder TypeBuilder
            {
                get;
                private set;
            }

            protected FieldInfo ImplField
            {
                get;
                private set;
            }

            public abstract TBuilder Emit(TMember member);
        }
    }
}
