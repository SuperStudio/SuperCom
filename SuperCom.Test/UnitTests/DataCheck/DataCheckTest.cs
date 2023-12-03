using Microsoft.VisualStudio.TestTools.UnitTesting;
using SuperCom.Entity;
using SuperUtils.Common;

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

        private void TestDataCheck(string input, string result)
        {
            Core.Utils.DataCheck dataCheck = new Core.Utils.DataCheck() {
                Enabled = true,
                UseCustom = false,
                SelectedIndex = 0,
            };
            portTabItem.SerialPort.DataCheck = dataCheck;
            string str =
                TransformHelper.FormatHexString(TransformHelper.ByteArrayToHexString(portTabItem.CalcHexValue(input)), "", " ");
            Assert.AreEqual(result, str);
        }

        [TestMethod]
        public void CalcHexValueTest1()
        {
            TestDataCheck("12 34", "12 34 BA");
        }

        [TestMethod]
        public void CalcHexValueTest2()
        {
            TestDataCheck("", "");
        }

        [TestMethod]
        public void CalcHexValueTest3()
        {
            TestDataCheck("12 ", "12 EE");
        }

        [TestMethod]
        public void CalcHexValueTest4()
        {
            TestDataCheck("1", "");
        }

        private void CalcHexValueWithIndex(int start, int end, int insert, string input, string result)
        {
            Core.Utils.DataCheck dataCheck = new Core.Utils.DataCheck() {
                Enabled = true,
                UseCustom = true,
                SelectedIndex = 0,
                CustomStart = start,
                CustomEnd = end,
                CustomInsert = insert,
            };
            portTabItem.SerialPort.DataCheck = dataCheck;
            string str =
                TransformHelper.FormatHexString(TransformHelper.ByteArrayToHexString(portTabItem.CalcHexValue(input)), "", " ");
            Assert.AreEqual(result, str);
        }

        // 测试下标
        [TestMethod]
        public void CalcHexValueWithIndexTest1()
        {
            CalcHexValueWithIndex(0, 0, 0, "12 34 56 78", "EE 12 34 56 78");
        }

        [TestMethod]
        public void CalcHexValueWithIndexTest2()
        {
            CalcHexValueWithIndex(0, -1, -1, "12 34 56 78", "12 34 56 78 EC");
        }
        [TestMethod]
        public void CalcHexValueWithIndexTest3()
        {
            CalcHexValueWithIndex(0, -1, -2, "12 34 56 78", "12 34 56 EC 78");
        }

        [TestMethod]
        public void CalcHexValueWithIndexTest4()
        {
            CalcHexValueWithIndex(0, -1, -5, "12 34 56 78", "EC 12 34 56 78");
        }

        [TestMethod]
        public void CalcHexValueWithIndexTest5()
        {
            CalcHexValueWithIndex(0, -1, -6, "12 34 56 78", "12 34 56 78");

        }

        [TestMethod]
        public void CalcHexValueWithIndexTest6()
        {
            CalcHexValueWithIndex(0, -2, -1, "12 34 56 78", "12 34 56 78 64");
        }

        [TestMethod]
        public void CalcHexValueWithIndexTest7()
        {
            CalcHexValueWithIndex(0, -4, -1, "12 34 56 78", "12 34 56 78 EE");
        }

        [TestMethod]
        public void CalcHexValueWithIndexTest8()
        {
            CalcHexValueWithIndex(0, -5, -1, "12 34 56 78", "12 34 56 78");
        }

        [TestMethod]
        public void CalcHexValueWithIndexTest9()
        {
            CalcHexValueWithIndex(0, -1, -1, "12", "12 EE");
        }

        [TestMethod]
        public void CalcHexValueWithIndexTest10()
        {
            CalcHexValueWithIndex(0, -1, -2, "12", "EE 12");
        }

        [TestMethod]
        public void CalcHexValueWithIndexTest11()
        {
            CalcHexValueWithIndex(0, -1, -3, "12", "12");
        }

        [TestMethod]
        public void CalcHexValueWithIndexTest12()
        {
            CalcHexValueWithIndex(0, 0, -1, "12", "12 EE");
        }

        // 遍历所有校验功能
        [TestMethod]
        public void AllPluginTest()
        {
            string[] resultArr = {
                "87 65 43 21 B0",
                "87 65 43 21 43",
                "87 65 43 21 D5",
                "87 65 43 21 E5",
                "87 65 43 21 23",
                "87 65 43 21 4F",
                "87 65 43 21 43",
                "87 65 43 21 7C",
                "87 65 43 21 2C",
                "87 65 43 21 80",
                "87 65 43 21 3D",
                "87 65 43 21 6B",
                "87 65 43 21 BC",
                "87 65 43 21 33",
                "87 65 43 21 20",
                "87 65 43 21 50",
                "87 65 43 21 83",
                "87 65 43 21 DC",
                "87 65 43 21 5D",
                "87 65 43 21 24",
                "87 65 43 21 67",
                "87 65 43 21 1A",
                "87 65 43 21 3A",
                "87 65 43 21 28",
                "87 65 43 21 0F",
                "87 65 43 21 17",
                "87 65 43 21 0E",
                "87 65 43 21 CD 98 D6 8E 01 BC A0 DE",
                "87 65 43 21 40 19 54 DE 7E AA 1D 6A",
                "87 65 43 21 58 21 72 53 80 00 00 00",
                "87 65 43 21 92 E0 8C A6 D2 CB B8 45",
                "87 65 43 21 07",
                "87 65 43 21 00",
                "87 65 43 21 05",
                "87 65 43 21 00",
                "87 65 43 21 0E",
                "87 65 43 21 CB 7B A0 03 FF",
                "87 65 43 21 02",
                "87 65 43 21 02",
                "87 65 43 21 F1 19 07 C3",
                "87 65 43 21 0F 76 DA 4F",
                "87 65 43 21 A1 50 0B FA",
                "87 65 43 21 99 AB 29 7E",
                "87 65 43 21 77 14 AB 32",
                "87 65 43 21 8F 26 A6 AA",
                "87 65 43 21 07 5F 14 DB",
                "87 65 43 21 66 54 D6 81",
                "87 65 43 21 13 CF 48 3C",
                "87 65 43 21 88 EB 54 CD",
                "87 65 43 21 7D 7F 7D",
                "87 65 43 21 28 10 FE",
                "87 65 43 21 E2 07 B2",
                "87 65 43 21 17 EE 4C",
                "87 65 43 21 92 3A 6C",
                "87 65 43 21 E1 B7 BF",
                "87 65 43 21 73 C7 07",
                "87 65 43 21 85 47 3E",
                "87 65 43 21 A7 0D D1",
                "87 65 43 21 F0 49 4D",
                "87 65 43 21 BB 7B",
                "87 65 43 21 5F D1",
                "87 65 43 21 90 52",
                "87 65 43 21 E6 E4",
                "87 65 43 21 58 36",
                "87 65 43 21 E2 E5",
                "87 65 43 21 CD 0C",
                "87 65 43 21 F9 DC",
                "87 65 43 21 3D 5C",
                "87 65 43 21 22 0A",
                "87 65 43 21 27 2F",
                "87 65 43 21 55 8D",
                "87 65 43 21 A7 C9",
                "87 65 43 21 A7 C9",
                "87 65 43 21 19 1B",
                "87 65 43 21 7C 36",
                "87 65 43 21 C5 AC",
                "87 65 43 21 1A 3A",
                "87 65 43 21 83 C9",
                "87 65 43 21 6F AD",
                "87 65 43 21 EB 6D",
                "87 65 43 21 D0 00",
                "87 65 43 21 32 84",
                "87 65 43 21 F0 31",
                "87 65 43 21 F0 30",
                "87 65 43 21 E1 E0",
                "87 65 43 21 4C 3A",
                "87 65 43 21 E1 1C",
                "87 65 43 21 07 FD",
                "87 65 43 21 14 92",
                "87 65 43 21 E1 38",
                "87 65 43 21 9E 42",
                "87 65 43 21 29",
                "87 65 43 21 4A",
                "87 65 43 21 98",
                "87 65 43 21 09",
                "87 65 43 21 77",
                "87 65 43 21 C4",
                "87 65 43 21 25",
                "87 65 43 21 3F",
                "87 65 43 21 D5",
                "87 65 43 21 59",
                "87 65 43 21 2F",
                "87 65 43 21 8E",
                "87 65 43 21 06",
                "87 65 43 21 47",
                "87 65 43 21 B0",
                "87 65 43 21 FE B0",
                "87 65 43 21 01 50",
                "87 65 43 21 50",
                "87 65 43 21 80",
                "87 65 43 21"
            };
            int count = resultArr.Length;
            string input = "87 65 43 21";
            Core.Utils.DataCheck dataCheck = new Core.Utils.DataCheck() {
                Enabled = true,
                UseCustom = true,
                SelectedIndex = 0,
                CustomStart = 0,
                CustomEnd = -1,
                CustomInsert = -1,
            };

            for (int i = 0; i < count; i++) {
                dataCheck.SelectedIndex = i;
                portTabItem.SerialPort.DataCheck = dataCheck;
                string str =
                    TransformHelper.FormatHexString(TransformHelper.ByteArrayToHexString(portTabItem.CalcHexValue(input)), "", " ");
                Assert.AreEqual(str, resultArr[i]);
            }

        }
    }
}
