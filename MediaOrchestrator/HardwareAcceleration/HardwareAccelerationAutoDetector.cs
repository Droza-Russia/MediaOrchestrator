using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace MediaOrchestrator
{
    /// <summary>
    ///     Автовыбор аппаратного ускорения по списку ffmpeg -hwaccels и ОС (NVIDIA / Intel / AMD / VAAPI / Video Toolbox).
    /// </summary>
    internal static class HardwareAccelerationAutoDetector
    {
        internal static bool TryDetect(string ffmpegExecutablePath, OperatingSystem os, CancellationToken cancellationToken, out HardwareAccelerationProfile profile)
        {
            profile = null;
            if (string.IsNullOrWhiteSpace(ffmpegExecutablePath) || !System.IO.File.Exists(ffmpegExecutablePath))
            {
                return false;
            }

            if (!TryGetSupportedHwaccels(ffmpegExecutablePath, cancellationToken, out var supported))
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

        private static bool TryGetSupportedHwaccels(string ffmpegPath, CancellationToken cancellationToken, out HashSet<string> supported)
        {
            supported = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            cancellationToken.ThrowIfCancellationRequested();
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
                    using (cancellationToken.Register(() =>
                           {
                               try
                               {
                                   if (!p.HasExited)
                                   {
                                       p.Kill();
                                   }
                               }
                               catch
                               {
                                   // ignore
                               }
                           }))
                    {
                        p.Start();
                        var stdoutTask = Task.Run(() => p.StandardOutput.ReadToEnd(), CancellationToken.None);
                        var stderrTask = Task.Run(() => p.StandardError.ReadToEnd(), CancellationToken.None);

                        const int timeoutMs = 15000;
                        var sw = Stopwatch.StartNew();
                        while (!p.WaitForExit(50))
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            if (sw.ElapsedMilliseconds > timeoutMs)
                            {
                                try
                                {
                                    if (!p.HasExited)
                                    {
                                        p.Kill();
                                    }
                                }
                                catch
                                {
                                    // ignore
                                }

                                return false;
                            }
                        }

                        cancellationToken.ThrowIfCancellationRequested();
                        Task.WaitAll(stdoutTask, stderrTask);

                        var output = stdoutTask.Result;
                        var err = stderrTask.Result;
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
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                return false;
            }

            return supported.Count > 0;
        }
    }
}
