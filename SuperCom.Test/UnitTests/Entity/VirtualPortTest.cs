using Microsoft.VisualStudio.TestTools.UnitTesting;
using SuperCom.Entity;
using SuperUtils.Reflections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperCom.Test.UnitTests.Entity
{
    [TestClass]
    public class VirtualPortTest
    {

        [TestMethod]
        public void ParseVirtualPortTest()
        {
            string data = "CNCA0 PortName=COM2,EmuBR=yes,EmuOverrun=yes";
            VirtualPort virtualPort = VirtualPortManager.ParseVirtualPort(data);
            //string str = ClassUtils.ToString(virtualPort, true);
            Assert.AreEqual(virtualPort.ID, "CNCA0");
            Assert.AreEqual(virtualPort.Name, "COM2");
            Assert.AreEqual(virtualPort.EmuBR, true);
            Assert.AreEqual(virtualPort.EmuOverrun, true);
            Assert.AreEqual(virtualPort.AddRITO, 0);
            Assert.AreEqual(virtualPort.HiddenMode, false);
        }

    }
}
