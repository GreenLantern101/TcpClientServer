
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SyncClientServer
{
	public class Server
	{
		// Listens for new incoming connections
		private TcpListener tcpListener;
		
		// Clients objects
		public Client client;
		//tcp client object of whatever is connecting to this server
		private TcpClient tcpClient_other = null;

		// Game stuff
		private Thread gameThread = null;

		// Other data
		public readonly string Name;
		public readonly int port_me;
		public bool Running { get; private set; }

		public Server()
		{
			Name = "SERVER_ONE";
			port_me = 32887;
			Running = false;
			
			// Create the listener, listening at any ip address
			tcpListener = new TcpListener(IPAddress.Any, port_me);
			
			//might need to use something like this to make public host
			//tcpListener = new TcpListener(IPAddress.Parse(Dns.GetHostName()), port_me);
		}
		
		// returns the private network IP address of server
		public static IPAddress GetLocalIPAddress()
		{
			var host = Dns.GetHostEntry(Dns.GetHostName());
			foreach (var ip in host.AddressList) {
				if (ip.AddressFamily == AddressFamily.InterNetwork) {
					Console.WriteLine("IP: " + ip);
					return ip;
				}
			}
			throw new Exception("Local IP Address not found.");
		}

		public void Shutdown()
		{
			if (Running) {
				Running = false;
				Console.WriteLine("Shutting down server...");
			}
			// gracefully disconnect client...
			if (client != null)
				client.Disconnect();
		}
		public void Start(Game game)
		{
			//------------------------------------------------ start server
			Console.WriteLine("Starting the \"{0}\" server on port {1}.", Name, port_me); 
			Console.WriteLine("Press Ctrl-C to shutdown the server at any time.");
			
			// Start running the server
			tcpListener.Start();
			Running = true;
			Console.WriteLine("Waiting for incoming connections...");

			//------------------------------------------------- start client
			/*
			client = new Client();
			//connect game client...
			client.Connect();
			*/
			//------------------------------------------------- run server & client
			this.Run(game);
		}

		public void Run(Game _currentGame)
		{
			
			while (Running) {
				//------------------------------------------------- server run cycle
				bool newconnection = false;
				// Handle any new clients
				if (tcpListener.Pending()) {
					_handleNewConnection();
					newconnection = true;
				}
					
				//Start a game for the first new connection
				if (newconnection) {
					//add networked player to game
					_currentGame.AddPlayer(tcpClient_other);

					// Start the game in a new thread!
					Console.WriteLine("Starting a \"{0}\" game.", _currentGame.Name);
					this.gameThread = new Thread(new ThreadStart(_currentGame.Run));
					gameThread.Start();
					
					
					//SYNC GAME AT BEGINNING immediately after connecting
					_currentGame.SyncGame_command();

				}
				
				//------------------------------------------------- client run cycle
				/*
				// Check for new packets
				client._handleIncomingPackets();
				
				//poll for local player input changes
				if (client.changed_local) {
					_currentGame.HandleInputAction(client.action_local);
					//reset flag
					client.changed_local = false;
				}
				//poll for remote player input changes
				if (client.changed_remote) {
					_currentGame.SyncGame_obey(client.action_remote);
					//reset flag
					client.changed_remote = false;
				}

				// Make sure that we didn't have a graceless disconnect
				if (IsDisconnected(this.client.tcpClient) 
				    && !this.client._clientRequestedDisconnect) {
					Running = false;
					Console.WriteLine("Other server disconnected from us ungracefully.");
					Thread.Sleep(3000);
				}
				*/
				
				//--------------------------------------------------- Take a small nap
				Thread.Sleep(10);
			}

			//-------------------------------------------------------- server STOP

			// Shutdown all of the threads, regardless if they are done or not
			if (gameThread != null)
				gameThread.Abort();

			// Disconnect any clients remaining
			if (tcpClient_other != null && tcpClient_other.Connected)
				DisconnectClient(tcpClient_other, "Other server is shutting down.");

			// Cleanup our resources
			tcpListener.Stop();

			// Info
			Console.WriteLine("The server has been shut down.");
			
			//-------------------------------------------------------- client STOP

			// Cleanup
			this.client._cleanupNetworkResources();
		}

		// Awaits for a new connection, sets it to networked client
		private void _handleNewConnection()
		{
			// Get the new client using a Future
			this.tcpClient_other = tcpListener.AcceptTcpClient();
			Console.WriteLine("New connection from {0}.", tcpClient_other.Client.RemoteEndPoint);

			// Send a welcome message
			string msg = String.Format("Welcome to the \"{0}\" server.\n", Name);
			Packet.SendPacket(tcpClient_other.GetStream(), new Packet("message", msg));
		}

		// Checks if a client has disconnected ungracefully
		public static bool IsDisconnected(TcpClient client)
		{
			try {
				Socket s = client.Client;
				return s.Poll(10 * 1000, SelectMode.SelectRead) && (s.Available == 0);
			} catch (SocketException) {
				// If socket error, assume it's disconnected
				return true;
			}
		}
		// Gracefully disconnect a TcpClient
		public void DisconnectClient(TcpClient client, string message = "")
		{
			Console.WriteLine("Disconnecting the client from {0}.", client.Client.RemoteEndPoint);

			// If no message set, use the default "Goodbye."
			if (message == "")
				message = "Goodbye.";

			// Send the "bye" message
			Packet.SendPacket(client.GetStream(), new Packet("bye", message));

			// Give the client some time to send and process the graceful disconnect
			Thread.Sleep(500);

			CleanupClient(client);
		}
		
		// cleans up resources for a TcpClient and closes it
		public void CleanupClient(TcpClient client)
		{
			client.GetStream().Close();     // Close network stream
			client.Close();                 // Close client
			//this.tcpClient_other = null;
		}

		// Will get a single packet from a TcpClient
		// Returns null if no data available or issue
		public Packet ReceivePacket(TcpClient client)
		{
			Packet packet = null;
			try {
				// First check there is data available
				if (client.Available == 0)
					return null;

				NetworkStream _msgStream = client.GetStream();
				packet = Packet.getPacketFromStream(_msgStream);

			} catch (Exception e) {
				// There was an issue in receiving
				Console.WriteLine("There was an issue receiving a packet from {0}.", client.Client.RemoteEndPoint);
				Console.WriteLine("Reason: {0}", e.Message);
			}

			return packet;
		}

	}
}
