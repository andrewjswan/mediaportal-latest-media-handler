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
extern alias RealNLog;

using System;
using System.IO;
using System.Threading;
using System.Timers;
using System.Windows.Forms;
using System.Diagnostics;
using System.Collections.Generic;

using RealNLog.NLog;

using MediaPortal.Configuration;
using MediaPortal.GUI.Library;
//using MediaPortal.Player;
//using MediaPortal.Playlists;
//using TvDatabase;
//using TvDatabase;
//using TvPlugin;
//using TvControl;

namespace LatestMediaHandler
{
  internal class LatestTVAllRecordingsHandler
  {
    #region declarations
    private Logger logger = LogManager.GetCurrentClassLogger();

    private LatestTVRecordingsHandler ltvrh = null;
    private Latest4TRRecordingsHandler l4trrh = null;
    private LatestArgusRecordingsHandler largusrh = null;

    private LatestTVRecordingsWorker MyLatestTVRecordingsWorker = null;
    #endregion

    public const int ControlID = 919199840;
    public const int Play1ControlID = 91919984;
    public const int Play2ControlID = 91919985;
    public const int Play3ControlID = 91919986;
    public const int Play4ControlID = 91919987;

    public List<int> ControlIDFacades;
    public List<int> ControlIDPlays;

    public LatestTVRecordingsHandler Ltvrh
    {
        get { return ltvrh; }
        set { ltvrh = value; }
    }

    public Latest4TRRecordingsHandler L4trrh
    {
        get { return l4trrh; }
        set { l4trrh = value; }
    }

    public LatestArgusRecordingsHandler Largusrh
    {
        get { return largusrh; }
        set { largusrh = value; }
    }

    public LatestTVAllRecordingsHandler()
    {
      Ltvrh = new LatestTVRecordingsHandler();
      L4trrh = new Latest4TRRecordingsHandler();
      Largusrh = new LatestArgusRecordingsHandler();

      ControlIDFacades = new List<int>();
      ControlIDPlays = new List<int>();
      //
      ControlIDFacades.Add(ControlID);
      ControlIDPlays.Add(Play1ControlID);
      ControlIDPlays.Add(Play2ControlID);
      ControlIDPlays.Add(Play3ControlID);
      ControlIDPlays.Add(Play4ControlID);
    }

    public int LastFocusedId
    {
      get 
      {
          if (Utils.Used4TRTV && !Utils.UsedArgus)
          {
              if (L4trrh != null)
              {
                  return L4trrh.LastFocusedId;
              }
          }
          else if (Utils.Used4TRTV && Utils.UsedArgus)
          {
              if (Largusrh != null)
              {
                  return Largusrh.LastFocusedId;
              }
          }
          else if (!Utils.Used4TRTV)
          {
              if (Ltvrh != null)
              {
                  return Ltvrh.LastFocusedId;
              }
          }
          return 0;
      }
      set 
      {
        if (Utils.Used4TRTV && !Utils.UsedArgus)
        {
          if (L4trrh != null)
          {
            L4trrh.LastFocusedId = value;
          }
        }
        else if (Utils.Used4TRTV && Utils.UsedArgus)
        {
          if (Largusrh != null)
          {
            Largusrh.LastFocusedId = value;
          }
        }
        else if (!Utils.Used4TRTV)
        {
          if (Ltvrh != null)
          {
            Ltvrh.LastFocusedId = value;
          }
        }
      }
    }

    internal void EmptyRecordingProps()
    {
      Utils.SetProperty("#latestMediaHandler.tvrecordings.label", Translation.LabelLatestAdded);
      //Active Recordings
      for (int z = 1; z < 4; z++)
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
      for (int z = 1; z < 4; z++)
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

    internal void EmptyLatestMediaPropsTVRecordings()
    {
      Utils.SetProperty("#latestMediaHandler.tvrecordings.label", Translation.LabelLatestAdded);
      Utils.SetProperty("#latestMediaHandler.tvrecordings.latest.enabled", "false");
      Utils.SetProperty("#latestMediaHandler.tvrecordings.hasnew", "false");
      for (int z = 1; z < 4; z++)
      {
        Utils.SetProperty("#latestMediaHandler.tvrecordings.latest" + z + ".thumb", string.Empty);
        Utils.SetProperty("#latestMediaHandler.tvrecordings.latest" + z + ".title", string.Empty);
        Utils.SetProperty("#latestMediaHandler.tvrecordings.latest" + z + ".dateAdded", string.Empty);
        Utils.SetProperty("#latestMediaHandler.tvrecordings.latest" + z + ".genre", string.Empty);
        Utils.SetProperty("#latestMediaHandler.tvrecordings.latest" + z + ".summary", string.Empty);
        Utils.SetProperty("#latestMediaHandler.tvrecordings.latest" + z + ".series", string.Empty);
        Utils.SetProperty("#latestMediaHandler.tvrecordings.latest" + z + ".episode", string.Empty);
        Utils.SetProperty("#latestMediaHandler.tvrecordings.latest" + z + ".episodename", string.Empty);
        Utils.SetProperty("#latestMediaHandler.tvrecordings.latest" + z + ".new", "false");
      }
    }

    internal void MyContextMenu()
    {
      try
      {
        if (Utils.Used4TRTV && !Utils.UsedArgus)
        {
          L4trrh.MyContextMenu();
        }
        else if (Utils.Used4TRTV && Utils.UsedArgus)
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
      if (!LatestMediaHandlerSetup.LatestTVRecordings.Equals("True", StringComparison.CurrentCulture))
        return ;

      if (Utils.GetIsStopping() == false)
      {
        try
        {
          int sync = Interlocked.CompareExchange(ref Utils.SyncPointTVRecordings, 1, 0);
          if (sync == 0)
          {
            // No other event was executing.                                   
            if (MyLatestTVRecordingsWorker == null)
            {
              MyLatestTVRecordingsWorker = new LatestTVRecordingsWorker();
              MyLatestTVRecordingsWorker.RunWorkerCompleted += MyLatestTVRecordingsWorker.OnRunWorkerCompleted;
            }
            if (MyLatestTVRecordingsWorker.IsBusy)
              return;

            MyLatestTVRecordingsWorker.RunWorkerAsync(Mode);
          }
        }
        catch (Exception ex)
        {
          Utils.SyncPointTVRecordings = 0;
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
      if (LatestMediaHandlerSetup.LatestTVRecordings.Equals("True", StringComparison.CurrentCulture))
      {
        try
        {
          if (Utils.Used4TRTV && !Utils.UsedArgus)
          {
            L4trrh.UpdateActiveRecordings();
          }
          else if (Utils.Used4TRTV && Utils.UsedArgus)
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
    }

    internal void UpdateLatestMediaInfo()
    {
      if (LatestMediaHandlerSetup.LatestTVRecordings.Equals("True", StringComparison.CurrentCulture))
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
    }

    internal void GetLatestMediaInfo()
    {
      int z = 1;
      if (LatestMediaHandlerSetup.LatestTVRecordings.Equals("True", StringComparison.CurrentCulture))
      {
        //TV Recordings
        LatestMediaHandler.LatestsCollection latestTVRecordings = null;
        try
        {
          MediaPortal.Profile.Settings xmlreader =  new MediaPortal.Profile.Settings(Config.GetFile(Config.Dir.Config, "MediaPortal.xml"));

          string use4TR = xmlreader.GetValue("plugins", "For The Record TV");
          string dllFile = Config.GetFile(Config.Dir.Plugins, @"Windows\ForTheRecord.UI.MediaPortal.dll");
          string dllFileArgus = Config.GetFile(Config.Dir.Plugins, @"Windows\ArgusTV.UI.MediaPortal.dll");

          if (use4TR != null && use4TR.Equals("yes", StringComparison.CurrentCulture) && File.Exists(dllFile))
          {
            FileVersionInfo myFileVersionInfo = FileVersionInfo.GetVersionInfo(dllFile);
            logger.Debug("ForTheRecord version = {0}", myFileVersionInfo.FileVersion);

            if (L4trrh == null)
            {
              L4trrh = new Latest4TRRecordingsHandler();
              try
              {
                //this can be removed when 1.6.0.2/1.6.1.0 is out for a while
                if (myFileVersionInfo.FileVersion == "1.6.0.1" || myFileVersionInfo.FileVersion == "1.6.0.0" || myFileVersionInfo.FileVersion == "1.5.0.3")
                {
                  l4trrh.Is4TRversion1602orAbove = false;
                } else {
                  l4trrh.Is4TRversion1602orAbove = true;
                }
              }
              catch
              {
                l4trrh.Is4TRversion1602orAbove = false;
              }
            }

            ResolveEventHandler assemblyResolve = L4trrh.OnAssemblyResolve;
            try
            {
              AppDomain currentDomain = AppDomain.CurrentDomain;
              currentDomain.AssemblyResolve += new ResolveEventHandler(L4trrh.OnAssemblyResolve);
              L4trrh.IsGetTypeRunningOnThisThread = true;
              latestTVRecordings = L4trrh.Get4TRRecordings();
              L4trrh.UpdateActiveRecordings();
              AppDomain.CurrentDomain.AssemblyResolve -= assemblyResolve;
              Utils.Used4TRTV = true;
              Utils.UsedArgus = false;
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
              logger.Error("GetLatestMediaInfo (TV 4TR Recordings): " + ex.ToString());
              AppDomain.CurrentDomain.AssemblyResolve -= assemblyResolve;
            }
          }
          else if (use4TR != null && use4TR.Equals("yes", StringComparison.CurrentCulture) && File.Exists(dllFileArgus))
          {
            FileVersionInfo myFileVersionInfo = FileVersionInfo.GetVersionInfo(dllFileArgus);
            logger.Debug("Argus version = {0}", myFileVersionInfo.FileVersion);

            if (Largusrh == null)
            {
              Largusrh = new LatestArgusRecordingsHandler();
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
              Utils.Used4TRTV = true;
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
              Ltvrh = new LatestTVRecordingsHandler();
            }
            latestTVRecordings = Ltvrh.GetTVRecordings();
            Ltvrh.UpdateActiveRecordings();
            Utils.Used4TRTV = false;
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
        if ((latestTVRecordings != null && latestTVRecordings.Count > 1) && 
            Utils.GetProperty("#latestMediaHandler.tvrecordings.latest" + z + ".title").Equals(latestTVRecordings[0].Title, StringComparison.CurrentCulture))
        {
          noNewRecordings = true;
          logger.Info("Updating Latest Media Info: TV Recording: No new recordings since last check!");
        }

        if (latestTVRecordings != null && latestTVRecordings.Count > 1)
        {
          if (!noNewRecordings)
          {
            EmptyLatestMediaPropsTVRecordings();
            z = 1;
            for (int i = 0; i < latestTVRecordings.Count && i < 4; i++)
            {
              logger.Info("Updating Latest Media Info: TV Recording: Recording " + z + ": " + latestTVRecordings[i].Title);

              string recsummary = (string.IsNullOrEmpty(latestTVRecordings[i].Summary) ? Translation.NoDescription : latestTVRecordings[i].Summary);
              string recsummaryoutline = Utils.GetSentences(recsummary, Utils.latestPlotOutlineSentencesNum);
              Utils.SetProperty("#latestMediaHandler.tvrecordings.latest" + z + ".thumb", latestTVRecordings[i].Thumb);
              Utils.SetProperty("#latestMediaHandler.tvrecordings.latest" + z + ".title", latestTVRecordings[i].Title);
              Utils.SetProperty("#latestMediaHandler.tvrecordings.latest" + z + ".dateAdded", latestTVRecordings[i].DateAdded);
              Utils.SetProperty("#latestMediaHandler.tvrecordings.latest" + z + ".genre", latestTVRecordings[i].Genre);
              Utils.SetProperty("#latestMediaHandler.tvrecordings.latest" + z + ".summary", recsummary);
              Utils.SetProperty("#latestMediaHandler.tvrecordings.latest" + z + ".summaryoutline", recsummaryoutline);
              Utils.SetProperty("#latestMediaHandler.tvrecordings.latest" + z + ".series", latestTVRecordings[i].SeriesIndex);
              Utils.SetProperty("#latestMediaHandler.tvrecordings.latest" + z + ".episode", latestTVRecordings[i].EpisodeIndex);
              Utils.SetProperty("#latestMediaHandler.tvrecordings.latest" + z + ".episodename", latestTVRecordings[i].ThumbSeries);
              Utils.SetProperty("#latestMediaHandler.tvrecordings.latest" + z + ".new", latestTVRecordings[i].New);
              z++;
            }
            //latestTVRecordings.Clear();
            Utils.SetProperty("#latestMediaHandler.tvrecordings.latest.enabled", "true");
            Utils.SetProperty("#latestMediaHandler.tvrecordings.hasnew", Utils.HasNewTVRecordings ? "true" : "false");
            logger.Debug("Updating Latest Media Info: TV Recording: Has new: " + (Utils.HasNewTVRecordings ? "true" : "false"));
          }
        }
        else
        {
          EmptyLatestMediaPropsTVRecordings();
          logger.Info("Updating Latest Media Info: TV Recording: No recordings found!");
        }
        //latestTVRecordings = null;
        z = 1;
      }
      else
      {
        EmptyLatestMediaPropsTVRecordings();
      }
    }

    internal void UpdateImageTimer(GUIWindow fWindow, Object stateInfo, ElapsedEventArgs e)
    {
      if (LatestMediaHandlerSetup.LatestTVRecordings.Equals("True", StringComparison.CurrentCulture))
      {
        try
        {
          if (fWindow.GetFocusControlId() == LatestTVAllRecordingsHandler.ControlID)
          {
            if (Utils.Used4TRTV && !Utils.UsedArgus)
            {
              L4trrh.UpdateSelectedImageProperties();
              L4trrh.NeedCleanup = true;
            }
            else if (Utils.Used4TRTV && Utils.UsedArgus)
            {
              Largusrh.UpdateSelectedImageProperties();
              Largusrh.NeedCleanup = true;
            }
            else
            {
              Ltvrh.UpdateSelectedImageProperties();
              Ltvrh.NeedCleanup = true;
            }
          }
          else
          {
            if (Utils.Used4TRTV && !Utils.UsedArgus)
            {
              if (L4trrh.NeedCleanup && L4trrh.NeedCleanupCount >= 5)
              {
                Utils.SetProperty("#latestMediaHandler.tvrecordings.selected.fanart1", " ");
                Utils.SetProperty("#latestMediaHandler.tvrecordings.selected.fanart2", " ");
                Utils.UnLoadImage(ref L4trrh.images);
                L4trrh.ShowFanart = 1;
                L4trrh.SelectedFacadeItem2 = -1;
                L4trrh.SelectedFacadeItem2 = -1;
                L4trrh.NeedCleanup = false;
                L4trrh.NeedCleanupCount = 0;
              }
              else if (L4trrh.NeedCleanup && L4trrh.NeedCleanupCount == 0)
              {
                Utils.SetProperty("#latestMediaHandler.tvrecordings.selected.showfanart1", "false");
                Utils.SetProperty("#latestMediaHandler.tvrecordings.selected.showfanart2", "false");
                L4trrh.NeedCleanupCount++;
              }
              else if (L4trrh.NeedCleanup)
              {
                L4trrh.NeedCleanupCount++;
              }
            }
            else if (Utils.Used4TRTV && Utils.UsedArgus)
            {
              if (Largusrh.NeedCleanup && Largusrh.NeedCleanupCount >= 5)
              {
                Utils.SetProperty("#latestMediaHandler.tvrecordings.selected.fanart1", " ");
                Utils.SetProperty("#latestMediaHandler.tvrecordings.selected.fanart2", " ");
                Utils.UnLoadImage(ref Largusrh.images);
                Largusrh.ShowFanart = 1;
                Largusrh.SelectedFacadeItem2 = -1;
                Largusrh.SelectedFacadeItem2 = -1;
                Largusrh.NeedCleanup = false;
                Largusrh.NeedCleanupCount = 0;
              }
              else if (Largusrh.NeedCleanup && Largusrh.NeedCleanupCount == 0)
              {
                Utils.SetProperty("#latestMediaHandler.tvrecordings.selected.showfanart1", "false");
                Utils.SetProperty("#latestMediaHandler.tvrecordings.selected.showfanart2", "false");
                Largusrh.NeedCleanupCount++;
              }
              else if (Largusrh.NeedCleanup)
              {
                Largusrh.NeedCleanupCount++;
              }
            }
            else
            {
              if (Ltvrh.NeedCleanup && Ltvrh.NeedCleanupCount >= 5)
              {
                Utils.SetProperty("#latestMediaHandler.tvrecordings.selected.fanart1", " ");
                Utils.SetProperty("#latestMediaHandler.tvrecordings.selected.fanart2", " ");
                Utils.UnLoadImage(ref Ltvrh.images);
                Ltvrh.ShowFanart = 1;
                Ltvrh.SelectedFacadeItem2 = -1;
                Ltvrh.SelectedFacadeItem2 = -1;
                Ltvrh.NeedCleanup = false;
                Ltvrh.NeedCleanupCount = 0;
              }
              else if (Ltvrh.NeedCleanup && Ltvrh.NeedCleanupCount == 0)
              {
                Utils.SetProperty("#latestMediaHandler.tvrecordings.selected.showfanart1", "false");
                Utils.SetProperty("#latestMediaHandler.tvrecordings.selected.showfanart2", "false");
                Ltvrh.NeedCleanupCount++;
              }
              else if (Ltvrh.NeedCleanup)
              {
                Ltvrh.NeedCleanupCount++;
              }
            }
          }
        }
        catch (Exception ex)
        {
          logger.Error("UpdateImageTimer (recordings): " + ex.ToString());
        }
      }
    }

    internal void InitFacade(ref GUIFacadeControl facade)
    {
      if (Utils.Used4TRTV && !Utils.UsedArgus)
      {
        if (L4trrh != null && L4trrh.Al != null)
        {
          for (int i = 0; i < L4trrh.Al.Count; i++)
          {
            GUIListItem _gc = L4trrh.Al[i] as GUIListItem;
            Utils.LoadImage(_gc.IconImage, ref L4trrh.imagesThumbs);
            facade.Add(_gc);
          }
        }
      }
      else if (Utils.Used4TRTV && Utils.UsedArgus)
      {
        if (L4trrh != null && Largusrh.Al != null)
        {
          for (int i = 0; i < Largusrh.Al.Count; i++)
          {
            GUIListItem _gc = Largusrh.Al[i] as GUIListItem;
            Utils.LoadImage(_gc.IconImage, ref Largusrh.imagesThumbs);
            facade.Add(_gc);
          }
        }
      }
      else if (!Utils.Used4TRTV)
      {
        if (Ltvrh != null && Ltvrh.Al != null)
        {
          for (int i = 0; i < Ltvrh.Al.Count; i++)
          {
            GUIListItem _gc = Ltvrh.Al[i] as GUIListItem;
            Utils.LoadImage(_gc.IconImage, ref Ltvrh.imagesThumbs);
            facade.Add(_gc);
          }
        }
      }
    }

    internal void ClearFacade(ref GUIFacadeControl facade)
    {
      facade.Clear();
      if (Utils.Used4TRTV && !Utils.UsedArgus)
      {
        Utils.UnLoadImage(ref L4trrh.images);
        Utils.UnLoadImage(ref L4trrh.imagesThumbs);
      }
      else if (Utils.Used4TRTV && Utils.UsedArgus)
      {
        Utils.UnLoadImage(ref Largusrh.images);
        Utils.UnLoadImage(ref Largusrh.imagesThumbs);
      }
      else
      {
        Utils.UnLoadImage(ref Ltvrh.images);
        Utils.UnLoadImage(ref Ltvrh.imagesThumbs);
      }
    }

    internal void SetupTVRecordingsLatest()
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

    internal void DisposeTVRecordingsLatest()
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
      bool Update = false;
      if (LatestMediaHandlerSetup.LatestTVRecordings.Equals("True", StringComparison.CurrentCulture))
      {
        try
        {
          switch (message.Message)
          {
            case GUIMessage.MessageType.GUI_MSG_PLAYBACK_ENDED:
            case GUIMessage.MessageType.GUI_MSG_PLAYBACK_STOPPED:
            {
              logger.Debug("Playback End/Stop detected: Refreshing latest.");
              Update = true;
              break;
            }
            case GUIMessage.MessageType.GUI_MSG_NOTIFY_REC:
            {
              logger.Debug("TV Recordings notify detected: Refreshing latest.");
              Update = true;
              break;
            }
          }
        }
        catch { }
        if (Update)
        {
          try
          {
            UpdateActiveRecordingsThread();
          }
          catch (Exception ex)
          {
            logger.Error("GUIWindowManager_OnNewMessage: " + ex.ToString());
          }
        }
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
        GUIFacadeControl facade = Utils.GetLatestsFacade(ControlID);
        if (facade != null && facade.Focus && facade.SelectedListItem != null)
        {
          idx = facade.SelectedListItem.ItemId;
        }
        //
        if (idx >= 0)
        {
          if (Utils.Used4TRTV && !Utils.UsedArgus)
          {
            action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_KEY_PRESSED;
            L4trrh.PlayRecording(idx);
          }
          else if (Utils.Used4TRTV && Utils.UsedArgus)
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
