using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperCom.WatchDog
{
    public abstract class AbstractDog : IDog
    {
        private const int DEFAULT_WATCH_INTERVAL = 10 * 1000;

        public Action OnNotFeed { get; set; }
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


        private int WatchInterval { get; set; }

        private bool Init { get; set; }

        public void Watch()
        {
            if (Init)
                return;
            Init = true;
            Task.Run(async () =>
            {
                while (true)
                {
                    Console.WriteLine($"看门狗每 {WatchInterval / 1000} s 看一下门...");
                    if (!Feed())
                    {
                        // 通知程序
                        OnNotFeed?.Invoke();
                        Console.WriteLine("看门狗要发飙了！");
                    }
                    await Task.Delay(WatchInterval);
                }

            });
        }


    }
}
