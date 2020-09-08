/*
  PacketLog.c - Log packets to file (as binary).

  Jason Hood, 12 to 15 October, 2010.

  8 & 9 December, 2010:
  + added player the packet is for/from;
  * don't write the packet if it is the same as the previous.

  File format:
    00 DWORD	'FLP1'
    00 DWORD	size
    04 time_t	time
    08 WORD	milliseconds
    0A size	packet

    00 DWORD	'FLP2'
    00 DWORD	size
    04 time_t	time
    08 WORD	milliseconds
    0A short	player (+ve = to, -ve = from)
    0C size	packet

    If the packet is a duplicate of the previous packet, use a size of 0 and
    only write player.
*/

#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <time.h>
#include <sys/timeb.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <io.h>

#define NAKED	__declspec(naked)
#define STDCALL __stdcall


#define ADDR_PACKET ((PBYTE) 0x65c10b0) 	// dalib.dll
#define ADDR_CLIENT ((PDWORD)0x6b6bd80) 	// remoteclient.dll


DWORD dummy;
#define ProtectX( addr, size ) \
  VirtualProtect( addr, size, PAGE_EXECUTE_READWRITE, &dummy );

#define RELOFS( from, to ) \
  (DWORD)(to) - (DWORD)(from) - 4;

#define CALL( from, to ) \
  *(PBYTE)(from) = 0xe8; \
  *(PDWORD)((DWORD)from+1) = RELOFS( (DWORD)from+1, to )


char packetlog[MAX_PATH];

char*  packet_data;
size_t packet_size;
size_t packet_capacity;


void STDCALL Packet_Log( int player, const BYTE* data, size_t size )
{
  FILE* file;
  struct _timeb t;

  if (size < 2)
    return;

  file = fopen( packetlog, "ab" );
  if (file != NULL)
  {
    if (size == packet_size && memcmp( packet_data, data, size ) == 0)
    {
      size = 0;
      fwrite( &size,   sizeof(size), 1, file );
      fwrite( &player, 2,	     1, file );
    }
    else
    {
      _ftime( &t );
      fwrite( &size,	  sizeof(size),      1, file );
      fwrite( &t.time,	  sizeof(t.time),    1, file );
      fwrite( &t.millitm, sizeof(t.millitm), 1, file );
      fwrite( &player,	  2,		     1, file );
      fwrite( data,	  size, 	     1, file );

      if (size > packet_capacity)
      {
	char* temp = realloc( packet_data, size );
	if (temp == NULL)
	{
	  size = packet_capacity;
	}
	else
	{
	  packet_data = temp;
	  packet_capacity = size;
	}
      }
      memcpy( packet_data, data, packet_size = size );
    }
    fclose( file );
  }

  if (_access( "LargeID.dat", 0 ) < 0)
  {
    file = fopen( "LargeID.dat", "wb" );
    if (file != NULL)
    {
      UINT* begin = *(UINT**)0x63fcb00;
      UINT* end   = *(UINT**)0x63fcb04;
      fwrite( begin, sizeof(UINT), end - begin, file );
      fclose( file );
    }
  }
}


NAKED
void Packet_Hook( void )
{
  __asm {
	push	ecx

	push	[esp+8+8]
	push	[esp+4+12]
	push	[ebx+0x68]
	call	Packet_Log

	mov	eax, [esp+8+8]
	pop	ecx
	cmp	eax, 32
	ret
  }
}


DWORD Client_Org;

NAKED
void Client_Hook( void )
{
  __asm {
	push	ecx

	mov	eax, dword ptr [ecx+0x68]
	push	[esp+8+4]
	neg	eax
	push	[esp+4+8]
	push	eax
	call	Packet_Log

	pop	ecx
	jmp	Client_Org
  }
}


void Patch( void )
{
  struct tm* tim;
  time_t t;
  FILE*  file;

  ProtectX( ADDR_PACKET, 7 );
  ProtectX( ADDR_CLIENT, 4 );

  CALL( ADDR_PACKET, Packet_Hook );
  ADDR_PACKET[5] = ADDR_PACKET[6] = 0x90;

  Client_Org = *ADDR_CLIENT;
  *ADDR_CLIENT = (DWORD)Client_Hook;

  t = time( NULL );
  tim = localtime( &t );
  sprintf( packetlog, "PacketLog-%.4d-%.2d-%.2d.%.2d%.2d%.2d.bin",
		      tim->tm_year + 1900, tim->tm_mon + 1, tim->tm_mday,
		      tim->tm_hour, tim->tm_min, tim->tm_sec );
  file = fopen( packetlog, "wb" );
  if (file != NULL)
  {
    fwrite( "FLP2", 4, 1, file );
    fclose( file );
  }
  else
  {
    *packetlog = '\0';
  }
}


BOOL WINAPI DllMain( HINSTANCE hinstDLL, DWORD fdwReason, LPVOID lpvReserved )
{
  if (fdwReason == DLL_PROCESS_ATTACH)
    Patch();

  return TRUE;
}
