//***********************************************************************
// Assembly         : LatestMediaHandler
// Author           : cul8er
// Created          : 05-09-2010
//
// Last Modified By : ajs
// Last Modified On : 10-05-2010
// Description      : 
//
// Copyright        : Open Source software licensed under the GNU/GPL agreement.
//***********************************************************************

namespace LatestMediaHandler
{
  /// <summary>
  /// Container for fanart data.
  /// </summary>
  internal class FanartImage
  {
    private string _id; // ENCAPSULATE FIELD BY CODEIT.RIGHT

    internal string Id
    {
      get { return _id; }
      set { _id = value; }
    }

    private string _artist; // ENCAPSULATE FIELD BY CODEIT.RIGHT

    internal string Artist
    {
      get { return _artist; }
      set { _artist = value; }
    }

    private string _diskImage; // ENCAPSULATE FIELD BY CODEIT.RIGHT

    internal string DiskImage
    {
      get { return _diskImage; }
      set { _diskImage = value; }
    }

    private string _sourceImage; // ENCAPSULATE FIELD BY CODEIT.RIGHT

    internal string SourceImage
    {
      get { return _sourceImage; }
      set { _sourceImage = value; }
    }

    private string _type; // ENCAPSULATE FIELD BY CODEIT.RIGHT

    internal string Type
    {
      get { return _type; }
      set { _type = value; }
    }

    private string _source; // ENCAPSULATE FIELD BY CODEIT.RIGHT

    internal string Source
    {
      get { return _source; }
      set { _source = value; }
    }

    /// <summary>
    /// Initializes a new instance of the FanartImage class.
    /// </summary>
    /// <param name="id">Identifier number</param>
    /// <param name="artist">Artist name</param>
    /// <param name="diskImage">Filename on disk</param>
    /// <param name="sourceImage">Filename at source</param>
    /// <param name="type">Type of the file</param>
    /// <param name="source">Source name (like htbackdrops)</param>
    internal FanartImage(string id, string artist, string diskImage, string sourceImage, string type, string source)
    {
      this.Id = id;
      this.Artist = artist;
      this.DiskImage = diskImage;
      this.SourceImage = sourceImage;
      this.Type = type;
      this.Source = source;
    }
  }
}
