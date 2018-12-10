using System;
using System.Threading.Tasks;

namespace Agoda.Frameworks.DB
{
    /// <summary>
    /// Universal interface for simple synchronous caching
    /// </summary>
    public interface IDbCache
    {
        /// <summary>
        /// Gets the item associated with this key if present.
        /// </summary>
        /// <param name="key">An object identifying the requested entry.</param>
        /// <param name="value">The located value or null.</param>
        /// <returns>True if the key was found.</returns>
        bool TryGetValue(string key, out object value);

        /// <summary>
        /// Create or overwrite an entry in the cache.
        /// </summary>
        void CreateEntry(string key, object value, TimeSpan? expirationRelativeToNow);
    }

    public static class SimpleCacheExtensions
    {
        public static T GetOrCreate<T>(
            this IDbCache cache,
            string key,
            TimeSpan? expirationRelativeToNow,
            Func<T> factory)
        {
            if (!cache.TryGetValue(key, out object result))
            {
                result = factory();
                cache.CreateEntry(key, result, expirationRelativeToNow);
            }

            return (T)result;
        }

        public static async Task<T> GetOrCreateAsync<T>(
            this IDbCache cache,
            string key,
            TimeSpan? expirationRelativeToNow,
            Func<Task<T>> factory)
        {
            if (!cache.TryGetValue(key, out object result))
            {
                // Async version of GetOrCreate is necessary, because
                // we want to store the result of Task<T> which is T.
                result = await factory();
                cache.CreateEntry(key, result, expirationRelativeToNow);
            }

            return (T)result;
        }
    }
}
