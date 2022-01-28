using System;
namespace Agoda.Frameworks.DB
{
    public sealed class DummyCache : IDbCache
    {
        public void CreateEntry(string key, object value, TimeSpan? expirationRelativeToNow)
        {
        }

        public bool TryGetValue(string key, out object value)
        {
            value = default;
            return false;
        }
    }
}
