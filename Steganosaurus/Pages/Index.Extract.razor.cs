namespace Steganosaurus.Pages;

using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Forms;
using F5.Core;

public sealed partial class Index
{
  private string MessageExtract { get; set; } = "I Am Groot";
  private string PasswordExtract { get; set; } = "abc123";

  private async Task LoadFileExtract(InputFileChangeEventArgs e)
  {
    await using var image = e.File.OpenReadStream();
    await using var imageData = new MemoryStream();
    await image.CopyToAsync(imageData);
    imageData.Seek(0, SeekOrigin.Begin);

    await using var ms = new MemoryStream();
    using var extractor = new JpegExtract(ms, PasswordExtract);
    extractor.Extract(imageData);
    ms.Position = 0;
    var sr = new StreamReader(ms);
    MessageExtract = await sr.ReadToEndAsync();
  }
}
