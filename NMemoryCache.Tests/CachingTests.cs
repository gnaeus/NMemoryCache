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

        private string _testKey = "CacheKey";

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

        [TestMethod]
        public async Task GetOrAddAsync_WillAddOnFirstCall()
        {
            int times = 0;

            DateTime actual = await _cache.GetOrAddAsync(_testKey, () =>
            {
                times++;
                return Task.FromResult(new DateTime(2001, 01, 01));
            });

            Assert.AreEqual(2001, actual.Year);
            Assert.AreEqual(1, times);
        }

        [TestMethod]
        public async Task GetOrAddAsync_WillAddOnFirstCall_ButReturnCachedOnSecond()
        {
            int times = 0;

            DateTime actualFirst = await _cache.GetOrAdd(_testKey, () =>
            {
                times++;
                return Task.FromResult(new DateTime(2001, 01, 01));
            });

            DateTime actualSecond = await _cache.GetOrAdd(_testKey, () =>
            {
                times++;
                return Task.FromResult(new DateTime(2002, 01, 01));
            });

            Assert.AreEqual(2001, actualFirst.Year);
            Assert.AreEqual(2001, actualSecond.Year);
            Assert.AreEqual(1, times);
        }

        [TestMethod]
        public async Task GetOrAddAsync_WillNotAdd_IfExistingData()
        {
            int times = 0;

            _cache.Add(_testKey, new DateTime(1999, 01, 01));

            DateTime actual = await _cache.GetOrAddAsync(_testKey, () =>
            {
                times++;
                return Task.FromResult(new DateTime(2001, 01, 01));
            });

            Assert.AreEqual(1999, actual.Year);
            Assert.AreEqual(0, times);
        }

        [TestMethod]
        public async Task GetOrAddAsync_WithExpiration_WillAdd_AndReturnCached()
        {
            DateTime actualFirst = await _cache.GetOrAddAsync(
                _testKey, DateTime.Now.AddSeconds(5),
                () => Task.FromResult(new DateTime(2001, 01, 01)));

            DateTime actualSecond = await _cache.GetAsync<DateTime>(_testKey);

            Assert.AreEqual(2001, actualFirst.Year);
            Assert.AreEqual(2001, actualSecond.Year);
        }

        [TestMethod]
        public void GetOrAdd_WillAddOnFirstCall()
        {
            int times = 0;

            DateTime actual = _cache.GetOrAdd(_testKey, () =>
            {
                times++;
                return new DateTime(2001, 01, 01);
            });

            Assert.AreEqual(2001, actual.Year);
            Assert.AreEqual(1, times);
        }

        [TestMethod]
        public void GetOrAdd_WillAddOnFirstCall_ButReturnCachedOnSecond()
        {
            int times = 0;

            var actualFirst = _cache.GetOrAdd(_testKey, () =>
            {
                times++;
                return new DateTime(2001, 01, 01);
            });

            var actualSecond = _cache.GetOrAdd(_testKey, () =>
            {
                times++;
                return new DateTime(2002, 01, 01);
            });

            Assert.AreEqual(2001, actualFirst.Year);
            Assert.AreEqual(2001, actualSecond.Year);
            Assert.AreEqual(1, times);
        }

        [TestMethod]
        public void GetOrAdd_WillNotAdd_IfExistingData()
        {
            int times = 0;

            _cache.Add(_testKey, new DateTime(1999, 01, 01));

            var actual = _cache.GetOrAdd(_testKey, () =>
            {
                times++;
                return new DateTime(2001, 01, 01);
            });

            Assert.AreEqual(1999, actual.Year);
            Assert.AreEqual(0, times);
        }

        [TestMethod]
        public void GetOrAdd_WithExpiration_WillAdd_AndReturnCached()
        {
            DateTime actualFirst = _cache.GetOrAdd(
                _testKey, DateTime.Now.AddSeconds(5),
                () => new DateTime(2001, 01, 01));

            DateTime actualSecond = _cache.Get<DateTime>(_testKey);

            Assert.AreEqual(2001, actualFirst.Year);
            Assert.AreEqual(2001, actualSecond.Year);
        }

        [TestMethod]
        public void GetWithClassTypeParam_ReturnsType()
        {
            var cached = new EventArgs();
            _cache.Add(_testKey, cached);
            Assert.AreEqual(cached, _cache.Get<EventArgs>(_testKey));
        }

        [TestMethod]
        public void GetWithInt_RetunsDefault_IfNotCached()
        {
            Assert.AreEqual(default(int), _cache.Get<int>(_testKey));
        }

        [TestMethod]
        public void GetWithNullableInt_RetunsCachedNonNullableInt()
        {
            const int cached = 123;
            _cache.Add(_testKey, cached);
            Assert.AreEqual(cached, _cache.Get<int?>(_testKey));
        }

        [TestMethod]
        public void GetWithNullableStructTypeParam_ReturnsType()
        {
            var cached = new DateTime();
            _cache.Add(_testKey, cached);
            Assert.AreEqual(cached, _cache.Get<DateTime?>(_testKey));
        }

        [TestMethod]
        public void GetWithStructTypeParam_ReturnsType()
        {
            var cached = new DateTime(2000, 1, 1);
            _cache.Add(_testKey, cached);
            Assert.AreEqual(cached, _cache.Get<DateTime>(_testKey));
        }

        [TestMethod]
        public void GetWithValueTypeParam_ReturnsType()
        {
            const int cached = 3;
            _cache.Add(_testKey, cached);
            Assert.AreEqual(3, _cache.Get<int>(_testKey));
        }

        [TestMethod, ExpectedException(typeof(InvalidCastException))]
        public void GetWithWrongClassTypeParam_ThrowsException()
        {
            _cache.Add(_testKey, new EventArgs());
            _cache.Get<ArgumentNullException>(_testKey);
        }

        [TestMethod, ExpectedException(typeof(InvalidCastException))]
        public void GetWithWrongStructTypeParam_ThrowsException()
        {
            _cache.Add(_testKey, new DateTime());
            _cache.Get<TimeSpan>(_testKey);
        }

        [TestMethod]
        public void RemovedItem_CannotBeRetrieved_FromCache()
        {
            _cache.Add(_testKey, new object());
            Assert.IsNotNull(_cache.Get<object>(_testKey));

            _cache.Remove(_testKey);
            Assert.IsNull(_cache.Get<object>(_testKey));
        }

        [TestMethod, ExpectedException(typeof(ApplicationException))]
        public async Task GetOrAddAsync_FaultedTask_ThrowsException()
        {
            await _cache.GetOrAddAsync<Entity>(_testKey, async () =>
            {
                await Task.Yield();
                throw new ApplicationException();
            });
        }

        [TestMethod]
        public async Task GetOrAddAsync_FaultedTask_DoesNotCacheIt()
        {
            try
            {
                await _cache.GetOrAddAsync<Entity>(_testKey, async () =>
                {
                    await Task.Yield();
                    throw new ApplicationException();
                });
            }
            catch (ApplicationException) { }

            Entity stillCached = await _cache.GetAsync<Entity>(_testKey);

            Assert.IsNull(stillCached);
        }

        [TestMethod]
        public void GetOrAddAsync_FaultedTask_ReturnsTaskToConsumer()
        {
            var faultedTask = _cache.GetOrAddAsync<Entity>(_testKey, async () =>
            {
                await Task.Yield();
                throw new ApplicationException();
            });

            try
            {
                faultedTask.Wait();
            }
            catch (AggregateException) { }

            Assert.IsNotNull(faultedTask);
            Assert.IsTrue(faultedTask.IsFaulted);
        }

        [TestMethod, ExpectedException(typeof(TaskCanceledException))]
        public async Task GetOrAddAsync_CancelledTask_ThrowsException()
        {
            await _cache.GetOrAddAsync(_testKey, () =>
            {
                var tcs = new TaskCompletionSource<Entity>();
                tcs.SetCanceled();
                return tcs.Task;
            });
        }

        [TestMethod]
        public async Task GetOrAddAsync_CancelledTask_DoesNotCacheIt()
        {
            try
            {
                await _cache.GetOrAddAsync(_testKey, () =>
                {
                    var tcs = new TaskCompletionSource<Entity>();
                    tcs.SetCanceled();
                    return tcs.Task;
                });
            }
            catch (TaskCanceledException) { }

            Entity stillCached = await _cache.GetAsync<Entity>(_testKey);

            Assert.IsNull(stillCached);
        }

        [TestMethod]
        public void GetOrAddAsync_CancelledTask_ReturnsTaskToConsumer()
        {
            var cancelledTask = _cache.GetOrAddAsync(_testKey, () =>
            {
                var tcs = new TaskCompletionSource<Entity>();
                tcs.SetCanceled();
                return tcs.Task;
            });

            try
            {
                cancelledTask.Wait();
            }
            catch (AggregateException) { }

            Assert.IsNotNull(cancelledTask);
            Assert.IsTrue(cancelledTask.IsCanceled);
        }

        [TestMethod, Timeout(1000)]
        public void GetOrAddAsync_WithLongTask_ReturnsBeforeTaskCompletes()
        {
            var incompleteTask = _cache.GetOrAddAsync(_testKey, () =>
            {
                Thread.Sleep(TimeSpan.FromSeconds(30));
                return Task.FromResult(_testObject);
            });

            Assert.IsNotNull(incompleteTask);
            Assert.IsFalse(incompleteTask.IsCompleted);
        }
    }
}
