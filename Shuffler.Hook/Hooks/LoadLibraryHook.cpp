#include "LoadLibraryHook.h"
#include "../Utils.h"
#include "XInputHook.h"

#include <codecvt>

Logger LoadLibraryHook::_logger = Logger("LoadLibraryHook");
HookHelper LoadLibraryHook::_hookHelper;
bool LoadLibraryHook::Enabled = false;

namespace {

decltype(&LoadLibraryA) OriginalLoadLibraryA = nullptr;
decltype(&LoadLibraryW) OriginalLoadLibraryW = nullptr;
decltype(&LoadLibraryExA) OriginalLoadLibraryExA = nullptr;
decltype(&LoadLibraryExW) OriginalLoadLibraryExW = nullptr;

}  // namespace

bool LoadLibraryHook::Install() {
    _logger.Info("Installing LoadLibrary hooks...");

    bool success = true;
    success &= _hookHelper.Hook("kernel32.dll", "LoadLibraryA", &OriginalLoadLibraryA, HookedLoadLibraryA);
    success &= _hookHelper.Hook("kernel32.dll", "LoadLibraryW", &OriginalLoadLibraryW, HookedLoadLibraryW);
    success &= _hookHelper.Hook("kernel32.dll", "LoadLibraryExA", &OriginalLoadLibraryExA, HookedLoadLibraryExA);
    success &= _hookHelper.Hook("kernel32.dll", "LoadLibraryExW", &OriginalLoadLibraryExW, HookedLoadLibraryExW);

    if (success) {
        _logger.Info("LoadLibrary hooks installed successfully");
    } else {
        _logger.Error("Failed to install one or more hooks");
    }

    return success;
}

bool LoadLibraryHook::Uninstall() {
    return _hookHelper.Uninstall();
}

HMODULE WINAPI LoadLibraryHook::HookedLoadLibraryA(LPCSTR lpLibFileName) {
    if (!Enabled) {
        return OriginalLoadLibraryA(lpLibFileName);
    }

    const HMODULE result = OriginalLoadLibraryA(lpLibFileName);

    if (result && XInputHook::IsXInputModule(result)) {
        _logger.InfoFormat("XInput DLL loaded: {} {} - installing XInput hooks", (void*)result, lpLibFileName);
        XInputHook::Enabled = true;
        if (XInputHook::Install(result)) {
            _logger.Info("XInput hooks installed successfully");
        }
    }

    return result;
}

HMODULE WINAPI LoadLibraryHook::HookedLoadLibraryW(LPCWSTR lpLibFileName) {
    if (!Enabled) {
        return OriginalLoadLibraryW(lpLibFileName);
    }

    const HMODULE result = OriginalLoadLibraryW(lpLibFileName);

    if (result && XInputHook::IsXInputModule(result)) {
        _logger.InfoFormat("XInput DLL loaded: {} - installing XInput hooks", Utils::WideToMultibyte(lpLibFileName));
        XInputHook::Enabled = true;
        if (XInputHook::Install(result)) {
            _logger.Info("XInput hooks installed successfully");
        }
    }

    return result;
}

HMODULE WINAPI LoadLibraryHook::HookedLoadLibraryExA(LPCSTR lpLibFileName, HANDLE hFile, DWORD dwFlags) {
    if (!Enabled) {
        return OriginalLoadLibraryExA(lpLibFileName, hFile, dwFlags);
    }

    const HMODULE result = OriginalLoadLibraryExA(lpLibFileName, hFile, dwFlags);

    if (result && XInputHook::IsXInputModule(result)) {
        _logger.InfoFormat("XInput DLL loaded: {} - installing XInput hooks", lpLibFileName);
        XInputHook::Enabled = true;
        if (XInputHook::Install(result)) {
            _logger.Info("XInput hooks installed successfully");
        }
    }

    return result;
}

HMODULE WINAPI LoadLibraryHook::HookedLoadLibraryExW(LPCWSTR lpLibFileName, HANDLE hFile, DWORD dwFlags) {
    if (!Enabled) {
        return OriginalLoadLibraryExW(lpLibFileName, hFile, dwFlags);
    }

    const HMODULE result = OriginalLoadLibraryExW(lpLibFileName, hFile, dwFlags);

    if (result && XInputHook::IsXInputModule(result)) {
        _logger.InfoFormat("XInput DLL loaded: {} - installing XInput hooks", Utils::WideToMultibyte(lpLibFileName));
        XInputHook::Enabled = true;
        if (XInputHook::Install(result)) {
            _logger.Info("XInput hooks installed successfully");
        }
    }

    return result;
}
