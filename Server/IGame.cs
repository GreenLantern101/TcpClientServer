
using System;
using System.Net.Sockets;

namespace AsyncMultithreadClientServer
{
	/// <summary>
	/// Description of IGame.
	/// </summary>
	public interface IGame
	{
		#region Properties
		// Name of the game
		string Name { get; }

		// How many players are needed to start
		int RequiredPlayers { get; }
		#endregion // Properties

		#region Functions
		// Adds a player to the game (should be before it starts)
		bool AddPlayer(TcpClient player);

		// Tells the server to disconnect a player
		void DisconnectClient(TcpClient client);

		// The main game loop
		void Run();
		#endregion // Functions
	}
}
