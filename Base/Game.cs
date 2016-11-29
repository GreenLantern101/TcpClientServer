
using System;
using System.Net.Sockets;
using System.Threading;

namespace AsyncMultithreadClientServer
{
	public class Game
	{
		// Objects for the game
		private Server _server;
		private TcpClient _player;
		private Random rand;
		private bool _needToDisconnectClient = false;
		
		private int numCandies;

		// Name of the game
		public string Name {
			get { return "Nim"; }
		}

		// Just needs only one player
		public int RequiredPlayers {
			get { return 1; }
		}
                
		// Constructor
		public Game(Server server)
		{
			_server = server;
			rand = new Random();
			
			// Should be [20, 40]
			numCandies = rand.Next(20, 40);
			Console.WriteLine("There are {0} candies.", numCandies);
		}

		// Adds only a single player to the game
		public bool AddPlayer(TcpClient client)
		{
			// Make sure only one player was added
			if (_player == null) {
				_player = client;
				return true;
			}

			return false;
		}

		// If the client who disconnected is ours, we need to quit our game
		public void DisconnectClient(TcpClient client)
		{
			_needToDisconnectClient = (client == _player);
		}

		// Main loop of the Game
		// Packets are sent sent synchronously though
		public void Run()
		{
			// Make sure we have a player
			bool running = (_player != null);
			if (running) {
				// Send a instruction packet
				Packet introPacket = new Packet("message",
					                     "Hi, you may take 1 to 5 candies each turn." +
					                     "The player who takes the last candy loses.\n");
				Packet.SendPacket(_player.GetStream(), introPacket).GetAwaiter().GetResult();
			} else
				return;
			

			// Some bools for game state
			bool gameEnded = false;
			bool clientConnected = true;
			bool clientDisconnectedGracefully = false;

			// Main game loop
			while (running) {
				string message = numCandies + " candies left.\n"
				                 + "How many candies will you take(1-5)? ";
				// Poll for input
				Packet inputPacket = new Packet("input", message);
				Packet.SendPacket(_player.GetStream(), inputPacket).GetAwaiter().GetResult();

				// Read their answer
				Packet answerPacket = null;
				while (answerPacket == null) {
					answerPacket = _server.ReceivePacket(_player).GetAwaiter().GetResult();
					Thread.Sleep(10);
				}

				// Check for graceful disconnect
				if (answerPacket.Command == "bye") {
					_server.HandleDisconnectedClient(_player);
					clientDisconnectedGracefully = true;
				}

				// Check input
				if (answerPacket.Command == "input") {
					Packet responsePacket = new Packet("message");

					int taken;
					if (int.TryParse(answerPacket.Message, out taken)) {
						if (taken < 1 || taken > 5 || taken > numCandies)
							responsePacket.Message = "Not allowed.";
						else {
						
							numCandies -= taken;
							
							if (numCandies == 0) {
								gameEnded = true;
								responsePacket.Message = "You took the last candy and lose!\n";
							}
						}
					} else {
						responsePacket.Message = "Invalid number, try again.\n";
					}

					// Send the message
					Packet.SendPacket(_player.GetStream(), responsePacket).GetAwaiter().GetResult();
				}

				// Take a small nap
				Thread.Sleep(10);

				// Keep playing until game ends
				running &= !gameEnded;

				// Check for disconnect, may have happend gracefully before
				if (!_needToDisconnectClient && !clientDisconnectedGracefully)
					clientConnected &= !Server.IsDisconnected(_player);
				else
					clientConnected = false;
                
				running &= clientConnected;
			}

			// Thank the player and disconnect them
			if (clientConnected)
				_server.DisconnectClient(_player, "Thanks for playing!");
			else
				Console.WriteLine("Client disconnected from game.");

			Console.WriteLine("Ending a \"{0}\" game.", Name);
		}
		
		#region Program Execution
		public static Server server;

		// For when the user Presses Ctrl-C, this will gracefully shutdown the server
		public static void InterruptHandler(object sender, ConsoleCancelEventArgs args)
		{
			args.Cancel = true;
			if (server != null)
				server.Shutdown();
		}

		public static void Main(string[] args)
		{
			// Handler for Ctrl-C presses
			Console.CancelKeyPress += InterruptHandler;
			
			server = new Server();
			server.Start();
		}
		#endregion // Program Execution
	}
}
