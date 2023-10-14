using SuperUtils.WPF.VieModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static SuperCom.App;

namespace SuperCom.Core.Telnet
{
    public class TelnetServerManager : ViewModelBase
    {

        public const string HELP =
            "help               for telnet help\r\n" +
            "sendtoall xxx      send xxx to all telnet client\r\n" +
            "exit/quit          for exit telnet\r\n";

        private static Action<string> Log { get; set; }

        private static TelnetServer TelnetServer { get; set; }

        public static TelnetServerManager Instance { get; set; } = new TelnetServerManager();

        private bool _Running;
        public bool Running {
            get { return _Running; }
            set {
                _Running = value;
                RaisePropertyChanged();
            }
        }

        private TelnetServerManager() { }

        public static bool IsRunning()
        {
            if (Instance == null) {
                return false;
            }
            return Instance.Running;
        }


        public static void Stop()
        {
            if (TelnetServer == null)
                return;
            try {
                Log?.Invoke("stop telnet server");
                TelnetServer.Stop();
                RemoveHandler();
                TelnetServer = null;

                Instance.Running = false;
            } catch (Exception e) {
                Log?.Invoke(e.ToString());
            }
        }

        public static bool Start(string ip)
        {
            IPAddress iPAddress = IPAddress.Parse(ip);
            if (iPAddress == null) {
                Log?.Invoke("ip parse failed");
                return false;
            }

            try {
                Log?.Invoke($"start telnet server at {ip}");
                TelnetServer = new TelnetServer(iPAddress);
                AddHandler();
                TelnetServer.Start();
                Instance.Running = true;
                Log?.Invoke($"start success!");
            } catch (Exception e) {
                TelnetServer = null;
                Log?.Invoke(e.Message);
                return false;
            }
            return true;
        }

        private static void AddHandler()
        {
            TelnetServer.ClientConnected += OnClientConnected;
            TelnetServer.ClientDisconnected += OnClientDisconnected;
            TelnetServer.ConnectionBlocked += OnConnectionBlocked;
            TelnetServer.MessageReceived += OnMessageReceived;
        }

        private static void RemoveHandler()
        {
            TelnetServer.ClientConnected -= OnClientConnected;
            TelnetServer.ClientDisconnected -= OnClientDisconnected;
            TelnetServer.ConnectionBlocked -= OnConnectionBlocked;
            TelnetServer.MessageReceived -= OnMessageReceived;
        }

        private static void OnClientConnected(TelnetClient c)
        {
            Log?.Invoke("connected: " + c);
            TelnetServer.sendMessageToClient(c, "Telnet Server By SuperCom (root:123456)" + TelnetServer.END_LINE + "Login: ");
        }

        private static void OnClientDisconnected(TelnetClient c)
        {
            Log?.Invoke("disconnected: " + c);
        }

        private static void OnConnectionBlocked(IPEndPoint ep)
        {
            Log?.Invoke(string.Format("blocked: {0}:{1} at {2}", ep.Address, ep.Port, DateTime.Now));
        }

        private static void OnMessageReceived(TelnetClient c, string message)
        {
            if (c.getCurrentStatus() != EClientStatus.LoggedIn) {
                HandleLogin(c, message);
                return;
            }

            Log?.Invoke($"[client {c.getClientID()}] {message}");

            if (message == "quit" || message == "exit") {
                TelnetServer.sendMessageToClient(c, TelnetServer.END_LINE + "good bye!");
                TelnetServer.kickClient(c);
            } else if (message == "clear") {
                TelnetServer.clearClientScreen(c);
                TelnetServer.sendMessageToClient(c, TelnetServer.CURSOR);
            } else if (message == "help") {
                TelnetServer.sendMessageToClient(c, TelnetServer.END_LINE + HELP);
                TelnetServer.sendMessageToClient(c, TelnetServer.CURSOR);
            } else if (message.StartsWith("sendtoall") && message.IndexOf(" ") > 0) {
                TelnetServer.sendMessageToAll(message.Substring(message.IndexOf(" ")));
            } else
                TelnetServer.sendMessageToClient(c, TelnetServer.END_LINE + TelnetServer.CURSOR);
        }

        private static void HandleLogin(TelnetClient c, string message)
        {
            EClientStatus status = c.getCurrentStatus();
            if (status == EClientStatus.Guest) {
                if (message == "root") {
                    TelnetServer.sendMessageToClient(c, TelnetServer.END_LINE + "password: ");
                    c.setStatus(EClientStatus.Authenticating);
                } else
                    TelnetServer.kickClient(c);
            } else if (status == EClientStatus.Authenticating) {
                if (message == "123456") {
                    TelnetServer.clearClientScreen(c);
                    TelnetServer.sendMessageToClient(c, "login success!" + TelnetServer.END_LINE + TelnetServer.CURSOR);
                    c.setStatus(EClientStatus.LoggedIn);
                } else
                    TelnetServer.kickClient(c);
            }
        }


        public override void Init()
        {
            throw new NotImplementedException();
        }

        public static void SetLogFunc(Action<string> onLog)
        {
            Log = onLog;
        }
    }
}
