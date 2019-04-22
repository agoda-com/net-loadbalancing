using System;
using System.Collections.Generic;
using System.Linq;

namespace Agoda.Frameworks.Http
{
    public interface IErrorEntry
    {
        int HttpCode { get; }
        string Source { get; }
    }
    public sealed class ErrorEntry : IErrorEntry
    {
        public ErrorEntry(int httpCode, string source)
        {
            HttpCode = httpCode;
            Source = source;
        }

        public int HttpCode { get; }
        public string Source { get; }
    }

    public interface IErrorList
    {
        bool HasError { get; }
        bool HasResult { get; }
        IReadOnlyList<IErrorEntry> List { get; }
    }
    public sealed class ErrorList : IErrorList
    {
        public ErrorList(IEnumerable<IErrorEntry> entries, bool hasResult)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries));
            List = entries.ToArray();
            HasResult = hasResult;
        }

        public bool HasError => List.Any();

        public bool HasResult { get; }

        public IReadOnlyList<IErrorEntry> List { get; }
    }
}
