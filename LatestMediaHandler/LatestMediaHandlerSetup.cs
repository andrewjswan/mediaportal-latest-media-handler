//***********************************************************************
// Assembly         : LatestMediaHandler
// Author           : cul8er
// Created          : 05-09-2010
//
// Last Modified By : cul8er
// Last Modified On : 10-05-2010
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
    private static string fhThreadPriority = "Lowest";
    internal static System.Timers.Timer ReorgTimer = null;
    private System.Timers.Timer refreshTimer = null;

    private LatestMediaHandlerConfig xconfig = null;
    private LatestReorgWorker MyLatestReorgWorker = null;

    private Hashtable windowsUsingFanartLatest; //used to know what skin files that supports latest media fanart     

    internal static int SyncPointReorg;
    internal static int SyncPointTVRecordings;
    internal static int SyncPointMusicUpdate;
    internal static int SyncPointMvCMusicUpdate;

    internal int SyncPointRefresh;

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

    internal static string FHThreadPriority
    {
      get { return LatestMediaHandlerSetup.fhThreadPriority; }
      set { LatestMediaHandlerSetup.fhThreadPriority = value; }
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

    internal static void HandleOldImages(ref ArrayList al)
    {
      try
      {
        if (al != null && al.Count > 1)
        {
          int i = 0;
          while (i < (al.Count - 1))
          {
            //unload old image to free MP resource
            UNLoadImage(al[i].ToString());

            //remove old no longer used image
            al.RemoveAt(i);
          }
        }
      }
      catch (Exception ex)
      {
        logger.Error("HandleOldImages: " + ex.ToString());
      }
    }

    internal static void EmptyAllImages(ref ArrayList al)
    {
      try
      {
        if (al != null)
        {
          foreach (Object obj in al)
          {
            //unload old image to free MP resource
            if (obj != null)
            {
              UNLoadImage(obj.ToString());
            }
          }

          //remove old no longer used image
          al.Clear();
        }
      }
      catch (Exception ex)
      {
        //do nothing
        logger.Error("EmptyAllImages: " + ex.ToString());
      }
    }

    internal static void SetProperty(string property, string value)
    {
      try
      {
        if (property == null)
          return;

        GUIPropertyManager.SetProperty(property, value);
      }
      catch (Exception ex)
      {
        logger.Error("SetProperty: " + ex.ToString());
      }
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

          if (!string.IsNullOrEmpty(windowId) && windowId.Length > 0)
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
              {
                WindowsUsingFanartLatest.Add(windowId, windowId);
              }
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
      _flagLatest = (_xml.Contains(".latest.") && _xml.Contains("#latestMediaHandler."));

      sb = null;
    }

    private void UpdateReorgTimer(Object stateInfo, ElapsedEventArgs e)
    {
      if (Utils.GetIsStopping() == false)
      {
        try
        {
          int sync = Interlocked.CompareExchange(ref SyncPointReorg, 1, 0);
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
          //else
          //{
          //    SyncPointReorg = 0;
          //}
        }
        catch (Exception ex)
        {
          SyncPointReorg = 0;
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

      Lmvh  = new LatestMyVideosHandler();
      Lmph  = new LatestMovingPicturesHandler();
      Ltvsh = new LatestTVSeriesHandler();
      Lmh   = new LatestMusicHandler();
      Lph   = new LatestPictureHandler();
      Lmfh  = new LatestMyFilmsHandler();
      Lmch  = new LatestMvCentralHandler();
      ltvrh = new LatestTVAllRecordingsHandler();

      SyncPointReorg = 0;
      SyncPointTVRecordings = 0;
      SyncPointMusicUpdate = 0;
      SyncPointMvCMusicUpdate = 0;
      SyncPointRefresh = 0;

      ReorgTimerTick = Environment.TickCount;

      Lmh.EmptyLatestMediaPropsMusic();
      Lmvh.EmptyLatestMediaPropsMyVideos();
      Lph.EmptyLatestMediaPropsPictures();
      Ltvsh.EmptyLatestMediaPropsTVSeries();
      Lmph.EmptyLatestMediaPropsMovingPictures();
      Lmfh.EmptyLatestMediaPropsMyFilms();
      Lmch.EmptyLatestMediaPropsMvCentral();
      ltvrh.EmptyLatestMediaPropsTVRecordings();
      ltvrh.EmptyRecordingProps();
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

      string myThreadPriority = xmlreader.GetValue("general", "ThreadPriority");

      if (myThreadPriority != null && myThreadPriority.Equals("Normal", StringComparison.CurrentCulture))
      {
        FHThreadPriority = "Lowest";
      }
      else if (myThreadPriority != null && myThreadPriority.Equals("BelowNormal", StringComparison.CurrentCulture))
      {
        FHThreadPriority = "Lowest";
      }
      else
      {
        FHThreadPriority = "BelowNormal";
      }

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

        /*
        GUIWindow fWindow = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow);
        if (fWindow != null)
        {
          GUIWindowManager.ActivateWindow(fWindow.GetID, true, true, fWindow.GetFocusControlId());
        }
        */
        RefreshActiveWindow();

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
          StartupWorker MyStartupWorker = new StartupWorker();
          MyStartupWorker.RunWorkerCompleted += MyStartupWorker.OnRunWorkerCompleted;
          MyStartupWorker.RunWorkerAsync(Lmh);
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
          StartupWorker MyStartupWorker = new StartupWorker();
          MyStartupWorker.RunWorkerCompleted += MyStartupWorker.OnRunWorkerCompleted;
          MyStartupWorker.RunWorkerAsync(Lph);
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
          StartupWorker MyStartupWorker = new StartupWorker();
          MyStartupWorker.RunWorkerCompleted += MyStartupWorker.OnRunWorkerCompleted;
          MyStartupWorker.RunWorkerAsync(Lmvh);
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
          StartupWorker MyStartupWorker = new StartupWorker();
          MyStartupWorker.RunWorkerCompleted += MyStartupWorker.OnRunWorkerCompleted;
          MyStartupWorker.RunWorkerAsync(Ltvsh);
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
          StartupWorker MyStartupWorker = new StartupWorker();
          MyStartupWorker.RunWorkerCompleted += MyStartupWorker.OnRunWorkerCompleted;
          MyStartupWorker.RunWorkerAsync(Lmph);
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
          StartupWorker MyStartupWorker = new StartupWorker();
          MyStartupWorker.RunWorkerCompleted += MyStartupWorker.OnRunWorkerCompleted;
          MyStartupWorker.RunWorkerAsync(Lmfh);
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
          StartupWorker MyStartupWorker = new StartupWorker();
          MyStartupWorker.RunWorkerCompleted += MyStartupWorker.OnRunWorkerCompleted;
          MyStartupWorker.RunWorkerAsync(Lmch);
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
      if (fWindow.GetFocusControlId() == LatestMovingPicturesHandler.ControlID)
      {
        Lmph.MyContextMenu();
      }
      else if (fWindow.GetFocusControlId() == LatestMyVideosHandler.ControlID)
      {
        Lmvh.MyContextMenu();
      }
      else if (fWindow.GetFocusControlId() == LatestMvCentralHandler.ControlID)
      {
        Lmch.MyContextMenu();
      }
      else if (fWindow.GetFocusControlId() == LatestTVSeriesHandler.ControlID)
      {
        Ltvsh.MyContextMenu();
      }
      else if (fWindow.GetFocusControlId() == LatestMyFilmsHandler.ControlID)
      {
        Lmfh.MyContextMenu();
      }
      else if (fWindow.GetFocusControlId() == LatestTVAllRecordingsHandler.ControlID)
      {
        Ltvrh.MyContextMenu();
      }
      else if (fWindow.GetFocusControlId() == LatestMusicHandler.ControlID)
      {
        Lmh.MyContextMenu();
      }
    }

    private void OnMessage(GUIMessage message)
    {
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

    private void SetActiveLatestControl(GUIWindow fWindow)
    {
      try
      {
        GUIControl gc = null ;

        gc =fWindow.GetControl(LatestMovingPicturesHandler.Play1ControlID);
        if ((gc != null) && (gc.Focusable) && (gc.IsVisible))
          GUIControl.FocusControl(fWindow.GetID, gc.GetID) ;
        /*
        gc = fWindow.GetControl(LatestPictureHandler.Play1ControlID);
        if ((gc != null) && (gc.Focusable) && (gc.IsVisible))
          GUIControl.FocusControl(fWindow.GetID, gc.GetID) ;
        */
        gc = fWindow.GetControl(LatestTVSeriesHandler.Play1ControlID);
        if ((gc != null) && (gc.Focusable) && (gc.IsVisible))
          GUIControl.FocusControl(fWindow.GetID, gc.GetID) ;
        
        gc = fWindow.GetControl(LatestMusicHandler.Play1ControlID);
        if ((gc != null) && (gc.Focusable) && (gc.IsVisible))
          GUIControl.FocusControl(fWindow.GetID, gc.GetID) ;
        
        gc = fWindow.GetControl(LatestTVAllRecordingsHandler.Play1ControlID);
        if ((gc != null) && (gc.Focusable) && (gc.IsVisible))
          GUIControl.FocusControl(fWindow.GetID, gc.GetID) ;
        
        gc = fWindow.GetControl(LatestMyFilmsHandler.Play1ControlID);
        if ((gc != null) && (gc.Focusable) && (gc.IsVisible))
          GUIControl.FocusControl(fWindow.GetID, gc.GetID) ;
        
        gc = fWindow.GetControl(LatestMyVideosHandler.Play1ControlID);
        if ((gc != null) && (gc.Focusable) && (gc.IsVisible))
          GUIControl.FocusControl(fWindow.GetID, gc.GetID) ;
        /*
        gc = fWindow.GetControl(LatestMvCentralHandler.Play1ControlID);
        if ((gc != null) && (gc.Focusable) && (gc.IsVisible))
          fWindow.FocusControl(fWindow.GetID, gc.GetID) ;
        */
      }
      catch (Exception ex)
      {
        logger.Error("SetActiveLatestControl: " + ex.ToString());
      }
    }

    internal void OnAction(GUIWindow fWindow, ref MediaPortal.GUI.Library.Action action, bool ContextMenu) 
    {
      if (fWindow != null)
      {
        if (fWindow.GetFocusControlId() == LatestMovingPicturesHandler.ControlID || 
            fWindow.GetFocusControlId() == LatestPictureHandler.ControlID ||
            fWindow.GetFocusControlId() == LatestTVSeriesHandler.ControlID || 
            fWindow.GetFocusControlId() == LatestMusicHandler.ControlID || 
            fWindow.GetFocusControlId() == LatestTVAllRecordingsHandler.ControlID || 
            fWindow.GetFocusControlId() == LatestMyFilmsHandler.ControlID ||
            fWindow.GetFocusControlId() == LatestMyVideosHandler.ControlID || 
            fWindow.GetFocusControlId() == LatestMvCentralHandler.ControlID)
        {
          if (action.IsUserAction())
          {
            GUIGraphicsContext.ResetLastActivity();
          }
          //action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_KEY_PRESSED;                                    
          action.wID = 0;
          //DoContextMenu();
          ShowLMHDialog slmhd = new ShowLMHDialog();
          GUIWindowManager.SendThreadMessage(new GUIMessage()
                                             {
                                               TargetWindowId = 35,
                                               SendToTargetWindow = true,
                                               Object = slmhd
                                             });
          //GUIWindowManager.SendMessage(new GUIMessage() { TargetWindowId = 35, SendToTargetWindow = true, Object = slmhd });
          return;
        }
        if (ContextMenu) 
        {
          SetActiveLatestControl(fWindow);
          return;
        }

        if (Lmh.PlayMusicAlbum(fWindow)) return;
        if (lmvh.PlayMovie(fWindow)) return;
        if (Ltvsh.PlayTVSeries(fWindow)) return;
        if (Lmph.PlayMovingPicture(fWindow)) return;
        if (lmfh.PlayMovie(fWindow)) return;
        if (ltvrh.PlayRecording(fWindow, ref action)) return;
      }
    }

    private void OnNewAction(MediaPortal.GUI.Library.Action action)
    {
      try
      {
        if (GUIWindowManager.ActiveWindow == 35 && GUIWindowManager.RoutedWindow == -1)
        {
          GUIWindow fWindow = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow);

          switch (action.wID)
          {
            case MediaPortal.GUI.Library.Action.ActionType.ACTION_CONTEXT_MENU:
            {
              OnAction(fWindow, ref action, true);
              break;
            }
            case MediaPortal.GUI.Library.Action.ActionType.ACTION_MOUSE_CLICK:
            case MediaPortal.GUI.Library.Action.ActionType.ACTION_SELECT_ITEM:
            {
              OnAction(fWindow, ref action, false);
              break;
            }
            default:
              break;
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
        {
          x = LatestMovingPicturesHandler.ControlID;
        }
        else if (obj is LatestMyVideosHandler)
        {
          x = LatestMyVideosHandler.ControlID;
        }
        else if (obj is LatestMvCentralHandler)
        {
          x = LatestMvCentralHandler.ControlID;
        }
        else if (obj is LatestTVSeriesHandler)
        {
          x = LatestTVSeriesHandler.ControlID;
        }
        else if (obj is LatestMusicHandler)
        {
          x = LatestMusicHandler.ControlID;
        }
        else if (obj is LatestTVRecordingsHandler)
        {
          x = LatestTVAllRecordingsHandler.ControlID;
        }
        else if (obj is Latest4TRRecordingsHandler)
        {
          x = LatestTVAllRecordingsHandler.ControlID;
        }
        else if (obj is LatestPictureHandler)
        {
          x = LatestPictureHandler.ControlID;
        }
        else if (obj is LatestMyFilmsHandler)
        {
          x = LatestMyFilmsHandler.ControlID;
        }

        GUIFacadeControl facade = gw.GetControl(x) as GUIFacadeControl;

        if (facade != null)
        {
          facade.Clear();
          if (x == LatestMovingPicturesHandler.ControlID)
          {
            if (Lmph != null && Lmph.Al != null)
            {
              for (int i = 0; i < Lmph.Al.Count; i++)
              {
                GUIListItem _gc = Lmph.Al[i] as GUIListItem;
                Utils.LoadImage(_gc.IconImage, ref Lmph.imagesThumbs);
                facade.Add(_gc);
              }
            }
          }
          else if (x == LatestMyVideosHandler.ControlID)
          {
            if (Lmvh != null && Lmvh.Al != null)
            {
              for (int i = 0; i < Lmvh.Al.Count; i++)
              {
                GUIListItem _gc = Lmvh.Al[i] as GUIListItem;
                Utils.LoadImage(_gc.IconImage, ref Lmvh.imagesThumbs);
                facade.Add(_gc);
              }
            }
          }
          else if (x == LatestMvCentralHandler.ControlID)
          {
            if (Lmch != null && Lmch.Al != null)
            {
              for (int i = 0; i < Lmch.Al.Count; i++)
              {
                GUIListItem _gc = Lmch.Al[i] as GUIListItem;
                Utils.LoadImage(_gc.IconImage, ref Lmch.imagesThumbs);
                facade.Add(_gc);
              }
            }
          }
          else if (x == LatestTVSeriesHandler.ControlID)
          {
            if (Ltvsh != null && Ltvsh.Al != null)
            {
              for (int i = 0; i < Ltvsh.Al.Count; i++)
              {
                GUIListItem _gc = Ltvsh.Al[i] as GUIListItem;
                Utils.LoadImage(_gc.IconImage, ref Ltvsh.imagesThumbs);
                facade.Add(_gc);
              }
            }
          }
          else if (x == LatestMusicHandler.ControlID)
          {
            if (Lmh != null && Lmh.Al != null)
            {
              for (int i = 0; i < Lmh.Al.Count; i++)
              {
                GUIListItem _gc = Lmh.Al[i] as GUIListItem;
                Utils.LoadImage(_gc.IconImage, ref Lmh.imagesThumbs);
                facade.Add(_gc);
              }
            }
          }
          else if (x == LatestTVAllRecordingsHandler.ControlID)
          {
            if (Ltvrh != null)
              Ltvrh.InitFacade(ref facade) ;
          }
          else if (x == LatestPictureHandler.ControlID)
          {
            if (Lph != null && Lph.Al != null)
            {
              for (int i = 0; i < Lph.Al.Count; i++)
              {
                GUIListItem _gc = Lph.Al[i] as GUIListItem;
                Utils.LoadImage(_gc.IconImage, ref Lph.imagesThumbs);
                facade.Add(_gc);
              }
            }
          }
          else if (x == LatestMyFilmsHandler.ControlID)
          {
            if (Lmfh != null && Lmfh.Al != null)
            {
              for (int i = 0; i < Lmfh.Al.Count; i++)
              {
                GUIListItem _gc = Lmfh.Al[i] as GUIListItem;
                Utils.LoadImage(_gc.IconImage, ref Lmfh.imagesThumbs);
                facade.Add(_gc);
              }
            }
          }

          SetFocusOnFacade(facade, x);
          if (facade.ListLayout != null)
          {
            facade.CurrentLayout = GUIFacadeControl.Layout.List;
            facade.ListLayout.IsVisible = (!facade.Focus) ? false : facade.ListLayout.IsVisible;
          }
          else if (facade.FilmstripLayout != null)
          {
            facade.CurrentLayout = GUIFacadeControl.Layout.Filmstrip;
            facade.FilmstripLayout.IsVisible = (!facade.Focus) ? false : facade.FilmstripLayout.IsVisible;
          }
          else if (facade.CoverFlowLayout != null)
          {
            facade.CurrentLayout = GUIFacadeControl.Layout.CoverFlow;
            facade.CoverFlowLayout.IsVisible = (!facade.Focus) ? false : facade.CoverFlowLayout.IsVisible;
          }
          facade.Visible = false;
        }
      }
      catch (Exception ex)
      {
        logger.Error("InitFacade: " + ex.ToString());
      }
    }

    internal void SetFocusOnFacade(GUIFacadeControl facade, int x)
    {
      if (x == LatestMovingPicturesHandler.ControlID)
      {
        if (Lmph != null)
          facade.SelectedListItemIndex = Lmph.LastFocusedId;
      }
      else if (x == LatestMyVideosHandler.ControlID)
      {
        if (Lmvh != null)
          facade.SelectedListItemIndex = Lmvh.LastFocusedId;
      }
      else if (x == LatestMvCentralHandler.ControlID)
      {
        if (Lmch != null)
          facade.SelectedListItemIndex = Lmch.LastFocusedId;
      }
      else if (x == LatestTVSeriesHandler.ControlID)
      {
        if (Ltvsh != null)
          facade.SelectedListItemIndex = Ltvsh.LastFocusedId;
      }
      else if (x == LatestMusicHandler.ControlID)
      {
        if (Lmh != null)
          facade.SelectedListItemIndex = Lmh.LastFocusedId;
      }
      else if (x == LatestTVAllRecordingsHandler.ControlID)
      {
        if (Ltvrh != null)
          facade.SelectedListItemIndex = Ltvrh.LastFocusedId;
      }
      else if (x == LatestPictureHandler.ControlID)
      {
        if (Lph != null)
          facade.SelectedListItemIndex = Lph.LastFocusedId;
      }
      else if (x == LatestMyFilmsHandler.ControlID)
      {
        if (Lmfh != null)
          facade.SelectedListItemIndex = Lmfh.LastFocusedId;
      }
    }

    internal void GuiWindowManagerOnActivateWindow(int activeWindowId)
    {
      try
      {
        string windowId = String.Empty + GUIWindowManager.ActiveWindow;
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

          GUIWindow gw = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow);
          GUIFacadeControl facade = gw.GetControl(LatestMovingPicturesHandler.ControlID) as GUIFacadeControl;
          if (facade != null)
          {
            facade.Clear();
            Utils.UnLoadImage(ref Lmph.images);
            Utils.UnLoadImage(ref Lmph.imagesThumbs);
          }
          facade = gw.GetControl(LatestMyVideosHandler.ControlID) as GUIFacadeControl;
          if (facade != null)
          {
            facade.Clear();
            Utils.UnLoadImage(ref Lmvh.images);
            Utils.UnLoadImage(ref Lmvh.imagesThumbs);
          }
          facade = gw.GetControl(LatestMvCentralHandler.ControlID) as GUIFacadeControl;
          if (facade != null)
          {
            facade.Clear();
            Utils.UnLoadImage(ref Lmch.images);
            Utils.UnLoadImage(ref Lmch.imagesThumbs);
          }
          facade = gw.GetControl(LatestTVSeriesHandler.ControlID) as GUIFacadeControl;
          if (facade != null)
          {
            facade.Clear();
            Utils.UnLoadImage(ref Ltvsh.images);
            Utils.UnLoadImage(ref Ltvsh.imagesThumbs);
          }
          facade = gw.GetControl(LatestMusicHandler.ControlID) as GUIFacadeControl;
          if (facade != null)
          {
            facade.Clear();
            Utils.UnLoadImage(ref Lmh.images);
            Utils.UnLoadImage(ref Lmh.imagesThumbs);
          }
          facade = gw.GetControl(LatestTVAllRecordingsHandler.ControlID) as GUIFacadeControl;
          if (facade != null)
          {
            Ltvrh.ClearFacade(ref facade) ;
          }
          facade = gw.GetControl(LatestPictureHandler.ControlID) as GUIFacadeControl;
          if (facade != null)
          {
            facade.Clear();
            Utils.UnLoadImage(ref Lph.images);
            Utils.UnLoadImage(ref Lph.imagesThumbs);
          }
          facade = gw.GetControl(LatestMyFilmsHandler.ControlID) as GUIFacadeControl;
          if (facade != null)
          {
            facade.Clear();
            Utils.UnLoadImage(ref Lmfh.images);
            Utils.UnLoadImage(ref Lmfh.imagesThumbs);
          }
        }
      }
      catch (Exception ex)
      {
        logger.Error("GUIWindowManager_OnActivateWindow: " + ex.ToString());
      }
    }

    internal static void UpdateLatestMediaInfo()
    {
      Ltvrh.UpdateLatestMediaInfo();
    }

    private void UpdateImageTimer(Object stateInfo, ElapsedEventArgs e)
    {
      try
      {
        int sync = Interlocked.CompareExchange(ref SyncPointRefresh, 1, 0);
        if (sync == 0)
        {
          if (Utils.IsIdle())
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
          SyncPointRefresh = 0;
        }
      }
      catch (Exception ex)
      {
        SyncPointRefresh = 0;
        logger.Error("UpdateImageTimer: " + ex.ToString());
      }

    }

    /// <summary>
    /// UnLoad image (free memory)
    /// </summary>
    private static void UNLoadImage(string filename)
    {
      try
      {
        GUITextureManager.ReleaseTexture(filename);
      }
      catch (Exception ex)
      {
        logger.Error("UnLoadImage: " + ex.ToString());
      }
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
      return "cul8er (maintained by yoavain, ajs)";
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