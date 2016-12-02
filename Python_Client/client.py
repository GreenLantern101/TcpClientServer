
import sys, socket
from time import sleep
import struct
import json

class Client:

    def __init__(self, sock=None):
        if sock is None:
            #INET is IPv4
            self.sock = socket.socket(
                socket.AF_INET, socket.SOCK_STREAM)
        else:
            self.sock = sock

        self.MSGLEN = 64
        print("Socket created.")

    def Run(self):
        self.Connect()
        running = True

        while(running):
            raw = ''
            try:
                raw = self.Receive_String(self.sock)
            except Exception as e:
                print("Error while receiving: " + str(e))
                break

            if raw:
                parsed_raw = json.loads(raw)
                message = parsed_raw['message']
                command = parsed_raw['command']
                if command == 'input':
                    self._handleInput(message)
                else:
                    print(message)

            #sleep in seconds
            sleep(.010)

    def _handleInput(self, query):
        read = input(query)
        message = "{\"command\":\"input\", \"message\":\"" + read + "\"}"
        #print(message_bytes)
        self.Send_String(self.sock, message)

    def Connect(self):
        connected = False;
        while not connected:
            try:
                addr = 'localhost'
                port = 32890
                self.sock.connect(('10.66.178.65', 32890))
                print("Connected to " + addr + " server, at port " + str(port))
                connected = True;
            except:
                print("Failed to connect. Retrying...")
                sleep(3)

    def Send_String(self, sock, string):
        # Prefix each message with message length as 2 bytes (unsigned short)
        str_bytes = string.encode("utf-8")
        toSend = struct.pack('H', len(str_bytes)) + str_bytes
        #print(msg)
        sock.sendall(toSend)

    def Receive_String(self, sock):
        # first two bytes are length of message
        raw_msglen = self.recvall(sock, 2)

        if not raw_msglen:
            return None
        #convert bytes array into an unsigned short
        msglen = struct.unpack('H', raw_msglen)[0]
        # Read the message data
        bytes_data = self.recvall(sock, msglen)

        return bytes_data.decode("utf-8")

    def recvall(self, sock, n):
        # Helper function to recv n bytes or return None if EOF is hit
        bytes_data = b''
        while len(bytes_data) < n:
            packet = sock.recv(n - len(bytes_data))
            if not packet:
                return None
            bytes_data += packet
        return bytes_data

    def Close(self):
        self.sock.shutdown()
        self.sock.close()

if __name__ == '__main__':
    client = Client()
    client.Run()
