using BluetoothDeskApp.Services;
using Xunit;
using System.Threading.Tasks;

namespace BluetoothDeskApp.Tests;

public class SimulatorServiceTests
{
    [Fact]
    public async Task Simulator_EmitsData_AfterConnect()
    {
        var simulator = new SimulatorService();
        var gotData = false;

        simulator.DataReceived += _ => gotData = true;

        await simulator.ConnectAsync();
        await Task.Delay(1200);
        await simulator.DisconnectAsync();

        Assert.True(gotData);
    }
}
