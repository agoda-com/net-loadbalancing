using System;
using System.Collections.Generic;
using Agoda.Frameworks.LoadBalancing;
using Moq;
using NUnit.Framework;

namespace Agoda.Frameworks.DB.Test
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
    }
}
