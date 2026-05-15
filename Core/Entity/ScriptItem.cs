using System;
using System.ComponentModel;
using System.IO;

namespace SuperCom.Entity
{
    /// <summary>
    /// 脚本项数据模型
    /// </summary>
    public class ScriptItem : INotifyPropertyChanged
    {
        private string _scriptPath;
        private string _content;
        private bool _isExecuting;
        private ScriptStatus _status = ScriptStatus.Waiting;
        private bool _isSelected;

        public string ScriptPath
        {
            get => _scriptPath;
            set
            {
                _scriptPath = value;
                OnPropertyChanged(nameof(ScriptPath));
                OnPropertyChanged(nameof(FileName));
                OnPropertyChanged(nameof(DisplayName));
            }
        }

        public string Content
        {
            get => _content;
            set
            {
                _content = value;
                OnPropertyChanged(nameof(Content));
            }
        }

        public bool IsExecuting
        {
            get => _isExecuting;
            set
            {
                _isExecuting = value;
                OnPropertyChanged(nameof(IsExecuting));
                OnPropertyChanged(nameof(CanExecute));
                OnPropertyChanged(nameof(CanStop));
            }
        }

        public ScriptStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(StatusText));
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }

        // 辅助属性
        public string FileName => Path.GetFileName(ScriptPath);
        public string DisplayName => FileName ?? "(未命名)";
        public string Extension => Path.GetExtension(ScriptPath)?.ToLowerInvariant();
        
        public bool IsBatchScript => Extension == ".bat" || Extension == ".cmd";
        public bool IsPowerShellScript => Extension == ".ps1";

        public bool CanExecute => !IsExecuting || Status == ScriptStatus.Completed || Status == ScriptStatus.Running || Status == ScriptStatus.Stopped || Status == ScriptStatus.Error;
        public bool CanStop => IsExecuting;

        public string StatusText => Status switch
        {
            ScriptStatus.Waiting => "等待中",
            ScriptStatus.Executing => "执行中...",
            ScriptStatus.Running => "循环运行中",
            ScriptStatus.Completed => "已完成",
            ScriptStatus.Stopped => "已停止",
            ScriptStatus.Error => "执行出错",
            _ => "未知"
        };

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 加载脚本内容
        /// </summary>
        public bool LoadContent()
        {
            try
            {
                if (File.Exists(ScriptPath))
                {
                    Content = File.ReadAllText(ScriptPath, GetEncoding());
                    return true;
                }
            }
            catch { }
            return false;
        }

        /// <summary>
        /// 根据扩展名推断编码
        /// </summary>
        private System.Text.Encoding GetEncoding()
        {
            // 默认使用系统编码（GBK）
            // PowerShell 脚本通常用 UTF-8
            if (IsPowerShellScript)
                return System.Text.Encoding.UTF8;
            
            return System.Text.Encoding.Default;
        }

        public void RefreshStatus()
        {
            OnPropertyChanged(nameof(CanExecute));
            OnPropertyChanged(nameof(CanStop));
            OnPropertyChanged(nameof(StatusText));
        }
    }

    /// <summary>
    /// 脚本执行状态
    /// </summary>
    public enum ScriptStatus
    {
        Waiting,    // 等待执行
        Executing,  // 执行中
        Running,    // 循环运行中
        Completed,  // 已完成
        Stopped,    // 已停止
        Error       // 执行出错
    }
}
