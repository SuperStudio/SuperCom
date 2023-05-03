using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace SuperCom.Test
{
    [TestClass]
    public class ComPortComparer
    {
        [TestMethod]
        public void ComPortComparerTest()
        {
            List<string> comList = new List<string>()
            {
                "COM1","COM11","COM111","COM2","COM20","COM99"
            };
            List<string> sorted = new List<string>()
            {
                "COM1","COM2","COM11","COM20","COM99","COM111"
            };
            List<string> list = comList.OrderBy(arg => arg, new SuperCom.Comparers.ComPortComparer()).ToList();
            CollectionAssert.AreEqual(list, sorted);
        }
    }
}
