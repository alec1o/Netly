public partial class FixUdp
{
    // Skip macOS Test: Firewall isn't allowing UDP connection.
    public bool IsMac()
    {
        var value = OperatingSystem.IsMacOS();

        if (value)
        {
            // https://github.com/dotnet/runtime/issues/97718
            // https://support.apple.com/guide/mac-help/change-firewall-settings-on-mac-mh11783/mac
            output.WriteLine("macOS is skipped because firewall problem");
        }

        return value;
    }
}