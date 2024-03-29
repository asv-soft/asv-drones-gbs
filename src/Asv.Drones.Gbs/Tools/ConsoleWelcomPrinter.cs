﻿using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace Asv.Drones.Gbs;

/// <summary>
/// ConsoleWelcomePrinter provides methods for printing welcome messages to the console. </summary>
/// <summary/>
public static class ConsoleWelcomePrinter
    {
        /// <summary>
        /// Prints a welcome message to the console using the specified color and additional values.
        /// </summary>
        /// <param name="src">The source assembly.</param>
        /// <param name="color">The color of the welcome message. Default is ConsoleColor.Cyan.</param>
        /// <param name="additionalValues">Additional key-value pairs to include in the welcome message.</param>
        public static void PrintWelcomeToConsole(this Assembly src, ConsoleColor color = ConsoleColor.Cyan, params KeyValuePair<string, string>[] additionalValues)
        {
            var old = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(src.PrintWelcome(additionalValues));
            Console.ForegroundColor = old;
        }

        /// <summary>
        /// Prints a welcome message using the information from the specified <see cref="Assembly"/>.
        /// </summary>
        /// <param name="src">The <see cref="Assembly"/> to get the information from.</param>
        /// <param name="additionalValues">Additional key-value pairs to include in the welcome message.</param>
        /// <returns>A string containing the formatted welcome message.</returns>
        public static string PrintWelcome(this Assembly src, IEnumerable<KeyValuePair<string, string>> additionalValues = null)
        {
            var header = new[]
            {
                src.GetTitle(),
                src.GetDescription(),
                src.GetCopyrightHolder(),
            };
            var values = new List<KeyValuePair<string, string>>
            {
                new("Version",src.GetInformationalVersion() ),
#if DEBUG
                new("Build","Debug" ),
#else
                new KeyValuePair<string, string>("Build", "Release"),
#endif
                new("Process",Process.GetCurrentProcess().Id.ToString()),
                new("OS",Environment.OSVersion.ToString()),
                new("Machine",Environment.MachineName),
                new("Environment",Environment.Version.ToString()),
            };

            if (additionalValues != null) values.AddRange(additionalValues);

            return PrintWelcome(header, values);
        }


        /// <summary>
        /// Prints the welcome message with the given header and values.
        /// </summary>
        /// <param name="header">The collection of strings to be printed as the header of the welcome message.</param>
        /// <param name="values">The collection of key-value pairs to be printed as the values of the welcome message.</param>
        /// <param name="padding">The padding applied between the keys and values. (Default is 1)</param>
        /// <returns>The welcome message as a string.</returns>
        private static string PrintWelcome(IEnumerable<string> header, IEnumerable<KeyValuePair<string, string>> values,
            int padding = 1)
        {
            var keysWidth = values.Select(_ => _.Key.Length).Max();
            var valueWidth = values.Select(_ => _.Value.Length).Max();
            return PrintWelcome(header, values, keysWidth, valueWidth, padding);
        }

        /// <summary>
        /// Prints a formatted welcome message.
        /// </summary>
        /// <param name="header">The header lines for the welcome message.</param>
        /// <param name="values">The key-value pairs to be displayed in the welcome message.</param>
        /// <param name="keyWidth">The width of the key column.</param>
        /// <param name="valueWidth">The width of the value column.</param>
        /// <param name="padding">The padding size around each value.</param>
        /// <returns>The formatted welcome message as a string.</returns>
        public static string PrintWelcome(IEnumerable<string> header, IEnumerable<KeyValuePair<string, string>> values, int keyWidth, int valueWidth, int padding)
        {
            var sb = new StringBuilder();

            var headerWidth = keyWidth + valueWidth + padding * 4 + 1;

            sb.Append('╔').Append('═', headerWidth).Append('╗').Append(' ').AppendLine();
            foreach (var hdr in header)
            {
                sb.Append("║").Append(' ', padding).Append(hdr.PadLeft(headerWidth - padding * 2)).Append(' ', padding).Append("║▒").AppendLine();
            }
            sb.Append('╠').Append('═', padding * 2).Append('═', keyWidth).Append('╦').Append('═', valueWidth).Append('═', padding * 2).Append("╣▒").AppendLine();
            foreach (var pair in values)
            {
                sb.Append('║').Append(' ', padding).Append(pair.Key.PadLeft(keyWidth)).Append(' ', padding).Append('║').Append(' ', padding).Append(pair.Value.PadRight(valueWidth)).Append(' ', padding).Append("║▒").AppendLine();
            }

            sb.Append('╚').Append('═', padding * 2).Append('═', keyWidth).Append('╩').Append('═', valueWidth).Append('═', padding * 2).Append("╝▒").AppendLine();
            sb.Append(' ').Append('▒', headerWidth + 2);
            return sb.ToString();
        }


    }