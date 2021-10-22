using System.Windows.Input;

namespace LogViewer.Common.Interfaces
{
    public interface ICommandAncestor
        : ICommand
    {
        void OnCanExecuteChanged();
    }
}