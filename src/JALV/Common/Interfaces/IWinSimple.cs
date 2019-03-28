using System;
using System.Windows;

namespace JALV.Common.Interfaces
{
    public interface IWinSimple
    {
        bool? DialogResult { get; set; }

        Window Owner { get; set; }

        void Close();
    }
}