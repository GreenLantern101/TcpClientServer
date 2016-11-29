
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
		private Random _rng;
		private bool _needToDisconnectClient = false;

		// Name of the game
		public string Name {
			get { return "Guess My Number"; }
		}

		// Just needs only one player
		public int RequiredPlayers {
			get { return 1; }
		}
                
		// Constructor
		public Game(Server server)
		{
			_server = server;
			_rng = new Random();
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
					                                 "Welcome player, I want you to guess my number.\n" +
					                                 "It's somewhere between (and including) 1 and 100.\n");
				_server.SendPacket(_player, introPacket).GetAwaiter().GetResult();
			} else
				return;

			// Should be [1, 100]
			int theNumber = _rng.Next(1, 101);
			Console.WriteLine("Our number is: {0}", theNumber);

			// Some bools for game state
			bool correct = false;
			bool clientConnected = true;
			bool clientDisconnectedGracefully = false;

			// Main game loop
			while (running) {
				// Poll for input
				Packet inputPacket = new Packet("input", "Your guess: ");
				_server.SendPacket(_player, inputPacket).GetAwaiter().GetResult();

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

					int theirGuess;
					if (int.TryParse(answerPacket.Message, out theirGuess)) {

						// See if they won
						if (theirGuess == theNumber) {
							correct = true;
							responsePacket.Message = "Correct!  You win!\n";
						} else if (theirGuess < theNumber)
							responsePacket.Message = "Too low.\n";
						else if (theirGuess > theNumber)
							responsePacket.Message = "Too high.\n";
					} else
						responsePacket.Message = "That wasn't a valid number, try again.\n";

					// Send the message
					_server.SendPacket(_player, responsePacket).GetAwaiter().GetResult();
				}

				// Take a small nap
				Thread.Sleep(10);

				// If they aren't correct, keep them here
				running &= !correct;

				// Check for disconnect, may have happend gracefully before
				if (!_needToDisconnectClient && !clientDisconnectedGracefully)
					clientConnected &= !Server.IsDisconnected(_player);
				else
					clientConnected = false;
                
				running &= clientConnected;
			}

			// Thank the player and disconnect them
			if (clientConnected)
				_server.DisconnectClient(_player, "Thanks for playing \"Guess My Number\"!");
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
