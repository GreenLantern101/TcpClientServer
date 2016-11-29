﻿
using System;
using System.Text;
using System.Net.Sockets;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace AsyncMultithreadClientServer
{
	public class Packet
	{
		[JsonProperty("command")]
		public string Command { get; set; }

		[JsonProperty("message")]
		public string Message { get; set; }

		// Makes a packet
		public Packet(string command = "", string message = "")
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
		
		public byte[] getPacketBuffer()
		{
			// convert JSON to buffer and its length to a 16 bit unsigned integer buffer
			byte[] jsonBuffer = Encoding.UTF8.GetBytes(this.ToJson());
			byte[] lengthBuffer = BitConverter.GetBytes(Convert.ToUInt16(jsonBuffer.Length));

			// Join the buffers
			byte[] packetBuffer = new byte[lengthBuffer.Length + jsonBuffer.Length];
			lengthBuffer.CopyTo(packetBuffer, 0);
			jsonBuffer.CopyTo(packetBuffer, lengthBuffer.Length);
				
			return packetBuffer;
		}
		
		
		public static Packet getPacketFromStream(NetworkStream _msgStream)
		{
			Task<Packet> getfromstream = Packet.getTaskFromStream(_msgStream);
			Packet packet = getfromstream.GetAwaiter().GetResult();
			return packet;
		}
		private static async Task<Packet> getTaskFromStream(NetworkStream _msgStream)
		{
			Packet packet;
			
			// There must be some incoming data, the first two bytes are the size of the Packet
			byte[] lengthBuffer = new byte[2];
			await _msgStream.ReadAsync(lengthBuffer, 0, 2);
			ushort packetByteSize = BitConverter.ToUInt16(lengthBuffer, 0);

			// Now read that many bytes from what's left in the stream, it must be the Packet
			byte[] jsonBuffer = new byte[packetByteSize];
			await _msgStream.ReadAsync(jsonBuffer, 0, jsonBuffer.Length);

			// Convert it into a packet datatype
			string jsonString = Encoding.UTF8.GetString(jsonBuffer);
			packet = Packet.FromJson(jsonString);
			
			return packet;
		}
	}
	
	//PRE-PROCESSING needed:
	/*Call the ToJson() method on the Packet to get it as a string.  
	Then after that, encode it (in UTF-8) into a byte array
	Get how many bytes are in that first byte array, 
	then encode that number as an unsigned 16 bit integer into a second byte array.  
	The resulting array should be exactly two bytes long
	Join those two arrays into a new one with the "length buffer," ahead of the "JSON buffer,"
	*/

	
	
}
