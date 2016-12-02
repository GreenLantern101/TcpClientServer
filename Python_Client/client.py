
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
            #send works
            tosend = "hi".encode("utf-8")
            print(tosend)
            self.send_msg(self.sock, tosend)
            '''
            NOTE:
            receive works on python end, but on c# server, returns non-fatal error (NEED TO SEND BACK A PACKET?)
            this is because Receive() gets a FIXED size message (64 bytes)? --> need to be variable...
            '''
            try:
                message = self.recv_msg(self.sock)
                if message:
                    print(message.decode("utf-8"))
            except Exception as e:
                print("Error while receiving: " + str(e))

            read = input("How many candies to take (1-5): ")
            message = "{'command':'input', 'message':\"" + read + "\"}"
            message_bytes = message.encode("utf-8")
            #print(message_bytes)
            self.send_msg(self.sock, message_bytes)
            sleep(10)


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
    '''
    def Send(self, msg):
        totalsent = 0
        print(msg)
        while totalsent < self.MSGLEN:
            try:
                sent = self.sock.send(msg[totalsent:])
            except Exception as msg:
                print('Error code: ' + str(msg[0]) + ' , Error message : ' + str(msg[1]))
            if sent == 0:
                print("Unable to send.")
                break
            totalsent = totalsent + sent
    '''

    def send_msg(self, sock, msg):
        # Prefix each message with message length as 2 bytes (unsigned short)
        msg = struct.pack('H', len(msg)) + msg
        print(msg)
        sock.sendall(msg)

    '''
    def Receive(self):
        chunks = []
        bytes_recd = 0
        while bytes_recd < self.MSGLEN:
            chunk = self.sock.recv(min(self.MSGLEN - bytes_recd, 2048))
            if chunk == b'':
                raise RuntimeError("Unable to receive.")
            chunks.append(chunk)
            bytes_recd = bytes_recd + len(chunk)
        return b''.join(chunks)
    '''
    def recv_msg(self, sock):
        # first two bytes are length of message
        raw_msglen = self.recvall(sock, 2)

        if not raw_msglen:
            return None
        print("Raw message length: " + str(raw_msglen))
        #convert bytes array into an unsigned short
        msglen = struct.unpack('H', raw_msglen)[0]
        print("Message length: " + str(msglen))
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