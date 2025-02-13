#pragma once

#include "Logger.h"
#include <Windows.h>
#include <functional>
#include <thread>

enum class IpcMessageType : uint32_t { Enable = 1, Disable = 2, SetActiveController = 3 };

#pragma pack(push, 1)
struct IpcMessage {
    IpcMessageType Type;
    int ControllerId;
};
#pragma pack(pop)

class IpcHandler {
    using EnableCallback = std::function<void()>;
    using DisableCallback = std::function<void()>;
    using SetControllerCallback = std::function<void(int)>;

    Logger _logger = Logger("IpcHandler");

    EnableCallback _onEnable;
    DisableCallback _onDisable;
    SetControllerCallback _onSetController;
    bool _shutdownRequested;
    HANDLE _pipeHandle;
    std::thread _pipeThread;

  public:
    IpcHandler(EnableCallback onEnable, DisableCallback onDisable, SetControllerCallback onSetController);
    ~IpcHandler();

    bool Start();
    void Stop();

  private:
    void PipeServerThread();
    void HandleMessage(const IpcMessage& msg);
};