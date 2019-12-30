using System;
using System.Collections.Generic;
using System.Linq;
using Agoda.Frameworks.LoadBalancing;
using Moq;
using NUnit.Framework;

namespace Agoda.Frameworks.DB.Tests
{
    public class DbResourceManagerTest
    {
        [Test]
        public void ChooseDb_Success()
        {
            var mgr = new DbResourceManager(new Dictionary<string, IResourceManager<string>>()
            {
                {"mobile_ro", Mock.Of<IResourceManager<string>>()}
            });
            Assert.IsNotNull(mgr.ChooseDb("mobile_ro"));
        }

        [Test]
        public void ChooseDb_Not_Supported()
        {
            var mgr = new DbResourceManager(new Dictionary<string, IResourceManager<string>>()
            {
                {"mobile_ro", Mock.Of<IResourceManager<string>>()}
            });
            Assert.Throws<NotSupportedException>(() =>
            {
                mgr.ChooseDb("unsupported_db");
            });
        }

        [Test]
        public void UpdateResources()
        {
            var mgr = DbResourceManager.Create(new Dictionary<string, string[]>()
            {
                ["db1"] = new[] { "db1-01", "db1-02", "db1-03" }
            });
            // Remove 01, add 04
            mgr.UpdateResources("db1", new[] { "db1-02", "db1-03", "db1-04" });
            Assert.AreEqual(
                new[] { "db1-02", "db1-03", "db1-04" },
                mgr.AllResources["db1"].Resources.Keys.OrderBy(x => x).ToArray());
        }

        [Test]
        public void AddResources()
        {
            var mgr = DbResourceManager.Create(new Dictionary<string, string[]>()
            {
                ["db1"] = new[] { "db1-01", "db1-02", "db1-03" }
            });
            // Add 04, 01 duplicated
            mgr.AddResources("db1", new[] { "db1-01", "db1-04" });
            Assert.AreEqual(
                new[] { "db1-01", "db1-02", "db1-03", "db1-04" },
                mgr.AllResources["db1"].Resources.Keys.OrderBy(x => x).ToArray());
        }

        [Test]
        public void RemoveResources()
        {
            var mgr = DbResourceManager.Create(new Dictionary<string, string[]>()
            {
                ["db1"] = new[] { "db1-01", "db1-02", "db1-03" }
            });
            // Remove 01, 04 redundant
            mgr.RemoveResources("db1", new[] { "db1-01", "db1-04" });
            Assert.AreEqual(
                new[] { "db1-02", "db1-03" },
                mgr.AllResources["db1"].Resources.Keys.OrderBy(x => x).ToArray());
        }


        [Test]
        public void UpdateResources_Fail()
        {
            var mgr = DbResourceManager.Create(new Dictionary<string, string[]>()
            {
                ["db1"] = new[] { "db1-01", "db1-02", "db1-03" }
            });
            Assert.Throws<ArgumentException>(() => mgr.UpdateResources("db2", new[] { "db2" }));
        }


        [Test]
        public void AddResources_Fail()
        {
            var mgr = DbResourceManager.Create(new Dictionary<string, string[]>()
            {
                ["db1"] = new[] { "db1-01", "db1-02", "db1-03" }
            });
            Assert.Throws<ArgumentException>(() => mgr.AddResources("db2", new[] { "db2" }));
        }

        [Test]
        public void RemoveResources_Fail()
        {
            var mgr = DbResourceManager.Create(new Dictionary<string, string[]>()
            {
                ["db1"] = new[] { "db1-01", "db1-02", "db1-03" }
            });
            Assert.Throws<ArgumentException>(() => mgr.RemoveResources("db2", new[] { "db2" }));
        }
    }
}
