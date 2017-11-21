using NUnit.Framework;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace BusterWood.Stomp.UnitTests
{
    [TestFixture]
    public class FrameReaderTests
    {
        [TestCase("a:b", "a", "b")]
        [TestCase("content-length:123", "content-length", "123")]
        [TestCase("a: ", "a", " ")]
        [TestCase("a:1\\c3", "a", "1:3")]
        [TestCase("a:1\\n3", "a", "1\n3")]
        [TestCase("a:1\\r3", "a", "1\r3")]
        [TestCase("a:1\\\\3", "a", "1\\3")]
        public void can_parse_header(string input, string key, string value)
        {
            var parser = new FrameReader(new MemoryStream());
            var result = parser.ParseHeader(input);
            Assert.AreEqual(key, result.Key, "key");
            Assert.AreEqual(value, result.Value, "value");
        }

        [TestCase("")]
        [TestCase("abc")]
        [TestCase("a:b:c")]
        [TestCase("a:\\")]
        [TestCase("a:\\a")]
        [TestCase("a:\\b")]
        [TestCase("a:\\d")]
        [TestCase("a:\\0")]
        public void fail_to_parse_invalid_header(string input)
        {
            var parser = new FrameReader(new MemoryStream());
            Assert.Throws<FrameException>(() => parser.ParseHeader(input));
        }

        [Test]
        public async Task can_parse_connect()
        {
            string input = @"CONNECT
hello:world

" + '\0';
            var parser = new FrameReader(new MemoryStream(Encoding.UTF8.GetBytes(input)));
            var f = await parser.Parse();
            Assert.IsNotNull(f, "frame");
            Assert.AreEqual("CONNECT", f.Command);
            Assert.AreEqual("world", f.Headers["hello"]);
            Assert.AreEqual(0, f.Body.Length, "Body.Length");
        }
    }
}
