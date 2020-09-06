using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Linq;

namespace RegexFileSearcher
{
    public static class CompressedFileWalker
    {
        public static IEnumerable<FilePath> GetCompressedFiles(FilePath filePath)
        {
            List<FilePath> results = new List<FilePath>();
            if (!IsZipFile(filePath))
            {
                return results;
            }

            try
            {
                using FileStream zipToOpen = new FileStream(filePath.Path, FileMode.Open);
                using ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Read);
                results.AddRange(GetCompressedFilesInner(filePath, archive.Entries));
            }
            catch
            {
                // Any zip file related exception
            }

            return results;
        }

        private static IEnumerable<FilePath> GetCompressedFilesInner(FilePath filePath,
            IEnumerable<ZipArchiveEntry> archiveEntries)
        {
            foreach (ZipArchiveEntry archiveEntry in archiveEntries)
            {
                FilePath compressedFilePath = new FilePath(archiveEntry.FullName);
                if (IsZipFile(compressedFilePath))
                {
                    Stream entryStream = null;
                    try
                    {
                        entryStream = archiveEntry.Open();
                    }
                    catch
                    {
                        // ZipArchiveEntry.Open() related issues
                    }

                    if (entryStream != null)
                    {
                        foreach (FilePath compressedFile in GetCompressedFiles(compressedFilePath, entryStream))
                        {
                            yield return compressedFile;
                        }
                    }
                }
                else
                {
                    yield return new FilePath(filePath.Path, compressedFilePath);
                }
            }
        }

        private static IEnumerable<FilePath> GetCompressedFiles(FilePath compressedFilePath,
            Stream compressedZipFile)
        {
            List<FilePath> results = new List<FilePath>();
            try
            {
                using ZipArchive archive = new ZipArchive(compressedZipFile, ZipArchiveMode.Read);
                results.AddRange(GetCompressedFilesInner(compressedFilePath, archive.Entries));
            }
            catch
            {
                // new ZipArchive() related issues
            }

            return results;
        }

        private static bool IsZipFile(FilePath filePath)
        {
            string extension = Path.GetExtension(filePath.Path).ToLower();
            return extension == ".zip" || extension == ".gz";
        }
    }
}
