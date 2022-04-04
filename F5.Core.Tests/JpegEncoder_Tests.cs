namespace F5.Core.Tests;

using NUnit.Framework;
using System.Drawing;
using System.IO;
using James;

public sealed class JpegEncoder_Tests
{
  [Test]
  public void Embed()
  {
    using var image = Image.FromFile("borneo.jpg");
    using var dummy = new MemoryStream();
    using var jpg = new JpegEncoder(image, dummy, null);
    using var ms = new MemoryStream();
    using var strm = new StreamWriter(ms);
    strm.Write("I Am Groot");
    strm.Flush();
    ms.Position = 0;

    jpg.Compress(ms, "abc123");
  }
}