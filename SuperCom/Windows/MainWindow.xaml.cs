
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Search;
using SuperCom.Config;
using SuperCom.Controls.DataTemplate;
using SuperCom.Core.Entity;
using SuperCom.Core.Entity.Enums;
using SuperCom.Core.Events;
using SuperCom.Core.Interfaces;
using SuperCom.Core.Settings;
using SuperCom.Core.Telnet;
using SuperCom.Entity;
using SuperCom.Entity.Enums;
using SuperCom.Upgrade;
using SuperCom.ViewModel;
using SuperCom.Windows;
using SuperControls.Style;
using SuperControls.Style.Plugin;
using SuperControls.Style.UserControls.TabControlPro;
using SuperControls.Style.UserControls.TabControlPro.Enums;
using SuperControls.Style.Utils;
using SuperControls.Style.Windows;
using SuperUtils.Common;
using SuperUtils.Framework.ORM.Enums;
using SuperUtils.IO;
using SuperUtils.Systems;
using SuperUtils.Time;
using SuperUtils.Values;
using SuperUtils.WPF.VisualTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Linq;
using static SuperCom.App;

namespace SuperCom
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : BaseWindow
    {
        private const double DEFAULT_SEND_PANEL_HEIGHT = 186;
        #region "属性"


        private Window_ShortCut window_ShortCut { get; set; }
        private Window_Setting window_Setting { get; set; }
        private Window_Monitor window_Monitor { get; set; }
        private Window_TelnetServer Window_TelnetServer { get; set; }
        private Window_VirtualPort virtualPort { get; set; }
        public VieModel_Main vieModel { get; set; }


        /// <summary>
        /// 支持标签栏拖拽
        /// </summary>
        private FrameworkElement CurrentDragElement { get; set; }

        #endregion

        public MainWindow()
        {
            InitializeComponent();
            Init();
        }

        public void Init()
        {
            InitSqlite();
            ConfigManager.InitConfig();
            vieModel = new VieModel_Main();
            this.DataContext = vieModel;
            SetLang();
            PathManager.Init();

            // 看门狗
            App.OnMemoryChanged += (memory) => {
                vieModel.MemoryUsed = Math.Ceiling((double)memory / 1024 / 1024);
            };

            App.OnMemoryDog += OnMemoryDog;

            colorPicker.SelectedColorChanged += (s, e) => {
                Logger.Info($"color picker set color: {colorPicker.SelectedColor}");
            };

            Logger.Info("main window init");
        }

        private async void mainWindow_ContentRendered(object sender, EventArgs e)
        {
            this.TopMenu = TopMenus;
            //AdjustWindow();
            if (ConfigManager.Main.FirstRun)
                ConfigManager.Main.FirstRun = false;
            InitThemeSelector();
            RefreshSetting();
            ReadXshdList();
            LoadDonateConfig();
            await BackupData();
            LoadFontFamily();
            InitUpgrade();
            OpenBeforePorts();
            SetBaudRateAction();
            //InitNotice(); // todo
            ApplyScreenStatus();
            InitEventManager();
        }

        private void InitEventManager()
        {
            BasicEventManager.RegisterEvent(EventType.OpenAll, OnRecvProc);
            BasicEventManager.RegisterEvent(EventType.CloseAll, OnRecvProc);
            BasicEventManager.RegisterEvent(EventType.ProcOne, OnRecvProc);
            BasicEventManager.RegisterEvent(EventType.OpenTab, OnRecvProc);
            BasicEventManager.RegisterEvent(EventType.Remark, OnRecvRemark);
            BasicEventManager.RegisterEvent(EventType.StatusText, OnRecvStatusText);
            BasicEventManager.RegisterEvent(EventType.RemoveTabBar, OnRecvRemoveTabBar);
            BasicEventManager.RegisterEvent(EventType.NotifyTabItemBaudRate, OnRecvNotifyTabItemBaudRate);
            BasicEventManager.RegisterEvent(EventType.ShowHighLight, OnRecvShowHighLight);
            BasicEventManager.RegisterEvent(EventType.OnSendCommandModify, OnRecvOnSendCommandModify);
        }

        private void OnRecvOnSendCommandModify(object data)
        {
            RefreshSendCommands();
        }

        private void OnRecvShowHighLight(object data)
        {
            if (data is int index) {
                OpenSetting();
                window_Setting.SetTabSelected(Window_Setting.HIGH_LIGHT_TAB_INDEX);
                window_Setting.SetHighLightIndex((int)index);
            }
        }

        private void OnRecvNotifyTabItemBaudRate(object data)
        {
            List<TabItemPro> tabItemPros = tabPanelPro.ItemsSource.ToList();
            foreach (var item in tabItemPros) {
                if (item.TabItemControl is ComTemplate comTemplate) {
                    comTemplate.OnBaudRateChange();
                }
            }
        }


        private void OnRecvRemoveTabBar(object data)
        {
            if (data is string text) {
                tabPanelPro.Remove(text);
            }
        }
        private void OnRecvStatusText(object data)
        {
            if (data is string text)
                vieModel.StatusText = text;
        }

        private async void OnRecvProc(object data)
        {
            if (data is TabInfo tabInfo) {
                List<string> nameList = tabInfo.Data as List<string>;
                ConnectType connectType = tabInfo.ConnectType;
                bool isConnected = tabInfo.IsConnected;
                if (!tabInfo.RemoveBar) {
                    // 新建 tab
                    foreach (string name in nameList) {
                        Logger.Info($"proc tab bar: {name}, connect: {isConnected}");
                        OpenTabBar(name, connectType, isConnected);
                    }
                    await Task.Delay(200);
                }

                foreach (string name in nameList) {
                    Logger.Info($"set tab[{name}] connect: {true}");
                    bool procOk = await SetTabConnect(name, connectType, isConnected);
                    tabInfo.IsConnected = procOk ? isConnected : !isConnected;
                    tabInfo.Data = name;
                }

                if (tabInfo.RemoveBar) {
                    // 移除 tab
                    foreach (string name in nameList) {
                        OnRecvRemoveTabBar(name);
                    }
                }
            }
        }

        private void OnConnectorChanged(TabInfo tabInfo)
        {
            string name = tabInfo.Data as string;
            // 设置侧边栏
            BasicEventManager.SendEvent(EventType.StatusChanged, tabInfo);
            // 设置选项 bar
            TabItemPro tabItemPro = tabPanelPro.GetItemById(name);
            if (tabItemPro != null && tabItemPro is ConnectorTabItem connectorTabItem) {
                connectorTabItem.Connected = tabInfo.IsConnected;
            }
        }

        private async Task<bool> SetTabConnect(string name, ConnectType connectType, bool connect)
        {
            TabItemPro tabItemPro = tabPanelPro.GetItemById(name);
            if (tabItemPro == null || !(tabItemPro.TabItemControl is IConnectTemplate))
                return false;
            IConnectTemplate connectTemplate = tabItemPro.TabItemControl as IConnectTemplate;
            if (connectTemplate == null)
                return false;

            bool procOk = false;
            if (connect)
                procOk = await connectTemplate.Open();
            else
                procOk = await connectTemplate.Close();

            return procOk;
        }

        private void OnRecvRemark(object data)
        {
            if (data is string name)
                Remark(name);
        }

        private void RemoveAllBar()
        {
            TabInfo tabInfo = new TabInfo();
            tabInfo.ConnectType = ConnectType.Com;
            tabInfo.IsConnected = false;
            tabInfo.RemoveBar = true;
            tabInfo.Data = tabPanelPro.ItemsSource.Select(arg => arg.Name).ToList();
            OnRecvProc(tabInfo);
        }

        private void OnMemoryDog()
        {
            App.GetDispatcher()?.Invoke(() => {
                if (vieModel != null && vieModel.PortTabItems != null && vieModel.PortTabItems.Count > 0) {
                    ComConnector port = null;
                    long maxLength = 0;
                    foreach (var item in vieModel.PortTabItems) {
                        if (item.TextEditor == null || !item.Connected)
                            continue;
                        if (item.TextEditor.Text.Length > maxLength) {
                            maxLength = item.TextEditor.Text.Length;
                            port = item;
                        }
                    }
                    if (port != null && maxLength > 0) {
                        TextEditor oldTextEditor = port.TextEditor;
                        Border border = oldTextEditor.Parent as Border;
                        TextEditor newTextEditor = new TextEditor();
                        //SetTextEditorConfig(ref newTextEditor, true); // todo

                        IHighlightingDefinition syntaxHighlighting = oldTextEditor.SyntaxHighlighting;
                        double FontSize = oldTextEditor.FontSize;

                        newTextEditor.SyntaxHighlighting = syntaxHighlighting;
                        newTextEditor.FontSize = FontSize;
                        newTextEditor.Options = oldTextEditor.Options;
                        newTextEditor.ShowLineNumbers = oldTextEditor.ShowLineNumbers;
                        newTextEditor.Language = oldTextEditor.Language;
                        newTextEditor.FontFamily = oldTextEditor.FontFamily;
                        newTextEditor.Foreground = oldTextEditor.Foreground;


                        if (port.FixedText)
                            newTextEditor.TextChanged -= port.TextBox_TextChanged;
                        else
                            newTextEditor.TextChanged += port.TextBox_TextChanged;
                        port.TextEditor = newTextEditor;
                        border.Child = newTextEditor;
                        MessageCard.Warning($"{LangManager.GetValueByKey("MemLimitClearLog")}: {port.Name}");
                    }
                }
            });
        }

        private void SetLang()
        {
            // 设置语言
            string lang = ConfigManager.Settings.CurrentLanguage;
            if (!string.IsNullOrEmpty(lang)
                && SuperControls.Style.LangManager.SupportLanguages.Contains(lang)) {
                SuperControls.Style.LangManager.SetLang(lang);
                SuperCom.Lang.LangManager.SetLang(lang);
                Logger.Debug($"{LangManager.GetValueByKey("SetLang")}：{lang}");
            }
        }

        public void RefreshSetting()
        {
            this.CloseToTaskBar = ConfigManager.CommonSettings.CloseToBar;
        }

        /// <summary>
        /// 读取自定义语法高亮
        /// </summary>
        public void ReadXshdList()
        {
            // 记录先前选定的
            Dictionary<AbstractConnector, IHighlightingDefinition> beforeSeclections = new Dictionary<AbstractConnector, IHighlightingDefinition>();
            List<TabItemPro> tabItemPros = tabPanelPro.ItemsSource.ToList();
            foreach (var item in tabItemPros) {
                if (item.TabItemControl is ComTemplate comTemplate &&
                    comTemplate.Connector is ComConnector connector) {
                    if (connector.SerialPort == null)
                        continue;
                    beforeSeclections.Add(connector, connector.TextEditor.SyntaxHighlighting);
                }
            }

            HighlightingManager.Instance.Clear();
            string[] xshd_list = FileHelper.TryGetAllFiles(HighLightRule.GetDirName(), "*.xshd");
            foreach (var xshdPath in xshd_list) {
                try {
                    IHighlightingDefinition customHighlighting;
                    using (Stream s = File.OpenRead(xshdPath)) {
                        if (s == null)
                            throw new InvalidOperationException("Could not find embedded resource");
                        using (XmlReader reader = new XmlTextReader(s)) {
                            customHighlighting = ICSharpCode.AvalonEdit.Highlighting.Xshd.
                                HighlightingLoader.Load(reader, HighlightingManager.Instance);
                        }
                    }
                    // 检查是否在数据库中存在
                    string name = customHighlighting.Name;
                    if (HighLightRule.DEFAULT_RULES.Contains(name) || HighLightRule.AllRules.FirstOrDefault(arg => arg.RuleName.Equals(name)) != null)
                        HighlightingManager.Instance.RegisterHighlighting(name, null, customHighlighting);
                } catch (Exception ex) {
                    MessageCard.Error(ex.Message);
                    continue;
                }

            }

            GlobalSettings.HighLightSetting.LoadDefinitions();
            NotifyHighLightChange(beforeSeclections);
            Logger.Info("read high light xshd success");
        }

        private void NotifyHighLightChange(Dictionary<AbstractConnector, IHighlightingDefinition> beforeSeclections)
        {
            List<TabItemPro> tabItemPros = tabPanelPro.ItemsSource.ToList();
            foreach (var item in tabItemPros) {
                if (item.TabItemControl is ComTemplate comTemplate &&
                    comTemplate.Connector is ComConnector comConnector &&
                    beforeSeclections.ContainsKey(comConnector)) {
                    comTemplate.OnHighLightChange(beforeSeclections[comConnector]);
                }
            }
        }


        private void InitSqlite()
        {
            ComSettings.InitSqlite();
            AdvancedSend.InitSqlite();
            HighLightRule.InitSqlite();
            ShortCutBinding.InitSqlite();
            VarMonitor.InitSqlite();
        }


        private void SetPortSelected(object sender, MouseButtonEventArgs e)
        {
            CanDragTabItem = false;
            if (CurrentDragElement != null)
                Mouse.Capture(CurrentDragElement, CaptureMode.None);
        }

        private void BeginDragTabItem(object sender, MouseButtonEventArgs e)
        {
            CanDragTabItem = true;
            CurrentDragElement = sender as FrameworkElement;
            Mouse.Capture(CurrentDragElement, CaptureMode.Element);

            Border border = (Border)sender;
            if (border == null || border.Tag == null)
                return;
            string portName = border.Tag.ToString();
            if (string.IsNullOrEmpty(portName) || vieModel.PortTabItems == null ||
                vieModel.PortTabItems.Count <= 0)
                return;
            SetPortTabSelected(portName);
        }

        private void Border_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!CanDragTabItem)
                return;
        }

        public void SetPortTabSelected(string portName)
        {
            if (vieModel.PortTabItems == null)
                return;
            for (int i = 0; i < vieModel.PortTabItems.Count; i++) {
                if (vieModel.PortTabItems[i].Name.Equals(portName)) {
                    vieModel.PortTabItems[i].Selected = true;
                    SetGridVisible(portName);
                } else {
                    vieModel.PortTabItems[i].Selected = false;
                }
            }
        }


        /// <summary>
        /// 设置下一个选中
        /// </summary>
        /// <param name="forward"></param>
        public void SetPortTabSelected(bool forward)
        {
            if (vieModel.PortTabItems == null || vieModel.PortTabItems.Count <= 1)
                return;
            int idx = 0;
            for (int i = 0; i < vieModel.PortTabItems.Count; i++) {
                if (vieModel.PortTabItems[i].Selected) {
                    idx = i;
                    break;
                }
            }
            if (forward)
                idx++;
            else
                idx--;
            if (idx < 0)
                idx = vieModel.PortTabItems.Count - 1;
            if (idx >= vieModel.PortTabItems.Count)
                idx = 0;
            SetPortTabSelected(vieModel.PortTabItems[idx].Name);
        }

        private void SetGridVisible(string portName)
        {
            // todo
            //Logger.Debug(portName);
            //if (string.IsNullOrEmpty(portName))
            //    return;
            //for (int i = 0; i < itemsControl.Items.Count; i++) {
            //    ContentPresenter presenter = (ContentPresenter)itemsControl.ItemContainerGenerator.ContainerFromItem(itemsControl.Items[i]);
            //    if (presenter == null) {
            //        Logger.Debug($"presenter[{i}] is null");
            //        continue;
            //    }
            //    Grid grid = VisualHelper.FindElementByName<Grid>(presenter, "baseGrid");
            //    if (grid == null || grid.Tag == null) {
            //        Logger.Debug($"presenter[{i}] baseGrid is null");
            //        continue;
            //    }

            //    string name = grid.Tag.ToString();
            //    if (portName.Equals(name))
            //        grid.Visibility = Visibility.Visible;
            //    else
            //        grid.Visibility = Visibility.Hidden;
            //}
        }


        // todo 滚动tab栏
        private void PortTab_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scrollViewer = sender as ScrollViewer;
            scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset - e.Delta);
            e.Handled = true;
        }


        private async void ClosePortTabItemByName(string portName, Button button = null)
        {
            if (string.IsNullOrEmpty(portName) || vieModel.PortTabItems?.Count <= 0)
                return;
            await RemovePortTabItem(portName, button);
            // 默认选中 0
            if (vieModel.PortTabItems.Count > 0)
                SetPortTabSelected(vieModel.PortTabItems[0].Name);
        }

        private async Task<bool> RemovePortTabItem(string portName, Button button = null)
        {
            if (vieModel.PortTabItems == null || string.IsNullOrEmpty(portName))
                return false;

            int idx = -1;
            try {
                for (int i = 0; idx < vieModel.PortTabItems.Count; i++) {
                    if (portName.Equals(vieModel.PortTabItems[i].Name)) {
                        idx = i;
                        break;
                    }
                }

                if (idx >= 0 && idx < vieModel.PortTabItems.Count) {
                    if (button != null)
                        button.IsEnabled = false;
                    // todo
                    //bool success = await ClosePort(portName);
                    //if (success) {
                    //    vieModel.PortTabItems[idx].Pinned = false;
                    //    SavePinnedByName(portName, false);
                    //    SaveComSettings();
                    //    await RemovePortsByName(new List<string> { portName });
                    //}
                    if (button != null)
                        button.IsEnabled = true;
                }
                return true;
            } catch (Exception ex) {
                MessageNotify.Error(ex.Message);
                Logger?.Error(ex);
                return false;
            }

        }

        private async Task<bool> RemovePortsByName(List<string> portNames)
        {
            if (portNames == null || portNames.Count == 0)
                return true;
            SaveComSettings();
            List<string> toRemoved = new List<string>();
            bool success = false;
            foreach (var name in portNames) {
                vieModel.DoingWorkMsg = $"{LangManager.GetValueByKey("ClosePort")}: {name}";
                //success = await ClosePort(name); // todo
                if (success)
                    toRemoved.Add(name);
            }
            // 移除 item
            foreach (var portName in toRemoved) {
                if (vieModel.PortTabItems.FirstOrDefault(arg => arg.Name.Equals(portName))
                    is ComConnector portTabItem) {
                    vieModel.PortTabItems.Remove(portTabItem);
                    comSidePanel.Update(portName.GetHashCode(), "PortTabItem", null);
                }
            }
            return true;
        }

        private void OnTabItemProClose(TabItemPro tabItemPro)
        {
            if (tabItemPro.TabItemControl is IConnectTemplate connectTemplate)
                connectTemplate.Close();
        }


        private void OpenTabBar(string name, ConnectType connectType, bool connect)
        {
            TabItemPro tabItemPro = tabPanelPro.GetItemById(name);
            if (tabItemPro != null) {
                // 已经打开，则设为选中
                tabItemPro.Selected = true;
                Logger.Warn($"tab bar [{name}] is existed");
                return;
            }

            // 未打开，则打开该选项卡
            UIElement element = null;
            if (connectType == ConnectType.Com) {
                ComTemplate comTemplate = new ComTemplate(name);
                comTemplate.OnConnectStatusChanged += OnConnectorChanged;
                element = comTemplate;
            }
            tabItemPro = new ConnectorTabItem(name, connect, element, TabPosition.Top);
            tabItemPro.OnClose += OnTabItemProClose;
            tabPanelPro.Add(tabItemPro);

            // todo
            // object Detail = comSidePanel.Get(portName.GetHashCode(), typeof(SideComPort).GetProperty("Detail"));
            //object PortType = comSidePanel.Get(portName.GetHashCode(), typeof(SideComPort).GetProperty("PortType"));
            //if (Detail != null)
            //    tabItemPro.Detail = Detail.ToString();
            ////if (PortType != null && Enum.TryParse(PortType.ToString(), out PortType portType))
            ////    tabItemPro.PortType = portType;

        }

        public void ScrollIntoView(ComConnector portTabItem)
        {
            // todo
            //if (portTabItem == null)
            //    return;
            //var container = tabItemsControl.ItemContainerGenerator.ContainerFromItem(portTabItem) as FrameworkElement;
            //if (container != null)
            //    container.BringIntoView();
        }

        private void ShowAbout(object sender, RoutedEventArgs e)
        {
            Dialog_About about = new Dialog_About();
            string local = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            local = local.Substring(0, local.Length - ".0.0".Length);
            about.AppName = ConfigManager.APP_NAME;
            about.AppSubName = ConfigManager.APP_SUB_NAME;
            about.Version = local;
            about.ReleaseDate = ConfigManager.RELEASE_DATE;
            about.Author = UrlManager.AUTHOR;
            about.License = UrlManager.LICENSE;
            about.GithubUrl = UrlManager.GITHUB_URL;
            about.WebUrl = UrlManager.WEB_URL;
            about.JoinGroupUrl = UrlManager.JOIN_GROUP_URL;
            about.Image = SuperUtils.Media.ImageHelper
                .ImageFromUri("pack://application:,,,/SuperCom;Component/Resources/Ico/ICON_256.png");
            about.ShowDialog();
        }

        private void OpenContextMenu(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button == null || button.ContextMenu == null)
                return;
            button.ContextMenu.IsOpen = true;
        }















        private (ToggleButton, TextEditor) FindToggleButtonByBaseGrid(Grid baseGrid)
        {
            Grid rooGrid = baseGrid.Children.OfType<Grid>().FirstOrDefault();
            Border firstBorder = rooGrid.Children.OfType<Border>().FirstOrDefault();
            Border lastBorder = rooGrid.Children.OfType<Border>().LastOrDefault();
            ToggleButton toggleButton = (lastBorder.Child as StackPanel).Children.OfType<ToggleButton>().FirstOrDefault();
            return (toggleButton, firstBorder.Child as TextEditor);
        }




        private string GetPortName(FrameworkElement element)
        {
            if (element == null)
                return null;
            StackPanel stackPanel = element.Parent as StackPanel;
            if (stackPanel != null && stackPanel.Parent is Border border) {
                if (border.Tag != null) {
                    return border.Tag.ToString();
                }
            }
            return null;
        }






        private void ShowSettingsPopup(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) {
                Border border = sender as Border;
                ContextMenu contextMenu = border.ContextMenu;
                contextMenu.PlacementTarget = border;
                contextMenu.Placement = PlacementMode.Top;
                contextMenu.IsOpen = true;
            }
            e.Handled = true;
        }

        private void ShowContextMenu(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) {
                Border border = sender as Border;
                ContextMenu contextMenu = border.ContextMenu;
                contextMenu.PlacementTarget = border;
                contextMenu.Placement = PlacementMode.Bottom;
                contextMenu.IsOpen = true;
            }
            e.Handled = true;
        }

        private void SplitPanel(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button == null)
                return;
            if (button.Parent is Grid grid) {
                SplitPanel(SplitPanelType.Left | SplitPanelType.Right);
            } else if (button.Parent is StackPanel panel) {
                int idx = panel.Children.IndexOf(button);
                if (idx == 0) {
                    SplitPanel(SplitPanelType.Top | SplitPanelType.Bottom);
                } else if (idx == 1) {
                    SplitPanel(SplitPanelType.Top | SplitPanelType.Bottom | SplitPanelType.Left | SplitPanelType.Right);
                } else if (idx == 2) {
                    SplitPanel(SplitPanelType.Bottom | SplitPanelType.Left | SplitPanelType.Right);
                } else if (idx == 3) {
                    SplitPanel(SplitPanelType.Top | SplitPanelType.Left | SplitPanelType.Right);
                } else if (idx == 4) {
                    SplitPanel(SplitPanelType.None);
                }
            }
        }

        private void SplitPanel(SplitPanelType type)
        {
            if (type == SplitPanelType.None) {
                Console.WriteLine(SplitPanelType.None);
            }
            if ((type & SplitPanelType.Left) != 0) {
                Console.WriteLine(SplitPanelType.Left);
            }
            if ((type & SplitPanelType.Right) != 0) {
                Console.WriteLine(SplitPanelType.Right);
            }
            if ((type & SplitPanelType.Top) != 0) {
                Console.WriteLine(SplitPanelType.Top);
            }
            if ((type & SplitPanelType.Bottom) != 0) {
                Console.WriteLine(SplitPanelType.Bottom);
            }
        }

        private void mainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // 保存配置
            SaveOpeningPorts();
            SaveComSettings();
            SaveConfigValue();
            GlobalSettings.ComSetting.SaveBaudRate();
            try {
                RemoveAllBar();
            } catch (Exception ex) {
                App.Logger.Error(ex.Message);
            }

            // 注意，只有启用防止系统休眠，关闭 App 后才取消该休眠
            if (ConfigManager.Settings.AvoidScreenClose)
                Win32Helper.CancelPreventSleep();
        }



        /// <summary>
        /// 保存串口的配置文件
        /// </summary>
        private void SaveComSettings()
        {
            foreach (var portTabItem in vieModel.PortTabItems) {

                ComSettings comSettings = GlobalSettings.ComSetting.GetComSetting(portTabItem.Name);
                if (comSettings == null)
                    comSettings = new ComSettings();
                comSettings.PortName = portTabItem.Name;
                comSettings.Connected = portTabItem.Connected;
                // PortTabItem portTabItem = item.PortTabItem;

                comSettings.WriteData = portTabItem.WriteData;
                comSettings.AddNewLineWhenWrite = portTabItem.AddNewLineWhenWrite;
                comSettings.SendHex = portTabItem.SendHex;
                comSettings.RecvShowHex = portTabItem.RecvShowHex;
                comSettings.EnabledFilter = portTabItem.EnabledFilter;
                comSettings.EnabledMonitor = portTabItem.EnabledMonitor;
                comSettings.AddTimeStamp = portTabItem.AddTimeStamp;
                portTabItem.SerialPort.RefreshSetting();
                comSettings.PortSetting = portTabItem.SerialPort?.SettingJson;

                MapperManager.ComMapper.Insert(comSettings, InsertMode.Replace);
            }
        }

        private void SaveOpeningPorts()
        {
            ConfigManager.Main.OpeningPorts = JsonUtils.TrySerializeObject(vieModel.PortTabItems.Select(arg => arg.Name).ToList());
            ConfigManager.Main.Save();
        }


        private void SaveConfigValue()
        {
            ConfigManager.Main.X = this.Left;
            ConfigManager.Main.Y = this.Top;
            ConfigManager.Main.Width = this.Width;
            ConfigManager.Main.Height = this.Height;
            //ConfigManager.Main.WindowState = (long)baseWindowState;
            ConfigManager.Main.SideGridWidth = SideGridColumn.ActualWidth;
            ConfigManager.Main.Save();
        }







        private string GetCurrentText(FrameworkElement element)
        {
            MenuItem menuItem = element as MenuItem;
            if (menuItem != null && menuItem.Parent is ContextMenu contextMenu) {
                if (contextMenu.PlacementTarget is TextEditor textEditor) {
                    return textEditor.SelectedText;
                }
            }
            return null;
        }



        public void ApplyScreenStatus()
        {
            if (ConfigManager.Settings.AvoidScreenClose)
                Win32Helper.PreventSleep();
        }

        public void InitNotice()
        {
            noticeViewer.SetConfig(UrlManager.NOTICE_URL, ConfigManager.Main.LatestNotice);
            noticeViewer.onError += (error) => {
                App.Logger?.Error(error);
            };

            noticeViewer.onShowMarkdown += (markdown) => {
                //MessageCard.Info(markdown);
            };
            noticeViewer.onNewNotice += (newNotice) => {
                ConfigManager.Main.LatestNotice = newNotice;
                ConfigManager.Main.Save();
            };

            noticeViewer.BeginCheckNotice();
        }

        public void SetBaudRateAction()
        {
            // todo
            //vieModel.OnBaudRatesChanged += (beforePorts) => {

            //};
        }

        private void LoadFontFamily()
        {
            int idx = 1;
            int count = VisualHelper.SYSTEM_FONT_FAMILIES.Keys.Count;
            foreach (string fontName in VisualHelper.SYSTEM_FONT_FAMILIES.Keys) {
                MenuItem menuItem = new MenuItem();
                menuItem.Header = fontName;
                menuItem.FontFamily = VisualHelper.SYSTEM_FONT_FAMILIES[fontName];
                menuItem.IsCheckable = true;
                menuItem.IsChecked = fontName.Equals(ConfigManager.Main.TextFontName);
                menuItem.Checked += (s, e) => {
                    string name = (s as MenuItem).Header.ToString();
                    SetFontFamily(name);


                };
                FontMenuItem.Items.Add(menuItem);
                //Logger.Info($"[{idx++}/{count}]load font: {fontName}");
            }
        }

        public void SetFontFamily(string name)
        {
            if (string.IsNullOrEmpty(name))
                return;
            foreach (MenuItem item in FontMenuItem.Items) {
                if (name.Equals(item.Header.ToString()))
                    continue;
                item.IsChecked = false;
            }

            foreach (ComConnector item in vieModel.PortTabItems) {
                TextEditor textEditor = item.TextEditor;
                if (textEditor != null)
                    textEditor.FontFamily = VisualHelper.SYSTEM_FONT_FAMILIES[name];
            }
            ConfigManager.Main.TextFontName = name;
            ConfigManager.Main.Save();
            Logger.Info($"set font: {name}");
        }

        public void InitUpgrade()
        {
            UpgradeHelper.Init();
            UpgradeHelper.OnBeforeCopyFile += () => {
                CloseAllPorts(null, null);
                this.CloseToTaskBar = false;
                this.Close();
            };
            CheckUpgrade();
        }

        private void LoadDonateConfig()
        {
            vieModel.ShowDonate = true;
            string json_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_config.json");
            if (File.Exists(json_path)) {
                string v = FileHelper.TryReadFile(json_path);
                if (!string.IsNullOrEmpty(v)) {
                    Dictionary<string, object> dict = JsonUtils.TryDeserializeObject<Dictionary<string, object>>(v);
                    if (dict != null && dict.ContainsKey("ShowDonate")) {
                        string showDonate = dict["ShowDonate"].ToString();
                        if (!string.IsNullOrEmpty(showDonate)) {
                            vieModel.ShowDonate = showDonate.ToLower().Equals("false") ? false : true;
                        }
                    }
                }
            }
        }

        public void InitThemeSelector()
        {
            ThemeSelectorDefault.AddTransParentColor("TabItem.Background");
            ThemeSelectorDefault.AddTransParentColor("Window.Title.Background");
            ThemeSelectorDefault.AddTransParentColor("Window.Side.Background");
            ThemeSelectorDefault.AddTransParentColor("Window.Side.Hover.Background");
            ThemeSelectorDefault.AddTransParentColor("ListBoxItem.Background");
            ThemeSelectorDefault.SetThemeConfig(ConfigManager.Settings.ThemeIdx, ConfigManager.Settings.ThemeID);
            ThemeSelectorDefault.onThemeChanged += (ThemeIdx, ThemeID) => {
                ConfigManager.Settings.ThemeIdx = ThemeIdx;
                ConfigManager.Settings.ThemeID = ThemeID;
                ConfigManager.Settings.Save();
            };
            ThemeSelectorDefault.onBackGroundImageChanged += (image) => {
                ImageBackground.Source = image;
            };
            ThemeSelectorDefault.onSetBgColorTransparent += () => {
                BorderTitle.Background = Brushes.Transparent;
            };

            ThemeSelectorDefault.onReSetBgColorBinding += () => {
                BorderTitle.SetResourceReference(Control.BackgroundProperty, "Window.Title.Background");
            };

            ThemeSelectorDefault.InitThemes();
        }

        private async void OpenBeforePorts()
        {
            // todo
            //if (string.IsNullOrEmpty(ConfigManager.Main.OpeningPorts))
            //    return;
            //List<string> list = JsonUtils.TryDeserializeObject<List<string>>(ConfigManager.Main.OpeningPorts);
            //foreach (string portName in list) {
            //    ComSettings comSettings = vieModel.GetComSettingList().FirstOrDefault(arg => arg.PortName.Equals(portName));
            //    if (comSettings != null && comSettings.Connected) {
            //        // 这里不需要等待
            //        OpenPort(portName);
            //    } else {
            //        // 这里不需要等待
            //        OpenTab(portName, false);
            //        OpenPort(portName, false);// todo
            //    }
            //}
            //SetFontFamily(ConfigManager.Main.TextFontName);
        }

        public void AdjustWindow()
        {

            if (ConfigManager.Main.FirstRun) {
                this.Width = SystemParameters.WorkArea.Width * 0.8;
                this.Height = SystemParameters.WorkArea.Height * 0.8;
                this.Left = SystemParameters.WorkArea.Width * 0.1;
                this.Top = SystemParameters.WorkArea.Height * 0.1;
            } else {
                //if (ConfigManager.Main.Height == SystemParameters.WorkArea.Height && ConfigManager.Main.Width < SystemParameters.WorkArea.Width)
                //{
                //    //baseWindowState = 0;
                //    this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                //    this.CanResize = true;
                //}
                //else
                //{
                //    this.Left = ConfigManager.Main.X;
                //    this.Top = ConfigManager.Main.Y;
                //    this.Width = ConfigManager.Main.Width;
                //    this.Height = ConfigManager.Main.Height;
                //}


                //baseWindowState = (BaseWindowState)ConfigManager.Main.WindowState;
                //if (baseWindowState == BaseWindowState.FullScreen)
                //{
                //    this.WindowState = System.Windows.WindowState.Maximized;
                //}
                //else if (baseWindowState == BaseWindowState.None)
                //{
                //    baseWindowState = 0;
                //    this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                ////}
                //if (this.Width == SystemParameters.WorkArea.Width
                //    && this.Height == SystemParameters.WorkArea.Height) baseWindowState = BaseWindowState.Maximized;

                //if (baseWindowState == BaseWindowState.Maximized || baseWindowState == BaseWindowState.FullScreen)
                //{
                //    MaxPath.Data = Geometry.Parse(PathData.MaxToNormalPath);
                //    MaxMenuItem.Header = "窗口化";
                //}


            }
        }



        private void Border_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (SideGridColumn.ActualWidth <= 100) {
                SideGridColumn.Width = new GridLength(0);
                sideGridMenuItem.IsChecked = false;
            }
        }

        private void OpenSetting()
        {
            window_Setting?.Close();
            window_Setting = new Window_Setting();
            window_Setting.Owner = this;
            window_Setting.Show();
            window_Setting.Focus();
            window_Setting.BringIntoView();
        }

        private void OpenSetting(object sender, RoutedEventArgs e)
        {
            OpenSetting();
        }

        #region "历史记录弹窗"






        private void SetSelectedStatus(ItemsControl itemsControl)
        {
            if (itemsControl == null || itemsControl.ItemsSource == null)
                return;
            for (int i = 0; i < itemsControl.Items.Count; i++) {
                ContentPresenter presenter = (ContentPresenter)itemsControl.ItemContainerGenerator.ContainerFromItem(itemsControl.Items[i]);
                if (presenter == null)
                    continue;
                Border border = VisualHelper.FindElementByName<Border>(presenter, "baseBorder");
                if (border == null)
                    continue;
                if (i == vieModel.SendHistorySelectedIndex) {
                    border.Background = (Brush)FindResource("ListBoxItem.Selected.Active.Background");
                    border.BorderBrush = (Brush)FindResource("ListBoxItem.Selected.Active.BorderBrush");
                } else {
                    border.SetResourceReference(Control.BackgroundProperty, "Background");
                    border.SetResourceReference(Control.BorderBrushProperty, "BorderBrush");
                }
                // 滚动当前视图
                double offset = vieModel.SendHistorySelectedIndex * border.ActualHeight;
                ScrollViewer scrollViewer = itemsControl.Parent as ScrollViewer;
                scrollViewer.ScrollToVerticalOffset(offset);
            }

        }


        #endregion



        private void HideSide(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            if (menuItem.IsChecked) {
                SideGridColumn.Width = new GridLength(200);
            } else {
                SideGridColumn.Width = new GridLength(0);
                Border_SizeChanged(null, null);
            }

        }




        public void RefreshSendCommands()
        {
            GlobalSettings.SendCommand.LoadSendCommands();
            List<TabItemPro> tabItemPros = tabPanelPro.ItemsSource.ToList();
            foreach (var item in tabItemPros) {
                if (item.TabItemControl is ComTemplate comTemplate) {
                    comTemplate.SetComboboxStatus();
                }
            }
        }

        private ComboBox FindCombobox(TextEditor textEditor)
        {
            Grid grid = (textEditor.Parent as Border).Parent as Grid;
            Grid rootGrid = grid.Parent as Grid;
            Grid borderGrid = rootGrid.Children.OfType<Grid>().LastOrDefault();
            Border border = borderGrid.Children.OfType<Border>().Last();
            Grid grid1 = border.Child as Grid;
            StackPanel stackPanel = grid1.Children.OfType<StackPanel>().FirstOrDefault();
            if (stackPanel != null)
                return stackPanel.Children.OfType<ComboBox>().LastOrDefault();

            return null;
        }

        private void ScrollViewer_PreviewMouseWheel_1(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scrollViewer = sender as ScrollViewer;
            scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset - e.Delta);
            e.Handled = true;
        }

        private void Remark(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            ContextMenu contextMenu = menuItem.Parent as ContextMenu;
            FrameworkElement frameworkElement = contextMenu.PlacementTarget as FrameworkElement;
            if (frameworkElement != null && frameworkElement.Tag != null) {
                string name = frameworkElement.Tag.ToString();
                Remark(name);
            }
        }

        private void Remark(string name)
        {
            ComConnector portTabItem = vieModel.PortTabItems.Where(arg => arg.Name.Equals(name)).FirstOrDefault();
            if (portTabItem != null) {
                Logger.Info("click remark btn");
                DialogInput dialogInput = new DialogInput(LangManager.GetValueByKey("PleaseEnterRemark"), portTabItem.Remark);
                if (dialogInput.ShowDialog(this) == true) {
                    string value = dialogInput.Text;
                    portTabItem.Remark = value;
                    portTabItem.SerialPort.SaveRemark(value);
                    comSidePanel.Update(name.GetHashCode(), "Remark", value);
                    ComSettings comSettings = GlobalSettings.ComSetting.GetComSetting(name);
                    if (comSettings != null) {
                        Dictionary<string, object> dict = JsonUtils.TryDeserializeObject<Dictionary<string, object>>(comSettings.PortSetting);
                        if (dict != null && dict.ContainsKey("Remark")) {
                            dict["Remark"] = value;
                            comSettings.PortSetting = JsonUtils.TrySerializeObject(dict);
                            Logger.Info($"set remark: {value}");
                        }
                    }
                }
            } else {
                MessageNotify.Info(LangManager.GetValueByKey("RemarkAfterOpenPort"));
            }
        }



        private void OpenLog(object sender, RoutedEventArgs e)
        {
            MessageNotify.Info(LangManager.GetValueByKey("Developing"));
        }




        private void ShowPluginWindow(object sender, RoutedEventArgs e)
        {
            Window_Plugin window_Plugin = new Window_Plugin();

            PluginConfig config = new PluginConfig();
            config.PluginBaseDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins");
            config.RemoteUrl = UrlManager.GetPluginUrl();
            // 读取本地配置
            window_Plugin.OnEnabledChange += (data, enabled) => {
                return true;
            };

            window_Plugin.OnBeginDelete += (data) => {
                return true;
            };

            window_Plugin.OnBeginDownload += (data) => {
                return true;
            };

            window_Plugin.SetConfig(config);
            window_Plugin.Icon = this.Icon;
            window_Plugin.Show();
        }

        private async void CheckUpgrade()
        {
            // 启动后检查更新
            try {
                await Task.Delay(UpgradeHelper.AUTO_CHECK_UPGRADE_DELAY);
                (string LatestVersion, string ReleaseDate, string ReleaseNote) result = await UpgradeHelper.GetUpgradeInfo();
                string remote = result.LatestVersion;
                string ReleaseDate = result.ReleaseDate;
                if (!string.IsNullOrEmpty(remote)) {
                    string local = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
                    local = local.Substring(0, local.Length - ".0.0".Length);
                    if (local.CompareTo(remote) < 0) {
                        MessageCard.Info($"{LangManager.GetValueByKey("ExistNewVersion")}\n{LangManager.GetValueByKey("Version")}：{remote}\n{LangManager.GetValueByKey("Date")}：{ReleaseDate}", () => {
                            UpgradeHelper.OpenWindow(this);
                        }, targetWindow: this);
                    }
                }
            } catch (Exception ex) {
                App.Logger.Error(ex.Message);
            }
        }




        private async Task<bool> BackupData()
        {
            if (ConfigManager.Settings.AutoBackup) {
                int period = Config.WindowConfig.Settings.BackUpPeriods[(int)ConfigManager.Settings.AutoBackupPeriodIndex];
                bool backup = false;
                string BackupPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "backup");
                string[] arr = DirHelper.TryGetDirList(BackupPath);
                if (arr != null && arr.Length > 0) {
                    string dirname = arr[arr.Length - 1];
                    if (Directory.Exists(dirname)) {
                        string dirName = Path.GetFileName(dirname);
                        DateTime before = DateTime.Now.AddDays(1);
                        DateTime now = DateTime.Now;
                        DateTime.TryParse(dirName, out before);
                        if (now.CompareTo(before) < 0 || (now - before).TotalDays > period) {
                            backup = true;
                        }
                    }
                } else {
                    backup = true;
                }

                if (backup) {
                    string dir = Path.Combine(BackupPath, DateHelper.NowDate());
                    bool created = DirHelper.TryCreateDirectory(dir);
                    if (!created)
                        return false;
                    string target_file = Path.Combine(dir, ConfigManager.SQLITE_DATA_PATH);
                    string origin_file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigManager.SQLITE_DATA_PATH);
                    FileHelper.TryCopyFile(origin_file, target_file);
                    // 复制语法高亮
                    string highLightDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AvalonEdit");
                    DirHelper.TryCopy(highLightDir, Path.Combine(dir, "AvalonEdit"));
                }
            }

            await Task.Delay(1);
            return false;
        }












        private void OpenShortCut(object sender, RoutedEventArgs e)
        {
            if (window_ShortCut != null)
                window_ShortCut.Close();
            window_ShortCut = null;
            window_ShortCut = new Window_ShortCut();
            window_ShortCut.ShowDialog();
            window_ShortCut.BringIntoView();
            window_ShortCut.Activate();
        }

        #region "快捷键处理"

        private async void OpenCloseCurrentPort(string name)
        {
            if (vieModel.PortTabItems == null || vieModel.PortTabItems.Count == 0)
                return;
            ComConnector item = vieModel.PortTabItems.Where(arg => arg.Name.Equals(name)).First();
            if (item == null)
                return;


            // todo
            //if (item.Connected)
            //    await ClosePort(name);
            //else
            //    await OpenPort(name);
        }

        public List<string> GetCurrentTab()
        {
            if (vieModel.PortTabItems == null || vieModel.PortTabItems.Count == 0)
                return new List<string>();
            return vieModel.PortTabItems.Select(arg => arg.Name).ToList();
        }

        public ComConnector GetPortTabItemByName(string name)
        {
            if (vieModel.PortTabItems == null || vieModel.PortTabItems.Count == 0)
                return null;
            return vieModel.PortTabItems.Where(arg => arg.Name.Equals(name)).First();
        }

        public bool IsPortConnecting()
        {
            if (vieModel.PortTabItems == null || vieModel.PortTabItems.Count == 0)
                return false;
            foreach (var item in vieModel.PortTabItems) {
                if (item.Connected)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 收起展开发送栏
        /// </summary>
        /// <param name="sender"></param>
        private void ExpandSendPanel(object sender)
        {
            Grid baseGrid = sender as Grid;
            if (baseGrid != null) {
                double height = baseGrid.RowDefinitions[2].ActualHeight;
                if (height <= 10)
                    baseGrid.RowDefinitions[2].Height = new GridLength(DEFAULT_SEND_PANEL_HEIGHT, GridUnitType.Pixel);
                else
                    baseGrid.RowDefinitions[2].Height = new GridLength(0, GridUnitType.Pixel);
            }
        }

        #endregion

        private bool KeyDownInTab(object sender)
        {
            return sender is Grid;
        }

        private void onPreviewKeyDown(object sender, KeyEventArgs e)
        {
            string portName = "";
            if (vieModel.PortTabItems?.Count > 0) {
                foreach (var portTabItem in vieModel.PortTabItems) {
                    if (portTabItem.Selected) {
                        portName = portTabItem.Name;
                        break;
                    }
                }
            }

            App.Logger.Debug($"{LangManager.GetValueByKey("PressKey")}：{e.Key}");

            // 快捷键检测
            ShortCutBinding shortCutBinding = null;
            int max = 0;
            foreach (var item in vieModel.ShortCutBindings) {
                // 贪婪匹配：最多的按键按下
                if (KeyBoardHelper.IsAllKeyDown(item.KeyList) && item.KeyList.Count > max) {
                    shortCutBinding = item;
                    max = item.KeyList.Count;
                }
            }
            if (shortCutBinding == null)
                return;
            ShortCutType type = (ShortCutType)shortCutBinding.KeyID;
            switch (type) {
                case ShortCutType.OpenCloseCurrentPort:
                    if (KeyDownInTab(sender))
                        OpenCloseCurrentPort(portName);
                    break;
                case ShortCutType.ExpandSendingBar:
                    if (KeyDownInTab(sender))
                        ExpandSendPanel(sender);
                    break;
                case ShortCutType.FullScreen:
                    // 全屏
                    if (this.WindowState == WindowState.Maximized) {
                        this.WindowState = WindowState.Normal;
                    } else if (this.WindowState == WindowState.Normal) {
                        this.WindowState = WindowState.Maximized;
                    }
                    e.Handled = true;
                    break;
                case ShortCutType.PinOrScroll: {
                        if (!KeyDownInTab(sender))
                            return;
                        // 固定滚屏
                        ComConnector portTabItem = vieModel.PortTabItems.FirstOrDefault(arg => arg.Name.Equals(portName));
                        if (portTabItem != null)
                            portTabItem.FixedText = !portTabItem.FixedText;
                    }
                    break;
                case ShortCutType.HexTransform: {
                        if (!KeyDownInTab(sender))
                            return;
                        // hex 转换
                        Grid baseGrid = sender as Grid;
                        (ToggleButton toggleButton, TextEditor textEditor) = FindToggleButtonByBaseGrid(baseGrid);
                        // todo
                        //if (textEditor != null)
                        //    OpenHex(textEditor.SelectedText);
                    }
                    break;
                case ShortCutType.TimeStampTransform: {
                        if (!KeyDownInTab(sender))
                            return;
                        // 时间戳
                        Grid baseGrid = sender as Grid;
                        (ToggleButton toggleButton, TextEditor textEditor) = FindToggleButtonByBaseGrid(baseGrid);
                        // todo
                        //if (textEditor != null)
                        //    OpenTime(textEditor.SelectedText);
                    }
                    break;
                case ShortCutType.FormatToJSON: {
                        if (!KeyDownInTab(sender))
                            return;
                        // 格式化为 JSON
                        Grid baseGrid = sender as Grid;
                        (ToggleButton toggleButton, TextEditor textEditor) = FindToggleButtonByBaseGrid(baseGrid);
                        if (textEditor != null) {
                            string origin = textEditor.SelectedText;
                            // todo
                            //string format = FormatString(FormatType.JSON, origin);
                            //textEditor.SelectedText = format;
                        }
                    }
                    break;
                case ShortCutType.JoinLine: {
                        if (!KeyDownInTab(sender))
                            return;
                        // 合并为一行
                        Grid baseGrid = sender as Grid;
                        (ToggleButton toggleButton, TextEditor textEditor) = FindToggleButtonByBaseGrid(baseGrid);
                        if (textEditor != null) {
                            string origin = textEditor.SelectedText;
                            // todo
                            //string format = FormatString(FormatType.JOINLINE, origin);
                            //textEditor.SelectedText = format;
                        }
                    }

                    break;
                case ShortCutType.SaveLogAs:
                    SaveLog(null, null);// 另存为
                    e.Handled = true;
                    break;
                case ShortCutType.Close:
                    ClosePortTabItemByName(portName);
                    e.Handled = true;
                    break;
                case ShortCutType.PinnedTab:
                    // todo
                    // PinPort(vieModel.PortTabItems.FirstOrDefault(arg => arg.Name.Equals(portName)));
                    e.Handled = true;
                    break;
                default:


                    break;

            }
        }

        public void OnShortCutChanged()
        {
            vieModel.LoadShortCut();
        }

        private void OpenFeedBack(object sender, RoutedEventArgs e)
        {
            FileHelper.TryOpenUrl(UrlManager.FeedbackUrl);
        }

        private void OpenHelp(object sender, RoutedEventArgs e)
        {
            FileHelper.TryOpenUrl(UrlManager.HelpUrl);
        }

        private void OpenDonate(object sender, RoutedEventArgs e)
        {
            Window_Donate window_Donate = new Window_Donate();
            window_Donate.SetUrl(UrlManager.GetDonateJsonUrl());
            window_Donate.ShowDialog();
        }

        private TextEditor GetTextEditorFromMenuItem(MenuItem menuItem, int depth = 0)
        {
            if (depth == 0) {
                if (menuItem != null && menuItem.Parent is ContextMenu contextMenu) {
                    if (contextMenu.PlacementTarget is TextEditor textEditor) {
                        return textEditor;
                    }
                }
            } else if (depth == 1) {
                if (menuItem != null && (menuItem.Parent as MenuItem).Parent is ContextMenu contextMenu) {
                    if (contextMenu.PlacementTarget is TextEditor textEditor) {
                        return textEditor;
                    }
                }
            }
            return null;
        }






        private void ShowUpgradeWindow(object sender, RoutedEventArgs e)
        {
            UpgradeHelper.OpenWindow(this);
        }





        private bool GetMenuItemCheckedStatus(object sender)
        {
            return (sender as MenuItem).IsChecked;
        }


        private void SetTextEditOption(string optionName, object status)
        {
            Logger.Info($"set {optionName}: {status}");
            foreach (ComConnector item in vieModel.PortTabItems) {
                TextEditor textEditor = item.TextEditor;
                if (textEditor != null) {
                    TextEditorOptions options = textEditor.Options;
                    if (options == null)
                        continue;
                    System.Reflection.PropertyInfo propertyInfo = options.GetType().GetProperty(optionName);
                    if (propertyInfo == null)
                        continue;
                    propertyInfo.SetValue(options, status);
                }
            }
        }
        private void SetTextPropOption(string propName, object status)
        {
            foreach (ComConnector item in vieModel.PortTabItems) {
                TextEditor textEditor = item.TextEditor;
                if (textEditor != null) {
                    System.Reflection.PropertyInfo propertyInfo = textEditor.GetType().GetProperty(propName);
                    if (propertyInfo == null)
                        continue;
                    propertyInfo.SetValue(textEditor, status);
                }
            }

        }

        private void SetTextViewReturn(object sender, RoutedEventArgs e)
        {
            bool status = GetMenuItemCheckedStatus(sender);
            SetTextEditOption("ShowEndOfLine", status);
        }

        private void SetTextViewSpace(object sender, RoutedEventArgs e)
        {
            bool status = GetMenuItemCheckedStatus(sender);
            SetTextEditOption("ShowSpaces", status);
        }

        private void SetTextViewTab(object sender, RoutedEventArgs e)
        {
            bool status = GetMenuItemCheckedStatus(sender);
            SetTextEditOption("ShowTabs", status);
        }

        private void SetTextHighLightCurrent(object sender, RoutedEventArgs e)
        {
            bool status = GetMenuItemCheckedStatus(sender);
            SetTextEditOption("HighlightCurrentLine", status);
        }

        private void SetTextViewLineNumber(object sender, RoutedEventArgs e)
        {
            bool value = GetMenuItemCheckedStatus(sender);
            SetTextPropOption("ShowLineNumbers", value);

        }

        private void colorPicker_SelectedColorChanged(object sender, EventArgs e)
        {
            ColorPicker colorPicker = sender as ColorPicker;
            SolidColorBrush brush = new SolidColorBrush(colorPicker.SelectedColor);
            SetTextPropOption("Foreground", brush);
            ConfigManager.Main.TextForeground = brush.ToString();

        }


        private void ShowVirtualPort(object sender, RoutedEventArgs e)
        {
            if (virtualPort == null) {
                virtualPort = new Window_VirtualPort();
                virtualPort.Show();
            } else {
                if (virtualPort.IsClosed) {
                    virtualPort = new Window_VirtualPort();
                    virtualPort.Show();
                }
                virtualPort.BringIntoView();
                virtualPort.Focus();
            }
        }


        private void CloseForegroundSelected(object sender, RoutedEventArgs e)
        {
            ForegroundPopup.IsOpen = false;
        }

        private void OpenForegroundSelected(object sender, RoutedEventArgs e)
        {
            ForegroundPopup.IsOpen = true;
            string colorText = ConfigManager.Main.TextForeground;
            if (!string.IsNullOrEmpty(colorText)) {

                Brush brush = VisualHelper.HexStringToBrush(colorText);
                if (brush != null) {
                    SolidColorBrush solidColorBrush = (SolidColorBrush)brush;
                    colorPicker.SetCurrentColor(solidColorBrush.Color);

                }
            }
        }



        private void OnShowRightPanel(object sender, RoutedEventArgs e)
        {
            ConfigManager.Main.ShowRightPanel = false;
            StackPanel panel = (sender as Button).Parent as StackPanel;
            Grid grid = panel.Parent as Grid;
            Grid rootGrid = grid.Parent as Grid;
            Grid monitorGrid = rootGrid.FindName("monitorGrid") as Grid;
            monitorGrid.Visibility = Visibility.Collapsed;
            ToggleButton toggleButton = panel.Children.OfType<ToggleButton>().FirstOrDefault();
            toggleButton.IsChecked = false;
        }







        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleButton toggleButton = sender as ToggleButton;
            Grid grid = toggleButton.Parent as Grid;
            TextBox textBox = grid.Children.OfType<TextBox>().FirstOrDefault();
            if ((bool)toggleButton.IsChecked) {
                textBox.TextWrapping = TextWrapping.Wrap;

            } else {
                textBox.TextWrapping = TextWrapping.NoWrap;
            }
        }

        private bool CanDragTabItem = false;


        private string GetPortNameByMenuItem(object sender)
        {
            MenuItem menuItem = sender as MenuItem;
            if (menuItem == null)
                return "";
            ContextMenu contextMenu = menuItem.Parent as ContextMenu;
            if (contextMenu != null && contextMenu.PlacementTarget is Border border) {
                if (border.Tag != null)
                    return border.Tag.ToString();
            }
            return "";
        }

        private void SavePinnedByName(string portName, bool pinned)
        {
            ComConnector portTabItem = vieModel.PortTabItems.Where(arg => arg.Name.Equals(portName)).FirstOrDefault();
            if (portTabItem == null)
                return;

            portTabItem.SerialPort.SavePinned(pinned);
            ComSettings comSettings = GlobalSettings.ComSetting.GetComSetting(portName);
            if (comSettings != null) {
                Dictionary<string, object> dict = JsonUtils.TryDeserializeObject<Dictionary<string, object>>(comSettings.PortSetting);
                if (dict != null && dict.ContainsKey("Pinned")) {
                    dict["Pinned"] = pinned.ToString();
                    comSettings.PortSetting = JsonUtils.TrySerializeObject(dict);
                }
            }
        }

        private void CloseCurrentPort(object sender, RoutedEventArgs e)
        {
            string portName = GetPortNameByMenuItem(sender);
            ClosePortTabItemByName(portName);
        }

        private async void CloseOtherPort(object sender, RoutedEventArgs e)
        {
            string portName = GetPortNameByMenuItem(sender);
            if (vieModel == null || vieModel.PortTabItems == null || vieModel.PortTabItems.Count == 0)
                return;
            List<ComConnector> list = vieModel.PortTabItems.ToList();
            int idx = -1;
            for (int i = 0; i < list.Count; i++) {
                if (list[i].Name.Equals(portName)) {
                    idx = i;
                    break;
                }
            }
            List<string> names = new List<string>();
            for (int i = idx + 1; i < list.Count; i++) {
                if (list[i].Pinned)
                    continue;
                names.Add(list[i].Name);
            }
            for (int i = 0; i < idx; i++) {
                if (list[i].Pinned)
                    continue;
                names.Add(list[i].Name);
            }
            vieModel.DoingLongWork = true;
            await RemovePortsByName(names);
            vieModel.DoingLongWork = false;
        }

        private async void CloseAllPorts(object sender, RoutedEventArgs e)
        {
            if (vieModel == null || vieModel.PortTabItems == null || vieModel.PortTabItems.Count == 0)
                return;
            List<string> names = vieModel.PortTabItems.Where(arg => !arg.Pinned).Select(arg => arg.Name).ToList();
            vieModel.DoingLongWork = true;
            await RemovePortsByName(names);
            vieModel.DoingLongWork = false;
        }




        private void CloseAllConnectPort(object sender, RoutedEventArgs e)
        {
            bool close = IsAllClose();
            foreach (var item in vieModel.PortTabItems) {
                string portName = item.Name;
                if (!close) {
                    if (!comSidePanel.Exists(portName.GetHashCode())) {
                        MessageNotify.Error($"{LangManager.GetValueByKey("OpenPortFailed")}: {portName}");
                        continue;
                    }
                    //OpenPort(portName);// todo
                } else {
                    // todo
                    //if (item.Connected)
                    //    ClosePort(portName);
                }
            }
        }

        private void SaveLog(object sender, RoutedEventArgs e)
        {
            ComConnector portTabItem = null;
            foreach (var item in vieModel.PortTabItems) {
                if (item.Selected) {
                    portTabItem = item;
                    break;
                }
            }


            if (portTabItem != null) {
                string fileName = portTabItem.SaveFileName;
                if (File.Exists(fileName)) {
                    string target = FileHelper.SaveFile(null, null, "Normal text file|*.txt|All types|*.*");
                    if (string.IsNullOrEmpty(target)) {
                        return;
                    }
                    if (!FileHelper.IsProperDirName(target)) {
                        MessageNotify.Error(LangManager.GetValueByKey("FileNameInvalid"));
                        return;
                    } else {
                        // 复制到该目录
                        FileHelper.TryCopyFile(fileName, target, true);
                        FileHelper.TryOpenSelectPath(target);
                        Logger.Info($"save log to {target}");
                    }
                } else {
                    MessageNotify.Warning(LangManager.GetValueByKey("CurrentNoLog"));
                }
            }



        }



        private void OpenAppDir(object sender, RoutedEventArgs e)
        {
            FileHelper.TryOpenPath(AppDomain.CurrentDomain.BaseDirectory);
        }

        private void ShowAscii(object sender, RoutedEventArgs e)
        {
            Window_Ascii window_Ascii = new Window_Ascii((int)ConfigManager.CommonSettings.AsciiSelectedIndex);
            window_Ascii.OnSelectedChanged += (index) => {
                ConfigManager.CommonSettings.AsciiSelectedIndex = index;
                ConfigManager.CommonSettings.Save();
            };
            window_Ascii.Icon = this.Icon;
            window_Ascii.Show();
        }

        private void ShowReferences(object sender, RoutedEventArgs e)
        {
            Window_References reference = new Window_References(UrlManager.REFERENCE_DATAS,
                (int)ConfigManager.CommonSettings.RefSelectedIndex);
            reference.OnSelectedChanged += (index) => {
                ConfigManager.CommonSettings.RefSelectedIndex = index;
                ConfigManager.CommonSettings.Save();
            };
            reference.Icon = this.Icon;
            reference.Show();
        }





        private void OpenHFAQ(object sender, RoutedEventArgs e)
        {
            FileHelper.TryOpenUrl(UrlManager.WIKI_FAQ);
        }

        private void OpenDevelop(object sender, RoutedEventArgs e)
        {
            FileHelper.TryOpenUrl(UrlManager.WIKI_DEVELOP);
        }

        private void mainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Logger.Info("main window loaded");
        }

        private void ExportAllCmd(object sender, RoutedEventArgs e)
        {
            List<DataBaseInfo> dataBaseInfos = ConfigImport.GetCurrentDataBaseInfo();
            if (dataBaseInfos == null || dataBaseInfos.Count == 0)
                return;

            Window_Import import = new Window_Import(dataBaseInfos, export: true);
            if ((bool)import.ShowDialog(this)) {
                if (import.CurrentBaseInfos == null)
                    return;

                string json = ConfigImport.ExportDataBaseInfo(import.CurrentBaseInfos.ToList());
                if (string.IsNullOrEmpty(json)) {
                    MessageCard.Error(LangManager.GetValueByKey("ExportFailed"));
                    return;
                }

                string fileName = FileHelper.SaveFile(this, filter: ConstValues.FILTER_JSON);

                FileHelper.TryWriteToFile(fileName, json, Encoding.UTF8);
                MessageNotify.Success(LangManager.GetValueByKey("ExportSuccess"));
                FileHelper.TryOpenSelectPath(fileName);
                Logger.Info($"export config to {fileName}");


            }
        }

        private void ImportAllCmd(object sender, RoutedEventArgs e)
        {
            string filePath = FileHelper.SelectFile(this, filter: ConstValues.FILTER_JSON);
            if (!File.Exists(filePath))
                return;

            string content = FileHelper.TryReadFile(filePath);
            if (string.IsNullOrEmpty(content))
                return;


            List<DataBaseInfo> dataBaseInfos = ConfigImport.ParseInfo(content);

            if (dataBaseInfos == null || dataBaseInfos.Count == 0) {
                MessageCard.Error(LangManager.GetValueByKey("ParseFailed"));
                return;
            }
            Window_Import window_Import = new Window_Import(dataBaseInfos, export: false);
            if ((bool)window_Import.ShowDialog(this))
                if (ConfigImport.ImportDataBaseInfo(dataBaseInfos, content)) {
                    RefreshSendCommands();
                    MessageNotify.Success(LangManager.GetValueByKey("ImportAllSuccess"));
                    MessageCard.Info(LangManager.GetValueByKey("ImportHighLightHint"));
                }
        }

        private void ShowVarMonitor(object sender, RoutedEventArgs e)
        {
            window_Monitor?.Close();
            window_Monitor = new Window_Monitor();
            window_Monitor.Show();
            window_Monitor.Focus();
            window_Monitor.BringIntoView();
        }

        private void StartTelnetServer(object sender, RoutedEventArgs e)
        {
            if (Window_TelnetServer == null) {
                Window_TelnetServer = new Window_TelnetServer();
                Window_TelnetServer.Show();
            } else {
                if (Window_TelnetServer.IsClosed) {
                    Window_TelnetServer = new Window_TelnetServer();
                    Window_TelnetServer.Show();
                }
                Window_TelnetServer.BringIntoView();
                Window_TelnetServer.Focus();
            }
        }

        private void onPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.XButton1 == MouseButtonState.Pressed) {
                SetPortTabSelected(true);
            } else if (e.XButton2 == MouseButtonState.Pressed) {
                SetPortTabSelected(false);
            }
        }

        private void ShowHexCheckSettings(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement ele && ele.Parent is Grid grid &&
                grid.Children.OfType<Popup>().FirstOrDefault() is Popup popup) {
                popup.IsOpen = true;
            }
        }

        private ComConnector GetCurrentPort(object sender)
        {
            if (sender is FrameworkElement ele &&
                ele.Tag != null && ele.Tag.ToString() is string portName &&
                vieModel.PortTabItems.FirstOrDefault(arg => arg.Name.Equals(portName)) is ComConnector portTabItem) {
                return portTabItem;
            }
            return null;
        }



        private bool IsAllClose()
        {
            bool close = false;
            foreach (var item in vieModel.PortTabItems) {
                if (item.Connected) {
                    close = true;
                    break;
                }
            }
            return close;
        }

        private void ContextMenu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (sender is FrameworkElement ele && ele.ContextMenu is ContextMenu contextMenu &&
                contextMenu.Items.OfType<MenuItem>().FirstOrDefault(arg => arg.Name.Equals("_CloseOpenAllMenuItem")) is MenuItem menuItem) {
                if (IsAllClose()) {
                    menuItem.Header = LangManager.GetValueByKey("DisConnectAll");
                } else {
                    menuItem.Header = LangManager.GetValueByKey("ConnectAll");
                }
            }
        }
    }
}
