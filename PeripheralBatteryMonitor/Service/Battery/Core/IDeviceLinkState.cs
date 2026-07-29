namespace PeripheralBatteryMonitor.Service.Battery.Core
{
    /// <summary>
    /// Optional capability a provider may implement when it maintains a live link to the
    /// device (e.g. GATT). Lets <c>BatteryDevice.IsConnected()</c> report connection state
    /// cheaply -- <see cref="IsLinkUp"/> must NOT perform I/O; it only reports cached state.
    /// </summary>
    public interface IDeviceLinkState
    {
        bool IsLinkUp(IBatteryDeviceContext ctx);
    }
}
