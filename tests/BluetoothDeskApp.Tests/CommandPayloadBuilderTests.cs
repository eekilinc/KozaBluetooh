using System;
using BluetoothDeskApp.Services;
using Xunit;

namespace BluetoothDeskApp.Tests;

public class CommandPayloadBuilderTests
{
    [Fact]
    public void Build_AsciiWithCrLf_AppendsLineEnding()
    {
        var payload = CommandPayloadBuilder.Build("STATUS", "ASCII", "CRLF");

        Assert.Equal(new byte[] { 0x53, 0x54, 0x41, 0x54, 0x55, 0x53, 0x0D, 0x0A }, payload);
    }

    [Fact]
    public void Build_HexNone_ParsesBytes()
    {
        var payload = CommandPayloadBuilder.Build("AA 01 FF", "HEX", "NONE");

        Assert.Equal(new byte[] { 0xAA, 0x01, 0xFF }, payload);
    }

    [Fact]
    public void ParseHex_InvalidLength_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => CommandPayloadBuilder.ParseHex("AAB"));
    }
}
