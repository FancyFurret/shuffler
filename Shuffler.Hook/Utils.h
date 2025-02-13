#pragma once

#include "ControllerManager.h"
#include <algorithm>
#include <intrin.h>
#include <string>

class Utils {
  public:
    static std::string WideToMultibyte(const wchar_t* wideStr, const UINT codePage = CP_UTF8) {
        if (!wideStr)
            return "";  // Handle null pointer safely

        const int sizeNeeded = WideCharToMultiByte(codePage, 0, wideStr, -1, nullptr, 0, nullptr, nullptr);
        if (sizeNeeded <= 0)
            return "";  // Conversion failed

        std::string multibyteStr(sizeNeeded - 1, 0);  // -1 to remove null terminator in std::string
        WideCharToMultiByte(codePage, 0, wideStr, -1, multibyteStr.data(), sizeNeeded, nullptr, nullptr);
        return multibyteStr;
    }

    template <typename T>
    static FARPROC FunctionToFarProc(T function) {
        union {
            T Function;
            FARPROC FarProc;
        } u;
        u.Function = function;
        return u.FarProc;
    }

    static std::string GetDllName(HMODULE module) {
        char modulePath[MAX_PATH];
        if (GetModuleFileNameA(module, modulePath, MAX_PATH) == 0) {
            return "";
        }

        std::string path(modulePath);
        const size_t lastSlash = path.find_last_of('\\');
        std::string fileName = (lastSlash != std::string::npos) ? path.substr(lastSlash + 1) : path;

        // Convert to lowercase
        std::ranges::transform(fileName, fileName.begin(), tolower);

        // Remove .dll extension if present
        const size_t dotPos = fileName.find(".dll");
        if (dotPos != std::string::npos) {
            fileName = fileName.substr(0, dotPos);
        }

        return fileName;
    }
};