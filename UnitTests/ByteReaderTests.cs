using NUnit.Framework;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace BusterWood.Stomp.UnitTests
{
    [TestFixture]
    public class ByteReaderTests
    {
        [TestCase("CONNECT\n")]
        [TestCase("CONNECT\r\n")]
        [TestCase("\n")]
        [TestCase("\r\n")]
        public async Task can_read_line(string input)
        {
            ByteReader br = CreateReaderForInput(input);
            var actual = await br.ReadLineAsync();
            Assert.AreEqual(input.TrimEnd('\r', '\n'), actual);
        }

        [TestCase("CONNECT\nagain\n", "again")]
        [TestCase("CONNECT\r\nhello world\r\n", "hello world")]
        [TestCase("CONNECT\r\n\r\n", "")]
        public async Task can_read_second_line(string input, string expected)
        {
            ByteReader br = CreateReaderForInput(input);
            var ignored = await br.ReadLineAsync();
            var actual = await br.ReadLineAsync();
            Assert.AreEqual(expected, actual);
        }

        [TestCase("CONNECT\nagain\0", 5, "again")]
        public async Task read_fixed_number_of_bytes_after_newline(string input, int charsToRead, string expected)
        {
            ByteReader br = CreateReaderForInput(input);
            var ignored = await br.ReadLineAsync();
            var actual = await br.ReadToNullAsync(charsToRead);
            Assert.AreEqual(charsToRead, actual.Length, "number of bytes");
            Assert.AreEqual(expected, Encoding.UTF8.GetString(actual));
        }

        [TestCase("CONNECT\nagain", 5)]
        [TestCase("CONNECT\nagainX", 5)]
        public async Task ReadNullTerminatedAsync_throws_exception(string input, int charsToRead)
        {
            ByteReader br = CreateReaderForInput(input);
            var ignored = await br.ReadLineAsync();
            try
            {
                var actual = await br.ReadToNullAsync(charsToRead);
            }
            catch (FrameException ex)
            {
                Assert.Pass(ex.Message);
            }
            Assert.Fail();
        }

        static ByteReader CreateReaderForInput(string input)
        {
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms, new UTF8Encoding(false), 1024, true);
            writer.WriteLine(input);
            writer.Flush();
            ms.Seek(0, SeekOrigin.Begin);

            var br = new ByteReader(ms, 5);
            return br;
        }
    }
}
