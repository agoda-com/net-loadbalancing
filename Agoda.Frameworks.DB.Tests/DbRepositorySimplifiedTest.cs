using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Moq;
using Moq.Dapper;
using NUnit.Framework;

namespace Agoda.Frameworks.DB.Tests
{
    public class DbRepositorySimplifiedTest : DbRepositoryTest
    {
        [Test]
        public async Task ExecuteQueryAsync_Success()
        {
            SetupAsync();

            var result = await _db.ExecuteQueryAsync<string>("mobile_ro", "sp_foo", CommandType.StoredProcedure, new
            {
                param1 = "value1",
                param2 = "value2"
            });
            Assert.AreEqual(_expectedRows, result);

            _dbResources.Verify(x => x.ChooseDb("mobile_ro").SelectRandomly(), Times.Exactly(1));

            Assert.AreEqual(0, _onErrorEvents.Count);

            Assert.AreEqual(1, _onQueryCompleteEvents.Count);
            Assert.IsInstanceOf<IAmNotAStoredProc>(_onQueryCompleteEvents[0].StoredProc);
            Assert.IsNull(_onQueryCompleteEvents[0].Error);
        }

        [Test]
        public async Task ExecuteQueryAsync_Hit_Cache_Success()
        {
            SetupAsync();
            _cache.Setup(x => x.TryGetValue(It.IsAny<string>(), out _cacheRows))
                .Returns(true);
            var result = await _db.ExecuteQueryAsync<string>("mobile_ro", "db.v1.sp_foo", CommandType.StoredProcedure, new
                {
                    param1 = "value1",
                    param2 = "value2"
                },
                TimeSpan.MaxValue);
            Assert.AreEqual(_cacheRows, result);

            _cache.Verify(
                x => x.TryGetValue(_expectedCacheKey, out _cacheRows),
                Times.Once);
            _dbResources.Verify(x => x.ChooseDb("mobile_ro").SelectRandomly(), Times.Never);

            Assert.AreEqual(0, _onQueryCompleteEvents.Count);
        }

        //[Test]
        // Skipping test since Moq.Dapper does not support mocking QuerySingleAsync
        public async Task ExecuteQuerySingleAsync_Success()
        {
            var expectedValue = "string";
            var sp = new FakeStoredProc();
            _connection.SetupDapperAsync(
                    c => c.QuerySingleAsync<string>(
                        sp.StoredProcedureName,
                        "foo",
                        null,
                        sp.CommandTimeoutSecs,
                        CommandType.StoredProcedure))
                .ReturnsAsync(expectedValue);

            var result = await _db.ExecuteQuerySingleAsync<string>("mobile_ro", "sp_foo", CommandType.StoredProcedure, new
            {
                param1 = "value1",
                param2 = "value2"
            });
            Assert.AreEqual(expectedValue, result);

            _dbResources.Verify(x => x.ChooseDb("mobile_ro").SelectRandomly(), Times.Exactly(1));

            Assert.AreEqual(0, _onErrorEvents.Count);

            Assert.AreEqual(1, _onQueryCompleteEvents.Count);
            Assert.IsInstanceOf<IAmNotAStoredProc>(_onQueryCompleteEvents[0].StoredProc);
            Assert.IsNull(_onQueryCompleteEvents[0].Error);
        }

        [Test]
        public async Task ExecuteQuerySingleAsync_Hit_Cache_Success()
        {
            SetupAsync();
            
            object cachedValue = "cachedValue";
            _cache.Setup(x => x.TryGetValue(It.IsAny<string>(), out cachedValue))
                .Returns(true);
            var result = await _db.ExecuteQuerySingleAsync<string>("mobile_ro", "db.v1.sp_foo", CommandType.StoredProcedure, new
                {
                    param1 = "value1",
                    param2 = "value2"
                },
                TimeSpan.MaxValue);
            Assert.AreEqual(cachedValue, result);

            _cache.Verify(
                x => x.TryGetValue(_expectedCacheKey, out cachedValue),
                Times.Once);
            _dbResources.Verify(x => x.ChooseDb("mobile_ro").SelectRandomly(), Times.Never);

            Assert.AreEqual(0, _onQueryCompleteEvents.Count);
        }
        
        // [Test]
        // Skipping test since Moq.Dapper does not support mocking ExecuteScalarAsync
        public async Task ExecuteScalarAsync_Success()
        {
            const string expectedReturn = "2";
            var sp = new FakeStoredProc();
            _connection.SetupDapperAsync(
                    c => c.ExecuteScalarAsync<string>(
                        sp.StoredProcedureName,
                        "foo",
                        null,
                        sp.CommandTimeoutSecs,
                        CommandType.StoredProcedure))
                .ReturnsAsync(expectedReturn);

            var result = await _db.ExecuteQueryAsync<string>("mobile_ro", "sp_foo", CommandType.StoredProcedure, new
            {
                param1 = "value1",
                param2 = "value2"
            });
            Assert.AreEqual(expectedReturn, result);

            _dbResources.Verify(x => x.ChooseDb("mobile_ro").SelectRandomly(), Times.Exactly(1));

            Assert.AreEqual(0, _onErrorEvents.Count);

            Assert.AreEqual(1, _onQueryCompleteEvents.Count);
            Assert.IsInstanceOf<IAmNotAStoredProc>(_onQueryCompleteEvents[0].StoredProc);
            Assert.IsNull(_onQueryCompleteEvents[0].Error);
        }
    }
}