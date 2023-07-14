using Microsoft.VisualStudio.TestTools.UnitTesting;
using SuperUtils.Tests;
using System;
using System.IO;
using System.Threading;


namespace SuperCom.Test.UITest.Upgrade
{
    [TestClass]
    public class UpgradeTest : AppTestBase
    {

        public UpgradeTest()
        {
            WinAppDriverPath = AppTestBase.DEFAULT_WINAPP_DRIVER_PATH;
            ApplicationPath = Path.GetFullPath(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\SuperCom\bin\Debug\SuperCom.exe"));
        }

        [TestInitialize]
        public void TestInitialize()
        {
            this.Initialize();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            this.Cleanup();
        }

        [ClassCleanup]
        public static void ClassCleanusp()
        {
            StopWinappDriver();
        }

        string[] X_PATH_LIST = {
            "/Window[@Name=\"SuperCom-超级串口工具\"][@AutomationId=\"MainWindow\"]/Window[@Name=\"升级\"][@AutomationId=\"Dialog_Upgrade\"]/Button[@ClassName=\"Button\"][@Name=\"开始更新\"]/Text[@ClassName=\"TextBlock\"][@Name=\"开始更新\"]",
        };

        public void TestUpgrade()
        {
            this.ClickById("MenuItem_About");
            this.ClickById("MenuItem_ShowUpgrade");
            Thread.Sleep(3);
            this.ClickByXPath(X_PATH_LIST[0]);
        }


        public void TestBasic()
        {

        }

    }
}
