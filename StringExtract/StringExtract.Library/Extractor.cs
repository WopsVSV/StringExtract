﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace StringExtract.Library
{
    /// <summary>
    /// The extractor class, which contains the core logic of the program
    /// </summary>
    public class Extractor
    {
        // Private members
        private int minimumCharacters;
        public int MinimumCharacters
        {
            get => minimumCharacters;
            set
            {
                if (value < 3) value = 3;
                minimumCharacters = value;
            }
        }

        // List of the extracted strings
        public List<string> Strings;

        /// <summary>
        /// The constructor for the extractor
        /// </summary>
        public Extractor()
        {
            Strings = new List<string>();
        }
        public Extractor(int minimumCharacters)
        {
            MinimumCharacters = minimumCharacters;
            Strings = new List<string>();
        }

        /// <summary>
        /// The main method that extracts the strings
        /// </summary>
        public IEnumerable<string> Extract(string filePath)
        {
            if (File.Exists(filePath))
            {
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    return ParseStream(stream);
                }
            }
            else
            {
                throw new IOException($"File does not exist: {filePath}");
            }
        }

        /// <summary>
        /// Parses the input stream
        /// </summary>
        private IEnumerable<string> ParseStream(FileStream stream)
        {
            const int MAX_BUFFER_SIZE = 32768;

            byte[] buffer = new byte[MAX_BUFFER_SIZE];
            int bufferSize;

            do
            {
                bufferSize = stream.Read(buffer, 0, MAX_BUFFER_SIZE);
                if (bufferSize > 0)
                {
                    return ProcessBuffer(buffer, bufferSize);
                }

            } while (bufferSize == MAX_BUFFER_SIZE);

            return new List<string>();
        }

        /// <summary>
        /// Processes a buffer
        /// </summary>
        private IEnumerable<string> ProcessBuffer(byte[] buffer, int bufferSize)
        {
            int offset = 0;

            int stringSize = 0;

            while (offset + minimumCharacters < bufferSize)
            {
                var outputString = string.Empty;
                int stringDiskSpace = ProcessString(buffer, bufferSize, offset, ref stringSize, ref outputString);

                if (stringSize >= minimumCharacters)
                {
                    yield return outputString;
                    offset += stringDiskSpace;
                }
                else offset++;
            }
        }

        /// <summary>
        /// Attemps to identify a string at a specific offset
        /// </summary>
        /// <returns>Returns the string disk space allocated to the string.</returns>
        private int ProcessString(byte[] buffer, int bufferSize, int offset, ref int stringSize, ref string outputString)
        {
            int i = 0;
            StringBuilder builder = new StringBuilder();

            // Try to parse as ascii or unicode
            try
            {
                if (Globals.isAscii[buffer[offset]])
                {
                    // Consider unicode case
                    if (buffer[offset + 1] == 0x00) // No null dereference by assumptions
                    {
                        // Parse as unicode
                        while (offset + i + 1 < bufferSize && i / 2 < Globals.MaxStringSize &&
                               Globals.isAscii[buffer[offset + i]] &&
                               buffer[offset + i + 1] == 0 && i / 2 + 1 < Globals.MaxStringSize)
                        {
                            builder.Append((char) buffer[offset + i]);
                            i += 2;
                        }

                        outputString = builder.ToString();
                        stringSize = i / 2;
                        return i;
                    }
                    else
                    {
                        // Parse as ASCII
                        i = offset;
                        // As longs as the next byte is a valid ASCII char
                        while (i < bufferSize && Globals.isAscii[buffer[i]])
                            i++;
                        stringSize = i - offset;
                        if (stringSize > Globals.MaxStringSize)
                            stringSize = Globals.MaxStringSize;

                        // Copy this string to the output
                        outputString = Encoding.ASCII.GetString(buffer, offset, stringSize);
                        return stringSize;
                    }
                }
            }
            catch {
                stringSize = 0;
                return 0;
            }
            // Nothing found ...
            stringSize = 0;
            return 0;
        }

    }
}
