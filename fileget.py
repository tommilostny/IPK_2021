from argparse import ArgumentParser
from re import compile
from socket import AF_INET, SOCK_DGRAM, SOCK_STREAM, SocketKind, socket, timeout
from sys import exit


def parse_arguments():
	parser = ArgumentParser()
	parser.add_argument("-n", "--nameserver", required=True)
	parser.add_argument("-f", "--surl", required=True)
	return parser.parse_args()

def resolve_ipaddress(address: str):
	ip_pattern = compile("^(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}:\d{1,5})$")
	if not ip_pattern.match(address):
		print('Bad NAMESERVER format format "IP:PORT"')
		exit(1)
	split = address.split(":")
	ip = split[0]
	port = int(split[1])
	return ip, port

def resolve_surl(surl: str):
	surl_pattern = compile("^(fsp://.+/.+)$")
	if not surl_pattern.match(surl):
		print('Bad SURL format format "fsp://SERVERNAME/FILE"')
		exit(2)
	split = surl.split("/")
	server = split[2]
	path = str.join("/", split[3:])
	return server, path

def send_socket(message: str, ip: str, port: int, buffer_size: int, socket_kind: SocketKind):
	with socket(AF_INET, socket_kind) as client_socket:
		client_socket.settimeout(5.0)
		client_socket.connect((ip, port))
		client_socket.sendall(message.encode())
		received_msg = client_socket.recv(buffer_size)

		while socket_kind is SOCK_STREAM: #loading data only from stream socket
			try:
				data = client_socket.recv(buffer_size)
			except timeout: break
			if not data: break
			received_msg += data

	return received_msg.decode()

args = parse_arguments()
server_ip, server_port = resolve_ipaddress(args.nameserver)
server_name, file_path = resolve_surl(args.surl)

whereis_message = f"WHEREIS {server_name}\r\n"
received = send_socket(whereis_message, server_ip, server_port, buffer_size=256, socket_kind=SOCK_DGRAM)

if received[:2] == "OK":
	_, server_port = resolve_ipaddress(received[3:])
	get_message = f"GET {file_path} FSP/1.0\r\nHostname: {server_name}\r\nAgent: xmilos02\r\n\r\n"
	received = send_socket(get_message, server_ip, server_port, buffer_size=4096, socket_kind=SOCK_STREAM)
	print(received)
else:
	print(f'{received}: "{server_name}"')
