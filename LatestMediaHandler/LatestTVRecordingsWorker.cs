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

using System;
using System.ComponentModel;
using System.Threading;

using RealNLog.NLog;

namespace LatestMediaHandler
{
  internal class LatestTVRecordingsWorker : BackgroundWorker
  {
    #region declarations

    private static Logger logger = LogManager.GetCurrentClassLogger();

    #endregion

    public LatestTVRecordingsWorker()
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

          Thread.CurrentThread.Name = "LatestTVRecordingsWorker";
          Utils.AllocateDelayStop("LatestTVRecordingsWorker-OnDoWork");
          logger.Info("Refreshing latest TV Recordings is starting.");

          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.scanned", ((Utils.DelayStopCount > 0) ? "true" : "false"));

          int arg = (int) e.Argument;
          if (arg == 0)
            LatestMediaHandlerSetup.Ltvrh.UpdateLatestMediaInfo();
          else
            LatestMediaHandlerSetup.Ltvrh.UpdateActiveRecordings();
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
        logger.Info("Refreshing latest TV Recordings is done.");
        Utils.ReleaseDelayStop("LatestTVRecordingsWorker-OnDoWork");
        Utils.SyncPointTVRecordings = 0;

        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.scanned", ((Utils.DelayStopCount > 0) ? "true" : "false"));
      }
      catch (Exception ex)
      {
        logger.Error("OnRunWorkerCompleted: " + ex.ToString());
      }
    }
  }
}
