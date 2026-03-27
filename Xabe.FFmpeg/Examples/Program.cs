using System;
using System.Threading.Tasks;
using Xabe.FFmpeg.Examples.Service;

namespace Xabe.FFmpeg.Examples
{
    internal static class Program
    {
        private static async Task<int> Main(string[] args)
        {
            var scenario = args.Length > 0 ? args[0].Trim().ToLowerInvariant() : "facade";

            switch (scenario)
            {
                case "facade":
                    await FacadeSamplesExample.RunAsync().ConfigureAwait(false);
                    return 0;
                case "stream-capture":
                    await StreamCaptureExample.RunAsync().ConfigureAwait(false);
                    return 0;
                case "stream-remux":
                    await StreamRemuxExample.RunAsync().ConfigureAwait(false);
                    return 0;
                case "overlay-timecode":
                    await OverlayTimecodeExample.RunAsync().ConfigureAwait(false);
                    return 0;
                case "help":
                case "--help":
                case "-h":
                    PrintUsage();
                    return 0;
                default:
                    Console.Error.WriteLine($"Unknown example '{scenario}'.");
                    PrintUsage();
                    return 1;
            }
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: dotnet run --project Xabe.FFmpeg/Examples/Xabe.FFmpeg.Examples.csproj -- <scenario>");
            Console.WriteLine("Available scenarios: facade, stream-capture, stream-remux, overlay-timecode");
        }
    }
}
