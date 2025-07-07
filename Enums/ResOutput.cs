namespace VerifyPro.Enums;

public enum ResOutput : ushort
{
    HighPressureSwitch = 0,   // HPS
    LowPressureSwitch = 1,    // LPS
    DischargeSensor1 = 2,     // DISCH1
    OilSensor1 = 3,           // OIL1
    TemperatureOverload = 4,  // TO
    ExhaustLine1 = 5,         // EXL1
    ExhaustLine2 = 6,         // EXL2
    ExhaustGas1 = 7,          // EXG1
    ExhaustGas2 = 8,          // EXG2
    ShortCircuitTotal = 9,    // SCT
    ShortCircuitGas1 = 10,    // SCG1
    ShortCircuitLine1 = 11,   // SCL1
    ShortCircuitGas2 = 12,    // SCG2
    ShortCircuitLine2 = 13,   // SCL2
    StandbyOutput1 = 14,      // standby1
    StandbyOutput2 = 15       // standby2
}
