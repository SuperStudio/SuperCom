using SuperCom.Config;
using SuperCom.Core.Entity;
using SuperCom.Core.Events;
using SuperCom.Entity;
using SuperUtils.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using static SuperCom.App;

namespace SuperCom.Core.Settings
{
    public class GlobalSettings
    {
        public static ComSettingManager ComSetting;
        public static HighLightSettingManager HighLightSetting;
        public static SendCommandManager SendCommand;

        static GlobalSettings()
        {
            ComSetting = ComSettingManager.CreateInstance();
            HighLightSetting = HighLightSettingManager.CreateInstance();
            SendCommand = SendCommandManager.CreateInstance();
        }

        public static void Init()
        {
            ComSetting.Init();
            HighLightSetting.Init();
            SendCommand.Init();
        }
    }
}
