﻿namespace F5.Ortega
{
  using System.IO;
  using F5.Util;

  internal sealed class HuffTable
  {
    // Declare input steam
    private readonly EmbedData dis;

    private readonly int[] Bits = new int[17];
    private readonly int[] EHUFCO = new int[257];
    private readonly int[] EHUFSI = new int[257];
    private readonly int[] HuffCode = new int[257];

    private readonly int[] HuffSize = new int[257];

    // Instance variables
    internal int[] HuffVal = new int[256];
    private int si, i, j, k, last_k, code;
    internal int[] MaxCode = new int[18];
    internal int[] MinCode = new int[17];
    internal int[] ValPtr = new int[17];

    // Constructor Methods
    internal HuffTable(Stream d, int l)
      : this(new EmbedData(d), l)
    {
    }

    internal HuffTable(EmbedData d, int l)
    {
      dis = d;
      // Get table data from input stream
      Len = 19 + GetTableData();
      SetSizeTable(); // Flow Chart C.1
      SetCodeTable(); // Flow Chart C.2
      SetOrderCodes(); // Flow Chart C.3
      SetDecoderTables(); // Generate decoder tables Flow Chart F.15
    }

    public int Len { get; }

    private int GetTableData()
    {
      // Get BITS list
      var count = 0;
      for (var x = 1; x < 17; x++)
      {
        Bits[x] = dis.Read();
        count += Bits[x];
      }

      // Read in HUFFVAL
      for (var x = 0; x < count; x++) HuffVal[x] = dis.Read();
      return count;
    }

    private void SetOrderCodes()
    {
      // Order Codes Flow Chart C.3
      k = 0;

      while (true)
      {
        i = HuffVal[k];
        EHUFCO[i] = HuffCode[k];
        EHUFSI[i] = HuffSize[k++];
        if (k >= last_k) break;
      }
    }

    private void SetDecoderTables()
    {
      // Decoder table generation Flow Chart F.15
      i = 0;
      j = 0;
      while (true)
      {
        if (++i > 16)
          return;

        if (Bits[i] == 0)
        {
          MaxCode[i] = -1;
        }
        else
        {
          ValPtr[i] = j;
          MinCode[i] = HuffCode[j];
          j = j + Bits[i] - 1;
          MaxCode[i] = HuffCode[j++];
        }
      }
    }

    private void SetCodeTable()
    {
      // Generate Code table Flow Chart C.2
      k = 0;
      code = 0;
      si = HuffSize[0];
      while (true)
      {
        HuffCode[k++] = code++;

        if (HuffSize[k] == si) continue;

        if (HuffSize[k] == 0) break;

        while (true)
        {
          code <<= 1;
          si++;
          if (HuffSize[k] == si) break;
        }
      }
    }

    private void SetSizeTable()
    {
      // Generate HUFFSIZE table Flow Chart C.1
      k = 0;
      i = 1;
      j = 1;
      while (true)
        if (j > Bits[i])
        {
          j = 1;
          i++;
          if (i > 16) break;
        }
        else
        {
          HuffSize[k++] = i;
          j++;
        }

      HuffSize[k] = 0;
      last_k = k;
    }
  }
}
