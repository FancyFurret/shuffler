#pragma once

#include "../Logger.h"
#include "../Utils.h"
#include "HookHelper.h"
#include <Windows.h>
#include <Xinput.h>
#include <string>
#include <unordered_set>

enum class XInputVersion : uint8_t { None, XInput14, XInput13, XInput910 };

class XInputHook {
    static Logger _logger;
    static HookHelper _hookHelper;
    static decltype(&XInputGetState) _originalXInputGetState;
    static std::unordered_set<HMODULE> _hookedModules;

    struct VersionHooks {
        decltype(&XInputGetState) GetState;
        decltype(&XInputGetCapabilities) GetCapabilities;
    };

    static const char* GetDllName(XInputVersion version) {
        switch (version) {
        case XInputVersion::XInput14:
            return "xinput1_4.dll";
        case XInputVersion::XInput13:
            return "xinput1_3.dll";
        case XInputVersion::XInput910:
            return "xinput9_1_0.dll";
        default:
            return nullptr;
        }
    }

  public:
    static bool Enabled;
    static bool Install();
    static bool Install(HMODULE module);
    static bool HookExisting();
    static bool Uninstall();

    static decltype(&XInputGetState) GetOriginalXInputGetState();

    static XInputVersion GetXInputVersion(HMODULE module) {
        std::string dllName = Utils::GetDllName(module);

        if (dllName == "xinput1_4")
            return XInputVersion::XInput14;
        if (dllName == "xinput1_3")
            return XInputVersion::XInput13;
        if (dllName == "xinput9_1_0")
            return XInputVersion::XInput910;

        return XInputVersion::None;
    }

    static bool IsXInputModule(HMODULE module) {
        return GetXInputVersion(module) != XInputVersion::None;
    }

    static bool IsModuleHooked(HMODULE module) {
        return _hookedModules.contains(module);
    }

    static DWORD WINAPI HookedXInputGetState(DWORD dwUserIndex, XINPUT_STATE* pState) WIN_NOEXCEPT;
    static DWORD WINAPI HookedXInputGetCapabilities(DWORD dwUserIndex, DWORD dwFlags,
                                                    XINPUT_CAPABILITIES* pCapabilities) WIN_NOEXCEPT;

    static DWORD WINAPI HookedXInputGetState14(DWORD dwUserIndex, XINPUT_STATE* pState) WIN_NOEXCEPT;
    static DWORD WINAPI HookedXInputGetState14Ordinal(DWORD dwUserIndex, XINPUT_STATE* pState) WIN_NOEXCEPT;
    static DWORD WINAPI HookedXInputGetState13(DWORD dwUserIndex, XINPUT_STATE* pState) WIN_NOEXCEPT;
    static DWORD WINAPI HookedXInputGetState13Ordinal(DWORD dwUserIndex, XINPUT_STATE* pState) WIN_NOEXCEPT;
    static DWORD WINAPI HookedXInputGetState910(DWORD dwUserIndex, XINPUT_STATE* pState) WIN_NOEXCEPT;

    // XInput GetCapabilities hooks
    static DWORD WINAPI HookedXInputGetCapabilities14(DWORD dwUserIndex, DWORD dwFlags,
                                                      XINPUT_CAPABILITIES* pCapabilities) WIN_NOEXCEPT;
    static DWORD WINAPI HookedXInputGetCapabilities14Ordinal(DWORD dwUserIndex, DWORD dwFlags,
                                                             XINPUT_CAPABILITIES* pCapabilities) WIN_NOEXCEPT;
    static DWORD WINAPI HookedXInputGetCapabilities13(DWORD dwUserIndex, DWORD dwFlags,
                                                      XINPUT_CAPABILITIES* pCapabilities) WIN_NOEXCEPT;
    static DWORD WINAPI HookedXInputGetCapabilities13Ordinal(DWORD dwUserIndex, DWORD dwFlags,
                                                             XINPUT_CAPABILITIES* pCapabilities) WIN_NOEXCEPT;
    static DWORD WINAPI HookedXInputGetCapabilities910(DWORD dwUserIndex, DWORD dwFlags,
                                                       XINPUT_CAPABILITIES* pCapabilities) WIN_NOEXCEPT;

  private:
    static bool HookModule(XInputVersion version);
};