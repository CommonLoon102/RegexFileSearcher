using System;
using System.Runtime.Serialization;

namespace RegexFileSearcher
{
    [Serializable]
    public class FileHandlerException : Exception
    {
        public FileHandlerException() { }
        public FileHandlerException(string message) : base(message) { }
        public FileHandlerException(string message, Exception inner) : base(message, inner) { }
        protected FileHandlerException(SerializationInfo info,
            StreamingContext context) : base(info, context) { }
    }
}
