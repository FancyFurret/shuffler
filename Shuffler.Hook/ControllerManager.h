#pragma once

#include "Logger.h"
#include <Windows.h>
#include <Xinput.h>
#include <unordered_map>

#define XINPUT_GAMEPAD_TRIGGER_THRESHOLD 30

enum class Button : uint16_t {
    DPadUp = 0x0001,
    DPadDown = 0x0002,
    DPadLeft = 0x0004,
    DPadRight = 0x0008,
    Start = 0x0010,
    Back = 0x0020,
    LeftThumbstick = 0x0040,
    RightThumbstick = 0x0080,
    LeftShoulder = 0x0100,
    RightShoulder = 0x0200,
    A = 0x1000,
    B = 0x2000,
    X = 0x4000,
    Y = 0x8000,
};

enum class TriggerInput : uint8_t { LeftTrigger, RightTrigger };

struct ButtonBits {
    uint16_t DpadUp : 1;           // 0x0001
    uint16_t DpadDown : 1;         // 0x0002
    uint16_t DpadLeft : 1;         // 0x0004
    uint16_t DpadRight : 1;        // 0x0008
    uint16_t Start : 1;            // 0x0010
    uint16_t Back : 1;             // 0x0020
    uint16_t LeftThumbstick : 1;   // 0x0040
    uint16_t RightThumbstick : 1;  // 0x0080
    uint16_t LeftShoulder : 1;     // 0x0100
    uint16_t RightShoulder : 1;    // 0x0200
    uint16_t : 2;                  // 2 reserved bits
    uint16_t A : 1;                // 0x1000
    uint16_t B : 1;                // 0x2000
    uint16_t X : 1;                // 0x4000
    uint16_t Y : 1;                // 0x8000
};

struct ControllerState {
    union {
        uint16_t ButtonStates;  // All button states as a single value
        ButtonBits Bits;
    };
    uint8_t LeftTrigger;
    uint8_t RightTrigger;
    int16_t LeftThumbstickX;
    int16_t LeftThumbstickY;
    int16_t RightThumbstickX;
    int16_t RightThumbstickY;
};

enum class InputType : uint8_t {
    Button,
    Trigger,
};

struct InputAction {
    InputType Type;
    union {
        Button Button;
        TriggerInput Trigger;
    };
};

struct ActionMapping {
    InputAction From;
    InputAction To;
};

class ControllerManager {
    struct PlayerProfile {
        uint8_t Index;
        std::vector<ActionMapping> Mappings;
    };

    static Logger _logger;
    static int _activeControllerIndex;
    static uint8_t _activePlayerIndex;
    static std::vector<PlayerProfile> _playerProfiles;

  public:
    static bool GetState(ControllerState* state);
    static void SetActivePlayer(uint8_t playerIndex);

    static void AddButtonMapping(uint8_t playerIndex, ActionMapping mapping);
    static void ClearButtonMappings(uint8_t playerIndex);

  private:
    static decltype(&XInputGetState) LoadXInput();
};