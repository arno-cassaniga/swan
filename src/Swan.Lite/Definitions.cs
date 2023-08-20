using System;
using System.Text;

namespace Swan
{
    /// <summary>
    /// Contains useful constants and definitions.
    /// </summary>
    public static partial class Definitions
    {
        /// <summary>
        /// Disables static constructors in this library. You'll need to call their initialization by yourself in case
        /// you decide to set this flag to <c>true</c>. In such case, setting this flag should be the very first
        /// interaction you have with this library.
        /// </summary>
        public static bool SuppressStaticConstructors = false;

        private static readonly Lazy<EncodingDefinitions> _encodings = new Lazy<EncodingDefinitions>(() => {
            var ansi = Encoding.GetEncoding(default(int));
            var win1252 = ansi;

            try {
                win1252 = Encoding.GetEncoding(1252);
            }
            catch { } // encoding not available, will stick to ansi for this

            return new EncodingDefinitions(ansi, win1252);
        });

        /// <summary>
        /// The MS Windows codepage 1252 encoding used in some legacy scenarios
        /// such as default CSV text encoding from Excel.
        /// </summary>
        public static Encoding Windows1252Encoding => _encodings.Value.Windows1252Encoding;

        /// <summary>
        /// The encoding associated with the default ANSI code page in the operating 
        /// system's regional and language settings.
        /// </summary>
        public static Encoding CurrentAnsiEncoding => _encodings.Value.CurrentAnsiEncoding;

        private class EncodingDefinitions
        {
            public readonly Encoding CurrentAnsiEncoding;
            public readonly Encoding Windows1252Encoding;

            public EncodingDefinitions(Encoding currentAnsiEncoding, Encoding windows1252Encoding)
            {
                this.CurrentAnsiEncoding = currentAnsiEncoding;
                this.Windows1252Encoding = windows1252Encoding;
            }
        }
    }
}
