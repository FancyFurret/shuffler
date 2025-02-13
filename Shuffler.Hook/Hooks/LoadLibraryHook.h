#pragma once

#include "../Logger.h"
#include "HookHelper.h"
#include <Windows.h>

class LoadLibraryHook {
    static Logger _logger;
    static HookHelper _hookHelper;

  public:
    static bool Enabled;
    static bool Install();
    static bool Uninstall();

    static HMODULE WINAPI HookedLoadLibraryA(LPCSTR lpLibFileName);
    static HMODULE WINAPI HookedLoadLibraryW(LPCWSTR lpLibFileName);
    static HMODULE WINAPI HookedLoadLibraryExA(LPCSTR lpLibFileName, HANDLE hFile, DWORD dwFlags);
    static HMODULE WINAPI HookedLoadLibraryExW(LPCWSTR lpLibFileName, HANDLE hFile, DWORD dwFlags);
};