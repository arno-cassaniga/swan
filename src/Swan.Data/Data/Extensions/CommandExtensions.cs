﻿namespace Swan.Data.Extensions;

/// <summary>
/// Provides extension methods for <see cref="IDbCommand"/> objects.
/// </summary>
public static partial class CommandExtensions
{
    /// <summary>
    /// Tries to preprare a command on the server side.
    /// Useful when executing the command multiple times by varying argument values.
    /// </summary>
    /// <typeparam name="T">The compatible command type.</typeparam>
    /// <param name="command">The command object.</param>
    /// <param name="exception">When prepare fails, the associated exception.</param>
    /// <returns>True if prepare succeeded. False otherwise.</returns>
    public static bool TryPrepare<T>(this T command, [NotNullWhen(false)] out Exception? exception)
        where T : IDbCommand
    {
        exception = null;

        if (command is null)
        {
            exception = new ArgumentNullException(nameof(command));
            return false;
        }

        try
        {
            command.Prepare();
            return true;
        }
        catch (Exception ex)
        {
            exception = ex;
            return false;
        }
    }

    /// <summary>
    /// Tries to find a parameter within the command parameter collection using the given name.
    /// The search is case-insensitive and the name can optionally start with a
    /// parameter prefix.
    /// </summary>
    /// <typeparam name="T">The command type.</typeparam>
    /// <param name="command">The command to search.</param>
    /// <param name="name">The name to find.</param>
    /// <param name="parameter">The parameter output (if found).</param>
    /// <returns>True if the paramater was found. False otherwise.</returns>
    public static bool TryFindParameter<T>(this T command, string name, [MaybeNullWhen(false)] out IDbDataParameter parameter)
        where T : IDbCommand
    {
        if (command is null)
            throw new ArgumentNullException(nameof(command));

        if (command.Connection is null)
            throw new ArgumentException(Library.CommandConnectionErrorMessage, nameof(command));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentNullException(nameof(name));

        var provider = command.Connection.Provider();
        var quotedName = provider.QuoteParameter(name);
        var unquotedName = provider.UnquoteParameter(name);

        parameter = default;
        foreach (IDbDataParameter p in command.Parameters)
        {
            var parameterName = p.ParameterName;

            if (string.IsNullOrWhiteSpace(parameterName))
                continue;

            if (string.Equals(unquotedName, parameterName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(quotedName, parameterName, StringComparison.OrdinalIgnoreCase))
            {
                parameter = p;
                return true;
            }
        }

        return false;
    }

    public static IDbDataParameter DefineParameter(this IDbCommand command, string name, DbType dbType,
        ParameterDirection direction = ParameterDirection.Input, int size = default, int precision = default, int scale = default, bool isNullable = default)
    {
        if (command is null)
            throw new ArgumentNullException(nameof(command));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentNullException(nameof(name));

        var needsAdding = false;
        if (!command.TryFindParameter(name, out var parameter))
        {
            needsAdding = true;
            parameter = command.CreateParameter();
            parameter.ParameterName = name;
        }

        parameter.DbType = dbType;
        parameter.Direction = direction;
        parameter.Size = (size == default && dbType == DbType.String) ? 4000 : size;
        parameter.Precision = Convert.ToByte(precision.Clamp(0, 255));
        parameter.Scale = Convert.ToByte(scale.Clamp(0, 255));

        if (isNullable)
        {
            parameter.GetType().TypeInfo().TryWriteProperty(
                parameter, nameof(IDataParameter.IsNullable), true);
        }

        if (needsAdding)
            command.Parameters.Add(parameter);

        return parameter;
    }

    public static IDbDataParameter DefineParameter(this IDbCommand command, string name, Type clrType,
        ParameterDirection direction = ParameterDirection.Input, int size = default, int precision = default, int scale = default, bool isNullable = default)
    {
        if (command is null)
            throw new ArgumentNullException(nameof(command));

        if (command.Connection is null)
            throw new ArgumentException(Library.CommandConnectionErrorMessage, nameof(command));

        var provider = command.Connection.Provider();
        if (!provider.TypeMapper.TryGetDbTypeFor(clrType, out var dbType))
            dbType = DbType.String;

        return command.DefineParameter(name, dbType.GetValueOrDefault(DbType.String), direction, size, precision, scale, isNullable);
    }

    public static IDbDataParameter DefineParameter(this IDbCommand command, IDbColumn column)
    {
        if (command is null)
            throw new ArgumentNullException(nameof(command));

        if (column is null)
            throw new ArgumentNullException(nameof(column));

        return command.DefineParameter(column.Name, column.DataType, ParameterDirection.Input,
            column.MaxLength, column.Precision, column.Scale, column.AllowsDBNull);
    }

    public static TCommand DefineParameters<TCommand>(this TCommand command, IEnumerable<IDbColumn> columns)
        where TCommand : IDbCommand
    {
        if (command is null)
            throw new ArgumentNullException(nameof(command));

        if (columns is null)
            throw new ArgumentNullException(nameof(columns));

        foreach (var column in columns)
            command.DefineParameter(column);

        return command;
    }

    public static TCommand SetParameter<TCommand, TValue>(this TCommand command, string name, TValue value, int? size = default)
        where TCommand : IDbCommand => command.SetParameter(name, value, typeof(TValue), size);

    public static TCommand SetParameter<TCommand>(this TCommand command, string name, object? value, Type clrType, int? size = default)
        where TCommand : IDbCommand
    {
        if (command is null)
            throw new ArgumentNullException(nameof(command));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentNullException(name);

        var isNullValue = Equals(value, null) || Equals(value, DBNull.Value);
        object dataValue = isNullValue ? DBNull.Value : value!;

        // Let's update the parameter if it already exists.
        if (command.TryFindParameter(name, out var parameter))
        {
            parameter.Value = dataValue;
            if (size.HasValue)
                parameter.Size = size.Value;

            return command;
        }

        parameter = command.DefineParameter(name, clrType, size: size.GetValueOrDefault());
        parameter.Value = dataValue;
        return command;
    }

    public static TCommand SetParameter<TCommand>(this TCommand command, string name, object? value, DbType dbType, int? size = default)
        where TCommand : IDbCommand
    {
        if (command is null)
            throw new ArgumentNullException(nameof(command));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentNullException(name);

        var isNullValue = Equals(value, null) || Equals(value, DBNull.Value);
        object dataValue = isNullValue ? DBNull.Value : value!;

        // Update the parameter if it exists.
        if (command.TryFindParameter(name, out var parameter))
        {
            parameter.Value = dataValue;
            parameter.DbType = dbType;

            if (size.HasValue)
                parameter.Size = size.Value;

            return command;
        }

        parameter = command.DefineParameter(name, dbType, size: size.GetValueOrDefault());
        parameter.Value = dataValue;
        return command;
    }

    /// <summary>
    /// Takes the given parameters object, extracts its publicly visible properties and values
    /// and adds the to the command's parameter collection. If the command text is set, it looks
    /// for the parameters within the command text before adding them.
    /// </summary>
    /// <typeparam name="TCommand"></typeparam>
    /// <param name="command"></param>
    /// <param name="parameters"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public static TCommand SetParameters<TCommand>(this TCommand command, object parameters)
        where TCommand : IDbCommand
    {
        if (command is null)
            throw new ArgumentNullException(nameof(command));

        if (command.Connection is null)
            throw new ArgumentException(Library.CommandConnectionErrorMessage, nameof(command));

        if (parameters is null)
            return command;

        var typeInfo = parameters.GetType().TypeInfo();
        var provider = command.Connection.Provider();
        
        var hasCommandText = !string.IsNullOrWhiteSpace(command.CommandText);
        var commandText = hasCommandText
            ? command.CommandText.AsSpan()
            : Array.Empty<char>().AsSpan();

        foreach (var (propertyName, property) in typeInfo.Properties)
        {
            if (!property.CanRead || !property.HasPublicGetter || !property.PropertyType.IsBasicType ||
                propertyName.Contains('.', StringComparison.Ordinal))
                continue;

            var parameterName = provider.QuoteParameter(propertyName);
            var containsParamter = hasCommandText && commandText.IndexOf(parameterName, StringComparison.InvariantCulture) >= 0;
            if (hasCommandText && !containsParamter)
                continue;

            if (property.TryRead(parameters, out var value))
                command.SetParameter(propertyName, value, property.PropertyType.BackingType.NativeType);
        }

        return command;
    }

    /// <summary>
    /// Sets the command's basic properties. Properties with null values will not be set.
    /// Calling this method with default argument only will result in no modification of the
    /// command object.
    /// </summary>
    /// <typeparam name="TCommand">The compatible command type.</typeparam>
    /// <param name="command">The command object.</param>
    /// <param name="commandText">The optional command text.</param>
    /// <param name="commandType">The optional command type.</param>
    /// <param name="dbTransaction">The optional associated transaction.</param>
    /// <param name="timeout">The optional command timeout.</param>
    /// <returns>The modified command object.</returns>
    public static TCommand WithProperties<TCommand>(this TCommand command, string? commandText = default, CommandType? commandType = default, IDbTransaction? dbTransaction = default, TimeSpan? timeout = default)
        where TCommand : IDbCommand
    {
        if (command is null)
            throw new ArgumentNullException(nameof(command));

        if (commandType.HasValue)
            command.CommandType = commandType.Value;

        if (dbTransaction != default)
            command.Transaction = dbTransaction;

        if (timeout.HasValue)
            command.CommandTimeout = Convert.ToInt32(timeout.Value.TotalSeconds).ClampMin(0);

        if (!string.IsNullOrWhiteSpace(commandText))
            command.CommandText = commandText;

        return command;
    }

    /// <summary>
    /// Appends the specified text to the <see cref="IDbCommand.CommandText"/>.
    /// Automatic spacing is enabled, and therefore, if the command text does not end with
    /// whitespace, it automatically adds a space between the existing command text and the appended
    /// one so you don't have to.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <param name="command">The command to append text to.</param>
    /// <param name="text">The text to append.</param>
    /// <param name="autoSpace">The auto-spacing flag.</param>
    /// <returns>The command with the modified command text.</returns>
    public static TCommand AppendText<TCommand>(this TCommand command, string text, bool autoSpace = true)
        where TCommand : IDbCommand
    {
        if (command is null)
            throw new ArgumentNullException(nameof(command));

        if (command.CommandText is null)
        {
            command.CommandText = text;
            return command;
        }

        command.CommandText = (autoSpace && command.CommandText.Length > 0 && !char.IsWhiteSpace(command.CommandText[0]))
            ? $"{command.CommandText} {text}"
            : $"{command.CommandText}{text}";

        return command;
    }

    /// <summary>
    /// Sets a command text to the provided command.
    /// </summary>
    /// <typeparam name="TCommand">The compatible command type.</typeparam>
    /// <param name="command">The command object.</param>
    /// <param name="commandText">The command text.</param>
    /// <returns>The modified command object.</returns>
    public static TCommand WithText<TCommand>(this TCommand command, string? commandText)
        where TCommand : IDbCommand
    {
        if (command is null)
            throw new ArgumentNullException(nameof(command));

        command.CommandText = commandText ?? string.Empty;
        return command;
    }

    /// <summary>
    /// Sets a trasnaction (or null value) to the provided command.
    /// </summary>
    /// <typeparam name="TCommand">The compatible command type.</typeparam>
    /// <param name="command">The command object.</param>
    /// <param name="transaction">The transaction.</param>
    /// <returns>The modified command object.</returns>
    public static TCommand WithTransaction<TCommand>(this TCommand command, IDbTransaction? transaction)
        where TCommand : IDbCommand
    {
        if (command is null)
            throw new ArgumentNullException(nameof(command));

        command.Transaction = transaction;
        return command;
    }

    /// <summary>
    /// Sets a command execution timeout.
    /// The timeout includes both, execution of the command and
    /// transfer of the results packets over the network.
    /// </summary>
    /// <param name="command">The command object.</param>
    /// <param name="timeout">The timeout value.</param>
    /// <returns>The modified command object.</returns>
    public static TCommand WithTimeout<TCommand>(this TCommand command, TimeSpan timeout)
        where TCommand : IDbCommand
    {
        if (command is null)
            throw new ArgumentNullException(nameof(command));

        command.CommandTimeout = Convert.ToInt32(timeout.TotalSeconds).ClampMin(0);
        return command;
    }

    /// <summary>
    /// Sets a command execution timeout.
    /// The timeout includes both, execution of the command and
    /// transfer of the results packets over the network.
    /// </summary>
    /// <param name="command">The command object.</param>
    /// <param name="seconds">The timeout value in seconds.</param>
    /// <returns>The modified command object.</returns>
    public static TCommand WithTimeout<TCommand>(this TCommand command, int seconds)
        where TCommand : IDbCommand
    {
        if (command is null)
            throw new ArgumentNullException(nameof(command));

        command.CommandTimeout = seconds.ClampMin(0);
        return command;
    }

    /// <summary>
    /// Sets the command type. Typically text or stored procedure.
    /// </summary>
    /// <param name="command">The command object.</param>
    /// <param name="commandType">The command type.</param>
    /// <returns>The modified command object.</returns>
    public static TCommand WithCommandType<TCommand>(this TCommand command, CommandType commandType)
        where TCommand : IDbCommand
    {
        if (command is null)
            throw new ArgumentNullException(nameof(command));

        command.CommandType = commandType;
        return command;
    }
}