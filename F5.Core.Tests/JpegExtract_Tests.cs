namespace F5.Core.Tests;

using System.IO;
using NUnit.Framework;

public sealed class JpegExtract_Tests
{
  [Test]
  public void Extract()
  {
    using var ms = new MemoryStream();
    using var extractor = new JpegExtract(ms, "abc123");
    extractor.Extract(File.OpenRead("borneo-embed.jpg"));
  }
}
