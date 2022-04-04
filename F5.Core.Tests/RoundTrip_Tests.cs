namespace F5.Core.Tests;

using System.Drawing;
using System.IO;
using F5.Core.James;
using FluentAssertions;
using NUnit.Framework;

public sealed class RoundTrip_Tests
{
  [Test]
  public void RoundTrip()
  {
    using var image = Image.FromFile("borneo.jpg");
    using var output = new MemoryStream();
    using var jpg = new JpegEncoder(image, output, null);
    using var msEmbed = new MemoryStream();
    using var strm = new StreamWriter(msEmbed);
    strm.Write("I Am Groot");
    strm.Flush();
    msEmbed.Position = 0;
    output.Position = 0;

    jpg.Compress(msEmbed, "abc123");


    using var msExtract = new MemoryStream();
    using var extractor = new JpegExtract(msExtract, "abc123");

    extractor.Extract(output);
    msExtract.Position = 0;
    var sr = new StreamReader(msExtract);
    var data = sr.ReadToEnd();

    data.Should().Be("I Am Groot");
  }
}
