
using System;
using System.Text;
using System.Net.Sockets;
using Newtonsoft.Json;

namespace SyncClientServer
{
	public class Packet
	{
		[JsonProperty("command")]
		public string Command { get; set; }

		[JsonProperty("message")]
		public string Message { get; set; }

		// Makes a packet
		public Packet(string command, string message)
		{
			Command = command;
			Message = message;
		}

		public override string ToString()
		{
			return string.Format(
				"[Packet:\n" +
				"  Command=`{0}`\n" +
				"  Message=`{1}`]",
				Command, Message);
		}

		// Serialize to Json --> TODO: put at end of constructor?
		public string ToJson()
		{
			return JsonConvert.SerializeObject(this);
		}

		// Deserialize
		public static Packet FromJson(string jsonData)
		{
			return JsonConvert.DeserializeObject<Packet>(jsonData);
		}
		
		#region SENDING
		// Sends a packet to a client asynchronously
		public static void SendPacket(NetworkStream stream, Packet packet)
		{
			try {
				byte[] packetBuffer = packet.getPacketBuffer();
				// Send the packet
				stream.Write(packetBuffer, 0, packetBuffer.Length);

			} catch (Exception e) {
				Console.WriteLine("Error sending a packet.");
				Console.WriteLine("Reason: {0}", e.Message);
			}
		}
		public byte[] getPacketBuffer()
		{
			// convert JSON to buffer and its length to a 16 bit unsigned short buffer
			byte[] jsonBuffer = Encoding.UTF8.GetBytes(this.ToJson());
			byte[] lengthBuffer = BitConverter.GetBytes(Convert.ToUInt16(jsonBuffer.Length));

			// Join the buffers
			byte[] packetBuffer = new byte[lengthBuffer.Length + jsonBuffer.Length];
			lengthBuffer.CopyTo(packetBuffer, 0);
			jsonBuffer.CopyTo(packetBuffer, lengthBuffer.Length);
			
			//Console.WriteLine(Encoding.UTF8.GetString(packetBuffer));
				
			return packetBuffer;
		}
		#endregion
		
		#region RECEIVING
		public static Packet getPacketFromStream(NetworkStream _msgStream)
		{
			Packet getfromstream = Packet.getTaskFromStream(_msgStream);
			Packet packet = getfromstream;
			return packet;
		}
		private static Packet getTaskFromStream(NetworkStream _msgStream)
		{
			Packet packet;
			
			// First two bytes are the size of the Packet
			byte[] lengthBuffer = new byte[2];
			_msgStream.Read(lengthBuffer, 0, 2);
			ushort packetByteSize = BitConverter.ToUInt16(lengthBuffer, 0);

			// Remaining bytes in the stream must be the Packet
			byte[] jsonBuffer = new byte[packetByteSize];
			_msgStream.Read(jsonBuffer, 0, jsonBuffer.Length);
			
			//Console.WriteLine(Encoding.UTF8.GetString(lengthBuffer));
			//Console.WriteLine(Encoding.UTF8.GetString(jsonBuffer));
			
			// Convert to packet datatype
			string jsonString = Encoding.UTF8.GetString(jsonBuffer);
			packet = Packet.FromJson(jsonString);
			
			return packet;
		}
		#endregion
	}
}
