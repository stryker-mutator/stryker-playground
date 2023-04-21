using System.Runtime.InteropServices;

namespace Stryker.Playground.Domain;

public static class PlatformUtil
{
    public static bool IsRunningOnWasm => RuntimeInformation.IsOSPlatform(OSPlatform.Create("WEBASSEMBLY"));
}