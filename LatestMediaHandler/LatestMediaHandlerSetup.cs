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

using MediaPortal.Configuration;
using MediaPortal.GUI.Library;
using MediaPortal.Services;

using RealNLog.NLog;
using RealNLog.NLog.Config;
using RealNLog.NLog.Targets;

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Timers;
using System.Windows.Forms;
using System.Xml;
using System.Xml.XPath;
using System.Diagnostics;

using Microsoft.Win32;

namespace LatestMediaHandler
{
  [PluginIcons("LatestMediaHandler.LatestMediaHandler_Icon.png", "LatestMediaHandler.LatestMediaHandler_Icon_Disabled.png")]

  public class LatestMediaHandlerSetup : IPlugin, ISetupForm
  {
    #region declarations

    /*
         * Log declarations
    */
    private static Logger logger = LogManager.GetCurrentClassLogger(); //log
    private const string LogFileName = "LatestMediaHandler.log"; //log's filename
    private const string OldLogFileName = "LatestMediaHandler.bak"; //log's old filename        

    /*
         * All Threads and Timers
    */
    private static string lmhThreadPriority = "Lowest";
    internal static System.Timers.Timer ReorgTimer = null;
    private System.Timers.Timer refreshTimer = null;

    private LatestMediaHandlerConfig xconfig = null;
    private LatestReorgWorker MyLatestReorgWorker = null;

    private Hashtable windowsUsingFanartLatest; //used to know what skin files that supports latest media fanart     

    private List<int> ControlIDFacades;
    private List<int> ControlIDPlays;

    internal static bool Starting = true;

    private static LatestMyVideosHandler lmvh = null;
    private static LatestMovingPicturesHandler lmph = null;
    private static LatestTVSeriesHandler ltvsh = null;
    private static LatestMusicHandler lmh = null;
    private static LatestPictureHandler lph = null;
    private static LatestMyFilmsHandler lmfh = null;
    private static LatestMvCentralHandler lmch = null;
    private static LatestTVAllRecordingsHandler ltvrh = null;

    private static int reorgTimerTick;

    private static string mpVersion = null;

    #endregion

    /*
     * 919198710 Lmvh  - LatestMyVideosHandler.ControlID
     * 919199970 Lmh   - LatestMusicHandler.ControlID
     * 919199710 Lph   - LatestPictureHandler.ControlID
     * 919199940 Ltvsh - LatestTVSeriesHandler.ControlID
     * 919199910 Lmph  - LatestMovingPicturesHandler.ControlID
     * 919199880 Lmfh  - LatestMyFilmsHandler.ControlID
     * 919299280 lmch  - LatestMvCentralHandler.ControlID
     * 919199840       - LatestTVAllRecordingsHandler.ControlID 
                       - if (Utils.Used4TRTV) L4trrh else if (Utils.usedArgus) largusrh else Ltvrh
    */

    internal static string DateFormat
    {
      get { return Utils.dateFormat; }
      set { Utils.dateFormat = value; }
    }

    internal static string ReorgInterval
    {
      get { return Utils.reorgInterval; }
      set { Utils.reorgInterval = value; }
    }

    internal static string LatestMusicType
    {
      get { return Utils.latestMusicType; }
      set { Utils.latestMusicType = value; }
    }

    internal static int ReorgTimerTick
    {
      get { return LatestMediaHandlerSetup.reorgTimerTick; }
      set { LatestMediaHandlerSetup.reorgTimerTick = value; }
    }

    /*
    internal static string UseLatestMediaCache
        {
            get { return useLatestMediaCache; }
            set { useLatestMediaCache = value; }
        }
    */

    internal static LatestPictureHandler Lph
    {
      get { return lph; }
      set { lph = value; }
    }

    internal static LatestTVAllRecordingsHandler Ltvrh
    {
      get { return ltvrh; }
      set { ltvrh = value; }
    }

    internal static LatestMusicHandler Lmh
    {
      get { return lmh; }
      set { lmh = value; }
    }

    internal static LatestMyVideosHandler Lmvh
    {
      get { return lmvh; }
      set { lmvh = value; }
    }

    internal static LatestMvCentralHandler Lmch
    {
      get { return lmch; }
      set { lmch = value; }
    }

    internal static LatestMovingPicturesHandler Lmph
    {
      get { return lmph; }
      set { lmph = value; }
    }

    internal static LatestMyFilmsHandler Lmfh
    {
      get { return lmfh; }
      set { lmfh = value; }
    }

    internal static LatestTVSeriesHandler Ltvsh
    {
      get { return ltvsh; }
      set { ltvsh = value; }
    }

    internal Hashtable WindowsUsingFanartLatest
    {
      get { return windowsUsingFanartLatest; }
      set { windowsUsingFanartLatest = value; }
    }

    internal static string RefreshDbPicture
    {
      get { return Utils.refreshDbPicture; }
      set { Utils.refreshDbPicture = value; }
    }

    internal static string RefreshDbMusic
    {
      get { return Utils.refreshDbMusic; }
      set { Utils.refreshDbMusic = value; }
    }

    internal static string LatestTVRecordings
    {
      get { return Utils.latestTVRecordings; }
      set { Utils.latestTVRecordings = value; }
    }

    internal static string LatestTVRecordingsWatched
    {
      get { return Utils.latestTVRecordingsWatched; }
      set { Utils.latestTVRecordingsWatched = value; }
    }

    internal static string LatestTVRecordingsUnfinished
    {
      get { return Utils.latestTVRecordingsUnfinished; }
      set { Utils.latestTVRecordingsUnfinished = value; }
    }

    internal static string LatestMyFilmsWatched
    {
      get { return Utils.latestMyFilmsWatched; }
      set { Utils.latestMyFilmsWatched = value; }
    }

    internal static string LatestTVSeries
    {
      get { return Utils.latestTVSeries; }
      set { Utils.latestTVSeries = value; }
    }

    internal static string LatestTVSeriesWatched
    {
      get { return Utils.latestTVSeriesWatched; }
      set { Utils.latestTVSeriesWatched = value; }
    }

    internal static string LatestTVSeriesRatings
    {
      get { return Utils.latestTVSeriesRatings; }
      set { Utils.latestTVSeriesRatings = value; }
    }

    internal static string LatestMyVideos
    {
      get { return Utils.latestMyVideos; }
      set { Utils.latestMyVideos = value; }
    }

    internal static string LatestMvCentral
    {
      get { return Utils.latestMvCentral; }
      set { Utils.latestMvCentral = value; }
    }

    internal static string LatestMyVideosWatched
    {
      get { return Utils.latestMyVideosWatched; }
      set { Utils.latestMyVideosWatched = value; }
    }

    internal static string LatestMovingPictures
    {
      get { return Utils.latestMovingPictures; }
      set { Utils.latestMovingPictures = value; }
    }

    internal static string LatestMovingPicturesWatched
    {
      get { return Utils.latestMovingPicturesWatched; }
      set { Utils.latestMovingPicturesWatched = value; }
    }

    internal static string LatestMusic
    {
      get { return Utils.latestMusic; }
      set { Utils.latestMusic = value; }
    }

    internal static string LatestPictures
    {
      get { return Utils.latestPictures; }
      set { Utils.latestPictures = value; }
    }

    internal static string LatestMyFilms
    {
      get { return Utils.latestMyFilms; }
      set { Utils.latestMyFilms = value; }
    }

    internal static string LMHThreadPriority
    {
      get { return LatestMediaHandlerSetup.lmhThreadPriority; }
      set { LatestMediaHandlerSetup.lmhThreadPriority = value; }
    }

    public static string MpVersion
    {
      get { return mpVersion; }
      set { mpVersion = value; }
    }

    internal static int GetReorgTimerInterval()
    {
      int newTick = Environment.TickCount - ReorgTimerTick;
      newTick = (Int32.Parse(LatestMediaHandlerSetup.ReorgInterval)*60000) - newTick;
      if (newTick < 0)
      {
        newTick = 2000;
      }
      return newTick;
    }

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

    private void SetupWindowsUsingLatestMediaHandlerVisibility(string SkinDir = (string) null, string ThemeDir = (string) null)
    {
      XPathDocument myXPathDocument;
      XPathNavigator myXPathNavigator;
      XPathNodeIterator myXPathNodeIterator;

      string windowId = String.Empty;
      string sNodeValue = String.Empty;

      var path = string.Empty ;

      if (string.IsNullOrEmpty(SkinDir))
      {
        WindowsUsingFanartLatest = new Hashtable();

        path = GUIGraphicsContext.Skin + @"\";
        logger.Debug("Scan Skin folder for XML: "+path) ;
      }
      else
      {
        path = ThemeDir;
        logger.Debug("Scan Skin Theme folder for XML: "+path) ;
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

          myXPathDocument     = new XPathDocument(fi.FullName);
          myXPathNavigator    = myXPathDocument.CreateNavigator();
          myXPathNodeIterator = myXPathNavigator.Select("/window/id");
          windowId            = GetNodeValue(myXPathNodeIterator);

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
                  HandleXmlImports(XMLFullName, windowId, ref _flagLatest);
                else if ((!string.IsNullOrEmpty(SkinDir)) && (!string.IsNullOrEmpty(ThemeDir)))
                  {
                    XMLFullName = Path.Combine(SkinDir, myXPathNodeIterator.Current.Value);
                    if (File.Exists(XMLFullName))
                      HandleXmlImports(XMLFullName, windowId, ref _flagLatest);
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
                  HandleXmlImports(XMLFullName, windowId, ref _flagLatest);
                else if ((!string.IsNullOrEmpty(SkinDir)) && (!string.IsNullOrEmpty(ThemeDir)))
                  {
                    XMLFullName = Path.Combine(SkinDir, myXPathNodeIterator.Current.Value);
                    if (File.Exists(XMLFullName))
                      HandleXmlImports(XMLFullName, windowId, ref _flagLatest);
                  }
              }
            }

            if (_flagLatest)
            {
              if (!WindowsUsingFanartLatest.Contains(windowId))
                WindowsUsingFanartLatest.Add(windowId, windowId);
            }
          }
        }
        catch (Exception ex)
        {
          logger.Error("SetupWindowsUsingLatestMediaHandlerVisibility: "+(string.IsNullOrEmpty(ThemeDir) ? string.Empty : "Theme: "+ThemeDir+" ")+"Filename:"+ XMLName) ;
          logger.Error(ex) ;
        }
      }

      if (string.IsNullOrEmpty(ThemeDir) && !string.IsNullOrEmpty(GUIGraphicsContext.ThemeName)) 
      {
        // Include Themes
        var tThemeDir = path+@"Themes\"+GUIGraphicsContext.ThemeName.Trim()+@"\";
        if (Directory.Exists(tThemeDir))
          {
            SetupWindowsUsingLatestMediaHandlerVisibility(path, tThemeDir);
            return;
          }
        tThemeDir = path+GUIGraphicsContext.ThemeName.Trim()+@"\";
        if (Directory.Exists(tThemeDir))
          SetupWindowsUsingLatestMediaHandlerVisibility(path, tThemeDir);
      }
    }

    private void HandleXmlImports(string filename, string windowId, ref bool _flagLatest)
    {
      XPathDocument myXPathDocument = new XPathDocument(filename);
      StringBuilder sb = new StringBuilder();
      string _xml = string.Empty;

      using (XmlWriter xmlWriter = XmlWriter.Create(sb))
      {
        myXPathDocument.CreateNavigator().WriteSubtree(xmlWriter);
      }

      _xml = sb.ToString();
      _flagLatest = (_xml.Contains(".latest.") && _xml.Contains("#latestMediaHandler.")) ? true : _flagLatest;

      sb = null;
    }

    private void UpdateReorgTimer(Object stateInfo, ElapsedEventArgs e)
    {
      if (Utils.GetIsStopping() == false)
      {
        try
        {
          int sync = Interlocked.CompareExchange(ref Utils.SyncPointReorg, 1, 0);
          if (sync == 0)
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
      try // Music
      {
        Lmh.SetupMusicLatest();
      }
      catch { }

      try // MyVideo
      {
        Lmvh.SetupVideoLatest();
      }
      catch { }

      try // TV Series
      {
        Ltvsh.SetupTVSeriesLatest();
      }
      catch { }

      try // Pictures
      {
        Lph.SetupPicturesLatest();
      }
      catch { }

      try // Moving Pictures
      {
        Lmph.SetupMovingPicturesLatest();
      }
      catch { }

      try // My Films
      {
        Lmfh.SetupMovieLatest();
      }
      catch { }

      try // mvCentral
      {
        Lmch.SetupMvCentralsLatest();
      }
      catch { }

      try // TV Recordings
      {
        Ltvrh.SetupTVRecordingsLatest();
      }
      catch { }

      try // Fanart Handler
      {
        UtilsFanartHandler.SetupFanartHandlerSubcribeScaperFinishedEvent();
      }
      catch (Exception ex)
      {
        logger.Error("SetupFanartHandlerSubcribeScaperFinishedEvent: " + ex.ToString());
      }
    }
    #endregion

    #region Dispose Handlers
    private void DisposeHandlers()
    {
      try // Music
      {
        Lmh.DisposeMusicLatest();
      }
      catch { }

      try // MyVideo
      {
        Lmvh.DisposeVideoLatest();
      }
      catch { }

      try // TV Series
      {
        Ltvsh.DisposeTVSeriesLatest();
      }
      catch { }

      try // Pictures
      {
        Lph.DisposePicturesLatest();
      }
      catch { }

      try // Moving Pictures
      {
        Lmph.DisposeMovingPicturesLatest();
      }
      catch { }

      try // My Films
      {
        Lmfh.DisposeMovieLatest();
      }
      catch { }

      try // mvCentral
      {
        Lmch.DisposeMvCentralsLatest();
      }
      catch { }

      try // TV Recordings
      {
        Ltvrh.DisposeTVRecordingsLatest();
      }
      catch { }

      try // Fanart Handler
      {
        UtilsFanartHandler.DisposeFanartHandlerSubcribeScaperFinishedEvent();
      }
      catch { }
    }
    #endregion

    /// <summary>
    /// Set start values on variables
    /// </summary>
    private void SetupVariables()
    {
      Utils.SetIsStopping(false);

      Lmh   = new LatestMusicHandler();
      Lmvh  = new LatestMyVideosHandler();
      Ltvsh = new LatestTVSeriesHandler();
      Lph   = new LatestPictureHandler();
      Lmph  = new LatestMovingPicturesHandler();
      Lmfh  = new LatestMyFilmsHandler();
      Lmch  = new LatestMvCentralHandler();
      ltvrh = new LatestTVAllRecordingsHandler();

      ReorgTimerTick = Environment.TickCount;
      //
      Utils.SetProperty("#latestMediaHandler.scanned", "false");
      //
      Lmh.EmptyLatestMediaPropsMusic();
      Lmvh.EmptyLatestMediaPropsMyVideos();
      Lph.EmptyLatestMediaPropsPictures();
      Ltvsh.EmptyLatestMediaPropsTVSeries();
      Lmph.EmptyLatestMediaPropsMovingPictures();
      Lmfh.EmptyLatestMediaPropsMyFilms();
      Lmch.EmptyLatestMediaPropsMvCentral();
      ltvrh.EmptyLatestMediaPropsTVRecordings();
      ltvrh.EmptyRecordingProps();

      //
      ControlIDFacades = new List<int>();
      ControlIDPlays = new List<int>();
      //
      ControlIDFacades.AddRange(Lmh.ControlIDFacades);
      ControlIDPlays.AddRange(Lmh.ControlIDPlays);

      ControlIDFacades.AddRange(Lmvh.ControlIDFacades);
      ControlIDPlays.AddRange(Lmvh.ControlIDPlays);  
      ControlIDFacades.AddRange(Ltvsh.ControlIDFacades);
      ControlIDPlays.AddRange(Ltvsh.ControlIDPlays);  
      ControlIDFacades.AddRange(Lph.ControlIDFacades);
      ControlIDPlays.AddRange(Lph.ControlIDPlays);  
      ControlIDFacades.AddRange(Lmph.ControlIDFacades);
      ControlIDPlays.AddRange(Lmph.ControlIDPlays);  
      ControlIDFacades.AddRange(Lmfh.ControlIDFacades);
      ControlIDPlays.AddRange(Lmfh.ControlIDPlays);  
      ControlIDFacades.AddRange(Lmch.ControlIDFacades);
      ControlIDPlays.AddRange(Lmch.ControlIDPlays);  
      ControlIDFacades.AddRange(ltvrh.ControlIDFacades);
      ControlIDPlays.AddRange(ltvrh.ControlIDPlays);  

      // logger.Debug("*** Init active facade controls: "+string.Join(" ", ControlIDFacades));
      // logger.Debug("*** Init active button controls: "+string.Join(" ", ControlIDPlays));

      // Utils.InitCheckMarks();
    }

/*
        internal static void UnloadLatestCache(ref ArrayList al)
        {
            if (al != null)
            {
                foreach (String s in al)
                {
                    GUITextureManager.ReleaseTexture(s);
                }             
            }
        }

        internal static void ReloadLatestCache(ref ArrayList images)
        {           
            if (images != null && images.Count > 0)
            {
                foreach (String s in images)
                {
                    Utils.LoadImage(s);
                }
            }
        }

        internal static void UpdateLatestCache(ref ArrayList al, ArrayList images)
        {
            if (al != null)
            {
                foreach (String s in al)
                {
                    GUITextureManager.ReleaseTexture(s);
                }
                al.Clear();    
            }
                       
            if (images != null && images.Count > 0)
            {
                foreach (String s in images)
                {
                    if (al.Contains(s) == false)
                    {
                        try
                        {
                            al.Add(s);
                        }
                        catch (Exception ex)
                        {
                            logger.Error("UpdateLatestCache: " + ex.ToString());
                        }
                        Utils.LoadImage(s);
                    }
                }
            }
        }
*/

    /// <summary>
    /// Setup logger. This funtion made by the team behind Moving Pictures 
    /// (http://code.google.com/p/moving-pictures/)
    /// </summary>
    private void InitLogger()
    {
      //LoggingConfiguration config = new LoggingConfiguration();
      LoggingConfiguration config = LogManager.Configuration ?? new LoggingConfiguration();

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

      FileTarget fileTarget = new FileTarget();
      fileTarget.FileName = Config.GetFile(Config.Dir.Log, LogFileName);
      fileTarget.Encoding = "utf-8";
      fileTarget.Layout = "${date:format=dd-MMM-yyyy HH\\:mm\\:ss} " +
                          "${level:fixedLength=true:padding=5} " +
                          "[${logger:fixedLength=true:padding=20:shortName=true}]: ${message} " +
                          "${exception:format=tostring}";

      config.AddTarget("file", fileTarget);

      // Get current Log Level from MediaPortal 
      LogLevel logLevel;
      MediaPortal.Profile.Settings xmlreader = new MediaPortal.Profile.Settings(Config.GetFile(Config.Dir.Config, "MediaPortal.xml"));

      string str = xmlreader.GetValue("general", "ThreadPriority");
      LMHThreadPriority = str == null || !str.Equals("Normal", StringComparison.CurrentCulture) ? (str == null || !str.Equals("BelowNormal", StringComparison.CurrentCulture) ? "BelowNormal" : "Lowest") : "Lowest";

      switch ((Level) xmlreader.GetValueAsInt("general", "loglevel", 0))
      {
        case Level.Error:
          logLevel = LogLevel.Error;
          break;
        case Level.Warning:
          logLevel = LogLevel.Warn;
          break;
        case Level.Information:
          logLevel = LogLevel.Info;
          break;
        case Level.Debug:
        default:
          logLevel = LogLevel.Debug;
          break;
      }

#if DEBUG
      logLevel = LogLevel.Debug;
#endif

      LoggingRule rule = new LoggingRule("*", logLevel, fileTarget);
      config.LoggingRules.Add(rule);

      LogManager.Configuration = config;
    }

    /// <summary>
    /// The plugin is started by Mediaportal
    /// </summary>
    public void Start()
    {
      try
      {
        Utils.DelayStop = new Hashtable();
        Utils.SetIsStopping(false);

        InitLogger();
        logger.Info("Latest Media Handler is starting.");
        logger.Info("Latest Media Handler version is " + Utils.GetAllVersionNumber());

        MpVersion = FileVersionInfo.GetVersionInfo(Application.ExecutablePath).ProductVersion;
        logger.Info("MediaPortal version is " + MpVersion);
        MpVersion = FileVersionInfo.GetVersionInfo(Application.ExecutablePath).ProductMajorPart.ToString()+"."+
                    FileVersionInfo.GetVersionInfo(Application.ExecutablePath).ProductMinorPart.ToString().PadLeft(2, '0');

        SetupConfigFile();
        Utils.LoadSettings();

        Translation.Init();

        SetupWindowsUsingLatestMediaHandlerVisibility();
        SetupVariables();
        Utils.SyncPointInit();

        GetLatestMediaInfo();

        Thread.Sleep(1000);
        
        InitHandlers();

        GUIWindowManager.OnActivateWindow += new GUIWindowManager.WindowActivationHandler(GuiWindowManagerOnActivateWindow);
        GUIGraphicsContext.OnNewAction += new OnActionHandler(OnNewAction);
        GUIWindowManager.Receivers += new SendMessageHandler(OnMessage);
        SystemEvents.PowerModeChanged += new PowerModeChangedEventHandler(OnSystemPowerModeChanged);

        ReorgTimer = new System.Timers.Timer((Int32.Parse(ReorgInterval)*60000));
        ReorgTimer.Elapsed += new ElapsedEventHandler(UpdateReorgTimer);
        ReorgTimer.Interval = (Int32.Parse(ReorgInterval)*60000);
        ReorgTimer.Start();

        refreshTimer = new System.Timers.Timer(250);
        refreshTimer.Elapsed += new ElapsedEventHandler(UpdateImageTimer);
        refreshTimer.Interval = 250;
        refreshTimer.Start();

        RefreshActiveWindow();

        Starting = false ;

        logger.Info("Latest Media Handler is started.");
      }
      catch (Exception ex)
      {
        logger.Error("Start: " + ex.ToString());
      }
    }

    private void RefreshActiveWindow()
    {
        string windowId = String.Empty + GUIWindowManager.ActiveWindow;
        if (Utils.GetIsStopping() == false && WindowsUsingFanartLatest.ContainsKey(windowId))
        {
          GUIWindow fWindow = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow);
          if (fWindow != null)
          {
            GUIWindowManager.ActivateWindow(fWindow.GetID, true, true, fWindow.GetFocusControlId());
          }
        }
    }

    private void GetLatestMediaInfo(string Mode = (string) "Start", int Level = (int) 0)
    {
      // Level 0 - All, 1 - Video, 2 - Music, 3 - Pictures, 4 - TV
      if ((Level == 0) || (Level == 1))
        GetLatestMediaInfoVideo(Mode) ;
      if ((Level == 0) || (Level == 2))
        GetLatestMediaInfoMusic(Mode) ;
      if ((Level == 0) || (Level == 3))
        GetLatestMediaInfoPictures(Mode) ;
      if ((Level == 0) || (Level == 4))
        GetLatestMediaInfoTV(Mode) ;
    }

    private static void GetLatestMediaInfoMusic(string Mode = (string) "Update")
    {
      // Music
      if (LatestMusic.Equals("True", StringComparison.CurrentCulture))
      {
        try
        {
          Lmh.GetLatestMediaInfoThread();
        }
        catch (Exception ex)
        {
          logger.Error("GetLatestMediaInfoMusic ["+Mode+"]: " + ex.ToString());
        }
      }
    }

    private static void GetLatestMediaInfoPictures(string Mode = (string) "Update")
    {
      // Pictures
      if (LatestPictures.Equals("True", StringComparison.CurrentCulture))
      {
        try
        {
          Lph.GetLatestMediaInfoThread();
        }
        catch (Exception ex)
        {
          logger.Error("GetLatestMediaInfoPictures ["+Mode+"]: " + ex.ToString());
        }
      }
    }

    private static void GetLatestMediaInfoTV(string Mode = (string) "Update")
    {
      // TV Record
      if (LatestTVRecordings.Equals("True", StringComparison.CurrentCulture))
      {
        try
        {
          Ltvrh.GetTVRecordings();
        }
        catch (Exception ex)
        {
          logger.Error("GetLatestMediaInfoTV ["+Mode+"]: " + ex.ToString());
        }
      }
    }

    private static void GetLatestMediaInfoVideo(string Mode = (string) "Update")
    {
      // MyVideo
      if (LatestMyVideos.Equals("True", StringComparison.CurrentCulture))
      {
        try
        {
          Lmvh.MyVideosUpdateLatestThread();
        }
        catch (Exception ex)
        {
          logger.Error("GetLatestMediaInfoVideo ["+Mode+"]: " + ex.ToString());
        }
      }

      // TVSeries
      if (LatestTVSeries.Equals("True", StringComparison.CurrentCulture))
      {
        try
        {
          Ltvsh.TVSeriesUpdateLatestThread();
        }
        catch (Exception ex)
        {
          logger.Error("GetLatestMediaInfoVideo ["+Mode+"]: " + ex.ToString());
        }
      }

      // Moving Pictures
      if (LatestMovingPictures.Equals("True", StringComparison.CurrentCulture))
      {
        try
        {
          Lmph.MovingPictureUpdateLatestThread();
        }
        catch (Exception ex)
        {
          logger.Error("GetLatestMediaInfoVideo ["+Mode+"]: " + ex.ToString());
        }
      }

      // MyFilms
      if (LatestMyFilms.Equals("True", StringComparison.CurrentCulture))
      {
        try
        {
          Lmfh.MyFilmsUpdateLatestThread();
        }
        catch (Exception ex)
        {
          logger.Error("GetLatestMediaInfoVideo ["+Mode+"]: " + ex.ToString());
        }
      }

      // mvCentral
      if (LatestMvCentral.Equals("True", StringComparison.CurrentCulture))
      {
        try
        {
          Lmch.GetLatestMediaInfoThread();
        }
        catch (Exception ex)
        {
          logger.Error("GetLatestMediaInfoVideo ["+Mode+"]: " + ex.ToString());
        }
      }
    }

    private void OnSystemPowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
      try
      {
        if (e.Mode == PowerModes.Resume)
        {
          logger.Info("LatestMediaHandler is resuming from standby/hibernate.");
          // StopTasks(false);
          // Start();

          // b: ajs
          Utils.HasNewInit();
          GetLatestMediaInfo();
          // e: ajs
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
        if (artist != null && Lmh.artistsWithImageMissing != null && Lmh.artistsWithImageMissing.Contains(artist))
        {
          logger.Info("Received new scraper event from FanartHandler plugin for artist " + artist + ".");
          // Lmh.GetLatestMediaInfo(false);
          GetLatestMediaInfoMusic("Fanart");
        }
/*
        else
        {
          if (artist != null)
          {
            logger.Debug("Received new scraper event from FanartHandler plugin for artist " + artist + ".");
          }
        }
*/
      }
      catch (Exception ex)
      {
        logger.Error("TriggerGetLatestMediaInfoOnEvent: " + ex.ToString());
      }
    }

    private void DoContextMenu()
    {
      GUIWindow fWindow = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow);
      if (fWindow == null)
        return ;

      int FocusControlID = fWindow.GetFocusControlId();
      if (!ControlIDFacades.Contains(FocusControlID) && !ControlIDPlays.Contains(FocusControlID))
        return;

      if ((FocusControlID == LatestMovingPicturesHandler.ControlID) || 
          (FocusControlID == LatestMovingPicturesHandler.Play1ControlID) || (FocusControlID == LatestMovingPicturesHandler.Play2ControlID) || (FocusControlID == LatestMovingPicturesHandler.Play3ControlID))
      {
        Lmph.MyContextMenu();
      }
      else if ((FocusControlID == LatestMyVideosHandler.ControlID) ||
          (FocusControlID == LatestMyVideosHandler.Play1ControlID) || (FocusControlID == LatestMyVideosHandler.Play2ControlID) || (FocusControlID == LatestMyVideosHandler.Play3ControlID))
      {
        Lmvh.MyContextMenu();
      }
      else if ((FocusControlID == LatestMvCentralHandler.ControlID) ||
          (FocusControlID == LatestMvCentralHandler.Play1ControlID) || (FocusControlID == LatestMvCentralHandler.Play2ControlID) || (FocusControlID == LatestMvCentralHandler.Play3ControlID))
      {
        Lmch.MyContextMenu();
      }
      else if ((FocusControlID == LatestPictureHandler.ControlID) ||
          (FocusControlID == LatestPictureHandler.Play1ControlID) || (FocusControlID == LatestPictureHandler.Play2ControlID) || (FocusControlID == LatestPictureHandler.Play3ControlID))
      {
        Lph.MyContextMenu();
      }
      else if ((FocusControlID == LatestTVSeriesHandler.ControlID) ||
          (FocusControlID == LatestTVSeriesHandler.Play1ControlID) || (FocusControlID == LatestTVSeriesHandler.Play2ControlID) || (FocusControlID == LatestTVSeriesHandler.Play3ControlID))
      {
        Ltvsh.MyContextMenu();
      }
      else if ((FocusControlID == LatestMyFilmsHandler.ControlID) ||
          (FocusControlID == LatestMyFilmsHandler.Play1ControlID) || (FocusControlID == LatestMyFilmsHandler.Play2ControlID) || (FocusControlID == LatestMyFilmsHandler.Play3ControlID))
      {
        Lmfh.MyContextMenu();
      }
      else if ((FocusControlID == LatestTVAllRecordingsHandler.ControlID) ||
          (FocusControlID == LatestTVAllRecordingsHandler.Play1ControlID) || (FocusControlID == LatestTVAllRecordingsHandler.Play2ControlID) || (FocusControlID == LatestTVAllRecordingsHandler.Play3ControlID))
      {
        Ltvrh.MyContextMenu();
      }
      else if ((FocusControlID == LatestMusicHandler.ControlID) ||
          (FocusControlID == LatestMusicHandler.Play1ControlID) || (FocusControlID == LatestMusicHandler.Play2ControlID) || (FocusControlID == LatestMusicHandler.Play3ControlID))
      {
        Lmh.MyContextMenu();
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
      catch
      {   }
    }

    internal void OnAction(GUIWindow fWindow, ref MediaPortal.GUI.Library.Action action, bool ContextMenu) 
    {
      if (fWindow != null)
      {
        //
        int FocusControlID = fWindow.GetFocusControlId();
        if (ControlIDFacades.Contains(FocusControlID) || ControlIDPlays.Contains(FocusControlID))
        {
          if (ContextMenu)
          {
            if (action.IsUserAction())
              GUIGraphicsContext.ResetLastActivity();
            action.wID = 0;

            ShowLMHDialog slmhd = new ShowLMHDialog();
            GUIWindowManager.SendThreadMessage(new GUIMessage()
                                               {
                                                 TargetWindowId = (int)GUIWindow.Window.WINDOW_SECOND_HOME,
                                                 SendToTargetWindow = true,
                                                 Object = slmhd
                                               });
            return;
          }
          else
          {
            if (Lmh != null && Lmh.PlayMusicAlbum(fWindow)) return;
            if (Lmvh != null && Lmvh.PlayMovie(fWindow)) return;
            if (Lph != null && Lph.PlayPictures(fWindow)) return;
            if (Ltvsh != null && Ltvsh.PlayTVSeries(fWindow)) return;
            if (Lmph != null && Lmph.PlayMovingPicture(fWindow)) return;
            if (Lmfh != null && Lmfh.PlayMovie(fWindow)) return;
            if (Lmch != null && Lmch.PlayMusicAlbum(fWindow)) return;
            if (Ltvrh != null && Ltvrh.PlayRecording(fWindow, ref action)) return;
            return;
          }
        }
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
        if ((GUIWindowManager.ActiveWindow == (int)GUIWindow.Window.WINDOW_SECOND_HOME) && (GUIWindowManager.RoutedWindow == -1))
        {
          GUIWindow fWindow = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow);
          if (fWindow == null)
            return;

          switch (action.wID)
          {
            case MediaPortal.GUI.Library.Action.ActionType.ACTION_CONTEXT_MENU:
            {
              Action = true ;
              Context = true;
              logger.Debug("OnNewAction: ACTION_CONTEXT_MENU");
              break;
            }
            case MediaPortal.GUI.Library.Action.ActionType.ACTION_MOUSE_CLICK:
            {
              Action  = (action.MouseButton == MouseButtons.Left) || (action.MouseButton == MouseButtons.Right) ;
              Context = (action.MouseButton == MouseButtons.Right);
              logger.Debug("OnNewAction: ACTION_MOUSE_CLICK [" + ((action.MouseButton == MouseButtons.Left) ? "L" : (action.MouseButton == MouseButtons.Right) ? "R" : "U") + "]");
              break;
            }
            case MediaPortal.GUI.Library.Action.ActionType.ACTION_SELECT_ITEM:
            {
              Action = true ;
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

    private void InitFacade(object obj, int windowId)
    {
      try
      {
        GUIWindow gw = GUIWindowManager.GetWindow(windowId);
        int x = 0;
        if (obj is LatestMovingPicturesHandler)
          x = LatestMovingPicturesHandler.ControlID;
        else if (obj is LatestMyVideosHandler)
          x = LatestMyVideosHandler.ControlID;
        else if (obj is LatestMvCentralHandler)
          x = LatestMvCentralHandler.ControlID;
        else if (obj is LatestTVSeriesHandler)
          x = LatestTVSeriesHandler.ControlID;
        else if (obj is LatestMusicHandler)
          x = LatestMusicHandler.ControlID;
        else if (obj is LatestTVAllRecordingsHandler)
          x = LatestTVAllRecordingsHandler.ControlID;
        else if (obj is LatestPictureHandler)
          x = LatestPictureHandler.ControlID;
        else if (obj is LatestMyFilmsHandler)
          x = LatestMyFilmsHandler.ControlID;
        //
        if (x == LatestMovingPicturesHandler.ControlID)
        {
          if (Lmph != null)
            Lmph.InitFacade(true) ;
          return;
        }
        else if (x == LatestMyVideosHandler.ControlID)
        {
          if (Lmvh != null)
            Lmvh.InitFacade(true) ;
          return ;
        }
        else if (x == LatestMvCentralHandler.ControlID)
        {
          if (Lmch != null)
            Lmch.InitFacade(true);
          return;
        }
        else if (x == LatestTVSeriesHandler.ControlID)
        {
          if (Ltvsh != null)
            Ltvsh.InitFacade(true);
          return;
        }
        else if (x == LatestMusicHandler.ControlID)
        {
          if (Lmh != null)
            Lmh.InitFacade(true);
          return ;
        }
        else if (x == LatestPictureHandler.ControlID)
        {
          if (Lph != null)
            Lph.InitFacade(true);
          return;
        }
        else if (x == LatestMyFilmsHandler.ControlID)
        {
          if (Lmfh != null)
          Lmfh.InitFacade(true);
          return;
        }
        //
        GUIFacadeControl facade = gw.GetControl(x) as GUIFacadeControl;
        if (facade != null)
        {
          facade.Clear();
          if (x == LatestTVAllRecordingsHandler.ControlID)
          {
            if (Ltvrh != null)
            {
              Ltvrh.InitFacade(ref facade);
              facade.SelectedListItemIndex = Ltvrh.LastFocusedId;
              Utils.UpdateFacade(ref facade);
              facade.Visible = false;
            }
          }
        }
      }
      catch (Exception ex)
      {
        logger.Error("InitFacade: " + ex.ToString());
      }
    }

    internal void DeInitFacade(int windowId)
    {
      if (Lmh != null)
        Lmh.DeInitFacade();
      if (Lmvh != null)
        Lmvh.DeInitFacade();
      if (Ltvsh != null)
        Ltvsh.DeInitFacade();
      if (Lmph != null)
        Lmph.DeInitFacade();
      if (Lph != null)
        Lph.DeInitFacade();
      if (Lmch != null)
        Lmch.DeInitFacade();
      if (Lmfh != null)
        Lmfh.DeInitFacade();

      GUIWindow gw = GUIWindowManager.GetWindow(windowId);
      GUIFacadeControl facade = gw.GetControl(LatestTVAllRecordingsHandler.ControlID) as GUIFacadeControl;
      if (facade != null)
      {
        Ltvrh.ClearFacade(ref facade) ;
      }
    }

    internal void GuiWindowManagerOnActivateWindow(int activeWindowId)
    {
      try
      {
        string windowId = String.Empty + GUIWindowManager.ActiveWindow;
        if (windowId != activeWindowId.ToString())
          logger.Debug("GuiWindowManagerOnActivateWindow: Hmmm, recieve ID: " + activeWindowId.ToString() + " but actual ID: "+windowId);

        if (Utils.GetIsStopping() == false && WindowsUsingFanartLatest.ContainsKey(windowId))
        {
          InitFacade(Lmph, activeWindowId);
          InitFacade(Ltvsh, activeWindowId);
          InitFacade(Lmh, activeWindowId);
          InitFacade(Ltvrh, activeWindowId);
          InitFacade(Lph, activeWindowId);
          InitFacade(Lmfh, activeWindowId);
          InitFacade(Lmvh, activeWindowId);
          InitFacade(Lmch, activeWindowId);
          //
          if (ReorgTimer != null && !ReorgTimer.Enabled)
          {
            ReorgTimer.Interval = GetReorgTimerInterval();
            ReorgTimer.Start();
          }
          //
          if (refreshTimer != null && !refreshTimer.Enabled)
            refreshTimer.Start();
          //
          GetLatestMediaInfoTV("WindowActivate");
        }
        else
        {
          if (refreshTimer != null && refreshTimer.Enabled)
            refreshTimer.Stop();
          if (ReorgTimer != null && ReorgTimer.Enabled)
            ReorgTimer.Stop();

          DeInitFacade (activeWindowId) ;
        }
      }
      catch (Exception ex)
      {
        logger.Error("GUIWindowManager_OnActivateWindow: " + ex.ToString());
      }
    }

    private void UpdateImageTimer(Object stateInfo, ElapsedEventArgs e)
    {
      try
      {
        int sync = Interlocked.CompareExchange(ref Utils.SyncPointRefresh, 1, 0);
        if (sync == 0)
        {
          if (Utils.IsIdle())
          {
            if (GUIWindowManager.ActiveWindow > (int)GUIWindow.Window.WINDOW_INVALID)
            {
              GUIWindow fWindow = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow);
              if (fWindow != null)
              {
                /*                            
                Utils.LogDevMsg("*******************************");
                Utils.LogDevMsg("Lmvh.images.Count:" + Lmvh.images.Count);
                Utils.LogDevMsg("Lmch.images.Count:" + Lmch.images.Count);
                Utils.LogDevMsg("Lmvh.imagesThumbs.Count:" + Lmvh.imagesThumbs.Count);
                Utils.LogDevMsg("Lmph.images.Count:" + Lmph.images.Count);
                Utils.LogDevMsg("Lmph.imagesThumbs.Count:" + Lmph.imagesThumbs.Count);
                Utils.LogDevMsg("Ltvsh.images.Count:" + Ltvsh.images.Count);
                Utils.LogDevMsg("Ltvsh.imagesThumbs.Count:" + Ltvsh.imagesThumbs.Count);
                Utils.LogDevMsg("Lmh.images.Count:" + Lmh.images.Count);
                Utils.LogDevMsg("Lmh.imagesThumbs.Count:" + Lmh.imagesThumbs.Count);
                Utils.LogDevMsg("Lph.images.Count:" + Lph.images.Count);
                Utils.LogDevMsg("Lph.imagesThumbs.Count:" + Lph.imagesThumbs.Count);
                Utils.LogDevMsg("Lmfh.images.Count:" + Lmfh.images.Count);
                Utils.LogDevMsg("Lmfh.imagesThumbs.Count:" + Lmfh.imagesThumbs.Count);
                Utils.LogDevMsg("L4trrh.images.Count:" + L4trrh.images.Count);
                Utils.LogDevMsg("L4trrh.imagesThumbs.Count:" + L4trrh.imagesThumbs.Count);
                */
                Lmh.UpdateImageTimer(fWindow, stateInfo, e);
                Lph.UpdateImageTimer(fWindow, stateInfo, e);
                Lmvh.UpdateImageTimer(fWindow, stateInfo, e);
                Ltvsh.UpdateImageTimer(fWindow, stateInfo, e);
                Lmph.UpdateImageTimer(fWindow, stateInfo, e);
                Lmfh.UpdateImageTimer(fWindow, stateInfo, e);
                Lmch.UpdateImageTimer(fWindow, stateInfo, e);
                Ltvrh.UpdateImageTimer(fWindow, stateInfo, e);
              }
            }
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
      /*
      try
      {
        String path = Config.GetFolder(Config.Dir.Config) + @"\LatestMediaHandler.xml";
        String pathOrg = Config.GetFolder(Config.Dir.Config) + @"\LatestMediaHandler.org";
        if (File.Exists(path))
        {
          //do nothing
        }
        else
        {
          File.Copy(pathOrg, path);
        }
      }
      catch (Exception ex)
      {
        logger.Error("setupConfigFile: " + ex.ToString());
      }
      */
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
        Utils.SetIsStopping(true);

        GUIWindowManager.OnActivateWindow -=  new GUIWindowManager.WindowActivationHandler(GuiWindowManagerOnActivateWindow);
        GUIGraphicsContext.OnNewAction -= new OnActionHandler(OnNewAction);
        GUIWindowManager.Receivers -= new SendMessageHandler(OnMessage);
        try
        {
          if (!suspending)
            SystemEvents.PowerModeChanged -= new PowerModeChangedEventHandler(OnSystemPowerModeChanged);
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
        if (refreshTimer != null)
        {
          refreshTimer.Stop();
          refreshTimer.Dispose();
        }

        DisposeHandlers();

        Utils.DelayStop = new Hashtable();
      }
      catch (Exception ex)
      {
        logger.Error("Stop: " + ex.ToString());
      }
    }

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