using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
