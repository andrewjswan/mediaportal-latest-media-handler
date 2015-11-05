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

using RealNLog.NLog;

using System;
using System.ComponentModel;
using System.Threading;

namespace LatestMediaHandler
{
  internal class LatestReorgWorker : BackgroundWorker
  {
    #region declarations

    private static Logger logger = LogManager.GetCurrentClassLogger();

    #endregion

    public LatestReorgWorker()
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

          Thread.CurrentThread.Name = "LatestReorgWorker";
          Utils.AllocateDelayStop("LatestReorgWorker-OnDoWork");

          Utils.SetProperty("#latestMediaHandler.scanned", ((Utils.DelayStopCount > 0) ? "true" : "false"));

          if (LatestMediaHandlerSetup.LatestMusic.Equals("True", StringComparison.CurrentCulture) &&
              LatestMediaHandlerSetup.RefreshDbMusic.Equals("True", StringComparison.CurrentCulture))
          {
            try
            {
              logger.Info("Music Database reorganisation starting.");
              LatestMediaHandlerSetup.Lmh.DoScanMusicShares();
              logger.Info("Music Database reorganisation is done.");
              LatestMediaHandlerSetup.Lmh.GetLatestMediaInfo(LatestMediaHandlerSetup.Starting);
            }
            catch
            {   }
          }
          if (LatestMediaHandlerSetup.LatestPictures.Equals("True", StringComparison.CurrentCulture) &&
              LatestMediaHandlerSetup.RefreshDbPicture.Equals("True", StringComparison.CurrentCulture))
          {
            try
            {
              logger.Info("Picture Database reorganisation starting.");
              LatestMediaHandlerSetup.Lph.RebuildPictureDatabase();
              logger.Info("Picture Database reorganisation is done.");
              LatestMediaHandlerSetup.Lph.GetLatestMediaInfo();
            }
            catch
            {   }
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
        Utils.ReleaseDelayStop("LatestReorgWorker-OnDoWork");
        LatestMediaHandlerSetup.ReorgTimer.Interval = (Int32.Parse(LatestMediaHandlerSetup.ReorgInterval)*60000);
        LatestMediaHandlerSetup.ReorgTimerTick = Environment.TickCount;
        Utils.SyncPointReorg = 0;

        Utils.SetProperty("#latestMediaHandler.scanned", ((Utils.DelayStopCount > 0) ? "true" : "false"));
      }
      catch (Exception ex)
      {
        logger.Error("OnRunWorkerCompleted: " + ex.ToString());
      }
    }

  }
}
