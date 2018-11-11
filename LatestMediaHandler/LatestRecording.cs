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

namespace LatestMediaHandler
{
  internal class LatestRecording
  {
    private string title;
    private string genre;
    private DateTime startDateTime;
    private string startTime;
    private string startDate;
    private string endTime;
    private string endDate;
    private string channel;
    private string channelLogo;

    internal string Title
    {
      get { return title; }
      set { title = value; }
    }

    internal string Genre
    {
      get { return genre; }
      set { genre = value; }
    }

    internal DateTime StartDateTime
    {
      get { return startDateTime; }
      set { startDateTime = value; }
    }

    internal string StartTime
    {
      get { return startTime; }
      set { startTime = value; }
    }

    internal string StartDate
    {
      get { return startDate; }
      set { startDate = value; }
    }

    internal string EndTime
    {
      get { return endTime; }
      set { endTime = value; }
    }

    internal string EndDate
    {
      get { return endDate; }
      set { endDate = value; }
    }

    internal string Channel
    {
      get { return channel; }
      set { channel = value; }
    }

    internal string ChannelLogo
    {
      get { return channelLogo; }
      set { channelLogo = value; }
    }

    internal LatestRecording(string title, string genre, DateTime startDateTime, string startDate, string startTime,
                             string endDate, string endTime, string channel, string channelLogo)
    {
      this.title = title;
      if (!string.IsNullOrEmpty(genre))
      {
        this.genre = genre.Replace("|", ",");
      }
      else
      {
        this.genre = genre;
      }
      this.startDateTime = startDateTime;
      this.startTime = startTime;
      this.startDate = startDate;
      this.endTime = endTime;
      this.endDate = endDate;
      this.channel = channel;
      this.channelLogo = channelLogo;
    }
  }
}
