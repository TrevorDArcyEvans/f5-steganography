namespace F5.Core.Crypt
{
  internal sealed class Permutation
  {
    private readonly int[] shuffled; // shuffled sequence

    // The constructor of class Permutation creates a shuffled
    // sequence of the integers 0 ... (size-1).
    internal Permutation(int size, F5Random random)
    {
      int i, randomIndex;
      shuffled = new int[size];

      // To create the shuffled sequence, we initialise an array
      // with the integers 0 ... (size-1).
      for (i = 0; i < size; i++)
        // initialise with "size" integers
        shuffled[i] = i;
      var maxRandom = size; // set number of entries to shuffle
      for (i = 0; i < size; i++)
      {
        // shuffle entries
        randomIndex = random.GetNextValue(maxRandom--);
        Swap(ref shuffled[maxRandom], ref shuffled[randomIndex]);
      }
    }

    public int Length => shuffled.Length;

    private static void Swap(ref int a, ref int b)
    {
      var temp = a;
      a = b;
      b = temp;
    }

    /// <summary>
    ///   get value #i from the shuffled sequence
    /// </summary>
    public int GetShuffled(int i)
    {
      return shuffled[i];
    }

    public FilteredCollection Filter(int[] coeff, int startIndex)
    {
      return new FilteredCollection(shuffled, coeff, startIndex);
    }
  }
}
