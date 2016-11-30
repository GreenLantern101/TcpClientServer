﻿
using System;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace AsyncMultithreadClientServer
{
	//essentially a wrapper over TcpClient
	public class Client
	{
		// Connection objects
		public TcpClient tcpClient;
		private IPAddress ipAddress_other;
		private int Port;
		public bool _clientRequestedDisconnect = false;

		// Messaging
		private NetworkStream _msgStream = null;
		private Dictionary<string, Func<string, Task>> _commandHandlers = new Dictionary<string, Func<string, Task>>();

		public Client()
		{
			tcpClient = new TcpClient(AddressFamily.InterNetwork);
			
			//ip address and port of opposing server should be put in "config.txt"
			string[] lines = File.ReadAllLines(Directory.GetCurrentDirectory() + "/../../config.txt");	
			ipAddress_other = IPAddress.Parse(lines[0]);
			Port = int.Parse(lines[1]);
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
				Console.WriteLine("Connected to server at {0}.", tcpClient.Client.RemoteEndPoint);

				// Get the message stream
				_msgStream = tcpClient.GetStream();

				// Hook up packet command handlers
				_commandHandlers["bye"] = _handleBye;
				_commandHandlers["message"] = _handleMessage;
				_commandHandlers["input"] = _handleInput;
			}
		}

		// Requests a disconnect, will send "bye" message
		// This should only be called by the user
		public void Disconnect()
		{
			Console.WriteLine("Disconnecting...");
			_clientRequestedDisconnect = true;
			Packet.SendPacket(this._msgStream, new Packet("bye")).GetAwaiter().GetResult();
		}
		public void _cleanupNetworkResources()
		{
			//Console.WriteLine("Cleaning up network resources...");
			if (_msgStream != null)
				_msgStream.Close();
			_msgStream = null;
			tcpClient.Close();
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
				}
			} catch (Exception) {
			}
		}

		#region Command Handlers
		private Task _handleBye(string message)
		{
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
