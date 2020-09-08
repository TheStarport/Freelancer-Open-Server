# Freelancer Open Server

As we pass the 10 year aniversary, we've attempted the supposed impossible and seem to have somewhat succeeded. This project is built upon the shoulders the open source projects and developers in the Freelancer community. Without the release of code by different people, this project would have been impossible. Some notable projects that directly contributed to this:

* UTFEditor (both the new C# one and the original)
* PacketDump (thanks Adoxa!)
* FLHook (thanks Horst! Motah, Wodka, Crazy and you know who you all are!)
* DSAM/DSPM
* [that quaternion tool on tsp]
* SurDump (thanks again Adoxa!) 

## Build Instructions

**WARNING!** That's an experimental Akka.Net build!
Google "Actor Model" to read more.

- Copy `src/flopenserver.cfg` to your executable directory and modify the values accordingly.
- Make sure you have the default saves from `src/Accts` directory in executables' directory before launching the server.
- Currently Akka build has no UI, so check the logs in `/logs` directory and/or launch with debugger.

## Akka Hierarchy
* /user/server - main server actor
	* /reader-socket - Socket that's bound to server port, UDPSock
	* /globals - Server info, Globals
	* /sessions/IP:PORT - UDP session, Session
		* /dplay-session - DPlay session, DPlaySession
		* /congestion - Packet flow management
		* /socket - UDP socket, not bound - only for sending to session
	* /player/FLID - main player actor, Player
		* /state - player state, State
			* /base - base state, BaseState
		* /chat - chat handler, Chat
		* /listhandler - player list handler, PlayerListHandler
		* /ship - player ship proxy, Player.Ship.Ship
