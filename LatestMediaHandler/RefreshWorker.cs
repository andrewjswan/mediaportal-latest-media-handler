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
extern alias LMHNLog;

using MediaPortal.GUI.Library;

using LMHNLog.NLog;

using System;
using System.ComponentModel;
using System.Threading;

namespace LatestMediaHandler
{

  internal class RefreshWorker : BackgroundWorker
  {
    #region declarations

    private static Logger logger = LogManager.GetCurrentClassLogger();
    private Object Argument { get; set;}
    private string WorkerName = "RefreshWorker";

    #endregion

    public RefreshWorker()
    {
      WorkerReportsProgress = false;
      WorkerSupportsCancellation = true;
    }

    protected override void OnDoWork(DoWorkEventArgs e)
    {
      if (!Utils.IsStopping)
      {
        // if (Utils.ActiveWindow == (int)GUIWindow.Window.WINDOW_INVALID)
        //   return;
        
        if (Utils.ActiveWindow != (int)GUIWindow.Window.WINDOW_SECOND_HOME)
        {
          Thread.Sleep(Utils.ScanDelay);
        }
        
        try
        {
          if (LatestMediaHandlerSetup.LMHThreadPriority == Utils.Priority.Lowest)
          {
            Thread.CurrentThread.Priority = ThreadPriority.Lowest;
          }
          else
          {
            Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
          }

          Argument = e.Argument;
          GetWorkerName();

          Thread.CurrentThread.Name = WorkerName;
          Utils.AllocateDelayStop(WorkerName);
          logger.Debug("RefreshWorker: Start: " + WorkerName);

          Utils.SetProperty("#latestMediaHandler.scanned", ((Utils.DelayStopCount > 0) ? "true" : "false"));
          Utils.ThreadToSleep();

          if (Argument is LatestMusicHandler)
          {
            ((LatestMusicHandler)Argument).GetLatestMediaInfo(LatestMediaHandlerSetup.Starting);
          }
          else if (Argument is LatestPictureHandler)
          {
            ((LatestPictureHandler)Argument).GetLatestMediaInfo();
          }
          else if (Argument is LatestMovingPicturesHandler)
          {
            ((LatestMovingPicturesHandler)Argument).MovingPictureUpdateLatest();
          }
          else if (Argument is LatestTVSeriesHandler)
          {
            ((LatestTVSeriesHandler)Argument).TVSeriesUpdateLatest();
            ((LatestTVSeriesHandler)Argument).ChangedEpisodeCount();
          }
          else if (Argument is LatestMyFilmsHandler)
          {
            ((LatestMyFilmsHandler)Argument).MyFilmsUpdateLatest();
          }
          else if (Argument is LatestMyVideosHandler)
          {
            ((LatestMyVideosHandler)Argument).MyVideosUpdateLatest();
          }
          else if (Argument is LatestMvCentralHandler)
          {
            ((LatestMvCentralHandler)Argument).GetLatestMediaInfo(LatestMediaHandlerSetup.Starting);
          }
        }
        catch (Exception ex)
        {
          logger.Error("OnDoWork: " + ex.ToString());
        }
      }
    }

    internal void OnRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
    {
      try
      {
        Utils.ReleaseDelayStop(WorkerName);
        logger.Debug("RefreshWorker: Complete: " + WorkerName);
        Utils.ThreadToSleep();

        Utils.SetProperty("#latestMediaHandler.scanned", ((Utils.DelayStopCount > 0) ? "true" : "false"));
      }
      catch (Exception ex)
      {
        logger.Error("OnRunWorkerCompleted: [" + Argument.ToString() + "][" + WorkerName + "]" + ex.ToString());
      }
    }

    private void GetWorkerName()
    {
      if (Argument == null)
      {
        return;
      }

      WorkerName = WorkerName + "." + Argument.ToString().Trim();
      if (Argument is LatestMusicHandler)
      {
        if (!((LatestMusicHandler)Argument).MainFacade)
        {
          WorkerName = WorkerName + "." + ((LatestMusicHandler)Argument).CurrentFacade.ControlID;
        }
      }
      else if (Argument is LatestPictureHandler)
      {
        if (!((LatestPictureHandler)Argument).MainFacade)
        {
          WorkerName = WorkerName + "." + ((LatestPictureHandler)Argument).CurrentFacade.ControlID;
        }
      }
      else if (Argument is LatestMovingPicturesHandler)
      {
        if (!((LatestMovingPicturesHandler)Argument).MainFacade)
        {
          WorkerName = WorkerName + "." + ((LatestMovingPicturesHandler)Argument).CurrentFacade.ControlID;
        }
      }
      else if (Argument is LatestTVSeriesHandler)
      {
        if (!((LatestTVSeriesHandler)Argument).MainFacade)
        {
          WorkerName = WorkerName + "." + ((LatestTVSeriesHandler)Argument).CurrentFacade.ControlID;
        }
      }
      else if (Argument is LatestMyFilmsHandler)
      {
        if (!((LatestMyFilmsHandler)Argument).MainFacade)
        {
          WorkerName = WorkerName + "." + ((LatestMyFilmsHandler)Argument).CurrentFacade.ControlID;
        }
      }
      else if (Argument is LatestMyVideosHandler)
      {
        if (!((LatestMyVideosHandler)Argument).MainFacade)
        {
          WorkerName = WorkerName + "." + ((LatestMyVideosHandler)Argument).CurrentFacade.ControlID;
        }
      }
      else if (Argument is LatestMvCentralHandler)
      {
        if (!((LatestMvCentralHandler)Argument).MainFacade)
        {
          WorkerName = WorkerName + "." + ((LatestMvCentralHandler)Argument).CurrentFacade.ControlID;
        }
      }
    }
  }
}
