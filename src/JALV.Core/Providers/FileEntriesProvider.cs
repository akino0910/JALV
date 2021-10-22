using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using JALV.Core.Domain;
using JALV.Core.Exceptions;

namespace JALV.Core.Providers
{
    public class FileEntriesProvider : AbstractEntriesProvider
    {
        private const string Separator = "[---]";
        private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss,fff";

        public override IEnumerable<LogItem> GetEntries(string dataSource, FilterParams filter)
        {
            if (string.IsNullOrEmpty(dataSource))
                throw new ArgumentNullException("dataSource");
            if (filter == null)
                throw new ArgumentNullException("filter");

            var pattern = filter.Pattern;
            if (string.IsNullOrEmpty(pattern))
                throw new NotValidValueException("filter pattern null");

            var file = new FileInfo(dataSource);
            if (!file.Exists)
                throw new FileNotFoundException("file not found", dataSource);

            var regex = new Regex(@"%\b(date|message|level)\b");
            var matches = regex.Matches(pattern);

            using (var reader = file.OpenText())
            {
                string s;
                while ((s = reader.ReadLine()) != null)
                {
                    var items = s.Split(new[] { Separator }, StringSplitOptions.RemoveEmptyEntries);
                    var entry = CreateEntry(items, matches);
                    entry.Logger = filter.Logger;
                    yield return entry;
                }
            }
        }

        private static LogItem CreateEntry(string[] items, MatchCollection matches)
        {
            if (items == null)
                throw new ArgumentNullException("items");
            if (matches == null)
                throw new ArgumentNullException("matches");

            if (items.Length != matches.Count)
                throw new NotValidValueException("different length of items/matches values");

            var entry = new LogItem();
            for (var i = 0; i < matches.Count; i++)
            {
                var value = items[i];
                var match = matches[i];
                var name = match.Value;
                switch (name)
                {
                    case "%date":
                        entry.TimeStamp = DateTime.ParseExact(
                            value, DateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
                        break;

                    case "%message":
                        entry.Message = value;
                        break;

                    case "%level":
                        entry.Level = value;
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(name, "unmanaged value");
                }
            }

            return entry;
        }
    }
}