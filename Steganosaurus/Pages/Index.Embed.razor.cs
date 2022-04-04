namespace Steganosaurus.Pages;

using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

public sealed partial class Index
{
  [Inject]
  private IJSRuntime JSRuntime { get; set; }

  private string MessageEmbed { get; set; } = "I Am Groot";
  private string PasswordEmbed { get; set; } = "abc123";
  private string FileNameEmbed { get; set; }

  private async Task LoadFileEmbed(InputFileChangeEventArgs e)
  {
    // TODO   LoadFileEmbed
    FileNameEmbed = $"{Path.GetFileNameWithoutExtension(e.File.Name)}-embed{Path.GetExtension(e.File.Name)}";
    await using var data = e.File.OpenReadStream();
    await using var ms = new MemoryStream();
    await data.CopyToAsync(ms);
    ms.Seek(0, SeekOrigin.Begin);
  }

  private async Task DownloadEmbed()
  {
    // TODO   DownloadEmbed
    var data = System.Text.Encoding.UTF8.GetBytes("TODO");
    await JSRuntime.InvokeVoidAsync("BlazorDownloadFile", FileNameEmbed, "image/jpeg", data);
  }
}
