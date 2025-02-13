#include "HidDeviceHook.h"

#include "../EmulatedDeviceDefinitions.h"
#include "../Logger.h"
#include "RawInputHook.h"

Logger HidDeviceHook::_logger = Logger("HidDeviceHook");
HookHelper HidDeviceHook::_hookHelper;
bool HidDeviceHook::Enabled = false;

namespace {

decltype(&CreateFileW) OriginalCreateFileW = nullptr;
decltype(&CreateFileA) OriginalCreateFileA = nullptr;
decltype(&CloseHandle) OriginalCloseHandle = nullptr;
decltype(&HidD_GetManufacturerString) OriginalHidDGetManufacturerString = nullptr;
decltype(&HidD_GetProductString) OriginalHidDGetProductString = nullptr;
decltype(&HidD_GetSerialNumberString) OriginalHidDGetSerialNumberString = nullptr;
decltype(&HidD_GetPreparsedData) OriginalHidDGetPreparsedData = nullptr;
decltype(&HidD_GetAttributes) OriginalHidDGetAttributes = nullptr;

}  // namespace

bool HidDeviceHook::Install() {
    _logger.Info("Installing HID device hooks...");

    bool success = true;
    success &= _hookHelper.Hook("kernel32.dll", "CreateFileW", &OriginalCreateFileW, HookedCreateFileW);
    success &= _hookHelper.Hook("kernel32.dll", "CreateFileA", &OriginalCreateFileA, HookedCreateFileA);
    success &= _hookHelper.Hook("kernel32.dll", "CloseHandle", &OriginalCloseHandle, HookedCloseHandle);
    success &= _hookHelper.Hook("hid.dll", "HidD_GetManufacturerString", &OriginalHidDGetManufacturerString,
                                HookedHidDGetManufacturerString);
    success &=
        _hookHelper.Hook("hid.dll", "HidD_GetProductString", &OriginalHidDGetProductString, HookedHidDGetProductString);
    success &= _hookHelper.Hook("hid.dll", "HidD_GetSerialNumberString", &OriginalHidDGetSerialNumberString,
                                HookedHidDGetSerialNumberString);
    success &=
        _hookHelper.Hook("hid.dll", "HidD_GetPreparsedData", &OriginalHidDGetPreparsedData, HookedHidDGetPreparsedData);
    success &= _hookHelper.Hook("hid.dll", "HidD_GetAttributes", &OriginalHidDGetAttributes, HookedHidDGetAttributes);

    if (success) {
        _logger.Info("HID device hooks installed successfully");
    } else {
        _logger.Error("Failed to install some HID device hooks");
    }

    return success;
}

bool HidDeviceHook::Uninstall() {
    return _hookHelper.Uninstall();
}

HANDLE WINAPI HidDeviceHook::HookedCreateFileW(LPCWSTR lpFileName, DWORD dwDesiredAccess, DWORD dwShareMode,
                                               LPSECURITY_ATTRIBUTES lpSecurityAttributes, DWORD dwCreationDisposition,
                                               DWORD dwFlagsAndAttributes, HANDLE hTemplateFile) {
    if (!Enabled) {
        return OriginalCreateFileW(lpFileName, dwDesiredAccess, dwShareMode, lpSecurityAttributes,
                                   dwCreationDisposition, dwFlagsAndAttributes, hTemplateFile);
    }

    // Check if this is one of our emulated device paths
    if (EmulatedDeviceDefinitions::IsEmulatedDevicePath(lpFileName)) {
        int controllerIndex = EmulatedDeviceDefinitions::GetControllerIndex(lpFileName);
        _logger.InfoFormat("CreateFileW called for emulated device (controller {})", controllerIndex);
        return EmulatedDeviceDefinitions::EMULATED_DEVICE_HANDLE;
    }

    return OriginalCreateFileW(lpFileName, dwDesiredAccess, dwShareMode, lpSecurityAttributes, dwCreationDisposition,
                               dwFlagsAndAttributes, hTemplateFile);
}

HANDLE WINAPI HidDeviceHook::HookedCreateFileA(LPCSTR lpFileName, DWORD dwDesiredAccess, DWORD dwShareMode,
                                               LPSECURITY_ATTRIBUTES lpSecurityAttributes, DWORD dwCreationDisposition,
                                               DWORD dwFlagsAndAttributes, HANDLE hTemplateFile) {
    if (!Enabled) {
        return OriginalCreateFileA(lpFileName, dwDesiredAccess, dwShareMode, lpSecurityAttributes,
                                   dwCreationDisposition, dwFlagsAndAttributes, hTemplateFile);
    }

    // Convert ANSI path to wide string for comparison
    wchar_t wideFileName[MAX_PATH];
    if (MultiByteToWideChar(CP_ACP, 0, lpFileName, -1, wideFileName, MAX_PATH) == 0) {
        return OriginalCreateFileA(lpFileName, dwDesiredAccess, dwShareMode, lpSecurityAttributes,
                                   dwCreationDisposition, dwFlagsAndAttributes, hTemplateFile);
    }

    // Check if this is one of our emulated device paths
    if (EmulatedDeviceDefinitions::IsEmulatedDevicePath(wideFileName)) {
        int controllerIndex = EmulatedDeviceDefinitions::GetControllerIndex(wideFileName);
        _logger.InfoFormat("CreateFileA called for emulated device (controller {})", controllerIndex);
        return EmulatedDeviceDefinitions::EMULATED_DEVICE_HANDLE;
    }

    return OriginalCreateFileA(lpFileName, dwDesiredAccess, dwShareMode, lpSecurityAttributes, dwCreationDisposition,
                               dwFlagsAndAttributes, hTemplateFile);
}

BOOL WINAPI HidDeviceHook::HookedCloseHandle(HANDLE hObject) {
    if (!Enabled || hObject != EmulatedDeviceDefinitions::EMULATED_DEVICE_HANDLE) {
        return OriginalCloseHandle(hObject);
    }

    _logger.Info("CloseHandle called for emulated device");

    // Always return success for our emulated handle
    return TRUE;
}

BOOLEAN WINAPI HidDeviceHook::HookedHidDGetManufacturerString(HANDLE hidDeviceObject, PVOID buffer,
                                                              ULONG bufferLength) {
    if (!Enabled || hidDeviceObject != EmulatedDeviceDefinitions::EMULATED_DEVICE_HANDLE) {
        return OriginalHidDGetManufacturerString(hidDeviceObject, buffer, bufferLength);
    }

    _logger.Info("HidD_GetManufacturerString called for emulated device");

    if (bufferLength < sizeof(wchar_t) * (wcslen(EmulatedDeviceDefinitions::MANUFACTURER_STRING) + 1)) {
        return FALSE;
    }

    wcscpy_s(static_cast<wchar_t*>(buffer), bufferLength / sizeof(wchar_t),
             EmulatedDeviceDefinitions::MANUFACTURER_STRING);
    return TRUE;
}

BOOLEAN WINAPI HidDeviceHook::HookedHidDGetProductString(HANDLE hidDeviceObject, PVOID buffer, ULONG bufferLength) {
    if (!Enabled || hidDeviceObject != EmulatedDeviceDefinitions::EMULATED_DEVICE_HANDLE) {
        return OriginalHidDGetProductString(hidDeviceObject, buffer, bufferLength);
    }

    _logger.Info("HidD_GetProductString called for emulated device");

    if (bufferLength < sizeof(wchar_t) * (wcslen(EmulatedDeviceDefinitions::PRODUCT_STRING) + 1)) {
        return FALSE;
    }

    wcscpy_s(static_cast<wchar_t*>(buffer), bufferLength / sizeof(wchar_t), EmulatedDeviceDefinitions::PRODUCT_STRING);
    return TRUE;
}

BOOLEAN WINAPI HidDeviceHook::HookedHidDGetSerialNumberString(HANDLE hidDeviceObject, PVOID buffer,
                                                              ULONG bufferLength) {
    if (!Enabled || hidDeviceObject != EmulatedDeviceDefinitions::EMULATED_DEVICE_HANDLE) {
        return OriginalHidDGetSerialNumberString(hidDeviceObject, buffer, bufferLength);
    }

    _logger.Info("HidD_GetSerialNumberString called for emulated device");

    // Return a simple serial number
    if (bufferLength >= 2 * sizeof(wchar_t)) {
        wcscpy_s(static_cast<wchar_t*>(buffer), bufferLength / sizeof(wchar_t), L"1");
        return TRUE;
    }

    return FALSE;
}

BOOLEAN WINAPI HidDeviceHook::HookedHidDGetPreparsedData(HANDLE hidDeviceObject, PHIDP_PREPARSED_DATA* preparsedData) {
    if (!Enabled || hidDeviceObject != EmulatedDeviceDefinitions::EMULATED_DEVICE_HANDLE) {
        return OriginalHidDGetPreparsedData(hidDeviceObject, preparsedData);
    }

    _logger.Info("HidD_GetPreparsedData called for emulated device");

    // Allocate memory for the preparsed data
    *preparsedData = static_cast<PHIDP_PREPARSED_DATA>(malloc(EmulatedDeviceDefinitions::PREPROCESSED_DATA_SIZE));
    if (!*preparsedData) {
        return FALSE;
    }

    // Copy our preprocessed data
    memcpy(*preparsedData, EmulatedDeviceDefinitions::PREPROCESSED_DATA,
           EmulatedDeviceDefinitions::PREPROCESSED_DATA_SIZE);
    return TRUE;
}

BOOLEAN WINAPI HidDeviceHook::HookedHidDGetAttributes(HANDLE hidDeviceObject, PHIDD_ATTRIBUTES attributes) {
    if (!Enabled || hidDeviceObject != EmulatedDeviceDefinitions::EMULATED_DEVICE_HANDLE) {
        return OriginalHidDGetAttributes(hidDeviceObject, attributes);
    }

    _logger.Info("HidD_GetAttributes called for emulated device");

    if (!attributes) {
        return FALSE;
    }

    attributes->Size = sizeof(HIDD_ATTRIBUTES);
    attributes->VendorID = EmulatedDeviceDefinitions::VENDOR_ID;
    attributes->ProductID = EmulatedDeviceDefinitions::PRODUCT_ID;
    attributes->VersionNumber = EmulatedDeviceDefinitions::VERSION_NUMBER;

    return TRUE;
}