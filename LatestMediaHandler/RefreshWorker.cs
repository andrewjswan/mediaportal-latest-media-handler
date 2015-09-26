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
extern alias RealNLog;

using RealNLog.NLog;

using System;
using System.ComponentModel;
using System.Threading;

namespace LatestMediaHandler
{

  internal class RefreshWorker : BackgroundWorker
  {
    #region declarations

    private static Logger logger = LogManager.GetCurrentClassLogger();

    #endregion

    public RefreshWorker()
    {
      WorkerReportsProgress = true;
      WorkerSupportsCancellation = true;
    }

    protected override void OnDoWork(DoWorkEventArgs e)
    {
      if (Utils.GetIsStopping() == false)
      {
        try
        {
          if (LatestMediaHandlerSetup.LMHThreadPriority.Equals("Lowest", StringComparison.CurrentCulture))
            Thread.CurrentThread.Priority = ThreadPriority.Lowest;
          else
            Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;

          Thread.CurrentThread.Name = "RefreshWorker";
          Utils.AllocateDelayStop("RefreshWorker-OnDoWork");

          logger.Debug("RefreshWorker: "+e.Argument.ToString());

          if (e.Argument is LatestMusicHandler)
          {
            LatestMediaHandlerSetup.Lmh.GetLatestMediaInfo(LatestMediaHandlerSetup.Starting);
          }
          else if (e.Argument is LatestPictureHandler)
          {
            LatestMediaHandlerSetup.Lph.GetLatestMediaInfo();
          }
          else if (e.Argument is LatestMovingPicturesHandler)
          {
            LatestMediaHandlerSetup.Lmph.MovingPictureUpdateLatest();
            // LatestMediaHandlerSetup.Lmph.SetupMovingPicturesLatest();
          }
          else if (e.Argument is LatestTVSeriesHandler)
          {
            /*
            if (LatestMediaHandlerSetup.Ltvsh.CurrentType == LatestTVSeriesHandler.Types.Latest)
            {
              LatestMediaHandlerSetup.Ltvsh.TVSeriesUpdateLatest(LatestTVSeriesHandler.Types.Latest, LatestMediaHandlerSetup.LatestTVSeriesWatched.Equals("True", StringComparison.CurrentCulture));
            }
            */
            LatestMediaHandlerSetup.Ltvsh.TVSeriesUpdateLatest(LatestMediaHandlerSetup.Ltvsh.CurrentType, LatestMediaHandlerSetup.LatestTVSeriesWatched.Equals("True", StringComparison.CurrentCulture));
            LatestMediaHandlerSetup.Ltvsh.ChangedEpisodeCount();
            // LatestMediaHandlerSetup.Ltvsh.SetupTVSeriesLatest();
          }
          else if (e.Argument is LatestMyFilmsHandler)
          {
            LatestMediaHandlerSetup.Lmfh.MyFilmsUpdateLatest();
            // LatestMediaHandlerSetup.Lmfh.SetupMovieLatest();
          }
          else if (e.Argument is LatestMyVideosHandler)
          {
            LatestMediaHandlerSetup.Lmvh.MyVideosUpdateLatest();
          }
          else if (e.Argument is LatestMvCentralHandler)
          {
            LatestMediaHandlerSetup.Lmch.GetLatestMediaInfo(LatestMediaHandlerSetup.Starting);
          }
        }
        catch (Exception ex)
        {
          Utils.ReleaseDelayStop("RefreshWorker-OnDoWork");
          logger.Error("OnDoWork: " + ex.ToString());
        }
      }
    }

    internal void OnRunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
    {
      try
      {
        Utils.ReleaseDelayStop("RefreshWorker-OnDoWork");
      }
      catch (Exception ex)
      {
        logger.Error("OnRunWorkerCompleted: " + ex.ToString());
      }
    }

  }
}
