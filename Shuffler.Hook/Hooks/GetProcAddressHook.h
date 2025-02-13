#pragma once

/**
 * Currently unused, caused issues with some games. May be revisited in the future.
 */

#include "../Logger.h"
#include <Windows.h>

class HookHelper;

class GetProcAddressHook {
    static Logger _logger;
    static HookHelper _hookHelper;
    static decltype(&GetProcAddress) _originalGetProcAddress;
    
  public:
    static bool Enabled;

    static bool Install();
    static bool Uninstall();
    static FARPROC WINAPI HookedGetProcAddress(HMODULE hModule, LPCSTR lpProcName);

    static FARPROC GetProcAddress(HMODULE hModule, LPCSTR lpProcName) {
        return _originalGetProcAddress ? _originalGetProcAddress(hModule, lpProcName)
                                       : ::GetProcAddress(hModule, lpProcName);
    }
};