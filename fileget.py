# fileget
#	autor: Tomáš Milostný (xmilos02)

from argparse import ArgumentParser
from os import mkdir
from re import compile
from socket import AF_INET, SOCK_DGRAM, SOCK_STREAM, inet_pton, socket, timeout
from sys import stderr
from typing import Tuple, Union


def parse_arguments():
	parser = ArgumentParser()
	parser.add_argument("-n", "--nameserver", required=True)
	parser.add_argument("-f", "--surl", required=True)
	return parser.parse_args()


def resolve_ipaddress(address:str) -> Tuple[str, int, bool]:
	"""	Check and split nameserver IPv4 address.
		Returns tuple in format (ip, port, status(everything ok)).
	"""
	parts = address.split(":")
	ip = parts[0]
	try:
		inet_pton(AF_INET, ip)
		port = int(parts[1])
		return ip, port, port >= 0 and port <= 65535

	except OSError as e: #illegal ip address
		stderr.write(f"{e}: {ip}\n")

	except IndexError: #missing port
		stderr.write(f"Bad NAMESERVER format (expected IPv4:PORT, got {address}).\n")
	return None, None, False


def resolve_surl(surl:str) -> Tuple[str, str, bool, bool]:
	"""	Check and split fsp SURL.
		Returns tuple in format (server_name, file_path, collective download (*)?, status)
	"""
	surl_pattern = compile("^(fsp://(\w|-|_|\.)+/(\w|-|_|\.|\*|/)+)$")
	if not surl_pattern.match(surl):
		stderr.write("Bad SURL format (expected fsp://SERVER_NAME/PATH)\n")
		return None, None, None, False

	parts = surl.split("/")
	return parts[2], str.join("/", parts[3:]), parts[-1] == "*", True


def process_socket(message:str, address:Tuple[str, int], buffer_size:int, path:str="") -> Tuple[bytes, bool]:
	"""	Creates and processes a FSP or NSP socket.
		Returns tuple in format (message from the server, status).
		GET request returns only the first message; full message is downloaded into a file.
	"""
	socket_kinds = { "GET": SOCK_STREAM, "WHEREIS": SOCK_DGRAM }
	request_type = message.split(" ")[0]

	if request_type not in socket_kinds.keys():
		return b"Invalid request.\n", False

	with socket(AF_INET, socket_kinds[request_type]) as client_socket:
		client_socket.settimeout(30.0)
		try:
			client_socket.connect(address)
			client_socket.sendall(message.encode())
			received_msg = client_socket.recv(buffer_size)
		except timeout:
			return b"Connection timed out.\n", False

		if request_type == "GET":
			msg_split = received_msg.split(b"\r\n")
			success = msg_split[0] == b"FSP/1.0 Success"
			if success:
				already_received_data = bytes.join(b"\r\n", msg_split[3:])
				length = int(msg_split[1].split(b":")[1])
				success = download_file_data(client_socket, buffer_size, path, already_received_data, length)
			else:
				received_msg = msg_split[0] + b": " + path.encode() + b"\n"

		elif request_type == "WHEREIS":
			success = received_msg[:2] == b"OK"
			if not success: received_msg = received_msg + b": " + server_name.encode() + b"\n"

	return received_msg, success


def generate_dir_structure(file_path:str):
	""" Creates directories structure specified in the UNIX style full FILE path. """
	path_part = ""
	for dir in file_path.split("/")[:-1]:
		path_part += dir
		try: mkdir(path_part)
		except FileExistsError: pass
		path_part += "/"


def download_file_data(socket:socket, buffer_size:int, path:str, start_data:bytes, total_data_length:int) -> bool:
	"""	Downloads file from established stream GET socket and prints the process to stdout.
		Requires start data, that could appear in the first message.
		Returns true if the whole file was downloaded correctly.
	"""
	generate_dir_structure(path)
	with open(path, "wb") as file:
		done = False
		data = start_data
		downloaded = len(start_data)
		while not done:
			print(f"Downloading {path} ... {round(downloaded / total_data_length * 100.0, 2)}%", end="\r")
			file.write(data)
			if downloaded != total_data_length:
				try: data = socket.recv(buffer_size)
				except timeout: break
				downloaded += len(data)
			else:
				done = True
	print()
	if not done:
		stderr.write(f"Error occurred while downloading {path}.\n")
	return done


def whereis_request(server_name:str, ip:str, port:int) -> Union[str, None]:
	"""	WHEREIS Name Service Protocol request.
		Returns result IP:PORT addess as string or None on error.
	"""
	whereis_message = f"WHEREIS {server_name}\r\n"
	content, success = process_socket(whereis_message, (ip, port), buffer_size=256)
	if not success:
		stderr.write(content.decode())
		return None
	return content.decode()[3:]


def get_request(file_path:str, server_name:str, ip:str, port:int, replace_in_path:str=None) -> bool:
	""" GET File Service Protocol request.
		Returns true if the request succeeded.
	"""
	get_message = f"GET {file_path} FSP/1.0\r\nHostname: {server_name}\r\nAgent: xmilos02\r\n\r\n"
	if replace_in_path is not None:
		file_path = file_path.replace(replace_in_path, "")
	content, success = process_socket(get_message, (ip, port), buffer_size=4096, path=file_path)
	if not success:
		stderr.write(content.decode())
	return success


def get_all_request(file_path:str, server_name:str, ip:str, port:int):
	""" GET ALL File Service Protocol request (* ending path). """
	file_path = file_path[:-1]
	if get_request("index", server_name, ip, port):
		with open("index", "r") as index:
			for file in index.read().splitlines():
				if not file_path in file: continue
				if not get_request(file, server_name, ip, port, file_path): break


if __name__ == "__main__":
	args = parse_arguments()
	server_ip, server_port, ok_ip = resolve_ipaddress(args.nameserver)
	server_name, file_path, collective_download, ok_surl  = resolve_surl(args.surl)

	if ok_ip and ok_surl:
		wireq_ip = whereis_request(server_name, server_ip, server_port)
		if wireq_ip is not None:
			_, server_port, _ = resolve_ipaddress(wireq_ip)
			if collective_download:
				get_all_request(file_path, server_name, server_ip, server_port)
			else:
				get_request(file_path, server_name, server_ip, server_port)
