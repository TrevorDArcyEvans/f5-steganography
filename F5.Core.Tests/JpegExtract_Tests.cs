namespace F5.Core.Tests;

using System.IO;
using NUnit.Framework;

public sealed class JpegExtract_Tests
{
  [Test]
  public void Extract()
  {
    using var extractor = new JpegExtract(File.OpenWrite("borneo-extract.txt"), "abc123");
    extractor.Extract(File.OpenRead("borneo-embed.jpg"));
  }
}
