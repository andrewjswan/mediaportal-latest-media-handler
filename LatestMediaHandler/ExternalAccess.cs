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
      bool _return = false;
      try
      {
        if (LatestMediaHandlerSetup.LatestMyFilms.Equals("True", StringComparison.CurrentCulture))
        {
          _return = true;
        }
      }
      catch (Exception ex)
      {
        logger.Error("UserHasEnabledMyFilmsSupport: " + ex.ToString());
      }
      return _return;

    }

    public static bool UserHasEnabledMvCentralSupport()
    {
      bool _return = false;
      try
      {
        if (LatestMediaHandlerSetup.LatestMvCentral.Equals("True", StringComparison.CurrentCulture))
        {
          _return = true;
        }
      }
      catch (Exception ex)
      {
        logger.Error("UserHasEnabledMvCentralSupport: " + ex.ToString());
      }
      return _return;

    }

    public static bool UserHasEnabledMyVideosSupport()
    {
      bool _return = false;
      try
      {
        if (LatestMediaHandlerSetup.LatestMyVideos.Equals("True", StringComparison.CurrentCulture))
        {
          _return = true;
        }
      }
      catch (Exception ex)
      {
        logger.Error("UserHasEnabledMyVideosSupport: " + ex.ToString());
      }
      return _return;

    }

    public static bool UserHasEnabledMovingPictureSupport()
    {
      bool _return = false;
      try
      {
        if (LatestMediaHandlerSetup.LatestMovingPictures.Equals("True", StringComparison.CurrentCulture))
        {
          _return = true;
        }
      }
      catch (Exception ex)
      {
        logger.Error("UserHasEnabledMovingPictureSupport: " + ex.ToString());
      }
      return _return;

    }

    public static bool UserHasEnabledMusicSupport()
    {
      bool _return = false;
      try
      {
        if (LatestMediaHandlerSetup.LatestMusic.Equals("True", StringComparison.CurrentCulture))
        {
          _return = true;
        }
      }
      catch (Exception ex)
      {
        logger.Error("UserHasEnabledMusicSupport: " + ex.ToString());
      }
      return _return;

    }

    public static bool UserHasEnabledPictureSupport()
    {
      bool _return = false;
      try
      {
        if (LatestMediaHandlerSetup.LatestPictures.Equals("True", StringComparison.CurrentCulture))
        {
          _return = true;
        }
      }
      catch (Exception ex)
      {
        logger.Error("UserHasEnabledPictureSupport: " + ex.ToString());
      }
      return _return;

    }

    public static bool UserHasEnabledTVSeriesSupport()
    {
      bool _return = false;
      try
      {
        if (LatestMediaHandlerSetup.LatestTVSeries.Equals("True", StringComparison.CurrentCulture))
        {
          _return = true;
        }
      }
      catch (Exception ex)
      {
        logger.Error("UserHasEnabledTVSeriesSupport: " + ex.ToString());
      }
      return _return;
    }

    public static bool UserHasEnabledTVRecordingsSupport()
    {
      bool _return = false;
      try
      {
        if (LatestMediaHandlerSetup.LatestTVRecordings.Equals("True", StringComparison.CurrentCulture))
        {
          _return = true;
        }
      }
      catch (Exception ex)
      {
        logger.Error("UserHasEnabledTVRecordingsSupport: " + ex.ToString());
      }
      return _return;
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

      if (latestsCategory == Utils.LatestsCategory.Music && LatestMediaHandlerSetup.Lmh != null)
      {
        return LatestMediaHandlerSetup.Lmh.GetLatestsList();
      }
      
      if (latestsCategory == Utils.LatestsCategory.MvCentral && LatestMediaHandlerSetup.Lmch != null)
      {
        return LatestMediaHandlerSetup.Lmch.GetLatestsList();
      }

      if (latestsCategory == Utils.LatestsCategory.Movies && LatestMediaHandlerSetup.Lmvh != null)
      {
        return LatestMediaHandlerSetup.Lmvh.GetLatestsList();
      }

      if (latestsCategory == Utils.LatestsCategory.MovingPictures && LatestMediaHandlerSetup.Lmph != null)
      {
        return LatestMediaHandlerSetup.Lmph.GetLatestsList();
      }

      if (latestsCategory == Utils.LatestsCategory.MyFilms && LatestMediaHandlerSetup.Lmfh != null)
      {
        return LatestMediaHandlerSetup.Lmfh.GetLatestsList();
      }

      if (latestsCategory == Utils.LatestsCategory.TVSeries && LatestMediaHandlerSetup.Ltvsh != null)
      {
        return LatestMediaHandlerSetup.Ltvsh.GetLatestsList();
      }
      return ht;
    }
    #endregion
  }
}
