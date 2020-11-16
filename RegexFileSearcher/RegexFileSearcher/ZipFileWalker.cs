using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.IO;

namespace RegexFileSearcher
{
    internal class ZipFileWalker
    {
        public int MaxFileSize { get; init; }

        public IEnumerable<FilePath> GetZippedFiles(FilePath filePath)
        {
            var results = new List<FilePath>();
            if (!filePath.Path.IsZipFile())
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

        private IEnumerable<FilePath> GetCompressedFilesInner(FilePath parentFilePath, Stream zipStream)
        {
            var results = new List<FilePath>();
            try
            {
                using var zipFile = new ZipFile(zipStream, leaveOpen: false);
                foreach (ZipEntry zipEntry in GetZipEntries(zipFile))
                {
                    string zipEntryName = zipEntry.Name;
                    if (zipEntryName.IsZipFile())
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

                        if (entryStream is not null)
                        {
                            var filePath = new FilePath(zipEntryName, parentFilePath);
                            foreach (FilePath compressedFile in GetCompressedFilesInner(filePath, entryStream))
                            {
                                results.Add(compressedFile);
                            }
                        }
                    }
                    else
                    {
                        results.Add(new(zipEntryName, parentFilePath));
                    }
                }
            }
            catch (ZipException) { }
            catch (IOException) { }

            return results;
        }

        private IEnumerable<ZipEntry> GetZipEntries(ZipFile zipFile)
        {
            foreach (ZipEntry zipEntry in zipFile)
            {
                if (zipEntry.IsFile && (MaxFileSize == 0 || zipEntry.Size <= MaxFileSize))
                {
                    yield return zipEntry;
                }
            }
        }
    }
}
