using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SuperCom.Entity.HighLightRule;

namespace SuperCom.Test.UnitTests.Entity
{
    [TestClass]
    public class RuleSetTest
    {

        [TestMethod]
        private static void TestRuleSet(long expect, params long[] data)
        {
            long id = RuleSet.GenerateID(data.ToList());
            Assert.AreEqual(expect, id);
        }

        [TestMethod]
        public void GenerateIDTest1()
        {
            TestRuleSet(2, 0, 1, 3, 4, 5);
        }

        [TestMethod]
        public void GenerateIDTest2()
        {
            TestRuleSet(0,  1, 2, 3, 4);
        }

        [TestMethod]
        public void GenerateIDTest3()
        {
            TestRuleSet(4, 0, 1, 2, 3);
        }
    }
}
