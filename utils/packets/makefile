# Makefile (VC6) for Packets.
# Jason Hood, 23 October, 2010.

CFLAGS = /nologo /W3 /Gf /Ox /MD
CPPFLAGS = /nologo /W3 /GfX /Ox /MD

all: PacketLog.dll PacketDump.exe PacketStats.exe PacketStatsExit.dll

PacketLog.dll: PacketLog.obj PacketLog.res
	cl $(CFLAGS) /LD $**

PacketDump.exe: PacketDump.obj PacketDump.res
	cl $(CFLAGS) $** setargv.obj

PacketStats.exe: PacketStats.obj PacketStats.res
	cl $(CFLAGS) $** /link /subsystem:windows user32.lib

PacketStatsExit.dll: PacketStatsExit.obj PacketStatsExit.res
	cl $(CFLAGS) /LD $**

clean:
	del *.obj *.res
