#include "IpcHandler.h"
#include "Logger.h"

IpcHandler::IpcHandler(EnableCallback onEnable, DisableCallback onDisable, SetControllerCallback onSetController)
    : _onEnable(std::move(onEnable)),
      _onDisable(std::move(onDisable)),
      _onSetController(std::move(onSetController)),
      _shutdownRequested(false),
      _pipeHandle(INVALID_HANDLE_VALUE) {}

IpcHandler::~IpcHandler() {
    Stop();
}

bool IpcHandler::Start() {
    _logger.Info("Starting IPC handler...");
    _shutdownRequested = false;
    _pipeThread = std::thread(&IpcHandler::PipeServerThread, this);
    return true;
}

void IpcHandler::Stop() {
    _logger.Info("Stopping IPC handler...");
    _shutdownRequested = true;

    if (_pipeHandle != INVALID_HANDLE_VALUE) {
        CloseHandle(_pipeHandle);
        _pipeHandle = INVALID_HANDLE_VALUE;
    }

    if (_pipeThread.joinable()) {
        _pipeThread.join();
    }
}

void IpcHandler::PipeServerThread() {
    auto pipeName = std::format(R"(\\.\pipe\ShufflerHook-{})", GetCurrentProcessId());
    _logger.InfoFormat("IPC server thread started. Pipe name: {}", pipeName);

    while (!_shutdownRequested) {
        _pipeHandle = CreateNamedPipeA(pipeName.c_str(), PIPE_ACCESS_DUPLEX,
                                       PIPE_TYPE_MESSAGE | PIPE_READMODE_MESSAGE | PIPE_WAIT, 1, sizeof(IpcMessage),
                                       sizeof(IpcMessage), 0, nullptr);

        if (_pipeHandle == INVALID_HANDLE_VALUE) {
            _logger.Info("Failed to create named pipe");
            return;
        }

        _logger.Info("Waiting for client connection...");
        if (ConnectNamedPipe(_pipeHandle, nullptr)) {
            _logger.Info("Client connected");

            while (!_shutdownRequested) {
                IpcMessage msg;
                DWORD bytesRead;

                if (ReadFile(_pipeHandle, &msg, sizeof(msg), &bytesRead, nullptr)) {
                    if (bytesRead == sizeof(msg))
                        HandleMessage(msg);
                } else {
                    _logger.Info("Client disconnected or error");
                    break;
                }
            }
        }

        CloseHandle(_pipeHandle);
        _pipeHandle = INVALID_HANDLE_VALUE;

        if (_shutdownRequested)
            break;
    }

    _logger.Info("IPC server thread stopped");
}

void IpcHandler::HandleMessage(const IpcMessage& msg) {
    switch (msg.Type) {
    case IpcMessageType::Enable:
        _logger.Info("IPC: Enable hooks");
        if (_onEnable)
            _onEnable();
        break;

    case IpcMessageType::Disable:
        _logger.Info("IPC: Disable hooks");
        if (_onDisable)
            _onDisable();
        break;

    case IpcMessageType::SetActiveController:
        _logger.InfoFormat("IPC: Set active controller to {}", msg.ControllerId);
        if (_onSetController)
            _onSetController(msg.ControllerId);
        break;
    }
}