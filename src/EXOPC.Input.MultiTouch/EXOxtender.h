#ifndef _EXOXTENDER_H_
#define _EXOXTENDER_H_

#include <list>

#define MAX_WINDOW_HANDLE 10000

typedef std::list<HWND> HandleList;
typedef void (CALLBACK *HookProc)(int code, WPARAM w, LPARAM l);
static LRESULT CALLBACK GetMsgProc(int code, WPARAM wParam, LPARAM lParam);
bool InitializeTouch(LPCWSTR windowName, HWND childWindowHandle);
void UninitializeTouch();

#endif 