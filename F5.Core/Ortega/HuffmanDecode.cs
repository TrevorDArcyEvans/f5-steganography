﻿namespace F5.Core.Ortega;

using System;
using System.IO;
using Util;
using log4net;

internal sealed class HuffmanDecode : IDisposable
{
  private const int DRI = 0xDD;
  private const int DNL = 0xDC;
  private const int EOI = 0xD9;
  private static readonly ILog logger = LogManager.GetLogger(typeof(HuffmanDecode));

  private static readonly byte[] APP =
  {
    0xE0, 0xE1, 0xE2, 0xE3, 0xE4, 0xE5, 0xE6, 0xE7, 0xE8, 0xE9, 0xEA, 0xEB, 0xEC, 0xED, 0xEE, 0xEF
  };

  private static readonly byte[,] deZZ =
  {
    { 0, 0 }, { 0, 1 }, { 1, 0 }, { 2, 0 },
    { 1, 1 }, { 0, 2 }, { 0, 3 }, { 1, 2 },
    { 2, 1 }, { 3, 0 }, { 4, 0 }, { 3, 1 },
    { 2, 2 }, { 1, 3 }, { 0, 4 }, { 0, 5 },
    { 1, 4 }, { 2, 3 }, { 3, 2 }, { 4, 1 },
    { 5, 0 }, { 6, 0 }, { 5, 1 }, { 4, 2 },
    { 3, 3 }, { 2, 4 }, { 1, 5 }, { 0, 6 },
    { 0, 7 }, { 1, 6 }, { 2, 5 }, { 3, 4 },
    { 4, 3 }, { 5, 2 }, { 6, 1 }, { 7, 0 },
    { 7, 1 }, { 6, 2 }, { 5, 3 }, { 4, 4 },
    { 3, 5 }, { 2, 6 }, { 1, 7 }, { 2, 7 },
    { 3, 6 }, { 4, 5 }, { 5, 4 }, { 6, 3 },
    { 7, 2 }, { 7, 3 }, { 6, 4 }, { 5, 5 },
    { 4, 6 }, { 3, 7 }, { 4, 7 }, { 5, 6 },
    { 6, 5 }, { 7, 4 }, { 7, 5 }, { 6, 6 },
    { 5, 7 }, { 6, 7 }, { 7, 6 }, { 7, 7 }
  };

  // added for decode()
  internal static readonly byte[] deZigZag =
  {
    0, 1, 5, 6, 14, 15, 27, 28,
    2, 4, 7, 13, 16, 26, 29, 42,
    3, 8, 12, 17, 25, 30, 41, 43,
    9, 11, 18, 24, 31, 40, 44, 53,
    10, 19, 23, 32, 39, 45, 52, 54,
    20, 22, 33, 38, 46, 51, 55, 60,
    21, 34, 37, 47, 50, 56, 59, 61,
    35, 36, 48, 49, 57, 58, 62, 63
  };

  // other variables
  private int B, CNT, Diff, Pred;
  private int[] C, H, V, T; // SOF0 parameters
  private int[,,] Cr, Cb;

  private readonly EmbedData dis;
  private readonly int[][] HuffVal = new int[4][];
  private int hftbl;
  private int K, Ssss, RS, R, J;

  // Instance variables
  // Declare header variables
  private int Lf, P, X, Y, Nf; // SOF0 parameters
  private int Lh, Tc, Th; // DHT parameters
  private int Lq, Pq, Tq; // DQT parameters
  private int Ls, Ns, Ss, Se, Ah, Al; // SOS parameters
  private readonly int[][] MaxCode = new int[4][];
  private readonly int[][] MinCode = new int[4][];
  private readonly int[][] QNT = ArrayHelper.CreateJagged<int>(4, 64);
  private int RI;
  private int size;
  private readonly int[][] ValPtr = new int[4][];
  private readonly int[] ZZ = new int[64];

  // Constructor Method
  public HuffmanDecode(Stream data)
  {
    size = (int)data.Length;
    dis = new EmbedData(data);
    // Parse out markers and header info
    var cont = true;
    byte b;

    while (cont)
    {
      b = dis.Read();
      if (b == 255)
      {
        b = dis.Read();
        switch (b)
        {
          case 192:
            SetSOF0();
            break;
          case 196:
            SetDHT();
            break;
          case 219:
            SetDQT();
            break;
          case 217:
          case 218:
            cont = false;
            break;
          case DRI:
            SetDRI();
            break;
          default:
            if (Array.IndexOf(APP, b) > -1) dis.Seek(dis.ReadInt() - 2, SeekOrigin.Current);
            break;
        }
      }
    }
  }

  /// <summary>
  ///   Return image data
  /// </summary>
  public int[] Decode()
  {
    int tmp;
    int[] Cs, Ta, Td; // SOS parameters
    var PRED = new int[Nf];
    for (var nComponent = 0; nComponent < Nf; nComponent++) PRED[nComponent] = 0;
    CNT = 0;
    // Read in Scan Header information
    Ls = dis.ReadInt();
    Ns = dis.Read();

    Cs = new int[Ns];
    Td = new int[Ns];
    Ta = new int[Ns];

    // get table information
    for (var i = 0; i < Ns; i++)
    {
      Cs[i] = dis.Read();
      Td[i] = dis.Read();
      Ta[i] = Td[i] & 0x0f;
      Td[i] >>= 4;
    }

    Ss = dis.Read();
    Se = dis.Read();
    Ah = dis.Read();
    Al = Ah & 0x0f;
    Ah >>= 4;

    // Calculate the Number of blocks encoded
    // warum doppelt so viel?
    var buff = new int[2 * 8 * 8 * GetBlockCount()];
    var pos = 0;
    var MCUCount = 0;

    var bDoIt = true;
    while (bDoIt)
    {
      // Get component 1 of MCU
      for (var nComponent = 0; nComponent < Nf; nComponent++)
      {
        for (var j = 0; j < H[nComponent] * V[nComponent]; j++)
        {
          // Get DC coefficient
          hftbl = Td[nComponent] * 2;
          tmp = DECODE();
          Diff = Receive(tmp);
          ZZ[0] = PRED[0] + Extend(Diff, tmp);
          PRED[nComponent] = ZZ[0];

          // Get AC coefficients
          hftbl = Ta[nComponent] * 2 + 1;
          Decode_AC_Coefficients();

          for (var i = 0; i < 64; i++)
          {
            // Zickzack???
            // buff[pos++]=ZZ[deZigZag[lp]];
            buff[pos++] = ZZ[i];
          }
        }
      }

      MCUCount++;
      if (MCUCount == RI)
      {
        MCUCount = 0;
        CNT = 0;
        for (var nComponent = 0; nComponent < Nf; nComponent++) PRED[nComponent] = 0;
        dis.Read();
        int tmpB = dis.Read();
        if (tmpB == EOI)
        {
          break;
        }
      }

      if (dis.Available <= 2)
      {
        if (dis.Available == 2)
        {
          dis.Read();
          if (dis.Read() != EOI)
          {
            logger.Warn("file does not end with EOI");
          }
        }
        else
        {
          logger.Warn("file does not end with EOI");
        }

        break;
      }
    }

    Array.Resize(ref buff, pos);
    return buff;
  }

  private int DECODE()
  {
    var cd = NextBit();
    var i = 1;
    while (true)
      if (cd > MaxCode[hftbl][i])
      {
        cd = (cd << 1) + NextBit();
        i++;
      }
      else
      {
        break;
      }

    J = ValPtr[hftbl][i];
    J = J + cd - MinCode[hftbl][i];
    var value = HuffVal[hftbl][J];
    return value;
  }

  private void Decode_AC_Coefficients()
  {
    K = 1;

    // Zero out array ZZ[]
    for (var i = 0; i < 64; i++)
    {
      ZZ[i] = 0;
    }

    while (true)
    {
      // System.out.println(hftbl);
      RS = DECODE();
      Ssss = RS % 16;
      R = RS >> 4;
      if (Ssss == 0)
      {
        if (R == 15)
        {
          K += 16;
        }
        else
        {
          return;
        }
      }
      else
      {
        K = K + R;
        Decode_ZZ(K);
        if (K == 63)
        {
          return;
        }

        K++;
      }
    }
  }

  private void Decode_ZZ(int k)
  {
    // Decoding a nonzero AC coefficient
    ZZ[k] = Receive(Ssss);
    ZZ[k] = Extend(ZZ[k], Ssss);
  }

  private void FillDHT(int index)
  {
    var ht = new HuffTable(dis);
    Lh -= ht.Len;
    HuffVal[index] = ht.HuffVal;
    ValPtr[index] = ht.ValPtr;
    MaxCode[index] = ht.MaxCode;
    MinCode[index] = ht.MinCode;
  }

  private void SetDHT()
  {
    // Read in Huffman tables
    // Lh length
    // Th index
    // Tc AC?
    Lh = dis.ReadInt();
    while (Lh > 0)
    {
      Tc = dis.Read();
      Th = Tc & 0x0f;
      Tc >>= 4;
      if (Th == 0)
      {
        FillDHT(Tc == 0 ? 0 : 1);
      }
      else
      {
        FillDHT(Tc == 0 ? 2 : 3);
      }
    }
  }

  private void SetDQT()
  {
    // Read in quatization tables
    Lq = dis.ReadInt();
    Pq = dis.Read();
    Tq = Pq & 0x0f;
    Pq >>= 4;

    for (var i = 0; i < 64; i++)
    {
      QNT[Tq][i] = dis.Read();
    }
  }

  private void SetDRI()
  {
    dis.ReadInt();
    RI = dis.ReadInt();
  }

  private static int Extend(int v, int t)
  {
    var Vt = 0x01 << (t - 1);
    if (v < Vt)
    {
      Vt = (-1 << t) + 1;
      v += Vt;
    }

    return v;
  }

  // Calculate the Number of blocks encoded
  public int GetBlockCount()
  {
    switch (Nf)
    {
      case 1:
        return (X + 7) / 8 * ((Y + 7) / 8);
      case 3:
        return 6 * ((X + 15) / 16) * ((Y + 15) / 16);
      default:
        logger.Warn("Nf weder 1 noch 3");
        return 0;
    }
  }

  public int GetComp()
  {
    return Nf;
  }

  public int GetPrec()
  {
    return P;
  }

  // Public get methods
  public int GetX()
  {
    return X;
  }

  public int GetY()
  {
    return Y;
  }

  /// <summary>
  ///   Return image data
  /// </summary>
  public void HuffDecode(int[,,] buffer)
  {
    int x, y, tmp;
    var sz = X * Y;
    var Block = new int[8, 8];
    int Cs, Ta, Td, blocks;

    // Read in Scan Header information
    Ls = dis.ReadInt();
    Ns = dis.Read();
    Cs = dis.Read();
    Td = dis.Read();
    Ta = Td & 0x0f;
    Td >>= 4;

    Ss = dis.Read();
    Se = dis.Read();
    Ah = dis.Read();
    Al = Ah & 0x0f;
    Ah >>= 4;

    // Calculate the Number of blocks encoded
    // blocks = X * Y / 64;
    blocks = GetBlockCount() / 6;

    // decode image data and return image data in array
    for (var j = 0; j < blocks; j++)
    {
      // Get DC coefficient
      if (Td == 0)
      {
        hftbl = 0;
      }
      else
      {
        hftbl = 2;
      }

      tmp = DECODE();
      Diff = Receive(tmp);
      ZZ[0] = Pred + Extend(Diff, tmp);
      Pred = ZZ[0];

      // Get AC coefficients
      if (Ta == 0)
      {
        hftbl = 1;
      }
      else
      {
        hftbl = 3;
      }

      Decode_AC_Coefficients();

      // dezigzag and dequantize block
      for (var i = 0; i < 64; i++)
      {
        Block[deZZ[i, 0], deZZ[i, 1]] = ZZ[i] * QNT[0][i];
      }

      // store blocks in buffer
      for (x = 0; x < 8; x++)
      {
        for (y = 0; y < 8; y++)
        {
          buffer[j, x, y] = Block[x, y];
        }
      }
    }

    dis.Close();
  }

  /// <summary>
  ///   Get one bit from entropy coded data stream
  /// </summary>
  private int NextBit()
  {
    if (CNT == 0)
    {
      CNT = 8;
      B = dis.Read();
      if (255 == B)
      {
        int b2 = dis.Read();
      }
    }

    var bit = B & 0X80;
    bit >>= 7; // move MSB to LSB
    CNT--; // Decrement counter
    B <<= 1; // Shift left one bit
    return bit;
  }

  /// <summary>
  ///   Return quantized coefficients
  /// </summary>
  public void RawDecode(int[,,] buffer)
  {
    int x, y, tmp;
    var block = new int[8, 8];
    int Cs, Ta, Td, blocks;

    // Read in Scan Header information
    Ls = dis.ReadInt();
    Ns = dis.Read();
    Cs = dis.Read();
    Td = dis.Read();
    Ta = Td & 0x0f;
    Td >>= 4;

    Ss = dis.Read();
    Se = dis.Read();
    Ah = dis.Read();
    Al = Ah & 0x0f;
    Ah >>= 4;

    // Calculate the Number of blocks encoded
    blocks = GetBlockCount() / 6;

    // decode image data and return image data in array
    for (var j = 0; j < blocks; j++)
    {
      // Get DC coefficient
      if (Td == 0)
      {
        hftbl = 0;
      }
      else
      {
        hftbl = 2;
      }

      tmp = DECODE();
      Diff = Receive(tmp);
      ZZ[0] = Pred + Extend(Diff, tmp);
      Pred = ZZ[0];

      // Get AC coefficients
      if (Ta == 0)
      {
        hftbl = 1;
      }
      else
      {
        hftbl = 3;
      }

      Decode_AC_Coefficients();

      // dezigzag
      for (var i = 0; i < 64; i++)
      {
        block[deZZ[i, 0], deZZ[i, 1]] = ZZ[i];
      }

      // store blocks in buffer
      logger.Info(j + " ");
      for (x = 0; x < 8; x++)
      {
        for (y = 0; y < 8; y++)
        {
          buffer[j, x, y] = block[x, y];
        }
      }
    }

    dis.Close();
  }

  private int Receive(int sss)
  {
    int v = 0, i = 0;
    while (true)
    {
      if (i == sss)
      {
        return v;
      }

      i++;
      v = (v << 1) + NextBit();
    }
  }

  // Return image data for RGB images
  public void RGB_Decode(int[,,] lum)
  {
    int x, y, a, b;
    var sz = X * Y;
    var block = new int[8, 8];
    int[] Cs, Ta, Td;
    int[] PRED = { 0, 0, 0 };

    // Read in Scan Header information
    Ls = dis.ReadInt();
    Ns = dis.Read();
    Cs = new int[Ns];
    Td = new int[Ns];
    Ta = new int[Ns];

    // get table information
    for (var i = 0; i < Ns; i++)
    {
      Cs[i] = dis.Read();
      Td[i] = dis.Read();
      Ta[i] = Td[i] & 0x0f;
      Td[i] >>= 4;
    }

    Ss = dis.Read();
    Se = dis.Read();
    Ah = dis.Read();
    Al = Ah & 0x0f;
    Ah >>= 4;

    // Calculate the Number of blocks encoded
    // blocks = X * Y / 64;
    var blocks = GetBlockCount() / 6;

    // decode image data and return image data in array
    for (a = 0; a < 32; a++)
    for (b = 0; b < 32; b++)
    {
      // Get component 1 of MCU
      int tmp;
      for (var j = 0; j < 4; j++)
      {
        // Get DC coefficient
        hftbl = 0;
        tmp = DECODE();
        Diff = Receive(tmp);
        ZZ[0] = PRED[0] + Extend(Diff, tmp);
        PRED[0] = ZZ[0];

        // Get AC coefficients
        hftbl = 1;
        Decode_AC_Coefficients();

        // dezigzag and dequantize block
        for (var i = 0; i < 64; i++)
        {
          block[deZZ[i, 0], deZZ[i, 1]] = ZZ[i] * QNT[0][i];
        }

        var line = j < 2 ? 0 : 62;

        // store blocks in buffer
        for (x = 0; x < 8; x++)
        {
          for (y = 0; y < 8; y++)
          {
            lum[b * 2 + j + line + a * 128, x, y] = block[x, y];
          }
        }
      }

      // getComponent 2 and 3 of image
      for (var j = 0; j < 2; j++)
      {
        // Get DC coefficient
        hftbl = 2;
        tmp = DECODE();
        Diff = Receive(tmp);
        ZZ[0] = PRED[j + 1] + Extend(Diff, tmp);
        PRED[j + 1] = ZZ[0];

        // Get AC coefficients
        hftbl = 3;
        Decode_AC_Coefficients();

        // dezigzag and dequantize block
        for (var i = 0; i < 64; i++)
        {
          block[deZZ[i, 0], deZZ[i, 1]] = ZZ[i] * QNT[1][i];
        }

        // store blocks in buffer
        if (j == 0)
        {
          for (x = 0; x < 8; x++)
          {
            for (y = 0; y < 8; y++)
            {
              Cb[a * 32 + b, x, y] = block[x, y];
            }
          }
        }
        else
        {
          for (x = 0; x < 8; x++)
          {
            for (y = 0; y < 8; y++)
            {
              Cr[a * 32 + b, x, y] = block[x, y];
            }
          }
        }
      }
    }

    dis.Close();
  }

  public void SetCb(int[,,] chrome)
  {
    Cb = chrome;
  }

  public void SetCr(int[,,] chrome)
  {
    Cr = chrome;
  }

  private void SetSOF0()
  {
    // Read in start of frame header data
    Lf = dis.ReadInt();
    P = dis.Read();
    Y = dis.ReadInt();
    X = dis.ReadInt();
    Nf = dis.Read();

    C = new int[Nf];
    H = new int[Nf];
    V = new int[Nf];
    T = new int[Nf];

    // Read in quatization table identifiers
    for (var i = 0; i < Nf; i++)
    {
      C[i] = dis.Read();
      H[i] = dis.Read();
      V[i] = H[i] & 0x0f;
      H[i] >>= 4;
      T[i] = dis.Read();
    }
  }

  #region IDisposable

  private bool _disposed;

  public void Dispose()
  {
    Dispose(true);
    GC.SuppressFinalize(this);
  }

  ~HuffmanDecode()
  {
    Dispose(false);
  }

  private void Dispose(bool disposing)
  {
    if (_disposed)
    {
      return;
    }

    if (disposing)
    {
      dis.Dispose();
    }

    _disposed = true;
  }

  #endregion
}
