using System;
using System.Collections;
using System.Collections.Generic;
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
        /// Dictionary of mapped characters
        /// </summary>
        private static readonly Dictionary<char, char> CharMap = new Dictionary<char, char>
        {
            {'A', 'Å'},
            {'B', 'ß'},
            {'C', 'C'},
            {'D', 'Đ'},
            {'E', 'Ē'},
            {'F', 'F'},
            {'G', 'Ğ'},
            {'H', 'Ħ'},
            {'I', 'Ĩ'},
            {'J', 'Ĵ'},
            {'K', 'Ķ'},
            {'L', 'Ŀ'},
            {'M', 'M'},
            {'N', 'Ń'},
            {'O', 'Ø'},
            {'P', 'P'},
            {'Q', 'Q'},
            {'R', 'Ŗ'},
            {'S', 'Ŝ'},
            {'T', 'Ŧ'},
            {'U', 'Ů'},
            {'V', 'V'},
            {'W', 'Ŵ'},
            {'X', 'X'},
            {'Y', 'Ÿ'},
            {'Z', 'Ż'},
            {'a', 'ä'},
            {'b', 'þ'},
            {'c', 'č'},
            {'d', 'đ'},
            {'e', 'ę'},
            {'f', 'ƒ'},
            {'g', 'ģ'},
            {'h', 'ĥ'},
            {'i', 'į'},
            {'j', 'ĵ'},
            {'k', 'ĸ'},
            {'l', 'ľ'},
            {'m', 'm'},
            {'n', 'ŉ'},
            {'o', 'ő'},
            {'p', 'p'},
            {'q', 'q'},
            {'r', 'ř'},
            {'s', 'ş'},
            {'t', 'ŧ'},
            {'u', 'ū'},
            {'v', 'v'},
            {'w', 'ŵ'},
            {'x', 'χ'},
            {'y', 'y'},
            {'z', 'ž'}
        };

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
                if (dic.Value != null)
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
                        if (0 == string.CompareOrdinal(keyString, "$this.Text"))
                            textResourcesList.Add(dic.Key, dic.Value);
                    }

            // It's entirely possible that there are no text strings in the
            // .ResX file.
            if (textResourcesList.Count > 0)
            {
                if (fileSaveName == null) return;

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
                pseudoLen = origLen * 5;
            else
                pseudoLen = (int)(origLen * 1.3);

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
                sb.Append(CharMap.ContainsKey(currChar) ? CharMap[currChar] : currChar);
            }

            // Poke on extra text to fill out the string.
            const string padStr = " !!!";
            var padCount = (pseudoLen - origLen - 2) / padStr.Length;
            if (padCount < 2)
                padCount = 2;

            for (var x = 0; x < padCount; x++)
                sb.Append(padStr);

            // Pop on the trailing "]"
            sb.Append("]");

            return sb.ToString();
        }
    }
}