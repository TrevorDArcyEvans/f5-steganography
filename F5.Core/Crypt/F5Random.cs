namespace F5.Core.Crypt;

internal sealed class F5Random
{
  private readonly BufferedSecureRandom _random;

  public F5Random(byte[] password)
  {
    _random = new BufferedSecureRandom(password);
  }

  /// <summary>
  ///   get a random byte
  /// </summary>
  /// <returns>random signed byte</returns>
  public int GetNextByte()
  {
    return _random.Next();
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
