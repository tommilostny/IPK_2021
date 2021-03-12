from argparse import ArgumentParser
from re import compile
from socket import AF_INET, SOCK_DGRAM, socket
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

def send_socket(message: str, ip: str, port: int, buffer_size: int):
	with socket(AF_INET, SOCK_DGRAM) as client_socket:
		client_socket.sendto(bytes(message, "utf-8"), (ip, port))
		received_msg, _ = client_socket.recvfrom(buffer_size)
	return received_msg

args = parse_arguments()
server_ip, server_port = resolve_ipaddress(args.nameserver)
server_name, file_path = resolve_surl(args.surl)

whereis_message = f"WHEREIS {server_name}\r\n"
received = send_socket(whereis_message, server_ip, server_port, buffer_size=256)

if received[:2] == b"OK":
	_, server_port = resolve_ipaddress(str(received)[5:-1])
	get_message = f"GET {file_path} FSP/1.0\r\nHostname: {server_name}\r\nAgent: xmilos02\r\n\r\n"
	
	print(get_message, end="")

	#content = send_socket(get_message, server_ip, server_port, buffer_size=4096)
	#print(content)
else:
	print(str(received)[2:-1] + f': "{server_name}"')
