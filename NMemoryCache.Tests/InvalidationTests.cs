using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NMemoryCache.Tests.Data;

namespace NMemoryCache.Tests
{
    [TestClass]
    public class InvalidationTests
    {
        protected IMemoryCache _cache;

        [TestInitialize]
        public virtual void TestInitialize()
        {
            _cache = new MemoryCache();
        }
    }
}
