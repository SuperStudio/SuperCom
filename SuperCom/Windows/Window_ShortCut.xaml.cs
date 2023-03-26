using SuperCom.Config;
using SuperCom.Entity;
using SuperControls.Style;
using SuperUtils.Common;
using SuperUtils.Framework.ORM.Mapper;
using SuperUtils.WPF.VisualTools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SuperCom
{
    /// <summary>
    /// Interaction logic for Window_ShortCut.xaml
    /// </summary>
    public partial class Window_ShortCut : BaseWindow, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged([CallerMemberName] string name = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        SqliteMapper<ShortCutBinding> mapper = new SqliteMapper<ShortCutBinding>(ConfigManager.SQLITE_DATA_PATH);

        private MainWindow mainWindow { get; set; }

        public Window_ShortCut()
        {
            InitializeComponent();
            this.DataContext = this;
            Init();

        }

        public void Init()
        {
            dataGrid.ItemsSource = null;
            ShortCutBindings = new ObservableCollection<ShortCutBinding>();
            List<ShortCutBinding> shortCutBindings = mapper.SelectList();
            foreach (var item in shortCutBindings)
            {
                item.RefreshKeyList();
                ShortCutBindings.Add(item);

            }
            ShortCutBindingList = ShortCutBindings.ToList();
            dataGrid.ItemsSource = ShortCutBindings;

            Window window = SuperUtils.WPF.VisualTools.WindowHelper.GetWindowByName("MainWindow", App.Current.Windows);
            if (window != null && window is MainWindow w)
            {
                mainWindow = w;
            }

        }

        private List<ShortCutBinding> ShortCutBindingList;

        private ObservableCollection<ShortCutBinding> _ShortCutBindings;
        public ObservableCollection<ShortCutBinding> ShortCutBindings
        {
            get { return _ShortCutBindings; }
            set { _ShortCutBindings = value; RaisePropertyChanged(); }
        }

        private void BaseWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }

        private void BaseWindow_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void BaseWindow_ContentRendered(object sender, EventArgs e)
        {

        }

        private void SearchBox_Search(object sender, RoutedEventArgs e)
        {

        }

        public void DoSearch(string text)
        {
            ShortCutBindings = new ObservableCollection<ShortCutBinding>();
            if (string.IsNullOrEmpty(text))
            {
                foreach (var item in ShortCutBindingList)
                {
                    ShortCutBindings.Add(item);
                }
                dataGrid.ItemsSource = null;
                dataGrid.ItemsSource = ShortCutBindings;
                return;
            }

            text = text.ToLower();
            List<string> searchList = text.Split(' ').ToList();
            bool searchKey = false;
            if (text.IndexOf("+") > 0)
            {
                searchKey = true;
                searchList = text.Split('+').ToList();
            }

            else
                searchList.Add(text);

            List<ShortCutBinding> found = new List<ShortCutBinding>();

            searchList.RemoveAll(arg => string.IsNullOrEmpty(arg));

            if (searchKey)
            {
                // 全量匹配
                foreach (var item in ShortCutBindingList)
                {
                    List<string> keyList = item.KeyStringList.Select(arg => arg.ToLower()).ToList();
                    bool allInKeyList = !searchList.Except(keyList).Any();
                    if (allInKeyList)
                        found.Add(item);
                }
            }
            else
            {
                // 搜索文字
                foreach (ShortCutBinding item in ShortCutBindingList)
                {
                    foreach (var search in searchList)
                    {
                        if (item.KeyName.ToLower().IndexOf(search) >= 0 || item.KeyStringList.Any(arg => arg.ToLower().IndexOf(search) >= 0))
                        {
                            found.Add(item);
                            break;
                        }
                    }
                }
            }
            foreach (ShortCutBinding binding in found)
            {
                ShortCutBindings.Add(binding);
            }
            dataGrid.ItemsSource = null;
            dataGrid.ItemsSource = ShortCutBindings;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string text = searchBox.Text;
            DoSearch(text);
        }


        private ShortCutBinding CurrentShortCutBinding;

        private void Row_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            // 按键输入框
            keyInputTextBox.Text = "";
            sameTextBlock.Visibility = Visibility.Collapsed;
            warningTextBlock.Visibility = Visibility.Collapsed;
            DataGridRow row = (DataGridRow)sender;
            if (row != null)
            {
                int idx = row.GetIndex();
                if (idx >= 0 && idx < dataGrid.Items.Count)
                {
                    ShortCutBinding shortcut = dataGrid.Items[idx] as ShortCutBinding;
                    List<int> keyCodeList = shortcut.KeyCodeList;
                    // 提取 funcKey
                    List<Key> allKey = new List<Key>();
                    foreach (int keyCode in keyCodeList)
                    {
                        Key key = (Key)keyCode;
                        if (KeyBoardHelper.IsFuncKey(key))
                            allKey.Insert(0, key);
                        else
                            allKey.Add(key);
                    }

                    if (allKey.Count > 0)
                    {
                        List<string> list = allKey.Select(arg => KeyBoardHelper.KeyToString(arg).RemoveKeyDiff()).ToList();
                        keyInputTextBox.Text = string.Join("+", list).RemoveKeyDiff();
                        //Keyboard.Focus(keyInputTextBox);

                    }

                    CurrentShortCutBinding = shortcut;
                }
            }
            e.Handled = true;
            inputKeyPopup.IsOpen = true;
            inputKeyPopup.Focus();
            keyInputTextBox.Focus();
        }



        public static List<Key> funcKeys = new List<Key>();     // 功能键 [1,3] 个
        public static List<Key> normalKeys = new List<Key>();   // 基础键 [1,3] 个
        public static List<Key> fastKeys = new List<Key>();
        private void TextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            Key currentKey = e.Key == Key.System ? e.SystemKey : e.Key;

            if (currentKey == Key.Escape)
            {
                inputKeyPopup.IsOpen = false;
                return;
            }
            else if (currentKey == Key.Back)
            {
                keyInputTextBox.Text = "";
                return;
            }
            else if (currentKey == Key.Enter)
            {
                // 保存该快捷方式
                SaveKey();
                inputKeyPopup.IsOpen = false;
                return;
            }


            if (KeyBoardHelper.IsFuncKey(currentKey))
            {
                if (!funcKeys.Contains(currentKey))
                    funcKeys.Add(currentKey);
            }
            else if (KeyBoardHelper.IsSupportFastKey(currentKey))
            {
                if (!normalKeys.Contains(currentKey))
                    normalKeys.Add(currentKey);
            }
            else
            {
                warningTextBlock.Visibility = Visibility.Visible;
                return;
            }

            List<string> keys = funcKeys.Select(arg => KeyBoardHelper.KeyToString(arg).RemoveKeyDiff()).ToList();
            List<string> normals = normalKeys.Select(arg => KeyBoardHelper.KeyToString(arg).RemoveKeyDiff()).ToList();
            keys.AddRange(normals);

            List<Key> allkey = new List<Key>();
            allkey.AddRange(funcKeys);
            allkey.AddRange(normalKeys);

            keyInputTextBox.Text = string.Join("+", keys).RemoveKeyDiff();
            hiddenTextBlock.Text = string.Join(",", allkey.Select(arg => (int)arg));
            ShortCutBinding shortCutBinding = ShortCutBindingList.Where(arg => arg.Keys.ToLower().Equals(hiddenTextBlock.Text.ToLower())).FirstOrDefault();
            if (shortCutBinding != null)
            {
                sameTextBlock.Text = $"存在冲突的快捷键：{shortCutBinding.KeyName}";
                sameTextBlock.Visibility = Visibility.Visible;
            }
            else
            {
                sameTextBlock.Visibility = Visibility.Collapsed;
            }

            bool containsFunKey =
                funcKeys.Contains(Key.LeftAlt) ||
                funcKeys.Contains(Key.LeftCtrl) ||
                funcKeys.Contains(Key.LeftShift) ||
                funcKeys.Contains(Key.RightAlt) ||
                funcKeys.Contains(Key.RightCtrl) ||
                funcKeys.Contains(Key.RightShift) ||
                KeyBoardHelper.IsFKey(currentKey);

            if (!containsFunKey || currentKey == Key.None)
            {
                warningTextBlock.Visibility = Visibility.Visible;
            }
            else
            {
                warningTextBlock.Visibility = Visibility.Hidden;
            }
            e.Handled = true;
        }


        private void SaveKey()
        {
            if (CurrentShortCutBinding != null && warningTextBlock.Visibility == Visibility.Hidden)
            {
                CurrentShortCutBinding.Keys = hiddenTextBlock.Text;
                CurrentShortCutBinding.RefreshKeyList();
                mapper.Update(CurrentShortCutBinding);
                Init();
                SearchBox_TextChanged(null, null);
                // 通知到其他应用
                mainWindow.OnShortCutChanged();
            }

            funcKeys.Clear();
            funcKeys.Clear();
        }

        private void TextBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            // 清除
            Key currentKey = e.Key == Key.System ? e.SystemKey : e.Key;

            if (KeyBoardHelper.IsFuncKey(currentKey))
            {
                if (funcKeys.Contains(currentKey))
                    funcKeys.Remove(currentKey);
                if (!fastKeys.Contains(currentKey))
                    fastKeys.Insert(0, currentKey);
            }
            else if (KeyBoardHelper.IsSupportFastKey(currentKey))
            {
                if (normalKeys.Contains(currentKey))
                    normalKeys.Remove(currentKey);
                if (!fastKeys.Contains(currentKey))
                    fastKeys.Add(currentKey);
            }
        }

        private void inputKeyPopup_Closed(object sender, EventArgs e)
        {
            funcKeys.Clear();
            normalKeys.Clear();
            fastKeys.Clear();
        }
    }
}
