//***********************************************************************
// Assembly         : LatestMediaHandler
// Author           : cul8er
// Created          : 05-09-2010
//
// Last Modified By : ajs
// Last Modified On : 30-09-2015
// Description      : 
//
// Copyright        : Open Source software licensed under the GNU/GPL agreement.
//***********************************************************************
extern alias LMHNLog;

using LMHNLog.NLog;

using System;
using System.Collections;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

namespace LatestMediaHandler
{
  internal class UtilsFanartHandler
  {
    private static Logger logger = LogManager.GetCurrentClassLogger();

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void SetupFanartHandlerSubcribeScaperFinishedEvent()
    {
      try
      {
        FanartHandler.ExternalAccess.ScraperCompleted += new FanartHandler.ExternalAccess.ScraperCompletedHandler(LatestMediaHandlerSetup.TriggerGetLatestMediaInfoOnEvent);
      }
      catch (FileNotFoundException)
      {
        Utils.FanartHandler = false;
        logger.Debug("FanartHandler not found, scraper events disabled.");
      }
      catch (MissingMethodException)
      {
        Utils.FanartHandler = false;
        logger.Debug("Old FanartHandler found, please update.");
      }
      catch 
      { 
        Utils.FanartHandler = false;
      }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void DisposeFanartHandlerSubcribeScaperFinishedEvent()
    {
      try
      {
        FanartHandler.ExternalAccess.ScraperCompleted -= new FanartHandler.ExternalAccess.ScraperCompletedHandler(LatestMediaHandlerSetup.TriggerGetLatestMediaInfoOnEvent);
      }
      catch (FileNotFoundException)
      {
        Utils.FanartHandler = false;
        logger.Debug("FanartHandler not found, scraper events disabled.");
      }
      catch (MissingMethodException)
      {
        Utils.FanartHandler = false;
        logger.Debug("Old FanartHandler found, please update.");
      }
      catch 
      { 
        Utils.FanartHandler = false;
      }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static string GetFanartForLatest(string tvshow)
    {
      if (!Utils.FanartHandler)
      {
        return string.Empty;
      }  

      try
      {
        Hashtable ht = FanartHandler.ExternalAccess.GetTVFanart(tvshow);
        if (ht != null && ht.Count > 0)
        {
          return ht[0].ToString();
        }
      }
      catch (FileNotFoundException) { }
      catch (MissingMethodException) { }
      catch (Exception ex)
      {
        logger.Error("GetFanartForLatest: " + ex.ToString());
      }
      return string.Empty;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static string GetMyVideoFanartForLatest(string title)
    {
      if (!Utils.FanartHandler)
      {
        return string.Empty;
      }  

      try
      {
        return FanartHandler.ExternalAccess.GetMyVideoFanart(title);
      }
      catch (FileNotFoundException) { }
      catch (MissingMethodException) { }
      catch (Exception ex)
      {
        logger.Error("GetMyVideoFanartForLatest: " + ex.ToString());
      }
      return string.Empty;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static Hashtable GetMusicFanartForLatest(string artist, string albumartist, string album)
    {
      if (!Utils.FanartHandler)
      {
        return null;
      }  

      try
      {
        return FanartHandler.ExternalAccess.GetMusicFanartForLatestMedia(artist, albumartist, album);
      }
      catch (FileNotFoundException) { }
      catch (MissingMethodException)
      { 
        logger.Error("GetMusicFanartForLatest: Update Fanart Handler plugin.");
      }
      catch (Exception ex)
      {
        logger.Error("GetMusicFanartForLatest: Possible: Update Fanart Handler plugin.");
        logger.Debug("GetMusicFanartForLatest: New: " + ex.ToString());
      }
      return GetMusicFanartForLatest(albumartist, album);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static Hashtable GetMusicFanartForLatest(string artist, string album = (string) null)
    {
      if (!Utils.FanartHandler)
      {
        return null;
      }  

      try
      {
        return FanartHandler.ExternalAccess.GetMusicFanartForLatestMedia(artist, album);
      }
      catch (FileNotFoundException) { }
      catch (MissingMethodException) { }
      catch (Exception ex)
      {
        logger.Error("GetMusicFanartForLatest: " + ex.ToString());
      }
      return null;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static string GetFHArtistName(string artist)
    {
      if (!Utils.FanartHandler)
      {
        return string.Empty;
      }  

      try
      {
        return FanartHandler.ExternalAccess.GetFHArtistName(artist);
      }
      catch (FileNotFoundException) { }
      catch (MissingMethodException) { }
      catch (Exception ex)
      {
        logger.Error("GetFHArtistName: " + ex.ToString());
      }
      return string.Empty;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ScrapeFanartAndThumb(string artist, string album)
    {
      if (!Utils.FanartHandler)
      {
        return;
      }  

      try
      {
        int i = 0;
        while (!FanartHandler.ExternalAccess.ScrapeFanart(artist, album) && i < 60)
        {
          if ((i % 5) == 0)
            logger.Info("Waiting for fanart and thumb for artist/album ... (" + i + "/60 seconds).");

          Thread.Sleep(1000);
          Utils.ThreadToSleep();
          i++;
        }
      }
      catch (FileNotFoundException) { }
      catch (MissingMethodException) { }
      catch (Exception ex)
      {
        logger.Error("ScrapeFanartAndThumb: " + ex.ToString());
      }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static string GetFanartTVForLatestMedia(string key1, string key2, string key3, Utils.FanartTV category)
    {
      if (!Utils.FanartHandler)
      {
        return string.Empty;
      }  

      try
      {
        return FanartHandler.ExternalAccess.GetFanartTVForLatestMedia(key1, key2, key3, category.ToString());
      }
      catch (FileNotFoundException) { }
      catch (MissingMethodException)
      { 
        logger.Error("GetFanartTVForLatestMedia: Update Fanart Handler plugin.");
      }
      catch (Exception ex)
      {
        logger.Error("GetFanartTVForLatestMedia: Possible: Update Fanart Handler plugin.");
        logger.Debug("GetFanartTVForLatestMedia: " + ex.ToString());
      }
      return string.Empty;
    }

  }
}
