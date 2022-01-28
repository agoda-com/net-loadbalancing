using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Agoda.Frameworks.LoadBalancing;

namespace Agoda.Frameworks.DB
{
    public interface IDbResourceManager
    {
        IReadOnlyDictionary<string, IResourceManager<string>> AllResources { get; }
        IResourceManager<string> ChooseDb(string dbName);
    }

    public class DbResourceManager : IDbResourceManager
    {
        public static IDbResourceManager Create(
            IReadOnlyDictionary<string, string[]> dbNameAndConnectionStrings)
        {
            var dict = dbNameAndConnectionStrings
                .ToDictionary(x => x.Key, x => ResourceManager.Create(x.Value));
            return new DbResourceManager(dict);
        }

        public DbResourceManager(IReadOnlyDictionary<string, IResourceManager<string>> resources)
        {
            AllResources = resources.ToImmutableSortedDictionary();
        }

        public IReadOnlyDictionary<string, IResourceManager<string>> AllResources { get; private set; }

        public IResourceManager<string> ChooseDb(string dbName)
        {
            if (AllResources.TryGetValue(dbName, out var db))
            {
                return db;
            }
            throw new NotSupportedException("Unsupported database type.");
        }
    }

    public static class DbResourceManagerExtension
    {
        public static void UpdateResources(
            this IDbResourceManager mgr,
            string dbName,
            IEnumerable<string> dataSources)
        {
            if (mgr.AllResources.TryGetValue(dbName, out var resource))
            {
                resource.UpdateResources(dataSources);
            }
            else
            {
                throw new ArgumentException($"Database {dbName} not found.", nameof(dbName));
            }
        }

        public static void AddResources(
            this IDbResourceManager mgr,
            string dbName,
            IEnumerable<string> dataSources)
        {
            if (mgr.AllResources.TryGetValue(dbName, out var resource))
            {
                resource.UpdateResources(
                    resource.Resources.Keys.Union(dataSources));
            }
            else
            {
                throw new ArgumentException($"Database {dbName} not found.", nameof(dbName));
            }
        }

        public static void RemoveResources(
            this IDbResourceManager mgr,
            string dbName,
            IEnumerable<string> dataSources)
        {
            if (mgr.AllResources.TryGetValue(dbName, out var resource))
            {
                resource.UpdateResources(
                    resource.Resources.Keys.Except(dataSources));
            }
            else
            {
                throw new ArgumentException($"Database {dbName} not found.", nameof(dbName));
            }
        }
    }
}
