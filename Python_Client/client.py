
import sys, socket
from time import sleep
import struct

'''
simple TCP client (sends/receives fixed messages only)
to use for variable messages: send message length first, use flush()?, need receive loop
'''

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
            message_bytes = ''
            try:
                message_bytes = self.recv_msg(self.sock)
            except Exception as e:
                print("Error while receiving: " + str(e))
                break

            if message_bytes:
                message = message_bytes.decode("utf-8");
                print(message)
                if message.find("input") != -1:
                    self._handleInput()





            #sleep in seconds
            sleep(.010)

    def _handleInput(self):
        read = input("How many candies to take (1-5): ")
        message = "{\"command\":\"input\", \"message\":\"" + read + "\"}"
        message_bytes = message.encode("utf-8")
        #print(message_bytes)
        self.send_msg(self.sock, message_bytes)

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
                print("attempting to connect...")
                sleep(3)
                print("connected = " + str(connected))

    def send_msg(self, sock, msg):
        # Prefix each message with message length as 2 bytes (unsigned short)
        msg = struct.pack('H', len(msg)) + msg
        #print(msg)
        sock.sendall(msg)

    def recv_msg(self, sock):
        # first two bytes are length of message
        raw_msglen = self.recvall(sock, 2)

        if not raw_msglen:
            return None
        #print("Raw message length: " + str(raw_msglen))
        #convert bytes array into an unsigned short
        msglen = struct.unpack('H', raw_msglen)[0]
        #print("Message length: " + str(msglen))
        # Read the message data
        return self.recvall(sock, msglen)

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
