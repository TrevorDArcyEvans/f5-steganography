namespace Steganosaurus.Pages;

using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Drawing;
using F5.Core.James;

public sealed partial class Index
{
  [Inject]
  private IJSRuntime JSRuntime { get; set; }

  private string MessageEmbed { get; set; } = "I Am Groot";
  private string PasswordEmbed { get; set; } = "abc123";
  private string FileNameEmbed { get; set; }
  private byte[] _data { get; set; }
  private bool IsFinishedEmbed { get; set; }

  private async Task LoadFileEmbed(InputFileChangeEventArgs e)
  {
    IsFinishedEmbed = false;
    StateHasChanged();

    FileNameEmbed = $"{Path.GetFileNameWithoutExtension(e.File.Name)}-embed{Path.GetExtension(e.File.Name)}";
    await using var imageStrm = e.File.OpenReadStream();
    await using var imageData = new MemoryStream();
    await imageStrm.CopyToAsync(imageData);
    imageData.Seek(0, SeekOrigin.Begin);
    using var image = Image.FromStream(imageData);
    await using var output = new MemoryStream();
    using var jpg = new JpegEncoder(image, output, null);
    await using var ms = new MemoryStream();
    await using var strm = new StreamWriter(ms);
    await strm.WriteAsync(MessageEmbed);
    await strm.FlushAsync();
    ms.Position = 0;

    jpg.Compress(ms, PasswordEmbed);
    ms.Position = 0;
    _data = ms.ToArray();

    IsFinishedEmbed = true;
    StateHasChanged();
  }

  private async Task DownloadEmbed()
  {
    await JSRuntime.InvokeVoidAsync("BlazorDownloadFile", FileNameEmbed, "image/jpeg", _data);
  }
}
