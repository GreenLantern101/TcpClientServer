﻿
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
		
		bool gameEnded = false;
		
		public int numCandies{ get; private set; }

		// Name of the game
		public string Name {
			get { return "Nim"; }
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
		
		public void RemoveCandies(int num)
		{
			numCandies -= num;
		}
		
		/// <summary>
		/// Called directly whenever local user inputs
		/// also called whenever game receives packet (networked player inputs)
		/// </summary>
		public void HandleInputAction(string message)
		{
			int taken;
			if (int.TryParse(message, out taken)) {
				if (taken < 1 || taken > 5 || taken > numCandies)
					Console.WriteLine("Not allowed.");
				else {
						
					this.RemoveCandies(taken);
							
					if (numCandies == 0) {
						gameEnded = true;
						Console.WriteLine("You took the last candy and lose!\n");
					}
				}
			} else {
				Console.WriteLine("Invalid number.\n");
			}
			
			SyncGame_command();
		}
		
		// called to ensure games same in different instances
		// called after every action update
		// command other game to sync with this
		public void SyncGame_command()
		{
			Packet syncPacket = new Packet("sync", this.numCandies.ToString());
			Packet.SendPacket(_player.GetStream(), syncPacket).GetAwaiter().GetResult();
		}
		
		// obey with an order to sync game
		public void SyncGame_obey(string message)
		{
			int num;
			if (int.TryParse(message, out num)) {
				this.numCandies = num;
				Console.WriteLine("\nSYNCED: " + numCandies + " candies left.");
			} else {
				Console.WriteLine("Unable to sync.\n");
			}
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
		
		
		private void PollForInput()
		{
			string message = numCandies + " candies left.\n"
			                 + "How many candies will you take(1-5)? ";
			message="";
			// Poll for input
			Packet inputPacket = new Packet("input", message);
			Packet.SendPacket(_player.GetStream(), inputPacket).GetAwaiter().GetResult();
		}
		

		// Main loop of the Game
		// Packets are sent sent synchronously though
		public void Run()
		{
			PollForInput();
			
			// Make sure we have a player
			bool running = (_player != null);
			if (running) {
				
				// Send a instruction packet
				Packet introPacket = new Packet("message",
					                     "Hi, you may take 1 to 5 candies each turn. " +
					                     "The player who takes the last candy loses.\n");
				Packet.SendPacket(_player.GetStream(), introPacket).GetAwaiter().GetResult();
			} else
				return;
			

			// Some bools for game state
			bool clientConnected = true;
			bool clientDisconnectedGracefully = false;

			// Main game loop
			while (running) {

				// Read their answer
				Packet answerPacket = null;
				while (answerPacket == null) {
					answerPacket = _server.ReceivePacket(_player).GetAwaiter().GetResult();
					Thread.Sleep(10);
				}

				// Cleanup disconnected client
				if (answerPacket.Command == "bye") {
					_server.CleanupClient(_player);
					clientDisconnectedGracefully = true;
				}

				// Check input
				if (answerPacket.Command == "input") {
					Packet responsePacket = new Packet("message");
					responsePacket.Message = "Input action received.";
					
					this.HandleInputAction(answerPacket.Message);

					// Send the message
					Packet.SendPacket(_player.GetStream(), responsePacket).GetAwaiter().GetResult();
				}
				
				//poll for local player input changes
				if (server.client.changed_local) {
					this.HandleInputAction(server.client.action_local);
					//reset flag
					server.client.changed_local = false;
				}
				//poll for local player input changes
				if (server.client.changed_remote) {
					this.SyncGame_obey(server.client.action_remote);
					//reset flag
					server.client.changed_remote = false;
				}

				// Take a small nap
				Thread.Sleep(10);

				// Keep playing until game ends
				running &= !gameEnded;

				// Check for disconnect, may have happened gracefully before
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
