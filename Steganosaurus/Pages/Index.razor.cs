namespace Steganosaurus.Pages;

using Microsoft.AspNetCore.Components.Forms;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

public sealed partial class Index
{

  private static async Task<string> GetImageString(IBrowserFile file)
  {
    var buffers = new byte[file.Size];
    await file.OpenReadStream().ReadAsync(buffers);
    return $"data:{file.ContentType};base64,{Convert.ToBase64String(buffers)}";
  }

  private static string GetDefaultImageString(int width = 64, int height = 64)
  {
    var img = new Image<Rgba32>(Configuration.Default, width, height);
    using var ms = new MemoryStream();
    img.SaveAsPng(ms);
    var bytes = ms.ToArray();
    return $"data:img/png;base64,{Convert.ToBase64String(bytes)}";
  }
}
