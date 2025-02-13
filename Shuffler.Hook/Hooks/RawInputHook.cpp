#include "RawInputHook.h"
#include "../ControllerManager.h"
#include "../EmulatedDeviceDefinitions.h"
#include "../Logger.h"
#include "HidDeviceHook.h"
#include <Xinput.h>
#include <algorithm>
#include <format>

Logger RawInputHook::_logger = Logger("RawInputHook");
HookHelper RawInputHook::_hookHelper;
bool RawInputHook::Enabled = false;

namespace {
decltype(&RegisterRawInputDevices) OriginalRegisterRawInputDevices = nullptr;
decltype(&GetRawInputData) OriginalGetRawInputData = nullptr;
decltype(&GetRawInputDeviceList) OriginalGetRawInputDeviceList = nullptr;
decltype(&GetRawInputDeviceInfoA) OriginalGetRawInputDeviceInfoA = nullptr;
decltype(&GetRawInputDeviceInfoW) OriginalGetRawInputDeviceInfoW = nullptr;
decltype(&GetRegisteredRawInputDevices) OriginalGetRegisteredRawInputDevices = nullptr;

PHIDP_PREPARSED_DATA GetPreparsedData() {
    return reinterpret_cast<PHIDP_PREPARSED_DATA>(const_cast<BYTE*>(EmulatedDeviceDefinitions::PREPROCESSED_DATA));
}

NTSTATUS SetHidValue(USAGE usage, ULONG value, PCHAR rawData, DWORD rawDataSize) {
    return HidP_SetUsageValue(HidP_Input, 0x01, 0, usage, value, GetPreparsedData(), rawData, rawDataSize);
}

ULONG RemapAxis(LONG value, bool invert = false) {
    return static_cast<ULONG>(std::clamp((invert ? -value : value) + (MAXWORD / 2), 0L, static_cast<LONG>(MAXWORD)));
}
}  // namespace

bool RawInputHook::Install() {
    _logger.Info("Installing RawInput hooks...");

    bool success = true;
    success &= _hookHelper.Hook("user32.dll", "RegisterRawInputDevices", &OriginalRegisterRawInputDevices,
                                HookedRegisterRawInputDevices);
    success &= _hookHelper.Hook("user32.dll", "GetRawInputData", &OriginalGetRawInputData, HookedGetRawInputData);
    success &= _hookHelper.Hook("user32.dll", "GetRawInputDeviceList", &OriginalGetRawInputDeviceList,
                                HookedGetRawInputDeviceList);
    success &= _hookHelper.Hook("user32.dll", "GetRawInputDeviceInfoA", &OriginalGetRawInputDeviceInfoA,
                                HookedGetRawInputDeviceInfoA);
    success &= _hookHelper.Hook("user32.dll", "GetRawInputDeviceInfoW", &OriginalGetRawInputDeviceInfoW,
                                HookedGetRawInputDeviceInfoW);
    success &= _hookHelper.Hook("user32.dll", "GetRegisteredRawInputDevices", &OriginalGetRegisteredRawInputDevices,
                                HookedGetRegisteredRawInputDevices);

    if (success) {
        _logger.Info("RawInput hooks installed successfully");
    } else {
        _logger.Error("Failed to install one or more RawInput hooks");
    }

    return success;
}

bool RawInputHook::Uninstall() {
    return _hookHelper.Uninstall();
}

BOOL WINAPI RawInputHook::HookedRegisterRawInputDevices(PCRAWINPUTDEVICE pRawInputDevices, UINT uiNumDevices,
                                                        UINT cbSize) {
    if (!Enabled) {
        return OriginalRegisterRawInputDevices(pRawInputDevices, uiNumDevices, cbSize);
    }

    _logger.InfoFormat("RegisterRawInputDevices called - devices: {}, size: {}", uiNumDevices, cbSize);

    // Log device registrations
    for (UINT i = 0; i < uiNumDevices; i++) {
        const RAWINPUTDEVICE& device = pRawInputDevices[i];
        _logger.InfoFormat("Device[{0}] - UsagePage: 0x{1:X}, Usage: 0x{2:X}, Flags: 0x{3:X}, Target: {4}", i,
                           device.usUsagePage, device.usUsage, device.dwFlags,
                           reinterpret_cast<void*>(device.hwndTarget));
    }

    return OriginalRegisterRawInputDevices(pRawInputDevices, uiNumDevices, cbSize);
}

UINT WINAPI RawInputHook::HookedGetRawInputDeviceList(PRAWINPUTDEVICELIST pRawInputDeviceList, PUINT puiNumDevices,
                                                      UINT cbSize) {
    if (!Enabled) {
        return OriginalGetRawInputDeviceList(pRawInputDeviceList, puiNumDevices, cbSize);
    }

    _logger.InfoFormat("GetRawInputDeviceList called - size: {}, devices: {}", cbSize,
                       puiNumDevices ? *puiNumDevices : 0);

    // Always report one device
    if (!pRawInputDeviceList) {
        *puiNumDevices = 1;
        _logger.Info("Reporting 1 device");
        return 0;
    }

    if (*puiNumDevices < 1) {
        *puiNumDevices = 1;
        return ERROR_INSUFFICIENT_BUFFER;
    }

    _logger.Info("Returning emulated device");
    pRawInputDeviceList[0].hDevice = EmulatedDeviceDefinitions::EMULATED_DEVICE_HANDLE;
    pRawInputDeviceList[0].dwType = RIM_TYPEHID;
    return 1;
}

UINT RawInputHook::HandleGetRawInputDeviceInfo(UINT uiCommand, LPVOID pData, PUINT pcbSize) {
    switch (uiCommand) {
    case RIDI_PREPARSEDDATA: {
        if (!pData) {
            *pcbSize = EmulatedDeviceDefinitions::PREPROCESSED_DATA_SIZE;
            return 0;
        }

        if (*pcbSize < EmulatedDeviceDefinitions::PREPROCESSED_DATA_SIZE) {
            *pcbSize = EmulatedDeviceDefinitions::PREPROCESSED_DATA_SIZE;
            return -1;
        }

        memcpy(pData, EmulatedDeviceDefinitions::PREPROCESSED_DATA, EmulatedDeviceDefinitions::PREPROCESSED_DATA_SIZE);
        return EmulatedDeviceDefinitions::PREPROCESSED_DATA_SIZE;
    }

    case RIDI_DEVICEINFO: {
        if (!pData) {
            *pcbSize = sizeof(RID_DEVICE_INFO);
            return 0;
        }

        if (*pcbSize < sizeof(RID_DEVICE_INFO)) {
            *pcbSize = sizeof(RID_DEVICE_INFO);
            return -1;
        }

        RID_DEVICE_INFO* info = static_cast<RID_DEVICE_INFO*>(pData);
        info->cbSize = sizeof(RID_DEVICE_INFO);
        info->dwType = RIM_TYPEHID;
        info->hid.dwVendorId = EmulatedDeviceDefinitions::VENDOR_ID;
        info->hid.dwProductId = EmulatedDeviceDefinitions::PRODUCT_ID;
        info->hid.dwVersionNumber = EmulatedDeviceDefinitions::VERSION_NUMBER;
        info->hid.usUsagePage = EmulatedDeviceDefinitions::USAGE_PAGE;
        info->hid.usUsage = EmulatedDeviceDefinitions::USAGE;

        _logger.InfoFormat(
            "Emulated device info - VID: 0x{0:X}, PID: 0x{1:X}, Ver: 0x{2:X}, Page: 0x{3:X}, Usage: 0x{4:X}",
            info->hid.dwVendorId, info->hid.dwProductId, info->hid.dwVersionNumber, info->hid.usUsagePage,
            info->hid.usUsage);
        return sizeof(RID_DEVICE_INFO);
    }

    default:
        _logger.InfoFormat("Unknown command: {}", uiCommand);
        return -1;
    }
}

UINT WINAPI RawInputHook::HookedGetRawInputDeviceInfoA(HANDLE hDevice, UINT uiCommand, LPVOID pData, PUINT pcbSize) {
    if (!Enabled || hDevice != EmulatedDeviceDefinitions::EMULATED_DEVICE_HANDLE) {
        return OriginalGetRawInputDeviceInfoA(hDevice, uiCommand, pData, pcbSize);
    }

    if (uiCommand == RIDI_DEVICENAME) {
        const int requiredSize = WideCharToMultiByte(CP_ACP, 0, EmulatedDeviceDefinitions::DEVICE_PATH_CONTROLLER_1, -1,
                                                     nullptr, 0, nullptr, nullptr);
        if (!pcbSize || requiredSize == 0)
            return ERROR_INVALID_PARAMETER;

        if (!pData) {
            *pcbSize = requiredSize;
            return ERROR_SUCCESS;
        }

        if (*pcbSize < static_cast<UINT>(requiredSize))
            return ERROR_INSUFFICIENT_BUFFER;

        if (!WideCharToMultiByte(CP_ACP, 0, EmulatedDeviceDefinitions::DEVICE_PATH_CONTROLLER_1, -1,
                                 static_cast<char*>(pData), *pcbSize, nullptr, nullptr)) {
            return ERROR_INVALID_PARAMETER;
        }

        return requiredSize;
    }

    return HandleGetRawInputDeviceInfo(uiCommand, pData, pcbSize);
}

UINT WINAPI RawInputHook::HookedGetRawInputDeviceInfoW(HANDLE hDevice, UINT uiCommand, LPVOID pData, PUINT pcbSize) {
    if (!Enabled || hDevice != EmulatedDeviceDefinitions::EMULATED_DEVICE_HANDLE) {
        return OriginalGetRawInputDeviceInfoW(hDevice, uiCommand, pData, pcbSize);
    }

    if (uiCommand == RIDI_DEVICENAME) {
        const size_t nameSize = (wcslen(EmulatedDeviceDefinitions::DEVICE_PATH_CONTROLLER_1) + 1) * sizeof(wchar_t);
        if (!pcbSize)
            return ERROR_INVALID_PARAMETER;

        if (!pData) {
            *pcbSize = static_cast<UINT>(nameSize);
            return ERROR_SUCCESS;
        }

        if (*pcbSize < nameSize)
            return ERROR_INSUFFICIENT_BUFFER;

        wcscpy_s(static_cast<wchar_t*>(pData), *pcbSize / sizeof(wchar_t),
                 EmulatedDeviceDefinitions::DEVICE_PATH_CONTROLLER_1);
        return static_cast<UINT>(nameSize);
    }

    return HandleGetRawInputDeviceInfo(uiCommand, pData, pcbSize);
}

UINT WINAPI RawInputHook::HookedGetRegisteredRawInputDevices(PRAWINPUTDEVICE pRawInputDevices, PUINT puiNumDevices,
                                                             UINT cbSize) {
    if (!Enabled) {
        return OriginalGetRegisteredRawInputDevices(pRawInputDevices, puiNumDevices, cbSize);
    }

    _logger.InfoFormat("GetRegisteredRawInputDevices called - size: {}", cbSize);

    if (puiNumDevices) {
        *puiNumDevices = 1;
        _logger.Info("Reporting 1 registered device");
    }

    if (pRawInputDevices) {
        if (cbSize < sizeof(RAWINPUTDEVICE)) {
            _logger.InfoFormat("Buffer too small - needed: {}, provided: {}", sizeof(RAWINPUTDEVICE), cbSize);
            return ERROR_INSUFFICIENT_BUFFER;
        }

        pRawInputDevices[0].usUsagePage = EmulatedDeviceDefinitions::USAGE_PAGE;
        pRawInputDevices[0].usUsage = EmulatedDeviceDefinitions::USAGE;
        pRawInputDevices[0].dwFlags = 0;
        pRawInputDevices[0].hwndTarget = nullptr;

        _logger.InfoFormat("Returning registered device - UsagePage: 0x{0:X}, Usage: 0x{1:X}, Flags: 0x{2:X}",
                           pRawInputDevices[0].usUsagePage, pRawInputDevices[0].usUsage, pRawInputDevices[0].dwFlags);
    }

    return ERROR_SUCCESS;
}

UINT WINAPI RawInputHook::HookedGetRawInputData(HRAWINPUT hRawInput, UINT uiCommand, LPVOID pData, PUINT pcbSize,
                                                UINT cbSizeHeader) {
    if (!Enabled)
        return OriginalGetRawInputData(hRawInput, uiCommand, pData, pcbSize, cbSizeHeader);

    switch (uiCommand) {
    case RID_INPUT: {
        constexpr UINT requiredSize = sizeof(RAWINPUTHEADER) + sizeof(RAWHID) + 12;

        if (!pData) {
            *pcbSize = requiredSize;
            return 0;
        }

        if (*pcbSize < requiredSize) {
            *pcbSize = requiredSize;
            return static_cast<UINT>(-1);
        }

        memset(pData, 0, requiredSize);
        auto* raw = static_cast<RAWINPUT*>(pData);
        raw->header.dwType = RIM_TYPEHID;
        raw->header.dwSize = sizeof(RAWINPUTHEADER);
        raw->header.hDevice = EmulatedDeviceDefinitions::EMULATED_DEVICE_HANDLE;
        raw->header.wParam = 0;
        raw->data.hid.dwSizeHid = 16;
        raw->data.hid.dwCount = 1;

        const PCHAR rawData = reinterpret_cast<PCHAR>(raw->data.hid.bRawData);
        const DWORD rawSize = raw->data.hid.dwSizeHid;

        ControllerState state;
        if (!ControllerManager::GetState(&state))
            return static_cast<UINT>(-1);

        USAGE buttonUsages[16];
        ULONG numButtons = 0;

        if (state.Bits.A)
            buttonUsages[numButtons++] = HID_BUTTON_A;
        if (state.Bits.B)
            buttonUsages[numButtons++] = HID_BUTTON_B;
        if (state.Bits.X)
            buttonUsages[numButtons++] = HID_BUTTON_X;
        if (state.Bits.Y)
            buttonUsages[numButtons++] = HID_BUTTON_Y;
        if (state.Bits.LeftShoulder)
            buttonUsages[numButtons++] = HID_BUTTON_LEFT_SHOULDER;
        if (state.Bits.RightShoulder)
            buttonUsages[numButtons++] = HID_BUTTON_RIGHT_SHOULDER;
        if (state.Bits.Back)
            buttonUsages[numButtons++] = HID_BUTTON_BACK;
        if (state.Bits.Start)
            buttonUsages[numButtons++] = HID_BUTTON_START;
        if (state.Bits.LeftThumbstick)
            buttonUsages[numButtons++] = HID_BUTTON_LEFT_THUMB;
        if (state.Bits.RightThumbstick)
            buttonUsages[numButtons++] = HID_BUTTON_RIGHT_THUMB;

        if (numButtons > 0) {
            if (FAILED(HidP_SetUsages(HidP_Input, HID_USAGE_BUTTON, 0, buttonUsages, &numButtons, GetPreparsedData(),
                                      reinterpret_cast<PCHAR>(raw->data.hid.bRawData), raw->data.hid.dwSizeHid))) {
                return static_cast<UINT>(-1);
            }
        }

        ULONG hatValue = 0;
        if (state.Bits.DpadUp && state.Bits.DpadRight)
            hatValue = HID_HAT_UP_RIGHT;
        else if (state.Bits.DpadDown && state.Bits.DpadRight)
            hatValue = HID_HAT_DOWN_RIGHT;
        else if (state.Bits.DpadDown && state.Bits.DpadLeft)
            hatValue = HID_HAT_DOWN_LEFT;
        else if (state.Bits.DpadUp && state.Bits.DpadLeft)
            hatValue = HID_HAT_UP_LEFT;
        else if (state.Bits.DpadUp)
            hatValue = HID_HAT_UP;
        else if (state.Bits.DpadRight)
            hatValue = HID_HAT_RIGHT;
        else if (state.Bits.DpadDown)
            hatValue = HID_HAT_DOWN;
        else if (state.Bits.DpadLeft)
            hatValue = HID_HAT_LEFT;

        if (hatValue != 0) {
            if (FAILED(SetHidValue(HID_USAGE_HAT, hatValue, rawData, rawSize))) {
                return static_cast<UINT>(-1);
            }
        }

        if (FAILED(SetHidValue(HID_USAGE_X, RemapAxis(state.LeftThumbstickX), rawData, rawSize)) ||
            FAILED(SetHidValue(HID_USAGE_Y, RemapAxis(state.LeftThumbstickY, true), rawData, rawSize)) ||
            FAILED(SetHidValue(HID_USAGE_RX, RemapAxis(state.RightThumbstickX), rawData, rawSize)) ||
            FAILED(SetHidValue(HID_USAGE_RY, RemapAxis(state.RightThumbstickY, true), rawData, rawSize))) {
            return static_cast<UINT>(-1);
        }

        LONG combinedTriggers = MAXWORD / 2 + -state.RightTrigger * TRIGGER_SCALE + state.LeftTrigger * TRIGGER_SCALE;
        if (FAILED(SetHidValue(HID_USAGE_Z, std::clamp(combinedTriggers, 0L, static_cast<LONG>(MAXWORD)), rawData,
                               rawSize))) {
            return static_cast<UINT>(-1);
        }

        *pcbSize = requiredSize;
        return requiredSize;
    }

    case RID_HEADER: {
        if (!pData) {
            *pcbSize = sizeof(RAWINPUTHEADER);
            return ERROR_SUCCESS;
        }

        if (*pcbSize < sizeof(RAWINPUTHEADER))
            return ERROR_INSUFFICIENT_BUFFER;

        RAWINPUTHEADER* header = static_cast<RAWINPUTHEADER*>(pData);
        header->dwType = RIM_TYPEHID;
        header->dwSize = sizeof(RAWINPUT);
        header->hDevice = EmulatedDeviceDefinitions::EMULATED_DEVICE_HANDLE;
        header->wParam = 0;
        *pcbSize = sizeof(RAWINPUTHEADER);
        _logger.Info("RID_HEADER - Returned header data");
        return ERROR_SUCCESS;
    }

    default:
        _logger.InfoFormat("Unknown command: {}", uiCommand);
        return ERROR_INVALID_PARAMETER;
    }
}