import socket, argparse, sys, re

parser = argparse.ArgumentParser()
parser.add_argument("-n", "--nameserver", required=True)
parser.add_argument("-f", "--surl", required=True)
args = parser.parse_args()

ip_pattern = re.compile("^(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}:\d{1,4})$")

if not ip_pattern.match(args.nameserver):
    print('Bad NAMESERVER format format "IP:PORT"')
    sys.exit(1)

split = args.nameserver.split(":")
server_ip = split[0]
server_port = split[1]

print(server_ip)
print(server_port)

client_socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
...
