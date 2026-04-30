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

        // Enter键拦截：PreviewKeyDown 是隧道事件，在 TextBox 默认处理之前触发
        private void TxtOutput_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) {
                e.Handled = true;
                Dispatcher.InvokeAsync(ExecuteCommandFromOutput);
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

        // Enter 拦截后执行：从 TxtOutput 提取提示符后的纯命令文本，发送给 cmd.exe
        private async void ExecuteCommandFromOutput()
        {
            string currentText;
            int promptIdx;
            lock (_outputLock) {
                currentText = _outputBuilder.ToString();
                promptIdx = _promptEndIndex;
            }

            // ★ 安全检查：如果 promptIdx 无效，从最后一行提取命令
            if (promptIdx <= 0 || promptIdx >= currentText.Length) {
                int lastNl = currentText.LastIndexOf('\n');
                if (lastNl >= 0) {
                    string lastLine = currentText.Substring(lastNl + 1).TrimEnd('\r', '\n');
                    if (lastLine.Contains(">") || lastLine.Contains("#") || lastLine.Contains("$")) {
                        AppendOutput("\r\n");
                        TxtOutput.Focus();
                        return;
                    }
                    currentText = lastLine;
                    promptIdx = 0;
                } else {
                    promptIdx = 0;
                }
            }

            string userInput = currentText.Substring(promptIdx).TrimEnd('\r', '\n');

            if (!_isRunning || _cmdProcess == null || _cmdProcess.HasExited) {
                MessageCard.Warning("请先启动 CMD 进程");
                TxtOutput.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(userInput)) {
                AppendOutput("\r\n");
                TxtOutput.Focus();
                return;
            }

            // 截断 _outputBuilder 到提示符位置
            lock (_outputLock) {
                _outputBuilder.Clear();
                _outputBuilder.Append(currentText.Substring(0, Math.Min(promptIdx, currentText.Length)));
            }

            TxtOutput.Text = currentText.Substring(0, Math.Min(promptIdx, currentText.Length));

            // 发送命令
            try {
                await _cmdProcess.StandardInput.WriteLineAsync(userInput);
                await _cmdProcess.StandardInput.FlushAsync();
            } catch (Exception ex) {
                AppendOutput($"\r\n[发送失败] {ex.Message}\r\n");
            }

            TxtOutput.Focus();
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
            Dispatcher.Invoke(() => {
                TxtOutput.Text = _outputBuilder.ToString();

                // ★ 修复：使用 non-greedy regex，Group1 只捕获到 > 为止
                int textLen = TxtOutput.Text.Length;

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
