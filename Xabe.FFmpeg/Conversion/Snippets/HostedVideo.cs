using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaOrchestrator.Exceptions;

namespace MediaOrchestrator
{
    public partial class Conversion
    {
        internal static Task DownloadHostedVideoAsync(
            string sourceUrl,
            string outputPath,
            HostedVideoDownloadSettings settings,
            CancellationToken cancellationToken = default)
        {
            return HostedVideoDownloader.FetchAsync(sourceUrl, outputPath, settings ?? new HostedVideoDownloadSettings(), cancellationToken);
        }

        private static class HostedVideoDownloader
        {
            internal static async Task FetchAsync(
                string sourceUrl,
                string outputPath,
                HostedVideoDownloadSettings settings,
                CancellationToken cancellationToken)
            {
                if (string.IsNullOrWhiteSpace(sourceUrl))
                {
                    throw new ArgumentException(ErrorMessages.SourceUrlMustBeProvided, nameof(sourceUrl));
                }

                if (string.IsNullOrWhiteSpace(outputPath))
                {
                    throw new ArgumentException(ErrorMessages.OutputPathMustBeProvided, nameof(outputPath));
                }

                settings = NormalizeSettings(settings);
                var workingDir = Path.GetDirectoryName(Path.GetFullPath(outputPath));
                if (!string.IsNullOrEmpty(workingDir))
                {
                    Directory.CreateDirectory(workingDir);
                }

                string downloader = ResolveDownloader(settings.DownloaderPath);
                var arguments = BuildArguments(sourceUrl, outputPath, settings);
                var outputLog = new StringBuilder();
                var errorLog = new StringBuilder();

                var startInfo = new ProcessStartInfo
                {
                    FileName = downloader,
                    Arguments = string.Join(" ", arguments.Select(QuoteArgument)),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = new Process
                {
                    StartInfo = startInfo,
                    EnableRaisingEvents = true
                })
                {
                    process.OutputDataReceived += (_, args) =>
                    {
                        if (!string.IsNullOrEmpty(args.Data))
                        {
                            outputLog.AppendLine(args.Data);
                        }
                    };

                    process.ErrorDataReceived += (_, args) =>
                    {
                        if (!string.IsNullOrEmpty(args.Data))
                        {
                            errorLog.AppendLine(args.Data);
                        }
                    };

                    try
                    {
                        process.Start();
                    }
                    catch (Exception ex) when (ex is Win32Exception || ex is FileNotFoundException || ex is PlatformNotSupportedException)
                    {
                        throw new HostedVideoDownloadException(
                            ErrorMessages.HostedVideoDownloaderNotFound(downloader),
                            sourceUrl,
                            outputPath,
                            downloader,
                            innerException: ex);
                    }

                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    using (var registration = cancellationToken.Register(() => TryKill(process)))
                    {
                        var exitCode = await WaitForExitAsync(process).ConfigureAwait(false);
                        cancellationToken.ThrowIfCancellationRequested();
                        if (exitCode != 0)
                        {
                            var before = errorLog.Length > 0 ? errorLog.ToString() : outputLog.ToString();
                            throw new HostedVideoDownloadException(
                                ErrorMessages.HostedVideoDownloadFailed(downloader, exitCode),
                                sourceUrl,
                                outputPath,
                                downloader,
                                rawOutput: before);
                        }
                    }
                }
            }

            private static HostedVideoDownloadSettings NormalizeSettings(HostedVideoDownloadSettings settings)
            {
                if (settings == null)
                {
                    return new HostedVideoDownloadSettings();
                }

                if (string.IsNullOrWhiteSpace(settings.Format))
                {
                    settings.Format = FFmpegHostedVideoArguments.DefaultFormat;
                }

                if (settings.AdditionalArguments == null)
                {
                    settings.AdditionalArguments = Array.Empty<string>();
                }

                return settings;
            }

            private static string ResolveDownloader(string configuredPath)
            {
                if (!string.IsNullOrWhiteSpace(configuredPath))
                {
                    return configuredPath;
                }

                var envValue = Environment.GetEnvironmentVariable("YT_DLP_PATH");
                if (!string.IsNullOrWhiteSpace(envValue))
                {
                    return envValue;
                }

                return FFmpegHostedVideoArguments.DownloaderExecutable;
            }

            private static IEnumerable<string> BuildArguments(string url, string outputPath, HostedVideoDownloadSettings settings)
            {
                yield return FFmpegHostedVideoArguments.NoConfigFlag;
                if (settings.NoProgress)
                {
                    yield return FFmpegHostedVideoArguments.NoProgressFlag;
                }

                if (settings.NoContinue)
                {
                    yield return FFmpegHostedVideoArguments.NoContinueFlag;
                }

                if (settings.NoPart)
                {
                    yield return FFmpegHostedVideoArguments.NoPartFlag;
                }

                if (settings.NoPlaylist)
                {
                    yield return FFmpegHostedVideoArguments.NoPlaylistFlag;
                }

                yield return FFmpegHostedVideoArguments.FormatOption;
                yield return settings.Format;

                if (!string.IsNullOrWhiteSpace(settings.MergeOutputFormat))
                {
                    yield return FFmpegHostedVideoArguments.MergeOutputFormatOption;
                    yield return settings.MergeOutputFormat;
                }

                if (!string.IsNullOrWhiteSpace(MediaOrchestrator.ExecutablesPath))
                {
                    yield return FFmpegHostedVideoArguments.FfmpegLocationOption;
                    yield return MediaOrchestrator.ExecutablesPath;
                }

                if (settings.AdditionalArguments != null)
                {
                    foreach (var additional in settings.AdditionalArguments)
                    {
                        if (string.IsNullOrWhiteSpace(additional))
                        {
                            continue;
                        }

                        yield return additional;
                    }
                }

                yield return FFmpegHostedVideoArguments.OutputOption;
                yield return outputPath;
                yield return url;
            }

            private static void TryKill(Process process)
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill();
                    }
                }
                catch
                {
                }
            }

            private static Task<int> WaitForExitAsync(Process process)
            {
                if (process.HasExited)
                {
                    return Task.FromResult(process.ExitCode);
                }

                var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
                void Handler(object sender, EventArgs args)
                {
                    tcs.TrySetResult(process.ExitCode);
                    process.Exited -= Handler;
                }

                process.Exited += Handler;
                if (process.HasExited)
                {
                    process.Exited -= Handler;
                    tcs.TrySetResult(process.ExitCode);
                }

                return tcs.Task;
            }

            private static string QuoteArgument(string argument)
            {
                if (string.IsNullOrEmpty(argument))
                {
                    return "\"\"";
                }

                if (argument.Contains(' ') || argument.Contains('"'))
                {
                    return "\"" + argument.Replace("\"", "\\\"") + "\"";
                }

                return argument;
            }
        }
    }
}
