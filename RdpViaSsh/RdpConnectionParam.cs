namespace RdpViaSsh
{
    public class RdpConnectionParam
    {
        public RdpConnectionParam(string server, int port, bool useSavedUser)
        {
            Server = server;
            Port = port;
            UseSavedUser = useSavedUser;
        }

        public string Server { get; }
		public int Port { get; }
        public bool UseSavedUser { get; }
    }
}
