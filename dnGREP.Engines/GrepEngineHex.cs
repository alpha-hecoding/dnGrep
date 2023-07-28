﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using dnGREP.Common;

namespace dnGREP.Engines
{
    public class GrepEngineHex : GrepEngineBase, IGrepEngine
    {
        public IList<string> DefaultFileExtensions => Array.Empty<string>();

        public bool IsSearchOnly => true;

        public Version? FrameworkVersion => Assembly.GetAssembly(typeof(IGrepEngine))?.GetName()?.Version;

        public bool Replace(string sourceFile, string destinationFile, string searchPattern,
            string replacePattern, SearchType searchType, GrepSearchOption searchOptions,
            Encoding encoding, IEnumerable<GrepMatch> replaceItems, CancellationToken cancellationToken)
        {
            // should not get here, replace is not allowed from a Hex search
            throw new NotImplementedException();
        }

        public List<GrepSearchResult> Search(string file, string searchPattern, SearchType searchType,
            GrepSearchOption searchOptions, Encoding encoding, CancellationToken cancellationToken)
        {
            using FileStream fileStream = new(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, FileOptions.SequentialScan);
            return Search(fileStream, file, searchPattern, searchType, searchOptions, encoding, cancellationToken);
        }

        public List<GrepSearchResult> Search(Stream input, string fileName, string searchPattern, SearchType searchType,
            GrepSearchOption searchOptions, Encoding encoding, CancellationToken cancellationToken)
        {
            List<GrepSearchResult> searchResults = new();

            byte?[] searchArray = ToByteArray(searchPattern);

            const int bufferSize = 4096;
            byte[] buffer1, buffer2;

            long length = input.Length;
            List<GrepMatch> matches = new();
            using (BinaryReader readStream = new(input))
            {
                int startIndex = 0;
                buffer1 = readStream.ReadBytes(bufferSize);

                while (readStream.BaseStream.Position < length)
                {
                    buffer2 = readStream.ReadBytes(bufferSize);

                    matches.AddRange(DoByteArraySearch(buffer1, buffer2, searchArray, startIndex, searchPattern, cancellationToken));

                    startIndex += buffer1.Length;
                    buffer1 = buffer2;
                }
                matches.AddRange(DoByteArraySearch(buffer1, null, searchArray, startIndex, searchPattern, cancellationToken));

            }

            if (matches.Count > 0)
            {
                searchResults.Add(new GrepSearchResult(fileName, searchPattern, matches, encoding) { IsHexFile = true });
            }

            return searchResults;
        }

        private static byte?[] ToByteArray(string searchPattern)
        {
            // the expected search pattern is a space separated list of byte values in hexadecimal: 20 68 74
            List<byte?> list = new();
            string[] parts = searchPattern.TrimEnd().Split(' ');
            foreach (string num in parts)
            {
                if (num == "?" || num == "??")
                {
                    list.Add(null);
                }
                else if (byte.TryParse(num, NumberStyles.HexNumber, null, out byte result))
                {
                    list.Add(result);
                }
            }
            return list.ToArray();
        }

        private static List<GrepMatch> DoByteArraySearch(byte[] buffer1, byte[]? buffer2,
            byte?[] searchArray, int index, string searchPattern, CancellationToken cancellationToken)
        {
            List<GrepMatch> globalMatches = new();
            foreach (var match in ByteArraySearchIterator(buffer1, buffer2, searchArray, index, searchPattern, cancellationToken))
            {
                globalMatches.Add(match);

                cancellationToken.ThrowIfCancellationRequested();
            }

            return globalMatches;
        }

        private static IEnumerable<GrepMatch> ByteArraySearchIterator(byte[] buffer1, byte[]? buffer2,
            byte?[] searchArray, int startIndex, string searchPattern, CancellationToken cancellationToken)
        {
            int combinedLength = buffer1.Length + (buffer2 == null ? 0 : buffer2.Length);

            for (int idx = 0; idx < buffer1.Length; idx++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (buffer1[idx] == searchArray[0] || !searchArray[0].HasValue)
                {
                    bool hasMatch = true;
                    bool compareComplete = searchArray.Length == 1;
                    for (int jdx = 1; jdx < searchArray.Length && idx + jdx < combinedLength && hasMatch; jdx++)
                    {
                        compareComplete = jdx == searchArray.Length - 1;
                        if (!searchArray[jdx].HasValue)
                        {
                            continue;
                        }
                        hasMatch = GetByte(buffer1, buffer2, idx + jdx) == searchArray[jdx];
                    }

                    if (hasMatch && compareComplete)
                    {
                        yield return new GrepMatch(searchPattern, 0, startIndex + idx, searchArray.Length);

                        // move to the end of this match to begin the next search (no overlapping matches)
                        idx += searchArray.Length - 1;
                    }
                }
            }
        }

        private static byte GetByte(byte[] buffer1, byte[]? buffer2, int index)
        {
            if (index < buffer1.Length)
                return buffer1[index];

            index -= buffer1.Length;
            if (index < buffer2?.Length)
                return buffer2[index];

            // error
            return 0;
        }

        public void Unload()
        {
        }
    }
}
