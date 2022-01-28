using System;
using NUnit.Framework;

namespace Agoda.Frameworks.DB.Tests
{
    public class StoredProcTest
    {
        public class TestRequestParameter
        {
            public DateTime DateTimeParam { get; set; }
            public int IntParam { get; set; }
            public string StringParam { get; set; }
        }

        public class TestStoredProc : AutoStoredProc<TestRequestParameter, object>
        {
            public override TimeSpan? CacheLifetime => throw new NotImplementedException();

            public override string DbName => throw new NotImplementedException();

            public override string StoredProcedureName => throw new NotImplementedException();

            public override int CommandTimeoutSecs => throw new NotImplementedException();

            public override int MaxAttemptCount => throw new NotImplementedException();
        }

        [Test]
        public void AutoStoredProc_GetParameters()
        {
            var sp = new TestStoredProc();
            var parameters = sp.GetParameters(new TestRequestParameter()
            {
                IntParam = 87,
                StringParam = "EightSeven",
                DateTimeParam = new DateTime(2020, 8, 7)
            });
            Assert.AreEqual(3, parameters.Length);
            Assert.AreEqual("DateTimeParam", parameters[0].Name);
            Assert.AreEqual(new DateTime(2020, 8, 7), (DateTime)parameters[0].Value);
            Assert.AreEqual("IntParam", parameters[1].Name);
            Assert.AreEqual(87, (int)parameters[1].Value);
            Assert.AreEqual("StringParam", parameters[2].Name);
            Assert.AreEqual("EightSeven", (string)parameters[2].Value);
        }
    }
}
