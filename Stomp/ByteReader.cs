using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace BusterWood.Stomp
{
    public class ByteReader
    {
        readonly Stream input;
        byte[] buf;
        int bufLen;

        public ByteReader(Stream input, int initialCapacity = 128)
        {
            this.input = input;
            buf = new byte[initialCapacity];
        }

        public async Task<string> ReadLineAsync()
        {
            int searchFrom = 0;
            for (;;)
            {
                // look for new line in buffer
                for (int i = searchFrom; i < bufLen; i++)
                {
                    if (buf[i] == '\n')
                    {
                        var toCopy = (i > 0 && buf[i - 1] == '\r') ? i - 1 : i; // trim '\r' before '\n'
                        var line = Encoding.UTF8.GetString(buf, 0, toCopy);
                        var remain = i + 1;
                        if (bufLen > remain)
                            Buffer.BlockCopy(buf, remain, buf, 0, bufLen - remain);
                        bufLen -= remain;
                        return line;
                    }
                }

                searchFrom = bufLen;
                // new line not found in buffer
                // resize buffer it not much space left to read
                if (bufLen > buf.Length / 2)
                    Array.Resize(ref buf, buf.Length * 2);
                // read more
                var n = await input.ReadAsync(buf, bufLen, buf.Length - bufLen);
                if (n == 0)
                    throw new FrameException("Unexpected end of file while reading a line");
                bufLen += n;
            }
        }

        public async Task<byte[]> ReadToNullAsync(int mustReadBytes)
        {
            var retVal = new byte[mustReadBytes];
            int bytesRead = 0;
            if (bufLen > 0)
            {
                bytesRead = mustReadBytes > bufLen ? bufLen : mustReadBytes;
                Buffer.BlockCopy(buf, 0, retVal, 0, bytesRead);
                bufLen -= bytesRead;
            }

            while (bytesRead != mustReadBytes)
            {
                var justRead = await input.ReadAsync(retVal, bytesRead, mustReadBytes - bytesRead);
                if (justRead == 0)
                    throw new FrameException("Unexpected end of file while reading body");
                bytesRead += justRead;
            }
            var gotNull = await input.ReadAsync(buf, 0, 1);
            if (gotNull == 0)
                throw new FrameException("Unexpected end of file while reading body null terminator");
            if (buf[0] != '\0')
                throw new FrameException($"Expected null terminator after body but was '{buf[0]}'");
            return retVal;
        }

        public async Task<byte[]> ReadToNullAsync()
        {
            int searchFrom = 0;
            for (;;)
            {
                // look for null char
                for (int i = searchFrom; i < bufLen; i++)
                {
                    if (buf[i] == '\0')
                    {
                        var toCopy = i;
                        var retVal = new byte[toCopy];
                        Buffer.BlockCopy(buf, 0, retVal, 0, toCopy);
                        var remain = i + 1;
                        if (bufLen > remain)
                            Buffer.BlockCopy(buf, remain, buf, 0, bufLen - remain);
                        bufLen -= remain;
                        return retVal;
                    }
                }

                searchFrom = bufLen;
                // new line not found in buffer
                // resize buffer it not much space left to read
                if (bufLen > buf.Length / 2)
                    Array.Resize(ref buf, buf.Length * 2);
                // read more
                var n = await input.ReadAsync(buf, bufLen, buf.Length - bufLen);
                if (n == 0)
                    throw new FrameException("Unexpected end of file while reading a line");
                bufLen += n;
            }
        }
    }
}