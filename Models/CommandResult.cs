namespace VerifyPro.Models;

public struct CommandResult
{
    public string CommandName { get; set; }
    public bool Success { get; set; }
    public byte[] Response { get; set; }
    public int ActualLength { get; set; }
}