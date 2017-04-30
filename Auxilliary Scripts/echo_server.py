# echo_server.py
import socket

def OpenSocket():
	host = ''        # Symbolic name meaning all available interfaces
	port = 8888      # Arbitrary non-privileged port
	s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
	s.bind((host, port))
	s.listen(1)
	
	print 'Listening on port ' + str(port) + '...'
	conn, addr = s.accept()
	
	print('Connected by', addr)
	while True:
		data = conn.recv(1024)
		if not data:
			break
		conn.sendall('RX ' + data)
		print 'sending {' + data + '}'
	conn.close()


def main():
	
	while(1):
		OpenSocket()


if __name__ == "__main__":
	main()