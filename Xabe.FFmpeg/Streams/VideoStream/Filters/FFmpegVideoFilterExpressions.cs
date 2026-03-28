using System;
using System.Globalization;
using System.Text;

namespace Xabe.FFmpeg
{
    internal static class FFmpegVideoFilterExpressions
    {
        internal static string Rotate(RotateDegrees rotateDegrees)
        {
            return rotateDegrees == RotateDegrees.Invert
                ? "transpose=2,transpose=2"
                : $"transpose={(int)rotateDegrees}";
        }

        internal static string PadToFit(int width, int height)
        {
            return $"scale={width}:{height}:force_original_aspect_ratio=decrease,pad={width}:{height}:-1:-1:color=black";
        }

        internal static string Reverse()
        {
            return "reverse";
        }

        internal static string BuildSubtitles(string subtitlePath, VideoSize? originalSize, string encode, string style)
        {
            var filter = $"'{subtitlePath}'".Replace("\\", "\\\\")
                                           .Replace(":", "\\:");
            if (!string.IsNullOrEmpty(encode))
            {
                filter += $":charenc={encode}";
            }

            if (!string.IsNullOrEmpty(style))
            {
                filter += $":force_style=\'{style}\'";
            }

            if (originalSize != null)
            {
                filter += $":original_size={originalSize.Value.ToFFmpegFormat()}";
            }

            return filter;
        }

        internal static string BuildOverlayPosition(Position position)
        {
            switch (position)
            {
                case Position.Bottom:
                    return "(main_w-overlay_w)/2:main_h-overlay_h";
                case Position.Center:
                    return "x=(main_w-overlay_w)/2:y=(main_h-overlay_h)/2";
                case Position.BottomLeft:
                    return "5:main_h-overlay_h";
                case Position.UpperLeft:
                    return "5:5";
                case Position.BottomRight:
                    return "(main_w-overlay_w):main_h-overlay_h";
                case Position.UpperRight:
                    return "(main_w-overlay_w):5";
                case Position.Left:
                    return "5:(main_h-overlay_h)/2";
                case Position.Right:
                    return "(main_w-overlay_w-5):(main_h-overlay_h)/2";
                case Position.Up:
                    return "(main_w-overlay_w)/2:5";
                default:
                    throw new ArgumentOutOfRangeException(nameof(position), position, null);
            }
        }

        internal static string BuildDrawText(
            string textOrTimecodeClause,
            string fontColor,
            int fontSize,
            int marginRight,
            int marginY,
            DrawTextVerticalAlign verticalAlign,
            string fontFilePath)
        {
            var x = $"w-tw-{marginRight.ToString(CultureInfo.InvariantCulture)}";
            var y = BuildDrawTextYExpression(verticalAlign, marginY);
            var font = BuildFontFileClause(fontFilePath);
            return $"{textOrTimecodeClause}:fontcolor={fontColor}:fontsize={fontSize.ToString(CultureInfo.InvariantCulture)}:x={x}:y={y}{font}";
        }

        internal static string BuildDrawTextClause(string text)
        {
            return $"text='{EscapeDrawTextQuotedContent(text)}'";
        }

        internal static string BuildPtsTimeClause(string prefix, string suffix, bool useLocalWallClock)
        {
            var inner = new StringBuilder();
            if (!string.IsNullOrEmpty(prefix))
            {
                inner.Append(EscapeDrawTextQuotedContent(prefix));
            }

            inner.Append(useLocalWallClock ? "%{localtime}" : "%{pts\\:hms}");

            if (!string.IsNullOrEmpty(suffix))
            {
                inner.Append(EscapeDrawTextQuotedContent(suffix));
            }

            return $"text='{inner}'";
        }

        internal static string BuildSmpteTimecodeClause(string startTimecode, double frameRate)
        {
            if (frameRate <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(frameRate));
            }

            var tc = string.IsNullOrWhiteSpace(startTimecode) ? "00:00:00:00" : startTimecode.Trim();
            var escapedTc = EscapeTimecodeColonsForDrawText(tc);
            var rateStr = frameRate.ToString(CultureInfo.InvariantCulture);
            return $"timecode='{escapedTc}':rate={rateStr}";
        }

        private static string BuildDrawTextYExpression(DrawTextVerticalAlign align, int marginY)
        {
            switch (align)
            {
                case DrawTextVerticalAlign.Top:
                    return marginY.ToString(CultureInfo.InvariantCulture);
                case DrawTextVerticalAlign.Bottom:
                    return $"h-th-{marginY.ToString(CultureInfo.InvariantCulture)}";
                default:
                    return "(h-th)/2";
            }
        }

        private static string BuildFontFileClause(string fontFilePath)
        {
            if (string.IsNullOrWhiteSpace(fontFilePath))
            {
                return string.Empty;
            }

            var full = global::System.IO.Path.GetFullPath(fontFilePath);
            var escaped = full.Replace("\\", "/").Replace(":", "\\:").Replace("'", "\\'");
            return $":fontfile='{escaped}'";
        }

        private static string EscapeDrawTextQuotedContent(string text)
        {
            return text
                .Replace("\\", "\\\\")
                .Replace("'", "\\'")
                .Replace(":", "\\:");
        }

        private static string EscapeTimecodeColonsForDrawText(string timecode)
        {
            return timecode.Replace(":", "\\:").Replace("'", "\\'");
        }
    }
}
