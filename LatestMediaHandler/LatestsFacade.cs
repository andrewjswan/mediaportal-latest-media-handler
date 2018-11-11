//***********************************************************************
// Assembly         : LatestMediaHandler
// Author           : ajs
// Created          : 15-01-2018
//
// Last Modified By : ajs
// Last Modified On : 15-01-2018
// Description      : 
//
// Copyright        : Open Source software licensed under the GNU/GPL agreement.
//***********************************************************************

using MediaPortal.GUI.Library;

using System;
using System.Xml;

namespace LatestMediaHandler
{
  /// <summary>
  /// Container for Latests Facade.
  /// </summary>
  internal class LatestsFacade
  {
    internal string Handler
    {
      get { return _handler; }
      set { _handler = value; }
    }
    private string _handler;

    internal string Title
    {
      get { return _title; }
    }
    private string _title;

    internal string MusicTitle
    {
      get { return _musictitle; }
    }
    private string _musictitle;

    internal string SubTitle
    {
      get { return _subtitle; }
    }
    private string _subtitle;

    internal string ThumbTitle
    {
      get { return _thumbtitle; }
    }
    private string _thumbtitle;

    internal int ControlID
    {
      get { return _id; }
      set { _id = value; }
    }
    private int _id;

    internal int FocusedID
    {
      get { return _focusedId; }
      set { _focusedId = value; }
    }
    private int _focusedId;

    internal int SelectedItem
    {
      get { return _selectedItem; }
      set { _selectedItem = value; }
    }
    private int _selectedItem;

    internal int SelectedImage
    {
      get { return _selectedImage; }
      set { _selectedImage = value; }
    }
    private int _selectedImage;

    internal bool LeftToRight
    {
      get { return _leftToRight; }
      set { _leftToRight = value; }
    }
    private bool _leftToRight;

    internal bool UnWatched
    {
      get { return _unwatched; }
      set { _unwatched = value; }
    }
    private bool _unwatched;

    internal bool AddProperties
    {
      get { return _addproperties; }
      set { _addproperties = value; }
    }
    private bool _addproperties;

    internal bool HasNew
    {
      get { return _hasnew; }
      set { _hasnew = value; }
    }
    private bool _hasnew;

    internal LatestsFacadeType Type
    {
      get { return _type; }
      set
      {
        _type = value;
        _title = string.Empty;
        _musictitle = string.Empty;
        switch (_type)
        {
          case LatestsFacadeType.Latests:
            _title = Translation.LabelLatestAdded;
            _musictitle = Translation.LatestAddedMusic;
            break;
          case LatestsFacadeType.Watched:
            _title = Translation.LabelLatestWatched;
            break;
          case LatestsFacadeType.Rated:
            _title = Translation.LabelHighestRated;
            break;
          case LatestsFacadeType.Played:
            _title = Translation.LabelLatestPlayed;
            _musictitle = Translation.LatestPlayedMusic;
            break;
          case LatestsFacadeType.MostPlayed:
            _title = Translation.LabelMostPlayed;
            _musictitle = Translation.MostPlayedMusic;
            break;
          case LatestsFacadeType.Next:
            _title = Translation.DisplayNextEpisodes;
            break;
        }
      }
    }
    private LatestsFacadeType _type;

    internal LatestsFacadeSubType SubType
    {
      get { return _subtype; }
      set
      {
        _subtype = value;
        _subtitle = string.Empty;
        switch (_subtype)
        {
          case LatestsFacadeSubType.Series:
            _subtitle = Translation.LabelSeriesLatestSeries;
            break;
          case LatestsFacadeSubType.Seasons:
            _subtitle = Translation.LabelSeriesLatestSeasons;
            break;
          case LatestsFacadeSubType.Episodes:
            _subtitle = Translation.LabelSeriesLatestEpisodes;
            break;
        }
      }
    }
    private LatestsFacadeSubType _subtype;

    internal LatestsFacadeThumbType ThumbType
    {
      get { return _thumbtype; }
      set 
      { 
        _thumbtype = value; 
        _thumbtitle = string.Empty;
        switch (_thumbtype)
        {
          case LatestsFacadeThumbType.Series:
            _thumbtitle = Translation.SeriesThumbSeries;
            break;
          case LatestsFacadeThumbType.Seasons:
            _thumbtitle = Translation.SeriesThumbSeasons;
            break;
          case LatestsFacadeThumbType.Episodes:
            _thumbtitle = Translation.SeriesThumbEpisodes;
            break;
          case LatestsFacadeThumbType.Track:
            _thumbtitle = Translation.MvCThumbTrack;
            break;
          case LatestsFacadeThumbType.Album:
            _thumbtitle = Translation.MvCThumbAlbum;
            break;
          case LatestsFacadeThumbType.Artist:
            _thumbtitle = Translation.MvCThumbArtist;
            break;
        }
      }
    }
    private LatestsFacadeThumbType _thumbtype;

    internal GUIFacadeControl.Layout Layout
    {
      get { return _layout; }
      set { _layout = value; }
    }
    private GUIFacadeControl.Layout _layout;

    internal int Update = 0;
    internal GUIFacadeControl Facade = null;
    
    /// <summary>
    /// Initializes a new instance of the FanartImage class.
    /// </summary>
    internal LatestsFacade()
    {
      ControlID = 0;
      FocusedID = -1;
      SelectedItem = -1;
      SelectedImage = -1;
      LeftToRight = true;
      UnWatched = true;
      AddProperties = false;
      HasNew = false;
      Update = 0;
      Type = LatestsFacadeType.Latests;
      SubType = LatestsFacadeSubType.None;
      ThumbType = LatestsFacadeThumbType.None;
      Layout = GUIFacadeControl.Layout.Filmstrip;
    }

    /// <summary>
    /// Initializes a new instance of the FanartImage class with ID.
    /// </summary>
    /// <param name="id">Control ID</param>
    internal LatestsFacade(int id) : this()
    {
      ControlID = id;
    }

    /// <summary>
    /// Initializes a new instance of the FanartImage class with ID.
    /// </summary>
    /// <param name="id">Control ID</param>
    /// <param name="name">Handler name</param>
    internal LatestsFacade(int id, string name) : this(id)
    {
      Handler = name;
    }

    /// <summary>
    /// Initializes a new instance of the FanartImage class from XMLNode.
    /// </summary>
    /// <param name="node">XML Facade Node</param>
    internal LatestsFacade(XmlNode node) : this()
    {
      if (node == null)
      {
        return;
      }

      XmlNode nodeID = node.SelectSingleNode("id");
      if (nodeID != null && nodeID.InnerText != null)
      {
        string _id = nodeID.InnerText;
        if (!string.IsNullOrEmpty(_id))
        {
          int _ID = 0;
          if (Int32.TryParse(_id, out _ID))
          {
            ControlID = _ID;
          }
        }
      }

      XmlNode nodeText = node.SelectSingleNode("lefttoright");
      if (nodeText != null && nodeText.InnerText != null)
      {
        string innerText = nodeText.InnerText;
        LeftToRight = Utils.GetBool(innerText);
      }

      nodeText = node.SelectSingleNode("watched");
      if (nodeText != null && nodeText.InnerText != null)
      {
        string innerText = nodeText.InnerText;
        UnWatched = Utils.GetBool(innerText);
      }

      nodeText = node.SelectSingleNode("addproperties");
      if (nodeText != null && nodeText.InnerText != null)
      {
        string innerText = nodeText.InnerText;
        AddProperties = Utils.GetBool(innerText);
      }

      nodeText = node.SelectSingleNode("layout");
      if (nodeText != null && nodeText.InnerText != null)
      {
        string innerText = nodeText.InnerText;
        SetFacadeLayout(innerText);
      }

      nodeText = node.SelectSingleNode("type");
      if (nodeText != null && nodeText.InnerText != null)
      {
        string innerText = nodeText.InnerText;
        SetFacadeType(innerText);
      }

      nodeText = node.SelectSingleNode("subtype");
      if (nodeText != null && nodeText.InnerText != null)
      {
        string innerText = nodeText.InnerText;
        SetFacadeSubType(innerText);
      }

      nodeText = node.SelectSingleNode("thumb");
      if (nodeText != null && nodeText.InnerText != null)
      {
        string innerText = nodeText.InnerText;
        SetFacadeThumbType(innerText);
      }
    }

    /// <summary>
    /// Initializes a new instance of the FanartImage class from XMLNode.
    /// </summary>
    /// <param name="node">XML Facade Node</param>
    /// <param name="name">Handler name</param>
    internal LatestsFacade(string name, XmlNode node) : this(node)
    {
      Handler = name;
    }

    /// <summary>
    /// Set Main facade Skin settings
    /// </summary>
    /// <param name="node">XML Main Facade Node</param>
    internal void SetMainFacadeFromSkin(XmlNode node)
    {
      if (node == null)
      {
        return;
      }

      XmlNode nodeText = node.SelectSingleNode("lefttoright");
      if (nodeText != null && nodeText.InnerText != null)
      {
        string mainLeftToRight = nodeText.InnerText;
        if (!string.IsNullOrWhiteSpace(mainLeftToRight))
        {
          LeftToRight = Utils.GetBool(mainLeftToRight);
        }
      }

      nodeText = node.SelectSingleNode("layout");
      if (nodeText != null && nodeText.InnerText != null)
      {
        string innerText = nodeText.InnerText;
        SetFacadeLayout(innerText);
      }
    }

    /// <summary>
    /// Set facade Layout
    /// </summary>
    /// <param name="layout">Layout string name</param>
    internal void SetFacadeLayout(string layout)
    {
      if (string.IsNullOrEmpty(layout))
      {
        return;
      }

      GUIFacadeControl.Layout _l;
      if (Enum.TryParse(layout, out _l))
      {
        Layout = _l;
      }
    }

    /// <summary>
    /// Set facade Type
    /// </summary>
    /// <param name="type">Type string name</param>
    internal void SetFacadeType(string type)
    {
      if (string.IsNullOrEmpty(type))
      {
        return;
      }

      type = type.ToLowerInvariant();
      switch (type)
      {
        case "latests":
          Type = LatestsFacadeType.Latests;
          break;
        case "watched":
          Type = LatestsFacadeType.Watched;
          break;
        case "rated":
          Type = LatestsFacadeType.Rated;
          break;
        case "played":
          Type = LatestsFacadeType.Played;
          break;
        case "mostplayed":
          Type = LatestsFacadeType.MostPlayed;
          break;
      }
    }

    /// <summary>
    /// Set facade SubType
    /// </summary>
    /// <param name="type">SubType string name</param>
    internal void SetFacadeSubType(string type)
    {
      if (string.IsNullOrEmpty(type))
      {
        return;
      }

      type = type.ToLowerInvariant();
      switch (type)
      {
        case "series":
          SubType = LatestsFacadeSubType.Series;
          break;
        case "seasons":
          SubType = LatestsFacadeSubType.Seasons;
          break;
        case "episodes":
          SubType = LatestsFacadeSubType.Episodes;
          break;
      }
    }

    /// <summary>
    /// Set facade ThumbType
    /// </summary>
    /// <param name="type">SubType string name</param>
    internal void SetFacadeThumbType(string type)
    {
      if (string.IsNullOrEmpty(type))
      {
        return;
      }

      type = type.ToLowerInvariant();
      switch (type)
      {
        case "serie":
          ThumbType = LatestsFacadeThumbType.Series;
          break;
        case "season":
          ThumbType = LatestsFacadeThumbType.Seasons;
          break;
        case "episode":
          ThumbType = LatestsFacadeThumbType.Episodes;
          break;
        case "track":
          ThumbType = LatestsFacadeThumbType.Track;
          break;
        case "album":
          ThumbType = LatestsFacadeThumbType.Album;
          break;
        case "artist":
          ThumbType = LatestsFacadeThumbType.Artist;
          break;
      }
    }
  }

  public enum LatestsFacadeType
  {
    Latests,    // Latest added
    Watched,    // Latest Watched
    Rated,      // Highest Rated
    Played,     // Latest Played
    MostPlayed, // Most Played
    Next,       // Next Episodes - Internal
  }

  public enum LatestsFacadeSubType
  {
    Series,     // Latest added series
    Seasons,    // Latest added seasons
    Episodes,   // Latest added episodes
    None,
  }

  public enum LatestsFacadeThumbType
  {
    Series,     // Series
    Seasons,    // Seasons
    Episodes,   // Episodes
    Track,      // Track
    Album,      // Album
    Artist,     // Artist
    None,
  }
}
