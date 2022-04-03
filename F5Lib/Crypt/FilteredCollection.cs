namespace F5.Crypt
{
  using System.Collections.Generic;

  internal sealed class FilteredCollection
  {
    private readonly int[] coeff;
    private readonly int[] iterable;
    private int now;

    public FilteredCollection(int[] iterable, int[] coeff)
    {
      this.iterable = iterable;
      this.coeff = coeff;
    }

    public FilteredCollection(int[] iterable, int[] coeff, int startIndex)
      : this(iterable, coeff)
    {
      now = startIndex;
    }

    public int Current => iterable[now];

    private bool IsValid(int n)
    {
      return n % 64 != 0 && coeff[n] != 0;
    }

    public List<int> Offer(int count)
    {
      var result = new List<int>(count);
      while (count > 0)
      {
        while (now < iterable.Length && !IsValid(Current)) now++;
        if (now < iterable.Length)
        {
          count--;
          result.Add(Current);
          now++;
        }
      }

      return result;
    }

    public int Offer()
    {
      return Offer(1)[0];
    }
  }
}
