using System;
using System.Threading;

namespace NMemoryCache
{
    internal interface ILazyValue { }

    internal class LazyValue<T> : Lazy<T>, ILazyValue
    {
        public LazyValue(Func<T> valueFactory)
            : base(valueFactory, LazyThreadSafetyMode.ExecutionAndPublication)
        {
        }
    }
}
