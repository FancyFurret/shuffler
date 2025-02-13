#include "HookHelper.h"
#include "../ControllerManager.h"

decltype(&GetProcAddress) HookHelper::OriginalGetProcAddress = nullptr;