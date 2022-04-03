namespace F5.Crypt
{
  using Org.BouncyCastle.Crypto.Digests;
  using Org.BouncyCastle.Crypto.Prng;

  public class BufferedSecureRandom
  {
    private readonly byte[] buffer;
    private readonly int bufferSize;
    private readonly DigestRandomGenerator random;
    private int current;

    public BufferedSecureRandom(byte[] password, int bufferSize = 1024)
    {
      this.bufferSize = bufferSize;
      buffer = new byte[bufferSize];
      random = new DigestRandomGenerator(new Sha1Digest());
      random.AddSeedMaterial(password);
      random.NextBytes(buffer);
    }

    public byte Next()
    {
      if (current >= bufferSize)
      {
        random.NextBytes(buffer);
        current = 0;
      }

      return buffer[current++];
    }
  }

  internal class F5Random
  {
    private readonly BufferedSecureRandom random;

    public F5Random(byte[] password)
    {
      random = new BufferedSecureRandom(password);
    }

    /// <summary>
    ///   get a random byte
    /// </summary>
    /// <returns>random signed byte</returns>
    public int GetNextByte()
    {
      return random.Next();
    }

    /// <summary>
    ///   get a random integer 0 ... (maxValue-1)
    /// </summary>
    /// <param name="maxValue">maxValue (excluding)</param>
    /// <returns>random integer</returns>
    public int GetNextValue(int maxValue)
    {
      var retVal = GetNextByte() | (GetNextByte() << 8) | (GetNextByte() << 16) | (GetNextByte() << 24);
      retVal %= maxValue;
      return retVal < 0 ? retVal + maxValue : retVal;
    }
  }
}
