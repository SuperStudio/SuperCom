using SuperCom.Config;
using SuperCom.Config.WindowConfig;
using SuperControls.Style;
using SuperControls.Style.Windows;
using SuperUtils.Common;
using SuperUtils.IO;
using SuperUtils.Time;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private bool _isRunning;
        private bool _isLoopExecute; // 循环执行标志
        private string _currentOutputPath;
        private readonly object _outputLock = new object();
        private StringBuilder _outputBuilder = new StringBuilder();
        private int _defaultDelay = 1000;

        // 只读提示符保护
        private int _promptEndIndex = 0;  // 当前可编辑区域的起点（提示符结束位置）
        private string _currentPrompt = "";  // 当前命令提示符文本
        private static readonly System.Text.RegularExpressions.Regex _promptRegex
            = new System.Text.RegularExpressions.Regex(@"^([A-Z]:\\.+>) (.+)",
                System.Text.RegularExpressions.RegexOptions.Multiline);

        public ObservableCollection<Entity.CmdCommand> Commands { get; set; }

        #endregion

        public Window_CMD()
        {
            InitializeComponent();
            Commands = new ObservableCollection<Entity.CmdCommand>();
            DataContext = this;
            DataGridCommands.ItemsSource = Commands;

            // 加载设置
            _defaultDelay = ConfigManager.CmdSettings.DefaultDelay > 0
                ? ConfigManager.CmdSettings.DefaultDelay
                : 1000;
            TxtDefaultDelay.Text = _defaultDelay.ToString();

            // 加载指令列表
            LoadCommands();

            UpdateStatus("就绪");
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
                // 非循环模式：先执行一轮
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

                // 循环模式：继续循环执行
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

        // Enter键拦截：必须用 PreviewKeyDown（隧道事件，在 TextBox 处理前触发）
        // e.Handled=true 可阻止 TextBox 插入换行符
        // 由于 PreviewKeyDown 在 TextBox 更新 Text 之前触发，
        // CaretIndex 无法准确反映实际内容位置，改用 Text.Length 在拦截瞬间记录
        private int _enterKeyLength = -1;  // Enter 按下时记录 Text.Length

        private void TxtOutput_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Enter：先记录当前长度，再阻止 TextBox 插入换行
            if (e.Key == Key.Enter) {
                _enterKeyLength = TxtOutput.Text.Length;
                e.Handled = true;  // 阻止 TextBox 插入换行（PreviewKeyDown 比 TextBox 更早触发）
                Dispatcher.InvokeAsync(ExecuteCommandFromOutput);
                return;
            }

            // 只读提示符区域保护
            int caret = TxtOutput.CaretIndex;
            int selStart = TxtOutput.SelectionStart;

            // Backspace 阻止越界
            if (e.Key == Key.Back) {
                if (caret <= _promptEndIndex) {
                    e.Handled = true;
                    return;
                }
                if (selStart < _promptEndIndex) {
                    TxtOutput.SelectionStart = _promptEndIndex;
                    TxtOutput.SelectionLength = 0;
                    e.Handled = true;
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

        // Enter 拦截后执行：提取提示符后的纯命令文本，发送给 cmd.exe
        // 关键：必须在发送前把 TxtOutput 截断到 Enter 按下时的长度（去掉 TextBox 插入的换行），
        // 这样 cmd.exe 的回显会追加在干净的历史末尾，不会出现重复的提示符行
        private async void ExecuteCommandFromOutput()
        {
            int len = _enterKeyLength;
            _enterKeyLength = -1;

            if (len < 0 || len > TxtOutput.Text.Length)
                return;

            // 提取纯命令文本
            string userInput = TxtOutput.Text.Substring(_promptEndIndex, len - _promptEndIndex).TrimEnd('\r', '\n');

            // 立即把 TxtOutput 截断到 Enter 按下时的干净状态
            TxtOutput.Text = TxtOutput.Text.Substring(0, len);
            lock (_outputLock) {
                _outputBuilder.Clear();
                _outputBuilder.Append(TxtOutput.Text);
            }

            if (!_isRunning || _cmdProcess == null || _cmdProcess.HasExited) {
                MessageCard.Warning("请先启动 CMD 进程");
                TxtOutput.Focus();
                return;
            }

            // ★ 空命令保护：不发送给 cmd.exe（空行会导致进程异常/退出）
            if (string.IsNullOrWhiteSpace(userInput)) {
                // 仅在 UI 中换行，不发送
                AppendOutput("\r\n");
                _promptEndIndex = TxtOutput.Text.Length;
                TxtOutput.CaretIndex = _promptEndIndex;
                TxtOutput.Focus();
                return;
            }

            // 发送命令前先输出一个换行，确保命令回显在新行开始
            AppendOutput("\r\n");

            // 估算新提示符位置
            int baseLen = TxtOutput.Text.Length;
            int estimatedPromptIdx = baseLen + userInput.Length + 8;
            _promptEndIndex = estimatedPromptIdx;
            _currentPrompt = "";
            TxtOutput.CaretIndex = Math.Min(estimatedPromptIdx, TxtOutput.Text.Length);

            try {
                await _cmdProcess.StandardInput.WriteLineAsync(userInput);
                await _cmdProcess.StandardInput.FlushAsync();
            } catch (Exception ex) {
                AppendOutput($"\r\n[发送失败] {ex.Message}\r\n");
            }

            TxtOutput.Focus();
        }

        // 每次释放按键时确保光标在可编辑区
        private void TxtOutput_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            Dispatcher.InvokeAsync(() => {
                if (TxtOutput.CaretIndex < _promptEndIndex) {
                    TxtOutput.CaretIndex = Math.Min(_promptEndIndex, TxtOutput.Text.Length);
                }
                if (TxtOutput.SelectionStart < _promptEndIndex) {
                    TxtOutput.SelectionStart = _promptEndIndex;
                    TxtOutput.SelectionLength = Math.Max(0, TxtOutput.SelectionLength
                        - (_promptEndIndex - TxtOutput.SelectionStart));
                }
            }, System.Windows.Threading.DispatcherPriority.Loaded);
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

        // 底部输入框：Enter 键执行指令
        private void TxtExecuteCommand_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) {
                e.Handled = true;
                ExecuteFromLegacyInput();
            }
        }

        // 底部输入框：PreviewKeyDown 阻止默认换行行为
        private void TxtExecuteCommand_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) {
                e.Handled = true;  // 阻止 TextBox 插入换行
            }
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

            // 发送前在输出区追加换行和命令标记，确保输出从新行开始
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
            Dispatcher.Invoke(() => {
                TxtOutput.Text = _outputBuilder.ToString();

                // 更新提示符边界：扫描文本末尾的提示符行
                int textLen = TxtOutput.Text.Length;

                // 匹配两类提示符：
                // 1. Windows: X:\path\to\dir>
                // 2. Shell: #  $  /  等（adb shell 进入 Android 环境后）
                System.Text.RegularExpressions.Regex promptRx =
                    new System.Text.RegularExpressions.Regex(@"^(.+[A-Za-z]:[\\/].+[#>$] |[#/$] )$");

                // 从输出末尾往前找最后一个换行行
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

                // 如果末尾没有换行（提示符和光标在同一行），检查整体格式
                if (_promptEndIndex == 0 && textLen > 2) {
                    string currentText = TxtOutput.Text;
                    // 尝试 Windows 提示符（最后 > 位置）
                    int lastGt = currentText.LastIndexOf('>');
                    if (lastGt > 1) {
                        string possible = currentText.Substring(lastGt - 1);
                        if (System.Text.RegularExpressions.Regex.IsMatch(possible, @"^[A-Za-z]:[\\/].+> *$")) {
                            _promptEndIndex = lastGt + 1;
                            _currentPrompt = possible.Substring(0, possible.LastIndexOf('>') + 1);
                        }
                    }
                    // 尝试 Shell 提示符（adb shell 进入 Android 后出现 # 或 $）
                    if (_promptEndIndex == 0) {
                        int hashPos = currentText.LastIndexOf('#');
                        int dollarPos = currentText.LastIndexOf('$');
                        int lastShell = Math.Max(hashPos, dollarPos);
                        if (lastShell > 0) {
                            bool atEnd = lastShell == currentText.Length - 1;
                            bool followedBySpace = !atEnd && currentText[lastShell + 1] == ' ';
                            if (atEnd || followedBySpace) {
                                // 提取提示符文本：#prompt# 或 $prompt$ 形式，取 # 或 $ 之前的内容
                                string promptText = currentText.Substring(0, lastShell + 1);
                                _promptEndIndex = lastShell + 1;
                                _currentPrompt = currentText[lastShell].ToString();
                            }
                        }
                    }
                }

                TxtOutput.ScrollToEnd();

                // 光标强制锚定到可编辑区域起点
                if (TxtOutput.CaretIndex < _promptEndIndex) {
                    TxtOutput.CaretIndex = Math.Min(_promptEndIndex, TxtOutput.Text.Length);
                }
            }, System.Windows.Threading.DispatcherPriority.Background);
        }

        private void TxtOutput_TextChanged(object sender, TextChangedEventArgs e)
        {
            // 自动滚动到底部
            ((TextBox)sender).ScrollToEnd();
        }

        // 已移除 GetScrollViewer 辅助方法，TextBox 内置 ScrollToEnd()

        private void ClearOutput()
        {
            Dispatcher.Invoke(() => {
                lock (_outputLock) {
                    _outputBuilder.Clear();
                }
                TxtOutput.Text = "";
                _promptEndIndex = 0;
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

        #region 窗口事件

        private void BaseWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 聚焦到添加指令输入框
            TxtNewCommand.Focus();
        }

        private void BaseWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            StopProcess();
            SaveCommands();
            UpdateStatus("已关闭");
        }

        #endregion
    }
}
