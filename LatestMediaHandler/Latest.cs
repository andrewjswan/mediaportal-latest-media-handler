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

using System;
using System.Globalization;
using System.Linq;

namespace LatestMediaHandler
{
  internal class Latest
  {
    internal string Id { get; set; }
    internal string DBId { get; set; }

    private string dateAdded;
    internal string DateAdded
    {
      get { return dateAdded; }
    }

    private string dateWatched;
    internal string DateWatched
    {
      get { return dateWatched; }
    }

    private string thumb;
    internal string Thumb
    {
      get { return thumb; }
      set 
      {
        if (!string.IsNullOrEmpty(value))
        {
          thumb = value.Replace("/", @"\");
        }
        else
        {
          thumb = value;
        }
      }
    }

    private string thumbSeries;
    internal string ThumbSeries
    {
      get { return thumbSeries; }
      set
      {
        if (!string.IsNullOrEmpty(value))
        {
          this.thumbSeries = value.Replace("/", @"\");
        }
        else
        {
          this.thumbSeries = value;
        }
      }
    }

    private string fanart;
    internal string Fanart
    {
      get { return fanart; }
      set 
      {
        if (!string.IsNullOrEmpty(value))
        {
          fanart = value.Replace("/", @"\");
        }
        else
        {
          fanart = value;
        }
      }
    }

    internal string Title { get; set; }

    internal string Subtitle { get; set; }

    internal string Artist { get; set; }

    internal string Album { get; set; }

    private string genre;
    internal string Genre
    {
      get { return genre; }
      set 
      {
        if (!string.IsNullOrEmpty(value))
        {
          genre = string.Join(", ", value.Split(new string[3] { "|", "/", "," }, StringSplitOptions.RemoveEmptyEntries)
                                         .Where(x => !string.IsNullOrWhiteSpace(x))
                                         .Select(s => s.Trim())
                                         .Select(s => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(s))
                                         .Distinct(StringComparer.CurrentCultureIgnoreCase)
                                         .ToArray());

        }
        else
        {
          genre = value;
        }
      }
    }

    internal string Rating { get; set; }

    internal double DoubleRating
    {
      get
      {
        double r = 0.0;
        if (!string.IsNullOrEmpty(Rating))
        {
          if (!Double.TryParse(Rating, out r))
          {
            Double.TryParse(Rating.Replace(".", ","), out r);
          }
        }
        return r;
      }
      set { Rating = value.ToString(CultureInfo.CurrentCulture); }
    }

    internal int RoundedRating 
    {
      get
      {
        return (int)Math.Round(DoubleRating, MidpointRounding.AwayFromZero);
      }
    }

    internal string Classification { get; set; }

    internal string Runtime { get; set; }

    internal string Year { get; set; }

    internal string UserRating { get; set; }

    internal string SeriesIndex { get; set; }

    internal string SeasonIndex { get; set; }

    internal string EpisodeIndex { get; set; }

    internal string Banner { get; set; }

    internal string ClearArt { get; set; }

    internal string ClearLogo { get; set; }

    internal string AnimatedPoster { get; set; }

    internal string AnimatedBackground { get; set; }

    private DateTime dateTime;
    internal DateTime DateTimeAdded
    {
      get { return dateTime; }
      set 
      { 
        dateTime = value;
        dateAdded = value == DateTime.MinValue ? string.Empty : string.Format("{0:" + Utils.DateFormat + "}", value);
      }
    }

    private DateTime dateTimeWatched;
    internal DateTime DateTimeWatched
    {
      get { return dateTimeWatched; }
      set
      {
        dateTimeWatched = value;
        dateWatched = value == DateTime.MinValue ? string.Empty : string.Format("{0:" + Utils.DateFormat + "}", value);
      }
    }

    internal string CD { get; set; }

    internal string Summary { get; set; }

    internal string Plot
    {
      get { return (string.IsNullOrEmpty(Summary) ? Translation.NoDescription : Summary); }
    }

    internal string PlotOutline
    {
      get { return Utils.GetSentences(Plot, Utils.LatestPlotOutlineSentencesNum); }
    }

    internal string MoviePlotOutline
    {
      get
      {
        if (string.IsNullOrWhiteSpace(Subtitle))
        {
          return Utils.GetSentences(Plot, Utils.LatestPlotOutlineSentencesNum);
        }
        return Subtitle;
      }
    }

    internal object Playable { get; set; }

    internal string Directory { get; set; }

    internal string Studios { get; set; }

    internal bool IsNew { get; set; }

    internal string New
    {
      get { return (IsNew ? "true" : "false"); }
      set { IsNew = value.ToLower().Equals("true"); }
    }

    internal Latest()
    {
      Id = string.Empty;
      Thumb = string.Empty;
      ThumbSeries = string.Empty;
      Fanart = string.Empty;
      Title = string.Empty;
      Subtitle = string.Empty;
      Artist = string.Empty;
      Album = string.Empty;
      Genre = string.Empty;
      Rating = string.Empty;
      UserRating = string.Empty;
      Classification = string.Empty;
      Runtime = string.Empty;
      Year = string.Empty;
      SeriesIndex = string.Empty;
      SeasonIndex = string.Empty;
      EpisodeIndex = string.Empty;
      Playable = string.Empty;
      Summary = string.Empty;
      Directory = string.Empty;
      Banner = string.Empty;
      ClearArt = string.Empty;
      ClearLogo = string.Empty;
      CD = string.Empty;
      AnimatedPoster = string.Empty;
      AnimatedBackground = string.Empty;
      DateTimeAdded = DateTime.MinValue;
      DateTimeWatched = DateTime.MinValue;
      IsNew = false;
    }

  }
}
