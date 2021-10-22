using System;
using LogViewer.Core.Domain;

namespace LogViewer.Core.Providers
{
    public static class EntriesProviderFactory
    {
        public static AbstractEntriesProvider GetProvider(EntriesProviderType type = EntriesProviderType.Xml)
        {
            switch (type)
            {
                case EntriesProviderType.Json:
                    return new JsonEntriesProvider();

                case EntriesProviderType.Xml:
                    return new XmlEntriesProvider();

                case EntriesProviderType.Sqlite:
                    return new SqliteEntriesProvider();

                case EntriesProviderType.MsSqlServer:
                    return new MsSqlServerEntriesProvider();

                default:
                    var message = $"Type {type} not supported";
                    throw new NotImplementedException(message);
            }
        }
    }
}