using System.Windows.Input;

namespace JALV.Common.Interfaces
{
    public interface ICommandAncestor
        : ICommand
    {
        void OnCanExecuteChanged();
    }
}
