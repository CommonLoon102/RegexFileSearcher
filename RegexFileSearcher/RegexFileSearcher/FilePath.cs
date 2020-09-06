using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace RegexFileSearcher
{
    public class FilePath : IConvertible
    {
        public FilePath Parent { get; private set; }

        public string Path { get; private set; }

        public FilePath(string path)
        {
            Path = path;
        }

        public FilePath(string path, FilePath zipFile)
            : this(path)
        {
            Parent = zipFile;
        }

        public string GetInmostFilePath()
        {
            FilePath filePath = this;
            while (filePath.Parent != null)
            {
                filePath = filePath.Parent;
            }

            return filePath.Path;
        }

        public string GetFileContent()
        {
            if (Parent == null)
            {
                return File.OpenText(Path).ReadToEnd();
            }

            using ZipArchive archive = ZipFile.Open(Path, ZipArchiveMode.Read);
            return GetFileContent(archive, Parent);
        }

        private string GetFileContent(ZipArchive archive, FilePath compressedFile)
        {
            using Stream stream = archive.GetEntry(compressedFile.Path).Open();
            if (compressedFile.Parent == null)
            {
                TextReader tr = new StreamReader(stream);
                return tr.ReadToEnd();
            }
            else
            {
                using ZipArchive subArchive = new ZipArchive(stream, ZipArchiveMode.Read);
                {
                    return GetFileContent(subArchive, compressedFile.Parent);
                }
            }
        }

        private static string GetFullPath(FilePath filePath)
        {
            if (filePath.Parent == null)
            {
                return filePath.Path;
            }
            else
            {
                return System.IO.Path.Combine(filePath.Path, GetFullPath(filePath.Parent));
            }
        }

        #region IConvertible members
        public TypeCode GetTypeCode()
        {
            return TypeCode.String;
        }

        public bool ToBoolean(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public byte ToByte(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public char ToChar(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public DateTime ToDateTime(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public decimal ToDecimal(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public double ToDouble(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public short ToInt16(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public int ToInt32(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public long ToInt64(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public sbyte ToSByte(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public float ToSingle(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public string ToString(IFormatProvider provider)
        {
            return GetFullPath(this);
        }

        public object ToType(Type conversionType, IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public ushort ToUInt16(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public uint ToUInt32(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public ulong ToUInt64(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }
        #endregion // IConvertible members
    }
}
