namespace Steganosaurus.Pages;

using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Forms;
using F5.Core;

public sealed partial class Index
{
  private string MessageExtract { get; set; } = string.Empty;
  private string PasswordExtract { get; set; } = "abc123";
  private bool IsFinishedExtract { get; set; }
  private string SourceFileNameExtract { get; set; } = "Upload image";
  private string ImgExtractUrl { get; set; } = GetDefaultImageString();

  private async Task LoadFileExtract(InputFileChangeEventArgs e)
  {
    IsFinishedExtract = false;
    SourceFileNameExtract = e.File.Name;
    ImgExtractUrl = await GetImageString(e.File);
    StateHasChanged();

    await using var imageStrm = e.File.OpenReadStream();
    await using var imageData = new MemoryStream();
    await imageStrm.CopyToAsync(imageData);
    imageData.Seek(0, SeekOrigin.Begin);

    await using var ms = new MemoryStream();
    using var extractor = new JpegExtract(ms, PasswordExtract);
    extractor.Extract(imageData);
    ms.Position = 0;
    var sr = new StreamReader(ms);
    MessageExtract = await sr.ReadToEndAsync();

    IsFinishedExtract = true;
    StateHasChanged();
  }
}
