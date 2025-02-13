#include "XInputHook.h"
#include "../ControllerManager.h"
#include "../Logger.h"
#include "../Utils.h"
#include "HookHelper.h"
#include "LoadLibraryHook.h"

#include <codecvt>
#include <format>

Logger XInputHook::_logger = Logger("XInputHook");
HookHelper XInputHook::_hookHelper;
bool XInputHook::Enabled = false;
decltype(&XInputGetState) XInputHook::_originalXInputGetState = nullptr;
std::unordered_set<HMODULE> XInputHook::_hookedModules;

bool XInputHook::Install() {
    _logger.Info("Installing XInput hooks...");
    return HookExisting();
}

bool XInputHook::Install(HMODULE module) {
    if (!module) {
        _logger.Error("Invalid module handle");
        return false;
    }

    if (IsModuleHooked(module)) {
        _logger.Info("Module already hooked");
        return true;
    }

    const XInputVersion version = GetXInputVersion(module);
    if (version == XInputVersion::None) {
        _logger.Error("Invalid XInput module");
        return false;
    }

    if (HookModule(version)) {
        _hookedModules.insert(module);
        return true;
    }

    return false;
}

bool XInputHook::HookExisting() {
    _logger.Info("Checking for already-loaded XInput DLLs...");
    bool anySuccess = false;

    // LoadLibraryA("xinput1_4.dll");  // Ensure xinput1_4.dll is loaded first

    constexpr XInputVersion versions[] = {XInputVersion::XInput14, XInputVersion::XInput13, XInputVersion::XInput910};
    for (const auto version : versions) {
        const char* dllName = GetDllName(version);
        if (!dllName)
            continue;

        HMODULE module = GetModuleHandleA(dllName);
        if (module && !IsModuleHooked(module)) {
            _logger.InfoFormat("Found {} - attempting to hook...", dllName);
            if (HookModule(version)) {
                _hookedModules.insert(module);
                anySuccess = true;
                _logger.InfoFormat("Successfully hooked {}", dllName);

                // For xinput9_1_0.dll, also load xinput1_4.dll. Fixes an issue with Spelunky 2
                if (version == XInputVersion::XInput910) {
                    LoadLibraryHook::HookedLoadLibraryA("xinput1_4.dll");
                }
            }
        }
    }

    if (!anySuccess) {
        _logger.Info("No XInput DLLs found or failed to hook any of them");
    }

    return anySuccess;
}

bool XInputHook::HookModule(const XInputVersion version) {
    bool success = true;
    VersionHooks hooks = {};

    const char* dllName = GetDllName(version);
    if (!dllName)
        return false;

    // Hook XInputGetState by name
    success &= _hookHelper.Hook(dllName, "XInputGetState", &hooks.GetState, HookedXInputGetState);
    success &= _hookHelper.Hook(dllName, "XInputGetCapabilities", &hooks.GetCapabilities, HookedXInputGetCapabilities);

    if ((version == XInputVersion::XInput14 && hooks.GetState) ||
        (hooks.GetState && !_originalXInputGetState))  // Prefer 1.4 over 1.3
        _originalXInputGetState = hooks.GetState;

    // Hook XInputGetState by ordinal for 1.3 and 1.4
    if (version != XInputVersion::XInput910) {
        bool ordinalSuccess = _hookHelper.Hook(dllName, 100, &hooks.GetState, HookedXInputGetState);
        ordinalSuccess &= _hookHelper.Hook(dllName, 101, &hooks.GetCapabilities, HookedXInputGetCapabilities);
        success |= ordinalSuccess;
    }

    return success;
}

decltype(&XInputGetState) XInputHook::GetOriginalXInputGetState() {
    return _originalXInputGetState;
}

bool XInputHook::Uninstall() {
    _hookedModules.clear();
    return _hookHelper.Uninstall();
}

DWORD WINAPI XInputHook::HookedXInputGetState(DWORD dwUserIndex, XINPUT_STATE* pState) WIN_NOEXCEPT {
    if (!Enabled || dwUserIndex != 0) 
        return ERROR_DEVICE_NOT_CONNECTED;

    // Convert XInput state to our ControllerState
    ControllerState controllerState;
    if (!ControllerManager::GetState(&controllerState)) {
        _logger.Error("Failed to get controller state");
        return ERROR_DEVICE_NOT_CONNECTED;
    }

    pState->Gamepad.wButtons = controllerState.ButtonStates;
    pState->Gamepad.bLeftTrigger = controllerState.LeftTrigger;
    pState->Gamepad.bRightTrigger = controllerState.RightTrigger;
    pState->Gamepad.sThumbLX = controllerState.LeftThumbstickX;
    pState->Gamepad.sThumbLY = controllerState.LeftThumbstickY;
    pState->Gamepad.sThumbRX = controllerState.RightThumbstickX;
    pState->Gamepad.sThumbRY = controllerState.RightThumbstickY;

    return ERROR_SUCCESS;
}

DWORD WINAPI XInputHook::HookedXInputGetCapabilities(DWORD dwUserIndex, DWORD dwFlags,
                                                     XINPUT_CAPABILITIES* pCapabilities) WIN_NOEXCEPT {
    if (!Enabled || !pCapabilities || dwUserIndex != 0) {
        return ERROR_DEVICE_NOT_CONNECTED;
    }

    // Set up the capabilities structure for a standard gamepad
    pCapabilities->Type = XINPUT_DEVTYPE_GAMEPAD;
    pCapabilities->SubType = XINPUT_DEVSUBTYPE_GAMEPAD;
    pCapabilities->Flags = 0;  // No special flags

    // Set up supported features
    pCapabilities->Gamepad.wButtons = XINPUT_GAMEPAD_DPAD_UP | XINPUT_GAMEPAD_DPAD_DOWN | XINPUT_GAMEPAD_DPAD_LEFT |
                                      XINPUT_GAMEPAD_DPAD_RIGHT | XINPUT_GAMEPAD_START | XINPUT_GAMEPAD_BACK |
                                      XINPUT_GAMEPAD_LEFT_THUMB | XINPUT_GAMEPAD_RIGHT_THUMB |
                                      XINPUT_GAMEPAD_LEFT_SHOULDER | XINPUT_GAMEPAD_RIGHT_SHOULDER | XINPUT_GAMEPAD_A |
                                      XINPUT_GAMEPAD_B | XINPUT_GAMEPAD_X | XINPUT_GAMEPAD_Y;

    // Set up analog input ranges
    pCapabilities->Gamepad.bLeftTrigger = 255;
    pCapabilities->Gamepad.bRightTrigger = 255;
    pCapabilities->Gamepad.sThumbLX = 32767;
    pCapabilities->Gamepad.sThumbLY = 32767;
    pCapabilities->Gamepad.sThumbRX = 32767;
    pCapabilities->Gamepad.sThumbRY = 32767;

    // Vibration not supported in this implementation
    pCapabilities->Vibration.wLeftMotorSpeed = 0;
    pCapabilities->Vibration.wRightMotorSpeed = 0;

    return ERROR_SUCCESS;
}

DWORD WINAPI XInputHook::HookedXInputGetState14(_In_ DWORD dwUserIndex, _Out_ XINPUT_STATE* pState) WIN_NOEXCEPT {
    return HookedXInputGetState(dwUserIndex, pState);
}

DWORD WINAPI XInputHook::HookedXInputGetState14Ordinal(_In_ DWORD dwUserIndex,
                                                       _Out_ XINPUT_STATE* pState) WIN_NOEXCEPT {
    return HookedXInputGetState(dwUserIndex, pState);
}

DWORD WINAPI XInputHook::HookedXInputGetState13(_In_ DWORD dwUserIndex, _Out_ XINPUT_STATE* pState) WIN_NOEXCEPT {
    return HookedXInputGetState(dwUserIndex, pState);
}

DWORD WINAPI XInputHook::HookedXInputGetState13Ordinal(_In_ DWORD dwUserIndex,
                                                       _Out_ XINPUT_STATE* pState) WIN_NOEXCEPT {
    return HookedXInputGetState(dwUserIndex, pState);
}

DWORD WINAPI XInputHook::HookedXInputGetState910(_In_ DWORD dwUserIndex, _Out_ XINPUT_STATE* pState) WIN_NOEXCEPT {
    return HookedXInputGetState(dwUserIndex, pState);
}

DWORD WINAPI XInputHook::HookedXInputGetCapabilities14(DWORD dwUserIndex, DWORD dwFlags,
                                                       XINPUT_CAPABILITIES* pCapabilities) WIN_NOEXCEPT {
    return HookedXInputGetCapabilities(dwUserIndex, dwFlags, pCapabilities);
}

DWORD WINAPI XInputHook::HookedXInputGetCapabilities14Ordinal(DWORD dwUserIndex, DWORD dwFlags,
                                                              XINPUT_CAPABILITIES* pCapabilities) WIN_NOEXCEPT {
    return HookedXInputGetCapabilities(dwUserIndex, dwFlags, pCapabilities);
}

DWORD WINAPI XInputHook::HookedXInputGetCapabilities13(DWORD dwUserIndex, DWORD dwFlags,
                                                       XINPUT_CAPABILITIES* pCapabilities) WIN_NOEXCEPT {
    return HookedXInputGetCapabilities(dwUserIndex, dwFlags, pCapabilities);
}

DWORD WINAPI XInputHook::HookedXInputGetCapabilities13Ordinal(DWORD dwUserIndex, DWORD dwFlags,
                                                              XINPUT_CAPABILITIES* pCapabilities) WIN_NOEXCEPT {
    return HookedXInputGetCapabilities(dwUserIndex, dwFlags, pCapabilities);
}

DWORD WINAPI XInputHook::HookedXInputGetCapabilities910(DWORD dwUserIndex, DWORD dwFlags,
                                                        XINPUT_CAPABILITIES* pCapabilities) WIN_NOEXCEPT {
    return HookedXInputGetCapabilities(dwUserIndex, dwFlags, pCapabilities);
}
