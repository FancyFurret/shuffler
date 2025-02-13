#pragma once

#include "../Logger.h"
#include "HookHelper.h"
#include <Windows.h>
#include <hidsdi.h>

class HidDeviceHook {
    static Logger _logger;
    static HookHelper _hookHelper;

  public:
    static bool Enabled;
    static bool Install();
    static bool Uninstall();

  private:
    static HANDLE WINAPI HookedCreateFileW(LPCWSTR lpFileName, DWORD dwDesiredAccess, DWORD dwShareMode,
                                           LPSECURITY_ATTRIBUTES lpSecurityAttributes, DWORD dwCreationDisposition,
                                           DWORD dwFlagsAndAttributes, HANDLE hTemplateFile);

    static HANDLE WINAPI HookedCreateFileA(LPCSTR lpFileName, DWORD dwDesiredAccess, DWORD dwShareMode,
                                           LPSECURITY_ATTRIBUTES lpSecurityAttributes, DWORD dwCreationDisposition,
                                           DWORD dwFlagsAndAttributes, HANDLE hTemplateFile);

    static BOOL WINAPI HookedCloseHandle(HANDLE hObject);

    static BOOLEAN WINAPI HookedHidDGetManufacturerString(HANDLE hidDeviceObject, PVOID buffer, ULONG bufferLength);

    static BOOLEAN WINAPI HookedHidDGetProductString(HANDLE hidDeviceObject, PVOID buffer, ULONG bufferLength);

    static BOOLEAN WINAPI HookedHidDGetSerialNumberString(HANDLE hidDeviceObject, PVOID buffer, ULONG bufferLength);

    static BOOLEAN WINAPI HookedHidDGetPreparsedData(HANDLE hidDeviceObject, PHIDP_PREPARSED_DATA* preparsedData);

    static BOOLEAN WINAPI HookedHidDGetAttributes(HANDLE hidDeviceObject, PHIDD_ATTRIBUTES attributes);
};