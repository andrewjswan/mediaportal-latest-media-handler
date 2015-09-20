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


using System.Diagnostics;
using Microsoft.Win32;

namespace LatestMediaHandler
{
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
    private static string mpVersion = null;
    private LatestTVRecordingsWorker MyLatestTVRecordingsWorker = null;
    private LatestReorgWorker MyLatestReorgWorker = null;
    private static string fhThreadPriority = "Lowest";
    //internal static System.Timers.Timer TVRecordingsTimer = null;
    internal static System.Timers.Timer ReorgTimer = null;
    private System.Timers.Timer refreshTimer = null;
    private LatestMediaHandlerConfig xconfig = null;

    private static int restricted = 0; //MovingPicture restricted property
    private Hashtable windowsUsingFanartLatest; //used to know what skin files that supports latest media fanart     

    internal static int SyncPointReorg /* = 0*/;
    internal static int SyncPointTVRecordings /* = 0*/;
    internal static int SyncPointMusicUpdate;
    internal static int SyncPointMvCMusicUpdate;
    internal int SyncPointRefresh;

    private static LatestMyVideosHandler lmvh;
    private static LatestMovingPicturesHandler lmph;
    private static LatestTVSeriesHandler ltvsh;
    private static LatestMusicHandler lmh;
    private static LatestTVRecordingsHandler ltvrh = null;
    private static Latest4TRRecordingsHandler l4trrh = null;
    private static LatestArgusRecordingsHandler largusrh = null;
    private static LatestPictureHandler lph;
    private static LatestMyFilmsHandler lmfh = null;
    private static LatestMvCentralHandler lmch = null;

    //private static int tVRecordingsTimerTick;
    private static int reorgTimerTick;

    #endregion

    /**
         * 919199910 Lmph
         * 919199940 Ltvsh
         * 919199970 Lmh
         * 919199840 if (Utils.Used4TRTV) L4trrh else Ltvrh
         * 919199710 Lph
         * 919199880 Lmfh
         **/

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

    /*internal static int TVRecordingsTimerTick
        {
            get { return LatestMediaHandlerSetup.tVRecordingsTimerTick; }
            set { LatestMediaHandlerSetup.tVRecordingsTimerTick = value; }
        }*/

    internal static int ReorgTimerTick
    {
      get { return LatestMediaHandlerSetup.reorgTimerTick; }
      set { LatestMediaHandlerSetup.reorgTimerTick = value; }
    }

/*        internal static string UseLatestMediaCache
        {
            get { return useLatestMediaCache; }
            set { useLatestMediaCache = value; }
        }*/

    internal static LatestPictureHandler Lph
    {
      get { return lph; }
      set { lph = value; }
    }

    internal static LatestTVRecordingsHandler Ltvrh
    {
      get { return ltvrh; }
      set { ltvrh = value; }
    }

    internal static Latest4TRRecordingsHandler L4trrh
    {
      get { return l4trrh; }
      set { l4trrh = value; }
    }

    internal static LatestArgusRecordingsHandler Largusrh
    {
      get { return largusrh; }
      set { largusrh = value; }
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

    internal static int Restricted
    {
      get { return LatestMediaHandlerSetup.restricted; }
      set { LatestMediaHandlerSetup.restricted = value; }
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

    /*internal static int GetTVRecordingsTimerInterval()
        {
            int newTick = Environment.TickCount - TVRecordingsTimerTick;
            newTick = 300000 - newTick;
            if (newTick < 0)
            {
                newTick = 2000;
            }
            return newTick;
        }*/

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

    private void UpdateTVRecordingsTimer()
    {
      if (Utils.GetIsStopping() == false)
      {
        try
        {
          int sync = Interlocked.CompareExchange(ref SyncPointTVRecordings, 1, 0);
          if (sync == 0)
          {
            // No other event was executing.                                   
            if (MyLatestTVRecordingsWorker == null)
            {
              MyLatestTVRecordingsWorker = new LatestTVRecordingsWorker();
              MyLatestTVRecordingsWorker.RunWorkerCompleted += MyLatestTVRecordingsWorker.OnRunWorkerCompleted;
            }
            MyLatestTVRecordingsWorker.RunWorkerAsync();
          }
        }
        catch (Exception ex)
        {
          SyncPointTVRecordings = 0;
          logger.Error("UpdateTVRecordingsTimer: " + ex.ToString());
        }
      }

    }

    private void UpdateTVRecordingsTimer(Object stateInfo, ElapsedEventArgs e)
    {
      UpdateTVRecordingsTimer();
    }

    /// <summary>
    /// Set start values on variables
    /// </summary>
    private void SetupVariables()
    {
      Utils.SetIsStopping(false);
      Restricted = 0;

      Lmvh  = new LatestMyVideosHandler();
      Lmph  = new LatestMovingPicturesHandler();
      Ltvsh = new LatestTVSeriesHandler();
      Lmh   = new LatestMusicHandler();
      Lph   = new LatestPictureHandler();
      Lmfh  = new LatestMyFilmsHandler();
      Lmch  = new LatestMvCentralHandler();

      SyncPointReorg = 0;
      SyncPointTVRecordings = 0;
      SyncPointMusicUpdate = 0;
      SyncPointMvCMusicUpdate = 0;
      SyncPointRefresh = 0;

      //TVRecordingsTimerTick = Environment.TickCount;
      ReorgTimerTick = Environment.TickCount;

      EmptyLatestMediaPropsMusic();
      EmptyLatestMediaPropsMyVideos();
      EmptyLatestMediaPropsPictures();
      EmptyLatestMediaPropsTVRecordings();
      EmptyLatestMediaPropsTVSeries();
      EmptyLatestMediaPropsMyFilms();
      EmptyLatestMediaPropsMovingPictures();
      EmptyLatestMediaPropsMvCentral();
      EmptyRecordingProps();
    }

    internal static void EmptyRecordingProps()
    {
      SetProperty("#latestMediaHandler.tvrecordings.label", Translation.LabelLatestAdded);
      //Active Recordings
      for (int z = 1; z < 4; z++)
      {
        SetProperty("#latestMediaHandler.tvrecordings.active" + z + ".title", string.Empty);
        SetProperty("#latestMediaHandler.tvrecordings.active" + z + ".genre", string.Empty);
        SetProperty("#latestMediaHandler.tvrecordings.active" + z + ".startTime", string.Empty);
        SetProperty("#latestMediaHandler.tvrecordings.active" + z + ".startDate", string.Empty);
        SetProperty("#latestMediaHandler.tvrecordings.active" + z + ".endTime", string.Empty);
        SetProperty("#latestMediaHandler.tvrecordings.active" + z + ".endDate", string.Empty);
        SetProperty("#latestMediaHandler.tvrecordings.active" + z + ".channel", string.Empty);
        SetProperty("#latestMediaHandler.tvrecordings.active" + z + ".channelLogo", string.Empty);
      }

      //Scheduled recordings
      for (int z = 1; z < 4; z++)
      {
        SetProperty("#latestMediaHandler.tvrecordings.scheduled" + z + ".title", string.Empty);
        SetProperty("#latestMediaHandler.tvrecordings.scheduled" + z + ".startTime", string.Empty);
        SetProperty("#latestMediaHandler.tvrecordings.scheduled" + z + ".startDate", string.Empty);
        SetProperty("#latestMediaHandler.tvrecordings.scheduled" + z + ".endTime", string.Empty);
        SetProperty("#latestMediaHandler.tvrecordings.scheduled" + z + ".endDate", string.Empty);
        SetProperty("#latestMediaHandler.tvrecordings.scheduled" + z + ".channel", string.Empty);
        SetProperty("#latestMediaHandler.tvrecordings.scheduled" + z + ".channelLogo", string.Empty);
      }
    }

    internal static void EmptyLatestMediaPropsMovingPictures()
    {
      SetProperty("#latestMediaHandler.movingpicture.label", Translation.LabelLatestAdded);
      SetProperty("#latestMediaHandler.movingpicture.latest.enabled", "false");
      SetProperty("#latestMediaHandler.movingpicture.hasnew", "false");
      for (int z = 1; z < 4; z++)
      {
        SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".thumb", string.Empty);
        SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".fanart", string.Empty);
        SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".title", string.Empty);
        SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".dateAdded", string.Empty);
        SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".genre", string.Empty);
        SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".rating", string.Empty);
        SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".roundedRating", string.Empty);
        SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".classification", string.Empty);
        SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".runtime", string.Empty);
        SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".year", string.Empty);
        SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".id", string.Empty);
        SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".plot", string.Empty);
      }
    }

    internal static void EmptyLatestMediaPropsMvCentral()
    {
      SetProperty("#latestMediaHandler.mvcentral.label", Translation.LabelLatestAdded);
      SetProperty("#latestMediaHandler.mvcentral.hasnew", "false");
      for (int z = 1; z < 4; z++)
      {
        SetProperty("#latestMediaHandler.mvcentral.latest" + z + ".thumb", string.Empty);
        SetProperty("#latestMediaHandler.mvcentral.latest" + z + ".artist", string.Empty);
        SetProperty("#latestMediaHandler.mvcentral.latest" + z + ".album", string.Empty);
        SetProperty("#latestMediaHandler.mvcentral.latest" + z + ".track", string.Empty);
        SetProperty("#latestMediaHandler.mvcentral.latest" + z + ".dateAdded", string.Empty);
        SetProperty("#latestMediaHandler.mvcentral.latest" + z + ".fanart", string.Empty);
        SetProperty("#latestMediaHandler.mvcentral.latest" + z + ".genre", string.Empty);
      }
    }

    internal static void EmptyLatestMediaPropsMusic()
    {
      SetProperty("#latestMediaHandler.music.label", Translation.LabelLatestAdded);
      if (LatestMediaHandlerSetup.LatestMusicType.Equals("Most Played Music"))
        SetProperty("#latestMediaHandler.music.label", Translation.LabelMostPlayed);
      else if (LatestMediaHandlerSetup.LatestMusicType.Equals("Latest Played Music"))
        SetProperty("#latestMediaHandler.music.label", Translation.LabelLatestPlayed);

      SetProperty("#latestMediaHandler.music.latest.enabled", "false");
      SetProperty("#latestMediaHandler.music.hasnew", "false");
      for (int z = 1; z < 4; z++)
      {
        SetProperty("#latestMediaHandler.music.latest" + z + ".thumb", string.Empty);
        SetProperty("#latestMediaHandler.music.latest" + z + ".artist", string.Empty);
        SetProperty("#latestMediaHandler.music.latest" + z + ".album", string.Empty);
        SetProperty("#latestMediaHandler.music.latest" + z + ".dateAdded", string.Empty);
        SetProperty("#latestMediaHandler.music.latest" + z + ".fanart", string.Empty);
        //SetProperty("#latestMediaHandler.music.latest" + z + ".fanart2", string.Empty);
        SetProperty("#latestMediaHandler.music.latest" + z + ".genre", string.Empty);
      }
    }

    internal static void EmptyLatestMediaPropsPictures()
    {
      SetProperty("#latestMediaHandler.picture.label", Translation.LabelLatestAdded);
      SetProperty("#latestMediaHandler.picture.latest.enabled", "false");
      SetProperty("#latestMediaHandler.picture.hasnew", "false");
      for (int z = 1; z < 4; z++)
      {
        SetProperty("#latestMediaHandler.picture.latest" + z + ".title", string.Empty);
        SetProperty("#latestMediaHandler.picture.latest" + z + ".thumb", string.Empty);
        SetProperty("#latestMediaHandler.picture.latest" + z + ".filename", string.Empty);
        SetProperty("#latestMediaHandler.picture.latest" + z + ".dateAdded", string.Empty);
      }
    }

    internal static void EmptyLatestMediaPropsMyFilms()
    {
      SetProperty("#latestMediaHandler.myfilms.label", Translation.LabelLatestAdded);
      SetProperty("#latestMediaHandler.myfilms.latest.enabled", "false");
      SetProperty("#latestMediaHandler.myfilms.hasnew", "false");
      for (int z = 1; z < 4; z++)
      {
        SetProperty("#latestMediaHandler.myfilms.latest" + z + ".poster", string.Empty);
        SetProperty("#latestMediaHandler.myfilms.latest" + z + ".fanart", string.Empty);
        SetProperty("#latestMediaHandler.myfilms.latest" + z + ".title", string.Empty);
        SetProperty("#latestMediaHandler.myfilms.latest" + z + ".dateAdded", string.Empty);
        SetProperty("#latestMediaHandler.myfilms.latest" + z + ".rating", string.Empty);
        SetProperty("#latestMediaHandler.myfilms.latest" + z + ".roundedRating", string.Empty);
        SetProperty("#latestMediaHandler.myfilms.latest" + z + ".year", string.Empty);
        SetProperty("#latestMediaHandler.myfilms.latest" + z + ".id", string.Empty);
      }
    }

    internal static void EmptyLatestMediaPropsMyVideos()
    {
      SetProperty("#latestMediaHandler.myvideo.label", Translation.LabelLatestAdded);
      SetProperty("#latestMediaHandler.myvideo.latest.enabled", "false");
      SetProperty("#latestMediaHandler.myvideo.hasnew", "false");
      for (int z = 1; z < 4; z++)
      {
        SetProperty("#latestMediaHandler.myvideo.latest" + z + ".thumb", string.Empty);
        SetProperty("#latestMediaHandler.myvideo.latest" + z + ".fanart", string.Empty);
        SetProperty("#latestMediaHandler.myvideo.latest" + z + ".title", string.Empty);
        SetProperty("#latestMediaHandler.myvideo.latest" + z + ".dateAdded", string.Empty);
        SetProperty("#latestMediaHandler.myvideo.latest" + z + ".genre", string.Empty);
        SetProperty("#latestMediaHandler.myvideo.latest" + z + ".rating", string.Empty);
        SetProperty("#latestMediaHandler.myvideo.latest" + z + ".roundedRating", string.Empty);
        SetProperty("#latestMediaHandler.myvideo.latest" + z + ".classification", string.Empty);
        SetProperty("#latestMediaHandler.myvideo.latest" + z + ".runtime", string.Empty);
        SetProperty("#latestMediaHandler.myvideo.latest" + z + ".year", string.Empty);
        SetProperty("#latestMediaHandler.myvideo.latest" + z + ".id", string.Empty);
        SetProperty("#latestMediaHandler.myvideo.latest" + z + ".plot", string.Empty);
      }
    }

    internal static void EmptyLatestMediaPropsTVSeries()
    {
      SetProperty("#latestMediaHandler.tvseries.label", Translation.LabelLatestAdded);
      SetProperty("#latestMediaHandler.tvseries.latest.enabled", "false");
      SetProperty("#latestMediaHandler.tvseries.hasnew", "false");
      for (int z = 1; z < 4; z++)
      {
        SetProperty("#latestMediaHandler.tvseries.latest" + z + ".thumb", string.Empty);
        SetProperty("#latestMediaHandler.tvseries.latest" + z + ".serieThumb", string.Empty);
        SetProperty("#latestMediaHandler.tvseries.latest" + z + ".fanart", string.Empty);
        SetProperty("#latestMediaHandler.tvseries.latest" + z + ".serieName", string.Empty);
        SetProperty("#latestMediaHandler.tvseries.latest" + z + ".seasonIndex", string.Empty);
        SetProperty("#latestMediaHandler.tvseries.latest" + z + ".episodeName", string.Empty);
        SetProperty("#latestMediaHandler.tvseries.latest" + z + ".episodeIndex", string.Empty);
        SetProperty("#latestMediaHandler.tvseries.latest" + z + ".dateAdded", string.Empty);
        SetProperty("#latestMediaHandler.tvseries.latest" + z + ".genre", string.Empty);
        SetProperty("#latestMediaHandler.tvseries.latest" + z + ".rating", string.Empty);
        SetProperty("#latestMediaHandler.tvseries.latest" + z + ".roundedRating", string.Empty);
        SetProperty("#latestMediaHandler.tvseries.latest" + z + ".classification", string.Empty);
        SetProperty("#latestMediaHandler.tvseries.latest" + z + ".runtime", string.Empty);
        SetProperty("#latestMediaHandler.tvseries.latest" + z + ".firstAired", string.Empty);
        SetProperty("#latestMediaHandler.tvseries.latest" + z + ".plot", string.Empty);
      }
    }

    internal static void EmptyLatestMediaPropsTVRecordings()
    {
      SetProperty("#latestMediaHandler.tvrecordings.label", Translation.LabelLatestAdded);
      SetProperty("#latestMediaHandler.tvrecordings.latest.enabled", "false");
      SetProperty("#latestMediaHandler.tvrecordings.hasnew", "false");
      for (int z = 1; z < 4; z++)
      {
        SetProperty("#latestMediaHandler.tvrecordings.latest" + z + ".thumb", string.Empty);
        SetProperty("#latestMediaHandler.tvrecordings.latest" + z + ".title", string.Empty);
        SetProperty("#latestMediaHandler.tvrecordings.latest" + z + ".dateAdded", string.Empty);
        SetProperty("#latestMediaHandler.tvrecordings.latest" + z + ".genre", string.Empty);
      }
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

        LatestMediaHandlerSetup.Restricted = 0;
        try
        {
          LatestMediaHandlerSetup.Restricted = Lmph.MovingPictureIsRestricted();
        }
        catch { }

        GetLatestMediaInfo();

        GUIWindowManager.OnActivateWindow += new GUIWindowManager.WindowActivationHandler(GuiWindowManagerOnActivateWindow);

        GUIGraphicsContext.OnNewAction += new OnActionHandler(OnNewAction);
        GUIWindowManager.Receivers += new SendMessageHandler(OnMessage);
        SystemEvents.PowerModeChanged += OnSystemPowerModeChanged;

        ReorgTimer = new System.Timers.Timer((Int32.Parse(ReorgInterval)*60000));
        ReorgTimer.Elapsed += new ElapsedEventHandler(UpdateReorgTimer);
        ReorgTimer.Interval = (Int32.Parse(ReorgInterval)*60000);
        ReorgTimer.Start();

        refreshTimer = new System.Timers.Timer(250);
        refreshTimer.Elapsed += new ElapsedEventHandler(UpdateImageTimer);
        refreshTimer.Interval = 250;
        refreshTimer.Start();

        try
        {
          UtilsFanartHandler.SetupFanartHandlerSubcribeScaperFinishedEvent();
        }
        catch (Exception ex)
        {
          logger.Error("SetupFanartHandlerSubcribeScaperFinishedEvent: " + ex.ToString());
        }

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
          GetLatestTVRecMediaInfo();
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
      if (fWindow.GetFocusControlId() == 919199910)
      {
        Lmph.MyContextMenu();
      }
      else if (fWindow.GetFocusControlId() == 919198710)
      {
        Lmvh.MyContextMenu();
      }
      else if (fWindow.GetFocusControlId() == 919299280)
      {
        Lmch.MyContextMenu();
      }
      else if (fWindow.GetFocusControlId() == 919199940)
      {
        Ltvsh.MyContextMenu();
      }
      else if (fWindow.GetFocusControlId() == 919199880)
      {
        Lmfh.MyContextMenu();
      }
      else if (fWindow.GetFocusControlId() == 919199840)
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
      else if (fWindow.GetFocusControlId() == 919199970)
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
        switch (message.Message)
        {
          case GUIMessage.MessageType.GUI_MSG_NOTIFY_REC:
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
            // GetLatestMediaInfoTV();
            break;
          }
          case GUIMessage.MessageType.GUI_MSG_VIDEOINFO_REFRESH:
          {
            logger.Debug("VideoInfo refresh detected: Refreshing fanarts.");
            try
            {
              GetLatestMediaInfoVideo();
              GetLatestMediaInfoPictures();
            }
            catch (Exception ex)
            {
              logger.Error("GUIWindowManager_OnNewMessage: " + ex.ToString());
            }
            break;
          }
        case GUIMessage.MessageType.GUI_MSG_PLAYBACK_ENDED:
        case GUIMessage.MessageType.GUI_MSG_PLAYBACK_STOPPED:
          {
            logger.Debug("Playback End/Stop detected: Refreshing latest.");
            try
            {
              GetLatestMediaInfoVideo();
              GetLatestMediaInfoMusic();
              GetLatestMediaInfoPictures();
            }
            catch (Exception ex)
            {
              logger.Error("GUIWindowManager_OnNewMessage: " + ex.ToString());
            }
            break;
          }
          case GUIMessage.MessageType.GUI_MSG_DATABASE_SCAN_ENDED:
          {
            logger.Debug("DB Scan end detected: Refreshing latest.");
            try
            {
              GetLatestMediaInfoMusic();
              GetLatestMediaInfoPictures();
            }
            catch (Exception ex)
            {
              logger.Error("GUIWindowManager_OnNewMessage: " + ex.ToString());
            }
            break;
          }
          case GUIMessage.MessageType.GUI_MSG_START_SLIDESHOW:
          {
            logger.Debug("Slideshow detected: Refreshing latest.");
            try
            {
              GetLatestMediaInfoPictures();
            }
            catch (Exception ex)
            {
              logger.Error("GUIWindowManager_OnNewMessage: " + ex.ToString());
            }
            break;
          }
        }
      }
      catch
      {
        //DO NOTHING!!
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
            case MediaPortal.GUI.Library.Action.ActionType.ACTION_MOUSE_CLICK:
            case MediaPortal.GUI.Library.Action.ActionType.ACTION_SELECT_ITEM:
              if (fWindow != null)
              {
                if (fWindow.GetFocusControlId() == 919199910 || fWindow.GetFocusControlId() == 919199710 ||
                    fWindow.GetFocusControlId() == 919199940 || fWindow.GetFocusControlId() == 919199970 || 
                    fWindow.GetFocusControlId() == 919199840 || fWindow.GetFocusControlId() == 919199880 ||
                    fWindow.GetFocusControlId() == 919198710 || fWindow.GetFocusControlId() == 919299280)
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
                }
                else if (fWindow.GetFocusControlId() == 91919991)
                {
                  try
                  {
                    Lmph.PlayMovingPicture(1);
                  }
                  catch (Exception ex)
                  {
                    MessageBox.Show("Unable to play movie! " + ex.ToString());
                  }
                }
                else if (fWindow.GetFocusControlId() == 91919992)
                {
                  try
                  {
                    Lmph.PlayMovingPicture(2);
                  }
                  catch (Exception ex)
                  {
                    MessageBox.Show("Unable to play movie! " + ex.ToString());
                  }
                }
                else if (fWindow.GetFocusControlId() == 91919993)
                {
                  try
                  {
                    Lmph.PlayMovingPicture(3);
                  }
                  catch (Exception ex)
                  {
                    MessageBox.Show("Unable to play movie! " + ex.ToString());
                  }
                }
                else if (fWindow.GetFocusControlId() == 91919994)
                {
                  try
                  {
                    Ltvsh.PlayTVSeries(1);
                  }
                  catch (Exception ex)
                  {
                    MessageBox.Show("Unable to play episode! " + ex.ToString());
                  }
                  MediaPortal.Playlists.PlayListPlayer.SingletonPlayer.PlayNext();
                }
                else if (fWindow.GetFocusControlId() == 91919995)
                {
                  try
                  {
                    Ltvsh.PlayTVSeries(2);
                  }
                  catch (Exception ex)
                  {
                    MessageBox.Show("Unable to play episode! " + ex.ToString());
                  }
                }
                else if (fWindow.GetFocusControlId() == 91919996)
                {
                  try
                  {
                    Ltvsh.PlayTVSeries(3);
                  }
                  catch (Exception ex)
                  {
                    MessageBox.Show("Unable to play episode! " + ex.ToString());
                  }
                }
                else if (fWindow.GetFocusControlId() == 91919997)
                {
                  try
                  {
                    Lmh.PlayMusicAlbum(1);
                  }
                  catch (Exception ex)
                  {
                    MessageBox.Show("Unable to play album! " + ex.ToString());
                  }
                }
                else if (fWindow.GetFocusControlId() == 91919998)
                {
                  try
                  {
                    Lmh.PlayMusicAlbum(2);
                  }
                  catch (Exception ex)
                  {
                    MessageBox.Show("Unable to play album! " + ex.ToString());
                  }
                }
                else if (fWindow.GetFocusControlId() == 91919999)
                {
                  try
                  {
                    Lmh.PlayMusicAlbum(3);
                  }
                  catch (Exception ex)
                  {
                    MessageBox.Show("Unable to play album! " + ex.ToString());
                  }
                }
                else if (fWindow.GetFocusControlId() == 91919984)
                {
                  try
                  {
                    if (Utils.Used4TRTV && !Utils.UsedArgus)
                    {
                      action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_KEY_PRESSED;
                      L4trrh.PlayRecording(1);
                    }
                    else if (Utils.Used4TRTV && Utils.UsedArgus)
                    {
                      action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_KEY_PRESSED;
                      Largusrh.PlayRecording(1);
                    }
                    else
                    {
                      Ltvrh.PlayRecording(1);
                    }
                  }
                  catch (Exception ex)
                  {
                    MessageBox.Show("Unable to play recording! " + ex.ToString());
                  }
                }
                else if (fWindow.GetFocusControlId() == 91919985)
                {
                  try
                  {
                    if (Utils.Used4TRTV && !Utils.UsedArgus)
                    {
                      action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_KEY_PRESSED;
                      L4trrh.PlayRecording(2);
                    }
                    else if (Utils.Used4TRTV && Utils.UsedArgus)
                    {
                      action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_KEY_PRESSED;
                      Largusrh.PlayRecording(2);
                    }
                    else
                    {
                      Ltvrh.PlayRecording(2);
                    }
                  }
                  catch (Exception ex)
                  {
                    MessageBox.Show("Unable to play recording! " + ex.ToString());
                  }
                }
                else if (fWindow.GetFocusControlId() == 91919986)
                {
                  try
                  {
                    if (Utils.Used4TRTV && !Utils.UsedArgus)
                    {
                      action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_KEY_PRESSED;
                      L4trrh.PlayRecording(3);
                    }
                    else if (Utils.Used4TRTV && Utils.UsedArgus)
                    {
                      action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_KEY_PRESSED;
                      Largusrh.PlayRecording(3);
                    }
                    else
                    {
                      Ltvrh.PlayRecording(3);
                    }

                  }
                  catch (Exception ex)
                  {
                    MessageBox.Show("Unable to play recording! " + ex.ToString());
                  }
                }
                else if (fWindow.GetFocusControlId() == 91919987)
                {
                  try
                  {
                    if (Utils.Used4TRTV && !Utils.UsedArgus)
                    {
                      action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_KEY_PRESSED;
                      L4trrh.PlayRecording(4);
                    }
                    else if (Utils.Used4TRTV && Utils.UsedArgus)
                    {
                      action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_KEY_PRESSED;
                      Largusrh.PlayRecording(4);
                    }
                    else
                    {
                      Ltvrh.PlayRecording(4);
                    }

                  }
                  catch (Exception ex)
                  {
                    MessageBox.Show("Unable to play recording! " + ex.ToString());
                  }
                }
                else if (fWindow.GetFocusControlId() == 91919988)
                {
                  try
                  {
                    lmfh.PlayMovie(1);
                  }
                  catch (Exception ex)
                  {
                    MessageBox.Show("Unable to play film! " + ex.ToString());
                  }
                }
                else if (fWindow.GetFocusControlId() == 91919989)
                {
                  try
                  {
                    lmfh.PlayMovie(2);
                  }
                  catch (Exception ex)
                  {
                    MessageBox.Show("Unable to play film! " + ex.ToString());
                  }
                }
                else if (fWindow.GetFocusControlId() == 91919990)
                {
                  try
                  {
                    lmfh.PlayMovie(3);
                  }
                  catch (Exception ex)
                  {
                    MessageBox.Show("Unable to play film! " + ex.ToString());
                  }
                }
                else if (fWindow.GetFocusControlId() == 91915991)
                {
                  try
                  {
                    lmvh.PlayMovie(1);
                  }
                  catch (Exception ex)
                  {
                    MessageBox.Show("Unable to play film! " + ex.ToString());
                  }
                }
                else if (fWindow.GetFocusControlId() == 91915992)
                {
                  try
                  {
                    lmvh.PlayMovie(2);
                  }
                  catch (Exception ex)
                  {
                    MessageBox.Show("Unable to play film! " + ex.ToString());
                  }
                }
                else if (fWindow.GetFocusControlId() == 91915993)
                {
                  try
                  {
                    lmvh.PlayMovie(3);
                  }
                  catch (Exception ex)
                  {
                    MessageBox.Show("Unable to play film! " + ex.ToString());
                  }
                }
              }

              break;
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

    internal static void GetLatestTVRecMediaInfo()
    {
      int z = 1;
      if (LatestTVRecordings.Equals("True", StringComparison.CurrentCulture))
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
              latestTVRecordings = Largusrh.Get4TRRecordings();
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
        if ((latestTVRecordings != null && latestTVRecordings.Count > 1) && GUIPropertyManager.GetProperty("#latestMediaHandler.tvrecordings.latest" + z + ".title").Equals(latestTVRecordings[0].Title))
        {
          noNewRecordings = true;
          logger.Info("Updating Latest Media Info: Latest tv recording: No new recordings since last check!");
        }

        if (latestTVRecordings != null && latestTVRecordings.Count > 1)
        {
          /*if (GUIPropertyManager.GetProperty("#latestMediaHandler.tvrecordings.latest" + z + ".title").Equals(latestTVRecordings[0].Title))
                    {
                        logger.Info("Updating Latest Media Info: Latest tv recording: No new recordings since last check!");
                    }
                    else*/
          if (!noNewRecordings)
          {
            for (int i = 0; i < 4; i++)
            {
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.latest" + z + ".thumb", string.Empty);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.latest" + z + ".title", string.Empty);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.latest" + z + ".dateAdded", string.Empty);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.latest" + z + ".genre", string.Empty);
              z++;
            }

            z = 1;
            for (int i = 0; i < latestTVRecordings.Count && i < 4; i++)
            {
              logger.Info("Updating Latest Media Info: Latest tv recording " + z + ": " + latestTVRecordings[i].Title);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.latest" + z + ".thumb", latestTVRecordings[i].Thumb);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.latest" + z + ".title", latestTVRecordings[i].Title);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.latest" + z + ".dateAdded", latestTVRecordings[i].DateAdded);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.latest" + z + ".genre", latestTVRecordings[i].Genre);
              z++;
            }
            //latestTVRecordings.Clear();
            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.latest.enabled", "true");
            logger.Debug("Updating Latest Media Info: Latest tv recording has new: " + (Utils.HasNewTVRecordings ? "true" : "false"));
          }
        }
        else
        {
          for (int i = 0; i < 4; i++)
          {
            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.latest" + z + ".thumb", string.Empty);
            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.latest" + z + ".title", string.Empty);
            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.latest" + z + ".dateAdded", string.Empty);
            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.latest" + z + ".genre", string.Empty);
            z++;
          }
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.latest.enabled", "false");
          logger.Info("Updating Latest Media Info: Latest tv recording: No recordings found!");
        }
        //latestTVRecordings = null;
        z = 1;
      }
      else
      {
        LatestMediaHandlerSetup.EmptyLatestMediaPropsTVRecordings();
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
          x = 919199910;
        }
        else if (obj is LatestMyVideosHandler)
        {
          x = 919198710;
        }
        else if (obj is LatestMvCentralHandler)
        {
          x = 919299280;
        }
        else if (obj is LatestTVSeriesHandler)
        {
          x = 919199940;
        }
        else if (obj is LatestMusicHandler)
        {
          x = 919199970;
        }
        else if (obj is LatestTVRecordingsHandler)
        {
          x = 919199840;
        }
        else if (obj is Latest4TRRecordingsHandler)
        {
          x = 919199840;
        }
        else if (obj is LatestPictureHandler)
        {
          x = 919199710;
        }
        else if (obj is LatestMyFilmsHandler)
        {
          x = 919199880;
        }

        GUIFacadeControl facade = gw.GetControl(x) as GUIFacadeControl;

        if (facade != null)
        {
          facade.Clear();
          if (x == 919199910)
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
          else if (x == 919198710)
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
          else if (x == 919299280)
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
          else if (x == 919199940)
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
          else if (x == 919199970)
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
          else if (x == 919199840 && Utils.Used4TRTV && !Utils.UsedArgus)
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
          else if (x == 919199840 && Utils.Used4TRTV && Utils.UsedArgus)
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
          else if (x == 919199840 && !Utils.Used4TRTV)
          {
            if (Ltvrh != null && Ltvrh.Al != null)
            {
              for (int i = 0; i < Ltvrh.Al.Count; i++)
              {
                GUIListItem _gc = Ltvrh.Al[i] as GUIListItem;
                facade.Add(_gc);
                Utils.LoadImage(_gc.IconImage, ref Ltvrh.imagesThumbs);
              }
            }
          }
          else if (x == 919199710)
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
          else if (x == 919199880)
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
            if (!facade.Focus)
            {
              facade.ListLayout.IsVisible = false;
            }
          }
          else if (facade.FilmstripLayout != null)
          {
            facade.CurrentLayout = GUIFacadeControl.Layout.Filmstrip;
            if (!facade.Focus)
            {
              facade.FilmstripLayout.IsVisible = false;
            }
          }
          else if (facade.CoverFlowLayout != null)
          {
            facade.CurrentLayout = GUIFacadeControl.Layout.CoverFlow;
            if (!facade.Focus)
            {
              facade.CoverFlowLayout.IsVisible = false;
            }
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
      if (x == 919199910)
      {
        if (Lmph != null)
        {
          facade.SelectedListItemIndex = Lmph.LastFocusedId;
        }
      }
      else if (x == 919198710)
      {
        if (Lmvh != null)
        {
          facade.SelectedListItemIndex = Lmvh.LastFocusedId;
        }
      }
      else if (x == 919299280)
      {
        if (Lmch != null)
        {
          facade.SelectedListItemIndex = Lmch.LastFocusedId;
        }
      }
      else if (x == 919199940)
      {
        if (Ltvsh != null)
        {
          facade.SelectedListItemIndex = Ltvsh.LastFocusedId;
        }
      }
      else if (x == 919199970)
      {
        if (Lmh != null)
        {
          facade.SelectedListItemIndex = Lmh.LastFocusedId;
        }
      }
      else if (x == 919199840 && Utils.Used4TRTV && !Utils.UsedArgus)
      {
        if (L4trrh != null)
        {
          facade.SelectedListItemIndex = L4trrh.LastFocusedId;
        }
      }
      else if (x == 919199840 && Utils.Used4TRTV && Utils.UsedArgus)
      {
        if (Largusrh != null)
        {
          facade.SelectedListItemIndex = Largusrh.LastFocusedId;
        }
      }
      else if (x == 919199840 && !Utils.Used4TRTV)
      {
        if (Ltvrh != null)
        {
          facade.SelectedListItemIndex = Ltvrh.LastFocusedId;
        }
      }
      else if (x == 919199710)
      {
        if (Lph != null)
        {
          facade.SelectedListItemIndex = Lph.LastFocusedId;
        }
      }
      else if (x == 919199880)
      {
        if (Lmfh != null)
        {
          facade.SelectedListItemIndex = Lmfh.LastFocusedId;
        }
      }
    }

    internal void GuiWindowManagerOnActivateWindow(int activeWindowId)
    {
      try
      {
        string windowId = String.Empty + GUIWindowManager.ActiveWindow;
        if (Utils.GetIsStopping() == false && WindowsUsingFanartLatest.ContainsKey(windowId))
        {
          /*if (TVRecordingsTimer != null && !TVRecordingsTimer.Enabled)
                    {                        
                        TVRecordingsTimer.Interval = GetTVRecordingsTimerInterval();
                        TVRecordingsTimer.Start();
                    }*/
          if (ReorgTimer != null && !ReorgTimer.Enabled)
          {
            ReorgTimer.Interval = GetReorgTimerInterval();
            ReorgTimer.Start();
          }
          //Thread thread = new Thread(new ThreadStart(ReloadLatestCache));
          //thread.Start();

          InitFacade(Lmph, activeWindowId);
          InitFacade(Ltvsh, activeWindowId);
          InitFacade(Lmh, activeWindowId);
          if (Utils.Used4TRTV && !Utils.UsedArgus)
          {
            InitFacade(L4trrh, activeWindowId);
          }
          else if (Utils.Used4TRTV && Utils.UsedArgus)
          {
            InitFacade(Largusrh, activeWindowId);
          }
          else
          {
            InitFacade(Ltvrh, activeWindowId);
          }
          InitFacade(Lph, activeWindowId);
          InitFacade(Lmfh, activeWindowId);
          InitFacade(Lmvh, activeWindowId);
          InitFacade(Lmch, activeWindowId);
          if (refreshTimer != null && !refreshTimer.Enabled)
          {
            refreshTimer.Start();
          }
          UpdateTVRecordingsTimer();
        }
        else
        {
          if (refreshTimer != null && refreshTimer.Enabled)
          {
            refreshTimer.Stop();
          }
          if (ReorgTimer != null && ReorgTimer.Enabled)
          {
            ReorgTimer.Stop();
          }
          GUIWindow gw = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow);
          GUIFacadeControl facade = gw.GetControl(919199910) as GUIFacadeControl;
          if (facade != null)
          {
            facade.Clear();
            Utils.UnLoadImage(ref Lmph.images);
            Utils.UnLoadImage(ref Lmph.imagesThumbs);
          }
          facade = gw.GetControl(919198710) as GUIFacadeControl;
          if (facade != null)
          {
            facade.Clear();
            Utils.UnLoadImage(ref Lmvh.images);
            Utils.UnLoadImage(ref Lmvh.imagesThumbs);
          }
          facade = gw.GetControl(919299280) as GUIFacadeControl;
          if (facade != null)
          {
            facade.Clear();
            Utils.UnLoadImage(ref Lmch.images);
            Utils.UnLoadImage(ref Lmch.imagesThumbs);
          }
          facade = gw.GetControl(919199940) as GUIFacadeControl;
          if (facade != null)
          {
            facade.Clear();
            Utils.UnLoadImage(ref Ltvsh.images);
            Utils.UnLoadImage(ref Ltvsh.imagesThumbs);
          }
          facade = gw.GetControl(919199970) as GUIFacadeControl;
          if (facade != null)
          {
            facade.Clear();
            Utils.UnLoadImage(ref Lmh.images);
            Utils.UnLoadImage(ref Lmh.imagesThumbs);
          }
          facade = gw.GetControl(919199840) as GUIFacadeControl;
          if (facade != null)
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
          facade = gw.GetControl(919199710) as GUIFacadeControl;
          if (facade != null)
          {
            facade.Clear();
            Utils.UnLoadImage(ref Lph.images);
            Utils.UnLoadImage(ref Lph.imagesThumbs);
          }
          facade = gw.GetControl(919199880) as GUIFacadeControl;
          if (facade != null)
          {
            facade.Clear();
            Utils.UnLoadImage(ref Lmfh.images);
            Utils.UnLoadImage(ref Lmfh.imagesThumbs);
          }

          //Thread thread = new Thread(new ThreadStart(UnloadLatestCache));
          //thread.Start();                    
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
        int sync = Interlocked.CompareExchange(ref SyncPointRefresh, 1, 0);
        if (sync == 0)
        {
          if (Utils.IsIdle())
          {
            GUIWindow fWindow = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow);
            if (fWindow != null)
            {
              /*                            Utils.LogDevMsg("*******************************");
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
                                                        Utils.LogDevMsg("L4trrh.imagesThumbs.Count:" + L4trrh.imagesThumbs.Count);*/

              if (LatestMyVideos.Equals("True", StringComparison.CurrentCulture))
              {
                try
                {
                  if (fWindow.GetFocusControlId() == 919198710)
                  {
                    Lmvh.UpdateSelectedImageProperties();
                    Lmvh.NeedCleanup = true;
                  }
                  else
                  {
                    if (Lmvh.NeedCleanup && Lmvh.NeedCleanupCount >= 5)
                    {
                      LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myvideo.selected.fanart1", " ");
                      LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myvideo.selected.fanart2", " ");
                      Utils.UnLoadImage(ref Lmvh.images);
                      Lmvh.ShowFanart = 1;
                      Lmvh.SelectedFacadeItem2 = -1;
                      Lmvh.SelectedFacadeItem2 = -1;
                      Lmvh.NeedCleanup = false;
                      Lmvh.NeedCleanupCount = 0;
                    }
                    else if (Lmvh.NeedCleanup && Lmvh.NeedCleanupCount == 0)
                    {
                      LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myvideo.selected.showfanart1", "");
                      LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myvideo.selected.showfanart2", "");
                      Lmvh.NeedCleanupCount++;
                    }
                    else if (Lmvh.NeedCleanup)
                    {
                      Lmvh.NeedCleanupCount++;
                    }
                  }
                }
                catch (Exception ex)
                {
                  logger.Error("UpdateImageTimer (myvideo): " + ex.ToString());
                }
              }
              if (LatestMvCentral.Equals("True", StringComparison.CurrentCulture))
              {
                try
                {
                  if (fWindow.GetFocusControlId() == 919299280)
                  {
                    Lmch.UpdateSelectedImageProperties();
                    Lmch.NeedCleanup = true;
                  }
                  else
                  {
                    if (Lmch.NeedCleanup && Lmch.NeedCleanupCount >= 5)
                    {
                      LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.mvcentral.selected.fanart1", " ");
                      LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.mvcentral.selected.fanart2", " ");
                      Utils.UnLoadImage(ref Lmch.images);
                      Lmch.ShowFanart = 1;
                      Lmch.SelectedFacadeItem2 = -1;
                      Lmch.SelectedFacadeItem2 = -1;
                      Lmch.NeedCleanup = false;
                      Lmch.NeedCleanupCount = 0;
                    }
                    else if (Lmch.NeedCleanup && Lmch.NeedCleanupCount == 0)
                    {
                      LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.mvcentral.selected.showfanart1", "");
                      LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.mvcentral.selected.showfanart2", "");
                      Lmch.NeedCleanupCount++;
                    }
                    else if (Lmch.NeedCleanup)
                    {
                      Lmch.NeedCleanupCount++;
                    }
                  }
                }
                catch (Exception ex)
                {
                  logger.Error("UpdateImageTimer (mvcentral): " + ex.ToString());
                }
              }
              if (LatestMovingPictures.Equals("True", StringComparison.CurrentCulture))
              {
                try
                {
                  if (fWindow.GetFocusControlId() == 919199910)
                  {
                    Lmph.UpdateSelectedImageProperties();
                    Lmph.NeedCleanup = true;
                  }
                  else
                  {
                    if (Lmph.NeedCleanup && Lmph.NeedCleanupCount >= 5)
                    {
                      LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.selected.fanart1", " ");
                      LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.selected.fanart2", " ");
                      Utils.UnLoadImage(ref Lmph.images);
                      Lmph.ShowFanart = 1;
                      Lmph.SelectedFacadeItem2 = -1;
                      Lmph.SelectedFacadeItem2 = -1;
                      Lmph.NeedCleanup = false;
                      Lmph.NeedCleanupCount = 0;
                    }
                    else if (Lmph.NeedCleanup && Lmph.NeedCleanupCount == 0)
                    {
                      LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.selected.showfanart1", "");
                      LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.selected.showfanart2", "");
                      Lmph.NeedCleanupCount++;
                    }
                    else if (Lmph.NeedCleanup)
                    {
                      Lmph.NeedCleanupCount++;
                    }
                  }
                }
                catch (Exception ex)
                {
                  logger.Error("UpdateImageTimer (movingpicture): " + ex.ToString());
                }
              }
              if (LatestTVSeries.Equals("True", StringComparison.CurrentCulture))
              {
                try
                {
                  if (fWindow.GetFocusControlId() == 919199940)
                  {
                    Ltvsh.UpdateSelectedImageProperties();
                    Ltvsh.NeedCleanup = true;
                  }
                  else
                  {
                    if (Ltvsh.NeedCleanup && Ltvsh.NeedCleanupCount >= 5)
                    {
                      LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.selected.fanart1", " ");
                      LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.selected.fanart2", " ");
                      Utils.UnLoadImage(ref Ltvsh.images);
                      Ltvsh.ShowFanart = 1;
                      Ltvsh.SelectedFacadeItem2 = -1;
                      Ltvsh.SelectedFacadeItem2 = -1;
                      Ltvsh.NeedCleanup = false;
                      Ltvsh.NeedCleanupCount = 0;
                    }
                    else if (Ltvsh.NeedCleanup && Ltvsh.NeedCleanupCount == 0)
                    {
                      LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.selected.showfanart1", "");
                      LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.selected.showfanart2", "");
                      Ltvsh.NeedCleanupCount++;
                    }
                    else if (Ltvsh.NeedCleanup)
                    {
                      Ltvsh.NeedCleanupCount++;
                    }
                  }
                }
                catch (Exception ex)
                {
                  logger.Error("UpdateImageTimer (tvseries latest): " + ex.ToString());
                }
              }
              if (LatestMusic.Equals("True", StringComparison.CurrentCulture))
              {
                try
                {
                  if (fWindow.GetFocusControlId() == 919199970)
                  {
                    Lmh.UpdateSelectedImageProperties();
                    Lmh.NeedCleanup = true;
                  }
                  else
                  {
                    if (Lmh.NeedCleanup && Lmh.NeedCleanupCount >= 5)
                    {
                      LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.music.selected.fanart1", " ");
                      LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.music.selected.fanart2", " ");
                      Utils.UnLoadImage(ref Lmh.images);
                      Lmh.ShowFanart = 1;
                      Lmh.SelectedFacadeItem2 = -1;
                      Lmh.SelectedFacadeItem2 = -1;
                      Lmh.NeedCleanup = false;
                      Lmh.NeedCleanupCount = 0;
                    }
                    else if (Lmh.NeedCleanup && Lmh.NeedCleanupCount == 0)
                    {
                      LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.music.selected.showfanart1", "");
                      LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.music.selected.showfanart2", "");
                      Lmh.NeedCleanupCount++;
                    }
                    else if (Lmh.NeedCleanup)
                    {
                      Lmh.NeedCleanupCount++;
                    }
                  }
                }
                catch (Exception ex)
                {
                  logger.Error("UpdateImageTimer (music): " + ex.ToString());
                }
              }
              if (LatestPictures.Equals("True", StringComparison.CurrentCulture))
              {
                try
                {
                  if (fWindow.GetFocusControlId() == 919199710)
                  {
                    Lph.UpdateSelectedImageProperties();
                    Lph.NeedCleanup = true;
                  }
                  else
                  {
                    if (Lph.NeedCleanup && Lph.NeedCleanupCount >= 5)
                    {
                      LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.picture.selected.fanart1", " ");
                      LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.picture.selected.fanart2", " ");
                      Utils.UnLoadImage(ref Lph.images);
                      Lph.ShowFanart = 1;
                      Lph.SelectedFacadeItem2 = -1;
                      Lph.SelectedFacadeItem2 = -1;
                      Lph.NeedCleanup = false;
                      Lph.NeedCleanupCount = 0;
                    }
                    else if (Lph.NeedCleanup && Lph.NeedCleanupCount == 0)
                    {
                      LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.picture.selected.showfanart1", "");
                      LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.picture.selected.showfanart2", "");
                      Lph.NeedCleanupCount++;
                    }
                    else if (Lph.NeedCleanup)
                    {
                      Lph.NeedCleanupCount++;
                    }
                  }
                }
                catch (Exception ex)
                {
                  logger.Error("UpdateImageTimer (picture): " + ex.ToString());
                }
              }
              if (LatestMyFilms.Equals("True", StringComparison.CurrentCulture))
              {
                try
                {
                  if (fWindow.GetFocusControlId() == 919199880)
                  {
                    Lmfh.UpdateSelectedImageProperties();
                    Lmfh.NeedCleanup = true;
                  }
                  else
                  {
                    if (Lmfh.NeedCleanup && Lmfh.NeedCleanupCount >= 5)
                    {
                      LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myfilms.selected.fanart1", " ");
                      LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myfilms.selected.fanart2", " ");
                      Utils.UnLoadImage(ref Lmfh.images);
                      Lmfh.ShowFanart = 1;
                      Lmfh.SelectedFacadeItem2 = -1;
                      Lmfh.SelectedFacadeItem2 = -1;
                      Lmfh.NeedCleanup = false;
                      Lmfh.NeedCleanupCount = 0;
                    }
                    else if (Lmfh.NeedCleanup && Lmfh.NeedCleanupCount == 0)
                    {
                      LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myfilms.selected.showfanart1", "");
                      LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myfilms.selected.showfanart2", "");
                      Lmfh.NeedCleanupCount++;
                    }
                    else if (Lmfh.NeedCleanup)
                    {
                      Lmfh.NeedCleanupCount++;
                    }
                  }
                }
                catch (Exception ex)
                {
                  logger.Error("UpdateImageTimer (myfilms): " + ex.ToString());
                }
              }
              if (LatestTVRecordings.Equals("True", StringComparison.CurrentCulture))
              {
                try
                {
                  if (fWindow.GetFocusControlId() == 919199840)
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
                        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.selected.fanart1", " ");
                        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.selected.fanart2", " ");
                        Utils.UnLoadImage(ref L4trrh.images);
                        L4trrh.ShowFanart = 1;
                        L4trrh.SelectedFacadeItem2 = -1;
                        L4trrh.SelectedFacadeItem2 = -1;
                        L4trrh.NeedCleanup = false;
                        L4trrh.NeedCleanupCount = 0;
                      }
                      else if (L4trrh.NeedCleanup && L4trrh.NeedCleanupCount == 0)
                      {
                        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.selected.showfanart1", "");
                        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.selected.showfanart2", "");
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
                        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.selected.fanart1", " ");
                        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.selected.fanart2", " ");
                        Utils.UnLoadImage(ref Largusrh.images);
                        Largusrh.ShowFanart = 1;
                        Largusrh.SelectedFacadeItem2 = -1;
                        Largusrh.SelectedFacadeItem2 = -1;
                        Largusrh.NeedCleanup = false;
                        Largusrh.NeedCleanupCount = 0;
                      }
                      else if (Largusrh.NeedCleanup && Largusrh.NeedCleanupCount == 0)
                      {
                        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.selected.showfanart1", "");
                        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.selected.showfanart2", "");
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
                        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.selected.fanart1", " ");
                        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.selected.fanart2", " ");
                        Utils.UnLoadImage(ref Ltvrh.images);
                        Ltvrh.ShowFanart = 1;
                        Ltvrh.SelectedFacadeItem2 = -1;
                        Ltvrh.SelectedFacadeItem2 = -1;
                        Ltvrh.NeedCleanup = false;
                        Ltvrh.NeedCleanupCount = 0;
                      }
                      else if (Ltvrh.NeedCleanup && Ltvrh.NeedCleanupCount == 0)
                      {
                        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.selected.showfanart1", "");
                        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.selected.showfanart2", "");
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

            // Check TV Series need update ...
            if (LatestTVSeries.Equals("True", StringComparison.CurrentCulture))
            {
              try
              {
                string sLastOnlineUpdate = GUIPropertyManager.GetProperty("#TVSeries.LastOnlineUpdate");
                if (!string.IsNullOrEmpty(sLastOnlineUpdate))
                  try
                  {
                    DateTime dTmp = DateTime.Parse(sLastOnlineUpdate);
                    if (dTmp > Utils.TVSeriesLastUpdate)
                    {
                      try
                      {
                        Ltvsh.TVSeriesUpdateLatest(LatestTVSeriesHandler.Types.Latest, LatestMediaHandlerSetup.LatestTVSeriesWatched.Equals("True"));
                        Utils.TVSeriesLastUpdate = DateTime.Now ;
                      }
                      catch (Exception ex)
                      {
                        logger.Error("UpdateImageTimer (TV Series Online Update): " + ex.ToString());
                      }
                    }
                  }
                  catch
                  {   }
              }
              catch (Exception ex)
              {
                logger.Error("UpdateImageTimer (TV Series Online Update): " + ex.ToString());
              }
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
        GUIWindowManager.OnActivateWindow -=
          new GUIWindowManager.WindowActivationHandler(GuiWindowManagerOnActivateWindow);
        /*if (LatestMyVideos.Equals("True", StringComparison.CurrentCulture))
                {
                    GUIWindowManager.Receivers -= new SendMessageHandler(GUIWindowManager_OnNewMessage);
                }*/
        GUIGraphicsContext.OnNewAction -= new OnActionHandler(OnNewAction);
        GUIWindowManager.Receivers -= new SendMessageHandler(OnMessage);
        int ix = 0;
        try
        {
          UtilsFanartHandler.DisposeFanartHandlerSubcribeScaperFinishedEvent();
        }
        catch
        {
        }
        while (Utils.GetDelayStop() && ix < 20)
        {
          System.Threading.Thread.Sleep(500);
          ix++;
        }
        /*if (TVRecordingsTimer != null)
                {
                    TVRecordingsTimer.Stop();
                    TVRecordingsTimer.Dispose();
                }*/
        if (ReorgTimer != null)
        {
          ReorgTimer.Stop();
          ReorgTimer.Dispose();
        }
        if (MyLatestTVRecordingsWorker != null)
        {
          MyLatestTVRecordingsWorker.CancelAsync();
          MyLatestTVRecordingsWorker.Dispose();
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
        try
        {
          Lmph.DisposeMovingPicturesLatest();
        }
        catch
        {
        }
        try
        {
          Ltvsh.DisposeTVSeriesLatest();
        }
        catch
        {
        }
        try
        {
          Lmfh.DisposeMovieLatest();
        }
        catch
        {
        }
        try
        {
          if (!suspending)
          {
            SystemEvents.PowerModeChanged -= OnSystemPowerModeChanged;
          }
        }
        catch
        {
        }
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