using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RegexFileSearcher
{
    internal class ZipFileWalker
    {
        private readonly int _maxFileSize;

        public ZipFileWalker(int maxFileSize)
        {
            _maxFileSize = maxFileSize;
        }

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

                        if (entryStream != null)
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
                        results.Add(new FilePath(zipEntryName, parentFilePath));
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
                if (zipEntry.IsFile && (_maxFileSize == 0 || zipEntry.Size <= _maxFileSize))
                {
                    yield return zipEntry;
                }
            }
        }
    }
}
