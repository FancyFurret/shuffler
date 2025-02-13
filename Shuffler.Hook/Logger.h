#pragma once
#include <chrono>
#include <format>
#include <fstream>
#include <string>

enum class LogLevel { Debug, Info, Warning, Error };

struct LogEntry {
    std::string Message;
    LogLevel Level;
    std::chrono::system_clock::time_point Timestamp;
    std::string Source;

    LogEntry(std::string msg, LogLevel lvl, std::string src)
        : Message(std::move(msg)), Level(lvl), Timestamp(std::chrono::system_clock::now()), Source(std::move(src)) {}
};

class Logger {
    static std::ofstream _file;
    std::string _name;
    static std::string _appName;

  public:
    // static Logger Create(const std::string &name);
    explicit Logger(std::string name);
    static void Init(const std::string& logPath);

    void Debug(const std::string& message);
    void Info(const std::string& message);
    void Warning(const std::string& message);
    void Error(const std::string& message);

    template <typename... Args>
    void DebugFormat(std::format_string<Args...> format, Args&&... args) {
        Debug(std::format(format, std::forward<Args>(args)...));
    }

    template <typename... Args>
    void InfoFormat(std::format_string<Args...> format, Args&&... args) {
        Info(std::format(format, std::forward<Args>(args)...));
    }

    template <typename... Args>
    void WarningFormat(std::format_string<Args...> format, Args&&... args) {
        Warning(std::format(format, std::forward<Args>(args)...));
    }

    template <typename... Args>
    void ErrorFormat(std::format_string<Args...> format, Args&&... args) {
        Error(std::format(format, std::forward<Args>(args)...));
    }

  private:
    void Log(const LogEntry& entry);
};