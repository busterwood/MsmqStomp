using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MsmqStomp
{
    public class FrameWriter
    {
        static readonly byte[] nullByte = { 0 };
        readonly CharBuffer buf = new CharBuffer();
        readonly Stream output;

        public FrameWriter(Stream output)
        {
            this.output = output;
        }

        public async Task Write(Frame f)
        {
            buf.Clear();
            buf.Append(f.Command).Append(Environment.NewLine);
            foreach (var header in f.Headers)
            {
                buf.AppendHeader(header.Key).Append(':').AppendHeader(header.Value).Append(Environment.NewLine);
            }
            buf.Append(Environment.NewLine);

            await output.WriteUtf8Async(buf);
            await output.WriteAsync(f.Body, 0, f.Body.Length);
            await output.WriteAsync(nullByte, 0, nullByte.Length);
        }

    }

    /// <summary>
    /// A bit like a <see cref="StringBuilder"/> but specific to our needs as it avoid extra string and char array creation 
    /// when encoding to UTF8.
    /// </summary>
    class CharBuffer
    {
        public char[] Buffer;
        public int Length;

        public CharBuffer()
        {
            Buffer = new char[64];
            Length = 0;
        }

        public CharBuffer Append(char c)
        {
            if (Length + 1 >= Buffer.Length)
                Array.Resize(ref Buffer, Length * 2);
            Buffer[Length] = c;
            Length++;
            return this;
        }

        public CharBuffer Append(string s)
        {
            if (Length + s.Length >= Buffer.Length)
                Array.Resize(ref Buffer, (Length + s.Length) * 2);
            s.CopyTo(0, Buffer, Length, s.Length);
            Length += s.Length;
            return this;
        }

        /// <summary>Escapes header specific chars</summary>
        public CharBuffer AppendHeader(string header)
        {
            if (Length + header.Length >= Buffer.Length)
                Array.Resize(ref Buffer, (Length + header.Length) * 2);

            for (int i = 0; i < header.Length; i++)
            {
                switch (header[i])
                {
                    case '\n': Append("\\n"); break;
                    case '\r': Append("\\r"); break;
                    case ':': Append("\\c"); break;
                    case '\\': Append("\\\\"); break;
                    default: Append(header[i]); break;
                }
            }
            return this;
        }

        public CharBuffer Clear()
        {
            Length = 0;
            return this;
        }
    }

    static class StreamExtensions
    {
        static readonly UTF8Encoding utf8 = new UTF8Encoding(false);

        public static Task WriteUtf8Async(this Stream output, string text)
        {
            var bytes = utf8.GetBytes(text);
            return output.WriteAsync(bytes, 0, bytes.Length);
        }

        public static Task WriteUtf8Async(this Stream output, CharBuffer buf)
        {
            var bytes = utf8.GetBytes(buf.Buffer, 0, buf.Length);
            return output.WriteAsync(bytes, 0, bytes.Length);
        }
    }
}
