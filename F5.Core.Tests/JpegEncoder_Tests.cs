namespace F5.Core.Tests;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using NUnit.Framework;
using System.IO;
using James;

public sealed class JpegEncoder_Tests
{
  [Test]
  public void Embed()
  {
    using var image = Image.Load<Rgba32>("borneo.jpg");
    using var output = new MemoryStream();
    using var jpg = new JpegEncoder(image, output, null);
    using var ms = new MemoryStream();
    using var strm = new StreamWriter(ms);
    strm.Write("I Am Groot");
    strm.Flush();
    ms.Position = 0;

    jpg.Compress(ms, "abc123");
  }
}