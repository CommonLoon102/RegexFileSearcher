﻿using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.IO;

namespace RegexFileSearcher
{
    public static class CompressedFileWalker
    {
        public static IEnumerable<FilePath> GetCompressedFiles(FilePath filePath)
        {
            List<FilePath> results = new List<FilePath>();
            if (!IsZipFile(filePath.Path))
            {
                return results;
            }

            try
            {
                using var zipStream = File.OpenRead(filePath.Path);
                results.AddRange(GetCompressedFilesInner(filePath, zipStream));
            }
            catch (ZipException) { }
            catch (PathTooLongException) { }
            catch (DirectoryNotFoundException) { }
            catch (UnauthorizedAccessException) { }
            catch (FileNotFoundException) { }
            catch (IOException) { }

            return results;
        }

        private static IEnumerable<FilePath> GetCompressedFilesInner(FilePath parentFilePath, Stream zipStream)
        {
            using var zipFile = new ZipFile(zipStream, leaveOpen: false);
            foreach (ZipEntry zipEntry in GetZipEntries(zipFile))
            {
                string zipEntryName = zipEntry.Name;
                if (IsZipFile(zipEntryName))
                {
                    Stream entryStream = null;
                    try
                    {
                        entryStream = zipFile.GetInputStream(zipEntry);
                    }
                    catch
                    {
                        // zipFile.GetInputStream() related exceptions
                    }

                    if (entryStream != null)
                    {
                        var filePath = new FilePath(zipEntryName, parentFilePath);
                        foreach (FilePath compressedFile in GetCompressedFilesInner(filePath, entryStream))
                        {
                            yield return compressedFile;
                        }
                    }
                }
                else
                {
                    yield return new FilePath(zipEntryName, parentFilePath);
                }
            }
        }

        private static IEnumerable<ZipEntry> GetZipEntries(ZipFile zipFile)
        {
            foreach (ZipEntry zipEntry in zipFile)
            {
                if (zipEntry.IsFile)
                {
                    yield return zipEntry;
                }
            }
        }

        private static bool IsZipFile(string fileName)
        {
            string extension = Path.GetExtension(fileName).ToLower();
            return extension == ".zip";
        }
    }
}
