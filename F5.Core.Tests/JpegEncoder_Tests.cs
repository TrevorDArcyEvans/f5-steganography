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
    using var jpg = new JpegEncoder(image, File.OpenWrite("borneo-embed.jpg"), null);

    // TODO   compress stream with pwd
    // "I Am Groot"
    jpg.Compress();
  }
}