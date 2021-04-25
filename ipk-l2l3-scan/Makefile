CC = dotnet

build:
	$(CC) build

run-args: build
	sudo $(CC) run --no-build -- --interface $(interface) --subnet $(subnet) --wait $(wait)

run-list: build
	$(CC) run --no-build

run-help: build
	$(CC) run --no-build -- --help

tar:
	tar -cvf xmilos02.tar *.cs *.csproj *.sln Makefile manual.pdf README.md
