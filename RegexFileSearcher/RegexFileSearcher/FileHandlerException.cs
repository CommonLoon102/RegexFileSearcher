using System;
using System.Collections.Generic;
using System.Text;

namespace RegexFileSearcher
{
    [Serializable]
    public class FileHandlerException : Exception
    {
        public FileHandlerException() { }
        public FileHandlerException(string message) : base(message) { }
        public FileHandlerException(string message, Exception inner) : base(message, inner) { }
        protected FileHandlerException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
