using Microsoft.VisualStudio.TestTools.UnitTesting;
using SuperCom.Entity;
using SuperUtils.Common;
using System.Collections.Generic;
using System.Linq;

namespace SuperCom.Test.UnitTests.DataCheck
{
    [TestClass]
    public class DataCheckTest
    {

        private static PortTabItem portTabItem = new PortTabItem("COM1", true);

        static DataCheckTest()
        {
            portTabItem.SerialPort = new SerialPortEx();
        }
        private void TestDataCheck(Core.Utils.DataCheck dataCheck, string input, string result)
        {
            portTabItem.SerialPort.DataCheck = dataCheck;
            string str =
                TransformHelper.FormatHexString(TransformHelper.ByteArrayToHexString(portTabItem.CalcHexValue(input)), "", " ");
            Assert.AreEqual(result, str);
        }



        [TestMethod]
        public void CalcHexValueTest1()
        {
            Core.Utils.DataCheck dataCheck = new Core.Utils.DataCheck() {
                Enabled = true,
                UseCustom = false,
                SelectedIndex = 0,
            };
            TestDataCheck(dataCheck, "12 34", "12 34 BA");
        }

        [TestMethod]
        public void CalcHexValueTest2()
        {
            Core.Utils.DataCheck dataCheck = new Core.Utils.DataCheck() {
                Enabled = true,
                UseCustom = false,
                SelectedIndex = 0,
            };
            TestDataCheck(dataCheck, "", "");
        }

        [TestMethod]
        public void CalcHexValueTest3()
        {
            Core.Utils.DataCheck dataCheck = new Core.Utils.DataCheck() {
                Enabled = true,
                UseCustom = false,
                SelectedIndex = 0,
            };
            TestDataCheck(dataCheck, "12 ", "12 EE");
        }

        [TestMethod]
        public void CalcHexValueTest4()
        {
            Core.Utils.DataCheck dataCheck = new Core.Utils.DataCheck() {
                Enabled = true,
                UseCustom = false,
                SelectedIndex = 0,
            };
            TestDataCheck(dataCheck, "1", "");
        }

    }
}
