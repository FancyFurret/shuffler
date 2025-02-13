#include "ControllerManager.h"

#include "Hooks/LoadLibraryHook.h"
#include "Hooks/XInputHook.h"

Logger ControllerManager::_logger("ControllerManager");
int ControllerManager::_activeControllerIndex = 0;
uint8_t ControllerManager::_activePlayerIndex = 0;
std::vector<ControllerManager::PlayerProfile> ControllerManager::_playerProfiles;

decltype(&XInputGetState) ControllerManager::LoadXInput() {
    if (XInputHook::GetOriginalXInputGetState())
        return XInputHook::GetOriginalXInputGetState();

    // If no XInput DLL is loaded, load xinput1_4.dll ourselves
    if (LoadLibraryHook::HookedLoadLibraryA("xinput1_4.dll")) {
        _logger.Info("Did not find original XInputGetState in XInputHook, loading xinput1_4.dll ourselves");
        if (auto original = XInputHook::GetOriginalXInputGetState()) {
            _logger.Info("Got original XInputGetState from xinput1_4.dll");
            return original;
        }
    }

    return nullptr;
}

bool ControllerManager::GetState(ControllerState* state) {
    auto originalXInputGetState = XInputHook::GetOriginalXInputGetState();
    if (!originalXInputGetState) {
        originalXInputGetState = LoadXInput();
        if (!originalXInputGetState)
            return false;
    }

    XINPUT_STATE xinputState;
    DWORD result = originalXInputGetState(_activeControllerIndex, &xinputState);

    if (result != ERROR_SUCCESS)
        return false;

    // Start with clean state for remapped values
    state->ButtonStates = 0;
    state->LeftTrigger = 0;
    state->RightTrigger = 0;
    state->LeftThumbstickX = xinputState.Gamepad.sThumbLX;
    state->LeftThumbstickY = xinputState.Gamepad.sThumbLY;
    state->RightThumbstickX = xinputState.Gamepad.sThumbRX;
    state->RightThumbstickY = xinputState.Gamepad.sThumbRY;

    if (_activePlayerIndex < _playerProfiles.size()) {
        const auto& [Index, Mappings] = _playerProfiles[_activePlayerIndex];
        uint16_t remappedButtons = 0;
        bool leftTriggerRemapped = false;
        bool rightTriggerRemapped = false;

        for (const auto& [From, To] : Mappings) {
            bool sourceIsActive = false;

            if (From.Type == InputType::Button) {
                sourceIsActive = (xinputState.Gamepad.wButtons & static_cast<uint16_t>(From.Button)) != 0;
                remappedButtons |= static_cast<uint16_t>(From.Button);
            } else if (From.Type == InputType::Trigger) {
                switch (From.Trigger) {
                case TriggerInput::LeftTrigger:
                    sourceIsActive = xinputState.Gamepad.bLeftTrigger > XINPUT_GAMEPAD_TRIGGER_THRESHOLD;
                    leftTriggerRemapped = true;
                    break;
                case TriggerInput::RightTrigger:
                    sourceIsActive = xinputState.Gamepad.bRightTrigger > XINPUT_GAMEPAD_TRIGGER_THRESHOLD;
                    rightTriggerRemapped = true;
                    break;
                }
            }

            if (sourceIsActive) {
                if (To.Type == InputType::Button) {
                    state->ButtonStates |= static_cast<uint16_t>(To.Button);
                } else if (To.Type == InputType::Trigger) {
                    switch (To.Trigger) {
                    case TriggerInput::LeftTrigger:
                        state->LeftTrigger = 255;
                        break;
                    case TriggerInput::RightTrigger:
                        state->RightTrigger = 255;
                        break;
                    }
                }
            }
        }

        state->ButtonStates |= (xinputState.Gamepad.wButtons & ~remappedButtons);
        if (!leftTriggerRemapped)
            state->LeftTrigger = xinputState.Gamepad.bLeftTrigger;
        if (!rightTriggerRemapped)
            state->RightTrigger = xinputState.Gamepad.bRightTrigger;
    } else {
        state->ButtonStates = xinputState.Gamepad.wButtons;
        state->LeftTrigger = xinputState.Gamepad.bLeftTrigger;
        state->RightTrigger = xinputState.Gamepad.bRightTrigger;
    }

    return true;
}

void ControllerManager::SetActivePlayer(uint8_t playerIndex) {
    _activePlayerIndex = playerIndex;
}

void ControllerManager::AddButtonMapping(uint8_t playerIndex, ActionMapping mapping) {
    while (_playerProfiles.size() <= playerIndex) {
        PlayerProfile profile;
        profile.Index = static_cast<uint8_t>(_playerProfiles.size());
        _playerProfiles.push_back(profile);
    }

    _playerProfiles[playerIndex].Mappings.push_back(mapping);
}

void ControllerManager::ClearButtonMappings(uint8_t playerIndex) {
    if (playerIndex < _playerProfiles.size()) {
        _playerProfiles[playerIndex].Mappings.clear();
    }
}