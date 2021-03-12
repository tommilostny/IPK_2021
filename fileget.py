import socket, argparse, sys, re

def parse_arguments():
	parser = argparse.ArgumentParser()
	parser.add_argument("-n", "--nameserver", required=True)
	parser.add_argument("-f", "--surl", required=True)
	return parser.parse_args()

def resolve_ipaddress(address: str):
	ip_pattern = re.compile("^(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}:\d{1,4})$")
	if not ip_pattern.match(address):
		print('Bad NAMESERVER format format "IP:PORT"')
		sys.exit(1)
	split = address.split(":")
	ip = split[0]
	port = int(split[1])
	return ip, port

def resolve_surl(surl: str):
	surl_pattern = re.compile("^(ftp://.+/.+)$");
	if not surl_pattern.match(surl):
		print('Bad SURL format format "ftp://SERVERNAME/FILE"')
		sys.exit(2)
	split = surl.split("/")
	server = split[2]
	path = "/".join(split[3:])
	return server, path

args = parse_arguments()
server_ip, server_port = resolve_ipaddress(args.nameserver)
server_name, file_path = resolve_surl(args.surl)

with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as client_socket:
	message = bytes(f"WHEREIS {server_name}\n", "utf-8")
	client_socket.sendto(message, (server_ip, server_port))
	received_msg, address = client_socket.recvfrom(2048)

print(f"Message is: {received_msg}")
