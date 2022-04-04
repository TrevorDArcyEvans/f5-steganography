namespace F5.Core.Crypt;

using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Prng;

internal sealed class BufferedSecureRandom
{
  private readonly byte[] _buffer;
  private readonly int _bufferSize;
  private readonly DigestRandomGenerator _random;
  private int _current;

  public BufferedSecureRandom(byte[] password, int bufferSize = 1024)
  {
    _bufferSize = bufferSize;
    _buffer = new byte[bufferSize];
    _random = new DigestRandomGenerator(new Sha1Digest());
    _random.AddSeedMaterial(password);
    _random.NextBytes(_buffer);
  }

  public byte Next()
  {
    if (_current < _bufferSize)
    {
      return _buffer[_current++];
    }

    _random.NextBytes(_buffer);
    _current = 0;

    return _buffer[_current++];
  }
}
