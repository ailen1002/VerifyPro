namespace VerifyPro.Models;

public abstract class Device
{
    public class ModbusTcpDevice
    {
        public required string Name { get; set; }
        public required string Ip { get; set; }
        public int Port { get; set; } = 502;
    }
    public class ModbusRtuDevice
    {
        public required string Name { get; set; }
        public required string SerialPort { get; set; }
        public required byte SlaveId{ get; set; }
        public int Baud { get; set; } = 19200;
    }
    public class TestDevice
    {
        public required string Name { get; set; }
        public required string Ip { get; set; }
        public int Port { get; set; } = 9000;
    }
}