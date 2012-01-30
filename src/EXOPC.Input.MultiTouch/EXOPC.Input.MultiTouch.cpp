// EXOPC.Input.MultiTouch.cpp : Defines the exported functions for the DLL application.
//

#include "stdafx.h"
#include <Windows.h>
#include <strsafe.h>
#include "EXOxtender.h"
#include <tpcshrd.h>
#include <map>
#include <vector>
#include <algorithm>

const unsigned int EX_TOUCH_INFO_START = 2060;
const unsigned int EX_TOUCH_INFO_POINT = 2061;
const unsigned int EX_TOUCH_INFO_END   = 2062;
const unsigned int EX_TOUCH_EVENT_START= 2075;
const unsigned int EX_TOUCH_EVENT_END  = 2076;
const unsigned int EX_TOUCH_EVENT_MOVE = 2077;
const unsigned int EX_TOUCH_EVENT_SIZE = 2078;

const unsigned int EX_DESTROYWINDOW = 1202;

const ULONG TOUCH_FLAGS(/*TWF_FINETOUCH|*/TWF_WANTPALM);

const DWORD dwHwndTabletProperty = TABLET_DISABLE_PRESSANDHOLD;

HHOOK hookGetMsg = NULL;
LPCWSTR hookWindowName = NULL;

// from dllmain.cpp
extern HWND keyboardHandle;
extern int nWindowHandle;
extern HWND windowHandle[MAX_WINDOW_HANDLE];
extern bool isTouch[MAX_WINDOW_HANDLE];

//extern std::auto_ptr<HandleList> s_windows;

//Map to hold last move coordinates for each contact ID
std::map<int, POINT> lastMoves;

// to register all windows as touch-capable
/*
std::vector<HWND> registeredWindows;
*/

//
// Store the application instance of this module to pass to
// hook initialization. This is set in DLLMain().
//
HMODULE g_appInstance = NULL;


void trace(char *s)
{
	FILE* trace = fopen("c:\\patrice\\patrice.txt", "a");
	if (trace != 0)
	{
		fprintf(trace, s);
		fclose(trace);
	}
}

void tracei(char *s, int i)
{
	char buf[1024];
	sprintf(buf, "%s%d\n", s, i);
	trace(buf);
}


void SetTabletpenserviceProperties(HWND hWnd){
    ATOM atom = ::GlobalAddAtom((LPCWSTR)L"MicrosoftTabletPenServiceProperty");    
    ::SetProp(hWnd, (LPCWSTR)L"MicrosoftTabletPenServiceProperty", reinterpret_cast<HANDLE>(dwHwndTabletProperty));
    ::GlobalDeleteAtom(atom);
}

/*
void wtos(TCHAR *in, char *out)
{
	while (*in)
	{
		*out++ = (char)*in++;
		*out = '\0';
	}
}
*/

BOOL CALLBACK enumChildWindowsCallback(
	__in HWND hwnd,
	__in LPARAM userData
	)
{
	/*
	TCHAR wclassName[1024];
	TCHAR wwindowTitle[1024];
	char className[1024];
	char windowTitle[1024];
	*/
	/*
	GetClassName(hwnd, wclassName, 1024);
	GetWindowText(hwnd, wwindowTitle, 1024);
	wtos(wclassName, className);
	wtos(wwindowTitle, windowTitle);
	*/
	//sprintf(buf, "init touch window for enumerated child = %d; class = %s; title = %s\n", hwnd, className, windowTitle);
	/*
	char buf[10240];
	sprintf(buf, "init touch window for enumerated child = %d; adr = %x\n", hwnd, windowsToRegister);
	trace(buf);
	*/
	//RegisterTouchWindow(hwnd, TOUCH_FLAGS);
	//windowsToRegister.push_back(hwnd);
	//pushWtr(hwnd);
	return true;
}

static void FixWindowForTouch(HWND hwnd)
{
	RegisterTouchWindow(hwnd, TOUCH_FLAGS);

	// disable right-click on touch and hold
	// using the method documented here: http://msdn.microsoft.com/en-us/library/ms812373.aspx

	// The atom identifier and Tablet PC atom
	ATOM atomID = 0;
	LPCWSTR tabletAtom = L"MicrosoftTabletPenServiceProperty";

	// Get the Tablet PC atom ID
	atomID = GlobalAddAtom(tabletAtom);

	if (atomID != 0)
	{
		// Try to disable press and hold gesture by 
		// setting the window property
		SetProp(hwnd, tabletAtom, (HANDLE)1);
	}
}

static void FixWindowForGesture(HWND hwnd)
{
	UnregisterTouchWindow(hwnd);

	// reenable right-click on touch and hold

	// The atom identifier and Tablet PC atom
	ATOM atomID = 0;
	LPCWSTR tabletAtom = L"MicrosoftTabletPenServiceProperty";

	// Get the Tablet PC atom ID
	atomID = GlobalAddAtom(tabletAtom);

	if (atomID != 0)
	{
		// Try to disable press and hold gesture by 
		// setting the window property
		RemoveProp(hwnd, tabletAtom);
	}

	/*
	GESTURECONFIG gc1[3];    
    UINT uiGcs1 = 3;

    ZeroMemory(&gc1, sizeof(gc1));
    gc1[0].dwID  = GID_ZOOM;
    gc1[1].dwID  = GID_ROTATE;
    gc1[2].dwID  = GID_PAN;
    BOOL bResult1 = GetGestureConfig(hwnd, 0, 0, &uiGcs1, gc1, sizeof(GESTURECONFIG));        
    if (!bResult1){                
        DWORD err = GetLastError();
		tracei("zoom get config err ", bResult1);
    }
	tracei("zoom want config ", gc1[0].dwWant);
	tracei("zoom block config ", gc1[0].dwBlock);
	*/

	/*
	DWORD dwPanWant  = GC_PAN_WITH_SINGLE_FINGER_VERTICALLY | GC_PAN_WITH_SINGLE_FINGER_HORIZONTALLY;
	DWORD dwPanBlock = GC_PAN_WITH_GUTTER | GC_PAN_WITH_INERTIA;

	// set the settings in the gesture configuration
	GESTURECONFIG gc[] = {{ GID_ZOOM, 0, GC_ZOOM },
						  { GID_ROTATE, GC_ROTATE, 0},
						  { GID_PAN, dwPanWant , dwPanBlock}
						 };
                     
	UINT uiGcs = 3;
	BOOL bResult = SetGestureConfig(hwnd, 0, uiGcs, gc, sizeof(GESTURECONFIG));

	if (!bResult)
	{ 
		DWORD err = GetLastError();
		tracei("error: ", err);
	}
	*/
}

static int findWindowHandle(HWND hwnd)
{
	for (int i = 0; i < nWindowHandle; ++i)
		if (windowHandle[i] == hwnd)
			return i;
	return -1;
}

static void setWindowHandle(HWND hwnd, bool is_touch)
{
	int i = findWindowHandle(hwnd);
	if (i == -1)
	{
		if (nWindowHandle == MAX_WINDOW_HANDLE)
		{
			// let's remove the oldest to make room
			for (int k = 1; k < MAX_WINDOW_HANDLE; ++k)
			{
				windowHandle[k-1] = windowHandle[k];
				isTouch[k-1] = isTouch[k];
			}
			--nWindowHandle;
		}
		i = nWindowHandle++;
		windowHandle[i] = hwnd;
	}
	isTouch[i] = is_touch;
}

void TouchIgnore(HWND hwnd)
{
	int k = findWindowHandle(hwnd);

	// remove from list
	if (k != -1)
	{
		for (int i = k + 1; i < nWindowHandle; ++i)
		{
			windowHandle[i-1] = windowHandle[i];
			isTouch[i-1] = isTouch[i];
		}
		--nWindowHandle;
	}
}

void GestureEnable(HWND windowHandle)
{
	setWindowHandle(windowHandle, false);
}

void TouchEnable(HWND windowHandle)
{
	setWindowHandle(windowHandle, true);
}

bool InitializeTouch(LPCWSTR windowName, HWND childWindowHandle)
{
	if (g_appInstance == NULL || windowName == NULL)
	{
		return false;
	}

	if (keyboardHandle == 0)
	{
		keyboardHandle = childWindowHandle;
	}

	nWindowHandle = 0;

	TouchEnable(childWindowHandle);

	unsigned long threadId = NULL;
	HWND hookWindow = FindWindow(NULL, windowName);
	if (hookWindow != 0)
	{
		threadId = GetWindowThreadProcessId(hookWindow,0);
	}

	if(!threadId)
		return false;

	hookGetMsg = SetWindowsHookEx(WH_GETMESSAGE, (HOOKPROC)GetMsgProc, g_appInstance, threadId);

	//hookGetMsg = SetWindowsHookEx(WH_GETMESSAGE, (HOOKPROC)GetMsgProc, g_appInstance, 0);

	if (hookGetMsg != NULL)
	{
		//if (childWindowHandle)
		//{
		//	s_windows.reset(new HandleList);
		//	s_windows->push_back(childWindowHandle);
		//}
		//int msgboxID = MessageBox(hookWindow,
		//						 (LPCWSTR)L"HOOK SUCCESS",
		//						 (LPCWSTR)L"TOUCH",
		//						 MB_OK
		//						 );
	}
	else
	{
		//int msgboxID = MessageBox(hookWindow,
		//						 (LPCWSTR)L"HOOK FAIL",
		//						 (LPCWSTR)L"TOUCH",
		//						 MB_OK
		//						 );
	}
	

	return hookGetMsg != NULL;
}

void UninitializeTouch()
{
	HWND hookWindow = FindWindow(NULL, hookWindowName);

	//if (hookWindow != NULL && touchRegistered)
	//	UnregisterTouchWindow(hookWindow);
	hookWindow = 0;

	keyboardHandle = 0;

	if (hookGetMsg != 0)
		UnhookWindowsHookEx(hookGetMsg);
	hookGetMsg = 0;

}

// Extracts contact point in client area coordinates (pixels) from a TOUCHINPUT
// structure. TOUCHINPUT structure uses 100th of a pixel units.
// in:
//      hWnd        window handle
//      ti          TOUCHINPUT structure (info about contact)
// returns:
//      POINT with contact coordinates
POINT GetTouchPoint(HWND hWnd, const TOUCHINPUT& ti)
{
    POINT pt;
	pt.x = ti.x / 100;
	pt.y = ti.y / 100;

	if (ti.dwMask & TOUCHINPUTMASKF_CONTACTAREA)
	{
		pt.x |= (ti.cxContact / 100) << 16;
		pt.y |= (ti.cyContact / 100) << 16;
	}

	//pt.x = GetSystemMetrics(SM_DIGITIZER);
    //ScreenToClient(hWnd, &pt);
    return pt;
}

// Extracts contact ID from a TOUCHINPUT structure.
// in:
//      ti          TOUCHINPUT structure (info about contact)
// returns:
//      ID assigned to the contact
inline int GetTouchContactID(const TOUCHINPUT& ti)
{
    return ti.dwID;
}

///////////////////////////////////////////////////////////////////////////////
// WM_TOUCH message handlers

// Handler for touch-down message.
// Starts a new stroke and assigns a color to it.
// in:
//      hWnd        window handle
//      ti          TOUCHINPUT structure (info about contact)

void sendAreaSize(HWND hWnd, int id, POINT pt)
{
	int sizeX = pt.x >> 16;
	int sizeY = pt.y >> 16;

	if (sizeX != 0 || sizeY != 0)
	{
		WPARAM wParam = MAKEWPARAM(EX_TOUCH_EVENT_SIZE, id);
		LPARAM lParam = MAKELPARAM(sizeX, sizeY);
		PostMessage(keyboardHandle, WM_APP + 5, wParam, lParam);
	}
}

void OnTouchDownHandler(HWND hWnd, const TOUCHINPUT& ti)
{
    // Extract contact info: point of contact and ID
	int iCursorId = GetTouchContactID(ti);
    POINT pt = GetTouchPoint(hWnd, ti);
    
	sendAreaSize(hWnd, iCursorId, pt);
	WPARAM wParam = MAKEWPARAM(EX_TOUCH_EVENT_START,iCursorId);
	LPARAM lParam = MAKELPARAM(pt.x & 0x0000ffff, pt.y & 0x0000ffff);

	//int msgboxID = MessageBox(
 //       hWnd,
 //       (LPCWSTR)L"EX_TOUCH_EVENT_START",
 //       (LPCWSTR)L"TOUCH",
 //       MB_OK
 //   );

	//PostMessage(hWnd, WM_APP + 5, wParam, lParam);
	// Posting to keyboardHandle sends the message to the WndProc of the application that has called InitializeTouch
	// (as opposed to sending it to any sub-process owning hWnd (the window in which the touch occurred)).
	PostMessage(keyboardHandle, WM_APP + 5, wParam, lParam);

	/*
	int globalX = pt.x;
	int globalY = pt.y;
	ScreenToClient(hWnd, &pt);
	int localX = pt.x;
	int localY = pt.y;

	char buf[10240];
	sprintf(buf, "down in window: %d; global = (%d, %d); local = (%d, %d)\n", hWnd, globalX, globalY, localX, localY);
	trace(buf);
	*/
}

// Handler for touch-move message.
// in:
//      hWnd        window handle
//      ti          TOUCHINPUT structure (info about contact)
void OnTouchMoveHandler(HWND hWnd, const TOUCHINPUT& ti)
{
    // Extract contact info: contact ID
    int iCursorId = GetTouchContactID(ti);

    // Extract contact info: contact point
    POINT pt = GetTouchPoint(hWnd, ti);

	//Don't send move message if current move is the same as last move
	std::map<int, POINT>::iterator it = lastMoves.find(iCursorId);

	if (it != lastMoves.end())
	{
		if ( (pt.x != (it->second).x) || (pt.y != (it->second).y) )
		{
			sendAreaSize(hWnd, iCursorId, pt);
			WPARAM wParam = MAKEWPARAM(EX_TOUCH_EVENT_MOVE, iCursorId);
			LPARAM lParam = MAKELPARAM(pt.x & 0x0000ffff, pt.y & 0x0000ffff);
			//PostMessage(hWnd, WM_APP + 5, wParam, lParam);
			PostMessage(keyboardHandle, WM_APP + 5, wParam, lParam);
			lastMoves[iCursorId] = pt;
		}
	}
	else
	{
		sendAreaSize(hWnd, iCursorId, pt);
		WPARAM wParam = MAKEWPARAM(EX_TOUCH_EVENT_MOVE, iCursorId);
		LPARAM lParam = MAKELPARAM(pt.x & 0x0000ffff, pt.y & 0x0000ffff);
		//PostMessage(hWnd, WM_APP + 5, wParam, lParam);
		PostMessage(keyboardHandle, WM_APP + 5, wParam, lParam);
		lastMoves[iCursorId] = pt;
	}

	/*WPARAM wParam = MAKEWPARAM(EX_TOUCH_EVENT_MOVE, iCursorId);
	LPARAM lParam = MAKELPARAM(pt.x, pt.y);
	PostMessage(hWnd, WM_APP + 5, wParam, lParam);*/

	//trace("move\n");
}

// Handler for touch-up message.
// in:
//      hWnd        window handle
//      ti          TOUCHINPUT structure (info about contact)
void OnTouchUpHandler(HWND hWnd, const TOUCHINPUT& ti)
{
    // Extract contact info: contact ID
    int iCursorId = GetTouchContactID(ti);

	// Extract contact info: contact point
    POINT pt = GetTouchPoint(hWnd, ti);

	sendAreaSize(hWnd, iCursorId, pt);
	WPARAM wParam = MAKEWPARAM(EX_TOUCH_EVENT_END, iCursorId);
	LPARAM lParam = MAKELPARAM(pt.x & 0x0000ffff, pt.y & 0x0000ffff);

		//int msgboxID = MessageBox(
  //      hWnd,
  //      (LPCWSTR)L"EX_TOUCH_EVENT_END",
  //      (LPCWSTR)L"TOUCH",
  //      MB_OK
  //  );

	//PostMessage(hWnd, WM_APP + 5, wParam, lParam);
	PostMessage(keyboardHandle, WM_APP + 5, wParam, lParam);

	//remove last move
    lastMoves.erase(ti.dwID);

	//trace("up\n");
}

static LRESULT CALLBACK GetMsgProc(int code, WPARAM wParam, LPARAM lParam)
{
	if (code >= 0)
	{
		switch(code)
		{
		case HC_ACTION:
			{
				LPMSG msg = (LPMSG)lParam;

				//for (HandleList::iterator it = s_windows->begin(); it != s_windows->end(); ++it)
				//{
				//	RegisterTouchWindow(*it, TOUCH_FLAGS);
				//}
				
				/*
				if(keyboardHandle != NULL)
				{
					FixWindowForTouch(keyboardHandle);
				}
				*/

				// to register all windows as touch-capable
				/*
				if (msg->hwnd != 0)
				{
					bool found = false;
					for (int i = 0; i < registeredWindows.size(); ++i)
						if (registeredWindows[i] == msg->hwnd)
						{
							found = true;
							break;
						}
					if (!found)
					{
						RegisterTouchWindow(msg->hwnd, TOUCH_FLAGS);
						char buf[10240];
						sprintf(buf, "registering %d; adr = %x\n", msg->hwnd, windowsToRegister);
						trace(buf);
						registeredWindows.push_back(msg->hwnd);
					}
				}
				*/
				
				/*
				while (windowsToRegister.size() > 0)
				{
					RegisterTouchWindow(windowsToRegister.back(), TOUCH_FLAGS);

					// register also all child windows to receive touch
					long userData = 0;
					EnumChildWindows(windowsToRegister.back(), enumChildWindowsCallback, userData);

					windowsToRegister.pop_back();
				}
				*/

				/*
						char buf[10240];
						sprintf(buf, "searching %d, size = %d adr = %x\n", msg->hwnd, wtrCount, windowsToRegister);
						trace(buf);
				*/

#if 0
				int pos = -1;
				for (int i = 0; i < /*windowsToRegister.size()*/wtrCount; ++i)
				{
					/*
						char buf[10240];
						sprintf(buf, "vector[%d] = %d\n", i, windowsToRegister[i]);
						trace(buf);
						*/
					if (windowsToRegister[i] == msg->hwnd)
					{
						/*
						char buf[10240];
						sprintf(buf, "is found in vector at pos: %d\n", i);
						trace(buf);
						*/
						pos = i;
					}
				}
				//std::vector<HWND>::iterator pos = std::find(windowsToRegister.begin(), windowsToRegister.end(), msg->hwnd);
				//if (pos != windowsToRegister.end())
				if (pos >= 0)
				{
					RegisterTouchWindow(msg->hwnd, TOUCH_FLAGS);
					//windowsToRegister.erase(pos);
					for (int i = pos + 1; i < wtrCount; ++i)
						windowsToRegister[i - 1] = windowsToRegister[i];
					--wtrCount;
					/*
						char buf[10240];
						sprintf(buf, "registering known window %d\n", msg->hwnd);
						trace(buf);
					*/
					FILE* trace = fopen("c:\\patrice\\patrice.txt", "a");
					if (trace != 0)
					{
						TCHAR windowTitle[1024];
						GetWindowText(msg->hwnd, windowTitle, 1024);
						TCHAR className[1024];
						GetClassName(msg->hwnd, className, 1024);
						fwprintf(trace, L"%s - %s\n", windowTitle, className);
						fclose(trace);
					}
				}
#endif

				//if (!IsTouchWindow(msg->hwnd, NULL))
				//{
				//	if (RegisterTouchWindow(msg->hwnd, TOUCH_FLAGS))
				//	{
				//		touchRegistered = true;
				//	}
				//}

				//TMP
				//FixWindowForTouch(msg->hwnd);
				//END TMP

				// a window is candidate for registering for touch (or back to gesture)
				// if its class is TmioEngineWin or Internet Explorer_Server
				// and the window or one of its ancertors has been declared with TouchEnable or GestureEnable

				HWND hwnd = msg->hwnd;
				TCHAR className[1024];

				if (hwnd != 0 && GetClassName(hwnd, className, 1024))
				{
					if (wcsncmp(className, L"TmioEngineWin", 1024) == 0
						|| wcsncmp(className, L"Internet Explorer_Server", 1024) == 0)
					{
						while (hwnd != 0)	// loop thru ancestors
						{
							int i = findWindowHandle(hwnd);
							if (i != -1)
							{
								if (isTouch[i])
									FixWindowForTouch(msg->hwnd);
								else
									FixWindowForGesture(msg->hwnd);
								break;
							}
							hwnd = GetParent(hwnd);
						}
					}
				}

				switch(msg->message)
				{
				case WM_TOUCH:
					{
						// WM_TOUCH message can contain several messages from different contacts
						// packed together.
						// Message parameters need to be decoded:
						unsigned int numInputs = (unsigned int) msg->wParam; // Number of actual per-contact messages
						TOUCHINPUT* ti = new TOUCHINPUT[numInputs]; // Allocate the storage for the parameters of the per-contact messages
						if (ti == NULL)
						{
							break;
						}

						//int maxTouchInputs = GetSystemMetrics(SM_MAXIMUMTOUCHES);

						// Unpack message parameters into the array of TOUCHINPUT structures, each
						// representing a message for one single contact.
						if (GetTouchInputInfo((HTOUCHINPUT)msg->lParam, numInputs, ti, sizeof(TOUCHINPUT)))
						{
							// For each contact, dispatch the message to the appropriate message
							// handler.

							for (unsigned int i = 0; i < numInputs; ++i)
							{
								if (ti[i].dwFlags & TOUCHEVENTF_DOWN)
								{
									OnTouchDownHandler(msg->hwnd, ti[i]);
								}
								else if (ti[i].dwFlags & TOUCHEVENTF_MOVE)
								{
									OnTouchMoveHandler(msg->hwnd, ti[i]);
								}
								else if (ti[i].dwFlags & TOUCHEVENTF_UP)
								{
									OnTouchUpHandler(msg->hwnd, ti[i]);
								}
							}
						}
						
						CloseTouchInputHandle((HTOUCHINPUT)lParam);
						delete [] ti;
					}
					break;

/*
				case WM_GESTURE:
					{
						trace("WM_GESTURE\n");
					}
					break;
*/

				case WM_QUIT:
					UninitializeTouch();
					break;

				case WM_APP + 5:
					int arg0 = LOWORD(msg->wParam);
					int arg1 = HIWORD(msg->wParam);
					int arg2 = LOWORD(msg->lParam);
					int arg3 = HIWORD(msg->lParam);

					if(arg0 == EX_DESTROYWINDOW)
					{
						DestroyWindow((HWND)msg->lParam);
					}
					break;
				}
				break;
			}
		}
	}

	return CallNextHookEx(0, code, wParam, lParam);
}
