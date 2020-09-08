/*
  PacketStatsExit.c - Execute IServerImpl::DumpPacketStats on closing FLServer.

  Jason Hood, 13 October, 2010.
*/

#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <time.h>
#include <stdio.h>

#define NAKED	__declspec(naked)
#define STDCALL __stdcall


#define ADDR_SHUTDOWN ((PBYTE)0x6d451da+1)


DWORD dummy;
#define ProtectX( addr, size ) \
  VirtualProtect( addr, size, PAGE_EXECUTE_READWRITE, &dummy );

#define RELOFS( from, to ) \
  *(PDWORD)((DWORD)(from)) = (DWORD)(to) - (DWORD)(from) - 4;

#define NEWOFS( from, to, prev ) \
  prev = (DWORD)(from) + *((PDWORD)(from)) + 4; \
  RELOFS( from, to )


char packetstats[MAX_PATH];


DWORD Shutdown_Org;

NAKED
void Shutdown_Hook( void )
{
  __asm {
	call	Shutdown_Org

	// IServerImpl::DumpPacketStats( LPCSTR filename )
	mov	eax, 0x6cf23b0
	push	offset packetstats
	call	eax
	ret
  }
}


void Patch( void )
{
  struct tm* tim;
  time_t t;

  ProtectX( ADDR_SHUTDOWN, 4 );
  NEWOFS( ADDR_SHUTDOWN, Shutdown_Hook, Shutdown_Org );

  t = time( NULL );
  tim = localtime( &t );
  sprintf( packetstats, "PacketStats-%.4d-%.2d-%.2d.%.2d%.2d%.2d.txt",
			tim->tm_year + 1900, tim->tm_mon + 1, tim->tm_mday,
			tim->tm_hour, tim->tm_min, tim->tm_sec );
}


BOOL WINAPI DllMain( HINSTANCE hinstDLL, DWORD fdwReason, LPVOID lpvReserved )
{
  if (fdwReason == DLL_PROCESS_ATTACH)
    Patch();

  return TRUE;
}
