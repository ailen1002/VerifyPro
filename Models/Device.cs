namespace VerifyPro.Models;

public abstract class Device
{
    public class ModbusDevice
    {
        public required string Name { get; set; }
        public required string Ip { get; set; }
        public int Port { get; set; } = 502;
    }
    public class TestDevice
    {
        public required string Name { get; set; }
        public required string Ip { get; set; }
        public int Port { get; set; } = 9000;
    }
}