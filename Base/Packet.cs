
using System;
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
