#pragma once

#include "../Logger.h"
#include "HookHelper.h"

#include <Windows.h>
#include <Xinput.h>
#include <hidusage.h>

class RawInputHook {
    static Logger _logger;
    static HookHelper _hookHelper;

  public:
    static bool Enabled;
    static bool Install();
    static bool Uninstall();

  private:
    static constexpr USAGE HID_USAGE_BUTTON = 0x09;
    static constexpr USAGE HID_USAGE_X = 0x30;
    static constexpr USAGE HID_USAGE_Y = 0x31;
    static constexpr USAGE HID_USAGE_Z = 0x32;
    static constexpr USAGE HID_USAGE_RX = 0x33;
    static constexpr USAGE HID_USAGE_RY = 0x34;
    static constexpr USAGE HID_USAGE_HAT = 0x39;

    static constexpr USAGE HID_BUTTON_A = 1;
    static constexpr USAGE HID_BUTTON_B = 2;
    static constexpr USAGE HID_BUTTON_X = 3;
    static constexpr USAGE HID_BUTTON_Y = 4;
    static constexpr USAGE HID_BUTTON_LEFT_SHOULDER = 5;
    static constexpr USAGE HID_BUTTON_RIGHT_SHOULDER = 6;
    static constexpr USAGE HID_BUTTON_BACK = 7;
    static constexpr USAGE HID_BUTTON_START = 8;
    static constexpr USAGE HID_BUTTON_LEFT_THUMB = 9;
    static constexpr USAGE HID_BUTTON_RIGHT_THUMB = 10;

    static constexpr ULONG HID_HAT_UP = 1;
    static constexpr ULONG HID_HAT_UP_RIGHT = 2;
    static constexpr ULONG HID_HAT_RIGHT = 3;
    static constexpr ULONG HID_HAT_DOWN_RIGHT = 4;
    static constexpr ULONG HID_HAT_DOWN = 5;
    static constexpr ULONG HID_HAT_DOWN_LEFT = 6;
    static constexpr ULONG HID_HAT_LEFT = 7;
    static constexpr ULONG HID_HAT_UP_LEFT = 8;

    static constexpr LONG TRIGGER_SCALE = 128;

    static BOOL WINAPI HookedRegisterRawInputDevices(PCRAWINPUTDEVICE pRawInputDevices, UINT uiNumDevices, UINT cbSize);
    static UINT WINAPI HookedGetRawInputData(HRAWINPUT hRawInput, UINT uiCommand, LPVOID pData, PUINT pcbSize,
                                             UINT cbSizeHeader);
    static UINT WINAPI HookedGetRawInputDeviceList(PRAWINPUTDEVICELIST pRawInputDeviceList, PUINT puiNumDevices,
                                                   UINT cbSize);
    static UINT WINAPI HookedGetRawInputDeviceInfoA(HANDLE hDevice, UINT uiCommand, LPVOID pData, PUINT pcbSize);
    static UINT WINAPI HookedGetRawInputDeviceInfoW(HANDLE hDevice, UINT uiCommand, LPVOID pData, PUINT pcbSize);
    static UINT WINAPI HookedGetRegisteredRawInputDevices(PRAWINPUTDEVICE pRawInputDevices, PUINT puiNumDevices,
                                                          UINT cbSize);

    static UINT HandleGetRawInputDeviceInfo(UINT uiCommand, LPVOID pData, PUINT pcbSize);
};