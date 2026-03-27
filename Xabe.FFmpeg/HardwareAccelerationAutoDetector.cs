using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
namespace Xabe.FFmpeg
{
    /// <summary>
    ///     Автовыбор аппаратного ускорения по списку ffmpeg -hwaccels и ОС (NVIDIA / Intel / AMD / VAAPI / Video Toolbox).
    /// </summary>
    internal static class HardwareAccelerationAutoDetector
    {
        internal static bool TryDetect(string ffmpegExecutablePath, OperatingSystem os, out HardwareAccelerationProfile profile)
        {
            profile = null;
            if (string.IsNullOrWhiteSpace(ffmpegExecutablePath) || !System.IO.File.Exists(ffmpegExecutablePath))
            {
                return false;
            }

            if (!TryGetSupportedHwaccels(ffmpegExecutablePath, out var supported))
            {
                return false;
            }

            var priority = GetPriorityList(os);
            foreach (var name in priority)
            {
                if (!supported.Contains(name))
                {
                    continue;
                }

                profile = CreateProfileForHwaccel(name, os);
                return profile != null;
            }

            return false;
        }

        private static IReadOnlyList<string> GetPriorityList(OperatingSystem os)
        {
            switch (os)
            {
                case OperatingSystem.Windows:
                    // NVIDIA (cuda), Intel (qsv), универсальный D3D11 / DXVA2 + кодирование AMF при необходимости
                    return new[] { "cuda", "qsv", "d3d11va", "dxva2" };
                case OperatingSystem.Linux:
                    return new[] { "cuda", "vaapi", "vdpau", "qsv" };
                case OperatingSystem.Osx:
                    return new[] { "videotoolbox" };
                default:
                    return new[] { "cuda", "vaapi", "vdpau", "qsv", "videotoolbox", "d3d11va", "dxva2" };
            }
        }

        private static HardwareAccelerationProfile CreateProfileForHwaccel(string hwaccel, OperatingSystem os)
        {
            switch (hwaccel.ToLower(CultureInfo.InvariantCulture))
            {
                case "cuda":
                    return new HardwareAccelerationProfile("cuda", "h264_cuvid", "h264_nvenc", "hevc_nvenc");
                case "qsv":
                    return new HardwareAccelerationProfile("qsv", "h264_qsv", "h264_qsv", "hevc_qsv");
                case "videotoolbox":
                    return new HardwareAccelerationProfile("videotoolbox", "h264", "h264_videotoolbox", "hevc_videotoolbox");
                case "vaapi":
                    return new HardwareAccelerationProfile("vaapi", "h264", "h264_vaapi", "hevc_vaapi");
                case "d3d11va":
                case "dxva2":
                    // Декод через D3D / DXVA, кодирование через AMF (типично для AMD под Windows)
                    return new HardwareAccelerationProfile(hwaccel, "h264", "h264_amf", "hevc_amf");
                case "vdpau":
                    // Часто только ускоренный декод; кодек выхода — программный libx264, если нет отдельного кодека GPU
                    return new HardwareAccelerationProfile("vdpau", "h264", "libx264", "libx265");
                default:
                    return new HardwareAccelerationProfile(hwaccel, "h264", "libx264", "libx265");
            }
        }

        private static bool TryGetSupportedHwaccels(string ffmpegPath, out HashSet<string> supported)
        {
            supported = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using (var p = new Process
                       {
                           StartInfo =
                           {
                               FileName = ffmpegPath,
                               Arguments = "-hide_banner -hwaccels",
                               UseShellExecute = false,
                               RedirectStandardOutput = true,
                               RedirectStandardError = true,
                               CreateNoWindow = true,
                           }
                       })
                {
                    p.Start();
                    var output = p.StandardOutput.ReadToEnd();
                    var err = p.StandardError.ReadToEnd();
                    p.WaitForExit(15000);
                    foreach (var line in (output + Environment.NewLine + err).Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        var token = line.Trim();
                        if (token.Length == 0 || token.IndexOf(' ') >= 0)
                        {
                            continue;
                        }

                        if (!char.IsLetter(token[0]))
                        {
                            continue;
                        }

                        supported.Add(token);
                    }
                }
            }
            catch
            {
                return false;
            }

            return supported.Count > 0;
        }
    }
}
