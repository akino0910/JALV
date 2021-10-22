using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using LogViewer.Core.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LogViewer.Core.Providers
{
    internal class JsonEntriesProvider : AbstractEntriesProvider
    {
        public override IEnumerable<LogItem> GetEntries(string dataSource, FilterParams filter)
        {
            var entryId = 1;

            var fs = new FileStream(dataSource, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var sr = new StreamReader(fs);

            while (!sr.EndOfStream)
            {
                var line = sr.ReadLine();
                var lineObject = JsonConvert.DeserializeObject<JObject>(line);

                string[] dateFormats =
                {
                    "MM/dd/yyyy HH:mm:ss",
                    "yyyy-MM-ddTHH:mm:ssZ", "yyyy-MM-dd HH:mm:ssZ",
                    "yyyy-MM-ddTHH:mm:ss.fffZ", "yyyy-MM-dd HH:mm:ss.fffZ",
                    "yyyy-MM-ddTHH:mm:ss,fffZ", "yyyy-MM-dd HH:mm:ss,fffZ",
                    "yyyy-MM-dd HH:mm:ss.fff", "yyyy-MM-dd HH:mm:ss,fff"
                };
                var dateString = lineObject?.SelectToken("date")?.Value<string>() ?? "";
                DateTime timestamp;
                try
                {
                    timestamp = DateTime.ParseExact(dateString, dateFormats, new CultureInfo("en-US"),
                        DateTimeStyles.None);
                }
                catch (FormatException)
                {
                    timestamp = new DateTime();
                }

                var entry = new LogItem
                {
                    File = dataSource,
                    Message = lineObject.SelectToken("message")?.Value<string>() ?? "",
                    TimeStamp = timestamp,
                    Level = lineObject.SelectToken("level")?.Value<string>()?.ToUpper() ?? "",
                    App = lineObject.SelectToken("appName")?.Value<string>() ?? "",
                    Logger = lineObject.SelectToken("instanceId")?.Value<string>() ?? "",
                    RequestId = lineObject.SelectToken("requestId")?.Value<string>() ?? "",
                    Thread = lineObject.SelectToken("thread")?.Value<string>() ?? "",
                    UserName = lineObject.SelectToken("user")?.Value<string>() ?? "",
                    MachineName = lineObject.SelectToken("machineName")?.Value<string>() ?? "",
                    HostName = lineObject.SelectToken("hostname")?.Value<string>() ?? "",
                    Throwable = lineObject.SelectToken("exception")?.Value<string>() ?? "",
                    Class = lineObject.SelectToken("logger")?.Value<string>() ?? "",
                    Method = lineObject.SelectToken("method")?.Value<string>() ?? "",
                    Id = entryId,
                    Path = dataSource
                };

                if (FilterByParameters(entry, filter))
                {
                    yield return entry;
                    entryId++;
                }
            }
        }

        private static bool FilterByParameters(LogItem entry, FilterParams parameters)
        {
            if (entry == null)
                throw new ArgumentNullException("entry");
            if (parameters == null)
                throw new ArgumentNullException("parameters");

            var accept = false;
            switch (parameters.Level)
            {
                case 1:
                    accept |= string.Equals(entry.Level, "ERROR",
                        StringComparison.InvariantCultureIgnoreCase);
                    break;

                case 2:
                    if (string.Equals(entry.Level, "INFO",
                        StringComparison.InvariantCultureIgnoreCase))
                        accept = true;
                    break;

                case 3:
                    if (string.Equals(entry.Level, "DEBUG",
                        StringComparison.InvariantCultureIgnoreCase))
                        accept = true;
                    break;
                case 4:
                    if (string.Equals(entry.Level, "WARN",
                        StringComparison.InvariantCultureIgnoreCase))
                        accept = true;
                    break;
                case 5:
                    if (string.Equals(entry.Level, "FATAL",
                        StringComparison.InvariantCultureIgnoreCase))
                        accept = true;
                    break;

                default:
                    accept = true;
                    break;
            }

            if (parameters.Date.HasValue)
                if (entry.TimeStamp < parameters.Date)
                    accept = false;

            if (!string.IsNullOrEmpty(parameters.Thread))
                if (!string.Equals(entry.Thread, parameters.Thread, StringComparison.InvariantCultureIgnoreCase))
                    accept = false;

            if (!string.IsNullOrEmpty(parameters.Message))
                if (!entry.Message.ToUpper().Contains(parameters.Message.ToUpper()))
                    accept = false;

            if (!string.IsNullOrEmpty(parameters.Logger))
                if (!entry.Logger.ToUpper().Contains(parameters.Logger.ToUpper()))
                    accept = false;

            return accept;
        }
    }
}