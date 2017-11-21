using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusterWood.Stomp
{
    public class FrameReader
    {
        readonly StringBuilder sb = new StringBuilder();
        readonly ByteReader input;
        
        public FrameReader(Stream input)
        {
            this.input = new ByteReader(input);
        }

        public async Task<Frame> Parse()
        {
            var f = new Frame();

            // read command: accept empty blank lines as keep-alive
            do
            {
                f.Command = await input.ReadLineAsync();
                if (f.Command == null)
                    return null; // no command, stream is now closed
            } while (f.Command.Length == 0);

            // read headers:
            for (;;)
            {
                var line = await input.ReadLineAsync();
                if (line == null)
                    throw new FrameException("Unexpected end of frame while reading headers");
                if (line.Length == 0) // empty line ends the headers
                    break;
                var pair = ParseHeader(line);
                if (!f.Headers.ContainsKey(pair.Key)) // only add the first value, other duplicates ignored, as per spec 1.2
                    f.Headers.Add(pair.Key, pair.Value);
            }

            // read body:
            // if header 'content-length' set then parse it and read exactly that number of bytes
            //TODO: limit the message size to something reasonable? config setting, 10MB?
            int? cl = f.ContentLength();
            if (cl != null)
                f.Body = await input.ReadToNullAsync(cl.Value);
            else
                f.Body = await input.ReadToNullAsync();

            return f;
        }

        // internal for testing
        internal KeyValuePair<string, string> ParseHeader(string line)
        {
            var bits = line.Split(':');
            if (bits.Length != 2)
                throw new FrameException($"Header must contain a single colon '{line}'");
            var key = RemoveEscapeChars(bits[0]);
            var value = RemoveEscapeChars(bits[1]);
            return new KeyValuePair<string, string>(key, value);
        }

        string RemoveEscapeChars(string header)
        {
            sb.Clear();
            for (int i = 0; i < header.Length; i++)
            {
                if (header[i] == '\\')
                {
                    i++;
                    if (i == header.Length)
                        throw new FrameException($"Invalid escape sequence in header '{header}'");
                    switch (header[i])
                    {
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 'c': sb.Append(':'); break;
                        case '\\': sb.Append('\\'); break;
                        default: throw new FrameException($"Invalid escape sequence \\{header[i]} in header '{header}'");
                    }
                }
                else
                    sb.Append(header[i]);
            }
            return sb.ToString();
        }
    }

    public class Frame
    {
        public string Command { get; set; }
        public Dictionary<string, string> Headers { get; } = new Dictionary<string, string>();
        public byte[] Body { get; set; }

        internal int? ContentLength()
        {
            string value;
            int len;
            return Headers.TryGetValue("content-length", out value) && int.TryParse(value, out len) ? len : (int?)null;
        }
    }

    public class Message
    {
        public Command Command { get; set; }
    }

    public enum Command
    {
        CONNECT,
    }
}
