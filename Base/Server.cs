
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncMultithreadClientServer
{
	public class Server
	{
		// Listens for new incoming connections
		private TcpListener _listener;
		
		private IPAddress ip_me;

		// Clients objects
		public Client client;
		private TcpClient _tcpClient = null;

		// Game stuff
		private Thread gameThread = null;
		private Game _currentGame = null;

		// Other data
		public readonly string Name;
		public readonly int Port;
		public bool Running { get; private set; }

		// Construct to create a new Games Server
		public Server()
		{
			// Set some of the basic data
			Name = "SERVER_ONE";
			Port = 32887;
			Running = false;
			
			// Create the listener, listening at any ip address
			_listener = new TcpListener(IPAddress.Any, Port);
		}
		
		// returns the private network IP address of server
		public static IPAddress GetLocalIPAddress()
		{
			var host = Dns.GetHostEntry(Dns.GetHostName());
			foreach (var ip in host.AddressList) {
				if (ip.AddressFamily == AddressFamily.InterNetwork) {
					Console.WriteLine("IP: " + ip.ToString());
					return ip;
				}
			}
			throw new Exception("Local IP Address Not Found!");
		}

		// Shutdown the server if its running
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
		public void Start()
		{
			//------------------------------------------------ start server
			Console.WriteLine("Starting the \"{0}\" server on port {1}.", Name, Port); 
			Console.WriteLine("Press Ctrl-C to shutdown the server at any time.");
			
			// Start running the server
			_listener.Start();
			Running = true;
			Console.WriteLine("Waiting for incoming connections...");

			//------------------------------------------------- start client
			client = new Client();
			//connect game client...
			client.Connect();
			//------------------------------------------------- run server & client
			this.Run();
		}

		// The main loop for the games server
		public void Run()
		{
			//server vars
			List<Task> newConnectionTasks = new List<Task>();
			
			//client vars
			bool wasRunning = Running;
			List<Task> messagetasks = new List<Task>();
			
			while (Running) {
				//------------------------------------------------- server run cycle
				bool newconnection = false;
				// Handle any new clients
				if (_listener.Pending()) {
					newConnectionTasks.Add(_handleNewConnection());
					newconnection = true;
				}
					
				//Start a game for the first new connection
				if (newconnection) {
					//start new game
					_currentGame = new Game(this);
					//add networked player to game
					_currentGame.AddPlayer(_tcpClient);

					// Start the game in a new thread!
					Console.WriteLine("Starting a \"{0}\" game.", _currentGame.Name);
					this.gameThread = new Thread(new ThreadStart(_currentGame.Run));
					gameThread.Start();

					// Create a new game
					_currentGame = new Game(this);
				}
				
				//------------------------------------------------- client run cycle
				
				// Check for new packets
				messagetasks.Add(this.client._handleIncomingPackets());

				// Make sure that we didn't have a graceless disconnect
				if (IsDisconnected(this.client.tcpClient) 
				    && !this.client._clientRequestedDisconnect) {
					Running = false;
					Console.WriteLine("The server has disconnected from us ungracefully.");
					Thread.Sleep(3000);
				}
				
				
				//--------------------------------------------------- Take a small nap
				Thread.Sleep(10);
			}

			//-------------------------------------------------------- server STOP
			// If client connected after loop exited, allow 1 second to finish task.
			Task.WaitAll(newConnectionTasks.ToArray(), 1000);

			// Shutdown all of the threads, regardless if they are done or not
			if (gameThread != null)
				gameThread.Abort();

			// Disconnect any clients remaining
			if (_tcpClient != null && _tcpClient.Connected)
				DisconnectClient(_tcpClient, "The server is shutting down.");

			// Cleanup our resources
			_listener.Stop();

			// Info
			Console.WriteLine("The server has been shut down.");
			
			//-------------------------------------------------------- client STOP
			
			// Just incase we have anymore packets, give them one second to be processed
			Task.WaitAll(messagetasks.ToArray(), 1000);

			// Cleanup
			this.client._cleanupNetworkResources();
		}

		// Awaits for a new connection, sets it to networked client
		private async Task _handleNewConnection()
		{
			// Get the new client using a Future
			this._tcpClient = await _listener.AcceptTcpClientAsync();
			Console.WriteLine("New connection from {0}.", _tcpClient.Client.RemoteEndPoint);

			// Send a welcome message
			string msg = String.Format("Welcome to the \"{0}\" server.\n", Name);
			await Packet.SendPacket(_tcpClient.GetStream(), new Packet("message", msg));
		}

		// Will attempt to gracefully disconnect a TcpClient
		// used for in-game clients
		public void DisconnectClient(TcpClient client, string message = "")
		{
			Console.WriteLine("Disconnecting the client from {0}.", client.Client.RemoteEndPoint);

			// If there wasn't a message set, use the default "Goodbye."
			if (message == "")
				message = "Goodbye.";

			// Send the "bye," message
			Task byePacket = Packet.SendPacket(client.GetStream(), new Packet("bye", message));

			// Give the client some time to send and proccess the graceful disconnect
			Thread.Sleep(2000);

			// Cleanup resources on our end
			byePacket.GetAwaiter().GetResult();
			HandleDisconnectedClient(client);
		}

		// Cleans up the resources if a client has disconnected,
		// gracefully or not.
		public void HandleDisconnectedClient(TcpClient client)
		{
			_cleanupClient(client);
			//this._tcpClient = null;
			
		}
		// cleans up resources for a TcpClient and closes it
		private static void _cleanupClient(TcpClient client)
		{
			client.GetStream().Close();     // Close network stream
			client.Close();                 // Close client
		}

		#region Packet Transmission Methods
		

		// Will get a single packet from a TcpClient
		// Will return null if there isn't any data available or some other
		// issue getting data from the client
		public async Task<Packet> ReceivePacket(TcpClient client)
		{
			Packet packet = null;
			try {
				// First check there is data available
				if (client.Available == 0)
					return null;

				NetworkStream _msgStream = client.GetStream();
				packet = Packet.getPacketFromStream(_msgStream);

				//Console.WriteLine("[RECEIVED]\n{0}", packet);
			} catch (Exception e) {
				// There was an issue in receiving
				Console.WriteLine("There was an issue receiving a packet from {0}.", client.Client.RemoteEndPoint);
				Console.WriteLine("Reason: {0}", e.Message);
			}

			return packet;
		}
		#endregion // Packet Transmission Methods

		#region TcpClient Helper Methods
		// Checks if a client has disconnected ungracefully
		// Adapted from: http://stackoverflow.com/questions/722240/instantly-detect-client-disconnection-from-server-socket
		public static bool IsDisconnected(TcpClient client)
		{
			try {
				Socket s = client.Client;
				return s.Poll(10 * 1000, SelectMode.SelectRead) && (s.Available == 0);
			} catch (SocketException) {
				// We got a socket error, assume it's disconnected
				return true;
			}
		}
		
		#endregion // TcpClient Helper Methods





		
	}
}
