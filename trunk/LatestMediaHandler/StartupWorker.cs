//***********************************************************************
// Assembly         : LatestMediaHandler
// Author           : cul8er
// Created          : 05-09-2010
//
// Last Modified By : cul8er
// Last Modified On : 10-05-2010
// Description      : 
//
// Copyright        : Open Source software licensed under the GNU/GPL agreement.
//***********************************************************************

namespace LatestMediaHandler
{
  extern alias RealNLog;
  using RealNLog.NLog;
  using System;
  using System.ComponentModel;
  using System.Threading;


    internal class StartupWorker : BackgroundWorker
  {
    #region declarations

    private static Logger logger = LogManager.GetCurrentClassLogger();

    #endregion

    public StartupWorker()
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
          if (LatestMediaHandlerSetup.FHThreadPriority.Equals("Lowest", StringComparison.CurrentCulture))
          {
            Thread.CurrentThread.Priority = ThreadPriority.Lowest;
          }
          else
          {
            Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
          }
          Thread.CurrentThread.Name = "StartupWorker";
          Utils.AllocateDelayStop("StartupWorker-OnDoWork");

          if (e.Argument is LatestMusicHandler)
          {
            LatestMediaHandlerSetup.Lmh.GetLatestMediaInfo(true);
          }
          else if (e.Argument is LatestPictureHandler)
          {
            LatestMediaHandlerSetup.Lph.GetLatestMediaInfo();
          }
          else if (e.Argument is LatestMovingPicturesHandler)
          {
            LatestMediaHandlerSetup.Lmph.MovingPictureUpdateLatest();
            LatestMediaHandlerSetup.Lmph.SetupMovingPicturesLatest();
          }
          else if (e.Argument is LatestTVSeriesHandler)
          {
            if (LatestMediaHandlerSetup.Ltvsh.CurrentType == LatestTVSeriesHandler.Types.Latest)
            {
              if (LatestMediaHandlerSetup.LatestTVSeriesWatched.Equals("True"))
              {
                LatestMediaHandlerSetup.Ltvsh.TVSeriesUpdateLatest(LatestTVSeriesHandler.Types.Latest, true);
              }
              else
              {
                LatestMediaHandlerSetup.Ltvsh.TVSeriesUpdateLatest(LatestTVSeriesHandler.Types.Latest, false);
              }
            }
            LatestMediaHandlerSetup.Ltvsh.ChangedEpisodeCount();
            LatestMediaHandlerSetup.Ltvsh.SetupTVSeriesLatest();
          }
          else if (e.Argument is LatestMyFilmsHandler)
          {
            LatestMediaHandlerSetup.Lmfh.MyFilmsUpdateLatest();
            LatestMediaHandlerSetup.Lmfh.SetupMovieLatest();
          }
          else if (e.Argument is LatestMyVideosHandler)
          {
            LatestMediaHandlerSetup.Lmvh.MyVideosUpdateLatest();
          }
          else if (e.Argument is LatestMvCentralHandler)
          {
            LatestMediaHandlerSetup.Lmch.GetLatestMediaInfo(true);
          }



        }
        catch (Exception ex)
        {
          Utils.ReleaseDelayStop("StartupWorker-OnDoWork");
          logger.Error("OnDoWork: " + ex.ToString());
        }
      }

    }

    internal void OnRunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
    {
      try
      {

        Utils.ReleaseDelayStop("StartupWorker-OnDoWork");
      }
      catch (Exception ex)
      {
        logger.Error("OnRunWorkerCompleted: " + ex.ToString());
      }
    }

  }
}
