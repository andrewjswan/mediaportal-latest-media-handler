//***********************************************************************
// Assembly         : LatestMediaHandler
// Author           : ajs
// Created          : 21-09-2015
//
// Last Modified By : ajs
// Last Modified On : 30-09-2015
// Description      : 
//
// Copyright        : Open Source software licensed under the GNU/GPL agreement.
//***********************************************************************
extern alias LMHNLog;

using MediaPortal.Configuration;
using MediaPortal.GUI.Library;

using LMHNLog.NLog;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Timers;

namespace LatestMediaHandler
{
  internal class LatestTVAllRecordingsHandler
  {
    #region declarations
    private Logger logger = LogManager.GetCurrentClassLogger();

    private LatestTVRecordingsHandler ltvrh = null;
    private LatestArgusRecordingsHandler largusrh = null;
    private int currentFacade = 0;

    private int showFanart = 1;
    private bool needCleanup = false;
    private int needCleanupCount = 0;
    private bool TVRecordingShowRedDot = false;

    private LatestTVRecordingsWorker MyLatestTVRecordingsWorker = null;
    #endregion

    public const int ControlID = 919199840;
    public const int Play1ControlID = 91919984;
    public const int Play2ControlID = 91919985;
    public const int Play3ControlID = 91919986;
    public const int Play4ControlID = 91919987;

    public List<LatestsFacade> ControlIDFacades;
    public List<int> ControlIDPlays;

    public bool MainFacade
    {
      get { return CurrentFacade.ControlID == ControlID; }
    }

    public LatestsFacade CurrentFacade
    {
      get { return ControlIDFacades[currentFacade]; }
    }

    public LatestsFacade LatestFacade
    {
      get { return ControlIDFacades[ControlIDFacades.Count - 1]; }
    }

    public int ShowFanart
    {
      get { return showFanart; }
      set { showFanart = value; }
    }

    public int NeedCleanupCount
    {
      get { return needCleanupCount; }
      set { needCleanupCount = value; }
    }

    public bool NeedCleanup
    {
      get { return needCleanup; }
      set { needCleanup = value; }
    }

    public LatestTVRecordingsHandler Ltvrh
    {
        get { return ltvrh; }
        set { ltvrh = value; }
    }

    public LatestArgusRecordingsHandler Largusrh
    {
        get { return largusrh; }
        set { largusrh = value; }
    }

    public LatestTVAllRecordingsHandler(int id = ControlID)
    {
      ControlIDFacades = new List<LatestsFacade>();
      ControlIDPlays = new List<int>();
      //
      ControlIDFacades.Add(new LatestsFacade(id, "TVRecordings"));
      if (id == ControlID)
      {
        ControlIDPlays.Add(Play1ControlID);
        ControlIDPlays.Add(Play2ControlID);
        ControlIDPlays.Add(Play3ControlID);
        ControlIDPlays.Add(Play4ControlID);
      }
      ControlIDFacades[ControlIDFacades.Count - 1].UnWatched = Utils.LatestTVRecordingsWatched;
      //
      EmptyLatestMediaProperties();
      EmptyRecordingProps();
      //
      Ltvrh = new LatestTVRecordingsHandler(this);
      Largusrh = new LatestArgusRecordingsHandler(this);
    }

    internal LatestTVAllRecordingsHandler(LatestsFacade facade) : this (facade.ControlID)
    {
      ControlIDFacades[ControlIDFacades.Count - 1] = facade;
    }

    internal void EmptyRecordingProps()
    {
      if (!MainFacade && !CurrentFacade.AddProperties)
      {
        return;
      }

      Utils.SetProperty("#latestMediaHandler.tvrecordings.label", Translation.LabelLatestAdded);
      Utils.SetProperty("#latestMediaHandler.tvrecordings.reddot", "false");

      //Active Recordings
      Utils.SetProperty("#latestMediaHandler.tvrecordings.active.count", string.Empty);
      for (int z = 1; z <= Utils.LatestsMaxTVNum; z++)
      {
        Utils.SetProperty("#latestMediaHandler.tvrecordings.active" + z + ".title", string.Empty);
        Utils.SetProperty("#latestMediaHandler.tvrecordings.active" + z + ".genre", string.Empty);
        Utils.SetProperty("#latestMediaHandler.tvrecordings.active" + z + ".startTime", string.Empty);
        Utils.SetProperty("#latestMediaHandler.tvrecordings.active" + z + ".startDate", string.Empty);
        Utils.SetProperty("#latestMediaHandler.tvrecordings.active" + z + ".endTime", string.Empty);
        Utils.SetProperty("#latestMediaHandler.tvrecordings.active" + z + ".endDate", string.Empty);
        Utils.SetProperty("#latestMediaHandler.tvrecordings.active" + z + ".channel", string.Empty);
        Utils.SetProperty("#latestMediaHandler.tvrecordings.active" + z + ".channelLogo", string.Empty);
      }

      //Scheduled recordings
      Utils.SetProperty("#latestMediaHandler.tvrecordings.scheduled.count", string.Empty);
      for (int z = 1; z <= Utils.LatestsMaxTVNum; z++)
      {
        Utils.SetProperty("#latestMediaHandler.tvrecordings.scheduled" + z + ".title", string.Empty);
        Utils.SetProperty("#latestMediaHandler.tvrecordings.scheduled" + z + ".startTime", string.Empty);
        Utils.SetProperty("#latestMediaHandler.tvrecordings.scheduled" + z + ".startDate", string.Empty);
        Utils.SetProperty("#latestMediaHandler.tvrecordings.scheduled" + z + ".endTime", string.Empty);
        Utils.SetProperty("#latestMediaHandler.tvrecordings.scheduled" + z + ".endDate", string.Empty);
        Utils.SetProperty("#latestMediaHandler.tvrecordings.scheduled" + z + ".channel", string.Empty);
        Utils.SetProperty("#latestMediaHandler.tvrecordings.scheduled" + z + ".channelLogo", string.Empty);
      }
    }

    internal void EmptyLatestMediaProperties()
    {
      if (!MainFacade && !CurrentFacade.AddProperties)
      {
        return;
      }

      Utils.SetProperty("#latestMediaHandler.tvrecordings.label", Translation.LabelLatestAdded);
      Utils.SetProperty("#latestMediaHandler.tvrecordings.latest.enabled", "false");
      Utils.SetProperty("#latestMediaHandler.tvrecordings.hasnew", "false");
      for (int z = 1; z <= Utils.LatestsMaxTVNum; z++)
      {
        Utils.SetProperty("#latestMediaHandler.tvrecordings.latest" + z + ".thumb", string.Empty);
        Utils.SetProperty("#latestMediaHandler.tvrecordings.latest" + z + ".title", string.Empty);
        Utils.SetProperty("#latestMediaHandler.tvrecordings.latest" + z + ".dateAdded", string.Empty);
        Utils.SetProperty("#latestMediaHandler.tvrecordings.latest" + z + ".genre", string.Empty);
        Utils.SetProperty("#latestMediaHandler.tvrecordings.latest" + z + ".summary", string.Empty);
        Utils.SetProperty("#latestMediaHandler.tvrecordings.latest" + z + ".series", string.Empty);
        Utils.SetProperty("#latestMediaHandler.tvrecordings.latest" + z + ".episode", string.Empty);
        Utils.SetProperty("#latestMediaHandler.tvrecordings.latest" + z + ".episodename", string.Empty);
        Utils.SetProperty("#latestMediaHandler.tvrecordings.latest" + z + ".directory", string.Empty);
        Utils.SetProperty("#latestMediaHandler.tvrecordings.latest" + z + ".new", "false");
      }
    }

    internal void MyContextMenu()
    {
      try
      {
        if (Utils.UsedArgus)
        {
          Largusrh.MyContextMenu();
        }
        else
        {
          Ltvrh.MyContextMenu();
        }
      }
      catch (Exception ex)
      {
        logger.Error("MyContextMenu: " + ex.ToString());
      }
    }

    internal void GetTVRecordings(int Mode = 0) // 0 - UpdateLatestMediaInfo(); 1 - UpdateActiveRecordings();
    {
      if (!Utils.LatestTVRecordings)
        return ;

      if (!Utils.IsStopping)
      {
        try
        {
          // No other event was executing.                                   
          if (MyLatestTVRecordingsWorker == null)
          {
            MyLatestTVRecordingsWorker = new LatestTVRecordingsWorker();
            MyLatestTVRecordingsWorker.RunWorkerCompleted += MyLatestTVRecordingsWorker.OnRunWorkerCompleted;
          }
          if (!MyLatestTVRecordingsWorker.IsBusy)
          {
            MyLatestTVRecordingsWorker.RunWorkerAsync(Mode);
          }
        }
        catch (Exception ex)
        {
          logger.Error("GetTVRecordings: [" + ((Mode == 0) ? "GET" : "UPDATE") + "] " + ex.ToString());
        }
      }
    }

    internal void UpdateActiveRecordingsThread()
    {
      GetTVRecordings(1);
    }

    internal void UpdateActiveRecordings()
    {
      // if (Interlocked.CompareExchange(ref CurrentFacade.Update, 1, 0) != 0)
      //   return;

      if (Utils.LatestTVRecordings)
      {
        try
        {
          if (Utils.UsedArgus)
          {
            Largusrh.UpdateActiveRecordings();
          }
          else
          {
            Ltvrh.UpdateActiveRecordings();
          }
        }
        catch (FileNotFoundException)
        {
          //do nothing    
        }
        catch (MissingMethodException)
        {
          //do nothing    
        }
        catch (Exception ex)
        {
          logger.Error("UpdateActiveRecordings: " + ex.ToString());
        }
      }
      // CurrentFacade.Update = 0;
    }

    internal void UpdateLatestMediaInfo()
    {
      if (Interlocked.CompareExchange(ref CurrentFacade.Update, 1, 0) != 0)
        return;

      if (Utils.LatestTVRecordings)
      {
        try
        {
          GetLatestMediaInfo();
        }
        catch (FileNotFoundException)
        {
          //do nothing    
        }
        catch (MissingMethodException)
        {
          //do nothing    
        }
        catch (Exception ex)
        {
          logger.Error("UpdateLatestMediaInfo: " + ex.ToString());
        }
      }

      CurrentFacade.Update = 0;
    }

    internal void GetLatestMediaInfo()
    {
      int z = 1;
      if (Utils.LatestTVRecordings)
      {
        //TV Recordings
        LatestsCollection latestTVRecordings = null;
        try
        {
          MediaPortal.Profile.Settings xmlreader =  new MediaPortal.Profile.Settings(Config.GetFile(Config.Dir.Config, "MediaPortal.xml"));

          string useArgus = xmlreader.GetValue("plugins", "ARGUS TV");
          string dllFile = Config.GetFile(Config.Dir.Plugins, @"Windows\ArgusTV.UI.MediaPortal.dll");

          if (useArgus != null && useArgus.Equals("yes", StringComparison.CurrentCulture) && File.Exists(dllFile))
          {
            FileVersionInfo myFileVersionInfo = FileVersionInfo.GetVersionInfo(dllFile);
            logger.Debug("Argus version = {0}", myFileVersionInfo.FileVersion);

            if (Largusrh == null)
            {
              Largusrh = new LatestArgusRecordingsHandler(this);
            }

            ResolveEventHandler assemblyResolve = Largusrh.OnAssemblyResolve;
            try
            {
              AppDomain currentDomain = AppDomain.CurrentDomain;
              currentDomain.AssemblyResolve += new ResolveEventHandler(Largusrh.OnAssemblyResolve);
              Largusrh.IsGetTypeRunningOnThisThread = true;
              latestTVRecordings = Largusrh.GetArgusRecordings();
              Largusrh.UpdateActiveRecordings();
              AppDomain.CurrentDomain.AssemblyResolve -= assemblyResolve;
              Utils.UsedArgus = true;
            }
            catch (FileNotFoundException)
            {
              //do nothing    
            }
            catch (MissingMethodException)
            {
              //do nothing    
            }
            catch (Exception ex)
            {
              logger.Error("GetLatestMediaInfo (TV Argus Recordings): " + ex.ToString());
              AppDomain.CurrentDomain.AssemblyResolve -= assemblyResolve;
            }
          }
          else
          {
            if (Ltvrh == null)
            {
              Ltvrh = new LatestTVRecordingsHandler(this);
            }
            latestTVRecordings = Ltvrh.GetTVRecordings();
            Ltvrh.UpdateActiveRecordings();
            Utils.UsedArgus = false;
          }
        }
        catch (FileNotFoundException)
        {
          //do nothing    
        }
        catch (MissingMethodException)
        {
          //do nothing    
        }
        catch (Exception ex)
        {
          logger.Error("GetLatestMediaInfo (TV Recordings): " + ex.ToString());
        }
        bool noNewRecordings = false;
        if ((latestTVRecordings != null && latestTVRecordings.Count > 0) && 
            Utils.GetProperty("#latestMediaHandler.tvrecordings.latest" + z + ".title").Equals(latestTVRecordings[0].Title, StringComparison.CurrentCulture))
        {
          noNewRecordings = true;
          logger.Info("Updating Latest Media Info: TV Recording: No new recordings since last check!");
        }

        if (latestTVRecordings != null && latestTVRecordings.Count > 0)
        {
          if (!noNewRecordings)
          {
            EmptyLatestMediaProperties();
            z = 1;
            for (int i = 0; i < latestTVRecordings.Count && i < Utils.LatestsMaxTVNum; i++)
            {
              logger.Info("Updating Latest Media Info: TV Recording: Recording " + z + ": " + latestTVRecordings[i].Title);

              string recsummary = (string.IsNullOrEmpty(latestTVRecordings[i].Summary) ? Translation.NoDescription : latestTVRecordings[i].Summary);
              string recsummaryoutline = Utils.GetSentences(recsummary, Utils.LatestPlotOutlineSentencesNum);
              Utils.SetProperty("#latestMediaHandler.tvrecordings.latest" + z + ".thumb", latestTVRecordings[i].Thumb);
              Utils.SetProperty("#latestMediaHandler.tvrecordings.latest" + z + ".title", latestTVRecordings[i].Title);
              Utils.SetProperty("#latestMediaHandler.tvrecordings.latest" + z + ".dateAdded", latestTVRecordings[i].DateAdded);
              Utils.SetProperty("#latestMediaHandler.tvrecordings.latest" + z + ".genre", latestTVRecordings[i].Genre);
              Utils.SetProperty("#latestMediaHandler.tvrecordings.latest" + z + ".summary", recsummary);
              Utils.SetProperty("#latestMediaHandler.tvrecordings.latest" + z + ".summaryoutline", recsummaryoutline);
              Utils.SetProperty("#latestMediaHandler.tvrecordings.latest" + z + ".series", latestTVRecordings[i].SeriesIndex);
              Utils.SetProperty("#latestMediaHandler.tvrecordings.latest" + z + ".episode", latestTVRecordings[i].EpisodeIndex);
              Utils.SetProperty("#latestMediaHandler.tvrecordings.latest" + z + ".episodename", latestTVRecordings[i].ThumbSeries);
              Utils.SetProperty("#latestMediaHandler.tvrecordings.latest" + z + ".directory", latestTVRecordings[i].Directory);
              Utils.SetProperty("#latestMediaHandler.tvrecordings.latest" + z + ".new", latestTVRecordings[i].New);
              z++;
            }
            //latestTVRecordings.Clear();
            Utils.SetProperty("#latestMediaHandler.tvrecordings.latest.enabled", "true");
            Utils.SetProperty("#latestMediaHandler.tvrecordings.hasnew", CurrentFacade.HasNew ? "true" : "false");
            logger.Debug("Updating Latest Media Info: TV Recording: Has new: " + (CurrentFacade.HasNew ? "true" : "false"));
          }
        }
        else
        {
          EmptyLatestMediaProperties();
          logger.Info("Updating Latest Media Info: TV Recording: No recordings found!");
        }
        //latestTVRecordings = null;
        z = 1;
      }
      else
      {
        EmptyLatestMediaProperties();
      }
      Utils.UpdateLatestsUpdate(Utils.LatestsCategory.TV, DateTime.Now);
    }

    internal void UpdateImageTimer(GUIWindow fWindow, Object stateInfo, ElapsedEventArgs e)
    {
      if (Utils.LatestTVRecordings)
      {
        try
        {
          bool showRedDot = false;
          if (Utils.UsedArgus)
          {
            showRedDot = Largusrh.GetRecordingRedDot();
          }
          else
          {
            showRedDot = Ltvrh.GetRecordingRedDot();
          }
          if (showRedDot != TVRecordingShowRedDot)
          {
            TVRecordingShowRedDot = showRedDot;
            Utils.SetProperty("#latestMediaHandler.tvrecordings.reddot", TVRecordingShowRedDot ? "true" : "false");

            logger.Debug("TV Recordings reddot changes detected: Refreshing Active/Schedulled.");
            UpdateActiveRecordingsThread();
          }
        }
        catch (Exception ex)
        {
          logger.Error("UpdateImageTimer/RedDot: " + ex.ToString());
        }

        try
        {
          if (fWindow.GetFocusControlId() == CurrentFacade.ControlID)
          {
            if (Utils.UsedArgus)
            {
              Largusrh.UpdateSelectedImageProperties();
            }
            else
            {
              Ltvrh.UpdateSelectedImageProperties();
            }
            NeedCleanup = true;
          }
          else
          {
            if (NeedCleanup && NeedCleanupCount >= 5)
            {
              Utils.SetProperty("#latestMediaHandler.tvrecordings.selected.fanart1", " ");
              Utils.SetProperty("#latestMediaHandler.tvrecordings.selected.fanart2", " ");
              if (Utils.UsedArgus)
              {
                Utils.UnLoadImages(ref Largusrh.images);
              }
              else
              {
                Utils.UnLoadImages(ref Ltvrh.images);
              }
              ShowFanart = 1;
              CurrentFacade.SelectedItem = -1;
              CurrentFacade.SelectedImage = -1;
              NeedCleanup = false;
              NeedCleanupCount = 0;
            }
            else if (NeedCleanup && NeedCleanupCount == 0)
            {
              Utils.SetProperty("#latestMediaHandler.tvrecordings.selected.showfanart1", "false");
              Utils.SetProperty("#latestMediaHandler.tvrecordings.selected.showfanart2", "false");
              NeedCleanupCount++;
            }
            else if (NeedCleanup)
            {
              NeedCleanupCount++;
            }
          }
        }
        catch (Exception ex)
        {
          logger.Error("UpdateImageTimer (recordings): " + ex.ToString());
        }
      }
    }

    internal void InitFacade()
    {
      if (!Utils.LatestTVRecordings)
      {
        return;
      }

      try
      {
        CurrentFacade.Facade = Utils.GetLatestsFacade(CurrentFacade.ControlID);
        if (CurrentFacade.Facade != null)
        {
          Utils.ClearFacade(ref CurrentFacade.Facade);
          InitFacade(ref CurrentFacade.Facade);
          Utils.UpdateFacade(ref CurrentFacade.Facade, CurrentFacade);
        }
      }
      catch (Exception ex)
      {
        logger.Error("InitFacade: " + ex.ToString());
      }
    }

    internal void InitFacade(ref GUIFacadeControl facade)
    {
      if (Utils.UsedArgus)
      {
        if (Largusrh != null && Largusrh.FacadeCollection != null)
        {
          for (int i = 0; i < Largusrh.FacadeCollection.Count; i++)
          {
            GUIListItem _gc = Largusrh.FacadeCollection[i] as GUIListItem;
            // Utils.LoadImage(_gc.IconImage, ref Largusrh.imagesThumbs);
            facade.Add(_gc);
          }
        }
      }
      else
      {
        if (Ltvrh != null && Ltvrh.FacadeCollection != null)
        {
          for (int i = 0; i < Ltvrh.FacadeCollection.Count; i++)
          {
            GUIListItem _gc = Ltvrh.FacadeCollection[i] as GUIListItem;
            // Utils.LoadImage(_gc.IconImage, ref Ltvrh.imagesThumbs);
            facade.Add(_gc);
          }
        }
      }
    }

    internal void ClearFacade(ref GUIFacadeControl facade)
    {
      Utils.ClearFacade(ref facade);
      if (Utils.UsedArgus)
      {
        Utils.UnLoadImages(ref Largusrh.images);
        Utils.UnLoadImages(ref Largusrh.imagesThumbs);
      }
      else
      {
        Utils.UnLoadImages(ref Ltvrh.images);
        Utils.UnLoadImages(ref Ltvrh.imagesThumbs);
      }
    }

    internal void DeInitFacade()
    {
      if (!Utils.LatestTVRecordings)
      {
        return;
      }

      try
      {
        CurrentFacade.Facade = Utils.GetLatestsFacade(CurrentFacade.ControlID, true);
        if (CurrentFacade.Facade != null)
        {
          ClearFacade(ref CurrentFacade.Facade);
        }
      }
      catch (Exception ex)
      {
        logger.Error("DeInitFacade: " + ex.ToString());
      }
    }

    internal void SetupReceivers()
    {
      try
      {
        GUIWindowManager.Receivers += new SendMessageHandler(OnMessage);
      }
      catch (Exception ex)
      {
        logger.Error("SetupTVRecordingsLatest: " + ex.ToString());
      }
    }

    internal void DisposeReceivers()
    {
      try
      {
        GUIWindowManager.Receivers -= new SendMessageHandler(OnMessage);

        if (MyLatestTVRecordingsWorker != null)
        {
          MyLatestTVRecordingsWorker.CancelAsync();
          MyLatestTVRecordingsWorker.Dispose();
        }
      }
      catch (Exception ex)
      {
        logger.Error("DisposeTVRecordingsLatest: " + ex.ToString());
      }
    }

    private void OnMessage(GUIMessage message)
    {
      if (Utils.LatestTVRecordings)
      {
        try
        {
          System.Threading.ThreadPool.QueueUserWorkItem(delegate { OnMessageTasks(message); }, null);
        }
        catch { }
      }
    }

    private void OnMessageTasks(GUIMessage message)
    {
      bool Update = false;

      Utils.ThreadToSleep();
      switch (message.Message)
      {
        case GUIMessage.MessageType.GUI_MSG_PLAYBACK_ENDED:
        case GUIMessage.MessageType.GUI_MSG_PLAYBACK_STOPPED:
        {
          logger.Debug("Playback End/Stop detected: Refreshing Active/Schedulled.");
          Update = true;
          break;
        }
        case GUIMessage.MessageType.GUI_MSG_NOTIFY_REC:
        {
          logger.Debug("TV Recordings notify detected: Refreshing Active/Schedulled.");
          Update = true;
          break;
        }
        case GUIMessage.MessageType.GUI_MSG_TV_ERROR_NOTIFY:
        {
          logger.Debug("TV Recordings error notify detected: Refreshing Active/Schedulled.");
          Update = true;
          break;
        }
        case GUIMessage.MessageType.GUI_MSG_MANUAL_RECORDING_STARTED:
        {
          logger.Debug("TV Recordings manual recording detected: Refreshing Active/Schedulled.");
          Update = true;
          break;
        }
      }

      if (Update)
      {
        UpdateActiveRecordingsThread();
      }
    }

    internal bool PlayRecording(GUIWindow fWindow, ref MediaPortal.GUI.Library.Action action)
    {
      try
      {
        int idx = -1;
        int FocusControlID = fWindow.GetFocusControlId();

        if (ControlIDPlays.Contains(FocusControlID))
        {
          idx = ControlIDPlays.IndexOf(FocusControlID)+1;
        }
        //
        // CurrentFacade.Facade = Utils.GetLatestsFacade(CurrentFacade.ControlID);
        if (CurrentFacade.Facade != null && CurrentFacade.Facade.Focus && CurrentFacade.Facade.SelectedListItem != null)
        {
          idx = CurrentFacade.Facade.SelectedListItem.ItemId;
        }
        //
        if (idx >= 0)
        {
          if (Utils.UsedArgus)
          {
            action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_KEY_PRESSED;
            Largusrh.PlayRecording(idx);
          }
          else
          {
            Ltvrh.PlayRecording(idx);
          }
          return true;
        }
      }
      catch (Exception ex)
      {
        logger.Error("Unable to play recording! " + ex.ToString());
        return true;
      }
      return false;
    }
  }
}
