using SuperCom.Config;
using SuperCom.Config.WindowConfig;
using SuperCom.Core.Commands;
using SuperCom.Entity;
using SuperControls.Style;
using SuperControls.Style.Windows;
using SuperUtils.Common;
using SuperUtils.IO;
using SuperUtils.Time;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SuperCom.Windows
{
    public partial class Window_CMD : BaseWindow
    {
        #region 字段

        private System.Diagnostics.Process _cmdProcess;
        private CancellationTokenSource _outputCts;
        private CancellationTokenSource _batchCts;
        private CancellationTokenSource _scriptCts;  // 脚本执行取消令牌
        private bool _isRunning;
        private bool _isLoopExecute; // 循环执行标志
        private string _currentOutputPath;
        private readonly object _outputLock = new object();
        private StringBuilder _outputBuilder = new StringBuilder();
        private int _defaultDelay = 1000;

        // 只读提示符保护
        private int _promptEndIndex = 0;  // 当前可编辑区域的起点（提示符结束位置）
        private int _userInputStart = 0;   // 当前行用户输入区域的起点（Enter插入换行后的位置）
        private string _currentPrompt = "";  // 当前命令提示符文本

        // 脚本管理
        private ScriptItem _currentExecutingScript;  // 当前正在执行的脚本
        private bool _isExecutingScript;  // 是否正在执行脚本

        public ObservableCollection<Entity.CmdCommand> Commands { get; set; }
        public ObservableCollection<Entity.ScriptItem> Scripts { get; set; }

        #endregion

        private static readonly string LogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SuperCom", "logs", "window_cmd_init.log");

        public Window_CMD()
        {
            // 初始化前检查
            try {
                var dir = Path.GetDirectoryName(LogPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.AppendAllText(LogPath,
                    $"[{DateTime.Now:HH:mm:ss.fff}] Window_CMD 构造开始\n", Encoding.UTF8);
            } catch { }

            try {
                InitializeComponent();
                try { File.AppendAllText(LogPath, $"[{DateTime.Now:HH:mm:ss.fff}] InitializeComponent 成功\n", Encoding.UTF8); } catch { }
            } catch (Exception ex) {
                var msg = $"InitializeComponent 失败:\n{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}";
                try { File.AppendAllText(LogPath, $"[{DateTime.Now:HH:mm:ss.fff}] {msg}\n", Encoding.UTF8); } catch { }
                MessageBox.Show(msg, "初始化失败", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }

            try
            {
                Commands = new ObservableCollection<Entity.CmdCommand>();
                Scripts = new ObservableCollection<Entity.ScriptItem>();
                DataContext = this;
                DataGridCommands.ItemsSource = Commands;
                ListBoxScripts.ItemsSource = Scripts;
            }
            catch (Exception ex)
            {
                MessageBox.Show("集合初始化失败: " + ex.Message + "\n" + ex.StackTrace, "错误");
                throw;
            }

            try
            {
                // 加载设置
                _defaultDelay = ConfigManager.CmdSettings.DefaultDelay > 0
                    ? ConfigManager.CmdSettings.DefaultDelay
                    : 1000;
                TxtDefaultDelay.Text = _defaultDelay.ToString();

                // 加载指令列表
                LoadCommands();

                // 加载脚本列表
                LoadScripts();

                // 绑定快捷键
                BindHotkeys();

                UpdateStatus("就绪");
            }
            catch (Exception ex)
            {
                MessageBox.Show("初始化失败: " + ex.Message + "\n" + ex.StackTrace, "错误");
                throw;
            }
        }

        private void LoadCommands()
        {
            Commands.Clear();
            string json = ConfigManager.CmdSettings.CommandsJson;
            if (!string.IsNullOrEmpty(json)) {
                var list = JsonUtils.TryDeserializeObject<List<Entity.CmdCommand>>(json);
                if (list != null) {
                    foreach (var item in list.OrderBy(x => x.Order))
                        Commands.Add(item);
                }
            }
        }

        private void SaveCommands()
        {
            var list = Commands.ToList();
            for (int i = 0; i < list.Count; i++)
                list[i].Order = i;
            ConfigManager.CmdSettings.CommandsJson = JsonUtils.TrySerializeObject(list);
            ConfigManager.CmdSettings.Save();
        }

        private void SaveScripts()
        {
            // 仅保存 ScriptPath 列表（不保存 Content，因为文件内容太大）
            var paths = Scripts.Select(s => s.ScriptPath).ToList();
            ConfigManager.CmdSettings.ScriptsJson = JsonUtils.TrySerializeObject(paths);
            ConfigManager.CmdSettings.Save();
        }

        private void LoadScripts()
        {
            Scripts.Clear();
            string json = ConfigManager.CmdSettings.ScriptsJson;
            if (!string.IsNullOrEmpty(json))
            {
                var paths = JsonUtils.TryDeserializeObject<List<string>>(json);
                if (paths != null)
                {
                    foreach (var path in paths)
                    {
                        if (File.Exists(path) && !Scripts.Any(s => s.ScriptPath == path))
                        {
                            var script = new Entity.ScriptItem { ScriptPath = path };
                            script.LoadContent();
                            Scripts.Add(script);
                        }
                    }
                }
            }
        }

        private void UpdateStatus(string text)
        {
            TxtStatus.Text = text;
            TxtStatusBar.Text = text;
        }

        #region 进程管理

        private async void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if (_isRunning && _cmdProcess != null && !_cmdProcess.HasExited)
                return;

            try {
                ClearOutput();
                AppendOutput("=== CMD 进程启动 ===\r\n");
                AppendOutput($"启动时间: {DateHelper.Now()}\r\n");
                AppendOutput("========================\r\n\r\n");

                _cmdProcess = new System.Diagnostics.Process();
                _cmdProcess.StartInfo.FileName = "cmd.exe";
                _cmdProcess.StartInfo.Arguments = "/Q";
                _cmdProcess.StartInfo.UseShellExecute = false;
                _cmdProcess.StartInfo.RedirectStandardInput = true;
                _cmdProcess.StartInfo.RedirectStandardOutput = true;
                _cmdProcess.StartInfo.RedirectStandardError = true;
                _cmdProcess.StartInfo.CreateNoWindow = true;
                _cmdProcess.StartInfo.StandardOutputEncoding = Encoding.Default;
                _cmdProcess.StartInfo.StandardErrorEncoding = Encoding.Default;

                _cmdProcess.EnableRaisingEvents = true;
                _cmdProcess.Exited += CmdProcess_Exited;

                _cmdProcess.Start();

                // 设置 UTF-8 编码
                await SendCommandAsync("chcp 65001>nul");

                _isRunning = true;
                BtnStart.IsEnabled = false;
                BtnStop.IsEnabled = true;
                UpdateStatus("运行中");

                // 异步读取输出
                _outputCts = new CancellationTokenSource();
                _ = ReadOutputAsync(_cmdProcess.StandardOutput, _outputCts.Token);
                _ = ReadOutputAsync(_cmdProcess.StandardError, _outputCts.Token);

            } catch (Exception ex) {
                AppendOutput($"[启动失败] {ex.Message}\r\n");
                UpdateStatus("启动失败");
            }
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            StopProcess();
        }

        private void StopProcess()
        {
            _isRunning = false;
            _outputCts?.Cancel();
            _batchCts?.Cancel();

            try {
                if (_cmdProcess != null && !_cmdProcess.HasExited) {
                    try { _cmdProcess.Kill(); } catch { }
                }
            } catch { }

            try { _cmdProcess?.Dispose(); } catch { }
            _cmdProcess = null;

            BtnStart.IsEnabled = true;
            BtnStop.IsEnabled = false;
            UpdateStatus("已停止");
            AppendOutput("\r\n=== CMD 进程已停止 ===\r\n");
        }

        private void CmdProcess_Exited(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() => {
                _isRunning = false;
                BtnStart.IsEnabled = true;
                BtnStop.IsEnabled = false;
                UpdateStatus("进程已退出");
                AppendOutput("\r\n=== CMD 进程已退出 ===\r\n");
            });
        }

        private async Task ReadOutputAsync(StreamReader reader, CancellationToken ct)
        {
            char[] buffer = new char[4096];
            try {
                while (!ct.IsCancellationRequested) {
                    int len = await reader.ReadAsync(buffer, 0, buffer.Length);
                    if (len == 0)
                        break;
                    string text = new string(buffer, 0, len);
                    AppendOutput(text);
                }
            } catch (OperationCanceledException) {
                // 正常取消
            } catch (Exception ex) {
                AppendOutput($"\r\n[读取输出异常] {ex.Message}\r\n");
            }
        }

        private async Task SendCommandAsync(string command)
        {
            if (_cmdProcess == null || _cmdProcess.HasExited)
                return;
            try {
                await _cmdProcess.StandardInput.WriteLineAsync(command);
                await _cmdProcess.StandardInput.FlushAsync();
            } catch (Exception ex) {
                AppendOutput($"\r\n[发送失败] {ex.Message}\r\n");
            }
        }

        #endregion

        #region 指令列表管理

        private void TxtDefaultDelay_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(TxtDefaultDelay.Text, out int delay) && delay >= 0) {
                _defaultDelay = delay;
                ConfigManager.CmdSettings.DefaultDelay = delay;
                ConfigManager.CmdSettings.Save();
            }
        }

        private void TxtNewCommand_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                AddCommand();
        }

        private void BtnAddCommand_Click(object sender, RoutedEventArgs e)
        {
            AddCommand();
        }

        private void AddCommand()
        {
            string cmd = TxtNewCommand.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(cmd))
                return;

            var idList = Commands.Select(x => x.CommandID).ToList();
            var newCmd = new Entity.CmdCommand {
                CommandID = Entity.CmdCommand.GenerateID(idList),
                Command = cmd,
                IsHex = false,
                Delay = _defaultDelay,
                IsSelected = true,
                Order = Commands.Count
            };

            Commands.Add(newCmd);
            SaveCommands();
            TxtNewCommand.Text = "";
            UpdateStatus($"已添加: {cmd}");
        }

        private void ChkSelectAll_Click(object sender, RoutedEventArgs e)
        {
            bool check = ChkSelectAll.IsChecked == true;
            foreach (var cmd in Commands)
                cmd.IsSelected = check;
        }

        private void BtnDeleteSelected_Click(object sender, RoutedEventArgs e)
        {
            var selected = Commands.Where(x => x.IsSelected).ToList();
            if (selected.Count == 0)
                return;

            var result = new MsgBox($"确认删除选中的 {selected.Count} 条指令？").ShowDialog(this);
            if (!(bool)result)
                return;

            foreach (var item in selected)
                Commands.Remove(item);
            SaveCommands();
            UpdateStatus($"已删除 {selected.Count} 条");
        }

        private void ChkLoop_Click(object sender, RoutedEventArgs e)
        {
            _isLoopExecute = ChkLoop.IsChecked == true;
        }

        private async void BtnExecuteSelected_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteSelectedCommands();
        }

        private async Task ExecuteSelectedCommands()
        {
            var selectedCmds = Commands.Where(x => x.IsSelected).ToList();
            if (selectedCmds.Count == 0) {
                MessageCard.Warning("请先勾选要执行的指令");
                return;
            }

            if (!_isRunning || _cmdProcess == null || _cmdProcess.HasExited) {
                MessageCard.Warning("请先启动 CMD 进程");
                return;
            }

            _batchCts = new CancellationTokenSource();
            BtnStart.IsEnabled = false;
            BtnStop.IsEnabled = true;
            UpdateStatus($"正在批量执行 {selectedCmds.Count} 条指令...");

            try {
                int executed = 0;
                foreach (var cmd in selectedCmds) {
                    if (_batchCts.Token.IsCancellationRequested)
                        break;

                    AppendOutput($"\r\n[执行] {cmd.Command}\r\n");
                    await SendCommandAsync(cmd.Command);
                    executed++;
                    UpdateStatus($"执行中 ({executed}/{selectedCmds.Count})");

                    if (cmd.Delay > 0) {
                        try {
                            await Task.Delay(cmd.Delay, _batchCts.Token);
                        } catch (OperationCanceledException) {
                            break;
                        }
                    }
                }

                UpdateStatus($"批量执行完成 ({executed} 条)");

                while (_isLoopExecute && !_batchCts.Token.IsCancellationRequested) {
                    foreach (var cmd in selectedCmds) {
                        if (_batchCts.Token.IsCancellationRequested)
                            break;

                        AppendOutput($"\r\n[执行] {cmd.Command}\r\n");
                        await SendCommandAsync(cmd.Command);
                        UpdateStatus($"执行中 (循环) - {cmd.Command}");

                        if (cmd.Delay > 0) {
                            try {
                                await Task.Delay(cmd.Delay, _batchCts.Token);
                            } catch (OperationCanceledException) {
                                break;
                            }
                        }
                    }
                }
            } catch (OperationCanceledException) {
                UpdateStatus("批量执行已停止");
            } finally {
                BtnStart.IsEnabled = !_isRunning || _cmdProcess?.HasExited == true;
                BtnStop.IsEnabled = _isRunning;
            }
        }

        #endregion

        #region 输出区指令执行

        // Enter键拦截：发送命令，不阻止 TextBox 自然处理（插入换行）
        private void TxtOutput_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) {
                // 同步记录当前输入区域起点，再让 TextBox 自然处理 Enter
                int inputStart = _userInputStart;
                Dispatcher.Invoke(() => {
                    ExecuteCommandFromOutput(inputStart);
                    _userInputStart = TxtOutput.Text.Length; // Enter 换行已由 TextBox 处理，新行起点
                });
                return;
            }

            int caret = TxtOutput.CaretIndex;
            int selStart = TxtOutput.SelectionStart;

            // Backspace 阻止越界（只保护提示符本身）
            if (e.Key == Key.Back) {
                if (TxtOutput.SelectionLength > 0 && selStart < _promptEndIndex) {
                    TxtOutput.SelectionStart = _promptEndIndex;
                    TxtOutput.SelectionLength = Math.Max(0, selStart + TxtOutput.SelectionLength - _promptEndIndex);
                    e.Handled = true;
                    return;
                }
                if (TxtOutput.SelectionLength == 0 && caret <= _promptEndIndex) {
                    e.Handled = true;
                    return;
                }
                return;
            }

            // Delete 阻止越界
            if (e.Key == Key.Delete) {
                if (selStart < _promptEndIndex) {
                    e.Handled = true;
                    return;
                }
            }

            // Left 键阻止越界
            if (e.Key == Key.Left && caret <= _promptEndIndex) {
                e.Handled = true;
                return;
            }
        }

        // Enter 后执行：发送命令
        private async void ExecuteCommandFromOutput(int inputStart)
        {
            string currentText = TxtOutput.Text;

            // 从用户输入区域提取纯命令
            string userInput = currentText.Substring(inputStart).TrimEnd('\r', '\n');

            if (!_isRunning || _cmdProcess == null || _cmdProcess.HasExited) {
                MessageCard.Warning("请先启动 CMD 进程");
                return;
            }

            if (string.IsNullOrWhiteSpace(userInput)) {
                return; // 空命令，不发送，直接换行
            }

            try {
                await _cmdProcess.StandardInput.WriteLineAsync(userInput);
                await _cmdProcess.StandardInput.FlushAsync();
            } catch (Exception ex) {
                Dispatcher.Invoke(() => {
                    AppendOutput($"\r\n[发送失败] {ex.Message}\r\n");
                });
            }
        }

        // 鼠标选择时强制不允许选到只读区
        private void TxtOutput_SelectionChanged(object sender, RoutedEventArgs e)
        {
            Dispatcher.InvokeAsync(() => {
                if (TxtOutput.SelectionStart < _promptEndIndex) {
                    int overflow = _promptEndIndex - TxtOutput.SelectionStart;
                    if (overflow > 0) {
                        TxtOutput.SelectionStart = _promptEndIndex;
                        TxtOutput.SelectionLength = Math.Max(0, TxtOutput.SelectionLength - overflow);
                    }
                }
            }, System.Windows.Threading.DispatcherPriority.Loaded);
        }

        // 底部输入框：PreviewKeyDown 拦截 Enter
        private void TxtExecuteCommand_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) {
                e.Handled = true;
                ExecuteFromLegacyInput();
            }
        }

        private void TxtExecuteCommand_KeyDown(object sender, KeyEventArgs e)
        {
            // 空实现
        }

        private void BtnExecuteCommand_Click(object sender, RoutedEventArgs e)
        {
            ExecuteFromLegacyInput();
        }

        // 备用输入方式：从独立的 TxtExecuteCommand 读取
        private async void ExecuteFromLegacyInput()
        {
            string cmd = TxtExecuteCommand.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(cmd))
                return;

            if (!_isRunning || _cmdProcess == null || _cmdProcess.HasExited) {
                MessageCard.Warning("请先启动 CMD 进程");
                return;
            }

            AppendOutput($"\r\n> {cmd}\r\n");

            await SendCommandAsync(cmd);
            TxtExecuteCommand.Text = "";
            TxtExecuteCommand.Focus();
        }

        #endregion

        #region 输出管理

        private void AppendOutput(string text)
        {
            lock (_outputLock) {
                _outputBuilder.Append(text);
            }
            // ★ 修复：AppendOutput 更新 _promptEndIndex，同时同步 _userInputStart
            // 如果 TextBox 末尾紧跟在上一次 AppendOutput 的内容后（即用户没有在中间打字），
            // 则 _userInputStart 也应推进到新文本末尾
            Dispatcher.Invoke(() => {
                int oldLen = TxtOutput.Text.Length - text.Length;
                TxtOutput.Text = _outputBuilder.ToString();
                int textLen = TxtOutput.Text.Length;

                // ★ 关键修复：_userInputStart 必须跟随 AppendOutput 同步推进
                // 当输出追加后，用户的输入区域起点应推进到新文本末尾
                if (textLen > oldLen && _userInputStart <= oldLen) {
                    _userInputStart = textLen;
                }

                System.Text.RegularExpressions.Regex promptRx =
                    new System.Text.RegularExpressions.Regex(@"^(.+?[A-Za-z]:[\\/][^>]+>) *$",
                        System.Text.RegularExpressions.RegexOptions.Multiline);

                _promptEndIndex = 0;
                for (int i = textLen - 2; i >= 0; i--) {
                    char c = TxtOutput.Text[i];
                    if (c == '\n') {
                        int lineStart = i + 1;
                        string rest = TxtOutput.Text.Substring(lineStart, textLen - lineStart);
                        var m = promptRx.Match(rest);
                        if (m.Success) {
                            _promptEndIndex = lineStart + m.Groups[1].Length;
                            _currentPrompt = m.Groups[1].Value;
                        }
                        break;
                    }
                }

                // 单行文本：尝试整体匹配
                if (_promptEndIndex == 0 && textLen > 2) {
                    string currentText = TxtOutput.Text;
                    int lastGt = currentText.LastIndexOf('>');
                    if (lastGt > 1) {
                        string beforeGt = currentText.Substring(0, lastGt + 1);
                        if (System.Text.RegularExpressions.Regex.IsMatch(beforeGt, @"^[^\r\n]*[A-Za-z]:[\\/][^>]*>$")) {
                            _promptEndIndex = lastGt + 1;
                            _currentPrompt = beforeGt;
                        }
                    }
                    if (_promptEndIndex == 0) {
                        int hashPos = currentText.LastIndexOf('#');
                        int dollarPos = currentText.LastIndexOf('$');
                        int lastShell = Math.Max(hashPos, dollarPos);
                        if (lastShell > 0) {
                            _promptEndIndex = lastShell + 1;
                            _currentPrompt = currentText[lastShell].ToString();
                        }
                    }
                }

                // 如果 _userInputStart 超过了 TextBox 长度（输出追加后），重置到末尾
                if (_userInputStart > textLen) {
                    _userInputStart = textLen;
                }
                TxtOutput.ScrollToEnd();
            }, System.Windows.Threading.DispatcherPriority.Background);
        }

        private void TxtOutput_TextChanged(object sender, TextChangedEventArgs e)
        {
            ((TextBox)sender).ScrollToEnd();
        }

        private void ClearOutput()
        {
            Dispatcher.Invoke(() => {
                lock (_outputLock) {
                    _outputBuilder.Clear();
                }
                TxtOutput.Text = "";
                _promptEndIndex = 0;
                _userInputStart = 0;  // 重置用户输入区域起点
                _currentPrompt = "";
            });
        }

        private void BtnClearOutput_Click(object sender, RoutedEventArgs e)
        {
            ClearOutput();
        }

        private void BtnSaveOutput_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog {
                Title = "保存输出",
                Filter = "文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
                DefaultExt = ".txt",
                FileName = $"CMD_Output_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
            };

            if (dialog.ShowDialog() == true) {
                _currentOutputPath = dialog.FileName;
                SaveOutputToFile();
                UpdateStatus($"已保存到: {_currentOutputPath}");
            }
        }

        private void SaveOutputToFile()
        {
            if (string.IsNullOrEmpty(_currentOutputPath))
                return;
            try {
                string text;
                lock (_outputLock) {
                    text = _outputBuilder.ToString();
                }
                if (!string.IsNullOrEmpty(text))
                    File.AppendAllText(_currentOutputPath, text, Encoding.UTF8);
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"保存输出失败: {ex.Message}");
            }
        }

        #endregion

        #region 脚本管理

        private void BindHotkeys()
        {
            // Ctrl+I 导入脚本
            var importBinding = new KeyBinding(
                new RelayCommand(_ => BtnImportScript_Click(null, null)),
                Key.I,
                ModifierKeys.Control);
            InputBindings.Add(importBinding);
        }

        private void BtnImportScript_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "导入脚本",
                    Filter = "批处理脚本 (*.bat;*.cmd)|*.bat;*.cmd|PowerShell 脚本 (*.ps1)|*.ps1|所有文件 (*.*)|*.*",
                    Multiselect = true,
                    CheckFileExists = true
                };

                if (dialog.ShowDialog() != true)
                    return;

                int added = 0;
                foreach (string filePath in dialog.FileNames)
                {
                    // 检查是否已导入
                    if (Scripts.Any(s => s.ScriptPath == filePath))
                    {
                        AppendOutput($"\r\n[提示] 脚本已存在: {filePath}\r\n");
                        continue;
                    }

                    var script = new Entity.ScriptItem
                    {
                        ScriptPath = filePath,
                        Status = Entity.ScriptStatus.Waiting
                    };

                    if (script.LoadContent())
                    {
                        Scripts.Add(script);
                        added++;
                        AppendOutput($"\r\n[导入] {script.FileName}\r\n");
                    }
                    else
                    {
                        AppendOutput($"\r\n[错误] 无法读取: {filePath}\r\n");
                    }
                }

                if (added > 0)
                {
                    UpdateStatus($"已导入 {added} 个脚本");
                    // 延迟选择，让 ListBox 有时间完成渲染
                    Dispatcher.BeginInvoke(new Action(() => {
                        try { ListBoxScripts.SelectedIndex = 0; } catch { }
                    }), System.Windows.Threading.DispatcherPriority.Loaded);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("导入脚本异常:\n" + ex.GetType().Name + ": " + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ListBoxScripts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 选择改变时，预览区会自动通过绑定更新
        }

        private async void BtnExecuteScript_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Entity.ScriptItem script)
            {
                await ExecuteScriptAsync(script);
            }
        }

        private async Task ExecuteScriptAsync(Entity.ScriptItem script)
        {
            if (script == null) return;

            if (_isExecutingScript)
            {
                MessageCard.Warning("已有脚本正在执行，请先停止");
                return;
            }

            if (!_isRunning || _cmdProcess == null || _cmdProcess.HasExited)
            {
                MessageCard.Warning("请先启动 CMD 进程");
                return;
            }

            // 重置脚本状态为等待中（允许重复执行）
            script.Status = ScriptStatus.Waiting;
            script.IsExecuting = true;
            _isExecutingScript = true;
            _currentExecutingScript = script;
            
            // 创建新的取消令牌源
            _scriptCts?.Dispose();
            _scriptCts = new CancellationTokenSource();

            AppendOutput($"\r\n{new string('=', 40)}\r\n");
            AppendOutput($"[脚本开始] {script.FileName}\r\n");
            AppendOutput($"路径: {script.ScriptPath}\r\n");
            AppendOutput($"{new string('=', 40)}\r\n\r\n");

            try
            {
                if (script.IsPowerShellScript)
                {
                    // PowerShell 脚本：使用 powershell -File 执行
                    await SendCommandAsync($"powershell -ExecutionPolicy Bypass -File \"{script.ScriptPath}\"");
                }
                else
                {
                    // 批处理脚本：直接调用（使用 call 命令确保正确执行）
                    await SendCommandAsync($"call \"{script.ScriptPath}\"");
                }

                // 等待脚本执行完成
                await Task.Delay(500);  // 给脚本一点启动时间
                
                // 检测脚本执行完成：等待输出稳定
                int stableCount = 0;
                int lastLength = _outputBuilder.Length;
                const int maxWaitIterations = 60; // 最多等待30秒
                int iterations = 0;
                
                while (stableCount < 3 && !_scriptCts.Token.IsCancellationRequested && iterations < maxWaitIterations)
                {
                    await Task.Delay(500);
                    if (_outputBuilder.Length == lastLength)
                        stableCount++;
                    else
                        stableCount = 0;
                    lastLength = _outputBuilder.Length;
                    iterations++;
                }

                if (!_scriptCts.Token.IsCancellationRequested)
                {
                    script.Status = Entity.ScriptStatus.Completed;
                    AppendOutput($"\r\n\r\n{new string('=', 40)}\r\n");
                    AppendOutput($"[脚本完成] {script.FileName}\r\n");
                    AppendOutput($"{new string('=', 40)}\r\n\r\n");
                }
            }
            catch (OperationCanceledException)
            {
                script.Status = Entity.ScriptStatus.Stopped;
                AppendOutput($"\r\n\r\n{new string('=', 40)}\r\n");
                AppendOutput($"[脚本停止] {script.FileName}\r\n");
                AppendOutput($"{new string('=', 40)}\r\n\r\n");
            }
            catch (Exception ex)
            {
                script.Status = Entity.ScriptStatus.Error;
                AppendOutput($"\r\n[脚本错误] {ex.Message}\r\n");
            }
            finally
            {
                // 安全地重置状态
                _isExecutingScript = false;
                _currentExecutingScript = null;
                
                if (script != null)
                {
                    script.IsExecuting = false;
                    // 如果状态还是Waiting（未改变），说明执行异常，设为Error
                    if (script.Status == ScriptStatus.Waiting)
                    {
                        script.Status = ScriptStatus.Error;
                    }
                    script.RefreshStatus();
                }
                
                UpdateStatus("就绪");
            }
        }

        // 停止按钮已从XAML移除，脚本执行完成或取消后自动停止
        // 如需手动停止，可关闭CMD窗口或使用顶部"停止"按钮

        private void SendCtrlC()
        {
            if (_cmdProcess == null || _cmdProcess.HasExited)
                return;

            // 发送 Ctrl+C 信号（AttachConsole 在后台线程安全执行）
            try
            {
                bool success = NativeMethods.AttachConsole((uint)_cmdProcess.Id);
                if (success)
                {
                    NativeMethods.GenerateConsoleCtrlEvent(NativeMethods.CTRL_C_EVENT, 0);
                    NativeMethods.FreeConsole();
                }
                // AttachConsole 失败时什么都不做
                // Task.Run 中的 BeginInvoke 会显示提示
            }
            catch (Exception)
            {
                // 静默忽略，让 Task.Run 中的 BeginInvoke 显示提示
            }
        }

        private void BtnRemoveScript_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Entity.ScriptItem script)
            {
                if (script.IsExecuting)
                {
                    MessageCard.Warning("请先停止脚本执行");
                    return;
                }
                Scripts.Remove(script);
                UpdateStatus($"已移除: {script.FileName}");
            }
        }

        #endregion

        #region 窗口事件

        private void BaseWindow_Loaded(object sender, RoutedEventArgs e)
        {
            TxtNewCommand.Focus();
        }

        private void BaseWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            StopProcess();
            SaveCommands();
            SaveScripts();
            UpdateStatus("已关闭");
        }

        #endregion
    }

    #region Native Methods

    /// <summary>
    /// Windows API 调用，用于向控制台进程发送控制信号
    /// </summary>
    internal static class NativeMethods
    {
        public const int CTRL_C_EVENT = 0;
        public const int CTRL_BREAK_EVENT = 1;

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        public static extern bool AttachConsole(uint dwProcessId);

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        public static extern bool FreeConsole();

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        public static extern bool GenerateConsoleCtrlEvent(uint dwCtrlEvent, uint dwProcessGroupId);

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        public static extern bool SetConsoleCtrlHandler(IntPtr handlerRoutine, bool add);
    }

    #endregion
}
