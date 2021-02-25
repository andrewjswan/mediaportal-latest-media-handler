//***********************************************************************
// Assembly         : LatestMediaHandler
// Author           : cul8er
// Created          : 05-09-2010
//
// Last Modified By : ajs
// Last Modified On : 16-02-2021
// Description      : 
//
// Copyright        : Open Source software licensed under the GNU/GPL agreement.
//***********************************************************************

using System;

namespace LatestMediaHandler
{

  #region MQTT Item

  public class MQTTItem
  {
    public string AirDate { get; set; }
    public string Title { get; set; }
    public string Release { get; set; }
    public string Episode { get; set; }
    public string Number { get; set; }
    public string Genres { get; set; }
    public double Rating { get; set; }
    public string Studio { get; set; }
    public string Aired { get; set; }
    public string Runtime { get; set; }
    public string Poster { get; set; }
    public string Fanart { get; set; }
    public bool Flag { get; set; }
    public string Id { get; set; }
    public string Filename { get; set; }

    public MQTTItem()
    {
    }

    internal MQTTItem(Latest item)
    {
      this.Id = item.DBId;
      this.Title = item.Title + (!string.IsNullOrEmpty(item.Year) ? " (" + item.Year + ")" : string.Empty);
      this.AirDate = item.DateTimeAdded.ToString("yyyy-MM-ddTHH:mm:ssZ");
      this.Release = "$day, $date $time";
      this.Episode = item.Subtitle;
      this.Number = (!string.IsNullOrEmpty(item.SeasonIndex + item.EpisodeIndex) ? string.Format("S{0}E{1}", item.SeasonIndex, item.EpisodeIndex) : string.Empty);
      this.Genres = item.Genre.Trim(',') ?? string.Empty;
      this.Rating = item.DoubleRating;
      this.Studio = item.Studios;
      this.Aired = string.Empty;
      this.Runtime = item.Runtime;
      this.Poster = item.Thumb;
      this.Fanart = item.Fanart;
      this.Flag = item.IsNew;
      this.Filename = string.Empty;
    }
  }

  #endregion
}
