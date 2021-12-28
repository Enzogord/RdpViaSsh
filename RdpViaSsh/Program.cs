using Microsoft.Extensions.Configuration;
using System;
using System.IO;
namespace RdpViaSsh
{
    static class Program
    {
        static void Main(string[] args)
        {
            ConsoleManager consoleManager = new ConsoleManager();
            IpPortProvider ipPortProvider = new IpPortProvider();

            consoleManager.OpenConsole();

            try
            {
                var userSettingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RdpViaSsh", "setting.ini");
                if(!File.Exists(userSettingsPath))
                {
                    Console.WriteLine($"Не найден файл конфигурации {userSettingsPath}");
                    Console.Read();
                    return;
                }
                var config = ReadConfiguration(userSettingsPath);

                RdpConnector connector = new RdpConnector(consoleManager, ipPortProvider, config);
                connector.SelectSettingAndConnect();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
                Console.Read();
            }
        }

        private static IConfiguration ReadConfiguration(string path)
        {
            var builder = new ConfigurationBuilder()
                .AddIniFile(path, optional: false);

            var configuration = builder.Build();
            return configuration;
        }
    }
}
