
using System;
using System.Net;
using System.Net.Sockets;

namespace AsyncMultithreadClientServer
{
	/// <summary>
	/// Description of IsClientDisconnected.
	/// </summary>
	public static class IsClientDisconnected
	{
		// Checks if a socket has disconnected
		// Adapted from -- http://stackoverflow.com/questions/722240/instantly-detect-client-disconnection-from-server-socket
		private static bool _isDisconnected(TcpClient client)
		{
			try {
				Socket s = client.Client;
				return s.Poll(10 * 1000, SelectMode.SelectRead) && (s.Available == 0);
			} catch (SocketException se) {
				// We got a socket error, assume it's disconnected
				//throw(se);
				return true;
			}
		}
	}
}
