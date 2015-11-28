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

using MediaPortal.GUI.Library;

namespace LatestMediaHandler
{

  internal class RefreshWorker : BackgroundWorker
  {
    #region declarations

    private static Logger logger = LogManager.GetCurrentClassLogger();
    private Object Argument { get; set;}

    #endregion

    public RefreshWorker()
    {
      WorkerReportsProgress = false; // true
      WorkerSupportsCancellation = true;
    }

    protected override void OnDoWork(DoWorkEventArgs e)
    {
      if (Utils.GetIsStopping() == false)
      {
        // if (GUIWindowManager.ActiveWindow == (int)GUIWindow.Window.WINDOW_INVALID)
        //   return;
        
        if (GUIWindowManager.ActiveWindow != (int)GUIWindow.Window.WINDOW_SECOND_HOME)
        {
          Thread.Sleep(Utils.scanDelay);
        }
        
        try
        {
          if (LatestMediaHandlerSetup.LMHThreadPriority.Equals("Lowest", StringComparison.CurrentCulture))
            Thread.CurrentThread.Priority = ThreadPriority.Lowest;
          else
            Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;

          Argument = e.Argument;
          Thread.CurrentThread.Name = "RefreshWorker-"+Argument.ToString().Trim();
          Utils.AllocateDelayStop("RefreshWorker-OnDoWork-"+Argument.ToString().Trim());
          logger.Debug("RefreshWorker: Start: "+Argument.ToString());

          Utils.SetProperty("#latestMediaHandler.scanned", ((Utils.DelayStopCount > 0) ? "true" : "false"));
          Utils.ThreadToSleep();

          if (Argument is LatestMusicHandler)
          {
            LatestMediaHandlerSetup.Lmh.GetLatestMediaInfo(LatestMediaHandlerSetup.Starting);
          }
          else if (Argument is LatestPictureHandler)
          {
            LatestMediaHandlerSetup.Lph.GetLatestMediaInfo();
          }
          else if (Argument is LatestMovingPicturesHandler)
          {
            LatestMediaHandlerSetup.Lmph.MovingPictureUpdateLatest();
          }
          else if (Argument is LatestTVSeriesHandler)
          {
            LatestMediaHandlerSetup.Ltvsh.TVSeriesUpdateLatest(LatestMediaHandlerSetup.Ltvsh.CurrentType, LatestMediaHandlerSetup.LatestTVSeriesWatched.Equals("True", StringComparison.CurrentCulture));
            LatestMediaHandlerSetup.Ltvsh.ChangedEpisodeCount();
          }
          else if (Argument is LatestMyFilmsHandler)
          {
            LatestMediaHandlerSetup.Lmfh.MyFilmsUpdateLatest();
          }
          else if (Argument is LatestMyVideosHandler)
          {
            LatestMediaHandlerSetup.Lmvh.MyVideosUpdateLatest();
          }
          else if (Argument is LatestMvCentralHandler)
          {
            LatestMediaHandlerSetup.Lmch.GetLatestMediaInfo(LatestMediaHandlerSetup.Starting);
          }
        }
        catch (Exception ex)
        {
          logger.Error("OnDoWork: " + ex.ToString());
        }
      }
    }

    internal void OnRunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
    {
      try
      {
        Utils.ReleaseDelayStop("RefreshWorker-OnDoWork-"+Argument.ToString().Trim());
        logger.Debug("RefreshWorker: Complete: "+Argument.ToString());
        Utils.ThreadToSleep();

        Utils.SetProperty("#latestMediaHandler.scanned", ((Utils.DelayStopCount > 0) ? "true" : "false"));
      }
      catch (Exception ex)
      {
        logger.Error("OnRunWorkerCompleted: ["+Argument.ToString()+"]" + ex.ToString());
      }
    }
  }
}
