using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text;

namespace Pseudo.Globalization
{
    ///Takes an English resource file (resx) and creates an artificial");/
    ///but still readable Euro-like language to exercise your i18n code");
    ///without a formal translation.");
    internal class Psuedoizer
    {
        private static readonly CultureInfo[] AllCultures = CultureInfo.GetCultures(CultureTypes.SpecificCultures);

        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main(string[] args)
        {
            Console.WriteLine("Psuedoizer: Adapted from MSDN BugSlayer 2004-Apr i18n Article.");
            if (args.Length < 2)
            {
                Console.WriteLine("Purpose: Takes an English resource file (resx) and creates an artificial");
                Console.WriteLine("         but still readable Euro-like language to exercise your i18n code");
                Console.WriteLine("         without a formal translation.");
                Console.WriteLine();
                Console.WriteLine("Psuedoizer.exe infile outfile [/b]");
                Console.WriteLine("    Example:");
                Console.WriteLine("    Psuedoizer.exe strings.en.resx strings.ja-JP.resx");
                Console.WriteLine("    /b - Include blank resources");
                Console.WriteLine();
                Console.WriteLine("Alternative: use a directory and a language code");
                Console.WriteLine("Psuedoizer.exe dir lang [/b]");
                Console.WriteLine("    Example:");
                Console.WriteLine("    Psuedoizer.exe . ja-JP");
                Console.WriteLine("    /b - Include blank resources");
                Environment.Exit(1);
            }

            var fileNameOrDirectory = args[0];
            var fileSaveNameOrLangCode = args[1];
            var includeBlankResources = (args.Length >= 3) && (args[2] == "/b");

            try
            {
                if (Directory.Exists(fileNameOrDirectory))
                    TranslateMultipleFiles(fileNameOrDirectory, fileSaveNameOrLangCode, includeBlankResources);
                else
                    TranslateSingleFile(fileNameOrDirectory, fileSaveNameOrLangCode, includeBlankResources);
            }
            catch (Exception e)
            {
                Console.Write(e.ToString());
                Environment.Exit(1);
            }
        }

        private static void TranslateMultipleFiles(string directory, string langCode, bool includeBlankResources)
        {
            foreach (var file in Directory.GetFiles(directory, "*.resx"))
            {
                // Check if it's the neutral resource file
                var fileName = Path.GetFileNameWithoutExtension(file);
                var extension = Path.GetExtension(fileName)?.Trim(' ', '.').ToLower();
                if (string.IsNullOrEmpty(extension) ||
                    !AllCultures.Any(
                        c => (c.Name.ToLower() == extension) || (c.TwoLetterISOLanguageName.ToLower() == extension)))
                    TranslateSingleFile(file, $"{directory}\\{fileName}.{langCode}.resx",
                        includeBlankResources);
            }

            foreach (var subDir in Directory.GetDirectories(directory))
                TranslateMultipleFiles(subDir, langCode, includeBlankResources);
        }

        private static void TranslateSingleFile(string fileName, string fileSaveName, bool includeBlankResources)
        {
            // Open the input file.
            var reader = new ResXResourceReader(fileName);
            try
            {
                // Get the enumerator.  If this throws an ArguementException
                // it means the file is not a .RESX file.
                reader.GetEnumerator();
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine("WARNING: could not parse " + fileName);
                Console.WriteLine("         " + ex.Message);
                return;
            }

            // Allocate the list for this instance.
            var textResourcesList = new SortedList();

            // Run through the file looking for only true text related
            // properties and only those with values set.
            foreach (DictionaryEntry dic in reader)
                if (null != dic.Value)
                    if ("System.String" == dic.Value.GetType().ToString())
                    {
                        var keyString = dic.Key.ToString();

                        // Make sure the key name does not start with the
                        // "$" or ">>" meta characters and is not an empty
                        // string (or we're explicitly including empty strings).
                        if ((false == keyString.StartsWith(">>")) &&
                            (false == keyString.StartsWith("$")) &&
                            (includeBlankResources || ("" != dic.Value.ToString())))
                            textResourcesList.Add(dic.Key, dic.Value);

                        // Special case the Windows Form "$this.Text" or
                        // I don't get the form titles.
                        if (0 == String.CompareOrdinal(keyString, "$this.Text"))
                            textResourcesList.Add(dic.Key, dic.Value);
                    }

            // It's entirely possible that there are no text strings in the
            // .ResX file.
            if (textResourcesList.Count > 0)
            {
                if (null != fileSaveName)
                {
                    if (File.Exists(fileSaveName))
                        File.Delete(fileSaveName);

                    // Create the new file.
                    var writer =
                        new ResXResourceWriter(fileSaveName);

                    foreach (DictionaryEntry textdic in textResourcesList)
                        writer.AddResource(textdic.Key.ToString(),
                            ConvertToFakeInternationalized(textdic.Value.ToString()));

                    writer.Generate();
                    writer.Close();
                    Console.WriteLine("{0}: converted {1} text resource(s).", fileName, textResourcesList.Count);
                }
            }
            else
            {
                Console.WriteLine("WARNING: No text resources found in " + fileName);
            }
        }

        /// <summary>
        ///     Converts a string to a pseudo internationized string.
        /// </summary>
        /// <remarks>
        ///     Primarily for latin based languages.  This will need updating to
        ///     work with Eastern languages.
        /// </remarks>
        /// <param name="inputString">
        ///     The string to use as a base.
        /// </param>
        /// <returns>
        ///     A longer and twiddled string.
        /// </returns>
        public static string ConvertToFakeInternationalized(string inputString)
        {
            //check if the input string is a http or https link... if it is, do not localize
            if (inputString.Contains("http://") || inputString.Contains("https://"))
                return inputString;


            // Calculate the extra space necessary for pseudo
            // internationalization.  The rules, according to "Developing
            // International Software" is that < 10  characters you should grow
            // by 400% while >= 10 characters should grow by 30%.

            var origLen = inputString.Length;
            int pseudoLen;
            if (origLen < 10)
                pseudoLen = origLen * 4 + origLen;
            else
                pseudoLen = (int)(origLen * 0.3) + origLen;

            var sb = new StringBuilder(pseudoLen);

            // The pseudo string will always start with a "[" and end
            // with a "]" so you can tell if strings are not built
            // correctly in the UI.
            sb.Append("[");

            var waitingForEndBrace = false;
            var waitingForGreaterThan = false;
            foreach (var currChar in inputString)
            {
                switch (currChar)
                {
                    case '{':
                        waitingForEndBrace = true;
                        break;
                    case '}':
                        waitingForEndBrace = false;
                        break;
                    case '<':
                        waitingForGreaterThan = true;
                        break;
                    case '>':
                        waitingForGreaterThan = false;
                        break;
                }
                if (waitingForEndBrace || waitingForGreaterThan)
                {
                    sb.Append(currChar);
                    continue;
                }
                switch (currChar)
                {
                    case 'A':
                        sb.Append('Å');
                        break;
                    case 'B':
                        sb.Append('ß');
                        break;
                    case 'C':
                        sb.Append('C');
                        break;
                    case 'D':
                        sb.Append('Đ');
                        break;
                    case 'E':
                        sb.Append('Ē');
                        break;
                    case 'F':
                        sb.Append('F');
                        break;
                    case 'G':
                        sb.Append('Ğ');
                        break;
                    case 'H':
                        sb.Append('Ħ');
                        break;
                    case 'I':
                        sb.Append('Ĩ');
                        break;
                    case 'J':
                        sb.Append('Ĵ');
                        break;
                    case 'K':
                        sb.Append('Ķ');
                        break;
                    case 'L':
                        sb.Append('Ŀ');
                        break;
                    case 'M':
                        sb.Append('M');
                        break;
                    case 'N':
                        sb.Append('Ń');
                        break;
                    case 'O':
                        sb.Append('Ø');
                        break;
                    case 'P':
                        sb.Append('P');
                        break;
                    case 'Q':
                        sb.Append('Q');
                        break;
                    case 'R':
                        sb.Append('Ŗ');
                        break;
                    case 'S':
                        sb.Append('Ŝ');
                        break;
                    case 'T':
                        sb.Append('Ŧ');
                        break;
                    case 'U':
                        sb.Append('Ů');
                        break;
                    case 'V':
                        sb.Append('V');
                        break;
                    case 'W':
                        sb.Append('Ŵ');
                        break;
                    case 'X':
                        sb.Append('X');
                        break;
                    case 'Y':
                        sb.Append('Ÿ');
                        break;
                    case 'Z':
                        sb.Append('Ż');
                        break;


                    case 'a':
                        sb.Append('ä');
                        break;
                    case 'b':
                        sb.Append('þ');
                        break;
                    case 'c':
                        sb.Append('č');
                        break;
                    case 'd':
                        sb.Append('đ');
                        break;
                    case 'e':
                        sb.Append('ę');
                        break;
                    case 'f':
                        sb.Append('ƒ');
                        break;
                    case 'g':
                        sb.Append('ģ');
                        break;
                    case 'h':
                        sb.Append('ĥ');
                        break;
                    case 'i':
                        sb.Append('į');
                        break;
                    case 'j':
                        sb.Append('ĵ');
                        break;
                    case 'k':
                        sb.Append('ĸ');
                        break;
                    case 'l':
                        sb.Append('ľ');
                        break;
                    case 'm':
                        sb.Append('m');
                        break;
                    case 'n':
                        sb.Append('ŉ');
                        break;
                    case 'o':
                        sb.Append('ő');
                        break;
                    case 'p':
                        sb.Append('p');
                        break;
                    case 'q':
                        sb.Append('q');
                        break;
                    case 'r':
                        sb.Append('ř');
                        break;
                    case 's':
                        sb.Append('ş');
                        break;
                    case 't':
                        sb.Append('ŧ');
                        break;
                    case 'u':
                        sb.Append('ū');
                        break;
                    case 'v':
                        sb.Append('v');
                        break;
                    case 'w':
                        sb.Append('ŵ');
                        break;
                    case 'x':
                        sb.Append('χ');
                        break;
                    case 'y':
                        sb.Append('y');
                        break;
                    case 'z':
                        sb.Append('ž');
                        break;
                    default:
                        sb.Append(currChar);
                        break;
                }
            }

            // Poke on extra text to fill out the string.
            const string PadStr = " !!!";
            var PadCount = (pseudoLen - origLen - 2) / PadStr.Length;
            if (PadCount < 2)
                PadCount = 2;

            for (var x = 0; x < PadCount; x++)
                sb.Append(PadStr);

            // Pop on the trailing "]"
            sb.Append("]");

            return sb.ToString();
        }
    }
}