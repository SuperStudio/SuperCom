using SuperControls.Style;
using System;
using System.Threading.Tasks;
using static SuperCom.App;

namespace SuperCom.WatchDog
{
    public abstract class AbstractDog : IDog
    {
        private const int DEFAULT_WATCH_INTERVAL = 10 * 1000;

        #region "属性"
        public Action OnNotFeed { get; set; }

        private int WatchInterval { get; set; }

        private bool Init { get; set; }
        private volatile bool Enabled = true;

        #endregion

        public virtual bool Feed()
        {
            throw new NotImplementedException();
        }

        public AbstractDog()
        {
            WatchInterval = DEFAULT_WATCH_INTERVAL;
        }

        public AbstractDog(int watchInterval)
        {
            WatchInterval = watchInterval;
        }

        public void Stop()
        {
            Enabled = false;
        }


        public void Watch()
        {
            if (Init)
                return;
            Init = true;
            Task.Run(async () => {
                while (Enabled) {
                    //Logger.Debug($"看门狗每 {WatchInterval / 1000} s 看一下门");
                    if (!Feed()) {
                        // 通知程序
                        Logger.Debug(LangManager.GetValueByKey("WatchDogShouting"));
                        OnNotFeed?.Invoke();
                    }
                    await Task.Delay(WatchInterval);
                }
                Logger.Info("feed dog stop");
            });
        }


    }
}
