using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NMemoryCache.Tests.Data;

namespace NMemoryCache.Tests
{
    [TestClass]
    public class CachingTests
    {
        protected IMemoryCache _cache;

        [TestInitialize]
        public virtual void TestInitialize()
        {
            _cache = new MemoryCache();
        }

        private string _testKey = "TestKey";

        private Entity _testObject = new Entity
        {
            Id = 1,
            Title = "Tests",
            Content = "Test test test...",
            Tags = new[] { "unit", "test" },
        };
        
        [TestMethod, ExpectedException(typeof(ArgumentNullException))]
        public void AddNullKey_ThrowsException()
        {
            _cache.Add(null, _testObject);
        }
        
        [TestMethod, ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void AddNegativeLifetime_ThrowsException()
        {
            _cache.Add(_testKey, TimeSpan.FromSeconds(-1), _testObject);
        }

        [TestMethod, ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void AddZeroLifetime_ThrowsException()
        {
            _cache.Add(_testKey, TimeSpan.Zero, _testObject);
        }
        
        [TestMethod, ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void AddAbsoluteExpirationInThePastThrowsException()
        {
            _cache.Add(_testKey, DateTime.UtcNow.AddSeconds(-1), _testObject);
        }

        [TestMethod, ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void AddAbsoluteExpirationAtThisMoment_ThrowsException()
        {
            _cache.Add(_testKey, DateTime.UtcNow, _testObject);
        }

        [TestMethod]
        public void AddThenGetGeneric_ReturnsValue()
        {
            _cache.Add(_testKey, _testObject);
            Assert.AreSame(_testObject, _cache.Get<Entity>(_testKey));
        }

        [TestMethod]
        public void AddThenGetObject_ReturnsValue()
        {
            _cache.Add(_testKey, _testObject);
            Assert.AreSame(_testObject, _cache.Get<object>(_testKey));
        }

        [TestMethod]
        public void AddWithLifetime_ReturnsValue()
        {
            _cache.Add(_testKey, TimeSpan.FromSeconds(1), _testObject);
            Assert.AreSame(_testObject, _cache.Get<Entity>(_testKey));
        }

        [TestMethod]
        public async Task AddWithLifetimeThatExpires_ReturnsNull()
        {
            _cache.Add(_testKey, TimeSpan.FromMilliseconds(50), _testObject);
            await Task.Delay(100);
            Assert.IsNull(_cache.Get<Entity>(_testKey));
        }

        [TestMethod]
        public void AddWithAbsoluteExpiration_ReturnsValue()
        {
            _cache.Add(_testKey, DateTime.UtcNow.AddSeconds(1), _testObject);
            Assert.AreSame(_testObject, _cache.Get<Entity>(_testKey));
        }

        [TestMethod]
        public async Task AddWithAbsoluteThatExpires_ReturnsNull()
        {
            _cache.Add(_testKey, DateTime.UtcNow.AddMilliseconds(50), _testObject);
            await Task.Delay(100);
            Assert.IsNull(_cache.Get<Entity>(_testKey));
        }

        [TestMethod]
        public void AddWithSliding_ReturnsValue()
        {
            _cache.Add(_testKey, true, TimeSpan.FromSeconds(1), _testObject);
            Assert.AreSame(_testObject, _cache.Get<Entity>(_testKey));
        }

        [TestMethod]
        public async Task AddWithSlidingThatExpires_ReturnsNull()
        {
            _cache.Add(_testKey, true, TimeSpan.FromMilliseconds(50), _testObject);
            await Task.Delay(100);
            Assert.IsNull(_cache.Get<Entity>(_testKey));
        }

        [TestMethod, ExpectedException(typeof(ArgumentNullException))]
        public void GetNullKey_ThrowsException()
        {
            _cache.Get<object>(null);
        }

        [TestMethod]
        public void GetCachedNullableStructTypeParam_ReturnsType()
        {
            DateTime? cached = new DateTime();
            _cache.Add(_testKey, cached);
            Assert.AreEqual(cached.Value, _cache.Get<DateTime>(_testKey));
        }

        [TestMethod]
        public async Task GetFromCacheTwiceAtSameTime_OnlyAddsOnce()
        {
            int times = 0;

            Task t1 = Task.Factory.StartNew(() =>
            {
                _cache.GetOrAdd(_testKey, () =>
                {
                    Interlocked.Increment(ref times);
                    return new DateTime(2001, 01, 01);
                });
            });

            Task t2 = Task.Factory.StartNew(() =>
            {
                _cache.GetOrAdd(_testKey, () =>
                {
                    Interlocked.Increment(ref times);
                    return new DateTime(2001, 01, 01);
                });
            });

            await Task.WhenAll(t1, t2);

            Assert.AreEqual(1, times);
        }

        [TestMethod]
        public async Task GetOrAdd_FollowinGetOrAddAsync_ReturnsTheFirstObject_AndUnwrapsTheFirstTask()
        {
            Func<Task<Entity>> fetchAsync = () => Task.FromResult(_testObject);
            Func<Entity> fetchSync = () => new Entity();

            Entity actualAsync = await _cache.GetOrAddAsync(_testKey, fetchAsync);
            Entity actualSync = _cache.GetOrAdd(_testKey, fetchSync);

            Assert.IsNotNull(actualAsync);
            Assert.AreEqual(_testObject, actualAsync);

            Assert.IsNotNull(actualSync);
            Assert.AreEqual(_testObject, actualSync);

            Assert.AreEqual(actualAsync, actualSync);
        }

        [TestMethod]
        public async Task GetOrAddAsync_FollowinGetOrAdd_ReturnsTheFirstObject_AndIgnoresTheSecondTask()
        {
            Func<Task<Entity>> fetchAsync = () => Task.FromResult(new Entity());
            Func<Entity> fetchSync = () => _testObject;

            Entity actualSync = _cache.GetOrAdd(_testKey, fetchSync);
            Entity actualAsync = await _cache.GetOrAddAsync(_testKey, fetchAsync);

            Assert.IsNotNull(actualAsync);
            Assert.AreEqual(_testObject, actualAsync);

            Assert.IsNotNull(actualSync);
            Assert.AreEqual(_testObject, actualSync);

            Assert.AreEqual(actualAsync, actualSync);
        }
    }
}
