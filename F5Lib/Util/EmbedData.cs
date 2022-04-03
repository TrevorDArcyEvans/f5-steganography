namespace F5.Util
{
  using System;
  using System.IO;

  internal sealed class EmbedData : IDisposable
  {
    private readonly Stream data;

    internal EmbedData(Stream data)
    {
      this.data = data;
      Seek(0, SeekOrigin.Begin);
    }

    internal EmbedData(byte[] data)
      : this(new MemoryStream(data))
    {
    }

    public long Available => data.Length - data.Position;

    public long Length => data.Length;

    public byte Read()
    {
      var b = data.ReadByte();
      return (byte)(b == -1 ? 0 : b);
    }

    /// <summary>
    ///   Read Integer from Stream
    /// </summary>
    public int ReadInt()
    {
      int b = Read();
      b <<= 8;
      b ^= Read();
      return b;
    }

    public void Close()
    {
      data.Close();
    }

    public long Seek(long offset, SeekOrigin origin)
    {
      return data.Seek(offset, origin);
    }

    #region IDisposable

    private bool _disposed;

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    ~EmbedData()
    {
      Dispose(false);
    }

    private void Dispose(bool disposing)
    {
      if (_disposed)
        return;
      if (disposing)
        data.Dispose();
      _disposed = true;
    }

    #endregion
  }
}
