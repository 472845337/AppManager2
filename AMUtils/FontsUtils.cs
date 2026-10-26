using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;

namespace AMUtils {
    internal class FontsUtils {

        internal static List<string> GetSystemFonts() {
            InstalledFontCollection installedFontCollection = new InstalledFontCollection();
            List<string> systemFonts = new List<string>();
            foreach (FontFamily fontFamily in installedFontCollection.Families) {
                if (fontFamily.IsStyleAvailable(FontStyle.Regular)) {
                    systemFonts.Add(fontFamily.Name);
                }
            }
            systemFonts.Reverse();
            return systemFonts;
        }

        internal static Font GetDrawingFont(string fontFamily, int fontSize, bool fontBold, bool fontItalic, bool fontUnderline, bool fontStrikeout) {
            FontStyle fontStyle = FontStyle.Regular;
            if (fontBold) {
                fontStyle |= FontStyle.Bold;
            }
            if (fontItalic) {
                fontStyle |= FontStyle.Italic;
            }
            if (fontUnderline) {
                fontStyle |= FontStyle.Underline;
            }
            if (fontStrikeout) {
                fontStyle |= FontStyle.Strikeout;
            }
            return new Font(fontFamily, fontSize, fontStyle);
        }
    }
}
