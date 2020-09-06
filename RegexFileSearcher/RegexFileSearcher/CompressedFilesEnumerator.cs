using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Linq;

namespace RegexFileSearcher
{
    // TODO: make it a real enumerator and add error handling
    public static class CompressedFilesEnumerator
    {
        public static IEnumerable<IEnumerable<FilePath>> GetCompressedFiles(IEnumerable<FilePath> filePaths)
        {
            foreach (FilePath filePath in filePaths)
            {
                if (IsZipFile(filePath))
                {
                    yield return GetCompressedFiles(filePath);
                }
            }
        }

        private static IEnumerable<FilePath> GetCompressedFiles(FilePath filePath)
        {
            List<FilePath> result = new List<FilePath>();
            try
            {
                using FileStream zipToOpen = new FileStream(filePath.Path, FileMode.Open);
                using ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Read);
                foreach (FilePath compressedFilePath in GetCompressedFilesInner(filePath, archive.Entries))
                {
                    result.Add(compressedFilePath);
                    //yield return compressedFilePath;
                }
            }
            catch
            {
            }

            foreach (var r in result)
                yield return r;
        }

        private static IEnumerable<FilePath> GetCompressedFilesInner(FilePath filePath,
            IEnumerable<ZipArchiveEntry> archiveEntries)
        {
            foreach (ZipArchiveEntry zip in archiveEntries)
            {
                FilePath compressedFilePath = new FilePath(zip.FullName);
                if (IsZipFile(compressedFilePath))
                {
                    foreach (FilePath compressedZipFile in GetCompressedFiles(compressedFilePath, zip.Open()).SelectMany(f => f))
                    {
                        yield return compressedZipFile;
                    }
                }
                else
                {
                    yield return new FilePath(filePath.Path, compressedFilePath);
                }
            }
        }

        private static IEnumerable<IEnumerable<FilePath>> GetCompressedFiles(FilePath compressedFilePath,
            Stream compressedZipFile)
        {
            using ZipArchive archive = new ZipArchive(compressedZipFile, ZipArchiveMode.Read);
            yield return GetCompressedFilesInner(compressedFilePath, archive.Entries);
        }

        private static bool IsZipFile(FilePath filePath)
        {
            string extension = Path.GetExtension(filePath.Path).ToLower();
            return extension == ".zip" || extension == ".gz";
        }
    }
}
