using System;
using System.Collections.Generic;

namespace Agoda.Frameworks.LoadBalancing
{
    public sealed class UpdateWeightEventArgs<TSource> : EventArgs
    {
        public UpdateWeightEventArgs(
            IReadOnlyDictionary<TSource, WeightItem> oldCollection,
            IReadOnlyDictionary<TSource, WeightItem> newCollection)
        {
            OldResources = oldCollection;
            NewResources = newCollection;
        }

        public IReadOnlyDictionary<TSource, WeightItem> OldResources { get; }
        public IReadOnlyDictionary<TSource, WeightItem> NewResources { get; }
    }
}
