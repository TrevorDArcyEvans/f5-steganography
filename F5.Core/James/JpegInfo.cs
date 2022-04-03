namespace F5.Core.James
{
  using System;
  using System.Drawing;
  using System.Drawing.Imaging;
  using System.Runtime.InteropServices;
  using F5.Core.Util;

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
    public int[] BlockHeight, BlockWidth;
    private readonly Bitmap bmp;
    public string Comment;

    internal float[][][] Components;
    private readonly int[] compWidth;
    private readonly int[] compHeight;
    public int ImageHeight, ImageWidth;

    public JpegInfo(Image image, string comment)
    {
      Components = new float[NumberOfComponents][][];
      compWidth = new int[NumberOfComponents];
      compHeight = new int[NumberOfComponents];
      BlockWidth = new int[NumberOfComponents];
      BlockHeight = new int[NumberOfComponents];
      bmp = (Bitmap)image;
      ImageWidth = image.Width;
      ImageHeight = image.Height;
      Comment = comment ?? "JPEG Encoder Copyright 1998, James R. Weeks and BioElectroMech.  ";
      InitYCC();
    }

    public JpegInfo(Image image)
      : this(image, string.Empty)
    {
    }

    /// <summary>
    ///   This method creates and fills three arrays, Y, Cb, and Cr using the input image.
    /// </summary>
    private void InitYCC()
    {
      int MaxHsampFactor, MaxVsampFactor;
      int i, x, y, width, height, stride;
      int size, pixelSize, offset, yPos;
      byte r, g, b;
      byte[] pixelData;

      MaxHsampFactor = MaxVsampFactor = 1;
      for (i = 0; i < NumberOfComponents; i++)
      {
        MaxHsampFactor = Math.Max(MaxHsampFactor, HsampFactor[i]);
        MaxVsampFactor = Math.Max(MaxVsampFactor, VsampFactor[i]);
      }

      for (i = 0; i < NumberOfComponents; i++)
      {
        compWidth[i] = ImageWidth % 8 != 0 ? (int)Math.Ceiling(ImageWidth / 8.0) * 8 : ImageWidth;
        compWidth[i] = compWidth[i] / MaxHsampFactor * HsampFactor[i];
        BlockWidth[i] = (int)Math.Ceiling(compWidth[i] / 8.0);

        compHeight[i] = ImageHeight % 8 != 0 ? (int)Math.Ceiling(ImageHeight / 8.0) * 8 : ImageHeight;
        compHeight[i] = compHeight[i] / MaxVsampFactor * VsampFactor[i];
        BlockHeight[i] = (int)Math.Ceiling(compHeight[i] / 8.0);
      }

      var Y = ArrayHelper.CreateJagged<float>(compHeight[0], compWidth[0]);
      var Cr1 = ArrayHelper.CreateJagged<float>(compHeight[0], compWidth[0]);
      var Cb1 = ArrayHelper.CreateJagged<float>(compHeight[0], compWidth[0]);
      var Cb2 = ArrayHelper.CreateJagged<float>(compHeight[1], compWidth[1]);
      var Cr2 = ArrayHelper.CreateJagged<float>(compHeight[2], compWidth[2]);

      using (bmp)
      {
        width = bmp.Width;
        height = bmp.Height;
        var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
        var bmpData = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
        stride = bmpData.Stride;
        size = stride * height;
        pixelSize = Image.GetPixelFormatSize(bmp.PixelFormat) / 8;
        pixelData = new byte[size];
        Marshal.Copy(bmpData.Scan0, pixelData, 0, size);
        bmp.UnlockBits(bmpData);
      }

      // In order to minimize the chance that grabPixels will throw an
      // exception it may be necessary to grab some pixels every few scanlines and
      // process those before going for more. The time expense may be prohibitive.
      // However, for a situation where memory overhead is a concern, this may
      // be the only choice.
      for (y = 0; y < height; y++)
      {
        yPos = stride * y;
        for (x = 0; x < width; x++)
        {
          offset = yPos + x * pixelSize;
          b = pixelData[offset];
          g = pixelData[offset + 1];
          r = pixelData[offset + 2];

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
      int inrow = 0, incol = 0, outrow, outcol, bias;
      var output = ArrayHelper.CreateJagged<float>(compHeight[comp], compWidth[comp]);
      float temp;
      for (outrow = 0; outrow < compHeight[comp]; outrow++)
      {
        bias = 1;
        for (outcol = 0; outcol < compWidth[comp]; outcol++)
        {
          temp = C[inrow][incol++]; // 00
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
}
