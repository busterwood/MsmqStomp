using System;
using System.Runtime.Serialization;

namespace MsmqStomp
{
    [Serializable]
    public class FrameException : Exception
    {
        public FrameException()
        {
        }

        public FrameException(string message) : base(message)
        {
        }

        public FrameException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected FrameException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}