using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

using JALV.Core.Domain;

namespace JALV.Core.Providers
{
    class JsonEntriesProvider : AbstractEntriesProvider
    {
        public override IEnumerable<LogItem> GetEntries(string dataSource, FilterParams filter)
        {
            var entryId = 1;

            FileStream fs = new FileStream(dataSource, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            StreamReader sr = new StreamReader(fs);

            while (!sr.EndOfStream)
            {
                var line = sr.ReadLine();
                var lineObject = JsonConvert.DeserializeObject<JObject>(line);

                string[] dateFormats = { "MM/dd/yyyy HH:mm:ss",
                    "yyyy-MM-ddTHH:mm:ssZ", "yyyy-MM-dd HH:mm:ssZ",
                    "yyyy-MM-ddTHH:mm:ss.fffZ", "yyyy-MM-dd HH:mm:ss.fffZ",
                    "yyyy-MM-ddTHH:mm:ss,fffZ", "yyyy-MM-dd HH:mm:ss,fffZ"  };
                var dateString = lineObject.SelectToken("date")?.Value<String>() ?? "";
                DateTime timestamp;
                try
                {
                    timestamp = DateTime.ParseExact(dateString, dateFormats, new CultureInfo("en-US"), DateTimeStyles.None);
                }
                catch (FormatException)
                {
                    timestamp = new DateTime();
                }

                LogItem entry = new LogItem()
                {
                    File = dataSource,
                    Message = lineObject.SelectToken("message")?.Value<String>() ?? "",
                    TimeStamp = timestamp,
                    Level = lineObject.SelectToken("level")?.Value<String>().ToUpper() ?? "",
                    App = lineObject.SelectToken("appname")?.Value<String>() ?? "",
                    Logger = lineObject.SelectToken("instanceId")?.Value<String>() ?? "",
                    RequestId = lineObject.SelectToken("requestId")?.Value<String>() ?? "",
                    Thread = lineObject.SelectToken("thread")?.Value<String>() ?? "",
                    UserName = lineObject.SelectToken("user")?.Value<String>() ?? "",
                    HostName = lineObject.SelectToken("hostname")?.Value<String>() ?? "",
                    Throwable = lineObject.SelectToken("exception")?.Value<String>() ?? "",
                    Class = lineObject.SelectToken("logger")?.Value<String>() ?? "",
                    Method = lineObject.SelectToken("method")?.Value<String>() ?? "",
                    Id = entryId,
                    Path = dataSource
                };

                if (filterByParameters(entry, filter))
                {
                    yield return entry;
                    entryId++;
                }
            }
        }

        private static bool filterByParameters(LogItem entry, FilterParams parameters)
        {
            if (entry == null)
                throw new ArgumentNullException("entry");
            if (parameters == null)
                throw new ArgumentNullException("parameters");

            bool accept = false;
            switch (parameters.Level)
            {
                case 1:
                    accept |= String.Equals(entry.Level, "ERROR",
                        StringComparison.InvariantCultureIgnoreCase);
                    break;

                case 2:
                    if (String.Equals(entry.Level, "INFO",
                        StringComparison.InvariantCultureIgnoreCase))
                        accept = true;
                    break;

                case 3:
                    if (String.Equals(entry.Level, "DEBUG",
                        StringComparison.InvariantCultureIgnoreCase))
                        accept = true;
                    break;
                case 4:
                    if (String.Equals(entry.Level, "WARN",
                        StringComparison.InvariantCultureIgnoreCase))
                        accept = true;
                    break;
                case 5:
                    if (String.Equals(entry.Level, "FATAL",
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

            if (!String.IsNullOrEmpty(parameters.Thread))
                if (!String.Equals(entry.Thread, parameters.Thread, StringComparison.InvariantCultureIgnoreCase))
                    accept = false;

            if (!String.IsNullOrEmpty(parameters.Message))
                if (!entry.Message.ToUpper().Contains(parameters.Message.ToUpper()))
                    accept = false;

            if (!String.IsNullOrEmpty(parameters.Logger))
                if (!entry.Logger.ToUpper().Contains(parameters.Logger.ToUpper()))
                    accept = false;

            return accept;
        }
    }
}
