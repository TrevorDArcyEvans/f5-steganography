namespace F5
{
  using System;
  using System.IO;
  using System.Text;
  using F5.Core.Crypt;
  using F5.Core.Ortega;
  using log4net;

  public class JpegExtract : IDisposable
  {
    private static readonly ILog logger = LogManager.GetLogger(typeof(JpegExtract));
    private int availableExtractedBits;
    private int extractedBit, pos;
    private int extractedByte;
    private int extractedFileLength;
    private int nBytesExtracted;
    private readonly Stream output;
    private readonly F5Random random;
    private int shuffledIndex;

    public JpegExtract(Stream output, string password)
      : this(output, Encoding.ASCII.GetBytes(password))
    {
    }

    public JpegExtract(Stream output, byte[] password)
    {
      this.output = output;
      random = new F5Random(password);
    }

    public void Extract(Stream input)
    {
      int[] coeff;
      int i, n, k, hash, code;

      using (var hd = new HuffmanDecode(input))
      {
        coeff = hd.Decode();
      }

      logger.Info("Permutation starts");
      var permutation = new Permutation(coeff.Length, random);
      logger.Info(coeff.Length + " indices shuffled");

      // extract length information
      CalcEmbeddedLength(permutation, coeff);
      k = (extractedFileLength >> 24) % 32;
      n = (1 << k) - 1;
      extractedFileLength &= 0x007fffff;

      logger.Info("Length of embedded file: " + extractedFileLength + " bytes");

      if (n > 0)
        while (true)
        {
          hash = 0;
          code = 1;
          while (code <= n)
          {
            pos++;
            if (pos >= coeff.Length)
              goto leaveContext;
            shuffledIndex = permutation.GetShuffled(pos);
            extractedBit = ExtractBit(coeff);
            if (extractedBit == -1)
              continue;
            if (extractedBit == 1)
              hash ^= code;
            code++;
          }

          for (i = 0; i < k; i++)
          {
            extractedByte |= ((hash >> i) & 1) << availableExtractedBits++;
            if (availableExtractedBits == 8)
            {
              WriteExtractedByte();
              // check for pending end of embedded data
              if (nBytesExtracted == extractedFileLength)
                goto leaveContext;
            }
          }
        }

      while (++pos < coeff.Length && pos < permutation.Length)
      {
        shuffledIndex = permutation.GetShuffled(pos);
        extractedBit = ExtractBit(coeff);
        if (extractedBit == -1)
          continue;
        extractedByte |= extractedBit << availableExtractedBits++;
        if (availableExtractedBits == 8)
        {
          WriteExtractedByte();
          if (nBytesExtracted == extractedFileLength)
            break;
        }
      }

      leaveContext: ;
      if (nBytesExtracted < extractedFileLength)
        logger.Warn("Incomplete file: only " + nBytesExtracted +
                    " of " + extractedFileLength + " bytes extracted");
    }

    /// <summary>
    ///   extract length information
    /// </summary>
    private void CalcEmbeddedLength(Permutation permutation, int[] coeff)
    {
      extractedFileLength = 0;
      pos = -1;

      var i = 0;
      while (i < 32 && ++pos < coeff.Length)
      {
        shuffledIndex = permutation.GetShuffled(pos);
        extractedBit = ExtractBit(coeff);
        if (extractedBit == -1)
          continue;
        extractedFileLength |= extractedBit << i++;
      }

      // remove pseudo random pad
      extractedFileLength ^= random.GetNextByte();
      extractedFileLength ^= random.GetNextByte() << 8;
      extractedFileLength ^= random.GetNextByte() << 16;
      extractedFileLength ^= random.GetNextByte() << 24;
    }

    private void WriteExtractedByte()
    {
      // remove pseudo random pad
      extractedByte ^= random.GetNextByte();
      output.WriteByte((byte)extractedByte);
      extractedByte = 0;
      availableExtractedBits = 0;
      nBytesExtracted++;
    }

    private int ExtractBit(int[] coeff)
    {
      int coeffVal;
      var mod64 = shuffledIndex % 64;
      if (mod64 == 0)
        return -1;
      shuffledIndex = shuffledIndex - mod64 + HuffmanDecode.deZigZag[mod64];
      coeffVal = coeff[shuffledIndex];
      if (coeffVal == 0)
        return -1;
      return coeffVal > 0 ? coeffVal & 1 : 1 - (coeffVal & 1);
    }

    #region IDisposable

    private bool _disposed;

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    ~JpegExtract()
    {
      Dispose(false);
    }

    private void Dispose(bool disposing)
    {
      if (_disposed)
        return;
      if (disposing)
        output.Dispose();
      _disposed = true;
    }

    #endregion
  }
}
