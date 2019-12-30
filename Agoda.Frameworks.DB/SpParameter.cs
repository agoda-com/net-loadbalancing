using System;
using System.Linq;

namespace Agoda.Frameworks.DB
{
    public class SpParameter
    {
        public readonly string Name;
        public readonly object Value;

        internal SpParameter(string name, object value)
        {
            Name = name;
            Value = value;
        }
        public SpParameter(string name, int value) : this(name, (object)value) 
        {
        }
        public SpParameter(string name, string value) : this(name, (object)value)
        {
        }
        public SpParameter(string name, long value) : this(name, (object)value)
        {
        }
        public SpParameter(string name, double value) : this(name, (object)value)
        {
        }
        public SpParameter(string name, char value) : this(name, (object)value)
        {
        }
        public SpParameter(string name, bool value) : this(name, (object)value)
        {
        }
        public SpParameter(string name, DateTime value) : this(name, (object)value)
        {
        }
        public SpParameter(string name, Guid value) : this(name, (object)value)
        {
        }
        // To support more parameter types, add more ctors above and edit SpParameterTest.
    }

    public static class SpParameterExtensions
    {
        const string dbPrefix = "db.v1.";

        public static string CreateCacheKey(this SpParameter[] parameters, string spName)
        {
            if (!parameters?.Any() ?? true)
            {
                return $"{dbPrefix}{spName}";
            }

            return parameters?
                .OrderBy(x => x.Name)
                .Aggregate(
                    $"{dbPrefix}{spName}:",
                    (seed, pair) =>
                    {
                        var value = pair.Value is DateTime dateTime
                            ? dateTime.Ticks.ToString()
                            : pair.Value.ToString();
                        return $"{seed}@{pair.Name}+{value}&";
                    });
        }
    }
}
