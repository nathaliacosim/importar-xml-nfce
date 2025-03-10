using ImportarXML.Data;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.IO;

namespace ImportarXML;

public class Program
{
    public static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        var config = LoadConfiguration();

        string postgresConnectionString = ConfigurePostgres(config);
        if (string.IsNullOrEmpty(postgresConnectionString))
        {
            Console.WriteLine("❌ A string de conexão do PostgreSQL está vazia ou nula.");
            return;
        }

        Console.WriteLine($"📡 Conexão com PostgreSQL estabelecida com sucesso!");

        string projectDirectory = Directory.GetCurrentDirectory();
        Console.WriteLine($"📂 Diretório do projeto: {projectDirectory}");

        string folderPath = Path.Combine(projectDirectory, "XMLFiles");
        if (!Directory.Exists(folderPath))
        {
            Console.WriteLine($"❌ A pasta '{folderPath}' não foi encontrada.");
            return;
        }

        var xmlFiles = Directory.GetFiles(folderPath, "*.xml");
        if (xmlFiles.Length == 0)
        {
            Console.WriteLine($"❌ Nenhum arquivo XML foi encontrado na pasta '{folderPath}'.");
            return;
        }

        var xmlRepository = new XmlRepository(postgresConnectionString);
        xmlRepository.ProcessarXmls();
    }

    private static IConfiguration LoadConfiguration()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        Console.WriteLine($"🌍 Host: {config["Postgres:Host"]}");
        Console.WriteLine($"📍 Porta: {config["Postgres:Port"]}");
        Console.WriteLine($"📚 Banco de Dados: {config["Postgres:Database"]}");
        Console.WriteLine($"🔑 Usuário: {config["Postgres:Username"]}");

        return config;
    }

    private static string ConfigurePostgres(IConfiguration config)
    {
        string host = config["Postgres:Host"];
        int port = int.Parse(config["Postgres:Port"]);
        string database = config["Postgres:Database"];
        string username = config["Postgres:Username"];
        string password = config["Postgres:Password"];

        var connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password}";
        Console.WriteLine($"🔌 Connection string do PostgreSQL: {connectionString}\n");

        return connectionString;
    }
}