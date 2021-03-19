# fileget
#	autor: Tomáš Milostný (xmilos02)
#	verze Python: 3.8.5

from argparse import ArgumentParser
from os import mkdir
from re import compile
from socket import AF_INET, SocketKind, gaierror, socket, timeout
from sys import stderr


def parse_arguments():
	parser = ArgumentParser()
	parser.add_argument("-n", "--nameserver", required=True)
	parser.add_argument("-f", "--surl", required=True)
	return parser.parse_args()

def resolve_ipaddress(address:str):
	ip_pattern = compile("^(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}:\d{1,5})$")
	if not ip_pattern.match(address):
		stderr.write("Bad NAMESERVER format format (expected IP:PORT)\n")
		return None, None, False
	split = address.split(":")
	ip = split[0]
	port = int(split[1])
	return ip, port, True

def resolve_surl(surl:str):
	surl_pattern = compile("^(fsp://([a-zA-Z]|-|_|\.)+/.+)$")
	if not surl_pattern.match(surl):
		stderr.write("Bad SURL format format (expected fsp://SERVER_NAME/PATH)\n")
		return None, None, None, False
	split = surl.split("/")
	server = split[2]
	path = str.join("/", split[3:])
	all_files = split[-1] == "*"
	return server, path, all_files, True

def process_socket(message:str, ip:str, port:int, socket_kind:SocketKind, buffer_size:int, path:str=""):
	with socket(AF_INET, socket_kind) as client_socket:
		client_socket.settimeout(5.0)
		try:
			client_socket.connect((ip, port))
			client_socket.sendall(message.encode())
			received_msg = client_socket.recv(buffer_size)
		except timeout:
			return b"Connection timed out.\n", False
		except gaierror as e:
			return ip.encode() + b": " + e.strerror.encode() + b"\n", False

		if socket_kind is SocketKind.SOCK_STREAM:
			msg_split = received_msg.split(b"\r\n")
			success = msg_split[0] == b"FSP/1.0 Success"
			if success:
				already_received_data = bytes.join(b"\n", msg_split[3:])
				success = download_file_data(client_socket, buffer_size, path, already_received_data)
			else:
				received_msg = msg_split[0] + b": " + path.encode() + b"\n"
		elif socket_kind is SocketKind.SOCK_DGRAM:
			success = received_msg[:2] == b"OK"
			if not success: received_msg = server_name.encode() + b": " + received_msg + b"\n"
		else: success = False

	return received_msg, success

def generate_dir_structure(file_path:str):
	path_part = ""
	for dir in file_path.split("/")[:-1]:
		path_part += dir
		try: mkdir(path_part)
		except FileExistsError: pass
		path_part += "/"

def download_file_data(socket:socket, buffer_size:int, path:str, start_data:bytes):
	print(f"Downloading {path} ...")
	generate_dir_structure(path)
	with open(path, "wb") as file:
		done = False
		data = start_data
		while not done:
			file.write(data)
			try: data = socket.recv(buffer_size)
			except timeout: break
			done = not data
	if not done:
		stderr.write(f"Error occurred while downloading {path}.\n")
	return done

def whereis_request(server_name:str, ip:str, port:int):
	whereis_message = f"WHEREIS {server_name}\r\n"
	content, success = process_socket(whereis_message, ip, port, SocketKind.SOCK_DGRAM, buffer_size=256)
	if not success:
		stderr.write(content.decode())
	return content, success

def get_request(file_path:str, server_name:str, ip:str, port:int, replace_in_path:str=None):
	get_message = f"GET {file_path} FSP/1.0\r\nHostname: {server_name}\r\nAgent: xmilos02\r\n\r\n"
	if replace_in_path is not None:
		file_path = file_path.replace(replace_in_path, "")
	content, success = process_socket(get_message, ip, port, SocketKind.SOCK_STREAM, buffer_size=4096, path=file_path)
	if not success:
		stderr.write(content.decode())
	return success

def get_all_request(file_path:str, server_name:str, ip:str, port:int):
	file_path = file_path[:-1]
	if get_request("index", server_name, ip, port):
		with open("index", "r") as index:
			for file in index.read().split("\n"):
				if not file_path in file: continue
				if file == "" or not get_request(file, server_name, ip, port, file_path): break


args = parse_arguments()
server_ip, server_port, ok_ip = resolve_ipaddress(args.nameserver)
server_name, file_path, collective_download, ok_surl  = resolve_surl(args.surl)

if ok_ip and ok_surl:
	wireq_content, wireq_success = whereis_request(server_name, server_ip, server_port)
	if wireq_success:
		_, server_port, _ = resolve_ipaddress(wireq_content.decode()[3:])
		if collective_download:
			get_all_request(file_path, server_name, server_ip, server_port)
		else:
			get_request(file_path, server_name, server_ip, server_port)
