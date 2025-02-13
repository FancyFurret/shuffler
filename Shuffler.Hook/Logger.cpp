#include "Logger.h"
#include <Windows.h>

std::ofstream Logger::_file;
std::string Logger::_appName;

void Logger::Init(const std::string& logPath) {
    _file.open(logPath, std::ios::app);
    if (CHAR processName[MAX_PATH]; GetModuleFileNameA(nullptr, processName, MAX_PATH) > 0) {
        _appName = std::string(processName);
        if (const size_t pos = _appName.find_last_of('\\'); pos != std::string::npos) {
            _appName = _appName.substr(pos + 1);
        }
    } else {
        _appName = "Unknown";
    }
}

Logger::Logger(std::string name) : _name(std::move(name)) {}

void Logger::Log(const LogEntry& entry) {
    if (!_file.is_open())
        return;

    // Get current time
    const auto now = std::chrono::system_clock::now();
    const auto timeT = std::chrono::system_clock::to_time_t(now);
    tm localTime;
    localtime_s(&localTime, &timeT);

    // Convert LogLevel to string
    std::string levelStr;
    switch (entry.Level) {
    case LogLevel::Debug:
        levelStr = "DEBUG";
        break;
    case LogLevel::Info:
        levelStr = "INFO";
        break;
    case LogLevel::Warning:
        levelStr = "WARN";
        break;
    case LogLevel::Error:
        levelStr = "ERROR";
        break;
    }

    char timeStr[32];
    std::strftime(timeStr, sizeof(timeStr), "%Y-%m-%d %H:%M:%S", &localTime);

    const auto logMessage = std::format("[{}] [{}] [PID:{}] [{}] [{}] {}\n", timeStr, _appName, GetCurrentProcessId(),
                                        levelStr, entry.Source, entry.Message);

    _file << logMessage;
    _file.flush();
}

void Logger::Debug(const std::string& message) {
    Log(LogEntry(message, LogLevel::Debug, _name));
}

void Logger::Info(const std::string& message) {
    Log(LogEntry(message, LogLevel::Info, _name));
}

void Logger::Warning(const std::string& message) {
    Log(LogEntry(message, LogLevel::Warning, _name));
}

void Logger::Error(const std::string& message) {
    Log(LogEntry(message, LogLevel::Error, _name));
}