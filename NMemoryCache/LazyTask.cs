using System;
using System.Threading;
using System.Threading.Tasks;

namespace NMemoryCache
{
    internal interface ILazyTask { }

    internal class LazyTask<T> : Lazy<Task<T>>, ILazyTask
    {
        public LazyTask(Func<Task<T>> taskFactory)
            : base(() => Task.Factory.StartNew(taskFactory).Unwrap(),
                   LazyThreadSafetyMode.ExecutionAndPublication)
        {
        }
    }
}
