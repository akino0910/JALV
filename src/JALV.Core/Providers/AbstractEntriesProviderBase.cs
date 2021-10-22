using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using JALV.Core.Domain;

namespace JALV.Core.Providers
{
    public abstract class AbstractEntriesProviderBase : AbstractEntriesProvider
    {
        public override IEnumerable<LogItem> GetEntries(string dataSource, FilterParams filter)
        {
            var enumerable = InternalGetEntries(dataSource, filter);
            return enumerable.ToArray(); // avoid file locks            
        }

        private IEnumerable<LogItem> InternalGetEntries(string dataSource, FilterParams filter)
        {
            using (var connection = CreateConnection(dataSource))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText =
                            @"select caller, date, level, logger, thread, message, exception from log where date >= @date";

                        var parameter = command.CreateParameter();
                        parameter.ParameterName = "@date";
                        parameter.Value = filter.Date ?? MinDateTime;
                        command.Parameters.Add(parameter);

                        switch (filter.Level)
                        {
                            case 1:
                                AddLevelClause(command, "ERROR");
                                break;

                            case 2:
                                AddLevelClause(command, "INFO");
                                break;

                            case 3:
                                AddLevelClause(command, "DEBUG");
                                break;

                            case 4:
                                AddLevelClause(command, "WARN");
                                break;

                            case 5:
                                AddLevelClause(command, "FATAL");
                                break;
                        }

                        AddLoggerClause(command, filter.Logger);
                        AddThreadClause(command, filter.Thread);
                        AddMessageClause(command, filter.Message);

                        AddOrderByClause(command);

                        using (var reader = command.ExecuteReader())
                        {
                            var index = 0;
                            while (reader.Read())
                            {
                                var caller = reader.GetString(0);
                                var split = caller.Split(',');

                                const string machineKey = "{log4jmachinename=";
                                var item0 = Find(split, machineKey);
                                var machineName = GetValue(item0, machineKey);

                                const string hostKey = " log4net:HostName=";
                                var item1 = Find(split, hostKey);
                                var hostName = GetValue(item1, hostKey);

                                const string userKey = " log4net:UserName=";
                                var item2 = Find(split, userKey);
                                var userName = GetValue(item2, userKey);

                                const string appKey = " log4japp=";
                                var item3 = Find(split, appKey);
                                var app = GetValue(item3, appKey);

                                var timeStamp = reader.GetDateTime(1);
                                var level = reader.GetString(2);
                                var logger = reader.GetString(3);
                                var thread = reader.GetString(4);
                                var message = reader.GetString(5);
                                var exception = reader.GetString(6);

                                var entry = new LogItem
                                {
                                    Id = ++index,
                                    TimeStamp = timeStamp,
                                    Level = level,
                                    Thread = thread,
                                    Logger = logger,
                                    Message = message,
                                    Throwable = exception,
                                    MachineName = machineName,
                                    HostName = hostName,
                                    UserName = userName,
                                    App = app
                                };
                                // TODO: other filters
                                yield return entry;
                            }
                        }
                    }

                    transaction.Commit();
                }
            }
        }

        protected abstract IDbConnection CreateConnection(string dataSource);

        private static void AddLevelClause(IDbCommand command, string level)
        {
            if (command == null)
                throw new ArgumentNullException("command");
            if (string.IsNullOrEmpty(level))
                throw new ArgumentNullException("level");

            command.CommandText += @" and level = @level";

            var parameter = command.CreateParameter();
            parameter.ParameterName = "@level";
            parameter.Value = level;
            command.Parameters.Add(parameter);
        }

        private static void AddLoggerClause(IDbCommand command, string logger)
        {
            if (command == null)
                throw new ArgumentNullException("command");
            if (string.IsNullOrEmpty(logger))
                return;

            command.CommandText += @" and logger like @logger";

            var parameter = command.CreateParameter();
            parameter.ParameterName = "@logger";
            parameter.Value = $"%{logger}%";
            command.Parameters.Add(parameter);
        }

        private static void AddThreadClause(IDbCommand command, string thread)
        {
            if (command == null)
                throw new ArgumentNullException("command");
            if (string.IsNullOrEmpty(thread))
                return;

            command.CommandText += @" and thread like @thread";

            var parameter = command.CreateParameter();
            parameter.ParameterName = "@thread";
            parameter.Value = $"%{thread}%";
            command.Parameters.Add(parameter);
        }

        private static void AddMessageClause(IDbCommand command, string message)
        {
            if (command == null)
                throw new ArgumentNullException("command");
            if (string.IsNullOrEmpty(message))
                return;

            command.CommandText += @" and message like @message";

            var parameter = command.CreateParameter();
            parameter.ParameterName = "@message";
            parameter.Value = $"%{message}%";
            command.Parameters.Add(parameter);
        }

        private static void AddOrderByClause(IDbCommand command)
        {
            if (command == null)
                throw new ArgumentNullException("command");

            command.CommandText += @" order by date ";
        }

        private static string GetValue(string item, string key)
        {
            return string.IsNullOrEmpty(item) ? string.Empty : item.Remove(0, key.Length);
        }

        private static string Find(IEnumerable<string> items, string key)
        {
            return items.SingleOrDefault(i => i.StartsWith(key));
        }
    }
}