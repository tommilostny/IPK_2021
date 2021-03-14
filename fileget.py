from argparse import ArgumentParser
from os import mkdir
from re import compile
from socket import AF_INET, SOCK_DGRAM, SOCK_STREAM, SocketKind, socket, timeout
from sys import exit, stderr


def parse_arguments():
	parser = ArgumentParser()
	parser.add_argument("-n", "--nameserver", required=True)
	parser.add_argument("-f", "--surl", required=True)
	return parser.parse_args()

def resolve_ipaddress(address:str):
	ip_pattern = compile("^(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}:\d{1,5})$")
	if not ip_pattern.match(address):
		stderr.write("Bad NAMESERVER format format (expected IP:PORT)\n")
		exit(1)
	split = address.split(":")
	ip = split[0]
	port = int(split[1])
	return ip, port

def resolve_surl(surl:str):
	surl_pattern = compile("^(fsp://([a-zA-Z]|-|_|\.)+/.+)$")
	if not surl_pattern.match(surl):
		stderr.write("Bad SURL format format (expected fsp://SERVER_NAME/PATH)\n")
		exit(2)
	split = surl.split("/")
	server = split[2]
	path = str.join("/", split[3:])
	all_files = split[-1] == "*"
	return server, path, all_files

def process_socket(message:str, ip:str, port:int, buffer_size:int, socket_kind:SocketKind, path:str=""):
	with socket(AF_INET, socket_kind) as client_socket:
		client_socket.settimeout(5.0)
		client_socket.connect((ip, port))
		client_socket.sendall(message.encode())
		received_msg = client_socket.recv(buffer_size)

		if socket_kind is SOCK_STREAM:
			msg_split = received_msg.split(b"\r\n")
			success = msg_split[0] == b"FSP/1.0 Success"
			if success:
				success = download_file_data(client_socket, buffer_size, path, start_data=bytes.join(b"\n", msg_split[3:-1]))
			else:
				stderr.write(f"{msg_split[0].decode()}: {path}\n")
		elif socket_kind is SOCK_DGRAM:
			success = received_msg[:2] == b"OK"
		else: success = False

	return received_msg, success

def generate_dir_structure(file_path:str):
	dir_path = ""
	for folder in file_path.split("/")[:-1]:
		dir_path += folder
		try: mkdir(dir_path)
		except FileExistsError: pass
		dir_path += "/"

def download_file_data(socket:socket, buffer_size:int, path:str, start_data:bytes):
	print(f"Downloading {path} ...")
	generate_dir_structure(path)
	done = False
	with open(path, "wb") as file:
		data = start_data
		while not done:
			file.write(data)
			try: data = socket.recv(buffer_size)
			except timeout: break
			if not data: done = True
	if not done:
		stderr.write(f"Error occurred while downloading {path}.\n")
	return done

def whereis_request(server_name:str, ip:str, port:int):
	whereis_message = f"WHEREIS {server_name}\r\n"
	return process_socket(whereis_message, ip, port, buffer_size=256, socket_kind=SOCK_DGRAM)

def get_request(file_path:str, server_name:str, ip:str, port:int):
	get_message = f"GET {file_path} FSP/1.0\r\nHostname: {server_name}\r\nAgent: xmilos02\r\n\r\n"
	_, status = process_socket(get_message, ip, port, buffer_size=4096, socket_kind=SOCK_STREAM, path=file_path)
	return status


args = parse_arguments()
server_ip, server_port = resolve_ipaddress(args.nameserver)
server_name, file_path, collective_download  = resolve_surl(args.surl)

wireq_content, success = whereis_request(server_name, server_ip, server_port)
if success:
	_, server_port = resolve_ipaddress(wireq_content.decode()[3:])
	if collective_download:
		if get_request("index", server_name, server_ip, server_port):
			with open("index", "r") as index:
				for file in index.read().split("\n"):
					if file == "" or not get_request(file, server_name, server_ip, server_port): break
	else:
		get_request(file_path, server_name, server_ip, server_port)
else:
	stderr.write(f"{wireq_content.decode()}: {server_name}\n")
