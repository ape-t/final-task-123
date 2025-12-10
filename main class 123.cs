using System;
using System.Collections.Generic;
using System.Data.SqlClient;

public class SqlServerChecker
{
    public void CheckSqlServerAvailability(string connectionString)
    {
        SqlConnection connection = null;
        
        try
        {
            Console.WriteLine("Подключение к SQL Server...");
            connection = new SqlConnection(connectionString);
            connection.Open();
            
            if (connection.State == System.Data.ConnectionState.Open)
            {
                Console.WriteLine("Подключение успешно!");
                Console.WriteLine($"Версия сервера: {connection.ServerVersion}");
                
                using (SqlCommand command = new SqlCommand("SELECT @@VERSION", connection))
                {
                    var version = command.ExecuteScalar();
                    Console.WriteLine($"Полная версия: {version}");
                }
            }
        }
        catch (SqlException sqlEx)
        {
            Console.WriteLine($"Ошибка SQL: {sqlEx.Message}");
            
            if (sqlEx.Number == 18456)
                Console.WriteLine("Неверный логин или пароль");
            else if (sqlEx.Number == 4060)
                Console.WriteLine("База данных не найдена");
            else if (sqlEx.Number == -1 || sqlEx.Number == 53)
                Console.WriteLine("Сервер недоступен");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
        }
        finally
        {
            if (connection != null)
            {
                connection.Close();
                connection.Dispose();
            }
        }
    }
}

public enum EnvironmentType
{
    Development,
    Production
}

public class ConnectionManager : IDisposable
{
    private Dictionary<EnvironmentType, string> connectionStrings;
    private Dictionary<EnvironmentType, SqlConnection> connections;
    
    public ConnectionManager()
    {
        connectionStrings = new Dictionary<EnvironmentType, string>();
        connections = new Dictionary<EnvironmentType, SqlConnection>();
        
        connectionStrings[EnvironmentType.Development] = "Server=localhost;Database=DevDB;Integrated Security=True;";
        connectionStrings[EnvironmentType.Production] = "Server=prod-server;Database=ProdDB;User Id=admin;Password=pass;";
    }
    
    public SqlConnection GetConnection(EnvironmentType environment)
    {
        if (!connectionStrings.ContainsKey(environment))
            throw new Exception($"Нет строки подключения для {environment}");
        
        if (connections.ContainsKey(environment) && 
            connections[environment] != null && 
            connections[environment].State == System.Data.ConnectionState.Open)
        {
            return connections[environment];
        }
        
        var connection = new SqlConnection(connectionStrings[environment]);
        connection.Open();
        connections[environment] = connection;
        
        return connection;
    }
    
    public void CloseAllConnections()
    {
        foreach (var conn in connections.Values)
        {
            if (conn != null && conn.State != System.Data.ConnectionState.Closed)
            {
                conn.Close();
            }
        }
        connections.Clear();
    }
    
    public void ClearConnectionPool(EnvironmentType environment)
    {
        if (connectionStrings.ContainsKey(environment))
        {
            SqlConnection.ClearPool(new SqlConnection(connectionStrings[environment]));
        }
    }
    
    public void SetConnectionString(EnvironmentType environment, string connectionString)
    {
        connectionStrings[environment] = connectionString;
    }
    
    public void Dispose()
    {
        CloseAllConnections();
        
        foreach (var env in connectionStrings.Keys)
        {
            ClearConnectionPool(env);
        }
    }
}

public class Program
{
    public static void Main()
    {
        var checker = new SqlServerChecker();
        checker.CheckSqlServerAvailability("Server=localhost;Database=TestDB;Integrated Security=True;");
        
        using (var manager = new ConnectionManager())
        {
            var devConnection = manager.GetConnection(EnvironmentType.Development);
            Console.WriteLine($"Dev база: {devConnection.Database}");
            
            var prodConnection = manager.GetConnection(EnvironmentType.Production);
            Console.WriteLine($"Prod база: {prodConnection.Database}");
        }
    }
}