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
          logger.Info("Database reorg is starting.");
          if (LatestMediaHandlerSetup.LatestMusic.Equals("True", StringComparison.CurrentCulture) &&
              LatestMediaHandlerSetup.RefreshDbMusic.Equals("True", StringComparison.CurrentCulture))
          {
            try
            {
              //LatestMediaHandlerSetup.Lmh = new LatestMusicHandler();
//                            lmh.InitDB();
              LatestMediaHandlerSetup.Lmh.DoScanMusicShares();
              //lmh.GetLatestMediaInfo();
              //lmh = null;
            }
            catch
            {
            }
          }
          if (LatestMediaHandlerSetup.LatestPictures.Equals("True", StringComparison.CurrentCulture) &&
              LatestMediaHandlerSetup.RefreshDbPicture.Equals("True", StringComparison.CurrentCulture))
          {
            try
            {
              //LatestMediaHandlerSetup.Lph = new LatestPictureHandler();
              LatestMediaHandlerSetup.Lph.RebuildPictureDatabase();
              LatestMediaHandlerSetup.Lph.GetLatestMediaInfo();
              //lph = null;
            }
            catch
            {
            }
          }
        }
        catch (Exception ex)
        {
          Utils.ReleaseDelayStop("LatestReorgWorker-OnDoWork");
          Utils.SyncPointReorg = 0;
          logger.Error("OnDoWork: " + ex.ToString());
        }
      }
      //Utils.ReleaseDelayStop("DirectoryWorker-OnDoWork");
      //Utils.SyncPointDirectory = 0;
      logger.Info("Database reorg is done.");
    }

    internal void OnRunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
    {
      try
      {
        Utils.ReleaseDelayStop("LatestReorgWorker-OnDoWork");
        LatestMediaHandlerSetup.ReorgTimer.Interval = (Int32.Parse(LatestMediaHandlerSetup.ReorgInterval)*60000);
        LatestMediaHandlerSetup.ReorgTimerTick = Environment.TickCount;
        Utils.SyncPointReorg = 0;
      }
      catch (Exception ex)
      {
        logger.Error("OnRunWorkerCompleted: " + ex.ToString());
      }
    }

  }
}
