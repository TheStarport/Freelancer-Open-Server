				    Packets
				 by Jason Hood

				   Release 4


Packets contains a number of utilities to indirectly examine Freelancer's
network traffic.


PacketStats
-----------

PacketStats uses Freelancer's own function to dump its stats
(IServerImpl::DumpPacketStats), to EXE\flservertrace.txt (overwritten each
time).	It can be executed whenever FLServer is running, but the stats are
cumulative.


PacketStatsExit
---------------

PacketStatsExit is a plugin to dump the stats when FLServer exits, saving to
EXE\PacketStats-YYYY-MM-DD.hhmmss.txt, where YYYY-MM-DD.hhmmss is the time
FLServer was started.


PacketLog
---------

PacketLog is a plugin to log the packets FLServer sends and receives (more or
less), to EXE\PacketLog-YYYY-MM-DD.hhmmss.bin.	ALL packets are logged, so the
file may grow very large and lag your server.  In addition, EXE\LargeID.dat is
created, to enable PacketDump to convert "small" ids (16-bit indices) to "large"
ids (32-bit hash code).


PacketDump
----------

PacketDump takes the log files generated above and translates them to text.
Most packets are recognised (a hex dump will result if not), but there's still
a lot of unknowns (shown as u_TYPE) and some items could probably be named
better.  Conversion results in the ".bin" extension being replaced with ".txt"
(or ".txt" appended).  However, the translated files will be written to standard
output, if it is redirected.  LargeID.dat is read from the same directory as the
first file (so if you move the log files out of EXE, move LargeID.dat, too).
Some common packets are filtered out by default - see packets-excluded.txt and
PacketDump's own help (run with no arguments).


----------
INSTALLING
----------

The plugins are installed via EXE\dacomsrv.ini [Libraries] entries.  They
require and assume the official patch has been installed.


-------
HISTORY
-------

Release 4
---------

PacketLog includes the player the packet is for/from.  It won't write a packet
if it is the same as the previous packet.

Improvements to PacketDump (see PacketDump.cpp for details).


Release 3
---------

Improvements to PacketDump (see PacketDump.c for details).


Release 2
---------

Improvements to PacketDump (see PacketDump.c for details).


--------------------------------
Jason Hood, 10 December, 2010.
http://freelancer.adoxa.cjb.net/
