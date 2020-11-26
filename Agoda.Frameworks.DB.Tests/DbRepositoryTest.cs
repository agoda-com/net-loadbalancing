using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Moq;
using Moq.Dapper;
using NUnit.Framework;

namespace Agoda.Frameworks.DB.Tests
{
    public class DbRepositoryTest
    {
        protected IDbRepository _db;
        protected Mock<IDbResourceManager> _dbResources;
        protected Mock<IDbCache> _cache;
        protected Mock<IDbConnection> _connection;
        protected string[] _expectedRows;
        protected object _cacheRows;
        protected readonly string _expectedCacheKey = "db.v1.sp_foo:@param1+value1&@param2+value2&";
        protected List<DbErrorEventArgs> _onErrorEvents;
        protected List<QueryCompleteEventArgs> _onQueryCompleteEvents;

        [SetUp]
        public void SetUp()
        {
            _expectedRows = new[] { "row1", "row2" };
            _cacheRows = new[] { "cache_row_1", "cache_row_2" };
            _cache = new Mock<IDbCache>();
            _cache.Setup(x => x.TryGetValue(It.IsAny<string>(), out _cacheRows))
                .Returns(false);
            _connection = new Mock<IDbConnection>();
            var sp = new FakeStoredProc();
            _connection.SetupDapper(
                    c => c.Query<string>(
                        sp.StoredProcedureName,
                        "foo",
                        null,
                        true,
                        sp.CommandTimeoutSecs,
                        CommandType.StoredProcedure))
                .Returns(_expectedRows);
            _dbResources = new Mock<IDbResourceManager>();
            _dbResources.Setup(x => x.ChooseDb(sp.DbName).SelectRandomly())
                .Returns("connection_str");
            _onErrorEvents = new List<DbErrorEventArgs>();
            _onQueryCompleteEvents = new List<QueryCompleteEventArgs>();
            _db = new DbRepository(
                _dbResources.Object,
                _cache.Object,
                _ => _connection.Object);
            _db.OnError += (sender, args) => _onErrorEvents.Add(args);
            _db.OnQueryComplete += (sender, args) => _onQueryCompleteEvents.Add(args);
        }

        protected void SetupAsync()
        {
            // We can only call either SetupDapper or SetupDapperAsync once.
            // Testing async methods helped me to find a bug of Cache.CreateOrGet.
            // Although it's still stupid copy-paste of code.
            var sp = new FakeStoredProc();
            _connection.SetupDapperAsync(
                    c => c.QueryAsync<string>(
                        sp.StoredProcedureName,
                        "foo",
                        null,
                        sp.CommandTimeoutSecs,
                        CommandType.StoredProcedure))
                .ReturnsAsync(_expectedRows);
        }

        [Test]
        public void Query_Hit_Cache()
        {
            _cache.Setup(x => x.TryGetValue(It.IsAny<string>(), out _cacheRows))
                .Returns(true);
            var result = _db.Query(new FakeStoredProc
            {
                CacheLifetime = TimeSpan.MaxValue
            }, "foo");
            Assert.AreEqual(_cacheRows, result);

            _cache.Verify(
                x => x.TryGetValue(_expectedCacheKey, out _cacheRows),
                Times.Once);
            _dbResources.Verify(x => x.ChooseDb("mobile_ro").SelectRandomly(), Times.Never);

            Assert.AreEqual(0, _onQueryCompleteEvents.Count);
        }

        [Test]
        public void Query_Hit_Cache_By_Provide_Key_By_Client()
        {
            var clientKey = "client-key";

            _cache.Setup(x => x.TryGetValue(It.IsAny<string>(), out _cacheRows))
                .Returns(true);
            var result = _db.Query(new FakeStoredProc
            {
                CacheLifetime = TimeSpan.MaxValue
            }, "foo", clientKey);

            Assert.AreEqual(_cacheRows, result);

            _cache.Verify(
                x => x.TryGetValue(clientKey, out _cacheRows),
                Times.Once);
            _dbResources.Verify(x => x.ChooseDb("mobile_ro").SelectRandomly(), Times.Never);

            Assert.AreEqual(0, _onQueryCompleteEvents.Count);
        }

        [Test]
        public void Query_No_Cache_Lifetime()
        {
            _cache.Setup(x => x.TryGetValue(It.IsAny<string>(), out _cacheRows))
                .Returns(true);
            var result = _db.Query(new FakeStoredProc(), "foo");
            Assert.AreEqual(_expectedRows, result);

            _cache.Verify(
                x => x.TryGetValue(It.IsAny<string>(), out _cacheRows),
                Times.Never);
            _dbResources.Verify(x => x.ChooseDb("mobile_ro").SelectRandomly(), Times.Once);

            Assert.AreEqual(1, _onQueryCompleteEvents.Count);
            Assert.IsInstanceOf<FakeStoredProc>(_onQueryCompleteEvents[0].StoredProc);
            Assert.IsNull(_onQueryCompleteEvents[0].Error);
        }

        [Test]
        public void Query_Missed_Cache()
        {
            var result = _db.Query(new FakeStoredProc
            {
                CacheLifetime = TimeSpan.MaxValue
            }, "foo");
            Assert.AreEqual(_expectedRows, result);

            _cache.Verify(
                x => x.TryGetValue(_expectedCacheKey, out _cacheRows),
                Times.Once);
            _dbResources.Verify(x => x.ChooseDb("mobile_ro").SelectRandomly(), Times.Once);

            Assert.AreEqual(1, _onQueryCompleteEvents.Count);
            Assert.IsInstanceOf<FakeStoredProc>(_onQueryCompleteEvents[0].StoredProc);
            Assert.IsNull(_onQueryCompleteEvents[0].Error);
        }

        [Test]
        public void Query_Retry_Success()
        {
            var attemptCount = 0;
            _db = new DbRepository(
                _dbResources.Object,
                _cache.Object,
                _ =>
                {
                    attemptCount++;
                    if (attemptCount < 2)
                    {
                        throw new InvalidOperationException();
                    }
                    return _connection.Object;
                });
            _db.OnError += (sender, args) => _onErrorEvents.Add(args);
            _db.OnQueryComplete += (sender, args) => _onQueryCompleteEvents.Add(args);

            var result = _db.Query(new FakeStoredProc(), "foo");
            Assert.AreEqual(_expectedRows, result);

            Assert.AreEqual(2, attemptCount);
            _dbResources.Verify(x => x.ChooseDb("mobile_ro").SelectRandomly(), Times.Exactly(2));

            Assert.AreEqual(1, _onErrorEvents.Count);
            Assert.IsInstanceOf<InvalidOperationException>(_onErrorEvents[0].Error);
            Assert.AreEqual(1, _onErrorEvents[0].AttemptCount);

            Assert.AreEqual(2, _onQueryCompleteEvents.Count);
            Assert.IsInstanceOf<FakeStoredProc>(_onQueryCompleteEvents[0].StoredProc);
            Assert.IsInstanceOf<FakeStoredProc>(_onQueryCompleteEvents[1].StoredProc);
            Assert.IsNotNull(_onQueryCompleteEvents[0].Error);
            Assert.IsNull(_onQueryCompleteEvents[1].Error);
        }

        [Test]
        public void Query_Retry_Failure()
        {
            var attemptCount = 0;
            _db = new DbRepository(
                _dbResources.Object,
                _cache.Object,
                _ =>
                {
                    attemptCount++;
                    throw new InvalidOperationException();
                });
            _db.OnError += (sender, args) => _onErrorEvents.Add(args);
            _db.OnQueryComplete += (sender, args) => _onQueryCompleteEvents.Add(args);

            Assert.Throws<InvalidOperationException>(() =>
            {
                _db.Query(new FakeStoredProc(), "foo");
            });

            Assert.AreEqual(2, attemptCount);
            _dbResources.Verify(x => x.ChooseDb("mobile_ro").SelectRandomly(), Times.Exactly(2));

            Assert.AreEqual(2, _onErrorEvents.Count);
            Assert.IsInstanceOf<InvalidOperationException>(_onErrorEvents[0].Error);
            Assert.IsInstanceOf<InvalidOperationException>(_onErrorEvents[1].Error);
            Assert.AreEqual(1, _onErrorEvents[0].AttemptCount);
            Assert.AreEqual(2, _onErrorEvents[1].AttemptCount);

            Assert.AreEqual(2, _onQueryCompleteEvents.Count);
            Assert.IsInstanceOf<FakeStoredProc>(_onQueryCompleteEvents[0].StoredProc);
            Assert.IsInstanceOf<FakeStoredProc>(_onQueryCompleteEvents[1].StoredProc);
            Assert.IsNotNull(_onQueryCompleteEvents[0].Error);
            Assert.IsNotNull(_onQueryCompleteEvents[1].Error);
        }

        [Test]
        public void ExecuteNonQuery_Success()
        {
            var result = _db.ExecuteNonQuery(new FakeNonQueryStoredProc(), "foo");
            // Setup for non-query is not supported
            Assert.AreEqual(0, result);

            _cache.Verify(
                x => x.TryGetValue(_expectedCacheKey, out _cacheRows),
                Times.Never);
            _dbResources.Verify(x => x.ChooseDb("mobile_ro").SelectRandomly(), Times.Once);

            Assert.AreEqual(1, _onQueryCompleteEvents.Count);
            Assert.IsInstanceOf<FakeNonQueryStoredProc>(_onQueryCompleteEvents[0].StoredProc);
            Assert.IsNull(_onQueryCompleteEvents[0].Error);
        }

        [Ignore("Unable to mock. Integration test is needed.")]
        // [Test]
        public void ExecuteReader_Test()
        {
            // Ignored
        }

        [Ignore("Unable to mock. Integration test is needed.")]
        // [Test]
        public void ExecuteReaderAsync_Test()
        {
            // Ignored
        }

        [Test]
        public void QueryMultiple_Hit_Cache()
        {
            object cacheResult = "cache_result";
            _cache.Setup(x => x.TryGetValue(It.IsAny<string>(), out cacheResult))
                .Returns(true);
            var result = _db.QueryMultiple(new FakeMultipleStoredProc
            {
                CacheLifetime = TimeSpan.MaxValue
            }, "foo");
            Assert.AreEqual(cacheResult, result);

            _cache.Verify(
                x => x.TryGetValue(_expectedCacheKey, out _cacheRows),
                Times.Once);
            _dbResources.Verify(x => x.ChooseDb("mobile_ro").SelectRandomly(), Times.Never);

            Assert.AreEqual(0, _onQueryCompleteEvents.Count);
        }

        [Test]
        public void QueryMultiple_No_Cache_Lifetime()
        {
            _cache.Setup(x => x.TryGetValue(It.IsAny<string>(), out _cacheRows))
                .Returns(true);
            var result = _db.QueryMultiple(new FakeMultipleStoredProc(), "foo");
            Assert.AreEqual("multiple_result", result);

            _cache.Verify(
                x => x.TryGetValue(It.IsAny<string>(), out _cacheRows),
                Times.Never);
            _dbResources.Verify(x => x.ChooseDb("mobile_ro").SelectRandomly(), Times.Once);

            Assert.AreEqual(1, _onQueryCompleteEvents.Count);
            Assert.IsInstanceOf<FakeMultipleStoredProc>(_onQueryCompleteEvents[0].StoredProc);
            Assert.IsNull(_onQueryCompleteEvents[0].Error);
        }

        [Test]
        public void QueryMultiple_Missed_Cache()
        {
            var result = _db.QueryMultiple(new FakeMultipleStoredProc
            {
                CacheLifetime = TimeSpan.MaxValue
            }, "foo");
            Assert.AreEqual("multiple_result", result);

            _cache.Verify(
                x => x.TryGetValue(_expectedCacheKey, out _cacheRows),
                Times.Once);
            _dbResources.Verify(x => x.ChooseDb("mobile_ro").SelectRandomly(), Times.Once);

            Assert.AreEqual(1, _onQueryCompleteEvents.Count);
            Assert.IsInstanceOf<FakeMultipleStoredProc>(_onQueryCompleteEvents[0].StoredProc);
            Assert.IsNull(_onQueryCompleteEvents[0].Error);
        }

        [Test]
        public void QueryMultiple_Retry_Success()
        {
            var attemptCount = 0;
            _db = new DbRepository(
                _dbResources.Object,
                _cache.Object,
                _ =>
                {
                    attemptCount++;
                    if (attemptCount < 2)
                    {
                        throw new InvalidOperationException();
                    }
                    return _connection.Object;
                });
            _db.OnError += (sender, args) => _onErrorEvents.Add(args);
            _db.OnQueryComplete += (sender, args) => _onQueryCompleteEvents.Add(args);

            var result = _db.QueryMultiple(new FakeMultipleStoredProc(), "foo");
            Assert.AreEqual("multiple_result", result);

            Assert.AreEqual(2, attemptCount);
            _dbResources.Verify(x => x.ChooseDb("mobile_ro").SelectRandomly(), Times.Exactly(2));

            Assert.AreEqual(1, _onErrorEvents.Count);
            Assert.IsInstanceOf<InvalidOperationException>(_onErrorEvents[0].Error);
            Assert.AreEqual(1, _onErrorEvents[0].AttemptCount);

            Assert.AreEqual(2, _onQueryCompleteEvents.Count);
            Assert.IsInstanceOf<FakeMultipleStoredProc>(_onQueryCompleteEvents[0].StoredProc);
            Assert.IsInstanceOf<FakeMultipleStoredProc>(_onQueryCompleteEvents[1].StoredProc);
            Assert.IsNotNull(_onQueryCompleteEvents[0].Error);
            Assert.IsNull(_onQueryCompleteEvents[1].Error);
        }

        [Test]
        public void QueryMultiple_Retry_Failure()
        {
            var attemptCount = 0;
            _db = new DbRepository(
                _dbResources.Object,
                _cache.Object,
                _ =>
                {
                    attemptCount++;
                    throw new InvalidOperationException();
                });
            _db.OnError += (sender, args) => _onErrorEvents.Add(args);
            _db.OnQueryComplete += (sender, args) => _onQueryCompleteEvents.Add(args);

            Assert.Throws<InvalidOperationException>(() =>
            {
                _db.QueryMultiple(new FakeMultipleStoredProc(), "foo");
            });

            Assert.AreEqual(2, attemptCount);
            _dbResources.Verify(x => x.ChooseDb("mobile_ro").SelectRandomly(), Times.Exactly(2));

            Assert.AreEqual(2, _onErrorEvents.Count);
            Assert.IsInstanceOf<InvalidOperationException>(_onErrorEvents[0].Error);
            Assert.IsInstanceOf<InvalidOperationException>(_onErrorEvents[1].Error);
            Assert.AreEqual(1, _onErrorEvents[0].AttemptCount);
            Assert.AreEqual(2, _onErrorEvents[1].AttemptCount);

            Assert.AreEqual(2, _onQueryCompleteEvents.Count);
            Assert.IsInstanceOf<FakeMultipleStoredProc>(_onQueryCompleteEvents[0].StoredProc);
            Assert.IsInstanceOf<FakeMultipleStoredProc>(_onQueryCompleteEvents[1].StoredProc);
            Assert.IsNotNull(_onQueryCompleteEvents[0].Error);
            Assert.IsNotNull(_onQueryCompleteEvents[1].Error);
        }

        [Test]
        public async Task QueryAsync_Hit_Cache()
        {
            SetupAsync();
            _cache.Setup(x => x.TryGetValue(It.IsAny<string>(), out _cacheRows))
                .Returns(true);
            var result = await _db.QueryAsync(new FakeStoredProc
            {
                CacheLifetime = TimeSpan.MaxValue
            }, "foo");
            Assert.AreEqual(_cacheRows, result);

            _cache.Verify(
                x => x.TryGetValue(_expectedCacheKey, out _cacheRows),
                Times.Once);
            _dbResources.Verify(x => x.ChooseDb("mobile_ro").SelectRandomly(), Times.Never);

            Assert.AreEqual(0, _onQueryCompleteEvents.Count);
        }

        [Test]
        public async Task QueryAsync_No_Cache_Lifetime()
        {
            SetupAsync();
            _cache.Setup(x => x.TryGetValue(It.IsAny<string>(), out _cacheRows))
                .Returns(true);
            var result = await _db.QueryAsync(new FakeStoredProc(), "foo");
            Assert.AreEqual(_expectedRows, result);

            _cache.Verify(
                x => x.TryGetValue(It.IsAny<string>(), out _cacheRows),
                Times.Never);
            _dbResources.Verify(x => x.ChooseDb("mobile_ro").SelectRandomly(), Times.Once);

            Assert.AreEqual(1, _onQueryCompleteEvents.Count);
            Assert.IsInstanceOf<FakeStoredProc>(_onQueryCompleteEvents[0].StoredProc);
            Assert.IsNull(_onQueryCompleteEvents[0].Error);
        }

        [Test]
        public async Task QueryAsync_Missed_Cache()
        {
            SetupAsync();
            var result = await _db.QueryAsync(new FakeStoredProc
            {
                CacheLifetime = TimeSpan.MaxValue
            }, "foo");
            Assert.AreEqual(_expectedRows, result);

            _cache.Verify(
                x => x.TryGetValue(_expectedCacheKey, out _cacheRows),
                Times.Once);
            _dbResources.Verify(x => x.ChooseDb("mobile_ro").SelectRandomly(), Times.Once);

            Assert.AreEqual(1, _onQueryCompleteEvents.Count);
            Assert.IsInstanceOf<FakeStoredProc>(_onQueryCompleteEvents[0].StoredProc);
            Assert.IsNull(_onQueryCompleteEvents[0].Error);
        }

        [Test]
        public async Task QueryAsync_Retry_Success()
        {
            SetupAsync();
            var attemptCount = 0;
            _db = new DbRepository(
                _dbResources.Object,
                _cache.Object,
                _ =>
                {
                    attemptCount++;
                    if (attemptCount < 2)
                    {
                        throw new InvalidOperationException();
                    }
                    return _connection.Object;
                });
            _db.OnError += (sender, args) => _onErrorEvents.Add(args);
            _db.OnQueryComplete += (sender, args) => _onQueryCompleteEvents.Add(args);

            var result = await _db.QueryAsync(new FakeStoredProc(), "foo");
            Assert.AreEqual(_expectedRows, result);

            Assert.AreEqual(2, attemptCount);
            _dbResources.Verify(x => x.ChooseDb("mobile_ro").SelectRandomly(), Times.Exactly(2));

            Assert.AreEqual(1, _onErrorEvents.Count);
            Assert.IsInstanceOf<InvalidOperationException>(_onErrorEvents[0].Error);
            Assert.AreEqual(1, _onErrorEvents[0].AttemptCount);

            Assert.AreEqual(2, _onQueryCompleteEvents.Count);
            Assert.IsInstanceOf<FakeStoredProc>(_onQueryCompleteEvents[0].StoredProc);
            Assert.IsInstanceOf<FakeStoredProc>(_onQueryCompleteEvents[1].StoredProc);
            Assert.IsNotNull(_onQueryCompleteEvents[0].Error);
            Assert.IsNull(_onQueryCompleteEvents[1].Error);
        }

        [Test]
        public void QueryAsync_Retry_Failure()
        {
            SetupAsync();
            var attemptCount = 0;
            _db = new DbRepository(
                _dbResources.Object,
                _cache.Object,
                _ =>
                {
                    attemptCount++;
                    throw new InvalidOperationException();
                });
            _db.OnError += (sender, args) => _onErrorEvents.Add(args);
            _db.OnQueryComplete += (sender, args) => _onQueryCompleteEvents.Add(args);

            Assert.ThrowsAsync<InvalidOperationException>(() =>
            {
                return _db.QueryAsync(new FakeStoredProc(), "foo");
            });

            Assert.AreEqual(2, attemptCount);
            _dbResources.Verify(x => x.ChooseDb("mobile_ro").SelectRandomly(), Times.Exactly(2));

            Assert.AreEqual(2, _onErrorEvents.Count);
            Assert.IsInstanceOf<InvalidOperationException>(_onErrorEvents[0].Error);
            Assert.IsInstanceOf<InvalidOperationException>(_onErrorEvents[1].Error);
            Assert.AreEqual(1, _onErrorEvents[0].AttemptCount);
            Assert.AreEqual(2, _onErrorEvents[1].AttemptCount);

            Assert.AreEqual(2, _onQueryCompleteEvents.Count);
            Assert.IsInstanceOf<FakeStoredProc>(_onQueryCompleteEvents[0].StoredProc);
            Assert.IsInstanceOf<FakeStoredProc>(_onQueryCompleteEvents[1].StoredProc);
            Assert.IsNotNull(_onQueryCompleteEvents[0].Error);
            Assert.IsNotNull(_onQueryCompleteEvents[1].Error);
        }

        [Test]
        public async Task ExecuteNonQueryAsync_Success()
        {
            SetupAsync();

            var result = await _db.ExecuteNonQueryAsync(new FakeNonQueryStoredProc(), "foo");
            // Setup for non-query is not supported
            Assert.AreEqual(0, result);

            _cache.Verify(
                x => x.TryGetValue(_expectedCacheKey, out _cacheRows),
                Times.Never);
            _dbResources.Verify(x => x.ChooseDb("mobile_ro").SelectRandomly(), Times.Once);

            Assert.AreEqual(1, _onQueryCompleteEvents.Count);
            Assert.IsInstanceOf<FakeNonQueryStoredProc>(_onQueryCompleteEvents[0].StoredProc);
            Assert.IsNull(_onQueryCompleteEvents[0].Error);
        }

        [Test]
        public async Task QueryMultipleAsync_Hit_Cache()
        {
            SetupAsync();
            object cacheResult = "cache_result";
            _cache.Setup(x => x.TryGetValue(It.IsAny<string>(), out cacheResult))
                .Returns(true);
            var result = await _db.QueryMultipleAsync(new FakeMultipleStoredProc
            {
                CacheLifetime = TimeSpan.MaxValue
            }, "foo");
            Assert.AreEqual(cacheResult, result);

            _cache.Verify(
                x => x.TryGetValue(_expectedCacheKey, out _cacheRows),
                Times.Once);
            _dbResources.Verify(x => x.ChooseDb("mobile_ro").SelectRandomly(), Times.Never);

            Assert.AreEqual(0, _onQueryCompleteEvents.Count);
        }

        [Test]
        public async Task QueryMultipleAsync_No_Cache_Lifetime()
        {
            SetupAsync();
            _cache.Setup(x => x.TryGetValue(It.IsAny<string>(), out _cacheRows))
                .Returns(true);
            var result = await _db.QueryMultipleAsync(new FakeMultipleStoredProc(), "foo");
            Assert.AreEqual("multiple_result", result);

            _cache.Verify(
                x => x.TryGetValue(It.IsAny<string>(), out _cacheRows),
                Times.Never);
            _dbResources.Verify(x => x.ChooseDb("mobile_ro").SelectRandomly(), Times.Once);

            Assert.AreEqual(1, _onQueryCompleteEvents.Count);
            Assert.IsInstanceOf<FakeMultipleStoredProc>(_onQueryCompleteEvents[0].StoredProc);
            Assert.IsNull(_onQueryCompleteEvents[0].Error);
        }

        [Test]
        public async Task QueryMultipleAsync_Missed_Cache()
        {
            SetupAsync();
            var result = await _db.QueryMultipleAsync(new FakeMultipleStoredProc
            {
                CacheLifetime = TimeSpan.MaxValue
            }, "foo");
            Assert.AreEqual("multiple_result", result);

            _cache.Verify(
                x => x.TryGetValue(_expectedCacheKey, out _cacheRows),
                Times.Once);
            _dbResources.Verify(x => x.ChooseDb("mobile_ro").SelectRandomly(), Times.Once);

            Assert.AreEqual(1, _onQueryCompleteEvents.Count);
            Assert.IsInstanceOf<FakeMultipleStoredProc>(_onQueryCompleteEvents[0].StoredProc);
            Assert.IsNull(_onQueryCompleteEvents[0].Error);
        }

        [Test]
        public async Task QueryMultipleAsync_Retry_Success()
        {
            SetupAsync();
            var attemptCount = 0;
            _db = new DbRepository(
                _dbResources.Object,
                _cache.Object,
                _ =>
                {
                    attemptCount++;
                    if (attemptCount < 2)
                    {
                        throw new InvalidOperationException();
                    }
                    return _connection.Object;
                });
            _db.OnError += (sender, args) => _onErrorEvents.Add(args);
            _db.OnQueryComplete += (sender, args) => _onQueryCompleteEvents.Add(args);

            var result = await _db.QueryMultipleAsync(new FakeMultipleStoredProc(), "foo");
            Assert.AreEqual("multiple_result", result);

            Assert.AreEqual(2, attemptCount);
            _dbResources.Verify(x => x.ChooseDb("mobile_ro").SelectRandomly(), Times.Exactly(2));

            Assert.AreEqual(1, _onErrorEvents.Count);
            Assert.IsInstanceOf<InvalidOperationException>(_onErrorEvents[0].Error);
            Assert.AreEqual(1, _onErrorEvents[0].AttemptCount);

            Assert.AreEqual(2, _onQueryCompleteEvents.Count);
            Assert.IsInstanceOf<FakeMultipleStoredProc>(_onQueryCompleteEvents[0].StoredProc);
            Assert.IsInstanceOf<FakeMultipleStoredProc>(_onQueryCompleteEvents[1].StoredProc);
            Assert.IsNotNull(_onQueryCompleteEvents[0].Error);
            Assert.IsNull(_onQueryCompleteEvents[1].Error);
        }

        [Test]
        public void QueryMultipleAsync_Retry_Failure()
        {
            SetupAsync();
            var attemptCount = 0;
            _db = new DbRepository(
                _dbResources.Object,
                _cache.Object,
                _ =>
                {
                    attemptCount++;
                    throw new InvalidOperationException();
                });
            _db.OnError += (sender, args) => _onErrorEvents.Add(args);
            _db.OnQueryComplete += (sender, args) => _onQueryCompleteEvents.Add(args);

            Assert.ThrowsAsync<InvalidOperationException>(() =>
            {
                return _db.QueryMultipleAsync(new FakeMultipleStoredProc(), "foo");
            });

            Assert.AreEqual(2, attemptCount);
            _dbResources.Verify(x => x.ChooseDb("mobile_ro").SelectRandomly(), Times.Exactly(2));

            Assert.AreEqual(2, _onErrorEvents.Count);
            Assert.IsInstanceOf<InvalidOperationException>(_onErrorEvents[0].Error);
            Assert.IsInstanceOf<InvalidOperationException>(_onErrorEvents[1].Error);
            Assert.AreEqual(1, _onErrorEvents[0].AttemptCount);
            Assert.AreEqual(2, _onErrorEvents[1].AttemptCount);

            Assert.AreEqual(2, _onQueryCompleteEvents.Count);
            Assert.IsInstanceOf<FakeMultipleStoredProc>(_onQueryCompleteEvents[0].StoredProc);
            Assert.IsInstanceOf<FakeMultipleStoredProc>(_onQueryCompleteEvents[1].StoredProc);
            Assert.IsNotNull(_onQueryCompleteEvents[0].Error);
            Assert.IsNotNull(_onQueryCompleteEvents[1].Error);
        }

        protected class FakeStoredProc : IStoredProc<string, string>
        {
            public string DbName => "mobile_ro";
            public string StoredProcedureName => "sp_foo";
            public int CommandTimeoutSecs => 1;
            public int MaxAttemptCount => 2;
            public TimeSpan? CacheLifetime { get; set; } = null;
            public SpParameter[] GetParameters(string parameters)
            {
                return new[]
                {
                    new SpParameter("param1", "value1"),
                    new SpParameter("param2", "value2")
                };
            }
        }

        private class FakeNonQueryStoredProc : IStoredProc<string>
        {
            public string DbName => "mobile_ro";
            public string StoredProcedureName => "sp_foo";
            public int CommandTimeoutSecs => 1;
            public int MaxAttemptCount => 2;
        }

        private class FakeMultipleStoredProc : IMultipleStoredProc<string, string>
        {
            public string DbName => "mobile_ro";
            public string StoredProcedureName => "sp_foo";
            public int CommandTimeoutSecs => 1;
            public int MaxAttemptCount => 2;
            public TimeSpan? CacheLifetime { get; set; } = null;
            public SpParameter[] GetParameters(string parameters)
            {
                return new[]
                {
                    new SpParameter("param1", "value1"),
                    new SpParameter("param2", "value2")
                };
            }

            public string Read(SqlMapper.GridReader reader)
                => "multiple_result";

            public Task<string> ReadAsync(SqlMapper.GridReader reader)
                => Task.FromResult("multiple_result");
        }
    }
}
