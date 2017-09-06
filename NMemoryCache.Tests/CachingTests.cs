using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NMemoryCache.Tests.Data;

namespace NMemoryCache.Tests
{
    // TODO: replace
    using ComplexTestObject = Entity;

    [TestClass]
    public class CachingTests
    {
        // TODO: replace
        private IMemoryCache sut => _cache;
        private object TestKey => _testKey;
        private Entity testObject => _testObject;

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
        public void AddAbsoluteExpirationInThePast_ThrowsException()
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
            Entity actualAsync = await _cache.GetOrAddAsync(
                _testKey, () => Task.FromResult(_testObject));

            Entity actualSync = _cache.GetOrAdd(_testKey, () => new Entity());

            Assert.IsNotNull(actualAsync);
            Assert.AreEqual(_testObject, actualAsync);

            Assert.IsNotNull(actualSync);
            Assert.AreEqual(_testObject, actualSync);

            Assert.AreEqual(actualAsync, actualSync);
        }

        [TestMethod]
        public async Task GetOrAddAsync_FollowinGetOrAdd_ReturnsTheFirstObject_AndIgnoresTheSecondTask()
        {
            Entity actualSync = _cache.GetOrAdd(_testKey, () => _testObject);

            Entity actualAsync = await _cache.GetOrAddAsync(
                _testKey, () => Task.FromResult(new Entity()));

            Assert.IsNotNull(actualAsync);
            Assert.AreEqual(_testObject, actualAsync);

            Assert.IsNotNull(actualSync);
            Assert.AreEqual(_testObject, actualSync);

            Assert.AreEqual(actualAsync, actualSync);
        }

        [TestMethod]
        public void GetOrAdd_ThenGet_ReturnsCorrectType()
        {
            _cache.GetOrAdd(_testKey, () => _testObject);
            var actual = _cache.Get<Entity>(_testKey);
            Assert.IsNotNull(actual);
            Assert.AreEqual(_testObject, actual);
        }

        [TestMethod]
        public void GetOrAdd_ThenGetValue_ReturnsCorrectType()
        {
            _cache.GetOrAdd(_testKey, () => 123);
            var actual = _cache.Get<int>(_testKey);
            Assert.AreEqual(123, actual);
        }

        [TestMethod, ExpectedException(typeof(InvalidCastException))]
        public void GetOrAdd_ThenGetWrongType_ThrowsException()
        {
            _cache.GetOrAdd(_testKey, () => _testObject);
            var actual = _cache.Get<ApplicationException>(_testKey);
        }

        [TestMethod]
        public async Task GetOrAddAsync_ThenGetAsync_ReturnsCorrectType()
        {
            await _cache.GetOrAddAsync(_testKey, () => Task.FromResult(_testObject));
            var actual = await _cache.GetAsync<Entity>(_testKey);
            Assert.IsNotNull(actual);
            Assert.AreEqual(_testObject, actual);
        }

        [TestMethod]
        public async Task GetOrAddAsync_ThenGet_ReturnsCorrectType()
        {
            await _cache.GetOrAddAsync(_testKey, () => Task.FromResult(_testObject));
            var actual = _cache.Get<Entity>(_testKey);
            Assert.IsNotNull(actual);
            Assert.AreEqual(_testObject, actual);
        }

        [TestMethod, ExpectedException(typeof(InvalidCastException))]
        public async Task GetOrAddAsync_ThenGetAsyncWrongType_ThrowsException()
        {
            await _cache.GetOrAddAsync(_testKey, () => Task.FromResult(_testObject));
            var actual = await _cache.GetAsync<ApplicationException>(_testKey);
        }
        
        [TestMethod, ExpectedException(typeof(InvalidCastException))]
        public async Task GetOrAddAsync_ThenGetWrongType_ThrowsException()
        {
            await _cache.GetOrAddAsync(_testKey, () => Task.FromResult(_testObject));
            var actual = _cache.Get<ApplicationException>(_testKey);
        }        
    }
}
