namespace RdpViaSsh
{
    public class SshConnectionParam
    {
        public SshConnectionParam(string server, int port, string user, string keyPath)
        {
            Server = server;
            Port = port;
            User = user;
            KeyPath = keyPath;
        }

		public string Server { get; }
		public int Port { get; }
		public string User { get; }
		public string KeyPath { get; }
	}
}
