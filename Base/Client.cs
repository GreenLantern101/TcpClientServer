﻿
using System;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace AsyncMultithreadClientServer
{
	//is essentially a wrapper over TcpClient
	public class Client
	{
		// Connection objects
		public readonly int Port;
		IPAddress ipAddress_other;
		public TcpClient tcpClient;
		public bool _clientRequestedDisconnect = false;

		// Messaging
		private NetworkStream _msgStream = null;
		private Dictionary<string, Func<string, Task>> _commandHandlers = new Dictionary<string, Func<string, Task>>();

		public Client()
		{
           
			//is the same as localhost: (local ip using "ipconfig" in cmd)
			ipAddress_other = IPAddress.Parse("10.66.178.65");
			
			//because AddressList[0] is IPv6, while server requests IPv4
			tcpClient = new TcpClient(ipAddress_other.AddressFamily);
			
			Port = 32887;
		}

		// Cleans up any leftover network resources
		public void _cleanupNetworkResources()
		{
			//Console.WriteLine("Cleaning up network resources...");
			if (_msgStream != null)
				_msgStream.Close();
			_msgStream = null;
			tcpClient.Close();
		}

		// Connects to the games server
		public void Connect()
		{
			//keep trying to connect to server, once per second
			while (!tcpClient.Connected) {
				// Connect to the server
				try {
					tcpClient.Connect(ipAddress_other, Port);
				} catch (SocketException se) {
					Console.WriteLine("[ERROR] {0}", se.Message);
					Console.WriteLine("Failed to connect. Trying again.");
					Thread.Sleep(3000);
				}
			}

			// check that we've connected
			if (tcpClient.Connected) {
				// Connected!
				Console.WriteLine("Connected to the server at {0}.", tcpClient.Client.RemoteEndPoint);

				// Get the message stream
				_msgStream = tcpClient.GetStream();

				// Hook up packet command handlers
				_commandHandlers["bye"] = _handleBye;
				_commandHandlers["message"] = _handleMessage;
				_commandHandlers["input"] = _handleInput;
			}
		}

		// Requests a disconnect, will send a "bye," message to the server
		// This should only be called by the user
		public void Disconnect()
		{
			Console.WriteLine("Disconnecting from the server...");
			_clientRequestedDisconnect = true;
			Packet.SendPacket(this._msgStream, new Packet("bye")).GetAwaiter().GetResult();
		}

		// Checks for new incoming messages and handles them
		// Handles one Packet at a time, even if more than one is in the memory stream
		public async Task _handleIncomingPackets()
		{
			try {
				// Check for new incoming messages
				if (tcpClient.Available > 0) {
					
					Packet packet = Packet.getPacketFromStream(_msgStream);

					// Dispatch it
					try {
						await _commandHandlers[packet.Command](packet.Message);
					} catch (KeyNotFoundException) {
					}

					//Console.WriteLine("[RECEIVED]\n{0}", packet);
				}
			} catch (Exception) {
			}
		}

		#region Command Handlers
		private Task _handleBye(string message)
		{
			// Print the message
			Console.WriteLine("The server is disconnecting us with this message:");
			Console.WriteLine(message);
			return Task.FromResult(0);  // Task.CompletedTask exists in .NET v4.6
		}

		// Just prints out a message sent from the server
		private Task _handleMessage(string message)
		{
			Console.Write(message);
			return Task.FromResult(0);  // Task.CompletedTask exists in .NET v4.6
		}

		// Gets input from the user and sends it to the server
		private async Task _handleInput(string message)
		{
			// Print the prompt and get a response to send
			Console.Write(message);
			string responseMsg = Console.ReadLine();

			// Send the response
			Packet resp = new Packet("input", responseMsg);
			await Packet.SendPacket(this._msgStream, resp);
		}
		#endregion // Command Handlers
	}
}
