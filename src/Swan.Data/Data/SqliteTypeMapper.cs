﻿namespace Swan.Data;

internal class SqliteTypeMapper : DbTypeMapper
{
    public SqliteTypeMapper()
        : base()
    {
        ProviderTypeMap[typeof(double)] = "DOUBLE";
        ProviderTypeMap[typeof(bool)] = "BOOLEAN";
        ProviderTypeMap[typeof(Guid)] = "TEXT";
        ProviderTypeMap[typeof(DateTime)] = "DATETIME";
        ProviderTypeMap[typeof(DateTimeOffset)] = "DATETIME";
        ProviderTypeMap[typeof(byte[])] = "BLOB";
    }
}
