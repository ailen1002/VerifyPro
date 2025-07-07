using System;

namespace VerifyPro.Enums;

public enum RelayOutput : ushort
{
    ApSwitch = 0,
    TestSwitch = 1,
    SnowSwitch = 2,
    SilentSwitch = 3,
    Drm1Switch = 4,
    Drm2Switch = 5,
    ForcedStopSwitch = 6,
    NumCompSwitch = 7,
    Boot = 8,
    UbSwitch = 9,
    MiconResetSwitch = 10,
    HicSwitch = 11,
    HighPressureSwitch = 12
}