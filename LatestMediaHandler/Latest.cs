//***********************************************************************
// Assembly         : LatestMediaHandler
// Author           : cul8er
// Created          : 05-09-2010
//
// Last Modified By : ajs
// Last Modified On : 24-09-2015
// Description      : 
//
// Copyright        : Open Source software licensed under the GNU/GPL agreement.
//***********************************************************************

namespace LatestMediaHandler
{
  internal class Latest
  {
    private string dateAdded;
    private string thumb;
    private string fanart;
    private string title;
    private string subtitle;
    private string artist;
    private string album;
    private string genre;
    private string rating;
    private string roundedRating;
    private string classification;
    private string runtime;
    private string year;
    private string seriesIndex;
    private string seasonIndex;
    private string episodeIndex;
    private string thumbSeries;
    private object playable;
    //string fanart1;        
    //string fanart2;
    private string id;
    private string summary;
    private bool isnew;

    internal string Summary
    {
      get { return summary; }
      set { summary = value; }
    }

    internal string Id
    {
      get { return id; }
      set { id = value; }
    }

    internal object Playable
    {
      get { return playable; }
      set { playable = value; }
    }

    internal string ThumbSeries
    {
      get { return thumbSeries; }
      set { thumbSeries = value; }
    }

    internal string DateAdded
    {
      get { return dateAdded; }
      set { dateAdded = value; }
    }

    internal string Thumb
    {
      get { return thumb; }
      set { thumb = value; }
    }

    internal string Fanart
    {
      get { return fanart; }
      set { fanart = value; }
    }

    internal string Title
    {
      get { return title; }
      set { title = value; }
    }

    internal string Subtitle
    {
      get { return subtitle; }
      set { subtitle = value; }
    }

    internal string Artist
    {
      get { return artist; }
      set { artist = value; }
    }

    internal string Album
    {
      get { return album; }
      set { album = value; }
    }

    internal string Genre
    {
      get { return genre; }
      set { genre = value; }
    }

    internal string Rating
    {
      get { return rating; }
      set { rating = value; }
    }

    internal string RoundedRating
    {
      get { return roundedRating; }
      set { roundedRating = value; }
    }

    internal string Classification
    {
      get { return classification; }
      set { classification = value; }
    }

    internal string Runtime
    {
      get { return runtime; }
      set { runtime = value; }
    }

    internal string Year
    {
      get { return year; }
      set { year = value; }
    }

    internal string SeriesIndex
    {
      get { return seriesIndex; }
      set { seriesIndex = value; }
    }

    internal string SeasonIndex
    {
      get { return seasonIndex; }
      set { seasonIndex = value; }
    }

    internal string EpisodeIndex
    {
      get { return episodeIndex; }
      set { episodeIndex = value; }
    }

    internal string New
    {
      get { return (isnew ? "true" : "false"); }
      set { isnew = value.ToLower().Equals("true"); }
    }

    internal Latest(string dateAdded, string thumb, string fanart, string title, string subtitle, string artist,
      string album, string genre, string rating, string roundedRating, string classification, string runtime,
      string year, string seasonIndex, string episodeIndex, string thumbSeries, object playable, string id,
      string summary, string seriesIndex, bool isnew = false)
    {
      this.dateAdded = dateAdded;
      this.thumb = thumb;
      this.fanart = fanart;
      this.title = title;
      this.subtitle = subtitle;
      this.artist = artist;
      this.album = album;
      if (genre != null && genre.Length > 0)
      {
        this.genre = genre.Replace("|", ",");
      }
      else
      {
        this.genre = genre;
      }
      this.rating = rating;
      this.roundedRating = roundedRating;
      this.classification = classification;
      this.runtime = runtime;
      this.year = year;
      this.seriesIndex = seriesIndex;
      this.seasonIndex = seasonIndex;
      this.episodeIndex = episodeIndex;
      this.thumbSeries = thumbSeries;
      this.playable = playable;
      //this.fanart1 = fanart1;
      //this.fanart2 = fanart2;
      this.id = id;
      this.summary = summary;
      this.isnew = isnew;
    }

  }
}
