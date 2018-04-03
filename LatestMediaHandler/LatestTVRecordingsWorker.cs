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

using LMHNLog.NLog;

using System;
using System.ComponentModel;
using System.Threading;

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
      if (!Utils.IsStopping)
      {
        try
        {
          if (LatestMediaHandlerSetup.LMHThreadPriority == Utils.Priority.Lowest)
            Thread.CurrentThread.Priority = ThreadPriority.Lowest;
          else
            Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;

          Thread.CurrentThread.Name = "LatestTVRecordingsWorker";
          Utils.AllocateDelayStop("LatestTVRecordingsWorker-OnDoWork");
          logger.Info("Refreshing latest TV Recordings is starting.");

          Utils.SetProperty("#latestMediaHandler.scanned", ((Utils.DelayStopCount > 0) ? "true" : "false"));

          int arg = (int) e.Argument;
          Reorganisation(arg == 0 ? Utils.TVCategory.Latests : Utils.TVCategory.Recording);
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

        Utils.SetProperty("#latestMediaHandler.scanned", ((Utils.DelayStopCount > 0) ? "true" : "false"));
      }
      catch (Exception ex)
      {
        logger.Error("OnRunWorkerCompleted: " + ex.ToString());
      }
    }

    internal void Reorganisation(Utils.TVCategory type)
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

          if (obj is LatestTVAllRecordingsHandler)
          {
            if (type == Utils.TVCategory.Latests)
            {
              ((LatestTVAllRecordingsHandler)obj).UpdateLatestMediaInfo();
            }
            else if (type == Utils.TVCategory.Recording)
            {
              ((LatestTVAllRecordingsHandler)obj).UpdateActiveRecordings();
            }
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
