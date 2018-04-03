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
      if (!Utils.IsStopping)
      {
        try
        {
          if (LatestMediaHandlerSetup.LMHThreadPriority == Utils.Priority.Lowest)
            Thread.CurrentThread.Priority = ThreadPriority.Lowest;
          else
            Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;

          Thread.CurrentThread.Name = "LatestReorgWorker";
          Utils.AllocateDelayStop("LatestReorgWorker-OnDoWork");

          Utils.SetProperty("#latestMediaHandler.scanned", ((Utils.DelayStopCount > 0) ? "true" : "false"));

          if (Utils.LatestMusic && Utils.RefreshDbMusic)
          {
            Reorganisation(Utils.LatestsCategory.Music);
          }
          if (Utils.LatestPictures && Utils.RefreshDbPicture)
          {
            Reorganisation(Utils.LatestsCategory.Pictures);
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
        LatestMediaHandlerSetup.ReorgTimer.Interval = (Int32.Parse(Utils.ReorgInterval)*60000);
        LatestMediaHandlerSetup.ReorgTimerTick = Environment.TickCount;
        Utils.SyncPointReorg = 0;

        Utils.SetProperty("#latestMediaHandler.scanned", ((Utils.DelayStopCount > 0) ? "true" : "false"));
      }
      catch (Exception ex)
      {
        logger.Error("OnRunWorkerCompleted: " + ex.ToString());
      }
    }

    internal void Reorganisation(Utils.LatestsCategory type)
    {
      if (LatestMediaHandlerSetup.Handlers == null)
      {
        return;
      }

      try
      {
        foreach (object obj in LatestMediaHandlerSetup.Handlers)
        {
          if (obj == null)
          {
            continue;
          }

          if (type == Utils.LatestsCategory.Music && obj is LatestMusicHandler)
          {
            logger.Info("Music Database reorganisation starting.");
            ((LatestMusicHandler)obj).DoScanMusicShares();
            logger.Info("Music Database reorganisation is done.");
            ((LatestMusicHandler)obj).GetLatestMediaInfo(LatestMediaHandlerSetup.Starting);
            return;
          }
          else if (type == Utils.LatestsCategory.Pictures && obj is LatestPictureHandler)
          {
            logger.Info("Picture Database reorganisation starting.");
            ((LatestPictureHandler)obj).RebuildPictureDatabase();
            logger.Info("Picture Database reorganisation is done.");
            ((LatestPictureHandler)obj).GetLatestMediaInfo();
            return;
          }
        }
      }
      catch (Exception ex)
      {
        logger.Error("Reorganisation: " + ex.ToString());
      }
    }
  }
}
