using System;
using System.Runtime.InteropServices;

namespace Xabe.FFmpeg
{
    internal class OperatingSystemProvider : IOperatingSystemProvider
    {
        public OperatingSystem GetOperatingSystem()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return OperatingSystem.Windows;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return OperatingSystem.Osx;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return OperatingSystem.Linux;

                // TODO : Как различить архитектуру Tizen / Raspberry
                // Linux (Armet) (Tizen)
                // Linux (LinuxArmhf) (для ОС на основе glibc) -> Raspberry Pi
            }

            throw new InvalidOperationException(ErrorMessages.OperatingSystemAndArchitectureMissing);
        }
    }
}
