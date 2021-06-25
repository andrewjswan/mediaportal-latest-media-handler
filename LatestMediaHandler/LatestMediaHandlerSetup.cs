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

using MediaPortal.Configuration;
using MediaPortal.GUI.Library;
using MediaPortal.Profile;

using Microsoft.Win32;

using LMHNLog.NLog;
using LMHNLog.NLog.Config;
using LMHNLog.NLog.Targets;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Timers;
using System.Windows.Forms;
using System.Xml;
using System.Xml.XPath;
using System.Linq;
using System.Threading.Tasks;

namespace LatestMediaHandler
{
  [PluginIcons("LatestMediaHandler.LatestMediaHandler_Icon.png", "LatestMediaHandler.LatestMediaHandler_Icon_Disabled.png")]

  public class LatestMediaHandlerSetup : IPlugin, ISetupForm
  {
    #region Declarations

    private static readonly object Locker = new object();

    /*
         * Log declarations
    */
    private static Logger logger = LogManager.GetCurrentClassLogger(); // log
    private const string LogFileName = "LatestMediaHandler.log";       // log's filename
    private const string OldLogFileName = "LatestMediaHandler.bak";    // log's old filename        

    /*
         * All Threads and Timers
    */
    private static Utils.Priority lmhThreadPriority = Utils.Priority.Lowest;
    private LatestMediaHandlerConfig xconfig = null;
    private LatestReorgWorker MyLatestReorgWorker = null;
    private static List<object> LatestsHandlers = null;
    private static int reorgTimerTick;
    private static string mpVersion = null;

    private Hashtable windowsUsingFanartLatest; // Used to know what skin files that supports latest media fanart     

    private List<LatestsFacade> ControlIDFacades;
    private List<int> ControlIDPlays;

    internal static System.Timers.Timer ReorgTimer = null;
    internal System.Timers.Timer RefreshTimer = null;
    internal static bool Starting = true;

    #endregion

    /*
     * 919198710 - LatestMyVideosHandler.ControlID
     * 919199970 - LatestMusicHandler.ControlID
     * 919199710 - LatestPictureHandler.ControlID
     * 919199940 - LatestTVSeriesHandler.ControlID
     * 919199910 - LatestMovingPicturesHandler.ControlID
     * 919199880 - LatestMyFilmsHandler.ControlID
     * 919299280 - LatestMvCentralHandler.ControlID
     * 919199840 - LatestTVAllRecordingsHandler.ControlID 
                 - if (Utils.usedArgus) largusrh else Ltvrh
    */

    internal static int ReorgTimerTick
    {
      get { return LatestMediaHandlerSetup.reorgTimerTick; }
      set { LatestMediaHandlerSetup.reorgTimerTick = value; }
    }

    internal static List<object> Handlers
    {
      get { return LatestsHandlers; }
    }

    internal static object GetMainHandler(Utils.LatestsCategory type)
    {
      if (LatestsHandlers == null)
      {
        return null;
      }

      try
      {
        foreach (object obj in LatestsHandlers)
        {
          if (obj == null)
          {
            continue;
          }

          if (type == Utils.LatestsCategory.MovingPictures && obj is LatestMovingPicturesHandler)
          {
            return obj;
          }
          else if (type == Utils.LatestsCategory.Movies && obj is LatestMyVideosHandler)
          {
            return obj;
          }
          else if (type == Utils.LatestsCategory.MvCentral && obj is LatestMvCentralHandler)
          {
            return obj;
          }
          else if (type == Utils.LatestsCategory.TVSeries && obj is LatestTVSeriesHandler)
          {
            return obj;
          }
          else if (type == Utils.LatestsCategory.Music && obj is LatestMusicHandler)
          {
            return obj;
          }
          else if (type == Utils.LatestsCategory.TV && obj is LatestTVAllRecordingsHandler)
          {
            return obj;
          }
          else if (type == Utils.LatestsCategory.Pictures && obj is LatestPictureHandler)
          {
            return obj;
          }
          else if (type == Utils.LatestsCategory.MyFilms && obj is LatestMyFilmsHandler)
          {
            return obj;
          }
        }
      }
      catch (Exception ex)
      {
        logger.Error("GetMainHandler: " + ex.ToString());
      }
      return null;
    }

    internal Hashtable WindowsUsingFanartLatest
    {
      get { return windowsUsingFanartLatest; }
      set { windowsUsingFanartLatest = value; }
    }

    internal static Utils.Priority LMHThreadPriority
    {
      get { return lmhThreadPriority; }
      set { lmhThreadPriority = value; }
    }

    public static string MpVersion
    {
      get { return mpVersion; }
      set { mpVersion = value; }
    }

    internal static int GetReorgTimerInterval()
    {
      int newTick = Environment.TickCount - ReorgTimerTick;
      try
      {
        newTick = (Int32.Parse(Utils.ReorgInterval) * 60000) - newTick;
        if (newTick < 0)
        {
          newTick = 2000;
        }
      }
      catch
      {
        newTick = 2000;
      }
      return newTick;
    }

    private void UpdateReorgTimer(Object stateInfo, ElapsedEventArgs e)
    {
      if (!Utils.IsStopping)
      {
        try
        {
          if (Interlocked.CompareExchange(ref Utils.SyncPointReorg, 1, 0) == 0)
          {
            // No other event was executing.                                                      
            if (MyLatestReorgWorker == null)
            {
              MyLatestReorgWorker = new LatestReorgWorker();
              MyLatestReorgWorker.RunWorkerCompleted += MyLatestReorgWorker.OnRunWorkerCompleted;
            }
            MyLatestReorgWorker.RunWorkerAsync();
          }
        }
        catch (Exception ex)
        {
          Utils.SyncPointReorg = 0;
          logger.Error("UpdateReorgTimer: " + ex.ToString());
        }
      }
    }

    #region Init Handlers
    private void InitHandlers()
    {
      try // Fanart Handler
      {
        UtilsFanartHandler.SetupFanartHandlerSubcribeScaperFinishedEvent();
      }
      catch { }

      if (LatestsHandlers == null)
      {
        return;
      }

      foreach (object obj in LatestsHandlers)
      {
        if (obj == null)
        {
          continue;
        }

        if (obj is LatestMovingPicturesHandler && Utils.LatestMovingPictures)
        {
          ((LatestMovingPicturesHandler)obj).SetupReceivers();
        }
        else if (obj is LatestMyVideosHandler && Utils.LatestMyVideos)
        {
          ((LatestMyVideosHandler)obj).SetupReceivers();
        }
        else if (obj is LatestMvCentralHandler && Utils.LatestMvCentral)
        {
          ((LatestMvCentralHandler)obj).SetupReceivers();
        }
        else if (obj is LatestTVSeriesHandler && Utils.LatestTVSeries)
        {
          ((LatestTVSeriesHandler)obj).SetupReceivers();
        }
        else if (obj is LatestMusicHandler && Utils.LatestMusic)
        {
          ((LatestMusicHandler)obj).SetupReceivers();
        }
        else if (obj is LatestTVAllRecordingsHandler && Utils.LatestTVRecordings)
        {
          ((LatestTVAllRecordingsHandler)obj).SetupReceivers();
        }
        else if (obj is LatestPictureHandler && Utils.LatestPictures)
        {
          ((LatestPictureHandler)obj).SetupReceivers();
        }
        else if (obj is LatestMyFilmsHandler && Utils.LatestMyFilms)
        {
          ((LatestMyFilmsHandler)obj).SetupReceivers();
        }
      }
    }
    #endregion

    #region Dispose Handlers
    private void DisposeHandlers()
    {
      try // Fanart Handler
      {
        UtilsFanartHandler.DisposeFanartHandlerSubcribeScaperFinishedEvent();
      }
      catch { }

      if (LatestsHandlers == null)
      {
        return;
      }

      foreach (object obj in LatestsHandlers)
      {
        if (obj == null)
        {
          continue;
        }

        if (obj is LatestMovingPicturesHandler && Utils.LatestMovingPictures)
        {
          ((LatestMovingPicturesHandler)obj).DisposeReceivers();
        }
        else if (obj is LatestMyVideosHandler && Utils.LatestMyVideos)
        {
          ((LatestMyVideosHandler)obj).DisposeReceivers();
        }
        else if (obj is LatestMvCentralHandler && Utils.LatestMvCentral)
        {
          ((LatestMvCentralHandler)obj).DisposeReceivers();
        }
        else if (obj is LatestTVSeriesHandler && Utils.LatestTVSeries)
        {
          ((LatestTVSeriesHandler)obj).DisposeReceivers();
        }
        else if (obj is LatestMusicHandler && Utils.LatestMusic)
        {
          ((LatestMusicHandler)obj).DisposeReceivers();
        }
        else if (obj is LatestTVAllRecordingsHandler && Utils.LatestTVRecordings)
        {
          ((LatestTVAllRecordingsHandler)obj).DisposeReceivers();
        }
        else if (obj is LatestPictureHandler && Utils.LatestPictures)
        {
          ((LatestPictureHandler)obj).DisposeReceivers();
        }
        else if (obj is LatestMyFilmsHandler && Utils.LatestMyFilms)
        {
          ((LatestMyFilmsHandler)obj).DisposeReceivers();
        }
      }
    }
    #endregion

    /// <summary>
    /// Set start values on variables
    /// </summary>
    private void SetupVariables()
    {
      Utils.IsStopping = false;

      LatestsHandlers = new List<object>();
      LatestsHandlers.Add(new LatestMusicHandler());
      LatestsHandlers.Add(new LatestMyVideosHandler());
      LatestsHandlers.Add(new LatestTVSeriesHandler());
      LatestsHandlers.Add(new LatestPictureHandler());
      LatestsHandlers.Add(new LatestMovingPicturesHandler());
      LatestsHandlers.Add(new LatestMyFilmsHandler());
      LatestsHandlers.Add(new LatestMvCentralHandler());
      LatestsHandlers.Add(new LatestTVAllRecordingsHandler());

      ReorgTimerTick = Environment.TickCount;

      Utils.SetProperty("#latestMediaHandler.scanned", "false");

      Utils.SyncPointInit();
    }

    internal void AddControlsIDs()
    {
      ControlIDFacades = new List<LatestsFacade>();
      ControlIDPlays = new List<int>();

      if (LatestsHandlers == null)
      {
        return;
      }

      foreach (object obj in LatestsHandlers)
      {
        if (obj == null)
        {
          continue;
        }

        if (obj is LatestMovingPicturesHandler)
        {
          ControlIDFacades.AddRange(((LatestMovingPicturesHandler)obj).ControlIDFacades);
          ControlIDPlays.AddRange(((LatestMovingPicturesHandler)obj).ControlIDPlays);
        }
        else if (obj is LatestMyVideosHandler)
        {
          ControlIDFacades.AddRange(((LatestMyVideosHandler)obj).ControlIDFacades);
          ControlIDPlays.AddRange(((LatestMyVideosHandler)obj).ControlIDPlays);
        }
        else if (obj is LatestMvCentralHandler)
        {
          ControlIDFacades.AddRange(((LatestMvCentralHandler)obj).ControlIDFacades);
          ControlIDPlays.AddRange(((LatestMvCentralHandler)obj).ControlIDPlays);
        }
        else if (obj is LatestTVSeriesHandler)
        {
          ControlIDFacades.AddRange(((LatestTVSeriesHandler)obj).ControlIDFacades);
          ControlIDPlays.AddRange(((LatestTVSeriesHandler)obj).ControlIDPlays);
        }
        else if (obj is LatestMusicHandler)
        {
          ControlIDFacades.AddRange(((LatestMusicHandler)obj).ControlIDFacades);
          ControlIDPlays.AddRange(((LatestMusicHandler)obj).ControlIDPlays);
        }
        else if (obj is LatestTVAllRecordingsHandler)
        {
          ControlIDFacades.AddRange(((LatestTVAllRecordingsHandler)obj).ControlIDFacades);
          ControlIDPlays.AddRange(((LatestTVAllRecordingsHandler)obj).ControlIDPlays);
        }
        else if (obj is LatestPictureHandler)
        {
          ControlIDFacades.AddRange(((LatestPictureHandler)obj).ControlIDFacades);
          ControlIDPlays.AddRange(((LatestPictureHandler)obj).ControlIDPlays);
        }
        else if (obj is LatestMyFilmsHandler)
        {
          ControlIDFacades.AddRange(((LatestMyFilmsHandler)obj).ControlIDFacades);
          ControlIDPlays.AddRange(((LatestMyFilmsHandler)obj).ControlIDPlays);
        }
      }
      // logger.Debug("*** Init active facade controls: "+string.Join(" ", ControlIDFacades.Select(x => x.ControlID.ToString())));
      // logger.Debug("*** Init active button controls: "+string.Join(" ", ControlIDPlays));
    }

    internal void EmptyLatestMediaProperties()
    {
      if (LatestsHandlers == null)
      {
        return;
      }

      foreach (object obj in LatestsHandlers)
      {
        if (obj == null)
        {
          continue;
        }

        if (obj is LatestMovingPicturesHandler)
          ((LatestMovingPicturesHandler)obj).EmptyLatestMediaProperties();
        else if (obj is LatestMyVideosHandler)
          ((LatestMyVideosHandler)obj).EmptyLatestMediaProperties();
        else if (obj is LatestMvCentralHandler)
          ((LatestMvCentralHandler)obj).EmptyLatestMediaProperties();
        else if (obj is LatestTVSeriesHandler)
          ((LatestTVSeriesHandler)obj).EmptyLatestMediaProperties();
        else if (obj is LatestMusicHandler)
          ((LatestMusicHandler)obj).EmptyLatestMediaProperties();
        else if (obj is LatestTVAllRecordingsHandler)
        {
          ((LatestTVAllRecordingsHandler)obj).EmptyLatestMediaProperties();
          ((LatestTVAllRecordingsHandler)obj).EmptyRecordingProps();
        }
        else if (obj is LatestPictureHandler)
          ((LatestPictureHandler)obj).EmptyLatestMediaProperties();
        else if (obj is LatestMyFilmsHandler)
          ((LatestMyFilmsHandler)obj).EmptyLatestMediaProperties();
      }
    }

    /// <summary>
    /// Setup logger. This funtion made by the team behind Moving Pictures 
    /// (http://code.google.com/p/moving-pictures/)
    /// </summary>
    private void InitLogger()
    {
      LoggingConfiguration logLatestMediaHandlerConfiguration = LogManager.Configuration ?? new LoggingConfiguration();

      try
      {
        FileInfo logFile = new FileInfo(Config.GetFile(Config.Dir.Log, LogFileName));
        if (logFile.Exists)
        {
          if (File.Exists(Config.GetFile(Config.Dir.Log, OldLogFileName)))
            File.Delete(Config.GetFile(Config.Dir.Log, OldLogFileName));

          logFile.CopyTo(Config.GetFile(Config.Dir.Log, OldLogFileName));
          logFile.Delete();
        }
      }
      catch (Exception) { }

      FileTarget fileTarget = new FileTarget()
      {
        FileName = Config.GetFile((Config.Dir)1, LogFileName),
        Name = "latestmedia-handler",
        Encoding = "utf-8",
        Layout = "${date:format=dd-MMM-yyyy HH\\:mm\\:ss} ${level:fixedLength=true:padding=5} [${logger:fixedLength=true:padding=20:shortName=true}]: ${message} ${exception:format=tostring}"
        // Layout = "${date:format=dd-MMM-yyyy HH\\:mm\\:ss.fff} ${level:fixedLength=true:padding=5} [${logger:fixedLength=true:padding=20:shortName=true}]: ${message} ${exception:format=tostring}"
      };
      logLatestMediaHandlerConfiguration.AddTarget("latestmedia-handler", fileTarget);

      // Get current Log Level from MediaPortal 
      LogLevel logLevel = LogLevel.Debug;
      string threadPriority = "Normal";
      int intLogLevel = 3;

      using (Settings xmlreader = new MPSettings())
      {
        threadPriority = xmlreader.GetValueAsString("general", "ThreadPriority", threadPriority);
        intLogLevel = xmlreader.GetValueAsInt("general", "loglevel", intLogLevel);
      }

      switch (intLogLevel)
      {
        case 0:
          logLevel = LogLevel.Error;
          break;
        case 1:
          logLevel = LogLevel.Warn;
          break;
        case 2:
          logLevel = LogLevel.Info;
          break;
        default:
          logLevel = LogLevel.Debug;
          break;
      }
      #if DEBUG
      logLevel = LogLevel.Debug;
      #endif

      LMHThreadPriority = string.IsNullOrEmpty(threadPriority) || !threadPriority.Equals("Normal", StringComparison.CurrentCulture) ?
                            (string.IsNullOrEmpty(threadPriority) || !threadPriority.Equals("BelowNormal", StringComparison.CurrentCulture) ?
                              Utils.Priority.BelowNormal :
                              Utils.Priority.Lowest) :
                            Utils.Priority.Lowest;


      LoggingRule loggingRule = new LoggingRule("LatestMediaHandler.*", logLevel, fileTarget);
      // LoggingRule loggingRule = new LoggingRule("*", logLevel, fileTarget);
      logLatestMediaHandlerConfiguration.LoggingRules.Add(loggingRule);

      LogManager.Configuration = logLatestMediaHandlerConfiguration;
    }

    /// <summary>
    /// The plugin is started by Mediaportal
    /// </summary>
    public void Start()
    {
      try
      {
        Utils.DelayStop = new Hashtable();
        Utils.IsStopping = false;

        InitLogger();
        logger.Info("Latest Media Handler is starting...");
        logger.Info("Latest Media Handler version is " + Utils.GetAllVersionNumber());

        MpVersion = FileVersionInfo.GetVersionInfo(Application.ExecutablePath).ProductVersion;
        logger.Info("MediaPortal version is " + MpVersion);
        MpVersion = FileVersionInfo.GetVersionInfo(Application.ExecutablePath).FileVersion;

        Translation.Init();
        SetupConfigFile();
        Utils.LoadSettings();

        SetupWindowsUsingLatestMediaHandlerVisibility();
        SetupVariables();

        Utils.LoadSkinSettings();
        AddControlsIDs();

        GUIWindowManager.OnActivateWindow += new GUIWindowManager.WindowActivationHandler(GuiWindowManagerOnActivateWindow);
        GUIGraphicsContext.OnNewAction += new OnActionHandler(OnNewAction);
        GUIWindowManager.Receivers += new SendMessageHandler(OnMessage);
        SystemEvents.PowerModeChanged += new PowerModeChangedEventHandler(OnSystemPowerModeChanged);

        InitHandlers();
        GetLatestMediaInfo();

        ReorgTimer = new System.Timers.Timer((Int32.Parse(Utils.ReorgInterval) * 60000));
        ReorgTimer.Elapsed += new ElapsedEventHandler(UpdateReorgTimer);
        ReorgTimer.Interval = (Int32.Parse(Utils.ReorgInterval) * 60000);
        ReorgTimer.Start();

        RefreshTimer = new System.Timers.Timer(250);
        RefreshTimer.Elapsed += new ElapsedEventHandler(UpdateImageTimer);
        RefreshTimer.Interval = 250;
        RefreshTimer.Start();

        Starting = false;

        logger.Info("Latest Media Handler is started.");
        OnActivateTask(GUIWindowManager.ActiveWindow);
      }
      catch (Exception ex)
      {
        logger.Error("Start: " + ex.ToString());
      }
    }

    private void GetLatestMediaInfo(string Mode = "Start", Utils.Category Level = Utils.Category.All)
    {
      Utils.HasNewInit();

      // Level 0 - All, 1 - Video, 2 - Music, 3 - Pictures, 4 - TV
      if ((Level == Utils.Category.All) || (Level == Utils.Category.Video))
      {
        GetLatestMediaInfoVideo(Mode);
      }
      if ((Level == Utils.Category.All) || (Level == Utils.Category.Music))
      {
        GetLatestMediaInfoMusic(Mode);
      }
      if ((Level == Utils.Category.All) || (Level == Utils.Category.Pictures))
      {
        GetLatestMediaInfoPictures(Mode);
      }
      if ((Level == Utils.Category.All) || (Level == Utils.Category.TV))
      {
        GetLatestMediaInfoTV(Mode);
      }
    }

    private static void GetLatestMediaInfoMusic(string Mode = "Update")
    {
      if (!Utils.LatestMusic)
      {
        return;
      }
      if (LatestsHandlers == null)
      {
        return;
      }

      // Music
      try
      {
        foreach (object obj in LatestsHandlers)
        {
          if (obj == null)
          {
            continue;
          }

          if (obj is LatestMusicHandler)
          {
            ((LatestMusicHandler)obj).GetLatestMediaInfoThread();
          }
        }
      }
      catch (Exception ex)
      {
        logger.Error("GetLatestMediaInfoMusic [" + Mode + "]: " + ex.ToString());
      }
    }

    private static void GetLatestMediaInfoPictures(string Mode = "Update")
    {
      if (!Utils.LatestPictures)
      {
        return;
      }
      if (LatestsHandlers == null)
      {
        return;
      }

      // Pictures
      try
      {
        foreach (object obj in LatestsHandlers)
        {
          if (obj == null)
          {
            continue;
          }

          if (obj is LatestPictureHandler)
          {
            ((LatestPictureHandler)obj).GetLatestMediaInfoThread();
          }
        }
      }
      catch (Exception ex)
      {
        logger.Error("GetLatestMediaInfoPictures [" + Mode + "]: " + ex.ToString());
      }
    }

    private static void GetLatestMediaInfoTV(string Mode = "Update")
    {
      if (!Utils.LatestTVRecordings)
      {
        return;
      }
      if (LatestsHandlers == null)
      {
        return;
      }
      // TV Record
      try
      {
        foreach (object obj in LatestsHandlers)
        {
          if (obj == null)
          {
            continue;
          }

          if (obj is LatestTVAllRecordingsHandler)
          {
            ((LatestTVAllRecordingsHandler)obj).GetTVRecordings();
          }
        }
      }
      catch (Exception ex)
      {
        logger.Error("GetLatestMediaInfoTV [" + Mode + "]: " + ex.ToString());
      }
    }

    private static void GetLatestMediaInfoVideo(string Mode = "Update")
    {
      if (LatestsHandlers == null)
      {
        return;
      }

      try
      {
        foreach (object obj in LatestsHandlers)
        {
          if (obj == null)
          {
            continue;
          }

          // MyVideo
          if (Utils.LatestMyVideos && obj is LatestMyVideosHandler)
          {
            ((LatestMyVideosHandler)obj).GetLatestMediaInfoThread();
          }
          // TVSeries
          if (Utils.LatestTVSeries && obj is LatestTVSeriesHandler)
          {
            ((LatestTVSeriesHandler)obj).GetLatestMediaInfoThread();
          }
          // Moving Pictures
          if (Utils.LatestMovingPictures && obj is LatestMovingPicturesHandler)
          {
            ((LatestMovingPicturesHandler)obj).GetLatestMediaInfoThread();
          }
          // MyFilms
          if (Utils.LatestMyFilms && obj is LatestMyFilmsHandler)
          {
            ((LatestMyFilmsHandler)obj).GetLatestMediaInfoThread();
          }
          // mvCentral
          if (Utils.LatestMvCentral && obj is LatestMvCentralHandler)
          {
            ((LatestMvCentralHandler)obj).GetLatestMediaInfoThread();
          }
        }
      }
      catch (Exception ex)
      {
        logger.Error("GetLatestMediaInfoVideo [" + Mode + "]: " + ex.ToString());
      }
    }

    private void OnSystemPowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
      try
      {
        if (e.Mode == PowerModes.Resume)
        {
          logger.Info("LatestMediaHandler is resuming from standby/hibernate.");

          GetLatestMediaInfo();
        }
        else if (e.Mode == PowerModes.Suspend)
        {
          logger.Info("LatestMediaHandler is suspending/hibernating...");
          // StopTasks(true);
          logger.Info("LatestMediaHandler is suspended/hibernated.");
        }
      }
      catch (Exception ex)
      {
        logger.Error("OnSystemPowerModeChanged: " + ex);
      }
    }

    internal static void TriggerGetLatestMediaInfoOnEvent(string type, string artist)
    {
      try
      {
        if (LatestsHandlers != null && !string.IsNullOrEmpty(artist))
        {
          // Music
          bool needUpdate = false;
          foreach (object obj in LatestsHandlers)
          {
            if (obj == null)
            {
              continue;
            }

            if (obj is LatestMusicHandler)
            {
              needUpdate = needUpdate || ((LatestMusicHandler)obj).artistsWithImageMissing != null && ((LatestMusicHandler)obj).artistsWithImageMissing.Contains(artist);
            }
          }

          if (needUpdate)
          {
            logger.Info("Received new scraper event from FanartHandler plugin for artist " + artist + ".");
            GetLatestMediaInfoMusic("Fanart");
          }
        }
      }
      catch (Exception ex)
      {
        logger.Error("TriggerGetLatestMediaInfoOnEvent: " + ex.ToString());
      }
    }

    private void DoContextMenu()
    {
      GUIWindow fWindow = GUIWindowManager.GetWindow(Utils.ActiveWindow);
      if (fWindow == null)
      {
        return;
      }

      int FocusControlID = fWindow.GetFocusControlId();
      if (!ControlIDFacades.Any(facade => facade.ControlID == FocusControlID) && !ControlIDPlays.Contains(FocusControlID))
      {
        return;
      }

      if (LatestsHandlers == null)
      {
        return;
      }

      try
      {
        foreach (object obj in LatestsHandlers)
        {
          if (obj == null)
          {
            continue;
          }

          if (obj is LatestMovingPicturesHandler)
          {
            if (((LatestMovingPicturesHandler)obj).ControlIDFacades.Any(facade => facade.ControlID == FocusControlID) || ((LatestMovingPicturesHandler)obj).ControlIDPlays.Contains(FocusControlID))
            {
              ((LatestMovingPicturesHandler)obj).MyContextMenu();
            }
          }
          else if (obj is LatestMyVideosHandler)
          {
            if (((LatestMyVideosHandler)obj).ControlIDFacades.Any(facade => facade.ControlID == FocusControlID) || ((LatestMyVideosHandler)obj).ControlIDPlays.Contains(FocusControlID))
            {
              ((LatestMyVideosHandler)obj).MyContextMenu();
            }
          }
          else if (obj is LatestMvCentralHandler)
          {
            if (((LatestMvCentralHandler)obj).ControlIDFacades.Any(facade => facade.ControlID == FocusControlID) || ((LatestMvCentralHandler)obj).ControlIDPlays.Contains(FocusControlID))
            {
              ((LatestMvCentralHandler)obj).MyContextMenu();
            }
          }
          else if (obj is LatestTVSeriesHandler)
          {
            if (((LatestTVSeriesHandler)obj).ControlIDFacades.Any(facade => facade.ControlID == FocusControlID) || ((LatestTVSeriesHandler)obj).ControlIDPlays.Contains(FocusControlID))
            {
              ((LatestTVSeriesHandler)obj).MyContextMenu();
            }
          }
          else if (obj is LatestMusicHandler)
          {
            if (((LatestMusicHandler)obj).ControlIDFacades.Any(facade => facade.ControlID == FocusControlID) || ((LatestMusicHandler)obj).ControlIDPlays.Contains(FocusControlID))
            {
              ((LatestMusicHandler)obj).MyContextMenu();
            }
          }
          else if (obj is LatestTVAllRecordingsHandler)
          {
            if (((LatestTVAllRecordingsHandler)obj).ControlIDFacades.Any(facade => facade.ControlID == FocusControlID) || ((LatestTVAllRecordingsHandler)obj).ControlIDPlays.Contains(FocusControlID))
            {
              ((LatestTVAllRecordingsHandler)obj).MyContextMenu();
            }
          }
          else if (obj is LatestPictureHandler)
          {
            if (((LatestPictureHandler)obj).ControlIDFacades.Any(facade => facade.ControlID == FocusControlID) || ((LatestPictureHandler)obj).ControlIDPlays.Contains(FocusControlID))
            {
              ((LatestPictureHandler)obj).MyContextMenu();
            }
          }
          else if (obj is LatestMyFilmsHandler)
          {
            if (((LatestMyFilmsHandler)obj).ControlIDFacades.Any(facade => facade.ControlID == FocusControlID) || ((LatestMyFilmsHandler)obj).ControlIDPlays.Contains(FocusControlID))
            {
              ((LatestMyFilmsHandler)obj).MyContextMenu();
            }
          }
        }
      }
      catch (Exception ex)
      {
        logger.Error("DoContextMenu: " + ex.ToString());
      }
    }

    private void OnMessage(GUIMessage message)
    {
      Utils.ThreadToSleep();
      try
      {
        if (message.Object is ShowLMHDialog)
        {
          DoContextMenu();
        }
      }
      catch (Exception ex)
      { 
        logger.Error("OnMessage: " + ex.ToString());
      }

      switch (message.Message)
      {
        case GUIMessage.MessageType.GUI_MSG_WINDOW_INIT_DONE:
        {
          UpdateFacades();
        }
        break;
      }
    }

    internal void OnAction(GUIWindow fWindow, ref MediaPortal.GUI.Library.Action action, bool ContextMenu)
    {
      if (fWindow != null)
      {
        //
        int FocusControlID = fWindow.GetFocusControlId();
        if (ControlIDFacades.Any(facade => facade.ControlID == FocusControlID) || ControlIDPlays.Contains(FocusControlID))
        {
          if (ContextMenu)
          {
            if (action.IsUserAction())
            {
              GUIGraphicsContext.ResetLastActivity();
            }
            action.wID = 0;

            ShowLMHDialog slmhd = new ShowLMHDialog();
            GUIWindowManager.SendThreadMessage(new GUIMessage()
            {
              // TargetWindowId = (int)GUIWindow.Window.WINDOW_SECOND_HOME,
              TargetWindowId = fWindow.GetID,
              SendToTargetWindow = true,
              Object = slmhd
            });
            return;
          }
          else
          {
            LatestsPlay(fWindow, ref action);
            return;
          }
        }
      }
    }

    private void LatestsPlay(GUIWindow fWindow, ref MediaPortal.GUI.Library.Action action)
    {
      if (LatestsHandlers == null)
      {
        return;
      }

      try
      {
        foreach (object obj in LatestsHandlers)
        {
          if (obj == null)
          {
            continue;
          }

          if (obj is LatestMovingPicturesHandler)
          {
            if (((LatestMovingPicturesHandler)obj).PlayMovingPicture(fWindow)) return;
          }
          else if (obj is LatestMyVideosHandler)
          {
            if (((LatestMyVideosHandler)obj).PlayMovie(fWindow)) return;
          }
          else if (obj is LatestMvCentralHandler)
          {
            if (((LatestMvCentralHandler)obj).PlayMusicAlbum(fWindow)) return;
          }
          else if (obj is LatestTVSeriesHandler)
          {
            if (((LatestTVSeriesHandler)obj).PlayTVSeries(fWindow)) return;
          }
          else if (obj is LatestMusicHandler)
          {
            if (((LatestMusicHandler)obj).PlayMusicAlbum(fWindow)) return;
          }
          else if (obj is LatestTVAllRecordingsHandler)
          {
            if (((LatestTVAllRecordingsHandler)obj).PlayRecording(fWindow, ref action)) return;
          }
          else if (obj is LatestPictureHandler)
          {
            if (((LatestPictureHandler)obj).PlayPictures(fWindow)) return;
          }
          else if (obj is LatestMyFilmsHandler)
          {
            if (((LatestMyFilmsHandler)obj).PlayMovie(fWindow)) return;
          }
        }
      }
      catch (Exception ex)
      {
        logger.Error("LatestsPlay: " + ex.ToString());
      }
    }

    private void OnNewAction(MediaPortal.GUI.Library.Action action)
    {
      bool Action = false;
      bool Context = false;

      if (action == null)
        return;

      try
      {
        // if ((Utils.ActiveWindow == (int)GUIWindow.Window.WINDOW_SECOND_HOME) && (GUIWindowManager.RoutedWindow == -1))
        if (WindowsUsingFanartLatest.ContainsKey(Utils.ActiveWindowStr) && (GUIWindowManager.RoutedWindow == -1))
        {
          GUIWindow fWindow = GUIWindowManager.GetWindow(Utils.ActiveWindow);
          if (fWindow == null)
            return;

          switch (action.wID)
          {
            case MediaPortal.GUI.Library.Action.ActionType.ACTION_CONTEXT_MENU:
              {
                Action = true;
                Context = true;
                logger.Debug("OnNewAction: ACTION_CONTEXT_MENU");
                break;
              }
            case MediaPortal.GUI.Library.Action.ActionType.ACTION_MOUSE_CLICK:
              {
                Action = (action.MouseButton == MouseButtons.Left) || (action.MouseButton == MouseButtons.Right);
                Context = (action.MouseButton == MouseButtons.Right);
                logger.Debug("OnNewAction: ACTION_MOUSE_CLICK [" + ((action.MouseButton == MouseButtons.Left) ? "L" : (action.MouseButton == MouseButtons.Right) ? "R" : "U") + "]");
                break;
              }
            case MediaPortal.GUI.Library.Action.ActionType.ACTION_SELECT_ITEM:
              {
                Action = true;
                Context = false;
                logger.Debug("OnNewAction: ACTION_SELECT_ITEM");
                break;
              }
            default:
              break;
          }

          if (Action)
          {
            OnAction(fWindow, ref action, Context);
          }
        }
      }
      catch (Exception ex)
      {
        logger.Error("OnNewAction: " + ex.ToString());
      }
    }

    private void InitFacade()
    {
      if (LatestsHandlers == null)
      {
        return;
      }
      if (Utils.ActiveWindow <= (int)GUIWindow.Window.WINDOW_INVALID)
      {
        return;
      }

      try
      {
        List<Task> TaskList = new List<Task>();
        foreach (object obj in LatestsHandlers)
        {
          if (obj == null)
          {
            continue;
          }

          Task task = null;
          if (obj is LatestMovingPicturesHandler)
          {
            // logger.debug("*** InitFacade: LatestMovingPicturesHandler " + windowId);
            task = new Task(((LatestMovingPicturesHandler)obj).InitFacade);
          }
          else if (obj is LatestMyVideosHandler)
          {
            // logger.debug("*** InitFacade: LatestMyVideosHandler " + windowId);
            task = new Task(((LatestMyVideosHandler)obj).InitFacade);
          }
          else if (obj is LatestMvCentralHandler)
          {
            // logger.debug("*** InitFacade: LatestMvCentralHandler " + windowId);
            task = new Task(((LatestMvCentralHandler)obj).InitFacade);
          }
          else if (obj is LatestTVSeriesHandler)
          {
            // logger.debug("*** InitFacade: LatestTVSeriesHandler " + windowId);
            task = new Task(((LatestTVSeriesHandler)obj).InitFacade);
          }
          else if (obj is LatestMusicHandler)
          {
            // logger.debug("*** InitFacade: LatestMusicHandler " + windowId);
            task = new Task(((LatestMusicHandler)obj).InitFacade);
          }
          else if (obj is LatestTVAllRecordingsHandler)
          {
            // logger.debug("*** InitFacade: LatestTVAllRecordingsHandler " + windowId);
            task = new Task(((LatestTVAllRecordingsHandler)obj).InitFacade);
          }
          else if (obj is LatestPictureHandler)
          {
            // logger.debug("*** InitFacade: LatestPictureHandler " + windowId);
            task = new Task(((LatestPictureHandler)obj).InitFacade);
          }
          else if (obj is LatestMyFilmsHandler)
          {
            // logger.debug("*** InitFacade: LatestMyFilmsHandler " + windowId);
            task = new Task(((LatestMyFilmsHandler)obj).InitFacade);
          }
          if (task != null)
          {
            task.Start();
            TaskList.Add(task);
          }
        }
        Task.WaitAll(TaskList.ToArray());
      }
      catch (Exception ex)
      {
        logger.Error("InitFacade: " + ex.ToString());
      }
    }

    internal void DeInitFacade()
    {
      if (LatestsHandlers == null)
      {
        return;
      }
      if (Utils.ActiveWindow <= (int)GUIWindow.Window.WINDOW_INVALID)
      {
        return;
      }

      try
      {
        List<Task> TaskList = new List<Task>();
        foreach (object obj in LatestsHandlers)
        {
          if (obj == null)
          {
            continue;
          }

          Task task = null;
          if (obj is LatestMovingPicturesHandler)
          {
            task = new Task(((LatestMovingPicturesHandler)obj).DeInitFacade);
          }
          else if (obj is LatestMyVideosHandler)
          {
            task = new Task(((LatestMyVideosHandler)obj).DeInitFacade);
          }
          else if (obj is LatestMvCentralHandler)
          {
            task = new Task(((LatestMvCentralHandler)obj).DeInitFacade);
          }
          else if (obj is LatestTVSeriesHandler)
          {
            task = new Task(((LatestTVSeriesHandler)obj).DeInitFacade);
          }
          else if (obj is LatestMusicHandler)
          {
            task = new Task(((LatestMusicHandler)obj).DeInitFacade);
          }
          else if (obj is LatestTVAllRecordingsHandler)
          {
            task = new Task(((LatestTVAllRecordingsHandler)obj).DeInitFacade);
          }
          else if (obj is LatestPictureHandler)
          {
            task = new Task(((LatestPictureHandler)obj).DeInitFacade);
          }
          else if (obj is LatestMyFilmsHandler)
          {
            task = new Task(((LatestMyFilmsHandler)obj).DeInitFacade);
          }
          if (task != null)
          {
            task.Start();
            TaskList.Add(task);
          }
        }
        Task.WaitAll(TaskList.ToArray());
      }
      catch (Exception ex)
      {
        logger.Error("DeInitFacade: " + ex.ToString());
      }
    }

    internal void GuiWindowManagerOnActivateWindow(int activeWindowId)
    {
      OnActivateTask(activeWindowId);
    }

    internal void GuiWindowManagerOnDeActivateWindow(int deActiveWindowId)
    {
      Utils.ActiveWindow = (int)GUIWindow.Window.WINDOW_INVALID;
    }

    private void OnActivateTask(int activeWindowId)
    {
      if (activeWindowId <= (int)GUIWindow.Window.WINDOW_INVALID)
      {
        return;
      }

      Utils.ActiveWindow = activeWindowId;
      logger.Debug("Activate Window: " + Utils.ActiveWindowStr + " " + GUIWindowManager.IsSwitchingToNewWindow);

      if (MpVersion.CompareTo("1.19") < 0)
      {
        UpdateFacades();
      }
    }

    private void StartTimers()
    {
      if (ReorgTimer != null && !ReorgTimer.Enabled)
      {
        ReorgTimer.Interval = GetReorgTimerInterval();
        ReorgTimer.Start();
      }
      if (RefreshTimer != null && !RefreshTimer.Enabled)
      {
        RefreshTimer.Start();
      }
    }

    private void StopTimers()
    {
      if (RefreshTimer != null && RefreshTimer.Enabled)
      {
        RefreshTimer.Stop();
      }
      if (ReorgTimer != null && ReorgTimer.Enabled)
      {
        ReorgTimer.Stop();
      }
    }

    private void UpdateFacades()
    {
      try
      {
        if (!Utils.IsStopping && WindowsUsingFanartLatest.ContainsKey(Utils.ActiveWindowStr))
        {
          // Start facade in thread
          new Thread(() =>
          {
            // logger.Debug("Update facades begin: " + Utils.ActiveWindowStr);
            InitFacade();
            StartTimers();
            GetLatestMediaInfoTV("WindowActivate");
            // logger.Debug("Update facades end: " + Utils.ActiveWindowStr);
          }).Start();
        }
        else
        {
          // Start facade in thread
          new Thread(() =>
          {
            // logger.Debug("Clean facades begin: " + Utils.ActiveWindowStr);
            StopTimers();
            DeInitFacade();
            // logger.Debug("Clean facades end: " + Utils.ActiveWindowStr);
          }).Start();
        }
      }
      catch (ThreadAbortException)
      {
      }
    }

    private void UpdateImageTimer(Object stateInfo, ElapsedEventArgs e)
    {
      if (LatestsHandlers == null)
      {
        return;
      }
      if (!Utils.IsIdle())
      {
        return;
      }
      if (Utils.ActiveWindow <= (int)GUIWindow.Window.WINDOW_INVALID)
      {
        return;
      }
      if (Interlocked.CompareExchange(ref Utils.SyncPointRefresh, 1, 0) != 0)
      {
        return;
      }

      try
      {
        GUIWindow fWindow = GUIWindowManager.GetWindow(Utils.ActiveWindow);
        if (fWindow == null)
        {
          Utils.SyncPointRefresh = 0;
          return;
        }

        foreach (object obj in LatestsHandlers)
        {
          if (obj == null)
          {
            continue;
          }

          if (obj is LatestMovingPicturesHandler)
          {
            ((LatestMovingPicturesHandler)obj).UpdateImageTimer(fWindow, stateInfo, e);
          }
          else if (obj is LatestMyVideosHandler)
          {
            ((LatestMyVideosHandler)obj).UpdateImageTimer(fWindow, stateInfo, e);
          }
          else if (obj is LatestMvCentralHandler)
          {
            ((LatestMvCentralHandler)obj).UpdateImageTimer(fWindow, stateInfo, e);
          }
          else if (obj is LatestTVSeriesHandler)
          {
            ((LatestTVSeriesHandler)obj).UpdateImageTimer(fWindow, stateInfo, e);
          }
          else if (obj is LatestMusicHandler)
          {
            ((LatestMusicHandler)obj).UpdateImageTimer(fWindow, stateInfo, e);
          }
          else if (obj is LatestTVAllRecordingsHandler)
          {
            ((LatestTVAllRecordingsHandler)obj).UpdateImageTimer(fWindow, stateInfo, e);
          }
          else if (obj is LatestPictureHandler)
          {
            ((LatestPictureHandler)obj).UpdateImageTimer(fWindow, stateInfo, e);
          }
          else if (obj is LatestMyFilmsHandler)
          {
            ((LatestMyFilmsHandler)obj).UpdateImageTimer(fWindow, stateInfo, e);
          }
        }
      }
      catch (Exception ex)
      {
        logger.Error("UpdateImageTimer: " + ex.ToString());
      }
      Utils.SyncPointRefresh = 0;
    }

    private void SetupConfigFile()
    {
    }

    /// <summary>
    /// The Plugin is stopped
    /// </summary>
    public void Stop()
    {
      try
      {
        StopTasks(false);
        logger.Info("Latest Media Handler is stopped.");
      }
      catch (Exception ex)
      {
        logger.Error("Stop: " + ex.ToString());
      }
    }

    private void StopTasks(bool suspending)
    {
      try
      {
        Utils.IsStopping = true;

        GUIWindowManager.OnActivateWindow -= new GUIWindowManager.WindowActivationHandler(GuiWindowManagerOnActivateWindow);
        GUIGraphicsContext.OnNewAction -= new OnActionHandler(OnNewAction);
        GUIWindowManager.Receivers -= new SendMessageHandler(OnMessage);
        try
        {
          if (!suspending)
          {
            SystemEvents.PowerModeChanged -= new PowerModeChangedEventHandler(OnSystemPowerModeChanged);
          }
        }
        catch { }

        int ix = 0;
        while (Utils.GetDelayStop() && ix < 20)
        {
          System.Threading.Thread.Sleep(500);
          ix++;
        }

        if (ReorgTimer != null)
        {
          ReorgTimer.Stop();
          ReorgTimer.Dispose();
        }
        if (MyLatestReorgWorker != null)
        {
          MyLatestReorgWorker.CancelAsync();
          MyLatestReorgWorker.Dispose();
        }
        if (RefreshTimer != null)
        {
          RefreshTimer.Stop();
          RefreshTimer.Dispose();
        }
        DisposeHandlers();

        Utils.DelayStop = new Hashtable();
      }
      catch (Exception ex)
      {
        logger.Error("Stop: " + ex.ToString());
      }
    }

    #region XMLs

    /// <summary>
    /// Get value from xml node
    /// </summary>
    private string GetNodeValue(XPathNodeIterator myXPathNodeIterator)
    {
      if (myXPathNodeIterator.Count > 0)
      {
        myXPathNodeIterator.MoveNext();
        return myXPathNodeIterator.Current.Value;
      }
      return String.Empty;
    }

    private void SetupWindowsUsingLatestMediaHandlerVisibility(string SkinDir = null, string ThemeDir = null)
    {
      XPathDocument myXPathDocument;
      XPathNavigator myXPathNavigator;
      XPathNodeIterator myXPathNodeIterator;

      string windowId = String.Empty;
      string sNodeValue = String.Empty;

      var path = string.Empty;
      var theme = string.Empty;

      if (string.IsNullOrEmpty(SkinDir))
      {
        WindowsUsingFanartLatest = new Hashtable();

        path = GUIGraphicsContext.Skin + @"\";
        theme = Utils.GetThemeFolder(path);
        logger.Debug("Scan Skin folder for XML: " + path);
      }
      else
      {
        path = ThemeDir;
        logger.Debug("Scan Skin Theme folder for XML: " + path);
      }

      DirectoryInfo di = new DirectoryInfo(path);
      FileInfo[] rgFiles = di.GetFiles("*.xml");

      var XMLName = string.Empty;

      foreach (FileInfo fi in rgFiles)
      {
        try
        {
          XMLName = fi.Name;
          var XMLFolder = fi.FullName.Substring(0, fi.FullName.LastIndexOf("\\"));

          myXPathDocument = new XPathDocument(fi.FullName);
          myXPathNavigator = myXPathDocument.CreateNavigator();
          myXPathNodeIterator = myXPathNavigator.Select("/window/id");
          windowId = GetNodeValue(myXPathNodeIterator);

          bool _flagLatest = false;
          if (!string.IsNullOrEmpty(windowId))
          {
            HandleXmlImports(fi.FullName, windowId, ref _flagLatest);

            myXPathNodeIterator = myXPathNavigator.Select("/window/controls/import");
            if (myXPathNodeIterator.Count > 0)
            {
              while (myXPathNodeIterator.MoveNext())
              {
                var XMLFullName = Path.Combine(XMLFolder, myXPathNodeIterator.Current.Value);
                if (File.Exists(XMLFullName))
                {
                  HandleXmlImports(XMLFullName, windowId, ref _flagLatest);

                  if (!string.IsNullOrEmpty(theme))
                  {
                    XMLFullName = Path.Combine(theme, myXPathNodeIterator.Current.Value);
                    if (File.Exists(XMLFullName))
                    {
                      HandleXmlImports(XMLFullName, windowId, ref _flagLatest);
                    }
                  }
                }
                else if ((!string.IsNullOrEmpty(SkinDir)) && (!string.IsNullOrEmpty(ThemeDir)))
                {
                  XMLFullName = Path.Combine(SkinDir, myXPathNodeIterator.Current.Value);
                  if (File.Exists(XMLFullName))
                  {
                    HandleXmlImports(XMLFullName, windowId, ref _flagLatest);
                  }
                }
              }
            }

            myXPathNodeIterator = myXPathNavigator.Select("/window/controls/include");
            if (myXPathNodeIterator.Count > 0)
            {
              while (myXPathNodeIterator.MoveNext())
              {
                var XMLFullName = Path.Combine(XMLFolder, myXPathNodeIterator.Current.Value);
                if (File.Exists(XMLFullName))
                {
                  HandleXmlImports(XMLFullName, windowId, ref _flagLatest);

                  if (!string.IsNullOrEmpty(theme))
                  {
                    XMLFullName = Path.Combine(theme, myXPathNodeIterator.Current.Value);
                    if (File.Exists(XMLFullName))
                    {
                      HandleXmlImports(XMLFullName, windowId, ref _flagLatest);
                    }
                  }
                }
                else if ((!string.IsNullOrEmpty(SkinDir)) && (!string.IsNullOrEmpty(ThemeDir)))
                {
                  XMLFullName = Path.Combine(SkinDir, myXPathNodeIterator.Current.Value);
                  if (File.Exists(XMLFullName))
                  {
                    HandleXmlImports(XMLFullName, windowId, ref _flagLatest);
                  }
                }
              }
            }

            if (_flagLatest)
            {
              if (!WindowsUsingFanartLatest.Contains(windowId))
              {
                WindowsUsingFanartLatest.Add(windowId, windowId);
              }
            }
          }
        }
        catch (Exception ex)
        {
          logger.Error("SetupWindowsUsingLatestMediaHandlerVisibility: " + (string.IsNullOrEmpty(ThemeDir) ? string.Empty : "Theme: " + ThemeDir + " ") + "Filename:" + XMLName);
          logger.Error(ex);
        }
      }

      if (string.IsNullOrEmpty(ThemeDir))
      {
        // Include Themes
        if (!string.IsNullOrEmpty(theme))
        {
          SetupWindowsUsingLatestMediaHandlerVisibility(path, theme);
        }
      }
    }

    private void HandleXmlImports(string filename, string windowId, ref bool _flagLatest)
    {
      XPathDocument myXPathDocument = new XPathDocument(filename);
      StringBuilder sb = new StringBuilder();
      using (XmlWriter xmlWriter = XmlWriter.Create(sb))
      {
        myXPathDocument.CreateNavigator().WriteSubtree(xmlWriter);
      }
      string _xml = sb.ToString();
      _flagLatest = _xml.Contains("#LatestMediaHandler:Yes") ? true : _flagLatest;
      _flagLatest = _xml.Contains("#latestMediaHandler.") && (_xml.Contains(".latest.") || _xml.Contains(".selected.")) ? true : _flagLatest;

      sb = null;
    }

    #endregion

    #region ISetupForm Members

    // Returns the name of the plugin which is shown in the plugin menu
    public string PluginName()
    {
      return "Latest Media Handler";
    }

    // Returns the description of the plugin is shown in the plugin menu
    public string Description()
    {
      return "Latest Media Handler for MediaPortal.";
    }

    // Returns the author of the plugin which is shown in the plugin menu
    public string Author()
    {
      return "ajs (maintained by yoavain, original by cul8er)";
    }

    // show the setup dialog
    public void ShowPlugin()
    {
      xconfig = new LatestMediaHandlerConfig();
      xconfig.ShowDialog();
    }

    // Indicates whether plugin can be enabled/disabled
    public bool CanEnable()
    {
      return true;
    }

    // Get Windows-ID
    public int GetWindowId()
    {
      // WindowID of windowplugin belonging to this setup
      // enter your own unique code
      return 730726;
    }

    // Indicates if plugin is enabled by default;
    public bool DefaultEnabled()
    {
      return true;
    }

    // indicates if a plugin has it's own setup screen
    public bool HasSetup()
    {
      return true;
    }

    /// <summary>
    /// If the plugin should have it's own button on the main menu of MediaPortal then it
    /// should return true to this method, otherwise if it should not be on home
    /// it should return false
    /// </summary>
    /// <param name="strButtonText">text the button should have</param>
    /// <param name="strButtonImage">image for the button, or empty for default</param>
    /// <param name="strButtonImageFocus">image for the button, or empty for default</param>
    /// <param name="strPictureImage">subpicture for the button or empty for none</param>
    /// <returns>true : plugin needs it's own button on home
    /// false : plugin does not need it's own button on home</returns>

    public bool GetHome(out string strButtonText, out string strButtonImage, out string strButtonImageFocus, out string strPictureImage)
    {
      strButtonText = String.Empty; // strButtonText = PluginName();
      strButtonImage = String.Empty;
      strButtonImageFocus = String.Empty;
      strPictureImage = String.Empty;
      return false;
    }

    #endregion
  }
}