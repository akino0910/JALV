using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using LogViewer.Core.Domain;

namespace LogViewer.Common
{
    public class FilteredGridManagerBase
        : DisposableObject
    {
        public FilteredGridManagerBase(DataGrid dg, Panel txtSearchPanel, KeyEventHandler keyUpEvent)
        {
            Dg = dg;
            TxtSearchPanel = txtSearchPanel;
            KeyUpEvent = keyUpEvent;
            FilterPropertyList = new List<string>();
            TxtCache = new Hashtable();
            IsFilteringEnabled = true;
        }

        protected override void OnDispose()
        {
            ClearCache();
            FilterPropertyList?.Clear();
            Dg?.Columns.Clear();
            if (Cvs != null)
            {
                if (Cvs.View != null)
                    Cvs.View.Filter = null;
                BindingOperations.ClearAllBindings(Cvs);
            }

            base.OnDispose();
        }

        #region Private Properties

        protected IList<string> FilterPropertyList;
        protected DataGrid Dg;
        protected Panel TxtSearchPanel;
        protected KeyEventHandler KeyUpEvent;
        protected CollectionViewSource Cvs;
        protected Hashtable TxtCache;

        #endregion

        #region Public Methods

        public virtual void AssignSource(Binding sourceBind)
        {
            if (Cvs == null)
                Cvs = new CollectionViewSource();
            else
                BindingOperations.ClearBinding(Cvs, CollectionViewSource.SourceProperty);

            BindingOperations.SetBinding(Cvs, CollectionViewSource.SourceProperty, sourceBind);
            BindingOperations.ClearBinding(Dg, ItemsControl.ItemsSourceProperty);
            var bind = new Binding { Source = Cvs, Mode = BindingMode.OneWay };
            Dg.SetBinding(ItemsControl.ItemsSourceProperty, bind);
        }

        public ICollectionView GetCollectionView()
        {
            if (Cvs != null)
            {
                //Assign filter method
                if (Cvs.View != null && Cvs.View.Filter == null)
                {
                    IsFilteringEnabled = false;
                    Cvs.View.Filter = ItemCheckFilter;
                    IsFilteringEnabled = true;
                }

                return Cvs.View;
            }

            return null;
        }

        public void ResetSearchTextBox()
        {
            if (FilterPropertyList != null && TxtSearchPanel != null)
            {
                //Clear all textbox text
                foreach (var prop in FilterPropertyList)
                {
                    var txt = TxtSearchPanel.FindName(GetTextBoxName(prop)) as TextBox;
                    if ((txt != null) & !string.IsNullOrEmpty(txt.Text))
                        txt.Text = string.Empty;
                }
            }
        }

        public void ClearCache()
        {
            TxtCache?.Clear();
        }

        public Func<object, bool> OnBeforeCheckFilter;

        public Func<object, bool, bool> OnAfterCheckFilter;

        public bool IsFilteringEnabled { get; set; }

        #endregion

        #region Private Methods

        protected string GetTextBoxName(string prop)
        {
            return $"txtFilter{prop}".Replace(".", "");
        }

        protected bool ItemCheckFilter(object item)
        {
            var res = true;

            if (!IsFilteringEnabled)
                return res;

            try
            {
                if (OnBeforeCheckFilter != null)
                    res = OnBeforeCheckFilter(item);

                if (!res)
                    return res;

                if (FilterPropertyList != null && TxtSearchPanel != null)
                {
                    //Check each filter property
                    foreach (var prop in FilterPropertyList)
                    {
                        TextBox txt = null;
                        if (TxtCache.ContainsKey(prop))
                            txt = TxtCache[prop] as TextBox;
                        else
                        {
                            txt = TxtSearchPanel.FindName(GetTextBoxName(prop)) as TextBox;
                            TxtCache[prop] = txt;
                        }

                        res = false;
                        if (txt == null)
                            res = true;
                        else
                        {
                            if (string.IsNullOrEmpty(txt.Text))
                                res = true;
                            else
                            {
                                try
                                {
                                    //Get property value
                                    var val = GetItemValue(item, prop);
                                    if (val != null)
                                    {
                                        var valToCompare = string.Empty;
                                        if (val is DateTime)
                                            valToCompare = ((DateTime)val).ToString(GlobalHelper.DisplayDateTimeFormat,
                                                System.Globalization.CultureInfo.GetCultureInfo(Properties.Resources
                                                    .CultureName));
                                        else
                                            valToCompare = val.ToString();

                                        if (valToCompare.IndexOf(txt.Text, StringComparison.OrdinalIgnoreCase) >= 0)
                                            res = true;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine(ex.Message);
                                    res = true;
                                }
                            }
                        }

                        if (!res)
                            return res;
                    }
                }

                res = true;
            }
            finally
            {
                if (OnAfterCheckFilter != null)
                    res = OnAfterCheckFilter(item, res);
            }

            return res;
        }

        protected object GetItemValue(object item, string prop)
        {
            object val = null;
            try
            {
                val = item.GetType().GetProperty(prop).GetValue(item, null);
            }
            catch
            {
                val = null;
            }

            return val;
        }

        #endregion
    }
}