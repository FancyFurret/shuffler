#include "ControllerManager.h"
#include "Hooks/GetProcAddressHook.h"
#include "Hooks/HidDeviceHook.h"
#include "Hooks/LoadLibraryHook.h"
#include "Hooks/RawInputHook.h"
#include "Hooks/XInputHook.h"
#include "IpcHandler.h"
#include "Logger.h"
#include "Utils.h"

#include <format>
#include <windows.h>

#pragma comment(lib, "detours.lib")

namespace {
Logger MainLogger("Main");
std::unique_ptr<IpcHandler> MainIpcHandler;

void OnEnable() {
    XInputHook::Enabled = true;
    RawInputHook::Enabled = true;
    HidDeviceHook::Enabled = true;
}

void OnDisable() {
    XInputHook::Enabled = false;
    RawInputHook::Enabled = false;
    HidDeviceHook::Enabled = false;
}

void OnSetController(int controllerId) {
    MainLogger.InfoFormat("Setting active controller to {}", controllerId);
    ControllerManager::SetActivePlayer(static_cast<uint8_t>(controllerId));
}
}  // namespace

BOOL APIENTRY DllMain(HMODULE hModule, DWORD ulReasonForCall, LPVOID lpReserved) {
    switch (ulReasonForCall) {
    case DLL_PROCESS_ATTACH:
        DisableThreadLibraryCalls(hModule);

        Logger::Init(R"(C:\Users\Myla\Documents\Dev\shuffler_hook.log)");

        XInputHook::Enabled = true;
        RawInputHook::Enabled = true;
        HidDeviceHook::Enabled = true;
        LoadLibraryHook::Enabled = true;

        if (
            !LoadLibraryHook::Install() ||
            !RawInputHook::Install() ||
            !HidDeviceHook::Install()) {
            MainLogger.Error("Failed to install hooks");
            return FALSE;
        }

        XInputHook::HookExisting();

        MainIpcHandler = std::make_unique<IpcHandler>(OnEnable, OnDisable, OnSetController);
        if (!MainIpcHandler->Start()) {
            MainLogger.Error("Failed to start IPC handler");
            return FALSE;
        }

        ControllerManager::SetActivePlayer(0);
        ControllerManager::AddButtonMapping(0, {
                                                   .From = {.Type = InputType::Button, .Button = Button::X},
                                                   .To = {.Type = InputType::Button, .Button = Button::A},
                                               });
        ControllerManager::AddButtonMapping(0, {
                                                   .From = {.Type = InputType::Trigger, .Trigger = TriggerInput::RightTrigger},
                                                   .To = {.Type = InputType::Button, .Button = Button::X},
                                               });
        ControllerManager::AddButtonMapping(0, {
                                                   .From = {.Type = InputType::Trigger, .Trigger = TriggerInput::LeftTrigger},
                                                   .To = {.Type = InputType::Trigger, .Trigger = TriggerInput::RightTrigger},
                                               });
        // ControllerManager::AddButtonMapping(0, {
        //                                            .From = {.Type = InputType::Button, .Button = Button::DPadLeft},
        //                                            .To = {.Type = InputType::Button, .Button = Button::DPadRight},
        //                                        });
        // ControllerManager::AddButtonMapping(0, {
        //                                            .From = {.Type = InputType::Button, .Button = Button::DPadRight},
        //                                            .To = {.Type = InputType::Button, .Button = Button::DPadLeft},
        //                                        });

        return TRUE;

    case DLL_PROCESS_DETACH:
        MainLogger.Info("DLL_PROCESS_DETACH");

        // Stop IPC handler first
        if (MainIpcHandler) {
            MainIpcHandler->Stop();
            MainIpcHandler.reset();
        }

        // Uninstall hooks in reverse order
        LoadLibraryHook::Uninstall();
        XInputHook::Uninstall();
        RawInputHook::Uninstall();
        HidDeviceHook::Uninstall();
        break;
    default:;
    }
    return TRUE;
}
