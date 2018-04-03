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

namespace LatestMediaHandler
{
  public class ExternalAccess
  {
    #region declarations

    private static Logger logger = LogManager.GetCurrentClassLogger();

    #endregion

    public static bool UserHasEnabledMyFilmsSupport()
    {
      return Utils.LatestMyFilms;
    }

    public static bool UserHasEnabledMvCentralSupport()
    {
      return Utils.LatestMvCentral;
    }

    public static bool UserHasEnabledMyVideosSupport()
    {
      return Utils.LatestMyVideos;
    }

    public static bool UserHasEnabledMovingPictureSupport()
    {
      return Utils.LatestMovingPictures;
    }

    public static bool UserHasEnabledMusicSupport()
    {
      return Utils.LatestMusic;
    }

    public static bool UserHasEnabledPictureSupport()
    {
      return Utils.LatestPictures;
    }

    public static bool UserHasEnabledTVSeriesSupport()
    {
      return Utils.LatestTVSeries;
    }

    public static bool UserHasEnabledTVRecordingsSupport()
    {
      return Utils.LatestTVRecordings;
    }

    #region From FanartHandler

    public static DateTime GetLatestsUpdate(string category)
    {
      Utils.LatestsCategory latestsCategory;
      if (Enum.TryParse(category, out latestsCategory))
      {
        if (Enum.IsDefined(typeof(Utils.LatestsCategory), latestsCategory))  
        {
          return Utils.GetLatestsUpdate(latestsCategory);
        } 
      }
      return new DateTime();
    }

    public static Hashtable GetLatests(string category)
    {
      Hashtable ht = new Hashtable();
      if (string.IsNullOrEmpty(category))
        return ht ;

      Utils.LatestsCategory latestsCategory;
      if (!Enum.TryParse(category, out latestsCategory))
        return ht ;
      if (!Enum.IsDefined(typeof(Utils.LatestsCategory), latestsCategory))  
        return ht ;

      return GetLatestsList(latestsCategory);
    }


    private static Hashtable GetLatestsList(Utils.LatestsCategory type)
    {
      if (LatestMediaHandlerSetup.Handlers == null)
      {
        return new Hashtable();
      }

      try
      {
        foreach (object obj in LatestMediaHandlerSetup.Handlers)
        {
          if (obj == null)
          {
            continue;
          }

          if (type == Utils.LatestsCategory.MovingPictures && obj is LatestMovingPicturesHandler)
          {
            return ((LatestMovingPicturesHandler)obj).GetLatestsList();
          }
          else if (type == Utils.LatestsCategory.Movies && obj is LatestMyVideosHandler)
          {
            return ((LatestMyVideosHandler)obj).GetLatestsList();
          }
          else if (type == Utils.LatestsCategory.MvCentral && obj is LatestMvCentralHandler)
          {
            return ((LatestMvCentralHandler)obj).GetLatestsList();
          }
          else if (type == Utils.LatestsCategory.TVSeries && obj is LatestTVSeriesHandler)
          {
            return ((LatestTVSeriesHandler)obj).GetLatestsList();
          }
          else if (type == Utils.LatestsCategory.Music && obj is LatestMusicHandler)
          {
            return ((LatestMusicHandler)obj).GetLatestsList();
          }
          /*
          else if (type == Utils.LatestsCategory.TV && obj is LatestTVAllRecordingsHandler)
          {
            return ((LatestTVAllRecordingsHandler)obj).GetLatestsList();
          }
          else if (type == Utils.LatestsCategory.Pictures && obj is LatestPictureHandler)
          {
            return ((LatestPictureHandler)obj).GetLatestsList();
          }
          */
          else if (type == Utils.LatestsCategory.MyFilms && obj is LatestMyFilmsHandler)
          {
            return ((LatestMyFilmsHandler)obj).GetLatestsList();
          }
        }
      }
      catch (Exception ex)
      {
        logger.Error("GetLatestsList: " + ex.ToString());
      }
      return new Hashtable();
    }
    #endregion
  }
}
