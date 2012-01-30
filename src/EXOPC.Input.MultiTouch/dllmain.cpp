// dllmain.cpp : Defines the entry point for the DLL application.
#include "stdafx.h"
#include "EXOxtender.h"
#include <vector>
//
// Capture the application instance of this module to pass to
// hook initialization.
//
extern HMODULE g_appInstance;

//
// some data will be shared across all
// instances of the DLL
//
#pragma data_seg(".SHARED")
// shared data
HWND keyboardHandle(0);
int nWindowHandle = 0;
HWND windowHandle[MAX_WINDOW_HANDLE] = {0};
bool isTouch[MAX_WINDOW_HANDLE] = {false};

//std::auto_ptr<HandleList> s_windows(0);

#pragma data_seg()
#pragma comment(linker, "/SECTION:.SHARED,RWS")

BOOL APIENTRY DllMain( HMODULE hModule,
                       DWORD  ul_reason_for_call,
                       LPVOID lpReserved
					 )
{
	switch (ul_reason_for_call)
	{
	case DLL_PROCESS_ATTACH:
		//
		// Capture the application instance of this module to pass to
		// hook initialization.
		//
		if (g_appInstance == NULL)
		{
			g_appInstance = hModule;
		}
		break;
	case DLL_THREAD_ATTACH:
		break;
	case DLL_THREAD_DETACH:
		break;
	case DLL_PROCESS_DETACH:
		UninitializeTouch();
		break;
	}
	return TRUE;
}

