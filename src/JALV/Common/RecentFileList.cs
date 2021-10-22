using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using Microsoft.Win32;

namespace JALV.Common
{
    /// <summary>
    /// Refs: http://www.codeproject.com/Articles/23731/RecentFileList-a-WPF-MRU
    /// </summary>
    public class RecentFileList : Separator
    {
        public interface IPersist
        {
            List<string> RecentFiles(int max);
            void InsertFile(string filepath, int max);
            void RemoveFile(string filepath, int max);
        }

        public IPersist Persister { get; set; }

        public void UseRegistryPersister()
        {
            Persister = new RegistryPersister();
        }

        public void UseRegistryPersister(string key)
        {
            Persister = new RegistryPersister(key);
        }

        public void UseXmlPersister()
        {
            Persister = new XmlPersister();
        }

        public void UseXmlPersister(string filepath)
        {
            Persister = new XmlPersister(filepath);
        }

        public void UseXmlPersister(Stream stream)
        {
            Persister = new XmlPersister(stream);
        }

        public int MaxNumberOfFiles { get; set; }
        public int MaxPathLength { get; set; }
        public MenuItem FileMenu { get; private set; }

        /// <summary>
        /// Used in: String.Format( MenuItemFormat, index, filepath, displayPath );
        /// Default = "_{0}:  {2}"
        /// </summary>
        public string MenuItemFormatOneToNine { get; set; }

        /// <summary>
        /// Used in: String.Format( MenuItemFormat, index, filepath, displayPath );
        /// Default = "{0}:  {2}"
        /// </summary>
        public string MenuItemFormatTenPlus { get; set; }

        public delegate string GetMenuItemTextDelegate(int index, string filepath);

        public GetMenuItemTextDelegate GetMenuItemTextHandler { get; set; }

        public event EventHandler<MenuClickEventArgs> MenuClick;

        private Separator _separator;
        private List<RecentFile> _recentFiles;

        public RecentFileList()
        {
            Persister = new RegistryPersister();

            MaxNumberOfFiles = 10;
            MaxPathLength = 150;
            MenuItemFormatOneToNine = "_{0}:  {2}";
            MenuItemFormatTenPlus = "{0}:  {2}";

            Loaded += (s, e) => HookFileMenu();
        }

        private void HookFileMenu()
        {
            var parent = Parent as MenuItem;
            if (parent == null) throw new ApplicationException("Parent must be a MenuItem");

            if (FileMenu == parent) return;

            if (FileMenu != null) FileMenu.SubmenuOpened -= _FileMenu_SubmenuOpened;

            FileMenu = parent;
            FileMenu.SubmenuOpened += _FileMenu_SubmenuOpened;
        }

        public List<string> RecentFiles => Persister.RecentFiles(MaxNumberOfFiles);

        public void RemoveFile(string filepath)
        {
            Persister.RemoveFile(filepath, MaxNumberOfFiles);
        }

        public void InsertFile(string filepath)
        {
            Persister.InsertFile(filepath, MaxNumberOfFiles);
        }

        private void _FileMenu_SubmenuOpened(object sender, RoutedEventArgs e)
        {
            SetMenuItems();
        }

        private void SetMenuItems()
        {
            RemoveMenuItems();

            LoadRecentFiles();

            InsertMenuItems();
        }

        private void RemoveMenuItems()
        {
            if (_separator != null) FileMenu.Items.Remove(_separator);

            if (_recentFiles != null)
                foreach (var r in _recentFiles)
                    if (r.MenuItem != null)
                        FileMenu.Items.Remove(r.MenuItem);

            _separator = null;
            _recentFiles = null;
        }

        private void InsertMenuItems()
        {
            if (_recentFiles == null) return;
            if (_recentFiles.Count == 0) return;

            var iMenuItem = FileMenu.Items.IndexOf(this);
            foreach (var r in _recentFiles)
            {
                var header = GetMenuItemText(r.Number + 1, r.Filepath, r.DisplayPath);

                r.MenuItem = new MenuItem { Header = header };
                r.MenuItem.Click += MenuItem_Click;

                FileMenu.Items.Insert(++iMenuItem, r.MenuItem);
            }

            _separator = new Separator();
            FileMenu.Items.Insert(++iMenuItem, _separator);
        }

        private string GetMenuItemText(int index, string filepath, string displaypath)
        {
            var delegateGetMenuItemText = GetMenuItemTextHandler;
            if (delegateGetMenuItemText != null) return delegateGetMenuItemText(index, filepath);

            var format = index < 10 ? MenuItemFormatOneToNine : MenuItemFormatTenPlus;

            var shortPath = ShortenPathname(displaypath, MaxPathLength);

            return string.Format(format, index, filepath, shortPath);
        }

        // This method is taken from Joe Woodbury's article at: http://www.codeproject.com/KB/cs/mrutoolstripmenu.aspx

        /// <summary>
        /// Shortens a pathname for display purposes.
        /// </summary>
        /// <param labelName="pathname">The pathname to shorten.</param>
        /// <param labelName="maxLength">The maximum number of characters to be displayed.</param>
        /// <remarks>Shortens a pathname by either removing consecutive components of a path
        /// and/or by removing characters from the end of the filename and replacing
        /// then with three elipses (...)
        /// <para>In all cases, the root of the passed path will be preserved in it's entirety.</para>
        /// <para>If a UNC path is used or the pathname and maxLength are particularly short,
        /// the resulting path may be longer than maxLength.</para>
        /// <para>This method expects fully resolved pathnames to be passed to it.
        /// (Use Path.GetFullPath() to obtain this.)</para>
        /// </remarks>
        /// <returns></returns>
        public static string ShortenPathname(string pathname, int maxLength)
        {
            if (pathname.Length <= maxLength)
                return pathname;

            var root = Path.GetPathRoot(pathname);
            if (root.Length > 3)
                root += Path.DirectorySeparatorChar;

            var elements = pathname.Substring(root.Length)
                .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            var filenameIndex = elements.GetLength(0) - 1;

            if (elements.GetLength(0) == 1) // pathname is just a root and filename
            {
                if (elements[0].Length > 5) // long enough to shorten
                {
                    // if path is a UNC path, root may be rather long
                    if (root.Length + 6 >= maxLength)
                    {
                        return root + elements[0].Substring(0, 3) + "...";
                    }

                    return pathname.Substring(0, maxLength - 3) + "...";
                }
            }
            else if (root.Length + 4 + elements[filenameIndex].Length >
                     maxLength) // pathname is just a root and filename
            {
                root += "...\\";

                var len = elements[filenameIndex].Length;
                if (len < 6)
                    return root + elements[filenameIndex];

                if (root.Length + 6 >= maxLength)
                {
                    len = 3;
                }
                else
                {
                    len = maxLength - root.Length - 3;
                }

                return root + elements[filenameIndex].Substring(0, len) + "...";
            }
            else if (elements.GetLength(0) == 2)
            {
                return root + "...\\" + elements[1];
            }
            else
            {
                var len = 0;
                var begin = 0;

                for (var i = 0; i < filenameIndex; i++)
                {
                    if (elements[i].Length > len)
                    {
                        begin = i;
                        len = elements[i].Length;
                    }
                }

                var totalLength = pathname.Length - len + 3;
                var end = begin + 1;

                while (totalLength > maxLength)
                {
                    if (begin > 0)
                        totalLength -= elements[--begin].Length - 1;

                    if (totalLength <= maxLength)
                        break;

                    if (end < filenameIndex)
                        totalLength -= elements[++end].Length - 1;

                    if (begin == 0 && end == filenameIndex)
                        break;
                }

                // assemble final string

                for (var i = 0; i < begin; i++)
                {
                    root += elements[i] + '\\';
                }

                root += "...\\";

                for (var i = end; i < filenameIndex; i++)
                {
                    root += elements[i] + '\\';
                }

                return root + elements[filenameIndex];
            }

            return pathname;
        }

        private void LoadRecentFiles()
        {
            _recentFiles = LoadRecentFilesCore();
        }

        private List<RecentFile> LoadRecentFilesCore()
        {
            var list = RecentFiles;

            var files = new List<RecentFile>(list.Count);

            var i = 0;
            foreach (var filepath in list)
                files.Add(new RecentFile(i++, filepath));

            return files;
        }

        private class RecentFile
        {
            public readonly int Number;
            public readonly string Filepath = "";
            public MenuItem MenuItem;

            public string DisplayPath =>
                Path.Combine(
                    Path.GetDirectoryName(Filepath),
                    Path.GetFileNameWithoutExtension(Filepath));

            public RecentFile(int number, string filepath)
            {
                Number = number;
                Filepath = filepath;
            }
        }

        public class MenuClickEventArgs : EventArgs
        {
            public string Filepath { get; }

            public MenuClickEventArgs(string filepath)
            {
                Filepath = filepath;
            }
        }

        private void MenuItem_Click(object sender, EventArgs e)
        {
            var menuItem = sender as MenuItem;

            OnMenuClick(menuItem);
        }

        protected virtual void OnMenuClick(MenuItem menuItem)
        {
            var filepath = GetFilepath(menuItem);

            if (string.IsNullOrEmpty(filepath)) return;

            var dMenuClick = MenuClick;
            dMenuClick?.Invoke(menuItem, new MenuClickEventArgs(filepath));
        }

        private string GetFilepath(MenuItem menuItem)
        {
            foreach (var r in _recentFiles)
                if (r.MenuItem == menuItem)
                    return r.Filepath;

            return string.Empty;
        }

        //-----------------------------------------------------------------------------------------

        private static class ApplicationAttributes
        {
            private static readonly Assembly Assembly;

            private static readonly AssemblyTitleAttribute TitleAttribute;
            private static readonly AssemblyCompanyAttribute CompanyAttribute;
            private static readonly AssemblyCopyrightAttribute CopyrightAttribute;
            private static readonly AssemblyProductAttribute ProductAttribute;

            public static string Title { get; }
            public static string CompanyName { get; }
            public static string Copyright { get; }
            public static string ProductName { get; }

            private static readonly Version Version;

            static ApplicationAttributes()
            {
                try
                {
                    Title = string.Empty;
                    CompanyName = string.Empty;
                    Copyright = string.Empty;
                    ProductName = string.Empty;

                    Assembly = Assembly.GetEntryAssembly();

                    if (Assembly != null)
                    {
                        var attributes = Assembly.GetCustomAttributes(false);

                        foreach (var attribute in attributes)
                        {
                            var type = attribute.GetType();

                            if (type == typeof(AssemblyTitleAttribute)) TitleAttribute = (AssemblyTitleAttribute)attribute;
                            if (type == typeof(AssemblyCompanyAttribute))
                                CompanyAttribute = (AssemblyCompanyAttribute)attribute;
                            if (type == typeof(AssemblyCopyrightAttribute))
                                CopyrightAttribute = (AssemblyCopyrightAttribute)attribute;
                            if (type == typeof(AssemblyProductAttribute))
                                ProductAttribute = (AssemblyProductAttribute)attribute;
                        }

                        Version = Assembly.GetName().Version;
                    }

                    if (TitleAttribute != null) Title = TitleAttribute.Title;
                    if (CompanyAttribute != null) CompanyName = CompanyAttribute.Company;
                    if (CopyrightAttribute != null) Copyright = CopyrightAttribute.Copyright;
                    if (ProductAttribute != null) ProductName = ProductAttribute.Product;
                    if (Version != null) Version.ToString();
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        //-----------------------------------------------------------------------------------------

        private class RegistryPersister : IPersist
        {
            public string RegistryKey { get; }

            public RegistryPersister()
            {
                RegistryKey =
                    "Software\\" +
                    ApplicationAttributes.CompanyName + "\\" +
                    ApplicationAttributes.ProductName + "\\" +
                    "RecentFileList";
            }

            public RegistryPersister(string key)
            {
                RegistryKey = key;
            }

            private string Key(int i)
            {
                return i.ToString("00");
            }

            public List<string> RecentFiles(int max)
            {
                var k = Registry.CurrentUser.OpenSubKey(RegistryKey) ?? Registry.CurrentUser.CreateSubKey(RegistryKey);

                var list = new List<string>(max);

                for (var i = 0; i < max; i++)
                {
                    var filename = (string)k.GetValue(Key(i));

                    if (string.IsNullOrEmpty(filename)) break;

                    list.Add(filename);
                }

                return list;
            }

            public void InsertFile(string filepath, int max)
            {
                var k = Registry.CurrentUser.OpenSubKey(RegistryKey);
                if (k == null) Registry.CurrentUser.CreateSubKey(RegistryKey);
                k = Registry.CurrentUser.OpenSubKey(RegistryKey, true);

                RemoveFile(filepath, max);

                for (var i = max - 2; i >= 0; i--)
                {
                    var sThis = Key(i);
                    var sNext = Key(i + 1);

                    var oThis = k.GetValue(sThis);
                    if (oThis == null) continue;

                    k.SetValue(sNext, oThis);
                }

                k.SetValue(Key(0), filepath);
            }

            public void RemoveFile(string filepath, int max)
            {
                var k = Registry.CurrentUser.OpenSubKey(RegistryKey);
                if (k == null) return;

                for (var i = 0; i < max; i++)
                {
                    again:
                    var s = (string)k.GetValue(Key(i));
                    if (s != null && s.Equals(filepath, StringComparison.CurrentCultureIgnoreCase))
                    {
                        RemoveFile(i, max);
                        goto again;
                    }
                }
            }

            private void RemoveFile(int index, int max)
            {
                var k = Registry.CurrentUser.OpenSubKey(RegistryKey, true);
                if (k == null) return;

                k.DeleteValue(Key(index), false);

                for (var i = index; i < max - 1; i++)
                {
                    var sThis = Key(i);
                    var sNext = Key(i + 1);

                    var oNext = k.GetValue(sNext);
                    if (oNext == null) break;

                    k.SetValue(sThis, oNext);
                    k.DeleteValue(sNext);
                }
            }
        }

        //-----------------------------------------------------------------------------------------

        private class XmlPersister : IPersist
        {
            public string Filepath { get; }
            public Stream Stream { get; }

            public XmlPersister()
            {
                Filepath =
                    Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        ApplicationAttributes.CompanyName + "\\" +
                        ApplicationAttributes.ProductName + "\\" +
                        "RecentFileList.xml");
            }

            public XmlPersister(string filepath)
            {
                Filepath = filepath;
            }

            public XmlPersister(Stream stream)
            {
                Stream = stream;
            }

            public List<string> RecentFiles(int max)
            {
                return Load(max);
            }

            public void InsertFile(string filepath, int max)
            {
                Update(filepath, true, max);
            }

            public void RemoveFile(string filepath, int max)
            {
                Update(filepath, false, max);
            }

            private void Update(string filepath, bool insert, int max)
            {
                var old = Load(max);

                var list = new List<string>(old.Count + 1);

                if (insert) list.Add(filepath);

                CopyExcluding(old, filepath, list, max);

                Save(list, max);
            }

            private void CopyExcluding(List<string> source, string exclude, List<string> target, int max)
            {
                foreach (var s in source)
                    if (!string.IsNullOrEmpty(s))
                        if (!s.Equals(exclude, StringComparison.OrdinalIgnoreCase))
                            if (target.Count < max)
                                target.Add(s);
            }

            private class SmartStream : IDisposable
            {
                private readonly bool _isStreamOwned = true;

                public Stream Stream { get; private set; }

                public static implicit operator Stream(SmartStream me)
                {
                    return me.Stream;
                }

                public SmartStream(string filepath, FileMode mode)
                {
                    _isStreamOwned = true;

                    Directory.CreateDirectory(Path.GetDirectoryName(filepath));

                    Stream = File.Open(filepath, mode);
                }

                public SmartStream(Stream stream)
                {
                    _isStreamOwned = false;
                    Stream = stream;
                }

                public void Dispose()
                {
                    if (_isStreamOwned && Stream != null) Stream.Dispose();

                    Stream = null;
                }
            }

            private SmartStream OpenStream(FileMode mode)
            {
                if (!string.IsNullOrEmpty(Filepath))
                {
                    return new SmartStream(Filepath, mode);
                }

                return new SmartStream(Stream);
            }

            private List<string> Load(int max)
            {
                var list = new List<string>(max);

                using (var ms = new MemoryStream())
                {
                    using (var ss = OpenStream(FileMode.OpenOrCreate))
                    {
                        if (ss.Stream.Length == 0) return list;

                        ss.Stream.Position = 0;

                        var buffer = new byte[1 << 20];
                        for (;;)
                        {
                            var bytes = ss.Stream.Read(buffer, 0, buffer.Length);
                            if (bytes == 0) break;
                            ms.Write(buffer, 0, bytes);
                        }

                        ms.Position = 0;
                    }

                    XmlTextReader x = null;

                    try
                    {
                        x = new XmlTextReader(ms);

                        while (x.Read())
                        {
                            switch (x.NodeType)
                            {
                                case XmlNodeType.XmlDeclaration:
                                case XmlNodeType.Whitespace:
                                    break;

                                case XmlNodeType.Element:
                                    switch (x.Name)
                                    {
                                        case "RecentFiles": break;

                                        case "RecentFile":
                                            if (list.Count < max) list.Add(x.GetAttribute(0));
                                            break;

                                        default:
                                            Debug.Assert(false);
                                            break;
                                    }

                                    break;

                                case XmlNodeType.EndElement:
                                    switch (x.Name)
                                    {
                                        case "RecentFiles": return list;
                                        default:
                                            Debug.Assert(false);
                                            break;
                                    }

                                    break;

                                default:
                                    Debug.Assert(false);
                                    break;
                            }
                        }
                    }
                    finally
                    {
                        x?.Close();
                    }
                }

                return list;
            }

            private void Save(List<string> list, int max)
            {
                using (var ms = new MemoryStream())
                {
                    XmlTextWriter x = null;

                    try
                    {
                        x = new XmlTextWriter(ms, Encoding.UTF8);
                        if (x == null)
                        {
                            Debug.Assert(false);
                            return;
                        }

                        x.Formatting = Formatting.Indented;

                        x.WriteStartDocument();

                        x.WriteStartElement("RecentFiles");

                        foreach (var filepath in list)
                        {
                            x.WriteStartElement("RecentFile");
                            x.WriteAttributeString("Filepath", filepath);
                            x.WriteEndElement();
                        }

                        x.WriteEndElement();

                        x.WriteEndDocument();

                        x.Flush();

                        using (var ss = OpenStream(FileMode.Create))
                        {
                            ss.Stream.SetLength(0);

                            ms.Position = 0;

                            var buffer = new byte[1 << 20];
                            for (;;)
                            {
                                var bytes = ms.Read(buffer, 0, buffer.Length);
                                if (bytes == 0) break;
                                ss.Stream.Write(buffer, 0, bytes);
                            }
                        }
                    }
                    finally
                    {
                        x?.Close();
                    }
                }
            }
        }

        //-----------------------------------------------------------------------------------------
    }
}