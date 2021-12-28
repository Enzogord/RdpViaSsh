using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;

namespace RdpViaSsh
{
    public class IpPortProvider
    {
		private bool IsFree(int port)
		{
			IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
			IPEndPoint[] listeners = properties.GetActiveTcpListeners();
			int[] openPorts = listeners.Select(item => item.Port).ToArray();
			return openPorts.All(openPort => openPort != port);
		}

		public int GetRandomFreePort(int port = 0)
		{
			port = (port > 0) ? port : new Random().Next(1, 65535);
			while(!IsFree(port))
			{
				port += 1;
			}
			return port;
		}
	}
}
