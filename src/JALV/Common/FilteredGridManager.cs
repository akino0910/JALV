using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using JALV.Common.Converters;
using JALV.Core.Domain;
using JALV.Properties;

namespace JALV.Common
{
    public class FilteredGridManager
        : FilteredGridManagerBase
    {
        public FilteredGridManager(DataGrid dg, Panel txtSearchPanel, KeyEventHandler keyUpEvent)
            : base(dg, txtSearchPanel, keyUpEvent)
        {
            _centerCellStyle = Application.Current.FindResource("CenterDataGridCellStyle") as Style;
            _adjConv = new AdjustValueConverter();
        }

        #region Private Properties

        private readonly Style _centerCellStyle;
        private readonly AdjustValueConverter _adjConv;

        #endregion

        #region Public Methods

        public void BuildDataGrid(IList<ColumnItem> columns)
        {
            if (Dg == null)
                return;

            if (FilterPropertyList == null)
                FilterPropertyList = new List<string>();
            else
                FilterPropertyList.Clear();

            if (columns != null)
            {
                foreach (var item in columns)
                {
                    var col = new DataGridTextColumn();
                    col.Header = item.Header;
                    if (item.Alignment == CellAlignment.Center && _centerCellStyle != null)
                        col.CellStyle = _centerCellStyle;
                    if (item.MinWidth != null)
                        col.MinWidth = item.MinWidth.Value;
                    if (item.Width != null)
                        col.Width = item.Width.Value;

                    var bind = new Binding(item.Field) { Mode = BindingMode.OneWay };
                    bind.ConverterCulture = CultureInfo.GetCultureInfo(Resources.CultureName);
                    if (!string.IsNullOrWhiteSpace(item.StringFormat))
                        bind.StringFormat = item.StringFormat;
                    col.Binding = bind;

                    //Add column to datagrid
                    Dg.Columns.Add(col);

                    if (TxtSearchPanel != null)
                    {
                        var widthBind = new Binding
                        {
                            Path = new PropertyPath("ActualWidth"),
                            Source = col,
                            Mode = BindingMode.OneWay,
                            Converter = _adjConv,
                            ConverterParameter = "-2"
                        };

                        var txt = new TextBox();
                        var txtStyle = Application.Current.FindResource("RoundWatermarkTextBox") as Style;
                        if (txtStyle != null)
                            txt.Style = txtStyle;
                        txt.Name = GetTextBoxName(item.Field);
                        txt.ToolTip = string.Format(Resources.FilteredGridManager_BuildDataGrid_FilterTextBox_Tooltip,
                            item.Header);
                        txt.Tag = txt.ToolTip.ToString().ToLower();
                        txt.Text = string.Empty;
                        txt.AcceptsReturn = false;
                        txt.SetBinding(FrameworkElement.WidthProperty, widthBind);
                        FilterPropertyList.Add(item.Field);
                        if (KeyUpEvent != null)
                            txt.KeyUp += KeyUpEvent;

                        RegisterControl(TxtSearchPanel, txt.Name, txt);
                        TxtSearchPanel.Children.Add(txt);
                    }
                }
            }

            Dg.ColumnReordered += OnColumnReordered;
        }

        #endregion

        #region Private methods

        private void OnColumnReordered(object sender, DataGridColumnEventArgs dataGridColumnEventArgs)
        {
            if (dataGridColumnEventArgs.Column == null || !(dataGridColumnEventArgs.Column is DataGridBoundColumn))
                return;

            var colBind = ((DataGridBoundColumn)dataGridColumnEventArgs.Column).Binding as Binding;
            if (colBind == null || colBind.Path == null)
                return;

            var field = colBind.Path.Path;
            if (string.IsNullOrWhiteSpace(field))
                return;

            var displayOrder = dataGridColumnEventArgs.Column.DisplayIndex;
            var textBoxName = GetTextBoxName(field);

            var textBox = (from tb in TxtSearchPanel.Children.OfType<TextBox>()
                where tb.Name == textBoxName
                select tb).FirstOrDefault();

            if (textBox == null)
                return;

            TxtSearchPanel.Children.Remove(textBox);
            TxtSearchPanel.Children.Insert(displayOrder, textBox);
        }

        private void RegisterControl<T>(FrameworkElement element, string controlName, T control)
        {
            if ((T)element.FindName(controlName) != null)
            {
                element.UnregisterName(controlName);
            }

            element.RegisterName(controlName, control);
        }

        #endregion
    }
}