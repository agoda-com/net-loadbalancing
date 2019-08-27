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
}
