
#pragma warning disable IDE1006 // Naming Styles

namespace MediaOrchestrator
{
    internal class FormatModel
    {
        private FormatModel()
        {
        }

        internal class Root
        {
            public Format format { get; set; }
        }

        internal class Tags
        {
            public string creation_time { get; set; }
        }

        internal class Format
        {
            public string format_name { get; set; }

            public string size { get; set; }

            public long bit_Rate { get; set; }

            public double duration { get; set; }

            public Tags tags { get; set; }
        }
    }
}
#pragma warning restore IDE1006 // Naming Styles
