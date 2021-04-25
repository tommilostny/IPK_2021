# IPK Projekt 2
## Zadání DELTA - Scanner síťové dostupnosti

Autor: Tomáš Milostný (xmilos02)

---

Program **ipk-l2l3-scan** je vytvořený v jazyce **C# 9.0** nad platformou **.NET 5.0**.
Skenuje zadané subnetové rozsahy pomocí protokolu ICMP/ICMPv6 a ARP.

Omezení oproti původnímu zadání: Projekt neimplementuje protokol NDP pro IPv6 subnety.

---

### Příklady spuštění:

* S využitím přiloženého **Makefile** na Unixových systémech:
    - Sestavení programu: ``make build``
    - Spuštění programu s argumenty cli: ``make interface=eth0 subnet=192.168.1.0/24 wait=1000 run-args``
    - Spuštění programu bez argumetů (výpis seznamu rozhraní): ``make run-list``
    - Výpis nápovědy: ``make run-help``
    - Zabalení projektu do .tar archivu: ``make tar``

* Z příkazové řádky nad **dotnet**:
    - Sestavení programu: ``dotnet build``
    - Spuštění programu s argumenty cli: ``dotnet run -- --interface "Wi-Fi" --subnet 192.168.1.0/24  --wait 1000``
    - Spuštění programu bez argumetů (výpis seznamu rozhraní): ``dotnet run``
    - Výpis nápovědy: ``dotnet run -- --help``

---

### Odevzdané soubory:

* [Program.cs](Program.cs) - hlavní program
* [ArgumentParser.cs](ArgumentParser.cs) - parser argumentů příkazové řádky (využívá System.CommandLine)
* [SubnetParser.cs](SubnetParser.cs) - parser subnetů (vytváří ``Subnet`` objekty)
* [Subnet.cs](Subnet.cs) - třída ``Subnet`` - drží si informace o skenované síti, aplikuje masku sítě na IP adresu zadanou argumentem
* [NetworkScanner.cs](NetworkScanner.cs) - provádí skenování rozsahu sítě na daném rozhraní, vypisuje status ICMP/ICMPv6 a ARP na standardní výstup
* [IcmpPacket.cs](IcmpPacket.cs) - struktura ICMP packetu, obsahuje statickou funkci, která tuto strukturu převede na pole bytů odeslané ICMP/ICMPv6 socketem
