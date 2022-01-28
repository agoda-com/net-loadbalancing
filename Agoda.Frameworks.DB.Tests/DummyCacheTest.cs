using System;
using NUnit.Framework;

namespace Agoda.Frameworks.DB.Tests
{
    public class DummyCacheTest
    {
        [Test]
        public void CreateEntryTest()
        {
            var cache = new DummyCache();
            cache.CreateEntry("key", "value", null);
        }

        [Test]
        public void TryGetValueTest()
        {
            var cache = new DummyCache();
            cache.CreateEntry("key", "value", null);
            var tryGetResult = cache.TryGetValue("key", out object value);
            // TryGetValue is always false
            Assert.IsFalse(tryGetResult);
            Assert.IsNull(value);
        }
    }
}
