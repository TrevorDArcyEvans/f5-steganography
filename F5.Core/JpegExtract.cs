namespace F5.Core
{
  using System.Text;
  using Crypt;
  using Ortega;
  using log4net;

  public sealed class JpegExtract : IDisposable
  {
    private static readonly ILog Logger = LogManager.GetLogger(typeof(JpegExtract));
    private int _availableExtractedBits;
    private int _extractedBit, _pos;
    private int _extractedByte;
    private int _extractedFileLength;
    private int _nBytesExtracted;
    private readonly Stream _output;
    private readonly F5Random _random;
    private int _shuffledIndex;

    public JpegExtract(Stream output, string password)
      : this(output, Encoding.ASCII.GetBytes(password))
    {
    }

    public JpegExtract(Stream output, byte[] password)
    {
      _output = output;
      _random = new F5Random(password);
    }

    public void Extract(Stream input)
    {
      int[] coeff;
      int i, n, k, hash, code;

      using (var hd = new HuffmanDecode(input))
      {
        coeff = hd.Decode();
      }

      Logger.Info("Permutation starts");
      var permutation = new Permutation(coeff.Length, _random);
      Logger.Info(coeff.Length + " indices shuffled");

      // extract length information
      CalcEmbeddedLength(permutation, coeff);
      k = (_extractedFileLength >> 24) % 32;
      n = (1 << k) - 1;
      _extractedFileLength &= 0x007fffff;

      Logger.Info("Length of embedded file: " + _extractedFileLength + " bytes");

      if (n > 0)
      {
        while (true)
        {
          hash = 0;
          code = 1;
          while (code <= n)
          {
            _pos++;
            if (_pos >= coeff.Length)
            {
              goto leaveContext;
            }

            _shuffledIndex = permutation.GetShuffled(_pos);
            _extractedBit = ExtractBit(coeff);
            if (_extractedBit == -1)
            {
              continue;
            }

            if (_extractedBit == 1)
            {
              hash ^= code;
            }

            code++;
          }

          for (i = 0; i < k; i++)
          {
            _extractedByte |= ((hash >> i) & 1) << _availableExtractedBits++;
            if (_availableExtractedBits == 8)
            {
              WriteExtractedByte();
              // check for pending end of embedded data
              if (_nBytesExtracted == _extractedFileLength)
              {
                goto leaveContext;
              }
            }
          }
        }
      }

      while (++_pos < coeff.Length && _pos < permutation.Length)
      {
        _shuffledIndex = permutation.GetShuffled(_pos);
        _extractedBit = ExtractBit(coeff);
        if (_extractedBit == -1)
        {
          continue;
        }

        _extractedByte |= _extractedBit << _availableExtractedBits++;
        if (_availableExtractedBits == 8)
        {
          WriteExtractedByte();
          if (_nBytesExtracted == _extractedFileLength)
          {
            break;
          }
        }
      }

      leaveContext: ;
      if (_nBytesExtracted < _extractedFileLength)
      {
        Logger.Warn("Incomplete file: only " + _nBytesExtracted + " of " + _extractedFileLength + " bytes extracted");
      }
    }

    /// <summary>
    ///   extract length information
    /// </summary>
    private void CalcEmbeddedLength(Permutation permutation, int[] coeff)
    {
      _extractedFileLength = 0;
      _pos = -1;

      var i = 0;
      while (i < 32 && ++_pos < coeff.Length)
      {
        _shuffledIndex = permutation.GetShuffled(_pos);
        _extractedBit = ExtractBit(coeff);
        if (_extractedBit == -1)
        {
          continue;
        }

        _extractedFileLength |= _extractedBit << i++;
      }

      // remove pseudo random pad
      _extractedFileLength ^= _random.GetNextByte();
      _extractedFileLength ^= _random.GetNextByte() << 8;
      _extractedFileLength ^= _random.GetNextByte() << 16;
      _extractedFileLength ^= _random.GetNextByte() << 24;
    }

    private void WriteExtractedByte()
    {
      // remove pseudo random pad
      _extractedByte ^= _random.GetNextByte();
      _output.WriteByte((byte)_extractedByte);
      _extractedByte = 0;
      _availableExtractedBits = 0;
      _nBytesExtracted++;
    }

    private int ExtractBit(int[] coeff)
    {
      int coeffVal;
      var mod64 = _shuffledIndex % 64;
      if (mod64 == 0)
      {
        return -1;
      }

      _shuffledIndex = _shuffledIndex - mod64 + HuffmanDecode.deZigZag[mod64];
      coeffVal = coeff[_shuffledIndex];
      if (coeffVal == 0)
      {
        return -1;
      }

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
      {
        return;
      }

      if (disposing)
      {
        _output.Dispose();
      }

      _disposed = true;
    }

    #endregion
  }
}
