using System.Windows.Input;

namespace WinLauncher
{
    /// <summary>
    /// 通用命令实现，用于在 MVVM 模式中绑定命令
    /// 无参数版本
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary>
        /// 当命令的可执行状态改变时触发
        /// 使用 CommandManager 自动重新查询建议
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>
        /// 判断命令是否可以执行
        /// </summary>
        public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;

        /// <summary>
        /// 执行命令
        /// </summary>
        public void Execute(object parameter) => _execute();
    }

    /// <summary>
    /// 泛型命令实现，支持带参数的命令
    /// </summary>
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool> _canExecute;

        public RelayCommand(Action<T> execute, Func<T, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>
        /// 判断命令是否可以执行，处理参数类型转换
        /// </summary>
        public bool CanExecute(object parameter)
        {
            if (parameter is T typedParameter)
            {
                return _canExecute?.Invoke(typedParameter) ?? true;
            }

            // 如果参数为 null，检查是否可以执行（对于引用类型）
            if (default(T) == null && parameter == null)
            {
                return _canExecute?.Invoke(default(T)) ?? true;
            }

            return false;
        }

        /// <summary>
        /// 执行命令，处理参数类型转换
        /// </summary>
        public void Execute(object parameter)
        {
            if (parameter is T typedParameter)
            {
                _execute(typedParameter);
            }
            else if (default(T) == null && parameter == null)
            {
                _execute(default(T));
            }
        }
    }
}