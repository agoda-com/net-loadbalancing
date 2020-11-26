using Moq;
using NUnit.Framework;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

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

        [Test]
        public async Task ExecuteQuerySingleAsync_Hit_Cache_By_Client_Key_Success()
        {
            SetupAsync();

            var cacheKey = "client-key";
            object cachedValue = "cachedValue";
            _cache.Setup(x => x.TryGetValue(cacheKey, out cachedValue))
                .Returns(true);
            var result = await _db.ExecuteQuerySingleAsync<string>("mobile_ro", "db.v1.sp_foo", CommandType.StoredProcedure, new
            {
                param1 = "value1",
                param2 = "value2"
            },
                TimeSpan.MaxValue, cacheKey);
            Assert.AreEqual(cachedValue, result);

            _cache.Verify(
                x => x.TryGetValue(cacheKey, out cachedValue),
                Times.Once);
            _dbResources.Verify(x => x.ChooseDb("mobile_ro").SelectRandomly(), Times.Never);

            Assert.AreEqual(0, _onQueryCompleteEvents.Count);
        }

        [Test]
        public async Task ExecuteReaderAsync_Hit_Cache_Success()
        {
            SetupAsync();
            
            object cachedValue = "cachedValue";
            _cache.Setup(x => x.TryGetValue(It.IsAny<string>(), out cachedValue))
                .Returns(true);
            var result = await _db.ExecuteReaderAsync<string>("mobile_ro", "db.v1.sp_foo", 1,
                2,new IDbDataParameter[]
                {
                    new SqlParameter("@param1", "value1"),
                    new SqlParameter("@param2", "value2")

                }, reader => Task.FromResult(cachedValue.ToString()), TimeSpan.MaxValue);
            Assert.AreEqual(cachedValue, result);

            _cache.Verify(
                x => x.TryGetValue(_expectedCacheKey, out cachedValue),
                Times.Once);
            _dbResources.Verify(x => x.ChooseDb("mobile_ro").SelectRandomly(), Times.Never);

            Assert.AreEqual(0, _onQueryCompleteEvents.Count);
        }

        [Test]
        public void ExecuteReader_Hit_Cache_Success()
        {
            object cachedValue = "cachedValue";
            _cache.Setup(x => x.TryGetValue(It.IsAny<string>(), out cachedValue))
                .Returns(true);
            var result = _db.ExecuteReader("mobile_ro", "db.v1.sp_foo", 1,
                2,new IDbDataParameter[]
                {
                    new SqlParameter("@param1", "value1"),
                    new SqlParameter("@param2", "value2")

                }, reader => cachedValue.ToString(), TimeSpan.MaxValue);
            Assert.AreEqual(cachedValue, result);

            _cache.Verify(
                x => x.TryGetValue(_expectedCacheKey, out cachedValue),
                Times.Once);
            _dbResources.Verify(x => x.ChooseDb("mobile_ro").SelectRandomly(), Times.Never);

            Assert.AreEqual(0, _onQueryCompleteEvents.Count);
        }
    }
}