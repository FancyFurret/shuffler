#include "GetProcAddressHook.h"
#include "HookHelper.h"
#include "XInputHook.h"

Logger GetProcAddressHook::_logger = Logger("GetProcAddressHook");
HookHelper GetProcAddressHook::_hookHelper;
bool GetProcAddressHook::Enabled = false;
decltype(&GetProcAddress) GetProcAddressHook::_originalGetProcAddress = nullptr;

bool GetProcAddressHook::Install() {
    _logger.Info("Installing GetProcAddress hook...");
    bool success = _hookHelper.Hook("kernel32.dll", "GetProcAddress", &_originalGetProcAddress, HookedGetProcAddress);

    if (success) {
        _logger.Info("GetProcAddress hook installed successfully");
    } else {
        _logger.Error("Failed to install GetProcAddress hook");
    }

    return success;
}

bool GetProcAddressHook::Uninstall() {
    return _hookHelper.Uninstall();
}

FARPROC WINAPI GetProcAddressHook::HookedGetProcAddress(HMODULE hModule, LPCSTR lpProcName) {
    if (lpProcName == nullptr)
        return _originalGetProcAddress(hModule, lpProcName);

    if (!XInputHook::IsModuleHooked(hModule)) {
        return _originalGetProcAddress(hModule, lpProcName);
    } 

    XInputVersion version = XInputHook::GetXInputVersion(hModule);
    if (version == XInputVersion::None)
        return _originalGetProcAddress(hModule, lpProcName);

    FARPROC result = nullptr;

    _logger.InfoFormat("GetProcAddress called for {} in {}", (void*)hModule, Utils::GetDllName(hModule));

    // Check if lpProcName is a string (not an ordinal)
    if (HIWORD(lpProcName) != 0) {
        if (strcmp(lpProcName, "XInputGetState") == 0) {
            switch (version) {
            case XInputVersion::XInput13:
                _logger.Info("Returning XInputGetState for XInput 1.3");
                result = Utils::FunctionToFarProc(XInputHook::HookedXInputGetState13);
                break;
            case XInputVersion::XInput14:
                _logger.Info("Returning XInputGetState for XInput 1.4");
                result = Utils::FunctionToFarProc(XInputHook::HookedXInputGetState14);
                break;
            case XInputVersion::XInput910:
                _logger.Info("Returning XInputGetState for XInput 9.1.0");
                result = Utils::FunctionToFarProc(XInputHook::HookedXInputGetState910);
                break;
            default:;
            }
        } else if (strcmp(lpProcName, "XInputGetCapabilities") == 0) {
            switch (version) {
            case XInputVersion::XInput13:
                result = Utils::FunctionToFarProc(XInputHook::HookedXInputGetCapabilities13);
                break;
            case XInputVersion::XInput14:
                result = Utils::FunctionToFarProc(XInputHook::HookedXInputGetCapabilities14);
                break;
            case XInputVersion::XInput910:
                result = Utils::FunctionToFarProc(XInputHook::HookedXInputGetCapabilities910);
                break;
            default:;
            }
        }
    } else {
        const WORD ordinal = LOWORD(reinterpret_cast<DWORD_PTR>(lpProcName));
        if (ordinal == 100) {
            switch (version) {
            case XInputVersion::XInput13:
                _logger.Info("Returning XInputGetState ordinal for XInput 1.3");
                result = Utils::FunctionToFarProc(XInputHook::HookedXInputGetState13Ordinal);
                break;
            case XInputVersion::XInput14:
                _logger.Info("Returning XInputGetState ordinal for XInput 1.4");
                result = Utils::FunctionToFarProc(XInputHook::HookedXInputGetState14Ordinal);
                break;
            default:;
            }
        } else if (ordinal == 101) {
            switch (version) {
            case XInputVersion::XInput13:
                result = Utils::FunctionToFarProc(XInputHook::HookedXInputGetCapabilities13Ordinal);
                break;
            case XInputVersion::XInput14:
                result = Utils::FunctionToFarProc(XInputHook::HookedXInputGetCapabilities14Ordinal);
                break;
            default:;
            }
        }
    }

    if (!result)
        result = _originalGetProcAddress(hModule, lpProcName);

    return result;
}