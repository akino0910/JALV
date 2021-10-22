using System.Windows;

namespace LogViewer.Common.Interfaces
{
    public interface IWinSimple
    {
        bool? DialogResult { get; set; }

        Window Owner { get; set; }

        void Close();
    }
}