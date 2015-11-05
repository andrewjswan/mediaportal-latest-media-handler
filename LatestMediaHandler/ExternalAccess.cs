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

extern alias RealNLog;

using System;

using RealNLog.NLog;

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
  }
}
