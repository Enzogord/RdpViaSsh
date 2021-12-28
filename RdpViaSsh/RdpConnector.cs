using Microsoft.Extensions.Configuration;
using Renci.SshNet;
using Renci.SshNet.Common;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;

namespace RdpViaSsh
{
    public class RdpConnector
    {
        private readonly ConsoleManager consoleManager;
        private readonly IpPortProvider ipPortProvider;
        private SshClient sshClient;
		private IConfiguration configuration;

		public RdpConnector(ConsoleManager consoleManager, IpPortProvider ipPortProvider, IConfiguration configuration)
		{
			this.consoleManager = consoleManager ?? throw new ArgumentNullException(nameof(consoleManager));
            this.ipPortProvider = ipPortProvider ?? throw new ArgumentNullException(nameof(ipPortProvider));
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

		public void SelectSettingAndConnect()
        {

			var sections = configuration.GetChildren().ToList();
			foreach(var section in sections)
			{
				var configIndex = sections.IndexOf(section);
				var keyPath = section["ssh_key_path"];
				var isKeyAccess = File.Exists(keyPath);
				var accessMessage = isKeyAccess ? "Key access" : "PasswordAccess";

				Console.WriteLine($"{++configIndex}. {section.Key} SSH: {section["ssh_user"]}@{section["ssh_server"]}:{section["ssh_port"]} {accessMessage}. " +
					$"RDP: {section["rdp_server"]}:{section["rdp_port"]}");
			}


			string command = "";
            int connectionIndex;
            bool indexParsed;
            do
            {
				Console.Write("Select connection:");
				command = Console.ReadLine();
				Console.WriteLine();

				indexParsed = int.TryParse(command, out connectionIndex);

                if(connectionIndex < 1)
                {
                    indexParsed = false;
                }

            } while(!indexParsed);

            bool useSavedUser = true;
			Console.Write("Use saved user for rdp connection? (y/n) default y: ");
			command = Console.ReadLine();
			if(command == "n")
			{
				useSavedUser = false;
			}

			var connectionSection = sections[--connectionIndex];
			string sshServer = connectionSection["ssh_server"];
			int sshPort = int.Parse(connectionSection["ssh_port"]);
			string sshUser = connectionSection["ssh_user"];
			string sshKeyPath = connectionSection["ssh_key_path"];

			string rdpServer = connectionSection["rdp_server"];
			int rdpPort = int.Parse(connectionSection["rdp_port"]);

			var sshConnectionParam = new SshConnectionParam(sshServer, sshPort, sshUser, sshKeyPath);
			var rdpConnectionParam = new RdpConnectionParam(rdpServer, rdpPort, useSavedUser);

			Connect(sshConnectionParam, rdpConnectionParam);
		}

		private void Connect(SshConnectionParam sshConnectionParam, RdpConnectionParam rdpConnectionParam)
		{
			TrySshConnect(sshConnectionParam);
			if(sshClient == null || !sshClient.IsConnected)
			{
				return;
			}

			var forwardedPort = ForwardRdpPort(rdpConnectionParam);
			if(forwardedPort == null || !forwardedPort.IsStarted)
			{
				return;
			}

			var rdpHost = $"{forwardedPort.BoundHost}:{forwardedPort.BoundPort}";
			var newUserParameter = rdpConnectionParam.UseSavedUser ? "" : " /prompt";
			using var process = new Process();
			process.StartInfo = new ProcessStartInfo
			{
				FileName = "mstsc",
				Arguments = $"/v:{rdpHost}{newUserParameter}"
			};
			process.Start();
			consoleManager.CloseConsole();
			process.WaitForExit();
		}

		private void TrySshConnect(SshConnectionParam sshConnectionParam)
        {
			if(File.Exists(sshConnectionParam.KeyPath))
			{
				PrivateKeyFile privateKeyFile = new PrivateKeyFile(sshConnectionParam.KeyPath);
				var connectionInfo = new PrivateKeyConnectionInfo(sshConnectionParam.Server, sshConnectionParam.Port, sshConnectionParam.User, privateKeyFile);
				connectionInfo.Timeout = TimeSpan.FromSeconds(30);
				ConnectToSshServer(connectionInfo);
			}
			else
			{
				ConnectWithPassword(sshConnectionParam);
			}
		}

		private void ConnectWithPassword(SshConnectionParam sshConnectionParam)
        {
			do
			{
				Console.Write("Enter password for ssh connection:");
				var password = ReadPassword();
				Console.WriteLine();
				var connectionInfo = new PasswordConnectionInfo(sshConnectionParam.Server, sshConnectionParam.Port, sshConnectionParam.User, password);
				ConnectToSshServer(connectionInfo);

			} while(!sshClient.IsConnected);
		}

		private string ReadPassword()
		{
			var password = string.Empty;
			ConsoleKey key;
			do
			{
				var keyInfo = Console.ReadKey(intercept: true);
				key = keyInfo.Key;

				if(key == ConsoleKey.Backspace && password.Length > 0)
				{
					Console.Write("\b \b");
					password = password[0..^1];
				}
				else if(!char.IsControl(keyInfo.KeyChar))
				{
					Console.Write("*");
					password += keyInfo.KeyChar;
				}
			} while(key != ConsoleKey.Enter);

			return password;
		}

		private void ConnectToSshServer(ConnectionInfo connectionInfo)
        {
			sshClient = new SshClient(connectionInfo);

			try
			{
				Console.Write("Connect to SSH server...");
				sshClient.Connect();
				if(sshClient.IsConnected)
				{
					Console.WriteLine(" Connected.");
				}
				else
				{
					Console.WriteLine(" Failed.");
				}
			}
			catch(SshException e)
			{
				var type = e.GetType();
				Console.WriteLine(" SSH connection error: {0}", e.Message);
				return;
			}
			catch(System.Net.Sockets.SocketException e)
			{
				Console.WriteLine(" Socket connection error: {0}", e.Message);
				return;
			}
		}

		private ForwardedPortLocal ForwardRdpPort(RdpConnectionParam rdpConnectionParam)
        {
			var newRdpPort = ipPortProvider.GetRandomFreePort();

			try
			{
				Console.Write("Forwarding RDP port...");
				var forwardedLocalPort = new ForwardedPortLocal(IPAddress.Loopback.ToString(), (uint)newRdpPort, rdpConnectionParam.Server, (uint)rdpConnectionParam.Port);
				sshClient.AddForwardedPort(forwardedLocalPort);
				forwardedLocalPort.Start();

				if(forwardedLocalPort.IsStarted)
				{
					Console.WriteLine($"Forwarded: {forwardedLocalPort}");
				}
				else
				{
					Console.WriteLine("Failed.");
				}
				return forwardedLocalPort;
			}
			catch(SshException e)
			{
				var type = e.GetType();
				Console.WriteLine("SSH connection error: {0}", e.Message);
				return null;
			}
			catch(System.Net.Sockets.SocketException e)
			{
				Console.WriteLine("Socket connection error: {0}", e.Message);
				return null;
			}
		}

		public void Dispose()
		{
			sshClient?.Dispose();
		}
	}
}
