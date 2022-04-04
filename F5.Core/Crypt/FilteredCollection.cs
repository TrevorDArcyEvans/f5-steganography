namespace F5.Core.Crypt;

using System.Collections.Generic;

internal sealed class FilteredCollection
{
  private readonly int[] _coeff;
  private readonly int[] _iterable;
  private int _now;

  public FilteredCollection(int[] iterable, int[] coeff)
  {
    _iterable = iterable;
    _coeff = coeff;
  }

  public FilteredCollection(int[] iterable, int[] coeff, int startIndex)
    : this(iterable, coeff)
  {
    _now = startIndex;
  }

  public int Current => _iterable[_now];

  private bool IsValid(int n)
  {
    return n % 64 != 0 && _coeff[n] != 0;
  }

  public List<int> Offer(int count)
  {
    var result = new List<int>(count);
    while (count > 0)
    {
      while (_now < _iterable.Length && !IsValid(Current)) _now++;
      if (_now < _iterable.Length)
      {
        count--;
        result.Add(Current);
        _now++;
      }
    }

    return result;
  }

  public int Offer()
  {
    return Offer(1)[0];
  }
}
