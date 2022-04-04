namespace F5.Core.Util;

using System;
using System.IO;

internal sealed class EmbedData : IDisposable
{
  private readonly Stream _data;

  internal EmbedData(Stream data)
  {
    this._data = data;
    Seek(0, SeekOrigin.Begin);
  }

  internal EmbedData(byte[] data)
    : this(new MemoryStream(data))
  {
  }

  public long Available => _data.Length - _data.Position;

  public long Length => _data.Length;

  public byte Read()
  {
    var b = _data.ReadByte();
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
    _data.Close();
  }

  public long Seek(long offset, SeekOrigin origin)
  {
    return _data.Seek(offset, origin);
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
      _data.Dispose();
    _disposed = true;
  }

  #endregion
}