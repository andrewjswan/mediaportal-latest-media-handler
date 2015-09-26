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

using MediaPortal.GUI.Library;
using MediaPortal.Configuration;

using RealNLog.NLog;

using System;
using System.Collections;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Threading;

namespace LatestMediaHandler
{
  /// <summary>
  /// Utility class used by the Latest Media Handler plugin.
  /// </summary>
  internal static class Utils
  {
    #region declarations

    private static Logger logger = LogManager.GetCurrentClassLogger();
    private const string RXMatchNonWordCharacters = @"[^\w|;]";
    private const string RXMatchMPvs = @"({)([0-9]+)(})$"; // MyVideos fanart scraper filename index
    private const string RXMatchMPvs2 = @"(\()([0-9]+)(\))$"; // MyVideos fanart scraper filename index

    private static bool isStopping /* = false*/; //is the plugin about to stop, then this will be true
    private static Hashtable delayStop = null;

    private static bool used4TRTV = false;
    private static bool usedArgus = false;
    private static DateTime lastRefreshRecording;

    private const string ConfigFilename = "LatestMediaHandler.xml";
    public  const string DefTVSeriesRatings = "TV-Y;TV-Y7;TV-G;TV-PG;TV-14;TV-MA";

    public static string latestPictures { get; set; }
    public static string latestMusic { get; set; }
    public static string latestMusicType { get; set; }
    public static string latestMyVideos { get; set; }
    public static string latestMyVideosWatched { get; set; }
    public static string latestMovingPictures { get; set; }
    public static string latestMovingPicturesWatched { get; set; }
    public static string latestTVSeries { get; set; }
    public static string latestTVSeriesWatched { get; set; }
    public static string latestTVSeriesRatings { get; set; }
    public static string latestTVRecordings { get; set; }
    public static string latestTVRecordingsWatched { get; set; }
    public static string latestTVRecordingsUnfinished { get; set; }
    public static string latestMyFilms { get; set; }
    public static string latestMyFilmsWatched { get; set; }
    public static string latestMvCentral { get; set; }
    public static string refreshDbPicture { get; set; }
    public static string refreshDbMusic { get; set; }
    public static string reorgInterval { get; set; }
    public static string dateFormat { get; set; }

    public static bool HasNewPictures { get; set; }
    public static bool HasNewMusic { get; set; }
    public static bool HasNewMyVideos { get; set; }
    public static bool HasNewMovingPictures { get; set; }
    public static bool HasNewTVSeries { get; set; }
    public static bool HasNewTVRecordings { get; set; }
    public static bool HasNewMyFilms { get; set; }
    public static bool HasNewMvCentral { get; set; }

    public static DateTime NewDateTime { get; set; }

    public static string[] PipesArray ;

    // SyncPoint
    internal static int SyncPointReorg;
    internal static int SyncPointRefresh;

    internal static int SyncPointMusicUpdate;
    internal static int SyncPointPicturesUpdate;
    internal static int SyncPointMyVideosUpdate;
    internal static int SyncPointTVSeriesUpdate;
    internal static int SyncPointMovingPicturesUpdate;
    internal static int SyncPointMyFilmsUpdate;
    internal static int SyncPointTVRecordings;
    internal static int SyncPointMvCMusicUpdate;
    //
    public const int ThreadSleep = 0;

    internal static DateTime LastRefreshRecording
    {
      get { return Utils.lastRefreshRecording; }
      set { Utils.lastRefreshRecording = value; }
    }

    internal static bool Used4TRTV
    {
      get { return Utils.used4TRTV; }
      set { Utils.used4TRTV = value; }
    }

    internal static bool UsedArgus
    {
      get { return Utils.usedArgus; }
      set { Utils.usedArgus = value; }
    }

    #endregion

    /// <summary>
    /// Return value.
    /// </summary>

    internal static Hashtable DelayStop
    {
      get { return Utils.delayStop; }
      set { Utils.delayStop = value; }
    }

    internal static string RemoveLeadingZeros(string s)
    {
      if (s != null)
      {
        char[] charsToTrim = {'0'};
        s = s.TrimStart(charsToTrim);
      }
      return s;
    }

    internal static void LogDevMsg(string msg)
    {
      logger.Debug("DEV MSG: " + msg);
    }

    internal static void AllocateDelayStop(string key)
    {
      if (DelayStop.Contains(key))
        DelayStop[key] = (int)DelayStop[key] + 1;
      else
        DelayStop.Add(key, 1);
    }

    internal static bool GetDelayStop()
    {
      if (DelayStop.Count == 0)
        return false;

      int i = 0;
      foreach (DictionaryEntry de in DelayStop)
      {
        i++;
        logger.Debug("DelayStop (" + i + "):" + de.Key.ToString() + " [" + de.Value.ToString() + "]");
      }
      return true;
    }

    internal static void ReleaseDelayStop(string key)
    {
      if (DelayStop.Contains(key))
      {
        DelayStop[key] = (int)DelayStop[key] - 1;
        if ((int)DelayStop[key] <= 0)
          DelayStop.Remove(key);
      }
    }

    /// <summary>
    /// Return value.
    /// </summary>
    internal static bool GetIsStopping()
    {
      return isStopping;
    }

    /// <summary>
    /// Set value.
    /// </summary>
    internal static void SetIsStopping(bool b)
    {
      isStopping = b;
    }

    internal static void ThreadToSleep()
    {
      Thread.Sleep(Utils.ThreadSleep); 
      // Application.DoEvents();
    }

    /// <summary>
    /// Returns plugin version.
    /// </summary>
    internal static string GetAllVersionNumber()
    {
      return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
    }

    internal static bool ShouldRefreshRecording()
    {
      try
      {
        TimeSpan ts = DateTime.Now - LastRefreshRecording;
        if (ts.TotalMilliseconds >= 300000)
        {
          return true;
        }
      }
      catch (Exception ex)
      {
        logger.Error("ShouldRefreshRecording: " + ex.ToString());
      }
      return false;
    }

    /// <summary>
    /// Load image
    /// </summary>
    internal static void LoadImage(string filename)
    {
      if (isStopping == false)
      {
        try
        {
          if (filename != null && filename.Length > 0)
          {
            GUITextureManager.Load(filename, 0, 0, 0, true);
          }
        }
        catch (Exception ex)
        {
          if (isStopping == false)
          {
            logger.Error("LoadImage (" + filename + "): " + ex.ToString());
          }

        }
      }
    }

    internal static void UNLoadImage(string name)
    {
      try
      {
        GUITextureManager.ReleaseTexture(name);
      }
      catch (Exception ex)
      {
        logger.Error("UnLoadImage: " + ex.ToString());
      }
    }

    [DllImport("gdiplus.dll", CharSet = CharSet.Unicode)]
    private static extern int GdipLoadImageFromFile(string filename, out IntPtr image);

    // Loads an Image from a File by invoking GDI Plus instead of using build-in 
    // .NET methods, or falls back to Image.FromFile. GDI Plus should be faster.
    //Method from MovingPictures plugin.
    internal static Image LoadImageFastFromFile(string filename)
    {
      IntPtr imagePtr = IntPtr.Zero;
      Image image = null;

      try
      {
        if (GdipLoadImageFromFile(filename, out imagePtr) != 0)
        {
          logger.Warn("gdiplus.dll method failed. Will degrade performance.");
          image = Image.FromFile(filename);
        }

        else
          image =
            (Image)
              typeof (Bitmap).InvokeMember("FromGDIplus",
                BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.InvokeMethod, null, null,
                new object[] {imagePtr});
      }
      catch (Exception)
      {
        logger.Error("Failed to load image from " + filename);
        image = null;
      }

      return image;

    }

    /// <summary>
    /// Get filename string.
    /// </summary>
    internal static string GetFilenameNoPath(string key)
    {
      if (key == null)
      {
        return string.Empty;
      }

      if (File.Exists(key))
      {
        key = Path.GetFileName(key);
      }

      key = key.Replace("/", "\\");
      if (key.LastIndexOf("\\", StringComparison.CurrentCulture) >= 0)
      {
        key = key.Substring(key.LastIndexOf("\\", StringComparison.CurrentCulture) + 1);
      }
      return key;
    }


    internal static void LoadImage(string name, ref ArrayList Images)
    {
      try
      {
        if (name == null)
          name = "";

        //load images as MP resource
        if (name != null && name.Length > 0)
        {
          if (Images != null && !Images.Contains(name))
          {
            try
            {
              Images.Add(name);
            }
            catch (Exception ex)
            {
              logger.Error("LoadImage: " + ex.ToString());
            }
            Utils.LoadImage(name);
          }
        }
      }
      catch (Exception ex)
      {
        logger.Error("LoadImage: " + ex.ToString());
      }
    }

    internal static void UnLoadImage(string name, ref ArrayList Images)
    {
      try
      {
        if (name == null)
          name = "";

        //load images as MP resource
        if (name != null && name.Length > 0)
        {
          if (Images != null)
          {
            foreach (Object image in Images)
            {
              //unload old image to free MP resource
              if (image != null && !image.ToString().Equals(name))
              {
                UNLoadImage(image.ToString());
              }
            }
            Images.Clear();
            Images.Add(name);
          }
        }
      }
      catch (Exception ex)
      {
        logger.Error("UnLoadImage: " + ex.ToString());
      }
    }

    internal static void UnLoadImage(ref ArrayList Images)
    {
      try
      {
        if (Images != null)
        {
          foreach (Object image in Images)
          {
            //unload old image to free MP resource
            if (image != null)
            {
              UNLoadImage(image.ToString());
            }
          }
          Images.Clear();
        }
      }
      catch (Exception ex)
      {
        logger.Error("UnLoadImage: " + ex.ToString());
      }
    }

    internal static void UpdateFacade(ref GUIFacadeControl facade, int LastFocusedId = -1000)
    {
      if (facade != null)
      {
        if (LastFocusedId != -1000)
          facade.SelectedListItemIndex = LastFocusedId;

        if (facade.ListLayout != null)
        {
          facade.CurrentLayout = GUIFacadeControl.Layout.List;
          if (!facade.Focus)
            facade.ListLayout.IsVisible = false;
        }
        else if (facade.FilmstripLayout != null)
        {
          facade.CurrentLayout = GUIFacadeControl.Layout.Filmstrip;
          if (!facade.Focus)
            facade.FilmstripLayout.IsVisible = false;
        }
        else if (facade.CoverFlowLayout != null)
        {
          facade.CurrentLayout = GUIFacadeControl.Layout.CoverFlow;
          if (!facade.Focus)
            facade.CoverFlowLayout.IsVisible = false;
        }
        if (!facade.Focus)
          facade.Visible = false;
      }
    }

    internal static bool IsIdle()
    {
      try
      {
        TimeSpan ts = DateTime.Now - GUIGraphicsContext.LastActivity;
        if (ts.TotalMilliseconds >= 350)
        {
          return true;
        }
      }
      catch (Exception ex)
      {
        logger.Error("IsIdle: " + ex.ToString());
      }
      return false;
    }
 
    /// <summary>
    /// Decide if image is corropt or not
    /// </summary>
    internal static bool IsFileValid(string filename)
    {
      if (filename == null)
      {
        return false;
      }

      Image checkImage = null;
      try
      {
        checkImage = Utils.LoadImageFastFromFile(filename);
        if (checkImage != null && checkImage.Width > 0)
        {
          checkImage.Dispose();
          checkImage = null;
          return true;
        }
        if (checkImage != null)
        {
          checkImage.Dispose();
        }
        checkImage = null;
      }
      catch //(Exception ex)
      {
        checkImage = null;
      }
      return false;
    }

    public static string Check(bool Value, bool Box = true)
    {
      return (Box ? "[" : string.Empty) + (Value ? "x" : " ") + (Box ? "]" : string.Empty) ;
    }

    public static string Check(string Value, bool Box = true)
    {
      return (Box ? "[" : string.Empty) + (Value.Equals("True", StringComparison.CurrentCulture) ? "x" : " ") + (Box ? "]" : string.Empty) ;
    }

    public static void SyncPointInit()
    {
      SyncPointReorg = 0;
      SyncPointRefresh = 0;

      SyncPointMusicUpdate = 0;
      SyncPointPicturesUpdate = 0;
      SyncPointMyVideosUpdate = 0;
      SyncPointTVSeriesUpdate = 0;
      SyncPointMovingPicturesUpdate = 0;
      SyncPointMyFilmsUpdate = 0;
      SyncPointTVRecordings = 0;
      SyncPointMvCMusicUpdate = 0;
    }

    public static void HasNewInit()
    {
      HasNewPictures = false;
      HasNewMusic = false;
      HasNewMyVideos = false;
      HasNewMovingPictures = false;
      HasNewTVSeries = false;
      HasNewTVRecordings = false;
      HasNewMyFilms = false;
      HasNewMvCentral = false;

      NewDateTime = DateTime.Now;
      PipesArray = new string[1] { "|" };
   }
    
    public static void LoadSettings(bool Conf = false)
    {
      latestPictures = "True";
      latestMusic = "True";
      latestMusicType = LatestMusicHandler.MusicTypeLatestAdded;
      latestMyVideos = "True";
      latestMyVideosWatched = "True";
      latestMovingPictures = "False";
      latestMovingPicturesWatched = "True";
      latestTVSeries = "True";
      latestTVSeriesWatched = "True";
      latestTVSeriesRatings = "1;1;1;1;1;1";
      latestTVRecordings = "False";
      latestTVRecordingsWatched = "True";
      latestTVRecordingsUnfinished = "True";
      latestMyFilms = "False";
      latestMyFilmsWatched = "True";
      latestMvCentral = "False";

      refreshDbPicture = "False";
      refreshDbMusic = "False";
      reorgInterval = "1440";

      dateFormat = "yyyy-MM-dd";

      try
      {
        logger.Debug("Load settings from: "+ConfigFilename);
        #region Load settings
        using (MediaPortal.Profile.Settings xmlreader = new MediaPortal.Profile.Settings(Config.GetFile(Config.Dir.Config, ConfigFilename)))
        {
          latestPictures = xmlreader.GetValueAsString("LatestMediaHandler", "latestPictures", latestPictures);
          latestMusic = xmlreader.GetValueAsString("LatestMediaHandler", "latestMusic", latestMusic);
          latestMusicType = xmlreader.GetValueAsString("LatestMediaHandler", "latestMusicType", latestMusicType).Trim();
          latestMyVideos = xmlreader.GetValueAsString("LatestMediaHandler", "latestMyVideos", latestMyVideos);
          latestMyVideosWatched = xmlreader.GetValueAsString("LatestMediaHandler", "latestMyVideosWatched", latestMyVideosWatched);
          latestMovingPictures = xmlreader.GetValueAsString("LatestMediaHandler", "latestMovingPictures", latestMovingPictures);
          latestMovingPicturesWatched = xmlreader.GetValueAsString("LatestMediaHandler", "latestMovingPicturesWatched", latestMovingPicturesWatched);
          latestTVSeries = xmlreader.GetValueAsString("LatestMediaHandler", "latestTVSeries", latestTVSeries);
          latestTVSeriesWatched = xmlreader.GetValueAsString("LatestMediaHandler", "latestTVSeriesWatched", latestTVSeriesWatched);
          latestTVSeriesRatings = xmlreader.GetValueAsString("LatestMediaHandler", "latestTVSeriesRatings", latestTVSeriesRatings);
          latestTVRecordings = xmlreader.GetValueAsString("LatestMediaHandler", "latestTVRecordings", latestTVRecordings);
          latestTVRecordingsWatched = xmlreader.GetValueAsString("LatestMediaHandler", "latestTVRecordingsWatched", latestTVRecordingsWatched);
          latestTVRecordingsUnfinished = xmlreader.GetValueAsString("LatestMediaHandler", "latestTVRecordingsUnfinished", latestTVRecordingsUnfinished);
          latestMyFilms = xmlreader.GetValueAsString("LatestMediaHandler", "latestMyFilms", latestMyFilms);
          latestMyFilmsWatched = xmlreader.GetValueAsString("LatestMediaHandler", "latestMyFilmsWatched", latestMyFilmsWatched);
          latestMvCentral = xmlreader.GetValueAsString("LatestMediaHandler", "latestMvCentral", latestMvCentral);
          refreshDbPicture = xmlreader.GetValueAsString("LatestMediaHandler", "refreshDbPicture", refreshDbPicture);
          refreshDbMusic = xmlreader.GetValueAsString("LatestMediaHandler", "refreshDbMusic", refreshDbMusic);
          reorgInterval = xmlreader.GetValueAsString("LatestMediaHandler", "reorgInterval", reorgInterval);
          //useLatestMediaCache = xmlreader.GetValueAsString("LatestMediaHandler", "useLatestMediaCache", useLatestMediaCache);
          dateFormat = xmlreader.GetValueAsString("LatestMediaHandler", "dateFormat", dateFormat);
        }
        #endregion
        logger.Debug("Load settings from: "+ConfigFilename+" complete.");
      }
      catch (Exception ex)
      {
        logger.Error("LoadSettings: "+ex);
      }

      #region Check Settings
      if (!Conf)
        if (!string.IsNullOrEmpty(latestTVSeriesRatings))
        {
/*
          "TV-Y: This program is designed to be appropriate for all children");
          "TV-Y7: This program is designed for children age 7 and above.");
          "TV-G: Most parents would find this program suitable for all ages.");
          "TV-PG: This program contains material that parents may find unsuitable for younger children.");
          "TV-14: This program contains some material that many parents would find unsuitable for children under 14 years of age.");
          "TV-MA: This program is specifically designed to be viewed by adults and therefore may be unsuitable for children under 17.");            
*/
          string[] s = latestTVSeriesRatings.Split(';');
          string[] r = DefTVSeriesRatings.Split(';');

          latestTVSeriesRatings = string.Empty;
          for (int i = 0; i < s.Length; i++)
          {
            if (s[i].Equals("1"))
              latestTVSeriesRatings = latestTVSeriesRatings + (string.IsNullOrEmpty(latestTVSeriesRatings) ? "" : ";") + r[i];
          }
        }
        else
        {
          latestTVSeriesRatings = DefTVSeriesRatings;
        }
      #endregion

      #region Report Settings
      logger.Debug("Latest: " + Check(latestPictures) + " Pictures, " + 
                                Check(latestMusic) + " Music, " +
                                Check(latestMyVideos) + Check(latestMyVideosWatched) + " MyVideo, " + 
                                Check(latestTVSeries) + Check(latestTVSeriesWatched) + " TVSeries, " +
                                Check(latestTVRecordings) + Check(latestTVRecordingsWatched) + " TV Recordings, " +
                                Check(latestMovingPictures) + Check(latestMovingPicturesWatched) + " MovingPictures, " +
                                Check(latestMyFilms) + Check(latestMyFilmsWatched) + " MyFilms, " +
                                Check(latestMvCentral) + " MvCentral");
      logger.Debug("Music Type: " + latestMusicType) ;
      logger.Debug("TVSeries ratings: " + latestTVSeriesRatings) ;
      logger.Debug("DB: " + Check(refreshDbPicture) + " Pictures, " + 
                            Check(refreshDbMusic) + " Music, "+
                            "Interval: " + reorgInterval);
      logger.Debug("Date Format: " + dateFormat) ;
      #endregion

      HasNewInit();
    }

    public static void SaveSettings()
    {
      try
      {
        logger.Debug("Save settings to: "+ConfigFilename);
        #region Save settings
        using (MediaPortal.Profile.Settings xmlwriter = new MediaPortal.Profile.Settings(Config.GetFile(Config.Dir.Config, ConfigFilename)))
        {
          xmlwriter.SetValue("LatestMediaHandler", "latestPictures", latestPictures);
          xmlwriter.SetValue("LatestMediaHandler", "latestMusic", latestMusic);
          xmlwriter.SetValue("LatestMediaHandler", "latestMusicType", latestMusicType);
          xmlwriter.SetValue("LatestMediaHandler", "latestMyVideos", latestMyVideos);
          xmlwriter.SetValue("LatestMediaHandler", "latestMyVideosWatched", latestMyVideosWatched);
          xmlwriter.SetValue("LatestMediaHandler", "latestMovingPictures", latestMovingPictures);
          xmlwriter.SetValue("LatestMediaHandler", "latestMovingPicturesWatched", latestMovingPicturesWatched);
          xmlwriter.SetValue("LatestMediaHandler", "latestTVSeries", latestTVSeries);
          xmlwriter.SetValue("LatestMediaHandler", "latestTVSeriesWatched", latestTVSeriesWatched);
          xmlwriter.SetValue("LatestMediaHandler", "latestTVSeriesRatings", latestTVSeriesRatings);
          xmlwriter.SetValue("LatestMediaHandler", "latestTVRecordings", latestTVRecordings);
          xmlwriter.SetValue("LatestMediaHandler", "latestTVRecordingsWatched", latestTVRecordingsWatched);
          xmlwriter.SetValue("LatestMediaHandler", "latestTVRecordingsUnfinished", latestTVRecordingsUnfinished);
          xmlwriter.SetValue("LatestMediaHandler", "latestMyFilms", latestMyFilms);
          xmlwriter.SetValue("LatestMediaHandler", "latestMyFilmsWatched", latestMyFilmsWatched);
          xmlwriter.SetValue("LatestMediaHandler", "latestMvCentral", latestMvCentral);
          xmlwriter.SetValue("LatestMediaHandler", "refreshDbPicture", refreshDbPicture);
          xmlwriter.SetValue("LatestMediaHandler", "refreshDbMusic", refreshDbMusic);
          xmlwriter.SetValue("LatestMediaHandler", "reorgInterval", reorgInterval);
          xmlwriter.SetValue("LatestMediaHandler", "dateFormat", dateFormat);
        } 
        #endregion
        /*
        try
        {
          xmlwriter.SaveCache();
        }
        catch
        {   }
        */
        logger.Debug("Save settings to: "+ConfigFilename+" complete.");
      }
      catch (Exception ex)
      {
        logger.Error("SaveSettings: "+ex);
      }
    }

  }
}
