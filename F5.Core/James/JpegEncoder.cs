namespace F5.Core.James
{
  using System;
  using System.Collections.Generic;
  using System.Drawing;
  using System.IO;
  using System.Text;
  using F5.Core.Crypt;
  using F5.Core.Util;
  using log4net;


  public sealed class JpegEncoder : IDisposable
  {
    private static readonly ILog logger = LogManager.GetLogger(typeof(JpegEncoder));
    private readonly int imageHeight, imageWidth;
    private DCT dct;
    private EmbedData embeddedData;
    private readonly Huffman huffman;
    private readonly JpegInfo JpegObj;
    private int n, quality;

    private readonly BufferedStream output;
    private string password;

    /// <summary>
    /// </summary>
    /// <param name="image"></param>
    /// <param name="quality">
    ///   Quality of the image. 0 to 100 and from bad image quality, high compression to good image quality
    ///   low compression
    /// </param>
    /// <param name="output"></param>
    /// <param name="comment"></param>
    public JpegEncoder(Image image, Stream output, string comment, int quality = 80)
    {
      this.quality = quality;

      // Getting picture information It takes the Width, Height and RGB scans of the image.
      JpegObj = new JpegInfo(image, comment);
      imageHeight = JpegObj.ImageHeight;
      imageWidth = JpegObj.ImageWidth;
      this.output = new BufferedStream(output);
      this.output.SetLength(0);
      dct = new DCT(this.quality);
      huffman = new Huffman();
    }

    public int Quality
    {
      get => quality;

      set
      {
        quality = value;
        dct = new DCT(quality);
      }
    }

    public void Compress()
    {
      WriteHeaders();
      WriteCompressedData();
      WriteEOI();
      output.Flush();
    }

    public void Compress(Stream embeddedData, string password)
    {
      this.embeddedData = new EmbedData(embeddedData);
      this.password = password;
      Compress();
    }

    private void WriteArray(byte[] data)
    {
      var length = ((data[2] & 0xFF) << 8) + (data[3] & 0xFF) + 2;
      output.Write(data, 0, length);
    }

    private void WriteMarker(byte[] data)
    {
      output.Write(data, 0, 2);
    }

    private void WriteEOI()
    {
      byte[] EOI = { 0xFF, 0xD9 };
      WriteMarker(EOI);
    }

    /// <summary>
    ///   the SOI marker
    /// </summary>
    private void WriteHeaderSOI()
    {
      byte[] SOI = { 0xFF, 0xD8 };
      WriteMarker(SOI);
    }

    /// <summary>
    ///   the JFIF header
    /// </summary>
    private void WriteHeaderJFIF()
    {
      // The order of the following headers is quiet inconsequential
      var JFIF = new byte[18]
      {
        0xff, 0xe0, 0x00, 0x10, 0x4a, 0x46,
        0x49, 0x46, 0x00, 0x01, 0x01, 0x01,
        0x00, 0x60, 0x00, 0x60, 0x00, 0x00
      };
      WriteArray(JFIF);
    }

    /// <summary>
    ///   Comment Header
    /// </summary>
    private void WriteHeaderComment()
    {
      var comment = Encoding.Default.GetBytes(JpegObj.Comment);
      var length = comment.Length;
      if (length > 0)
      {
        var COM = new byte[length + 4];
        COM[0] = 0xFF; // comment marker
        COM[1] = 0xFE; // comment marker
        length += 2; // including length of length
        COM[2] = (byte)((length >> 8) & 0xFF);
        COM[3] = (byte)(length & 0xFF);
        Array.Copy(comment, 0, COM, 4, comment.Length);
        WriteArray(COM);
      }
    }

    /// <summary>
    ///   The DQT header
    ///   0 is the luminance index and 1 is the chrominance index
    /// </summary>
    private void WriteHeaderDQT()
    {
      var DQT = new List<byte>(134) { 0xFF, 0xDB, 0x00, 0x84 };
      byte i, j;
      for (i = 0; i < 2; i++)
      {
        DQT.Add((byte)((0 << 4) + i));
        for (j = 0; j < 64; j++) DQT.Add((byte)dct.Quantum[i][Huffman.JpegNaturalOrder[j]]);
      }

      WriteArray(DQT.ToArray());
    }

    /// <summary>
    ///   Start of Frame Header
    /// </summary>
    private void WriteHeaderSOF0()
    {
      byte i;
      var SOF = new List<byte>(19)
      {
        0xFF, 0xC0, 0x00, 0x11,
        JpegInfo.Precision,
        (byte)((JpegObj.ImageHeight >> 8) & 0xFF),
        (byte)(JpegObj.ImageHeight & 0xFF),
        (byte)((JpegObj.ImageWidth >> 8) & 0xFF),
        (byte)(JpegObj.ImageWidth & 0xFF),
        JpegInfo.NumberOfComponents
      };
      for (i = 0; i < JpegInfo.NumberOfComponents; i++)
      {
        SOF.Add((byte)(i + 1));
        SOF.Add((byte)((JpegObj.HsampFactor[i] << 4) + JpegObj.VsampFactor[i]));
        SOF.Add((byte)JpegObj.QtableNumber[i]);
      }

      WriteArray(SOF.ToArray());
    }

    private void WriteHeaderDHT()
    {
      var DHT = new List<byte> { 0xFF, 0xC4, 0x00, 0x00 };
      byte i, j;
      for (i = 0; i < 4; i++)
      {
        for (j = 0; j < huffman.Bits[i].Length; j++) DHT.Add((byte)huffman.Bits[i][j]);

        for (j = 0; j < huffman.Val[i].Length; j++) DHT.Add((byte)huffman.Val[i][j]);
      }

      DHT[2] = (byte)(((DHT.Count - 2) >> 8) & 0xFF);
      DHT[3] = (byte)((DHT.Count - 2) & 0xFF);
      WriteArray(DHT.ToArray());
    }

    private void WriteHeaderSOS()
    {
      byte i;
      var SOS = new List<byte>(14) { 0xFF, 0xDA, 0x00, 0x0C, JpegInfo.NumberOfComponents };
      for (i = 0; i < JpegInfo.NumberOfComponents; i++)
      {
        SOS.Add((byte)(i + 1));
        SOS.Add((byte)((JpegObj.DCtableNumber[i] << 4) + JpegObj.ACtableNumber[i]));
      }

      SOS.Add(JpegInfo.Ss);
      SOS.Add(JpegInfo.Se);
      SOS.Add(JpegInfo.Ah << (4 + JpegInfo.Al));
      WriteArray(SOS.ToArray());
    }

    private void WriteHeaders()
    {
      WriteHeaderSOI();
      WriteHeaderJFIF();
      WriteHeaderComment();
      WriteHeaderDQT();
      WriteHeaderSOF0();
      WriteHeaderDHT();
      WriteHeaderSOS();
    }

    private int[] GetCoeff(int MinBlockWidth, int MinBlockHeight)
    {
      var dctArray1 = ArrayHelper.CreateJagged<float>(8, 8);
      var dctArray2 = ArrayHelper.CreateJagged<double>(8, 8);
      var dctArray3 = new int[8 * 8];
      int[] coeff;
      float[][] inputArray;
      int i, j, r, c, a, b, comp;
      int ypos, Width, Height, xblockoffset, yblockoffset;
      var xpos = 0;
      var shuffledIndex = 0;
      var coeffCount = 0;

      // westfeld
      // Before we enter these loops, we initialise the coeff for steganography here:
      for (r = 0; r < MinBlockHeight; r++)
      for (c = 0; c < MinBlockWidth; c++)
      for (comp = 0; comp < JpegInfo.NumberOfComponents; comp++)
      for (i = 0; i < JpegObj.VsampFactor[comp]; i++)
      for (j = 0; j < JpegObj.HsampFactor[comp]; j++)
        coeffCount += 64;

      coeff = new int[coeffCount];

      logger.Info("DCT/quantisation starts");
      logger.Info(imageWidth + " x " + imageHeight);
      for (r = 0; r < MinBlockHeight; r++)
      for (c = 0; c < MinBlockWidth; c++)
      {
        xpos = c * 8;
        ypos = r * 8;
        for (comp = 0; comp < JpegInfo.NumberOfComponents; comp++)
        {
          Width = JpegObj.BlockWidth[comp];
          Height = JpegObj.BlockHeight[comp];
          inputArray = JpegObj.Components[comp];

          var maxa = imageHeight / 2 * JpegObj.VsampFactor[comp] - 1;
          var maxb = imageWidth / 2 * JpegObj.HsampFactor[comp] - 1;

          for (i = 0; i < JpegObj.VsampFactor[comp]; i++)
          for (j = 0; j < JpegObj.HsampFactor[comp]; j++)
          {
            xblockoffset = j * 8;
            yblockoffset = i * 8;
            for (a = 0; a < 8; a++)
            for (b = 0; b < 8; b++)
            {
              // I believe this is where the dirty line at
              // the bottom of the image is
              // coming from. I need to do a check here to
              // make sure I'm not reading past
              // image data.
              // This seems to not be a big issue right
              // now. (04/04/98)

              // westfeld - dirty line fixed, Jun 6 2000
              var ia = Math.Min(ypos * JpegObj.VsampFactor[comp] + yblockoffset + a, maxa);
              var ib = Math.Min(xpos * JpegObj.HsampFactor[comp] + xblockoffset + b, maxb);

              // dctArray1[a][b] = inputArray[ypos +
              // yblockoffset + a][xpos + xblockoffset +
              // b];
              dctArray1[a][b] = inputArray[ia][ib];
            }

            // The following code commented out because on some
            // images this technique
            // results in poor right and bottom borders.
            // if ((!JpegObj.lastColumnIsDummy[comp] || c <
            // Width - 1) && (!JpegObj.lastRowIsDummy[comp] || r
            // < Height - 1)) {
            dctArray2 = DCT.ForwardDCT(dctArray1);
            dctArray3 = dct.QuantizeBlock(dctArray2, JpegObj.QtableNumber[comp]);
            // }
            // else {
            // zeroArray[0] = dctArray3[0];
            // zeroArray[0] = lastDCvalue[comp];
            // dctArray3 = zeroArray;
            // }
            // westfeld
            // For steganography, all dct
            // coefficients are collected in
            // coeff[] first. We do not encode
            // any Huffman Blocks here (we'll do
            // this later).
            Array.Copy(dctArray3, 0, coeff, shuffledIndex, 64);
            shuffledIndex += 64;
          }
        }
      }

      return coeff;
    }

    /// <summary>
    ///   This method controls the compression of the image. Starting at the
    ///   upper left of the image, it compresses 8x8 blocks of data until the
    ///   entire image has been compressed.
    /// </summary>
    /// <param name="out"></param>
    private void WriteCompressedData()
    {
      // This initial setting of MinBlockWidth and MinBlockHeight is done to
      // ensure they start with values larger than will actually be the case.
      var MinBlockWidth = imageWidth % 8 != 0 ? (int)(Math.Floor(imageWidth / 8.0) + 1) * 8 : imageWidth;
      var MinBlockHeight = imageHeight % 8 != 0 ? (int)(Math.Floor(imageHeight / 8.0) + 1) * 8 : imageHeight;
      int comp, shuffledIndex;
      for (comp = 0; comp < JpegInfo.NumberOfComponents; comp++)
      {
        MinBlockWidth = Math.Min(MinBlockWidth, JpegObj.BlockWidth[comp]);
        MinBlockHeight = Math.Min(MinBlockHeight, JpegObj.BlockHeight[comp]);
      }

      var lastDCvalue = new int[JpegInfo.NumberOfComponents];
      var emptyArray = new int[64];
      var coeff = GetCoeff(MinBlockWidth, MinBlockHeight);
      var coeffCount = coeff.Length;
      int i, j, r, c;
      var _changed = 0;
      var _embedded = 0;
      var _examined = 0;
      var _expected = 0;
      var _one = 0;
      var _large = 0;
      var _thrown = 0;
      var _zero = 0;

      logger.Info("got " + coeffCount + " DCT AC/DC coefficients");
      for (i = 0; i < coeffCount; i++)
        if (i % 64 == 0)
          continue;
        else if (coeff[i] == 1 || coeff[i] == -1)
          _one++;
        else if (coeff[i] == 0)
          _zero++;

      _large = coeffCount - _zero - _one - coeffCount / 64;
      _expected = _large + (int)(0.49 * _one);
      //

      logger.Info("one=" + _one);
      logger.Info("large=" + _large);
      //
      logger.Info("expected capacity: " + _expected + " bits");
      logger.Info("expected capacity with");

      for (i = 1; i < 8; i++)
      {
        int usable, changed, n;
        n = (1 << i) - 1;
        usable = _expected * i / n - _expected * i / n % n;
        changed = coeffCount - _zero - coeffCount / 64;
        changed = changed * i / n - changed * i / n % n;
        changed = n * changed / (n + 1) / i;
        //
        changed = _large - _large % (n + 1);
        changed = (changed + _one + _one / 2 - _one / (n + 1)) / (n + 1);
        usable /= 8;
        if (usable == 0)
          break;
        if (i == 1)
          logger.Info("default");
        else
          logger.Info("(1, " + n + ", " + i + ")");
        logger.Info(" code: " + usable + " bytes (efficiency: " + usable * 8 / changed + "." +
                    usable * 80 / changed % 10 + " bits per change)");
      }

      // westfeld
      if (embeddedData != null)
      {
        // Now we embed the secret data in the permutated sequence.
        logger.Info("Permutation starts");
        var random = new F5Random(Encoding.ASCII.GetBytes(password));
        var permutation = new Permutation(coeffCount, random);
        var nextBitToEmbed = 0;
        var byteToEmbed = Convert.ToInt32(embeddedData.Length);
        var availableBitsToEmbed = 0;
        // We start with the length information. Well,
        // the length information it is more than one
        // byte, so this first "byte" is 32 bits long.

        /*try {
            byteToEmbed = this.embeddedData.available();
        } catch (final Exception e) {
            e.printStackTrace();
        }*/


        logger.Info("Embedding of " + (byteToEmbed * 8 + 32) + " bits (" + byteToEmbed + "+4 bytes) ");
        // We use the most significant byte for the 1 of n
        // code, and reserve one extra bit for future use.
        if (byteToEmbed > 0x007fffff) byteToEmbed = 0x007fffff;

        // We calculate n now
        for (i = 1; i < 8; i++)
        {
          int usable;
          n = (1 << i) - 1;
          usable = _expected * i / n - _expected * i / n % n;
          usable /= 8;
          if (usable == 0) break;

          if (usable < byteToEmbed + 4) break;
        }

        var k = i - 1;
        n = (1 << k) - 1;
        switch (n)
        {
          case 0:
            logger.Info("using default code, file will not fit");
            n++;
            break;
          case 1:
            logger.Info("using default code");
            break;
          default:
            logger.Info("using (1, " + n + ", " + k + ") code");
            break;
        }

        byteToEmbed |= k << 24; // store k in the status word
        // Since shuffling cannot hide the distribution, the
        // distribution of all bits to embed is unified by
        // adding a pseudo random bit-string. We continue the random
        // we used for Permutation, initially seeked with password.
        byteToEmbed ^= random.GetNextByte();
        byteToEmbed ^= random.GetNextByte() << 8;
        byteToEmbed ^= random.GetNextByte() << 16;
        byteToEmbed ^= random.GetNextByte() << 24;
        nextBitToEmbed = byteToEmbed & 1;
        byteToEmbed >>= 1;
        availableBitsToEmbed = 31;
        _embedded++;

        for (i = 0; i < permutation.Length; i++)
        {
          var shuffled_index = permutation.GetShuffled(i);

          if (shuffled_index % 64 == 0 || coeff[shuffled_index] == 0)
            continue;

          var cc = coeff[shuffled_index];
          _examined += 1;

          if (cc > 0 && (cc & 1) != nextBitToEmbed)
          {
            coeff[shuffled_index]--;
            _changed++;
          }
          else if (cc < 0 && (cc & 1) == nextBitToEmbed)
          {
            coeff[shuffled_index]++;
            _changed++;
          }

          if (coeff[shuffled_index] != 0)
          {
            if (availableBitsToEmbed == 0)
            {
              if (n > 1 || embeddedData.Available == 1)
                break;

              byteToEmbed = embeddedData.Read();
              byteToEmbed ^= random.GetNextByte();
              availableBitsToEmbed = 8;
            }

            nextBitToEmbed = byteToEmbed & 1;
            byteToEmbed >>= 1;
            availableBitsToEmbed--;
            _embedded++;
          }
          else
          {
            _thrown++;
          }
        }

        if (n > 1)
        {
          var isLastByte = false;
          var filtered_index = permutation.Filter(coeff, i + 1);
          while (!isLastByte)
          {
            var kBitsToEmbed = 0;
            for (i = 0; i < k; i++)
            {
              if (availableBitsToEmbed == 0)
              {
                if (embeddedData.Available == 0)
                {
                  isLastByte = true;
                  break;
                }

                byteToEmbed = embeddedData.Read();
                byteToEmbed ^= random.GetNextByte();
                availableBitsToEmbed = 8;
              }

              nextBitToEmbed = byteToEmbed & 1;
              byteToEmbed >>= 1;
              availableBitsToEmbed--;
              kBitsToEmbed |= nextBitToEmbed << i;
              _embedded++;
            }

            var codeWord = filtered_index.Offer(n);
            int extractedBit;
            while (true)
            {
              var vhash = 0;
              var count = codeWord.Count;
              for (i = 0; i < count; i++)
              {
                var index = codeWord[i];
                extractedBit = coeff[index] > 0 ? coeff[index] & 1 : 1 - (coeff[index] & 1);
                if (extractedBit == 1) vhash ^= i + 1;
              }

              i = vhash ^ kBitsToEmbed;
              if (i == 0)
                break;
              i--;

              if (coeff[codeWord[i]] < 0)
                coeff[codeWord[i]]++;
              else
                coeff[codeWord[i]]--;
              _changed++;

              if (coeff[codeWord[i]] == 0)
              {
                _thrown++;
                codeWord.RemoveAt(i);
                codeWord.Add(filtered_index.Offer());
              }
            }
          }
        }
      }

      logger.Info("Starting Huffman Encoding.");
      shuffledIndex = 0;
      for (r = 0; r < MinBlockHeight; r++)
      for (c = 0; c < MinBlockWidth; c++)
      for (comp = 0; comp < JpegInfo.NumberOfComponents; comp++)
      for (i = 0; i < JpegObj.VsampFactor[comp]; i++)
      for (j = 0; j < JpegObj.HsampFactor[comp]; j++)
      {
        Array.Copy(coeff, shuffledIndex, emptyArray, 0, 64);
        huffman.HuffmanBlockEncoder(output, emptyArray, lastDCvalue[comp],
          JpegObj.DCtableNumber[comp], JpegObj.ACtableNumber[comp]);
        lastDCvalue[comp] = emptyArray[0];
        shuffledIndex += 64;
      }

      huffman.FlushBuffer(output);
    }

    #region IDisposable

    private bool _disposed;

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    ~JpegEncoder()
    {
      Dispose(false);
    }

    private void Dispose(bool disposing)
    {
      if (_disposed)
        return;
      if (disposing)
      {
        if (embeddedData != null) embeddedData.Dispose();

        if (output != null) output.Dispose();
      }

      _disposed = true;
    }

    #endregion
  }
}
