#pragma once

#include "../Logger.h"
#include "GetProcAddressHook.h"
#include <Windows.h>
#include <detours/detours.h>
#include <string>
#include <vector>

class HookHelper {
    struct HookInfo {
        void* OriginalFunc;
        void* HookFunc;
        std::string Name;
    };

    std::vector<HookInfo> _hooks;
    Logger _logger = Logger("HookHelper");

  public:
    static decltype(&GetProcAddress) OriginalGetProcAddress;

    template <typename T>
    bool Hook(const std::string& dllName, int ordinal, T* outOriginal, T hookFunc) {
        const HMODULE lib = GetModuleHandleA(dllName.c_str());
        if (!lib) {
            _logger.ErrorFormat("Failed to get {} handle", dllName);
            return false;
        }

        const FARPROC procAddr = GetProcAddressHook::GetProcAddress(lib, MAKEINTRESOURCEA(ordinal));
        if (!procAddr) {
            _logger.ErrorFormat("Failed to get proc address for ordinal {} in {}", ordinal, dllName);
            return false;
        }

        *outOriginal = reinterpret_cast<T>(procAddr);
        if (AttachHook(reinterpret_cast<void**>(outOriginal), reinterpret_cast<void*>(hookFunc))) {
            _logger.InfoFormat("Successfully hooked ordinal {} ({})", ordinal, dllName);
            _hooks.push_back({.OriginalFunc = reinterpret_cast<void*>(*outOriginal),
                              .HookFunc = reinterpret_cast<void*>(hookFunc),
                              .Name = dllName + "::" + std::to_string(ordinal)});

            return true;
        }

        _logger.ErrorFormat("Failed to hook ordinal {} ({})", ordinal, dllName);
        return false;
    }

    template <typename T>
    bool Hook(const std::string& dllName, const std::string& funcName, T* outOriginal, T hookFunc) {
        const HMODULE lib = GetModuleHandleA(dllName.c_str());
        if (!lib) {
            _logger.ErrorFormat("Failed to get {} handle", dllName);
            return false;
        }

        const FARPROC procAddr = GetProcAddressHook::GetProcAddress(lib, funcName.c_str());
        if (!procAddr) {
            _logger.ErrorFormat("Failed to get proc address for {} in {}", funcName, dllName);
            return false;
        }

        *outOriginal = reinterpret_cast<T>(procAddr);
        if (AttachHook(reinterpret_cast<void**>(outOriginal), reinterpret_cast<void*>(hookFunc))) {
            _logger.InfoFormat("Successfully hooked {} ({})", funcName, dllName);
            _hooks.push_back({.OriginalFunc = reinterpret_cast<void*>(outOriginal),
                              .HookFunc = reinterpret_cast<void*>(hookFunc),
                              .Name = dllName + "::" + funcName});

            return true;
        }

        _logger.ErrorFormat("Failed to hook {} ({})", funcName, dllName);
        return false;
    }

    bool Uninstall() {
        bool success = true;

        DetourTransactionBegin();
        DetourUpdateThread(GetCurrentThread());
        for (auto& [OriginalFunc, HookFunc, Name] : _hooks) {
            if (!DetourDetach(&OriginalFunc, HookFunc)) {
                _logger.ErrorFormat("Failed to detach hook {}", Name);
                success = false;
            }
        }
        DetourTransactionCommit();

        if (success)
            _logger.InfoFormat("All hooks uninstalled successfully");
        else
            _logger.Error("Failed to uninstall one or more hooks");

        return success;
    }

  private:
    bool AttachHook(void** originalFunction, void* hookFunction) {
        DetourTransactionBegin();
        DetourUpdateThread(GetCurrentThread());
        DetourAttach(originalFunction, hookFunction);

        if (LONG error = DetourTransactionCommit(); error != NO_ERROR) {
            _logger.ErrorFormat("Failed to attach hook. Error: {}", error);
            return false;
        }

        return true;
    }
};
