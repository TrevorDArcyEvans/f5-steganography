namespace F5.Core.James;

using System;
using Util;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

internal sealed class JpegInfo
{
  // the following are set as the default
  public const byte Precision = 8;
  public const byte NumberOfComponents = 3;
  public const byte Ss = 0;
  public const byte Se = 63;
  public const byte Ah = 0;
  public const byte Al = 0;
  public readonly int[] ACtableNumber = { 0, 1, 1 };
  public readonly int[] DCtableNumber = { 0, 1, 1 };

  // public int[] HsampFactor = {1, 1, 1};
  // public int[] VsampFactor = {1, 1, 1};
  public readonly int[] HsampFactor = { 2, 1, 1 };
  public readonly int[] QtableNumber = { 0, 1, 1 };
  public readonly int[] VsampFactor = { 2, 1, 1 };
  public readonly int[] BlockHeight;
  public readonly int[] BlockWidth;
  private readonly Image<Rgba32> _img;
  public readonly string Comment;

  internal readonly float[][][] Components;
  private readonly int[] _compWidth;
  private readonly int[] _compHeight;
  public readonly int ImageHeight;
  public readonly int ImageWidth;

  public JpegInfo(Image<Rgba32> image, string comment)
  {
    Components = new float[NumberOfComponents][][];
    _compWidth = new int[NumberOfComponents];
    _compHeight = new int[NumberOfComponents];
    BlockWidth = new int[NumberOfComponents];
    BlockHeight = new int[NumberOfComponents];
    _img = image;
    ImageWidth = image.Width;
    ImageHeight = image.Height;
    Comment = comment ?? "JPEG Encoder Copyright 1998, James R. Weeks and BioElectroMech.  ";
    InitYCC();
  }

  public JpegInfo(Image<Rgba32> image)
    : this(image, string.Empty)
  {
  }

  /// <summary>
  ///   This method creates and fills three arrays, Y, Cb, and Cr using the input image.
  /// </summary>
  private void InitYCC()
  {
    int MaxHsampFactor, MaxVsampFactor;
    MaxHsampFactor = MaxVsampFactor = 1;
    for (var i = 0; i < NumberOfComponents; i++)
    {
      MaxHsampFactor = Math.Max(MaxHsampFactor, HsampFactor[i]);
      MaxVsampFactor = Math.Max(MaxVsampFactor, VsampFactor[i]);
    }

    for (var i = 0; i < NumberOfComponents; i++)
    {
      _compWidth[i] = ImageWidth % 8 != 0 ? (int)Math.Ceiling(ImageWidth / 8.0) * 8 : ImageWidth;
      _compWidth[i] = _compWidth[i] / MaxHsampFactor * HsampFactor[i];
      BlockWidth[i] = (int)Math.Ceiling(_compWidth[i] / 8.0);

      _compHeight[i] = ImageHeight % 8 != 0 ? (int)Math.Ceiling(ImageHeight / 8.0) * 8 : ImageHeight;
      _compHeight[i] = _compHeight[i] / MaxVsampFactor * VsampFactor[i];
      BlockHeight[i] = (int)Math.Ceiling(_compHeight[i] / 8.0);
    }

    var Y = ArrayHelper.CreateJagged<float>(_compHeight[0], _compWidth[0]);
    var Cr1 = ArrayHelper.CreateJagged<float>(_compHeight[0], _compWidth[0]);
    var Cb1 = ArrayHelper.CreateJagged<float>(_compHeight[0], _compWidth[0]);
    var Cb2 = ArrayHelper.CreateJagged<float>(_compHeight[1], _compWidth[1]);
    var Cr2 = ArrayHelper.CreateJagged<float>(_compHeight[2], _compWidth[2]);


    var pixelData = new Rgba32[_img.Width * _img.Height];

    _img.ProcessPixelRows(acc =>
    {
      for (var y = 0; y < acc.Height; y++)
      {
        var pxRow = acc.GetRowSpan(y);
        for (var x = 0; x < pxRow.Length - 1; x++)
        {
          ref var px = ref pxRow[x];
          pixelData[y * _img.Width + x] = px;
        }
      }
    });


    // In order to minimize the chance that grabPixels will throw an
    // exception it may be necessary to grab some pixels every few scanlines and
    // process those before going for more. The time expense may be prohibitive.
    // However, for a situation where memory overhead is a concern, this may
    // be the only choice.
    for (var y = 0; y < _img.Height; y++)
    {
      for (var x = 0; x < _img.Width; x++)
      {
        var px = pixelData[y * _img.Width + x];
        var b = px.B;
        var g = px.G;
        var r = px.R;

        Y[y][x] = (float)(0.299 * r + 0.587 * g + 0.114 * b);
        Cb1[y][x] = 128 + (float)(-0.16874 * r - 0.33126 * g + 0.5 * b);
        Cr1[y][x] = 128 + (float)(0.5 * r - 0.41869 * g - 0.08131 * b);
      }
    }

    // Need a way to set the H and V sample factors before allowing
    // downsampling.
    // For now (04/04/98) downsampling must be hard coded.
    // Until a better downsampler is implemented, this will not be done.
    // Downsampling is currently supported. The downsampling method here
    // is a simple box filter.
    Cb2 = DownSample(Cb1, 1);
    Cr2 = DownSample(Cr1, 2);

    Components[0] = Y;
    Components[1] = Cb2;
    Components[2] = Cr2;
  }

  private float[][] DownSample(float[][] C, int comp)
  {
    int inrow = 0, incol = 0;
    var output = ArrayHelper.CreateJagged<float>(_compHeight[comp], _compWidth[comp]);
    for (var outrow = 0; outrow < _compHeight[comp]; outrow++)
    {
      var bias = 1;
      for (var outcol = 0; outcol < _compWidth[comp]; outcol++)
      {
        var temp = C[inrow][incol++];
        temp += C[inrow++][incol--]; // 01
        temp += C[inrow][incol++]; // 10
        temp += C[inrow--][incol++] + bias; // 11 -> 02
        output[outrow][outcol] = temp / (float)4.0;
        bias ^= 3;
      }

      inrow += 2;
      incol = 0;
    }

    return output;
  }
}
