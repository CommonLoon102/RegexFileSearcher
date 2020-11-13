using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.IO;

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

        public FilePath(string path, FilePath parent)
            : this(path)
        {
            Parent = parent;
        }

        public string GetFileContent()
        {
            if (Parent is null)
            {
                return File.OpenText(Path).ReadToEnd();
            }

            FilePath reversedFilePath = GetReversedFilePath(this);
            string rootZipPath = reversedFilePath.Path;
            using var zipStream = File.OpenRead(rootZipPath);
            using var archive = new ZipFile(zipStream, leaveOpen: false);
            return GetFileContent(archive, reversedFilePath.Parent);
        }

        private static FilePath GetReversedFilePath(FilePath filePath)
        {
            var pathList = new List<string>();
            while (filePath is not null)
            {
                pathList.Add(filePath.Path);
                filePath = filePath.Parent;
            }

            pathList.Reverse();
            var filePaths = new List<FilePath>(pathList.Count);
            for (int i = 0; i < pathList.Count; i++)
            {
                filePaths.Add(new(pathList[i]));
            }

            for (int i = 0; i < pathList.Count - 1; i++)
            {
                filePaths[i].Parent = filePaths[i + 1];
            }

            return filePaths[0];
        }

        private string GetFileContent(ZipFile zipFile, FilePath parent)
        {
            using Stream stream = zipFile.GetInputStream(zipFile.GetEntry(parent.Path));
            if (parent.Parent is null)
            {
                TextReader tr = new StreamReader(stream);
                return tr.ReadToEnd();
            }
            else
            {
                using var subZipFile = new ZipFile(stream, leaveOpen: false);
                {
                    return GetFileContent(subZipFile, parent.Parent);
                }
            }
        }

        private string GetFullPath(FilePath filePath)
            => filePath.Parent is null ?
                filePath.Path
                : System.IO.Path.Combine(GetFullPath(filePath.Parent), filePath.Path);

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
