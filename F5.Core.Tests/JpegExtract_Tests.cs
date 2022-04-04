namespace F5.Core.Tests;

using System.IO;
using NUnit.Framework;
using FluentAssertions;

public sealed class JpegExtract_Tests
{
  [Test]
  public void Extract()
  {
    using var ms = new MemoryStream();
    using var extractor = new JpegExtract(ms, "abc123");

    extractor.Extract(File.OpenRead("borneo-embed.jpg"));
    ms.Position = 0;
    var sr = new StreamReader(ms);
    var data = sr.ReadToEnd();

    data.Should().Be("I Am Groot");
  }
}
