using System;
using System.Diagnostics;
using JALV.Common.Interfaces;

namespace JALV.Common
{
    public class CommandRelay
        : ICommandAncestor
    {
        protected readonly Func<object, object> ExecuteFunc;
        protected readonly Predicate<object> CanExecutePredicate;

        public CommandRelay(Func<object, object> executeFunc, Predicate<object> canExecutePredicate)
        {
            ExecuteFunc = executeFunc;
            CanExecutePredicate = canExecutePredicate;
        }

        [DebuggerStepThrough]
        public bool CanExecute(object parameter)
        {
            return CanExecutePredicate?.Invoke(parameter) ?? true;
        }

        public event EventHandler CanExecuteChanged;

        [DebuggerStepThrough]
        public void Execute(object parameter)
        {
            ExecuteFunc(parameter);
        }

        public void OnCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}