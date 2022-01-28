using System;
using System.Linq;
using NUnit.Framework;

namespace Agoda.Frameworks.DB.Tests
{
    public class SpParameterTest
    {
        [Test]
        public void CreateCacheKey_Types()
        {
            var safeTypes = new[]
            {
                typeof(int),
                typeof(string),
                typeof(long),
                typeof(double),
                typeof(char),
                typeof(bool),
                typeof(DateTime),
                typeof(Guid),
            };
            var intParam = new SpParameter("int", 55);
            var stringParam = new SpParameter("string", "foo");
            var longParam = new SpParameter("long", 55L);
            var doubleParam = new SpParameter("double", 3.14);
            var charParam = new SpParameter("char", '*');
            var boolParam = new SpParameter("bool", true);
            var dateTimeParam = new SpParameter("DateTime", new DateTime(2000, 1, 1));
            var guidParam = new SpParameter("Guid", new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"));

            var parameters = new[]
            {
                intParam,
                stringParam,
                longParam,
                doubleParam,
                charParam,
                boolParam,
                dateTimeParam,
                guidParam
            };
            var key = parameters.CreateCacheKey("sp_foo");

            Assert.AreEqual(
                "db.v1.sp_foo:" +
                "@bool+True&" +
                "@char+*&" +
                "@DateTime+630822816000000000&" +
                "@double+3.14&" +
                "@Guid+dddddddd-dddd-dddd-dddd-dddddddddddd&" +
                "@int+55&" +
                "@long+55&" +
                "@string+foo&",
                key);

            var ctors = typeof(SpParameter).GetConstructors();
            foreach (var info in ctors)
            {
                Assert.IsTrue(
                    safeTypes.Any(t => t == info.GetParameters()[1].ParameterType),
                    "All ctor types for SpParameter need to add " +
                    "instance to the list above and update the test.");
            }
            Assert.AreEqual(safeTypes.Length, ctors.Length);
        }
    }
}
