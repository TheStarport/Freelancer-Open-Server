/*
  PacketDump.c - Translate the binary packet dump to text.

  Jason Hood, 12 to 23 October, 2010.

  This is for multiplayer; single player packets have some differences.

  v1.01, 25 October, 2010:
  + recognise the extra information from the Random Jumps plugin.

  v1.02, 21 November, 2010:
  + decode FLAC chat messages;
  + decode FLPACKET_SERVER_GFDESTROYNEWSBROADCAST, FLPACKET_SERVER_SETDIRECTIVE
    & FLPACKET_SERVER_SCANNOTIFY;
  * change "counter" to "newsid" in FLPACKET_SERVER_GFUPDATENEWSBROADCAST;
  * second "u_dword" in FLPACKET_SERVER_PLAYERLIST is "player";
  * (type & 4) in FLPACKET_SERVER_MISCOBJUPDATE is "player";
  - handle unexpected packets gracefully;
  - add newline after "para" chat type;

  v1.03, 5 & 6 December, 2010:
  - use the default exclusion file when dragging and dropping;
  * decode FLPACKET_CLIENT_TRADERESPONSE.

  v1.04, 8 to 10 December, 2010:
  * handle FLP2 format;
  * change size display to data (i.e. without the two packet type bytes);
  + added the maneuver strings;
  * removed "para" from chat messages;
  - fixed "packed" rotate order;
  * From FLPACKET_COMMON_PLAYER_TRADE is "player", not "object";
  * change "object" to "charid" in FLPACKET_CLIENT_GFSELECTOBJECT &
    FLPACKET_CLIENT_MISSIONRESPONSE;
  * identified unknowns in FLPACKET_COMMON_CHATMSG & FLPACKET_SERVER_PLAYERLIST;
  * last "u_byte" and "u_string" in FLPACKET_SERVER_CREATESHIP is "player" and
    "name";
  * change "counter" in FLPACKET_SERVER_CREATESHIP &
    FLPACKET_SERVER_CREATESOLAR, "object" in
    FLPACKET_SERVER_CHARSELECTVERIFIED & second "u_dword" in
    FLPACKET_COMMON_REQUEST_GROUP_POSITIONS to "pilot";
  - display additional "faction" values as signed (-1 means no affiliation).
*/

#define PVERS "1.04"
#define PDATE "10 December, 2010"

#include <time.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <io.h>
#include <math.h>
#include <string>
#include <map>
#include <vector>

#ifdef _WIN32
#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <conio.h>
#else
typedef unsigned char  BYTE;
typedef unsigned short WORD;
typedef unsigned int   DWORD;
#endif

#define lenof(a) (sizeof(a)/sizeof(*(a)))

#ifndef M_PI
# define M_PI 3.14159265357989323846
#endif

typedef std::map<std::string, std::wstring> CharNameMap;
typedef std::vector<std::wstring> StrVec;
typedef std::map<DWORD, int> ObjectPlayerMap;
typedef ObjectPlayerMap::const_iterator ObjectPlayerCIter;


int flp_ver, flp_size;

CharNameMap	char_name;
StrVec		player_name;
ObjectPlayerMap object_player, pilot_player;

BOOL found_error;

const char default_exclude_file[] = "packets-excluded.txt";

void exclude( const char* );


const char* common_packets[] = {
  "FLPACKET_COMMON_00",
  "FLPACKET_COMMON_UPDATEOBJECT",
  "FLPACKET_COMMON_FIREWEAPON",
  "FLPACKET_COMMON_03",
  "FLPACKET_COMMON_SETTARGET",
  "FLPACKET_COMMON_CHATMSG",
  "FLPACKET_COMMON_06",
  "FLPACKET_COMMON_07",
  "FLPACKET_COMMON_ACTIVATEEQUIP",
  "FLPACKET_COMMON_09",
  "FLPACKET_COMMON_0A",
  "FLPACKET_COMMON_0B",
  "FLPACKET_COMMON_0C",
  "FLPACKET_COMMON_0D",
  "FLPACKET_COMMON_ACTIVATECRUISE",
  "FLPACKET_COMMON_GOTRADELANE",
  "FLPACKET_COMMON_STOPTRADELANE",
  "FLPACKET_COMMON_SET_WEAPON_GROUP",
  "FLPACKET_COMMON_PLAYER_TRADE",
  "FLPACKET_COMMON_SET_VISITED_STATE",
  "FLPACKET_COMMON_JETTISONCARGO",
  "FLPACKET_COMMON_ACTIVATETHRUSTERS",
  "FLPACKET_COMMON_REQUEST_BEST_PATH",
  "FLPACKET_COMMON_REQUEST_GROUP_POSITIONS",
  "FLPACKET_COMMON_REQUEST_PLAYER_STATS",
  "FLPACKET_COMMON_SET_MISSION_LOG",
  "FLPACKET_COMMON_REQUEST_RANK_LEVEL",
  "FLPACKET_COMMON_POP_UP_DIALOG",
  "FLPACKET_COMMON_SET_INTERFACE_STATE",
  "FLPACKET_COMMON_TRACTOROBJECTS"
};

const char* server_packets[] = {
  "FLPACKET_SERVER_00",
  "FLPACKET_SERVER_CONNECTRESPONSE",
  "FLPACKET_SERVER_LOGINRESPONSE",
  "FLPACKET_SERVER_CHARACTERINFO",
  "FLPACKET_SERVER_CREATESHIP",
  "FLPACKET_SERVER_DAMAGEOBJECT",
  "FLPACKET_SERVER_DESTROYOBJECT",
  "FLPACKET_SERVER_LAUNCH",
  "FLPACKET_SERVER_CHARSELECTVERIFIED",
  "FLPACKET_SERVER_09",
  "FLPACKET_SERVER_ACTIVATEOBJECT",
  "FLPACKET_SERVER_LAND",
  "FLPACKET_SERVER_0C",
  "FLPACKET_SERVER_SETSTARTROOM",
  "FLPACKET_SERVER_GFCOMPLETENEWSBROADCASTLIST",
  "FLPACKET_SERVER_GFCOMPLETECHARLIST",
  "FLPACKET_SERVER_GFCOMPLETEMISSIONCOMPUTERLIST",
  "FLPACKET_SERVER_GFCOMPLETESCRIPTBEHAVIORLIST",
  "FLPACKET_SERVER_12",
  "FLPACKET_SERVER_GFCOMPLETEAMBIENTSCRIPTLIST",
  "FLPACKET_SERVER_GFDESTROYNEWSBROADCAST",
  "FLPACKET_SERVER_GFDESTROYCHARACTER",
  "FLPACKET_SERVER_GFDESTROYMISSIONCOMPUTER",
  "FLPACKET_SERVER_GFDESTROYSCRIPTBEHAVIOR",
  "FLPACKET_SERVER_18",
  "FLPACKET_SERVER_GFDESTROYAMBIENTSCRIPT",
  "FLPACKET_SERVER_GFSCRIPTBEHAVIOR",
  "FLPACKET_SERVER_1B",
  "FLPACKET_SERVER_1C",
  "FLPACKET_SERVER_GFUPDATEMISSIONCOMPUTER",
  "FLPACKET_SERVER_GFUPDATENEWSBROADCAST",
  "FLPACKET_SERVER_GFUPDATEAMBIENTSCRIPT",
  "FLPACKET_SERVER_GFMISSIONVENDORACCEPTANCE",
  "FLPACKET_SERVER_SYSTEM_SWITCH_OUT",
  "FLPACKET_SERVER_SYSTEM_SWITCH_IN",
  "FLPACKET_SERVER_SETSHIPARCH",
  "FLPACKET_SERVER_SETEQUIPMENT",
  "FLPACKET_SERVER_SETCARGO",
  "FLPACKET_SERVER_GFUPDATECHAR",
  "FLPACKET_SERVER_REQUESTCREATESHIPRESP",
  "FLPACKET_SERVER_CREATELOOT",
  "FLPACKET_SERVER_SETREPUTATION",
  "FLPACKET_SERVER_ADJUSTATTITUDE",
  "FLPACKET_SERVER_SETGROUPFEELINGS",
  "FLPACKET_SERVER_CREATEMINE",
  "FLPACKET_SERVER_CREATECOUNTER",
  "FLPACKET_SERVER_SETADDITEM",
  "FLPACKET_SERVER_SETREMOVEITEM",
  "FLPACKET_SERVER_SETCASH",
  "FLPACKET_SERVER_EXPLODEASTEROIDMINE",
  "FLPACKET_SERVER_REQUESTSPACESCRIPT",
  "FLPACKET_SERVER_SETMISSIONOBJECTIVESTATE",
  "FLPACKET_SERVER_REPLACEMISSIONOBJECTIVE",
  "FLPACKET_SERVER_SETMISSIONOBJECTIVES",
  "FLPACKET_SERVER_36",
  "FLPACKET_SERVER_CREATEGUIDED",
  "FLPACKET_SERVER_ITEMTRACTORED",
  "FLPACKET_SERVER_SCANNOTIFY",
  "FLPACKET_SERVER_3A",
  "FLPACKET_SERVER_3B",
  "FLPACKET_SERVER_REPAIROBJECT",
  "FLPACKET_SERVER_REMOTEOBJECTCARGOUPDATE",
  "FLPACKET_SERVER_SETNUMKILLS",
  "FLPACKET_SERVER_SETMISSIONSUCCESSES",
  "FLPACKET_SERVER_SETMISSIONFAILURES",
  "FLPACKET_SERVER_BURNFUSE",
  "FLPACKET_SERVER_CREATESOLAR",
  "FLPACKET_SERVER_SET_STORY_CUE",
  "FLPACKET_SERVER_REQUEST_RETURNED",
  "FLPACKET_SERVER_SET_MISSION_MESSAGE",
  "FLPACKET_SERVER_MARKOBJ",
  "FLPACKET_SERVER_CFGINTERFACENOTIFICATION",
  "FLPACKET_SERVER_SETCOLLISIONGROUPS",
  "FLPACKET_SERVER_SETHULLSTATUS",
  "FLPACKET_SERVER_SETGUIDEDTARGET",
  "FLPACKET_SERVER_SET_CAMERA",
  "FLPACKET_SERVER_REVERT_CAMERA",
  "FLPACKET_SERVER_LOADHINT",
  "FLPACKET_SERVER_SETDIRECTIVE",
  "FLPACKET_SERVER_SENDCOMM",
  "FLPACKET_SERVER_50",
  "FLPACKET_SERVER_USE_ITEM",
  "FLPACKET_SERVER_PLAYERLIST",
  "FLPACKET_SERVER_FORMATION_UPDATE",
  "FLPACKET_SERVER_MISCOBJUPDATE",
  "FLPACKET_SERVER_OBJECTCARGOUPDATE",
  "FLPACKET_SERVER_SENDNNMESSAGE",
  "FLPACKET_SERVER_SET_MUSIC",
  "FLPACKET_SERVER_CANCEL_MUSIC",
  "FLPACKET_SERVER_PLAY_SOUND_EFFECT",
  "FLPACKET_SERVER_GFMISSIONVENDORWHYEMPTY",
  "FLPACKET_SERVER_MISSIONSAVEA"
};

const char* client_packets[] = {
  "FLPACKET_CLIENT_00",
  "FLPACKET_CLIENT_LOGIN",
  "FLPACKET_CLIENT_02",
  "FLPACKET_CLIENT_MUNCOLLISION",
  "FLPACKET_CLIENT_REQUESTLAUNCH",
  "FLPACKET_CLIENT_REQUESTCHARINFO",
  "FLPACKET_CLIENT_SELECTCHARACTER",
  "FLPACKET_CLIENT_ENTERBASE",
  "FLPACKET_CLIENT_REQUESTBASEINFO",
  "FLPACKET_CLIENT_REQUESTLOCATIONINFO",
  "FLPACKET_CLIENT_GFREQUESTSHIPINFO",
  "FLPACKET_CLIENT_SYSTEM_SWITCH_OUT_COMPLETE",
  "FLPACKET_CLIENT_OBJCOLLISION",
  "FLPACKET_CLIENT_EXITBASE",
  "FLPACKET_CLIENT_ENTERLOCATION",
  "FLPACKET_CLIENT_EXITLOCATION",
  "FLPACKET_CLIENT_REQUESTCREATESHIP",
  "FLPACKET_CLIENT_GFGOODSELL",
  "FLPACKET_CLIENT_GFGOODBUY",
  "FLPACKET_CLIENT_GFSELECTOBJECT",
  "FLPACKET_CLIENT_MISSIONRESPONSE",
  "FLPACKET_CLIENT_REQSHIPARCH",
  "FLPACKET_CLIENT_REQEQUIPMENT",
  "FLPACKET_CLIENT_REQCARGO",
  "FLPACKET_CLIENT_REQADDITEM",
  "FLPACKET_CLIENT_REQREMOVEITEM",
  "FLPACKET_CLIENT_REQMODIFYITEM",
  "FLPACKET_CLIENT_REQSETCASH",
  "FLPACKET_CLIENT_REQCHANGECASH",
  "FLPACKET_CLIENT_1D",
  "FLPACKET_CLIENT_SAVEGAME",
  "FLPACKET_CLIENT_1F",
  "FLPACKET_CLIENT_MINEASTEROID",
  "FLPACKET_CLIENT_21",
  "FLPACKET_CLIENT_DBGCREATESHIP",
  "FLPACKET_CLIENT_DBGLOADSYSTEM",
  "FLPACKET_CLIENT_DOCK",
  "FLPACKET_CLIENT_DBGDESTROYOBJECT",
  "FLPACKET_CLIENT_26",
  "FLPACKET_CLIENT_TRADERESPONSE",
  "FLPACKET_CLIENT_28",
  "FLPACKET_CLIENT_29",
  "FLPACKET_CLIENT_2A",
  "FLPACKET_CLIENT_CARGOSCAN",
  "FLPACKET_CLIENT_2C",
  "FLPACKET_CLIENT_DBGCONSOLE",
  "FLPACKET_CLIENT_DBGFREESYSTEM",
  "FLPACKET_CLIENT_SETMANEUVER",
  "FLPACKET_CLIENT_DBGRELOCATE_SHIP",
  "FLPACKET_CLIENT_REQUEST_EVENT",
  "FLPACKET_CLIENT_REQUEST_CANCEL",
  "FLPACKET_CLIENT_33",
  "FLPACKET_CLIENT_34",
  "FLPACKET_CLIENT_INTERFACEITEMUSED",
  "FLPACKET_CLIENT_REQCOLLISIONGROUPS",
  "FLPACKET_CLIENT_COMMCOMPLETE",
  "FLPACKET_CLIENT_REQUESTNEWCHARINFO",
  "FLPACKET_CLIENT_CREATENEWCHAR",
  "FLPACKET_CLIENT_DESTROYCHAR",
  "FLPACKET_CLIENT_REQHULLSTATUS",
  "FLPACKET_CLIENT_GFGOODVAPORIZED",
  "FLPACKET_CLIENT_BADLANDSOBJCOLLISION",
  "FLPACKET_CLIENT_LAUNCHCOMPLETE",
  "FLPACKET_CLIENT_HAIL",
  "FLPACKET_CLIENT_REQUEST_USE_ITEM",
  "FLPACKET_CLIENT_ABORT_MISSION",
  "FLPACKET_CLIENT_SKIP_AUTOSAVE",
  "FLPACKET_CLIENT_JUMPINCOMPLETE",
  "FLPACKET_CLIENT_REQINVINCIBILITY",
  "FLPACKET_CLIENT_MISSIONSAVEB",
  "FLPACKET_CLIENT_REQDIFFICULTYSCALE",
  "FLPACKET_CLIENT_RTCDONE"
};


const char* Hardpoint[] = {
  "<unknown>",
  "HpCM01",
  "HpCargo01",
  "HpCargo02",
  "HpContrail01",
  "HpContrail02",
  "HpDockLight01",
  "HpDockLight02",
  "HpDockLight03",
  "HpHeadlight",
  "HpMine01",
  "HpRunningLight01",
  "HpRunningLight02",
  "HpRunningLight03",
  "HpRunningLight04",
  "HpRunningLight05",
  "HpRunningLight06",
  "HpRunningLight07",
  "HpRunningLight10",
  "HpRunningLight11",
  "HpRunningLight12",
  "HpRunningLight13",
  "HpShield01",
  "HpThruster01",
  "HpTurret01",
  "HpTurret02",
  "HpTurret03",
  "HpTurret04",
  "HpTurret05",
  "HpTurret06",
  "HpTurret07",
  "HpTurret08",
  "HpTurret09",
  "HpTurret_U1_01",
  "HpTurret_U1_02",
  "HpTurret_U1_03",
  "HpTurret_U1_04",
  "HpTurret_U1_05",
  "HpTurret_U3_01",
  "HpWeapon01",
  "HpWeapon02",
  "HpWeapon03",
  "HpWeapon04",
};


const char* TalkType[] = {
  "<unknown>",
  "bribe",
  "rumor",
  "job",
  "info",
};


// Resources 13050 - 13055.
const char* JobType[] = {
  "<unknown>",
  "Kill Ships",
  "Destroy Installation",
  "Assassinate",
  "Destroy Contraband",
  "Capture Prisoner",
  "Tractor Loot",
};


const char* NewsIcon[] = {
  "<unknown>",
  "critical",
  "world",
  "mission",
  "system",
  "faction",
  "universe",
};


const char* Channel[] = {
  "Console",
  "System",
  "Local",
  "Group",
  "Universe",
};


const char* ManeuverType[] = {
  "NULL",
  "Buzz",
  "Goto",
  "Trail",
  "Flee",
  "Evade",
  "Idle",
  "Dock",
  "Launch",
  "InstantTradeLane",
  "Formation",
  "Large Ship (move)",
  "Cruise",
  "Strafe",
  "Guide",
  "Face",
  "Loot",
  "Follow",
  "DrasticEvade",
  "FreeFlight",
  "Delay",
};


DWORD* LargeID;
int    LargeID_cnt;

DWORD SmallIDToLargeID( WORD SmallID )
{
  if (SmallID == 0 || SmallID >= LargeID_cnt)
    return SmallID;
  return LargeID[SmallID - 1];
}


union cPtrs_t
{
  const BYTE*	b;
  const char*	c;
  const short*	s;
  const WORD*	w;
  const int*	i;
  const long*	l;
  const DWORD*	d;
  const float*	f;
  const double* D;
};


union cPtrs_t items( FILE*, union cPtrs_t, DWORD, int );
union cPtrs_t fmtstr( FILE*, union cPtrs_t, int );
void Player_Name( FILE*, int );
void Object_Name( FILE*, const char*, int, DWORD, const ObjectPlayerMap& = object_player );
void ChannelStr( FILE* file, const char*, int, DWORD );


void Packet_Dump( FILE* file, FILE* bin, const BYTE* buf, size_t size, long offset )
{
  WORD millitm;
  struct tm* tim;
  int player;
  bool from;
  const char** packets;
  size_t packets_size;
  size_t ofs;
  const BYTE* beg;
  union cPtrs_t data;
  DWORD cnt;
  std::string charfile;
  std::wstring charname;

  data.b = buf;
  tim = localtime( data.l++ );
  millitm = *data.w++;
  from = false;
  if (flp_ver >= 2)
  {
    player = *data.s++;
    from = (player < 0);
  }

  packets = NULL;
  packets_size = 0;
  switch (data.b[1])
  {
    case 1: packets = common_packets; packets_size = lenof(common_packets); break;
    case 2: packets = server_packets; packets_size = lenof(server_packets); break;
    case 3: packets = client_packets; packets_size = lenof(client_packets); break;
  }
  if (data.b[0] < packets_size)
  {
    if (packets[data.b[0]] == NULL)
      return;
  }

  fprintf( file, "%.2d:%.2d:%.2d.%.3d: ",
		 tim->tm_hour, tim->tm_min, tim->tm_sec, millitm );
  if (packets_size == 0)
  {
    fprintf( file, "FLPACKET_%.6u_%.2u", data.b[1], data.b[0] );
  }
  else if (data.b[0] >= packets_size)
  {
    fprintf( file, "%.16s%.2u", packets[0], data.b[0] );
  }
  else
  {
    fputs( packets[data.b[0]], file );
  }
  fprintf( file, ", offset = 0x%X, size = %u\n", offset, size - 2 );

  if (flp_ver >= 2)
  {
    fprintf( file, "%14c%s: ", ' ', (from) ? "From" : "To" );
    // Reset the name on connection.
    if (data.b[1] == 2 && data.b[0] == 1) // FLPACKET_SERVER_CONNECTRESPONSE
    {
      if (player < player_name.size())
	player_name[player] = L"";
      fprintf( file, "%d\n", player );
    }
    // FLPACKET_CLIENT_SELECTCHARACTER is handled separately.
    else if (data.b[1] != 3 || data.b[0] != 6)
    {
      Player_Name( file, abs( player ) );
      offset = ftell( bin );
      size_t nxt;
      while (fread( &nxt, sizeof(nxt), 1, bin ) == 1 && nxt == 0)
      {
	short plyr;
	fread( &plyr, 2, 1, bin );
	if ((plyr & 0x8000) ^ (player & 0x8000))
	{
	  putc( '\n', file );
	  fprintf( file, "%14c%s: ", ' ', (plyr < 0) ? "From" : "To" );
	}
	else
	{
	  fputs( ", ", file );
	}
	player = plyr;
	Player_Name( file, abs( plyr ) );
	offset = ftell( bin );
      }
      fseek( bin, offset, SEEK_SET );
      putc( '\n', file );
    }
    player = abs( player );
  }

  data.b += 2; size -= 2;
  beg = data.b;

  try
  {

  switch (data.b[-1])
  {
    case 1: switch (data.b[-2])
    {
      case 0x01: // FLPACKET_COMMON_UPDATEOBJECT
      {
	fprintf( file, "\tu_byte  = 0x%.2X\n", *data.b++ );
	Object_Name( file, "object", 7, *data.d++ );
	fprintf( file, "\tpos     = %g, %g, %g\n", data.f[0], data.f[1], data.f[2] );
	data.f += 3;
	fprintf( file, "\trotate  = %g, %g, %g, %g\n", data.c[3] / 127.0,
						       data.c[0] / 127.0,
						       data.c[1] / 127.0,
						       data.c[2] / 127.0 );
	data.b += 4;
	fprintf( file, "\tu_float = %g\n", *data.c++ / 127.0 );
	fprintf( file, "\ttime    = %.*f\n", (*data.f < 1000) ? 6 : 3, *data.f );
	data.f++;
	break;
      }

      case 0x02: // FLPACKET_COMMON_FIREWEAPON
      {
	BYTE  type;
	float x, y, z;

	Object_Name( file, "object", 6, *data.d++ );
	type = *data.b++;
	if (type & 0x01)
	  x = *data.c++ / 127.0f * 64;
	else if (type & 0x02)
	  x = *data.w++ / 32767.0f * 8192;
	else
	  x = *data.f++;
	if (type & 0x04)
	  y = *data.c++ / 127.0f * 64;
	else if (type & 0x08)
	  y = *data.w++ / 32767.0f * 8192;
	else
	  y = *data.f++;
	if (type & 0x10)
	  z = *data.c++ / 127.0f * 64;
	else if (type & 0x20)	// BUG: it's read, but not actually written
	  z = *data.w++ / 32767.0f * 8192;
	else
	  z = *data.f++;
	fprintf( file, "\tpos    = %g, %g, %g\n", x, y, z );
	if (type & 0x40)
	  type = 1;
	else
	  type = *data.b++;
	while (type-- != 0)
	  fprintf( file, "\thpid   = %u\n", *data.w++ );
      }
      break;

      case 0x04: // FLPACKET_COMMON_SETTARGET
      {
	Object_Name( file, "object", 8, *data.d++ );
	Object_Name( file, "target", 8, *data.d++ );
	fprintf( file, "\tsubobjid = %u\n", *data.d++ );
	break;
      }

      case 0x05: // FLPACKET_COMMON_CHATMSG
      {
	int   width = 7;
	DWORD len = *data.d++;
	if (*data.d == 0xF1AC)
	{
	  switch (data.d[1])
	  {
	    case 2:
	    case 7:
	    case 9: break;
	    case 3:
	    case 5: width = 18; break;
	    case 4: width = 20; break;
	    case 6: width = 11; break;
	    case 8: width = 4;	break;
	  }
	  fprintf( file, "\t%-*s = 0x%.4X\n", width, "FLAC", *data.d++ );
	  fprintf( file, "\t%-*s = %u\n",     width, "type", *data.d++ );
	  switch (data.d[-1])
	  {
	    case 2:
	      fprintf( file, "\tbase    = %u\n", *data.d++ );
	      fprintf( file, "\tu_dword = %u\n", *data.d++ );
	      break;
	    case 3:
	      fprintf( file, "\tangular_drag       = %g, %g, %g\n", data.f[0], data.f[1], data.f[2] );
	      fprintf( file, "\trotation_inertia   = %g, %g, %g\n", data.f[3], data.f[4], data.f[5] );
	      fprintf( file, "\tsteering_torque    = %g, %g, %g\n", data.f[6], data.f[7], data.f[8] );
	      fprintf( file, "\tnudge_force        = %g\n", data.f[9] );
	      fprintf( file, "\tstrafe_force       = %g\n", data.f[10] );
	      fprintf( file, "\tstrafe_power_usage = %g\n", data.f[11] );
	      fprintf( file, "\tmass               = %g\n", data.f[12] );
	      fprintf( file, "\tlinear_drag        = %g\n", data.f[13] );
	      data.f += 14;
	      break;
	    case 4:
	      fprintf( file, "\tnickname             = %u\n", *data.d++ );
	      fprintf( file, "\tpower_usage          = %g\n", *data.f++ );
	      fprintf( file, "\trefire_delay         = %g\n", *data.f++ );
	      fprintf( file, "\tmuzzle_velocity      = %g\n", *data.f++ );
	      fprintf( file, "\tturn_rate            = %g\n", *data.f++ * 180/M_PI );
	      fprintf( file, "\tauto_turret          = %s\n", (*data.d++) ? "true" : "false" );
	      fprintf( file, "\tprojectile_archetype = %u\n", *data.d++ );
	      fprintf( file, "\trequires_ammo        = %s\n", (*data.d++) ? "true" : "false" );
	      fprintf( file, "\tdetonation_dist      = %g\n", sqrt( *data.f++ ) );
	      fprintf( file, "\tlifetime             = %g\n", *data.f++ );
	      fprintf( file, "\ttime_to_lock         = %g\n", *data.f++ );
	      fprintf( file, "\tseeker_range         = %g\n", *data.f++ );
	      fprintf( file, "\tseeker_fov_deg       = %g\n", acos(*data.f++) * 180/M_PI);
	      fprintf( file, "\tmax_angular_velocity = %g\n", *data.f++ );
	      fprintf( file, "\tseeker               = %s\n", (*data.d == 1) ? "DUMB" :
							      (*data.d == 2) ? "LOCK" : "none");
	      ++data.d;
	      break;
	    case 5:
	      fprintf( file, "\tnickname           = %u\n", *data.d++ );
	      fprintf( file, "\tcruise_charge_time = %g\n", *data.f++ );
	      fprintf( file, "\tcruise_power_usage = %g\n", *data.f++ );
	      fprintf( file, "\tlinear_drag        = %g\n", *data.f++ );
	      fprintf( file, "\tmax_force          = %g\n", *data.f++ );
	      fprintf( file, "\treverse_fraction   = %g\n", *data.f++ );
	      break;
	    case 6:
	      fprintf( file, "\tnickname    = %u\n", *data.d++ );
	      fprintf( file, "\tmax_force   = %g\n", *data.f++ );
	      fprintf( file, "\tpower_usage = %g\n", *data.f++ );
	      break;
	    case 7:
	      fprintf( file, "\tu_float = %g\n", *data.f++ );
	      fprintf( file, "\tu_float = %g\n", *data.f++ );
	      fprintf( file, "\tu_float = %g\n", *data.f++ );
	      fprintf( file, "\tu_float = %g\n", *data.f++ );
	      fprintf( file, "\tu_float = %g\n", *data.f++ );
	      break;
	    case 8:
	      break; // no data
	    case 9:
	      fprintf( file, "\tu_dword = %u\n", *data.d++ );
	      break;
	    default:
	      fprintf( file, "\tunknown = %u bytes\n", len );
	      data.b += len;
	      break;
	  }
	}
	else if (len == 8) // to/from Universe
	{
	  fprintf( file, "\tcommand = %u\n", *data.d++ );
	  fprintf( file, "\tplayer  = " );
	  Player_Name( file, *data.d++ );
	  putc( '\n', file );
	}
	else
	{
	  width = 5;
	  while (len != 0)
	  {
	    DWORD rdl = *data.d++;
	    DWORD siz = *data.d++;
	    switch (rdl)
	    {
	      case 1: // TRA
		fprintf( file, "\tTRA   = 0x%.8X\n", *data.d++ );
		fprintf( file, "\tmask  = 0x%.8X\n", *data.d++ );
		break;
	      case 2: // text
		fprintf( file, "\ttext  = %.*S\n", siz / 2, data.w );
		data.b += siz;
		break;
	      /*
	      case 5: // para
		fprintf( file, "\tpara\n" );
		break;
	      */
	      case 6: // style
		fprintf( file, "\tstyle = 0x%.4X\n", *data.w++ );
		break;
	      default: // just ignore 'em
		data.b += siz;
	    }
	    len -= 8 + siz;
	  }
	}
	ChannelStr( file, "to",   width, *data.d++ );
	ChannelStr( file, "from", width, *data.d++ );
	break;
      }

      case 0x08: // FLPACKET_COMMON_ACTIVATEEQUIP
      {
	Object_Name( file, "object", 6, *data.d++ );
	fprintf( file, "\tid     = %u\n", *data.w++ );
	fprintf( file, "\tequip  = %s\n", (*data.b++) ? "active" : "inactive" );
	break;
      }

      case 0x0E: // FLPACKET_COMMON_ACTIVATECRUISE
      {
	Object_Name( file, "object", 6, *data.d++ );
	fprintf( file, "\tcruise = %s\n", (*data.b++) ? "on" : "off" );
	fprintf( file, "\tu_byte = %u\n", *data.b++ );
	break;
      }

      case 0x0F: // FLPACKET_COMMON_GOTRADELANE
      {
	Object_Name( file, "object", 6, *data.d++ );
	fprintf( file, "\tring   = %u\n", *data.d++ );
	fprintf( file, "\tring   = %u\n", *data.d++ );
	fprintf( file, "\tu_byte = %u\n", *data.b++ );
	break;
      }

      case 0x10: // FLPACKET_COMMON_STOPTRADELANE
      {
	Object_Name( file, "object", 6, *data.d++ );
	fprintf( file, "\tring   = %u\n", *data.d++ );
	fprintf( file, "\tring   = %u\n", *data.d++ );
	break;
      }

      case 0x11: // FLPACKET_COMMON_SET_WEAPON_GROUP
      {
	const char* nl;
	DWORD len = *data.d++;
	while ((nl = (char*)memchr( data.c, '\n', len )) != NULL)
	{
	  fprintf( file, "\t%.*s", ++nl - data.c, data.c );
	  len -= nl - data.c;
	  data.c = nl;
	}
	if (len != 0)
	{
	  fprintf( file, "\t%.*s\n", len, data.c );
	  data.c += len;
	}
	break;
      }

      case 0x12: // FLPACKET_COMMON_PLAYER_TRADE
      {
	BYTE type = *data.b++;
	fprintf( file, "\ttype      = 0x%.2X\n", type );
	switch (type)
	{
	  case 0x01:
	  case 0x20:
	  case 0x80:
	    if (from)
	    {
	      fprintf( file, "\tplayer    = " );
	      Player_Name( file, *data.d++ );
	      putc( '\n', file );
	    }
	    else
	    {
	      Object_Name( file, "object", 9, *data.d++ );
	    }
	    break;
	  case 0x02:
	    fprintf( file, "\tu_int     = %d\n", *data.c++ );
	    break;
	  case 0x04:
	    fprintf( file, "\tmoney     = %u\n", *data.d++ );
	    if (size == 9) // IsMPServer
	      Object_Name( file, "object", 9, *data.d++ );
	    break;
	  case 0x08:
	    fprintf( file, "\tarchetype = %u\n", *data.d++ );
	    fprintf( file, "\tquantity  = %u\n", *data.d++ );
	    fprintf( file, "\thealth    = %g\n", *data.f++ );
	    fprintf( file, "\thpid      = %u\n", *data.w++ );
	    if (size == 19) // IsMPServer
	      Object_Name( file, "object", 9, *data.d++ );
	    break;
	  case 0x10:
	    fprintf( file, "\tu_dword   = %u\n", *data.d++ );
	    fprintf( file, "\tu_dword   = %u\n", *data.d++ );
	    fprintf( file, "\tu_word    = %u\n", *data.w++ );
	    if (size == 15) // IsMPServer
	      Object_Name( file, "object", 9, *data.d++ );
	    break;
	  case 0x40:
	    fprintf( file, "\tu_bool    = %u\n", *data.b++ );
	    if (size == 6) // IsMPServer
	      Object_Name( file, "object", 9, *data.d++ );
	    break;
	}
	break;
      }

      case 0x13: // FLPACKET_COMMON_SET_VISITED_STATE
      {
	data.d++; // size
	cnt = *data.d++;
	fprintf( file, "\tcount = %u\n", cnt );
	while (cnt--)
	{
	  fprintf( file, "\tvisit = %10u, %2u\n", *data.d, data.b[4] );
	  data.b += 5;
	}
	break;
      }

      case 0x14: // FLPACKET_COMMON_JETTISONCARGO
      {
	Object_Name( file, "object", 8, *data.d++ );
	fprintf( file, "\thpid     = %u\n", *data.w++ );
	fprintf( file, "\tquantity = %u\n", *data.d++ );
	break;
      }

      case 0x15: // FLPACKET_COMMON_ACTIVATETHRUSTERS
      {
	Object_Name( file, "object", 9, *data.d++ );
	fprintf( file, "\tthrusters = %s\n", (*data.b++) ? "on" : "off" );
	break;
      }

      case 0x16: // FLPACKET_COMMON_REQUEST_BEST_PATH
      {
	fprintf( file, "\tsize      = %u\n", *data.d++ );
	fprintf( file, "\tu_dword   = %u\n", *data.d++ );
	fprintf( file, "\twaypoints = %u\n", cnt = *data.d++ );
	fprintf( file, "\tu_byte    = %u\n", *data.b );
	data.d++; // looks to be a padded bool
	for (; cnt != 0; --cnt)
	{
	  fprintf( file, "\tpos       = %g, %g, %g\n", data.f[0], data.f[1], data.f[2] );
	  data.f += 3;
	  fprintf( file, "\ttarget    = %u\n", *data.d++ );
	  fprintf( file, "\tsystem    = %u\n", *data.d++ );
	}
	break;
      }

      case 0x17: // FLPACKET_COMMON_REQUEST_GROUP_POSITIONS
      {
	fprintf( file, "\tsize    = %u\n", *data.d++ );
	for (cnt = *data.d++; cnt != 0; --cnt)
	{
	  fprintf( file, "\tpos     = %g, %g, %g\n", data.f[0], data.f[1], data.f[2] );
	  data.f += 3;
	  fprintf( file, "\tu_dword = %u\n", *data.d++ );
	  fprintf( file, "\tsystem  = %u\n", *data.d++ );
	  Object_Name( file, "pilot", 7, *data.d++, pilot_player );
	}
	break;
      }

      case 0x18: // FLPACKET_COMMON_REQUEST_PLAYER_STATS
      {
	DWORD kills, houses;

	if (*data.d++ == 4)
	{
	  fprintf( file, "\tu_dword = %u\n", *data.d++ );
	  break;
	}
	fprintf( file, "\trm_completed      = %u\n", *data.d++ );
	fprintf( file, "\tu_dword           = %u\n", *data.d++ );
	fprintf( file, "\trm_failed         = %u\n", *data.d++ );
	fprintf( file, "\tu_dword           = %u\n", *data.d++ ); // total_cash_earned?
	fprintf( file, "\ttotal_time_played = %g\n", *data.f++ );
	fprintf( file, "\tsystems_visited   = %u\n", *data.d++ );
	fprintf( file, "\tbases_visited     = %u\n", *data.d++ );
	fprintf( file, "\tholes_visited     = %u\n", *data.d++ );
	kills = *data.d++;
	fprintf( file, "\trank              = %u\n", *data.d++ );
	fprintf( file, "\tcurrent_worth     = %u\n", *data.d++ );
	data.d++; // looks uninitialised
	houses = *data.d++;
	for (; kills != 0; --kills)
	{
	  fprintf( file, "\tship_type_killed  = %u, %u\n", data.d[0], data.d[1] );
	  data.d += 2;
	}
	for (; houses != 0; --houses)
	{
	  char rep[16];
	  sprintf( rep, "% g,", data.f[1] );
	  fprintf( file, "\thouse             = %-12s %u\n", rep, data.d[0] );
	  data.d += 2;
	}
	break;
      }

      case 0x19: // FLPACKET_COMMON_SET_MISSION_LOG
      {
	cnt = *data.d++ / 4;
	fprintf( file, "\tcount = %u\n", cnt );
	while (cnt--)
	  fprintf( file, "\tlog   = %u\n", *data.d++ );
	break;
      }

      case 0x1A: // FLPACKET_COMMON_REQUEST_RANK_LEVEL
      case 0x1B: // FLPACKET_COMMON_POP_UP_DIALOG
      break;

      case 0x1C: // FLPACKET_COMMON_SET_INTERFACE_STATE
      {
	cnt = *data.d++ / 4;
	fprintf( file, "\tcount     = %u\n", cnt );
	while (cnt--)
	  fprintf( file, "\tinterface = %u\n", *data.d++ );
	break;
      }

      case 0x1D: // FLPACKET_COMMON_TRACTOROBJECTS
      {
	Object_Name( file, "object", 6, *data.d++ );
	fprintf( file, "\tu_word = %u\n", *data.w++ );
	for (cnt = *data.w++; cnt != 0; --cnt)
	  fprintf( file, "\ttarget = %u\n", *data.d++ );
	break;
      }
    }
    break;

    case 2: switch (data.b[-2])
    {
      case 0x01: // FLPACKET_SERVER_CONNECTRESPONSE
      {
	fprintf( file, "\tresponse = %u\n", *data.d++ );
	break;
      }

      case 0x02: // FLPACKET_SERVER_LOGINRESPONSE
      {
	fprintf( file, "\tresponse = %u\n", *data.b++ );
	break;
      }

      case 0x03: // FLPACKET_SERVER_CHARACTERINFO
      {
	BYTE chr, chars;

	chars = *data.b++;
	for (chr = 0; chr < chars; ++chr)
	{
	  fprintf( file, "\tCharacter %d\n", chr + 1 );
	  cnt = *data.w++;
	  charfile.assign( data.c, cnt );
	  fprintf( file, "\tfile           = %.*s\n", cnt, data.c );
	  data.c += cnt;
	  fprintf( file, "\tu_byte         = %u\n", *data.b++ );
	  fprintf( file, "\tu_byte         = %u\n", *data.b++ );
	  cnt = *data.w++;
	  charname.assign( data.w, cnt );
	  char_name[charfile] = charname;
	  fprintf( file, "\tname           = %.*S\n", cnt, data.w );
	  data.w += cnt;
	  cnt = *data.w++;
	  fprintf( file, "\tdescription    = %.*S\n", cnt, data.w );
	  data.w += cnt;
	  fprintf( file, "\tdescrip_strid  = %u\n", *data.d++ );
	  fprintf( file, "\ttstamp         = %u,%u\n", data.d[0], data.d[1] );
	  data.d += 2;
	  fprintf( file, "\tship_archetype = %u\n", *data.d++ );
	  fprintf( file, "\tmoney          = %u\n", *data.d++ );
	  fprintf( file, "\tsystem         = %u\n", *data.d++ );
	  fprintf( file, "\tlast_base      = %u\n", *data.d++ );
	  fprintf( file, "\tu_dword        = %u\n", *data.d++ );
	  fprintf( file, "\tvoice          = %u\n", *data.d++ );
	  fprintf( file, "\trank           = %u\n", *data.d++ );
	  for (cnt = 0; cnt < 4; ++cnt)
	    fprintf( file, "\tu_float        = %g\n", *data.f++ );
	  fprintf( file, "\tu_dword        = %u\n", *data.d++ );
	  if (*data.b++)
	  {
	    fprintf( file, "\tcom_body       = %u\n", *data.d++ );
	    fprintf( file, "\tcom_head       = %u\n", *data.d++ );
	    fprintf( file, "\tcom_lefthand   = %u\n", *data.d++ );
	    fprintf( file, "\tcom_righthand  = %u\n", *data.d++ );
	    for (cnt = *data.d++; cnt != 0; --cnt)
	      fprintf( file, "\tcom_accessory  = %u\n", *data.d++ );
	  }
	  if (*data.b++)
	  {
	    fprintf( file, "\tbody           = %u\n", *data.d++ );
	    fprintf( file, "\thead           = %u\n", *data.d++ );
	    fprintf( file, "\tlefthand       = %u\n", *data.d++ );
	    fprintf( file, "\trighthand      = %u\n", *data.d++ );
	    for (cnt = *data.d++; cnt != 0; --cnt)
	      fprintf( file, "\taccessory      = %u\n", *data.d++ );
	  }
	  cnt = *data.b++;
	  data = items( file, data, cnt, 9 );
	  for (cnt = *data.d++; cnt > 0; --cnt)
	  {
	    data.w++;
	    fprintf( file, "\tcol_group      = %u, %g\n", data.w[-1], *data.f );
	    data.f++;
	  }
	  putc( '\n', file );
	}
	fprintf( file, "\tu_dword        = %u\n", *data.d++ );
	break;
      }

      case 0x04: // FLPACKET_SERVER_CREATESHIP
      case 0x42: // FLPACKET_SERVER_CREATESOLAR
      {
	BYTE ship = (data.b[-2] == 0x04);
	BYTE flag;

	Object_Name( file, "object", 9, *data.d++ );
	fprintf( file, "\tarchetype = %u\n", SmallIDToLargeID( *data.w++ ) );
	fprintf( file, "\tu_dword   = %u\n", *data.d++ );
	Object_Name( file, "pilot", 9, *data.d++, pilot_player );
	fprintf( file, "\tbody      = %u\n", *data.d++ );
	fprintf( file, "\thead      = %u\n", *data.d++ );
	for (cnt = *data.b++; cnt != 0; --cnt)
	  fprintf( file, "\taccessory = %u\n", *data.d++ );
	fprintf( file, "\tvoice     = %u\n", *data.d++ );
	fprintf( file, "\tpos       = %g, %g, %g\n", data.f[0], data.f[1], data.f[2] );
	data.f += 3;
	fprintf( file, "\trotate    = %g, %g, %g, %g\n", data.c[3] / 127.0,
							 data.c[0] / 127.0,
							 data.c[1] / 127.0,
							 data.c[2] / 127.0 );
	data.b += 4;
	fprintf( file, "\thealth    = %g\n", *data.b++ / 255.0 );
	for (cnt = *data.w++; cnt != 0; --cnt)
	{
	  const char* hp;
	  DWORD count, arch, hpid, hplen;
	  float health;
	  flag = *data.b++;
	  if (flag & 0x80)
	  {
	    count = 1;
	  }
	  else if (flag & 0x04)
	  {
	    count = *data.d++;
	  }
	  else
	  {
	    count = *data.b++;
	  }
	  if (flag & 0x40)
	  {
	    health = 1;
	  }
	  else
	  {
	    health = *data.b++ / 255.0f;
	  }
	  arch = SmallIDToLargeID( *data.w++ );
	  if (flag & 0x08)
	  {
	    hpid = *data.w++;
	  }
	  else
	  {
	    hpid = *data.b++;
	  }
	  if (flag & 0x20)
	  {
	    hp = "";
	    hplen = 0;
	  }
	  else if (flag & 0x10)
	  {
	    hplen = *data.b++;
	    hp = data.c;
	    data.b += hplen;
	  }
	  else
	  {
	    hplen = *data.b++;
	    if (hplen >= lenof(Hardpoint))
	      hplen = 0;
	    hp = Hardpoint[hplen];
	    hplen = strlen( hp );
	  }
	  fprintf( file, "\t%s     = %u, %.*s, %u, %g, %u, %u\n",
			 (flag & 0x01) ? "equip" : "cargo",
			 arch, hplen, hp, count, health,
			 (flag & 0x02) >> 1, hpid );
	}
	for (cnt = *data.b++; cnt != 0; --cnt)
	{
	  fprintf( file, "\tcol_group = %u, %g\n", data.b[0], data.b[1] / 255.0 );
	  data.b += 2;
	}
	if (ship)
	{
	  flag = *data.b++;
	  fprintf( file, "\tu_byte    = %u\n", flag & 1 );
	  fprintf( file, "\tu_byte    = %u\n", flag >> 1 );
	  fprintf( file, "\tu_vector  = %g, %g, %g\n", data.f[0], data.f[1], data.f[2] );
	  data.f += 3;
	  fprintf( file, "\tu_float   = %g\n", *data.c++ / 127.0 );
	  fprintf( file, "\tu_word    = %u\n", *data.w++ );
	  fprintf( file, "\tlevel     = %u\n", *data.b++ );
	  if (flag & 4)
	  {
	    fprintf( file, "\tplayer    = %u\n", *data.b++ );
	    fprintf( file, "\tu_word    = %u\n", *data.w++ );
	    cnt = *data.b++;
	    fprintf( file, "\tname      = %.*S\n", cnt, data.w );
	    data.w += cnt;
	  }
	  else
	  {
	    data.d++;
	    data = fmtstr( file, data, 1 );
	    data.d++;
	    data = fmtstr( file, data, 1 );
	  }
	  fprintf( file, "\tfaction   = %d\n", *data.i++ );
	  fprintf( file, "\trep       = %g\n", *data.c++ / 127.0 );
	}
	else
	{
	  fprintf( file, "\tu_byte    = %u\n", *data.b++ );
	  fprintf( file, "\tu_byte    = %u\n", *data.b++ );
	  fprintf( file, "\tfaction   = %d\n", *data.i++ );
	  fprintf( file, "\trep       = %g\n", *data.c++ / 127.0 );
	  data.d++;
	  data = fmtstr( file, data, 1 );
	  data.d++;
	  data = fmtstr( file, data, 1 );
	}
	break;
      }

      case 0x05: // FLPACKET_SERVER_DAMAGEOBJECT
      {
	Object_Name( file, "object", 8, *data.d++ );
	fprintf( file, "\tu_byte   = %u\n", *data.b++ );
	fprintf( file, "\tu_dword  = %u\n", *data.d++ );
	Object_Name( file, "sender", 8, *data.d++ );
	for (cnt = *data.w++; cnt != 0; --cnt)
	{
	  fprintf( file, "\tsubobjid = 0x%.4X\n", *data.w++ );
	  fprintf( file, "\tu_byte   = %u\n", *data.b++ );
	  fprintf( file, "\thit_pts  = %g\n", *data.f++ );
	}
	break;
      }

      case 0x06: // FLPACKET_SERVER_DESTROYOBJECT
      {
	Object_Name( file, "object", 6, *data.d );
	object_player.erase( *data.d++ );
	fprintf( file, "\tu_byte = %u\n", *data.b++ );
	break;
      }

      case 0x07: // FLPACKET_SERVER_LAUNCH
      {
	Object_Name( file, "object", 7, *data.d++ );
	fprintf( file, "\tbase    = %u\n", *data.d++ );
	fprintf( file, "\tu_int   = %d\n", *data.i++ );
	fprintf( file, "\tpos     = %g, %g, %g\n", data.f[0], data.f[1], data.f[2] );
	fprintf( file, "\trotate  = %g, %g, %g, %g\n",
				  data.f[3], data.f[4], data.f[5], data.f[6] );
	data.f += 7;
	break;
      }

      case 0x08: // FLPACKET_SERVER_CHARSELECTVERIFIED
      {
	pilot_player[*data.d] = player;
	Object_Name( file, "pilot", 5, *data.d++, pilot_player );
	fprintf( file, "\ttime  = %.*f\n", (*data.D < 1000) ? 6 : 3, *data.D );
	data.D++;
	break;
      }

      case 0x0A: // FLPACKET_SERVER_ACTIVATEOBJECT
      {
	fprintf( file, "\tobject   = %u\n", *data.d++ );
	fprintf( file, "\tactivate = %s\n", (*data.b++) ? "yes" : "no" );
	Object_Name( file, "object", 8, *data.d++ );
	break;
      }

      case 0x0B: // FLPACKET_SERVER_LAND
      {
	Object_Name( file, "object", 6, *data.d++ );
	fprintf( file, "\ttarget = %u\n", *data.d++ );
	fprintf( file, "\tbase   = %u\n", *data.d++ );
	break;
      }

      case 0x0D: // FLPACKET_SERVER_SETSTARTROOM
      {
	fprintf( file, "\tbase = %u\n", *data.d++ );
	fprintf( file, "\troom = %u\n", *data.d++ );
	break;
      }

      case 0x0E: // FLPACKET_SERVER_GFCOMPLETENEWSBROADCASTLIST
      {
	fprintf( file, "\tbase = %u\n", *data.d++ );
	break;
      }

      case 0x0F: // FLPACKET_SERVER_GFCOMPLETECHARLIST
      {
	fprintf( file, "\troom = %u\n", *data.d++ );
	break;
      }

      case 0x10: // FLPACKET_SERVER_GFCOMPLETEMISSIONCOMPUTERLIST
      {
	fprintf( file, "\tbase = %u\n", *data.d++ );
	break;
      }

      case 0x11: // FLPACKET_SERVER_GFCOMPLETESCRIPTBEHAVIORLIST
      case 0x13: // FLPACKET_SERVER_GFCOMPLETEAMBIENTSCRIPTLIST
      {
	fprintf( file, "\troom = %u\n", *data.d++ );
	break;
      }

      case 0x14: // FLPACKET_SERVER_GFDESTROYNEWSBROADCAST
      {
	fprintf( file, "\tbase   = %u\n", *data.d++ );
	fprintf( file, "\tnewsid = %u\n", *data.d++ );
	break;
      }

      case 0x15: // FLPACKET_SERVER_GFDESTROYCHARACTER
      {
	fprintf( file, "\troom   = %u\n", *data.d++ );
	fprintf( file, "\tcharid = %u\n", *data.d++ );
	break;
      }

      case 0x16: // FLPACKET_SERVER_GFDESTROYMISSIONCOMPUTER
      {
	fprintf( file, "\tbase    = %u\n", *data.d++ );
	fprintf( file, "\tcounter = %u\n", *data.d++ );
	break;
      }

      case 0x17: // FLPACKET_SERVER_GFDESTROYSCRIPTBEHAVIOR
      {
	fprintf( file, "\troom       = %u\n", *data.d++ );
	fprintf( file, "\tbehaviorid = %u\n", *data.d++ );
	break;
      }

      case 0x19: // FLPACKET_SERVER_GFDESTROYAMBIENTSCRIPT
      break;

      case 0x1A: // FLPACKET_SERVER_GFSCRIPTBEHAVIOR
      {
	DWORD count;
	DWORD talk;

	fprintf( file, "\tsize       = %u\n", *data.d++ );
	fprintf( file, "\tbehaviorid = %u\n", *data.d++ );
	fprintf( file, "\troom       = %u\n", *data.d++ );
	fprintf( file, "\tu_bool     = %u, %u, %u\n", data.b[0], data.b[1], data.b[2] );
	data.b += 3;
	fprintf( file, "\tu_dword    = %u, %u\n", data.d[0], data.d[1] );
	data.d += 2;
	fprintf( file, "\tu_bool     = %u\n", *data.b++ );
	for (count = *data.d++; count != 0; --count)
	{
	  fprintf( file, "\tunknown    = %u, %.*s\n", data.d[0], data.d[1], data.c+8 );
	  data.b += 8 + data.d[1];
	}
	for (count = *data.d++; count != 0; --count)
	{
	  fprintf( file, "\tscript     = %.*s, ", *data.d, data.c+4 );
	  data.b += 4 + *data.d;
	  fprintf( file, "%u\n", *data.d++ );
	}
	talk = *data.d;
	if (talk >= lenof(TalkType))
	  talk = 0;
	fprintf( file, "\ttalk       = %u (%s)\n", *data.d++, TalkType[talk] );
	fprintf( file, "\tcharid     = %u\n", *data.d++ );
	fprintf( file, "\tu_dword    = %u\n", *data.d++ );
	data = fmtstr( file, data, 2 );
	data = fmtstr( file, data, 2 );
	fprintf( file, "\tu_dword    = %u\n", *data.d++ );
	break;
      }

      case 0x1D: // FLPACKET_SERVER_GFUPDATEMISSIONCOMPUTER
      {
	DWORD type;

	fprintf( file, "\tsize     = %u\n", *data.d++ );
	fprintf( file, "\tcounter  = %u\n", *data.d++ );
	fprintf( file, "\tbase     = %u\n", *data.d++ );
	fprintf( file, "\tindex    = %u\n", *data.d++ );
	fprintf( file, "\tu_dword  = %u\n", *data.d++ );
	type = *data.d;
	if (type >= lenof(JobType))
	  type = 0;
	fprintf( file, "\ttype     = %u (%s)\n", *data.d++, JobType[type] );
	data = fmtstr( file, data, 0 );
	data = fmtstr( file, data, 0);
	data = fmtstr( file, data, 0 );
	fprintf( file, "\treward   = %u\n", *data.d++ );
	break;
      }

      case 0x1E: // FLPACKET_SERVER_GFUPDATENEWSBROADCAST
      {
	DWORD icon;

	fprintf( file, "\tsize     = %u\n", *data.d++ );
	fprintf( file, "\tnewsid   = %u\n", *data.d++ );
	fprintf( file, "\tbase     = %u\n", *data.d++ );
	fprintf( file, "\tu_word   = %u\n", *data.w++ );
	icon = *data.d;
	if (icon >= lenof(NewsIcon))
	  icon = 0;
	fprintf( file, "\ticon     = %u (%s)\n", *data.d++, NewsIcon[icon] );
	fprintf( file, "\tcategory = %u\n", *data.d++ );
	*data.w++; // ignore format count
	fprintf( file, "\theadline = %u\n", *data.d++ );
	*data.w++; // ignore format count
	fprintf( file, "\ttext     = %u\n", *data.d++ );
	*data.w++; // ignore format count
	fprintf( file, "\tlogo     = %.*s\n", *data.i, data.c+4 );
	data.b += 4 + *data.i;
	fprintf( file, "\tu_dword  = %u\n", *data.d++ );
	break;
      }

      case 0x1F: // FLPACKET_SERVER_GFUPDATEAMBIENTSCRIPT
      break;

      case 0x20: // FLPACKET_SERVER_GFMISSIONVENDORACCEPTANCE
      {
	fprintf( file, "\tsize    = %u\n", *data.d++ );
	fprintf( file, "\tcounter = %u\n", *data.d++ );
	fprintf( file, "\tbase    = %u\n", *data.d++ );
	fprintf( file, "\tu_byte  = %u\n", *data.b++ );
	fprintf( file, "\tu_word  = %u\n", *data.w++ );
	fprintf( file, "\tu_dword = %u\n", *data.d++ );
	break;
      }

      case 0x21: // FLPACKET_SERVER_SYSTEM_SWITCH_OUT
      {
	Object_Name( file, "object", 6, *data.d++ );
	fprintf( file, "\ttarget = %u\n", *data.d++ );
	if (size == 12) // Random Jumps plugin
	  fprintf( file, "\tsystem = %u\n", *data.d++ );
	break;
      }

      case 0x22: // FLPACKET_SERVER_SYSTEM_SWITCH_IN
      {
	Object_Name( file, "object", 7, *data.d++ );
	fprintf( file, "\tu_dword = %u\n", *data.d++ );
	fprintf( file, "\tpos     = %g, %g, %g\n", data.f[0], data.f[1], data.f[2] );
	fprintf( file, "\trotate  = %g, %g, %g, %g\n",
				  data.f[3], data.f[4], data.f[5], data.f[6] );
	data.f += 7;
	break;
      }

      case 0x23: // FLPACKET_SERVER_SETSHIPARCH
      {
	fprintf( file, "\tshiparch = %u\n", *data.d++ );
	break;
      }

      case 0x24: // FLPACKET_SERVER_SETEQUIPMENT
      {
	DWORD count = *data.w++;
	data = items( file, data, count, 0 );
	break;
      }

      case 0x25: // FLPACKET_SERVER_SETCARGO
      break;

      case 0x26: // FLPACKET_SERVER_GFUPDATECHAR
      {
	DWORD accessories;

	fprintf( file, "\tsize       = %u\n", *data.d++ );
	fprintf( file, "\tcharid     = %u\n", *data.d++ );
	fprintf( file, "\tplayer     = %s\n", (*data.d++) ? "yes" : "no" );
	fprintf( file, "\troom       = %u\n", *data.d++ );
	fprintf( file, "\tname       = %u\n", *data.d++ );
	fprintf( file, "\tfaction    = %d\n", *data.i++ );
	if (*data.i++ != -1)
	{
	  fprintf( file, "\tname       = %.*S\n", data.i[-1], data.w );
	  data.w += data.i[-1];
	}
	fprintf( file, "\thead       = %u\n", *data.d++ );
	fprintf( file, "\tbody       = %u\n", *data.d++ );
	fprintf( file, "\tlefthand   = %u\n", *data.d++ );
	fprintf( file, "\trighthand  = %u\n", *data.d++ );
	for (accessories = *data.d++; accessories != 0; --accessories)
	  fprintf( file, "\taccessory  = %u\n", *data.d++ );
	fprintf( file, "\tscript     = %.*s\n", *data.d, data.c+4 );
	data.b += 4 + *data.d;
	fprintf( file, "\tbehaviorid = %d\n", *data.i++ );
	if (*data.i++ != -1)
	{
	  fprintf( file, "\tlocation   = %.*s\n", data.i[-1], data.c );
	  data.b += data.i[-1];
	}
	fprintf( file, "\tplayer     = %s\n", (*data.d++) ? "yes" : "no" );
	fprintf( file, "\tposture    = %s\n", (*data.d++) ? "sitlow" : "stand" );
	fprintf( file, "\tvoice      = %u\n", *data.d++ );
	break;
      }

      case 0x27: // FLPACKET_SERVER_REQUESTCREATESHIPRESP
      {
	fprintf( file, "\tu_byte = %u\n", *data.b++ );
	object_player[*data.d] = player;
	Object_Name( file, "object", 6, *data.d++ );
	break;
      }

      case 0x28: // FLPACKET_SERVER_CREATELOOT
      {
	Object_Name( file, "parent", 10, *data.d++ );
	fprintf( file, "\tu_byte     = %u\n", *data.b++ );
	for (cnt = *data.b++; cnt != 0; --cnt)
	{
	  fprintf( file, "\tobject     = %u\n", *data.d++ );
	  fprintf( file, "\tloot       = %u\n", SmallIDToLargeID( *data.w++ ) );
	  fprintf( file, "\thit_pts    = %g\n", *data.f++ );
	  fprintf( file, "\tquantity   = %u\n", *data.w++ );
	  fprintf( file, "\tappearance = %u\n", SmallIDToLargeID( *data.w++ ) );
	  fprintf( file, "\thit_pts    = %g\n", *data.f++ );
	  fprintf( file, "\tu_float    = %g\n", *data.f++ );
	  fprintf( file, "\tpos        = %g, %g, %g\n", data.f[0], data.f[1], data.f[2] );
	  data.f += 3;
	  fprintf( file, "\tmission    = %u, %u\n", data.b[0], data.b[1] );
	  data.b += 2;
	}
	break;
      }

      case 0x29: // FLPACKET_SERVER_SETREPUTATION
      {
	BYTE fac = *data.b++;
	fprintf( file, "\tmask    = 0x%.2X\n", fac );
	fprintf( file, "\tobject  = %u\n", *data.d++ );
	fprintf( file, "\tfaction = %d\n", (fac & 1) ? *data.i++ : -1 );
	fprintf( file, "\trep     = %g\n", *data.f++ );
	break;
      }

      case 0x2A: // FLPACKET_SERVER_ADJUSTATTITUDE
      case 0x2B: // FLPACKET_SERVER_SETGROUPFEELINGS
      break;

      case 0x2C: // FLPACKET_SERVER_CREATEMINE
      {
	fprintf( file, "\tobject    = %u\n", *data.d++ );
	fprintf( file, "\tarchetype = %u\n", SmallIDToLargeID( *data.w++ ) );
	Object_Name( file, "parent", 9, *data.d++ );
	fprintf( file, "\thpid      = %u\n", *data.w++ );
	fprintf( file, "\tpos       = %g, %g, %g\n", data.f[0], data.f[1], data.f[2] );
	data.f += 3;
	break;
      }

      case 0x2D: // FLPACKET_SERVER_CREATECOUNTER
      {
	fprintf( file, "\tarchetype = %u\n", *data.d++ );
	fprintf( file, "\tobject    = %u\n", *data.d++ );
	fprintf( file, "\tu_dword   = %u\n", *data.d++ );
	Object_Name( file, "parent", 9, *data.d++ );
	fprintf( file, "\thpid      = %u\n", *data.w++ );
	fprintf( file, "\trotate    = %g, %g, %g, %g\n",
				  data.f[0], data.f[1], data.f[2], data.f[3] );
	fprintf( file, "\tpos       = %g, %g, %g\n", data.f[4], data.f[5], data.f[6] );
	fprintf( file, "\tu_vector  = %g, %g, %g\n", data.f[7], data.f[8], data.f[9] );
	fprintf( file, "\tu_vector  = %g, %g, %g\n", data.f[10], data.f[11], data.f[12] );
	data.f += 13;
	break;
      }

      case 0x2E: // FLPACKET_SERVER_SETADDITEM
      {
	fprintf( file, "\tgood     = %u\n", *data.d++ );
	fprintf( file, "\thpid     = %u\n", *data.d++ );
	fprintf( file, "\tquantity = %u\n", *data.d++ );
	fprintf( file, "\thealth   = %g\n", *data.f++ );
	fprintf( file, "\ttype     = %s\n", (*data.d++) ? "equip" : "cargo" );
	fprintf( file, "\tu_word   = 0x%.4X\n", *data.w++ );
	if (*data.d++ != 0)
	  fprintf( file, "\thp       = %s\n", data.c ); // length includes NUL
	data.b += data.d[-1];
	break;
      }

      case 0x2F: // FLPACKET_SERVER_SETREMOVEITEM
      {
	fprintf( file, "\thpid    = %u\n", *data.d++ );
	fprintf( file, "\tu_dword = %u\n", *data.d++ );
	fprintf( file, "\tu_dword = %u\n", *data.d++ );
	break;
      }

      case 0x30: // FLPACKET_SERVER_SETCASH
      {
	fprintf( file, "\tmoney = %d\n", *data.i++ );
	break;
      }

      case 0x31: // FLPACKET_SERVER_EXPLODEASTEROIDMINE
      {
	fprintf( file, "\tasteroid = %u\n", *data.d++ );
	fprintf( file, "\tpos      = %g, %g, %g\n", data.f[0], data.f[1], data.f[2] );
	data.f += 3;
	break;
      }

      case 0x32: // FLPACKET_SERVER_REQUESTSPACESCRIPT
      case 0x33: // FLPACKET_SERVER_SETMISSIONOBJECTIVESTATE
      case 0x34: // FLPACKET_SERVER_REPLACEMISSIONOBJECTIVE
      break;

      case 0x35: // FLPACKET_SERVER_SETMISSIONOBJECTIVES
      {
	fprintf( file, "\tu_dword  = %u\n", *data.d++ );
	cnt = *data.d++;
	fprintf( file, "\tu_dword  = %u\n", *data.d++ );
	data.d++;
	data = fmtstr( file, data, 0 );
	data.d++;
	data = fmtstr( file, data, 0 );
	while (cnt-- != 0)
	{
	  fprintf( file, "\tu_byte   = 0x%.2X (%u)\n", *data.b, *data.b );
	  data.b++;
	  data.d++;
	  data = fmtstr( file, data, 0 );
	}
	break;
      }

      case 0x37: // FLPACKET_SERVER_CREATEGUIDED
      {
	fprintf( file, "\tarchetype = %u\n", SmallIDToLargeID( *data.w++ ) );
	Object_Name( file, "parent", 9, *data.d++ );
	fprintf( file, "\thpid      = %u\n", *data.w++ );
	Object_Name( file, "target", 9, *data.d++ );
	fprintf( file, "\tsubobjid  = %u\n", *data.w++ );
	fprintf( file, "\tu_dword   = %u\n", *data.d++ );
	fprintf( file, "\tpos       = %g, %g, %g\n", data.f[0], data.f[1], data.f[2] );
	data.f += 3;
	fprintf( file, "\trotate    = %g, %g, %g, %g\n", data.c[3] / 127.0,
							 data.c[0] / 127.0,
							 data.c[1] / 127.0,
							 data.c[2] / 127.0 );
	data.b += 4;
	break;
      }

      case 0x38: // FLPACKET_SERVER_ITEMTRACTORED
      {
	fprintf( file, "\tobject = %u\n", *data.d++ );
	Object_Name( file, "owner", 6, *data.d++ );
	fprintf( file, "\tu_word = %u\n", *data.w++ );
	fprintf( file, "\tu_word = %u\n", *data.w++ );
	break;
      }

      case 0x39: // FLPACKET_SERVER_SCANNOTIFY
      {
	Object_Name( file, "object", 6, *data.d++ );
	fprintf( file, "\tgood   = %u\n", *data.d++ );
	break;
      }

      case 0x3C: // FLPACKET_SERVER_REPAIROBJECT
      break;

      case 0x3D: // FLPACKET_SERVER_REMOTEOBJECTCARGOUPDATE
      {
	Object_Name( file, "object", 6, *data.d++ );
	fprintf( file, "\tu_byte = %u\n", *data.b++ );
	cnt = *data.w++;
	data = items( file, data, cnt, 1 );
      }

      case 0x3E: // FLPACKET_SERVER_SETNUMKILLS
      case 0x3F: // FLPACKET_SERVER_SETMISSIONSUCCESSES
      case 0x40: // FLPACKET_SERVER_SETMISSIONFAILURES
      break;

      case 0x41: // FLPACKET_SERVER_BURNFUSE
      {
	Object_Name( file, "object", 7, *data.d++ );
	Object_Name( file, "object", 7, *data.d++ );
	fprintf( file, "\tfuse    = %u\n", *data.d++ );
	fprintf( file, "\tu_word  = %u\n", *data.w++ );
	fprintf( file, "\tu_dword = %u\n", *data.d++ );
	fprintf( file, "\tu_float = %g\n", *data.f++ );
	fprintf( file, "\tu_byte  = %u\n", *data.b++ );
	break;
      }

      case 0x43: // FLPACKET_SERVER_SET_STORY_CUE
      break;

      case 0x44: // FLPACKET_SERVER_REQUEST_RETURNED
      {
	Object_Name( file, "object", 6, *data.d++ );
	fprintf( file, "\tu_byte = %u\n", *data.b++ );
	fprintf( file, "\tu_word = %u\n", *data.w++ );
	break;
      }

      case 0x45: // FLPACKET_SERVER_SET_MISSION_MESSAGE
      {
	fprintf( file, "\tu_dword  = %u\n", *data.d++ );
	data.d++;
	data = fmtstr( file, data, 0 );
	break;
      }

      case 0x46: // FLPACKET_SERVER_MARKOBJ
      {
	Object_Name( file, "object", 6, *data.d++ );
	fprintf( file, "\tmark   = %s\n", (*data.b++) ? "on" : "off" );
	break;
      }

      case 0x47: // FLPACKET_SERVER_CFGINTERFACENOTIFICATION
      break;

      case 0x48: // FLPACKET_SERVER_SETCOLLISIONGROUPS
      {
	for (cnt = *data.w++; cnt != 0; --cnt)
	{
	  fprintf( file, "\tgroup  = %u\n", *data.w++ );
	  fprintf( file, "\thealth = %g\n", *data.f++ );
	}
	break;
      }

      case 0x49: // FLPACKET_SERVER_SETHULLSTATUS
      {
	fprintf( file, "\thealth = %g\n", *data.f++ );
	break;
      }

      case 0x4A: // FLPACKET_SERVER_SETGUIDEDTARGET
      case 0x4B: // FLPACKET_SERVER_SET_CAMERA
      case 0x4C: // FLPACKET_SERVER_REVERT_CAMERA
      case 0x4D: // FLPACKET_SERVER_LOADHINT
      break;

      case 0x4E: // FLPACKET_SERVER_SETDIRECTIVE
      {
	fprintf( file, "\tu_dword  = %u\n", *data.d++ );
	fprintf( file, "\tu_dword  = %u\n", *data.d++ );
	fprintf( file, "\tu_bool   = %u\n", *data.d++ & 0xFF ); // padded
	fprintf( file, "\tu_dword  = %u\n", *data.d++ );
	fprintf( file, "\tu_dword  = %u\n", *data.d++ );
	fprintf( file, "\tu_dword  = %u\n", *data.d++ );
	fprintf( file, "\tu_dword  = %u\n", *data.d++ );
	fprintf( file, "\tu_dword  = %u\n", *data.d++ );
	fprintf( file, "\tu_float  = %g\n", *data.f++ );
	fprintf( file, "\tu_float  = %g\n", *data.f++ );
	fprintf( file, "\tu_dword  = %u\n", *data.d++ );
	fprintf( file, "\tu_dword  = %u\n", *data.d++ );
	fprintf( file, "\tu_vector = %g, %g, %g\n", data.f[0], data.f[1], data.f[2] );
	data.f += 3;
	fprintf( file, "\tu_dword  = %u\n", *data.d++ );
	fprintf( file, "\tu_dword  = %u\n", *data.d++ );
	fprintf( file, "\tu_vector = %g, %g, %g\n", data.f[0], data.f[1], data.f[2] );
	data.f += 3;
	fprintf( file, "\tu_dword  = %u\n", *data.d++ );
	fprintf( file, "\tu_vector = %g, %g, %g\n", data.f[0], data.f[1], data.f[2] );
	data.f += 3;
	fprintf( file, "\tu_vector = %g, %g, %g\n", data.f[0], data.f[1], data.f[2] );
	data.f += 3;
	fprintf( file, "\tu_vector = %g, %g, %g\n", data.f[0], data.f[1], data.f[2] );
	data.f += 3;
	fprintf( file, "\tu_vector = %g, %g, %g\n", data.f[0], data.f[1], data.f[2] );
	data.f += 3;
	fprintf( file, "\tu_dword  = %u\n", *data.d++ );
	fprintf( file, "\tu_dword  = %u\n", *data.d++ );
	fprintf( file, "\tu_bool   = %u\n", *data.b++ );
	fprintf( file, "\tu_bool   = %u\n", *data.b++ );
	fprintf( file, "\tu_bool   = %u\n", *data.b++ );
	fprintf( file, "\tu_bool   = %u\n", *data.b++ );
	fprintf( file, "\tu_bool   = %u\n", *data.d++ & 0xFF ); // padded
	break;
      }

      case 0x4F: // FLPACKET_SERVER_SENDCOMM
      {
	Object_Name( file, "from", 9, *data.d++ );
	Object_Name( file, "to",   9, *data.d++ );
	fprintf( file, "\tvoice     = %u\n", *data.d++ );
	if (*data.b++)
	{
	  fprintf( file, "\thead      = %u\n", *data.d++ );
	  fprintf( file, "\tbody      = %u\n", *data.d++ );
	  for (cnt = *data.b++; cnt != 0; --cnt)
	    fprintf( file, "\taccessory = %u\n", *data.d++ );
	}
	fprintf( file, "\tu_dword   = %u\n", *data.d++ );
	for (cnt = *data.b++; cnt != 0; --cnt)
	  fprintf( file, "\tline      = %u\n", *data.d++ );
	fprintf( file, "\tresource  = %u\n", *data.w++ );
	fprintf( file, "\tu_float   = %g\n", *data.b++ / 255.0 * 10 );
	break;
      }

      case 0x51: // FLPACKET_SERVER_USE_ITEM
      {
	Object_Name( file, "object", 8, *data.d++ );
	fprintf( file, "\thpid     = %u\n", *data.w++ );
	fprintf( file, "\tquantity = %u\n", *data.w++ );
	break;
      }

      case 0x52: // FLPACKET_SERVER_PLAYERLIST
      {
	fprintf( file, "\tcommand = %u", *data.d++ );
	if (data.d[-1] == 1)
	  fputs( " (new)", file );
	else if (data.d[-1] == 2)
	  fputs( " (depart)", file );
	putc( '\n', file );
	fprintf( file, "\tplayer  = " );
	Player_Name( file, *data.d++ );
	putc( '\n', file );
	fprintf( file, "\tcurrent = %s\n", (*data.b++) ? "yes" : "no" );
	if (*data.b++ != 0)
	  fprintf( file, "\tname    = %S\n", data.w ); // length includes NUL
	data.w += data.b[-1];
	break;
      }

      case 0x53: // FLPACKET_SERVER_FORMATION_UPDATE
      {
	Object_Name( file, "object", 6, *data.d++ );
	fprintf( file, "\tpos    = %g, %g, %g\n", data.f[0], data.f[1], data.f[2] );
	data.f += 3;
	break;
      }

      case 0x54: // FLPACKET_SERVER_MISCOBJUPDATE
      {
	DWORD flag = *data.w++;
	fprintf( file, "\tflag    = 0x%.4X\n", flag );

	if (flag & 0x08)
	  Object_Name( file, "object", 7, *data.d++ );
	if (flag & 0x04)
	{
	  fprintf( file, "\tplayer  = " );
	  Player_Name( file, *data.d++ );
	  putc( '\n', file );
	}
	if (flag & 0x01)
	  fprintf( file, "\tu_dword = %u\n", *data.d++ );
	if (flag & 0x02)
	{
	  fprintf( file, "\tu_dword = %u\n", *data.d++ );
	  fprintf( file, "\tu_dword = %u\n", *data.d++ );
	}
	if (flag & 0x10)
	{
	  DWORD len = *data.w++;
	  if (len == 0)
	    fprintf( file, "\tnews    = <empty>\n" );
	  else
	    fprintf( file, "\tnews    = %.*S\n", len, data.w );
	  data.w += len;
	}
	if (flag & 0x20)
	  fprintf( file, "\tfaction = %d\n", *data.i++ );
	if (flag & 0x40)
	  fprintf( file, "\tu_word  = %u\n", *data.w++ );
	if (flag & 0x80)
	  fprintf( file, "\tsystem  = %u\n", *data.d++ );
	if (flag & 0x100)
	{
	  for (cnt = *data.b++; cnt != 0; --cnt)
	    Object_Name( file, "object", 7, *data.d++ );
	}
	break;
      }

      case 0x55: // FLPACKET_SERVER_OBJECTCARGOUPDATE
      {
	Object_Name( file, "object", 9, *data.d++ );
	fprintf( file, "\tarchetype = %u\n", *data.d++ );
	fprintf( file, "\thpid      = %u\n", *data.w++ );
	fprintf( file, "\tquantity  = %u\n", *data.w++ );
	fprintf( file, "\thealth    = %g\n", *data.f++ );
	fprintf( file, "\tu_byte    = %u\n", *data.b++ );
	break;
      }

      case 0x56: // FLPACKET_SERVER_SENDNNMESSAGE
      {
	fprintf( file, "\tmessage = %u\n", *data.d++ );
	break;
      }

      case 0x57: // FLPACKET_SERVER_SET_MUSIC
      {
	fprintf( file, "\tu_dword = %u\n", *data.d++ );
	fprintf( file, "\tu_dword = %u\n", *data.d++ );
	fprintf( file, "\tu_dword = %u\n", *data.d++ );
	fprintf( file, "\tmusic   = %u\n", *data.d++ );
	fprintf( file, "\tu_byte  = %u\n", *data.b++ );
	fprintf( file, "\tu_float = %g\n", *data.f++ );
	break;
      }

      case 0x58: // FLPACKET_SERVER_CANCEL_MUSIC
      case 0x59: // FLPACKET_SERVER_PLAY_SOUND_EFFECT
      break;

      case 0x5A: // FLPACKET_SERVER_GFMISSIONVENDORWHYEMPTY
      {
	fprintf( file, "\treason = %u\n", *data.b++ );
	break;
      }

      case 0x5B: // FLPACKET_SERVER_MISSIONSAVEA
      break;
    }
    break;

    case 3: switch (data.b[-2])
    {
      case 0x01: // FLPACKET_CLIENT_LOGIN
      {
	fprintf( file, "\tu_byte  = %u\n", *data.b++ );
	fprintf( file, "\taccount = %.*S\n", *data.w, data.w+1 );
	data.w += 1 + *data.w;
	break;
      }

      case 0x03: // FLPACKET_CLIENT_MUNCOLLISION
      {
	fprintf( file, "\tmunition = %u\n", *data.d++ );
	Object_Name( file, "object", 8, *data.d++ );
	Object_Name( file, "target", 8, *data.d++ );
	fprintf( file, "\tsubobjid = 0x%.4X\n", *data.w++ );
	fprintf( file, "\tpos      = %g, %g, %g\n", data.f[0], data.f[1], data.f[2] );
	data.f += 3;
	break;
      }

      case 0x04: // FLPACKET_CLIENT_REQUESTLAUNCH
      {
	Object_Name( file, "object", 6, *data.d++ );
	break;
      }

      case 0x05: // FLPACKET_CLIENT_REQUESTCHARINFO
	// no data
      break;

      case 0x06: // FLPACKET_CLIENT_SELECTCHARACTER
      {
	if (flp_ver >= 2)
	{
	  charfile.assign( data.c+2, *data.w );
	  if (player >= player_name.size())
	    player_name.resize( player + 1 );
	  player_name[player] = char_name[charfile];
	  Player_Name( file, player );
	  putc( '\n', file );
	}
	fprintf( file, "\tchar = %.*s\n", *data.w, data.c+2 );
	data.c += 2 + *data.w;
	break;
      }

      case 0x07: // FLPACKET_CLIENT_ENTERBASE
      {
	fprintf( file, "\tbase = %u\n", *data.d++ );
	break;
      }

      case 0x08: // FLPACKET_CLIENT_REQUESTBASEINFO
      {
	fprintf( file, "\tbase = %u\n", *data.d++ );
	fprintf( file, "\ttype = %s\n", (*data.b++) ? "enter" : "exit" );
	break;
      }

      case 0x09: // FLPACKET_CLIENT_REQUESTLOCATIONINFO
      {
	fprintf( file, "\troom = %u\n", *data.d++ );
	fprintf( file, "\ttype = %s\n", (*data.b++) ? "enter" : "exit" );
	break;
      }

      case 0x0A: // FLPACKET_CLIENT_GFREQUESTSHIPINFO
      break;

      case 0x0B: // FLPACKET_CLIENT_SYSTEM_SWITCH_OUT_COMPLETE
      {
	Object_Name( file, "object", 6, *data.d++ );
	break;
      }

      case 0x0C: // FLPACKET_CLIENT_OBJCOLLISION
      {
	Object_Name( file, "object", 8, *data.d++ );
	fprintf( file, "\tsubobjid = 0x%.4X\n", *data.w++ );
	Object_Name( file, "target", 8, *data.d++ );
	fprintf( file, "\tsubobjid = 0x%.4X\n", *data.w++ );
	fprintf( file, "\tdamage   = %g\n", *data.f++ );
	break;
      }

      case 0x0D: // FLPACKET_CLIENT_EXITBASE
      {
	fprintf( file, "\tbase = %u\n", *data.d++ );
	break;
      }

      case 0x0E: // FLPACKET_CLIENT_ENTERLOCATION
      case 0x0F: // FLPACKET_CLIENT_EXITLOCATION
      {
	fprintf( file, "\troom = %u\n", *data.d++ );
	break;
      }

      case 0x10: // FLPACKET_CLIENT_REQUESTCREATESHIP
	// no data
      break;

      case 0x11: // FLPACKET_CLIENT_GFGOODSELL
      {
	fprintf( file, "\tbase     = %u\n", *data.d++ );
	fprintf( file, "\tgood     = %u\n", *data.d++ );
	fprintf( file, "\tquantity = %u\n", *data.d++ );
	break;
      }

      case 0x12: // FLPACKET_CLIENT_GFGOODBUY
      {
	fprintf( file, "\tbase     = %u\n", *data.d++ );
	fprintf( file, "\tu_dword  = %u\n", *data.d++ );
	fprintf( file, "\tgood     = %u\n", *data.d++ );
	fprintf( file, "\tquantity = %u\n", *data.d++ );
	break;
      }

      case 0x13: // FLPACKET_CLIENT_GFSELECTOBJECT
      {
	fprintf( file, "\tcharid = %u\n", *data.d++ );
	break;
      }

      case 0x14: // FLPACKET_CLIENT_MISSIONRESPONSE
      {
	fprintf( file, "\tcharid   = %u\n", *data.d++ );
	fprintf( file, "\tresponse = %u\n", *data.d++ );
	fprintf( file, "\tu_byte   = %u\n", *data.b++ );
	break;
      }

      case 0x15: // FLPACKET_CLIENT_REQSHIPARCH
      {
	fprintf( file, "\tshiparch = %u\n", *data.d++ );
	break;
      }

      case 0x16: // FLPACKET_CLIENT_REQEQUIPMENT
      {
	DWORD count = *data.w++;
	data = items( file, data, count, 0 );
	break;
      }

      case 0x17: // FLPACKET_CLIENT_REQCARGO
      break;

      case 0x18: // FLPACKET_CLIENT_REQADDITEM
      {
	fprintf( file, "\tgood     = %u\n", *data.d++ );
	fprintf( file, "\tquantity = %u\n", *data.d++ );
	fprintf( file, "\thealth   = %g\n", *data.f++ );
	fprintf( file, "\ttype     = %s\n", (*data.b++) ? "equip" : "cargo" );
	fprintf( file, "\thp       = %s\n", data.c+4 ); // length includes NUL
	data.c += 4 + *data.d;
	break;
      }

      case 0x19: // FLPACKET_CLIENT_REQREMOVEITEM
      {
	fprintf( file, "\thpid     = %u\n", *data.d++ );
	fprintf( file, "\tquantity = %u\n", *data.d++ );
	break;
      }

      case 0x1A: // FLPACKET_CLIENT_REQMODIFYITEM
      break;

      case 0x1B: // FLPACKET_CLIENT_REQSETCASH
      {
	fprintf( file, "\tmoney = %d\n", *data.i++ );
	break;
      }

      case 0x1C: // FLPACKET_CLIENT_REQCHANGECASH
      {
	fprintf( file, "\tmoney = %+d\n", *data.i++ );
	break;
      }

      case 0x1E: // FLPACKET_CLIENT_SAVEGAME
      break;

      case 0x20: // FLPACKET_CLIENT_MINEASTEROID
      {
	fprintf( file, "\tsystem    = %u\n", *data.d++ );
	fprintf( file, "\tpos       = %g, %g, %g\n", data.f[0], data.f[1], data.f[2] );
	data.f += 3;
	fprintf( file, "\tappearnce = %u\n", *data.d++ );
	fprintf( file, "\tloot      = %u\n", *data.d++ );
	fprintf( file, "\tquantity  = %u\n", *data.d++ );
	break;
      }

      case 0x22: // FLPACKET_CLIENT_DBGCREATESHIP
      case 0x23: // FLPACKET_CLIENT_DBGLOADSYSTEM
      case 0x24: // FLPACKET_CLIENT_DOCK
      case 0x25: // FLPACKET_CLIENT_DBGDESTROYOBJECT
      break;

      case 0x27: // FLPACKET_CLIENT_TRADERESPONSE
      {
	// Not sure about this one, I think it's broken.  It writes two dwords;
	// it reads the first dword as a count, which it allocates and stores,
	// but then the second dword overwrites it.  The 1.1 version where it
	// reads it is different again, where it actually reads it properly,
	// but it still writes it the same way.
	fprintf( file, "\tcount = %u\n", *data.d++ );
	fprintf( file, "\tu_int = %u\n", *data.d++ );
	break;
      }

      case 0x2B: // FLPACKET_CLIENT_CARGOSCAN
      {
	Object_Name( file, "object", 6, *data.d++ );
	Object_Name( file, "target", 6, *data.d++ );
	break;
      }

      case 0x2D: // FLPACKET_CLIENT_DBGCONSOLE
      case 0x2E: // FLPACKET_CLIENT_DBGFREESYSTEM
      break;

      case 0x2F: // FLPACKET_CLIENT_SETMANEUVER
      {
	Object_Name( file, "object", 8, *data.d++ );
	Object_Name( file, "target", 8, *data.d++ );
	DWORD maneuver = *data.d;
	if (maneuver >= lenof(ManeuverType))
	  maneuver = 0;
	fprintf( file, "\tmaneuver = %u (%s)\n", *data.d++, ManeuverType[maneuver] );
	break;
      }

      case 0x30: // FLPACKET_CLIENT_DBGRELOCATE_SHIP
      break;

      case 0x31: // FLPACKET_CLIENT_REQUEST_EVENT
      {
	fprintf( file, "\tu_byte  = %u\n", *data.b++ );
	Object_Name( file, "object", 7, *data.d++ );
	Object_Name( file, "target", 7, *data.d++ );
	fprintf( file, "\tu_dword = %u\n", *data.d++ );
	fprintf( file, "\tu_byte  = %u\n", *data.b++ );
	break;
      }

      case 0x32: // FLPACKET_CLIENT_REQUEST_CANCEL
      {
	fprintf( file, "\tu_byte = %u\n", *data.b++ );
	Object_Name( file, "object", 6, *data.d++ );
	Object_Name( file, "target", 6, *data.d++ );
	fprintf( file, "\tu_byte = %u\n", *data.b++ );
	break;
      }

      case 0x35: // FLPACKET_CLIENT_INTERFACEITEMUSED
      break;

      case 0x36: // FLPACKET_CLIENT_REQCOLLISIONGROUPS
      {
	for (cnt = *data.w++; cnt != 0; --cnt)
	{
	  fprintf( file, "\tgroup  = %u\n", *data.w++ );
	  fprintf( file, "\thealth = %g\n", *data.f++ );
	}
	break;
      }

      case 0x37: // FLPACKET_CLIENT_COMMCOMPLETE
      case 0x38: // FLPACKET_CLIENT_REQUESTNEWCHARINFO
      break;

      case 0x39: // FLPACKET_CLIENT_CREATENEWCHAR
      {
	fprintf( file, "\tname    = %.*S\n", *data.w, data.w+1 );
	data.w += 1 + *data.w;
	fprintf( file, "\tFaction = %u\n", *data.d++ );
	fprintf( file, "\tbase    = %u\n", *data.d++ );
	fprintf( file, "\tPackage = %u\n", *data.d++ );
	fprintf( file, "\tPilot   = %u\n", *data.d++ );
	break;
      }

      case 0x3A: // FLPACKET_CLIENT_DESTROYCHAR
      {
	fprintf( file, "\tfile = %.*s\n", *data.w, data.c+2 );
	data.c += 2 + *data.w;
	break;
      }

      case 0x3B: // FLPACKET_CLIENT_REQHULLSTATUS
      {
	fprintf( file, "\thealth = %g\n", *data.f++ );
	break;
      }

      case 0x3C: // FLPACKET_CLIENT_GFGOODVAPORIZED
      case 0x3D: // FLPACKET_CLIENT_BADLANDSOBJCOLLISION
      break;

      case 0x3E: // FLPACKET_CLIENT_LAUNCHCOMPLETE
      {
	fprintf( file, "\tbase   = %u\n", *data.d++ );
	Object_Name( file, "object", 6, *data.d++ );
	break;
      }

      case 0x3F: // FLPACKET_CLIENT_HAIL
      {
	Object_Name( file, "from", 6, *data.d++ );
	Object_Name( file, "to",   6, *data.d++ );
	fprintf( file, "\tsystem = %u\n", *data.d++ );
	break;
      }

      case 0x40: // FLPACKET_CLIENT_REQUEST_USE_ITEM
      {
	Object_Name( file, "object", 8, *data.d++ );
	fprintf( file, "\thpid     = %u\n", *data.w++ );
	fprintf( file, "\tquantity = %u\n", *data.w++ );
	break;
      }

      case 0x41: // FLPACKET_CLIENT_ABORT_MISSION
      case 0x42: // FLPACKET_CLIENT_SKIP_AUTOSAVE
      break;

      case 0x43: // FLPACKET_CLIENT_JUMPINCOMPLETE
      {
	fprintf( file, "\tsystem = %u\n", *data.d++ );
	Object_Name( file, "object", 6, *data.d++ );
	break;
      }

      case 0x44: // FLPACKET_CLIENT_REQINVINCIBILITY
      {
	Object_Name( file, "object", 6, *data.d++ );
	fprintf( file, "\tu_byte = %u\n", *data.b++ );
	fprintf( file, "\tu_byte = %u\n", *data.b++ );
	break;
      }

      case 0x45: // FLPACKET_CLIENT_MISSIONSAVEB
      case 0x46: // FLPACKET_CLIENT_REQDIFFICULTYSCALE
      case 0x47: // FLPACKET_CLIENT_RTCDONE
      break;
    }
    break;
  }

  for (ofs = data.b - beg, size -= ofs; size != 0; data.b += 16, ofs += 16)
  {
    int j, end;
    end = (size > 16) ? 16 : size;
    fprintf( file, "\t%.4X:", ofs );
    for (j = 0; j < end; ++j)
      fprintf( file, " %.2X", data.b[j] );
    for (; j < 16; ++j)
      fprintf( file, "   " );
    fprintf( file, "   " );
    for (j = 0; j < end; ++j)
      fprintf( file, "%c", (data.b[j] < 32) ? '.' : data.c[j] );
    putc( '\n', file );

    size -= end;
  }

  }
  catch (...)
  {
    fprintf( file, "\n\n** FIXME **\n" );
    found_error = TRUE;
  }

  putc( '\n', file );
}


void Player_Name( FILE* file, int player )
{
  fprintf( file, "%d", player );
  if (player < player_name.size())
  {
    std::wstring name = player_name[player];
    if (!name.empty())
      fprintf( file, " \"%S\"", name.c_str() );
  }
}


void Object_Name( FILE* file, const char* type, int spc, DWORD object,
		  const ObjectPlayerMap& op )
{
  fprintf( file, "\t%-*s = %u", spc, type, object );
  ObjectPlayerCIter iter = op.find( object );
  if (iter != op.end() && iter->second < player_name.size())
    fprintf( file, " \"%S\"", player_name[iter->second].c_str() );
  putc( '\n', file );
}


void ChannelStr( FILE* file, const char* type, int spc, DWORD channel )
{
  char buf[32];
  const char* chn = NULL;

  if (channel == 0)
  {
    chn = Channel[0];
  }
  else if ((channel >> 16) != 0)
  {
    if ((channel >> 16) != 1 || (channel & 0xFFFF) >= lenof(Channel))
    {
      sprintf( buf, "0x%.8X", channel );
      chn = buf;
    }
    else
    {
      chn = Channel[channel & 0xFFFF];
    }
  }

  fprintf( file, "\t%-*s = ", spc, type );
  if (chn != NULL)
    fputs( chn, file );
  else
    Player_Name( file, (int)channel );
  putc( '\n', file );
}


union cPtrs_t items( FILE* file, union cPtrs_t data, DWORD len, int spc )
{
  for (; len != 0; --len)
  {
    fprintf( file, "\t%-*s = %u, %s, %d, %g, %u, %u\n",
		     5 + spc, (data.b[14]) ? "equip" : "cargo",
		     data.d[2],
		     (data.w[8] == 0) ? "" : data.c+18,
		     *data.i,
		     data.f[1],
		     data.b[15],
		     data.w[6] );
    data.b += 18 + data.w[8];
  }

  return data;
}


union cPtrs_t fmtstr( FILE* file, union cPtrs_t data, int spc )
{
  int count;
  wchar_t type;

  fprintf( file, "\tresource%*c= %u\n", 1 + spc, ' ', *data.d++ );
  for (count = *data.w++; count != 0; --count)
  {
    type = *data.w++;
    fprintf( file, "\ttype%*c= %%%C%d", 5 + spc, ' ', type, *data.b++ );
    if (type == '!')
    {
      fputc( '\n', file );
      data = fmtstr( file, data, spc );
    }
    else if (type == 'N')
    {
      fprintf( file, " -> %u, %u, %g, %g, %g\n",
		     data.d[0], data.d[1], data.f[2], data.f[3], data.f[4] );
      data.d += 5;
    }
    else
    {
      fprintf( file, " -> %u\n", *data.d++ );
    }
  }

  return data;
}


int main( int argc, char* argv[] )
{
  FILE*  in;
  size_t size;
  FILE*  out;
  char	 outname[_MAX_PATH];
  size_t len, max, read;
  BYTE*  buf;
  int	 redirect, multiple;
  long	 offset;
  const char* exclude_file;

  if (argc == 1 || strcmp( argv[1], "/?" ) == 0
		|| strcmp( argv[1], "--help" ) == 0)
  {
#ifdef _WIN32
    BOOL gui = FALSE;
    if (argc == 1)
    {
      // Simple test to see if we were run from a GUI window.
      CONSOLE_SCREEN_BUFFER_INFO csbi;
      if (GetConsoleScreenBufferInfo( GetStdHandle( STD_OUTPUT_HANDLE ), &csbi )
	  && csbi.dwCursorPosition.X == 0 && csbi.dwCursorPosition.Y == 0)
      {
	gui = TRUE;
      }
    }
#endif
    puts( "PacketDump by Jason Hood <jadoxa@yahoo.com.au>.\n"
	  "Version " PVERS " (" PDATE ").  Freeware.\n"
	  "http://freelancer.adoxa.cjb.net/\n"
	  "\n"
	  "Generate a textual representation of PacketLog data files.\n"
	  "\n"
	  "packetdump [-i] [-x FILE] FILE...\n"
	  "\n"
	  "-i\tignore the default exclusion file, include everything\n"
	  "-x\tignore the packets in this FILE\n"
	  "\n"
	  "The default exclusion file is \"packets-excluded.txt\" in the current\n"
	  "directory.  If FILE ends with \".bin\", it will be replaced with \".txt\",\n"
	  "otherwise \".txt\" will be appended.  To convert \"small\" ids to \"large\"\n"
	  "ids, \"LargeID.dat\" is read from the same directory as the first file."
	);
#ifdef _WIN32
    if (gui)
    {
      puts( "\nPress any key to exit (try running from Command Prompt)." );
      while (_kbhit()) _getch();
      _getch();
    }
#endif
    return 0;
  }

  exclude_file = default_exclude_file;
  while (argv[1] != NULL && *argv[1] == '-')
  {
    if (argv[1][1] == 'i')
    {
      exclude_file = NULL;
    }
    else if (argv[1][1] == 'x')
    {
      if (argv[2] != NULL)
	exclude_file = (++argv)[1];
    }
    ++argv;
  }

  redirect = !isatty( 1 );
  multiple = (argv[1] != NULL && argv[2] != NULL);
  out = stdout;
  buf = NULL;
  max = 0;

  if (exclude_file != NULL)
    exclude( exclude_file );

  if (argv[1] != NULL)
  {
    char* path;
    char* slash = argv[1];
    for (path = argv[1]; *path != '\0'; ++path)
      if (*path == '/' || *path == '\\')
	slash = path + 1;
    sprintf( outname, "%.*sLargeID.dat", slash - argv[1], argv[1] );
    in = fopen( outname, "rb" );
    if (in != NULL)
    {
      fseek( in, 0, SEEK_END );
      len = ftell( in );
      rewind( in );
      LargeID = (DWORD*)malloc( len );
      if (LargeID == NULL)
      {
	fprintf( stderr, "Failed to allocate %u bytes of memory.\n", len );
	return 2;
      }
      fread( LargeID, len, 1, in );
      fclose( in );
      LargeID_cnt = len / sizeof(DWORD);
    }
  }

  // Insert a dummy entry, making a one-based array.
  player_name.push_back( L"" );

  while (*++argv != NULL)
  {
    in = fopen( *argv, "rb" );
    if (in == NULL)
    {
      fprintf( stderr, "Unable to open \"%s\".\n", *argv );
      continue;
    }
    flp_size = 0;
    if (getc( in ) == 'F' && getc( in ) == 'L' && getc( in ) == 'P')
    {
       flp_ver = getc( in ) - '0';
       switch (flp_ver)
       {
	 case 1: flp_size = 6; break;
	 case 2: flp_size = 8; break;
       }
    }
    if (flp_size == 0)
    {
      fprintf( stderr, "Invalid file \"%s\".\n", *argv );
      fclose( in );
      continue;
    }
    fseek( in, 0, SEEK_END );
    size = ftell( in ) - 4;
    fseek( in, 4, SEEK_SET );

    if (!redirect)
    {
      len = strlen( *argv );
      if (len >= 4 && _stricmp( *argv + len - 4, ".bin" ) == 0)
	len -= 4;
      _snprintf( outname, sizeof(outname), "%.*s.txt", len, *argv );

      out = fopen( outname, "w" );
      if (out == NULL)
      {
	fprintf( stderr, "Unable to create \"%s\".\n", outname );
	fclose( in );
	continue;
      }
    }
    else if (multiple)
    {
      printf( "; %s\n\n", *argv );
    }

    found_error = FALSE;
    while (fread( &len, sizeof(len), 1, in ) == 1)
    {
      if (len == 0) // excluded packet, skip the player
      {
	getc( in );
	getc( in );
	continue;
      }
      if (len > max)
      {
	max = len;
	if (buf != NULL)
	  free( buf );
	buf = (BYTE*)malloc( len + flp_size );
	if (buf == NULL)
	{
	  fprintf( stderr, "Failed to allocate %u bytes of memory.\n", len + flp_size );
	  break;
	}
      }
      offset = ftell( in );
      if ((read = fread( buf, 1, len + flp_size, in )) != len + flp_size)
      {
	fprintf( stderr, "Expected %u bytes at offset 0x%X, but got %u.\n",
			 len + flp_size, offset, read );
	break;
      }
      Packet_Dump( out, in, buf, len, offset + flp_size + 2 );
    }
    if (found_error)
    {
      fprintf( stderr, "%s: Errors present - search for \"FIXME\".\n",
		       *argv );
    }

    if (!redirect)
    {
      fclose( out );
    }
    else if (multiple && argv[1] != NULL)
    {
      putchar( '\n' );
    }
    fclose( in );
  }

  if (buf != NULL)
    free( buf );

  return 0;
}


void exclude( const char* name )
{
  FILE* file;
  char	buf[80];
  const char** packets;
  int	cnt;

  file = fopen( name, "r" );
  if (file == NULL)
  {
#ifdef _WIN32
    // Drag and drop sets cwd to %HOMEDRIVE%%HOMEPATH%, apparently, so
    // try relative to the exe.
    char* wd;
    char* path;
    wd = strdup( _pgmptr );
    path = strrchr( wd, '\\' );
    if (path != NULL)
    {
      char cwd[MAX_PATH];
      GetCurrentDirectory( sizeof(cwd), cwd );
      path[1] = '\0';
      SetCurrentDirectory( wd );
      file = fopen( name, "r" );
      SetCurrentDirectory( cwd );
    }
    if (file == NULL)
#endif
    return;
  }

  while (fgets( buf, sizeof(buf), file ) != NULL)
  {
    if (strncmp( buf, "FLPACKET_COMMON_", 16 ) == 0)
    {
      packets = common_packets;
      cnt = lenof(common_packets);
    }
    else if (strncmp( buf, "FLPACKET_SERVER_", 16 ) == 0)
    {
      packets = server_packets;
      cnt = lenof(server_packets);
    }
    else if (strncmp( buf, "FLPACKET_CLIENT_", 16 ) == 0)
    {
      packets = client_packets;
      cnt = lenof(client_packets);
    }
    else
    {
      continue;
    }
    *strchr( buf, '\n' ) = '\0';
    while (--cnt >= 0)
    {
      if (packets[cnt] != NULL && strcmp( buf, packets[cnt] ) == 0)
	packets[cnt] = NULL;
    }
  }
  fclose( file );
}
