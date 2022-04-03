namespace F5.Util
{
  internal static class ArrayHelper
  {
    public static T[][] CreateJagged<T>(int x, int y)
    {
      var result = new T[x][];
      for (var i = 0; i < x; i++) result[i] = new T[y];
      return result;
    }

    public static T[][][] CreateJagged<T>(int x, int y, int z)
    {
      var result = new T[x][][];
      for (var i = 0; i < x; i++)
      {
        result[i] = new T[y][];
        for (var j = 0; j < y; j++) result[i][j] = new T[z];
      }

      return result;
    }
  }
}
