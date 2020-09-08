/*
  PacketStats.c - Execute IServerImpl::DumpPacketStats.

  Jason Hood, 12 & 13 October, 2010.
*/

#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <string.h>
#include <tlhelp32.h>


HANDLE GetFLServer( void )
{
  HANDLE hProcessSnap;
  HANDLE hProcess;
  PROCESSENTRY32 pe32;
  BOOL proc;

  hProcessSnap = CreateToolhelp32Snapshot( TH32CS_SNAPPROCESS, 0 );
  if (hProcessSnap == INVALID_HANDLE_VALUE)
    return NULL;

  hProcess = NULL;
  pe32.dwSize = sizeof(pe32);
  proc = Process32First( hProcessSnap, &pe32 );
  while (proc)
  {
    if (_stricmp( pe32.szExeFile, "flserver.exe" ) == 0)
    {
      hProcess = OpenProcess( PROCESS_ALL_ACCESS, FALSE, pe32.th32ProcessID );
      break;
    }
    proc = Process32Next( hProcessSnap, &pe32 );
  }
  CloseHandle( hProcessSnap );
  return hProcess;
}


int WINAPI WinMain( HINSTANCE hInstance, HINSTANCE hPrevInstance,
		    LPSTR lpCmdLine, int nCmdShow )
{
  HANDLE hFLServer;

  hFLServer = GetFLServer();
  if (hFLServer == NULL)
  {
    MessageBox( NULL, "FLServer.exe not found", "PacketStats", MB_OK );
    return 1;
  }

  CloseHandle( CreateRemoteThread( hFLServer, NULL, 4096,
			     // IServerImpl::DumpPacketStats( LPCSTR filename )
			     (LPTHREAD_START_ROUTINE)0x6cf23b0,
			     // "flservertrace.txt"
			     (LPVOID)0x639c43b,
			     0, NULL ) );
  CloseHandle( hFLServer );

  return 0;
}
