using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NMemoryCache.Tests.ConcurrentTags
{
    [TestClass]
    public class ConcurrentTagsInvalidationTests : InvalidationTests
    {
        [TestInitialize]
        public override void TestInitialize()
        {
            _cache = new NMemoryCache.ConcurrentTags.MemoryCache();
        }
    }
}
