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

using LMHNLog.NLog;

using System;
using System.Collections;
using System.IO;
using System.Threading;
using System.Xml;

namespace LatestMediaHandler
{
  /// <summary>
  /// Utility class used by the Latest Media Handler plugin.
  /// </summary>
  internal static class Utils
  {
    #region declarations

    private static Logger logger = LogManager.GetCurrentClassLogger();

    private static bool isStopping /* = false*/; //is the plugin about to stop, then this will be true
    private static Hashtable delayStop = null;

    private static bool usedArgus = false;
    private static DateTime lastRefreshRecording;
    private static Hashtable htLatestsUpdate = null;

    private static Object lockObject = new object();
    private static int activeWindow = (int)GUIWindow.Window.WINDOW_INVALID;

    private const string ConfigFilename = "LatestMediaHandler.xml";
    public const string ConfigSkinFilename = "LatestMediaHandler.SkinSettings.xml";
    public const string DefTVSeriesRatings = "TV-Y;TV-Y7;TV-G;TV-PG;TV-14;TV-MA";

    public static bool FanartHandler { get; set; }
    public static bool LatestPictures { get; set; }
    public static bool LatestMusic { get; set; }
    public static LatestsFacadeType LatestMusicType { get; set; }
    public static bool LatestMyVideos { get; set; }
    public static bool LatestMyVideosWatched { get; set; }
    public static bool LatestMovingPictures { get; set; }
    public static bool LatestMovingPicturesWatched { get; set; }
    public static bool LatestTVSeries { get; set; }
    public static bool LatestTVSeriesWatched { get; set; }
    public static string LatestTVSeriesRatings { get; set; }
    public static int LatestTVSeriesView { get; set; }
    public static int LatestTVSeriesType { get; set; }
    public static bool LatestTVRecordings { get; set; }
    public static bool LatestTVRecordingsWatched { get; set; }
    public static bool LatestTVRecordingsUnfinished { get; set; }
    public static bool LatestMyFilms { get; set; }
    public static bool LatestMyFilmsWatched { get; set; }
    public static bool LatestMvCentral { get; set; }
    public static int LatestMvCentralThumbType { get; set; }
    public static bool RefreshDbPicture { get; set; }
    public static bool RefreshDbMusic { get; set; }
    public static string ReorgInterval { get; set; }
    public static string DateFormat { get; set; }

    public static int ScanDelay { get; set; }

    public static bool PreloadImages { get; set; } 
    public static bool PreloadImagesInThread { get; set; } 
    public static bool SkinUseFacades { get; set; } 

    public static DateTime NewDateTime { get; set; }

    public static int LatestPlotOutlineSentencesNum { get; set; }

    public static int ActiveWindow
    {
      get { return activeWindow; }
      set { activeWindow = value; }
    }

    public static string ActiveWindowStr
    {
      get { return activeWindow.ToString(); }
    }

    public static string[] PipesArray;

    // SyncPoint
    internal static int SyncPointReorg;
    internal static int SyncPointRefresh;
    //
    public const int ThreadSleep = 0;
    //
    public const int FacadeMaxNum = 10;
    public static int LatestsMaxNum = 4;
    public static int LatestsMaxTVNum = 4;
    //
    internal static DateTime LastRefreshRecording
    {
      get { return lastRefreshRecording; }
      set { lastRefreshRecording = value; }
    }

    internal static bool UsedArgus
    {
      get { return usedArgus; }
      set { usedArgus = value; }
    }
    #endregion

    /// <summary>
    /// Return value.
    /// </summary>
    internal static Hashtable DelayStop
    {
      get { return delayStop; }
      set { delayStop = value; }
    }

    internal static int DelayStopCount
    {
      get { return delayStop.Count; }
    }

    internal static bool IsStopping
    {
      get { return isStopping; }
      set { isStopping = value; }
    }
    
    #region Latests update
    public static DateTime GetLatestsUpdate(LatestsCategory category)
    {
      if (htLatestsUpdate == null)
      {
        htLatestsUpdate = new Hashtable();
      }

      lock (htLatestsUpdate)
      {
        if (htLatestsUpdate.ContainsKey(category))
        {
          return (DateTime)htLatestsUpdate[category];
        }
        else
        {
          return new DateTime();
        }
      }
    }

    public static void UpdateLatestsUpdate(LatestsCategory category, DateTime dt)
    {
      if (htLatestsUpdate == null)
      {
        htLatestsUpdate = new Hashtable();
      }

      lock (htLatestsUpdate)
      {
        if (htLatestsUpdate.ContainsKey(category))
        {
          htLatestsUpdate.Remove(category);
        }
        htLatestsUpdate.Add(category, dt);
      }
    }

    public static void RemoveLatestsUpdate(LatestsCategory category)
    {
      if (htLatestsUpdate == null)
      {
        htLatestsUpdate = new Hashtable();
      }

      lock (htLatestsUpdate)
      {
        if (htLatestsUpdate.ContainsKey(category))
        {
          htLatestsUpdate.Remove(category);
        }
      }
    }
    #endregion

    #region Delay stop
    internal static void AllocateDelayStop(string key)
    {
      if (string.IsNullOrEmpty(key))
        return ;

      if (DelayStop == null)
      {
        DelayStop = new Hashtable();
      }
      if (DelayStop.Contains(key))
        DelayStop[key] = (int)DelayStop[key] + 1;
      else
        DelayStop.Add(key, 1);
    }

    internal static bool GetDelayStop()
    {
      if ((DelayStop == null) || (DelayStop.Count <= 0))
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
      if ((DelayStop == null) || (DelayStop.Count <= 0) || string.IsNullOrEmpty(key))
        return;

      if (DelayStop.Contains(key))
      {
        DelayStop[key] = (int)DelayStop[key] - 1;
        if ((int)DelayStop[key] <= 0)
          DelayStop.Remove(key);
      }
    }

    #endregion

    #region Distinct
    internal static string GetDistinct (string Input)
    {
      string result = string.Empty;

      Input = (Input != null ? Input.Trim() : string.Empty);
      if (string.IsNullOrEmpty(Input))
        return result ;

      Hashtable ht = new Hashtable();
      try
      {
        string key = string.Empty;
        string[] sInputs = Input.Split(PipesArray, StringSplitOptions.RemoveEmptyEntries);
        foreach (string sInput in sInputs)
        {
          key = sInput.ToLower().Trim();
          if (!ht.Contains(key))
          {
            result = result + (string.IsNullOrEmpty(result) ? string.Empty : "|") + sInput.Trim();
            ht.Add(key, key);
          }
        }
        if (ht != null)
          ht.Clear();
        ht = null;
      }
      catch (Exception ex)
      {
        logger.Error("GetDistinct: " + ex.ToString());
      }
      return result ;
    }

    internal static string GetFirstDistinctDate (string Input)
    {
      bool Dummy = false;
      return GetFirstDistinctDate (Input, ref Dummy);
    }

    internal static string GetFirstDistinctDate(string Input, ref bool IsNew)
    {
      string result = string.Empty;

      Input  = Input.Trim();
      if (string.IsNullOrEmpty(Input))
        return result ;

      Input = Input.Replace(",", "|");
      try
      {
        string[] sInputs = Input.Split(PipesArray, StringSplitOptions.RemoveEmptyEntries);
        foreach (string sInput in sInputs)
        {
          if (string.IsNullOrEmpty(result))
          {
            try
            {
              DateTime dTmp = DateTime.Parse(Input);
              IsNew = (dTmp > NewDateTime);
              result = String.Format("{0:" + Utils.DateFormat + "}", dTmp);
              return result;
            }
            catch 
            { 
              result = string.Empty;
              IsNew = false;
            }
          }
        }
      }
      catch (Exception ex)
      {
        logger.Error("GetFirstDistinctDate: " + ex.ToString());
      }
      return result ;
    }
    #endregion

    #region Image
    /// <summary>
    /// Load image
    /// </summary>
    internal static void LoadImage(string filename)
    {
      if (isStopping)
      {
        return;
      }

      try
      {
        if (!string.IsNullOrEmpty(filename))
        {
          GUITextureManager.Load(filename, 0, 0, 0, true);
        }
      }
      catch (Exception ex)
      {
        logger.Error("LoadImage (" + filename + "): " + ex.ToString());
      }
    }

    internal static void LoadImage(string name, ref ArrayList Images)
    {
      if (isStopping)
      {
        return;
      }
      if (string.IsNullOrEmpty(name))
      {
        return;
      }
      if (!PreloadImages)
      {
        return;
      }

      try
      {
        // Load images as MP resource
        if (Images != null && !Images.Contains(name))
        {
          try
          {
            Images.Add(name);
          }
          catch { }
          if (PreloadImagesInThread)
          {
            ThreadPool.QueueUserWorkItem(delegate { LoadImage(name); }, null);
          }
          else
          {
            LoadImage(name);
          }
        }
      }
      catch (Exception ex)
      {
        logger.Error("LoadImage: " + ex.ToString());
      }
    }

    /// <summary>
    /// UnLoad image (free memory)
    /// </summary>
    internal static void UnLoadImage(string name)
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

    internal static void UnLoadImage(string name, ref ArrayList Images)
    {
      if (string.IsNullOrEmpty(name))
      {
        return;
      }
      if (Images == null)
      {
        return;
      }
      if (!PreloadImages)
      {
        return;
      }

      try
      {
        // Unload images from MP resource
        for (int i = 0; i < Images.Count; i++)
        {
          string image = Images[i] as string;
          if (!string.IsNullOrEmpty(image) && image.Equals(name))
          {
            if (PreloadImagesInThread)
            {
              ThreadPool.QueueUserWorkItem(delegate { UnLoadImage(name); }, null); 
            }
            else
            {
              UnLoadImage(name);
            }
            Images.RemoveAt(i);
          }
        }
      }
      catch (Exception ex)
      {
        logger.Error("UnLoadImage: " + ex.ToString());
      }
    }

    internal static void UnLoadImages(ref ArrayList Images)
    {
      if (Images == null)
      {
        return;
      }
      if (!PreloadImages)
      {
        return;
      }

      try
      {
        foreach (Object image in Images)
        {
          // Unload old image to free MP resource
          if (image != null)
          {
            if (PreloadImagesInThread)
            {
              ThreadPool.QueueUserWorkItem(delegate { UnLoadImage(image.ToString()); }, null);
            }
            else
            {
              UnLoadImage(image.ToString());
            }
          }
        }
        Images.Clear();
      }
      catch (Exception ex)
      {
        logger.Error("UnLoadImages: " + ex.ToString());
      }
    }
    #endregion

    #region Facade
    /// <summary>
    /// Selects the specified item in the facade
    /// </summary>
    /// <param name="self"></param>
    /// <param name="index">index of the item</param>
    internal static void SelectIndex(this GUIFacadeControl self, LatestsFacade facade)
    {
      if (self == null)
      {
        return;
      }

      if (self.WindowId != ActiveWindow)
      {
        return;
      }
      if (self.Count <= 0)
      {
        return;
      }
      if (self.CurrentLayout == GUIFacadeControl.Layout.Filmstrip)
      {
        if (self.FilmstripLayout == null)
        {
          return;
        }
        if (self.FilmstripLayout.Width <= 0 || self.FilmstripLayout.ItemWidth <= 0)
        {
          return;
        }
      }
      else if (self.CurrentLayout == GUIFacadeControl.Layout.LargeIcons || self.CurrentLayout == GUIFacadeControl.Layout.SmallIcons)
      {
        if (self.ThumbnailLayout == null)
        {
          return;
        }
        if (self.ThumbnailLayout.Width <= 0 || self.ThumbnailLayout.ItemWidth <= 0)
        {
          return;
        }
        if (self.ThumbnailLayout.Height <= 5 || self.ThumbnailLayout.ItemHeight <= 0)
        {
          return;
        }
      }

      int index = facade.FocusedID;
      if (index < 0)
      {
        index = (facade.LeftToRight ? 0 : self.Count - 1);
      }
      if (index >= self.Count)
      {
        index = (facade.LeftToRight ? self.Count - 1 : 0);
      }

      self.SelectedListItemIndex = index;
      // GUIMessage msg = new GUIMessage(GUIMessage.MessageType.GUI_MSG_ITEM_SELECT, self.WindowId, 0, self.GetID, index, 0, null);
      // GUIGraphicsContext.SendMessage(msg);
    }

    /// <summary>
    /// Sets the facade skin defined layouts
    /// </summary>
    /// <param name="self"></param>
    public static void SetVisibleFromSkinCondition(this GUIFacadeControl self)
    {
      if (self == null)
      {
        return;
      }

      self.Visible = self.VisibleFromSkinCondition;
      if (self.CoverFlowLayout != null)
      {
        self.CoverFlowLayout.Visible = self.CoverFlowLayout.VisibleFromSkinCondition;
      }
      if (self.AlbumListLayout != null)
      {
        self.AlbumListLayout.Visible = self.AlbumListLayout.VisibleFromSkinCondition;
      }
      if (self.ThumbnailLayout != null)
      {
        self.ThumbnailLayout.Visible = self.ThumbnailLayout.VisibleFromSkinCondition;
      }
      if (self.ListLayout != null)
      {
        self.ListLayout.Visible = self.ListLayout.VisibleFromSkinCondition;
      }
      if (self.FilmstripLayout != null)
      {
        self.FilmstripLayout.Visible = self.FilmstripLayout.VisibleFromSkinCondition;
      }
    }

    /// <summary>
    /// Sets the facade and any defined layouts visibility to the visibility defined by the skin
    /// </summary>
    /// <param name="self"></param>
    public static void SetCurrentLayout(this GUIFacadeControl self, GUIFacadeControl.Layout layout)
    {
      if (self == null)
      {
        return;
      }

      if (layout == GUIFacadeControl.Layout.CoverFlow && self.CoverFlowLayout != null)
      {
        self.CurrentLayout = layout;
      }
      if (layout == GUIFacadeControl.Layout.AlbumView && self.AlbumListLayout != null)
      {
        self.CurrentLayout = layout;
      }
      if (layout == GUIFacadeControl.Layout.SmallIcons && self.ThumbnailLayout != null)
      {
        self.CurrentLayout = layout;
      }
      if (layout == GUIFacadeControl.Layout.LargeIcons && self.ThumbnailLayout != null)
      {
        self.CurrentLayout = layout;
      }
      if (layout == GUIFacadeControl.Layout.List && self.ListLayout != null)
      {
        self.CurrentLayout = layout;
      }
      if (layout == GUIFacadeControl.Layout.Filmstrip && self.FilmstripLayout != null)
      {
        self.CurrentLayout = layout;
      }
    }

    /// <summary>
    /// Check if Window is Full screen
    /// </summary>
    /// <param name="self"></param>
    public static bool ActiveWindowIsFullScreen(this GUIWindow self)
    {
      if (self == null)
      {
        return true;
      }

      return (self.GetID == 511 ||    // Music Full Screen Visualization
              self.GetID == 2005 ||   // Video Full Screen
              self.GetID == 602);     // My TV Full Screen
    }

    /// <summary>
    /// Check if Window fully loaded
    /// </summary>
    /// <param name="self"></param>
    public static bool WindowIsLoaded(this GUIWindow self)
    {
      if (self == null)
      {
        return true;
      }

      if (self.ActiveWindowIsFullScreen())
      {
        return true;
      }

      int _focused = -1;
      try
      {
        _focused = self.GetFocusControlId();
      }
      catch
      {
        _focused = -1;
      }
      return self.WindowLoaded && _focused >= 0;
    }

    internal static GUIFacadeControl GetLatestsFacade(int ControlID, bool fast = false)
    {
      if (ActiveWindow <= (int)GUIWindow.Window.WINDOW_INVALID)
      {
        return null;
      }
      if (!Utils.SkinUseFacades)
      {
        return null;
      }

      if (fast)
      {
        return GetLatestsFacadeFast(ControlID);
      }
      else
      {
        return GetLatestsFacadeSlow(ControlID);
      }
    }

    internal static GUIFacadeControl GetLatestsFacadeSlow(int ControlID)
    {
      if (ActiveWindow <= (int)GUIWindow.Window.WINDOW_INVALID)
      {
        return null;
      }

      GUIWindow gw = null;
      lock (lockObject)
      {
        int i = 0;
        int wId = ActiveWindow;
        try
        {
          gw = GUIWindowManager.GetWindow(wId); //, false);
          if (gw == null)
          {
            return null;
          }
          do
          {
            if (wId != ActiveWindow)
            {
              return null;
            }

            i++;
            Thread.Sleep(10);
          }
          while (i < 50 && !gw.WindowIsLoaded());
        }
        catch (Exception ex)
        {
          logger.Debug("GetLatestsFacadeSlow: " + wId + "/" + ActiveWindow);
          logger.Error("GetLatestsFacadeSlow: " + ex);
        }
        if (gw == null)
        {
          return null;
        }

        i = 0;
        GUIFacadeControl facade = null;
        try
        {
          bool bReady;
          do
          {
            if (ActiveWindow <= (int)GUIWindow.Window.WINDOW_INVALID)
            {
              return null;
            }
            if (wId != ActiveWindow)
            {
              return null;
            }

            facade = gw.GetControl(ControlID) as GUIFacadeControl;
            if (facade == null)
            {
              i++;
              bReady = false;
              Thread.Sleep(10);
            }
            else
            {
              bReady = true;
            }
          }
          while (i < 50 && !bReady);
        }
        catch (Exception ex)
        {
          logger.Debug("GetLatestsFacadeSlow: " + wId + "/" + ActiveWindow + " - " + ControlID);
          logger.Error("GetLatestsFacadeSlow: " + ex);
        }
        /*
        if (facade == null)
        {
          logger.Debug("GetLatestsFacade: Unable to find facade control [id:{0}].", ControlID);
        }
        else
        {
          logger.Debug("GetLatestsFacade: Found facade control [id:{0}].", ControlID);
        }
        */
        return facade;
      }
    }

    internal static GUIFacadeControl GetLatestsFacadeFast(int ControlID)
    {
      if (ActiveWindow <= (int)GUIWindow.Window.WINDOW_INVALID)
      {
        return null;
      }

      lock (lockObject)
      {
        GUIWindow gw = GUIWindowManager.GetWindow(ActiveWindow);
        if (gw == null)
        {
          return null;
        }

        GUIFacadeControl facade = gw.GetControl(ControlID) as GUIFacadeControl;
        /*
        if (facade == null)
        {
          logger.Debug("GetLatestsFacade: Unable to find facade control [id:{0}].", ControlID);
        }
        else
        {
          logger.Debug("GetLatestsFacade: Found facade control [id:{0}].", ControlID);
        }
        */
        return facade;
      }
    }

    internal static void ClearFacade(ref GUIFacadeControl facade)
    {
      if (facade != null)
      {
        lock (lockObject)
        {
          facade.Clear();
          // GUIControl.ClearControl(ActiveWindow, facade.GetID);
        }
      }
    }

    internal static void UpdateFacade(ref GUIFacadeControl facade, LatestsFacade latestfacade)
    {
      if (facade == null)
      {
        return;
      }

      lock (lockObject)
      {
        facade.SetCurrentLayout(latestfacade.Layout);
        facade.SetVisibleFromSkinCondition();
        facade.SelectIndex(latestfacade);
      }
    }
    #endregion

    #region Movies properties
    internal static void ClearLatestsMovieProperty(LatestsFacade facade, bool main)
    {
      ClearLatestsMovieProperty(facade, facade.Title, main);
    }

    internal static void ClearLatestsMovieProperty(LatestsFacade facade, string label, bool main)
    {
      if (!main && !facade.AddProperties)
      {
        return;
      }

      string handler = facade.Handler.ToLowerInvariant();
      string id = main ? string.Empty : facade.ControlID.ToString();
      if (!string.IsNullOrEmpty(id))
      {
        id = ".info." + id;
      }

      SetProperty("#latestMediaHandler." + handler + id + ".label", label);
      SetProperty("#latestMediaHandler." + handler + id + ".latest.enabled", "false");
      SetProperty("#latestMediaHandler." + handler + id + ".hasnew", "false");
      for (int z = 1; z <= LatestsMaxNum; z++)
      {
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".thumb", string.Empty);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".fanart", string.Empty);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".title", string.Empty);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".dateAdded", string.Empty);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".genre", string.Empty);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".rating", string.Empty);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".roundedRating", string.Empty);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".classification", string.Empty);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".runtime", string.Empty);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".year", string.Empty);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".id", string.Empty);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".plot", string.Empty);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".plotoutline", string.Empty);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".banner", string.Empty);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".clearart", string.Empty);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".clearlogo", string.Empty);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".cd", string.Empty);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".aniposter", string.Empty);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".anibackground", string.Empty);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".new", "false");
      }
    }

    internal static void FillLatestsMovieProperty(LatestsFacade facade, LatestsCollection collection, bool main)
    {
      if (!main && !facade.AddProperties)
      {
        return;
      }
      if (collection == null)
      {
        return;
      }

      string title = facade.Handler;
      string handler = title.ToLowerInvariant();
      string id = main ? string.Empty : facade.ControlID.ToString();
      string sId = id;
      if (!string.IsNullOrEmpty(id))
      {
        sId = " [" + sId + "]";
        id = ".info." + id;
      }

      int z = 1;
      for (int i = 0; i < collection.Count && i < LatestsMaxNum; i++)
      {
        logger.Info("Updating Media Info: " + title + sId + ": [" + z + "] " + collection[i].Title);

        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".thumb", collection[i].Thumb);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".fanart", collection[i].Fanart);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".title", collection[i].Title);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".dateAdded", collection[i].DateAdded);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".genre", collection[i].Genre);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".rating", collection[i].Rating);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".roundedRating", collection[i].RoundedRating);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".classification", collection[i].Classification);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".runtime", collection[i].Runtime);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".year", collection[i].Year);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".id", collection[i].Id);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".plot", collection[i].Plot);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".plotoutline", collection[i].MoviePlotOutline);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".banner", collection[i].Banner);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".clearart", collection[i].ClearArt);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".clearlogo", collection[i].ClearLogo);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".cd", collection[i].CD);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".aniposter", collection[i].AnimatedPoster);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".anibackground", collection[i].AnimatedBackground);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".new", collection[i].New);
        z++;
      }

      SetProperty("#latestMediaHandler." + handler + id + ".hasnew", facade.HasNew ? "true" : "false");
      logger.Debug("Updating Media Info: " + title + sId + ": Has new: " + (facade.HasNew ? "true" : "false"));
    }

    internal static void ClearSelectedMovieProperty(LatestsFacade facade)
    {
      string handler = facade.Handler.ToLowerInvariant();

      SetProperty("#latestMediaHandler." + handler + ".selected.fanart1", string.Empty);
      SetProperty("#latestMediaHandler." + handler + ".selected.fanart2", string.Empty);
      SetProperty("#latestMediaHandler." + handler + ".selected.showfanart1", string.Empty);
      SetProperty("#latestMediaHandler." + handler + ".selected.showfanart2", string.Empty);

      SetProperty("#latestMediaHandler." + handler + ".selected.thumb", string.Empty);
      SetProperty("#latestMediaHandler." + handler + ".selected.title", string.Empty);
      SetProperty("#latestMediaHandler." + handler + ".selected.dateAdded", string.Empty);
      SetProperty("#latestMediaHandler." + handler + ".selected.genre", string.Empty);
      SetProperty("#latestMediaHandler." + handler + ".selected.roundedRating", string.Empty);
      SetProperty("#latestMediaHandler." + handler + ".selected.runtime", string.Empty);
      SetProperty("#latestMediaHandler." + handler + ".selected.year", string.Empty);
      SetProperty("#latestMediaHandler." + handler + ".selected.id", string.Empty);

      SetProperty("#latestMediaHandler." + handler + ".selected.rating", string.Empty);
      SetProperty("#latestMediaHandler." + handler + ".selected.classification", string.Empty);
      SetProperty("#latestMediaHandler." + handler + ".selected.plot", string.Empty);
      SetProperty("#latestMediaHandler." + handler + ".selected.plotoutline", string.Empty);
      SetProperty("#latestMediaHandler." + handler + ".selected.banner", string.Empty);
      SetProperty("#latestMediaHandler." + handler + ".selected.clearart", string.Empty);
      SetProperty("#latestMediaHandler." + handler + ".selected.clearlogo", string.Empty);
      SetProperty("#latestMediaHandler." + handler + ".selected.cd", string.Empty);
      SetProperty("#latestMediaHandler." + handler + ".selected.aniposter", string.Empty);
      SetProperty("#latestMediaHandler." + handler + ".selected.anibackground", string.Empty);
      SetProperty("#latestMediaHandler." + handler + ".selected.new", string.Empty);
    }

    internal static void FillSelectedMovieProperty(LatestsFacade facade, GUIListItem item, Latest latest)
    {
      if (item == null || latest == null)
      {
        return;
      }
      string handler = facade.Handler.ToLowerInvariant();

      SetProperty("#latestMediaHandler." + handler + ".selected.thumb", item.IconImageBig);
      SetProperty("#latestMediaHandler." + handler + ".selected.title", item.Label);
      SetProperty("#latestMediaHandler." + handler + ".selected.dateAdded", item.Label3);
      SetProperty("#latestMediaHandler." + handler + ".selected.genre", item.Label2);
      SetProperty("#latestMediaHandler." + handler + ".selected.roundedRating", string.Empty + item.Rating);
      SetProperty("#latestMediaHandler." + handler + ".selected.runtime", string.Empty + item.Duration);
      SetProperty("#latestMediaHandler." + handler + ".selected.year", string.Empty + item.Year);
      SetProperty("#latestMediaHandler." + handler + ".selected.id", string.Empty + item.ItemId);

      SetProperty("#latestMediaHandler." + handler + ".selected.rating", latest.Rating);
      SetProperty("#latestMediaHandler." + handler + ".selected.classification", latest.Classification);
      SetProperty("#latestMediaHandler." + handler + ".selected.plot", latest.Plot);
      SetProperty("#latestMediaHandler." + handler + ".selected.plotoutline", latest.MoviePlotOutline);
      SetProperty("#latestMediaHandler." + handler + ".selected.banner", latest.Banner);
      SetProperty("#latestMediaHandler." + handler + ".selected.clearart", latest.ClearArt);
      SetProperty("#latestMediaHandler." + handler + ".selected.clearlogo", latest.ClearLogo);
      SetProperty("#latestMediaHandler." + handler + ".selected.cd", latest.CD);
      SetProperty("#latestMediaHandler." + handler + ".selected.aniposter", latest.AnimatedPoster);
      SetProperty("#latestMediaHandler." + handler + ".selected.anibackground", latest.AnimatedBackground);
      SetProperty("#latestMediaHandler." + handler + ".selected.new", latest.New);
    }
    #endregion

    #region TVSeries properties
    internal static void ClearLatestsTVSeriesProperty(LatestsFacade facade, bool main)
    {
      ClearLatestsTVSeriesProperty(facade, facade.Title, main);
    }

    internal static void ClearLatestsTVSeriesProperty(LatestsFacade facade, string label, bool main)
    {
      if (!main && !facade.AddProperties)
      {
        return;
      }

      string handler = facade.Handler.ToLowerInvariant();
      string id = main ? string.Empty : facade.ControlID.ToString();
      if (!string.IsNullOrEmpty(id))
      {
        id = ".info." + id;
      }

      SetProperty("#latestMediaHandler." + handler + id + ".label", label);
      SetProperty("#latestMediaHandler." + handler + id + ".latest.enabled", "false");
      SetProperty("#latestMediaHandler." + handler + id + ".latest.mode", string.Empty);
      SetProperty("#latestMediaHandler." + handler + id + ".latest.type", string.Empty);
      SetProperty("#latestMediaHandler." + handler + id + ".hasnew", "false");

      for (int z = 1; z <= LatestsMaxNum; z++)
      {
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".thumb", string.Empty);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".serieThumb", string.Empty);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".fanart", string.Empty);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".serieName", string.Empty);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".seasonIndex", string.Empty);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".episodeName", string.Empty);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".episodeIndex", string.Empty);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".dateAdded", string.Empty);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".genre", string.Empty);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".rating", string.Empty);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".roundedRating", string.Empty);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".classification", string.Empty);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".runtime", string.Empty);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".firstAired", string.Empty);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".plot", string.Empty);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".plotoutline", string.Empty);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".banner", string.Empty);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".clearart", string.Empty);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".clearlogo", string.Empty);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".cd", string.Empty);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".new", "false");
      }

      SetProperty("#latestMediaHandler." + handler + id + ".latest.mode", facade.SubType.ToString().ToLowerInvariant());
      SetProperty("#latestMediaHandler." + handler + id + ".latest.type", facade.SubTitle);
      if (facade.ThumbType != LatestsFacadeThumbType.None)
      {
        SetProperty("#latestMediaHandler." + handler + id + ".latest.thumbtype", facade.ThumbType.ToString().ToLowerInvariant());
      }
      else
      {
        SetProperty("#latestMediaHandler." + handler + id + ".latest.thumbtype", string.Empty);
      }
    }

    internal static void FillLatestsTVSeriesProperty(LatestsFacade facade, LatestsCollection collection, bool main)
    {
      if (!main && !facade.AddProperties)
      {
        return;
      }
      if (collection == null)
      {
        return;
      }

      string title = facade.Handler;
      string handler = title.ToLowerInvariant();
      string id = main ? string.Empty : facade.ControlID.ToString();
      string sId = id;
      if (!string.IsNullOrEmpty(id))
      {
        sId = " [" + sId + "]";
        id = ".info." + id;
      }

      int z = 1;
      for (int i = 0; i < collection.Count && i < LatestsMaxNum; i++)
      {
        logger.Info("Updating Media Info: " + title + " " + facade.SubType + sId + ": [" + z + "] " + 
                                              collection[i].Title + " - " + collection[i].Subtitle);

        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".thumb", collection[i].Thumb);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".serieThumb", collection[i].ThumbSeries);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".fanart", collection[i].Fanart);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".serieName", collection[i].Title);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".seasonIndex", collection[i].SeasonIndex);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".episodeName", collection[i].Subtitle);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".episodeIndex", collection[i].EpisodeIndex);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".dateAdded", collection[i].DateAdded);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".genre", collection[i].Genre);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".rating", collection[i].Rating);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".roundedRating", collection[i].RoundedRating);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".classification", collection[i].Classification);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".runtime", collection[i].Runtime);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".firstAired", collection[i].Year);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".plot", collection[i].Plot);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".plotoutline", collection[i].PlotOutline);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".banner", collection[i].Banner);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".clearart", collection[i].ClearArt);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".clearlogo", collection[i].ClearLogo);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".cd", collection[i].CD);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".new", collection[i].New);
        z++;
      }
      SetProperty("#latestMediaHandler." + handler + id + ".hasnew", facade.HasNew ? "true" : "false");
      logger.Debug("Updating Media Info: " + title + sId + ": Has new: " + (facade.HasNew ? "true" : "false"));
      if (facade.ThumbType != LatestsFacadeThumbType.None)
      {
        logger.Debug("Thumb for " + title + sId + ": " + facade.ThumbType);
      }
    }

    internal static void ClearSelectedTVSeriesProperty(LatestsFacade facade)
    {
      string handler = facade.Handler.ToLowerInvariant();

      SetProperty("#latestMediaHandler." + handler + ".selected.fanart1", string.Empty);
      SetProperty("#latestMediaHandler." + handler + ".selected.fanart2", string.Empty);
      SetProperty("#latestMediaHandler." + handler + ".selected.showfanart1", string.Empty);
      SetProperty("#latestMediaHandler." + handler + ".selected.showfanart2", string.Empty);

      SetProperty("#latestMediaHandler." + handler + ".selected.thumb", string.Empty);
      SetProperty("#latestMediaHandler." + handler + ".selected.serieThumb", string.Empty);
      SetProperty("#latestMediaHandler." + handler + ".selected.serieName", string.Empty);
      SetProperty("#latestMediaHandler." + handler + ".selected.seasonIndex", string.Empty);
      SetProperty("#latestMediaHandler." + handler + ".selected.episodeName", string.Empty);
      SetProperty("#latestMediaHandler." + handler + ".selected.episodeIndex", string.Empty);
      SetProperty("#latestMediaHandler." + handler + ".selected.dateAdded", string.Empty);
      SetProperty("#latestMediaHandler." + handler + ".selected.genre", string.Empty);
      SetProperty("#latestMediaHandler." + handler + ".selected.rating", string.Empty);
      SetProperty("#latestMediaHandler." + handler + ".selected.roundedRating", string.Empty);
      SetProperty("#latestMediaHandler." + handler + ".selected.classification", string.Empty);
      SetProperty("#latestMediaHandler." + handler + ".selected.runtime", string.Empty);
      SetProperty("#latestMediaHandler." + handler + ".selected.firstAired", string.Empty);
      SetProperty("#latestMediaHandler." + handler + ".selected.plot", string.Empty);
      SetProperty("#latestMediaHandler." + handler + ".selected.plotoutline", string.Empty);
      SetProperty("#latestMediaHandler." + handler + ".selected.banner", string.Empty);
      SetProperty("#latestMediaHandler." + handler + ".selected.clearart", string.Empty);
      SetProperty("#latestMediaHandler." + handler + ".selected.clearlogo", string.Empty);
      SetProperty("#latestMediaHandler." + handler + ".selected.cd", string.Empty);
      SetProperty("#latestMediaHandler." + handler + ".selected.new", string.Empty);
    }

    internal static void FillSelectedTVSeriesProperty(LatestsFacade facade, Latest latest)
    {
      if (latest == null)
      {
        return;
      }
      string handler = facade.Handler.ToLowerInvariant();

      SetProperty("#latestMediaHandler." + handler + ".selected.thumb", latest.Thumb);
      SetProperty("#latestMediaHandler." + handler + ".selected.serieThumb", latest.ThumbSeries);
      SetProperty("#latestMediaHandler." + handler + ".selected.serieName", latest.Title);
      SetProperty("#latestMediaHandler." + handler + ".selected.seasonIndex", latest.SeasonIndex);
      SetProperty("#latestMediaHandler." + handler + ".selected.episodeName", latest.Subtitle);
      SetProperty("#latestMediaHandler." + handler + ".selected.episodeIndex", latest.EpisodeIndex);
      SetProperty("#latestMediaHandler." + handler + ".selected.dateAdded", latest.DateAdded);
      SetProperty("#latestMediaHandler." + handler + ".selected.genre", latest.Genre);
      SetProperty("#latestMediaHandler." + handler + ".selected.rating", latest.Rating);
      SetProperty("#latestMediaHandler." + handler + ".selected.roundedRating", latest.RoundedRating);
      SetProperty("#latestMediaHandler." + handler + ".selected.classification", latest.Classification);
      SetProperty("#latestMediaHandler." + handler + ".selected.runtime", latest.Runtime);
      SetProperty("#latestMediaHandler." + handler + ".selected.firstAired", latest.Year);
      SetProperty("#latestMediaHandler." + handler + ".selected.plot", latest.Plot);
      SetProperty("#latestMediaHandler." + handler + ".selected.plotoutline", latest.PlotOutline);
      SetProperty("#latestMediaHandler." + handler + ".selected.banner", latest.Banner);
      SetProperty("#latestMediaHandler." + handler + ".selected.clearart", latest.ClearArt);
      SetProperty("#latestMediaHandler." + handler + ".selected.clearlogo", latest.ClearLogo);
      SetProperty("#latestMediaHandler." + handler + ".selected.cd", latest.CD);
      SetProperty("#latestMediaHandler." + handler + ".selected.new", latest.New);
    }
    #endregion

    #region Music properties
    internal static void ClearLatestsMusicProperty(LatestsFacade facade, bool main)
    {
      ClearLatestsMusicProperty(facade, facade.Title, main);
    }

    internal static void ClearLatestsMusicProperty(LatestsFacade facade, string label, bool main)
    {
      if (!main && !facade.AddProperties)
      {
        return;
      }

      string handler = facade.Handler.ToLowerInvariant();
      string id = main ? string.Empty : facade.ControlID.ToString();
      if (!string.IsNullOrEmpty(id))
      {
        id = ".info." + id;
      }

      SetProperty("#latestMediaHandler." + handler + id + ".label", label);
      SetProperty("#latestMediaHandler." + handler + id + ".latest.enabled", "false");
      SetProperty("#latestMediaHandler." + handler + id + ".hasnew", "false");
      for (int z = 1; z <= LatestsMaxNum; z++)
      {
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".thumb", string.Empty);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".artist", string.Empty);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".artistbio", string.Empty);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".artistbiooutline", string.Empty);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".album", string.Empty);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".track", string.Empty);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".year", string.Empty);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".dateAdded", string.Empty);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".fanart", string.Empty);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".genre", string.Empty);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".banner", string.Empty);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".clearart", string.Empty);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".clearlogo", string.Empty);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".cd", string.Empty);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".new", "false");
      }

      SetProperty("#latestMediaHandler." + handler + id + ".latest.mode", facade.Type.ToString().ToLowerInvariant());
      if (facade.ThumbType != LatestsFacadeThumbType.None)
      {
        SetProperty("#latestMediaHandler." + handler + id + ".latest.thumbtype", facade.ThumbType.ToString().ToLowerInvariant());
      }
      else
      {
        SetProperty("#latestMediaHandler." + handler + id + ".latest.thumbtype", string.Empty);
      }
    }

    internal static void FillLatestsMusicProperty(LatestsFacade facade, LatestsCollection collection, bool main)
    {
      if (!main && !facade.AddProperties)
      {
        return;
      }
      if (collection == null)
      {
        return;
      }

      string title = facade.Handler;
      string handler = title.ToLowerInvariant();
      string id = main ? string.Empty : facade.ControlID.ToString();
      string sId = id;
      if (!string.IsNullOrEmpty(id))
      {
        sId = " [" + sId + "]";
        id = ".info." + id;
      }

      int z = 1;
      for (int i = 0; i < collection.Count && i < LatestsMaxNum; i++)
      {
        logger.Info("Updating Media Info: " + title + " " + facade.Type + sId + ": [" + z + "] " + 
                                              collection[i].Artist + " - " + collection[i].Album + 
                                              " [" + collection[i].DateAdded + (!string.IsNullOrEmpty(collection[i].Id) ? "/" + collection[i].Id : string.Empty) + "] - " + 
                                              collection[i].Fanart);

        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".thumb", collection[i].Thumb);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".artist", collection[i].Artist);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".artistbio", collection[i].Plot);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".artistbiooutline", collection[i].PlotOutline);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".album", collection[i].Album);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".track", collection[i].Title);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".year", collection[i].Year);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".dateAdded", collection[i].DateAdded);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".fanart", collection[i].Fanart);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".genre", collection[i].Genre);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".banner", collection[i].Banner);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".clearart", collection[i].ClearArt);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".clearlogo", collection[i].ClearLogo);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".cd", collection[i].CD);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".new", collection[i].New);
        z++;
      }
      SetProperty("#latestMediaHandler." + handler + id + ".hasnew", facade.HasNew ? "true" : "false");
      logger.Debug("Updating Media Info: " + title + sId + ": Has new: " + (facade.HasNew ? "true" : "false"));
      if (facade.ThumbType != LatestsFacadeThumbType.None)
      {
        logger.Debug("Thumb for " + title + sId + ": " + facade.ThumbType);
      }
    }

    internal static void ClearSelectedMusicProperty(LatestsFacade facade)
    {
      string handler = facade.Handler.ToLowerInvariant();

      SetProperty("#latestMediaHandler." + handler + ".selected.fanart1", string.Empty);
      SetProperty("#latestMediaHandler." + handler + ".selected.fanart2", string.Empty);
      SetProperty("#latestMediaHandler." + handler + ".selected.showfanart1", string.Empty);
      SetProperty("#latestMediaHandler." + handler + ".selected.showfanart2", string.Empty);

      SetProperty("#latestMediaHandler." + handler + ".selected.thumb", string.Empty);
      SetProperty("#latestMediaHandler." + handler + ".selected.artist", string.Empty);
      SetProperty("#latestMediaHandler." + handler + ".selected.album", string.Empty);
      SetProperty("#latestMediaHandler." + handler + ".selected.dateAdded", string.Empty);
      SetProperty("#latestMediaHandler." + handler + ".selected.genre", string.Empty);

      SetProperty("#latestMediaHandler." + handler + ".selected.artistbio", string.Empty);
      SetProperty("#latestMediaHandler." + handler + ".selected.artistbiooutline", string.Empty);
      SetProperty("#latestMediaHandler." + handler + ".selected.year", string.Empty);
      SetProperty("#latestMediaHandler." + handler + ".selected.banner", string.Empty);
      SetProperty("#latestMediaHandler." + handler + ".selected.clearart", string.Empty);
      SetProperty("#latestMediaHandler." + handler + ".selected.clearlogo", string.Empty);
      SetProperty("#latestMediaHandler." + handler + ".selected.cd", string.Empty);
      SetProperty("#latestMediaHandler." + handler + ".selected.new", string.Empty);
    }

    internal static void FillSelectedMusicProperty(LatestsFacade facade, GUIListItem item, Latest latest)
    {
      if (item == null || latest == null)
      {
        return;
      }
      string handler = facade.Handler.ToLowerInvariant();

      SetProperty("#latestMediaHandler." + handler + ".selected.thumb", item.IconImageBig);
      SetProperty("#latestMediaHandler." + handler + ".selected.artist", item.Label);
      SetProperty("#latestMediaHandler." + handler + ".selected.album", item.Label2);
      SetProperty("#latestMediaHandler." + handler + ".selected.dateAdded", item.Label3);
      SetProperty("#latestMediaHandler." + handler + ".selected.genre", item.Path);

      SetProperty("#latestMediaHandler." + handler + ".selected.track", latest.Title);
      SetProperty("#latestMediaHandler." + handler + ".selected.artistbio", latest.Plot);
      SetProperty("#latestMediaHandler." + handler + ".selected.artistbiooutline", latest.PlotOutline);
      SetProperty("#latestMediaHandler." + handler + ".selected.year", latest.Year);
      SetProperty("#latestMediaHandler." + handler + ".selected.banner", latest.Banner);
      SetProperty("#latestMediaHandler." + handler + ".selected.clearart", latest.ClearArt);
      SetProperty("#latestMediaHandler." + handler + ".selected.clearlogo", latest.ClearLogo);
      SetProperty("#latestMediaHandler." + handler + ".selected.cd", latest.CD);
      SetProperty("#latestMediaHandler." + handler + ".selected.new", latest.New);
    }
    #endregion

    #region Pictures properties
    internal static void ClearLatestsPicturesProperty(LatestsFacade facade, bool main)
    {
      ClearLatestsPicturesProperty(facade, facade.Title, main);
    }

    internal static void ClearLatestsPicturesProperty(LatestsFacade facade, string label, bool main)
    {
      if (!main && !facade.AddProperties)
      {
        return;
      }

      string handler = facade.Handler.ToLowerInvariant();
      string id = main ? string.Empty : facade.ControlID.ToString();
      if (!string.IsNullOrEmpty(id))
      {
        id = ".info." + id;
      }

      SetProperty("#latestMediaHandler." + handler + id + ".label", label);
      SetProperty("#latestMediaHandler." + handler + id + ".latest.enabled", "false");
      SetProperty("#latestMediaHandler." + handler + id + ".hasnew", "false");
      for (int z = 1; z <= LatestsMaxNum; z++)
      {
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".title", string.Empty);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".thumb", string.Empty);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".filename", string.Empty);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".fanart", string.Empty);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".dateAdded", string.Empty);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".new", "false");
      }
    }

    internal static void FillLatestsPicturesProperty(LatestsFacade facade, LatestsCollection collection, bool main)
    {
      if (!main && !facade.AddProperties)
      {
        return;
      }
      if (collection == null)
      {
        return;
      }

      string title = facade.Handler;
      string handler = title.ToLowerInvariant();
      string id = main ? string.Empty : facade.ControlID.ToString();
      string sId = id;
      if (!string.IsNullOrEmpty(id))
      {
        sId = " [" + sId + "]";
        id = ".info." + id;
      }

      int z = 1;
      for (int i = 0; i < collection.Count && i < LatestsMaxNum; i++)
      {
        logger.Info("Updating Media Info: " + title + sId + ": [" + z + "] " + collection[i].Fanart);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".title", collection[i].Title);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".thumb", collection[i].Thumb);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".filename", collection[i].Fanart);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".fanart", collection[i].Fanart);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".dateAdded", collection[i].DateAdded);
        SetProperty("#latestMediaHandler." + handler + id + ".latest" + z + ".new", collection[i].New);
        z++;
      }
      SetProperty("#latestMediaHandler." + handler + id + ".hasnew", facade.HasNew ? "true" : "false");
      logger.Debug("Updating Media Info: " + title + sId + ": Has new: " + (facade.HasNew ? "true" : "false"));
    }

    internal static void ClearSelectedPicturesProperty(LatestsFacade facade)
    {
      string handler = facade.Handler.ToLowerInvariant();

      SetProperty("#latestMediaHandler." + handler + ".selected.fanart1", string.Empty);
      SetProperty("#latestMediaHandler." + handler + ".selected.fanart2", string.Empty);
      SetProperty("#latestMediaHandler." + handler + ".selected.showfanart1", string.Empty);
      SetProperty("#latestMediaHandler." + handler + ".selected.showfanart2", string.Empty);

      SetProperty("#latestMediaHandler." + handler + ".selected.thumb", string.Empty);
      SetProperty("#latestMediaHandler." + handler + ".selected.title", string.Empty);
      SetProperty("#latestMediaHandler." + handler + ".selected.dateAdded", string.Empty);
      SetProperty("#latestMediaHandler." + handler + ".selected.filename", string.Empty);

      SetProperty("#latestMediaHandler." + handler + ".selected.new", string.Empty);
    }

    internal static void FillSelectedPicturesProperty(LatestsFacade facade, GUIListItem item, Latest latest)
    {
      if (item == null || latest == null)
      {
        return;
      }
      string handler = facade.Handler.ToLowerInvariant();

      SetProperty("#latestMediaHandler." + handler + ".selected.thumb", item.IconImageBig);
      SetProperty("#latestMediaHandler." + handler + ".selected.title", item.Label);
      SetProperty("#latestMediaHandler." + handler + ".selected.dateAdded", item.Label2);
      SetProperty("#latestMediaHandler." + handler + ".selected.filename", item.Label);

      SetProperty("#latestMediaHandler." + handler + ".selected.new", latest.New);
    }
    #endregion

    #region Properties
    internal static void SetProperty(string property, string value)
    {
      if (string.IsNullOrEmpty(property))
      {
        return;
      }
      if (string.IsNullOrEmpty(value))
      {
        value = string.Empty;
      }

      try
      {
        GUIPropertyManager.SetProperty(property, value);
        //logger.Debug("SetProperty: "+property+" -> "+value) ;
      }
      catch (Exception ex)
      {
        logger.Error("SetProperty: " + ex.ToString());
      }
    }

    internal static string GetProperty(string property)
    {
      string result = string.Empty;
      if (string.IsNullOrEmpty(property))
      {
        return result;
      }

      try
      {
        result = GUIPropertyManager.GetProperty(property);
        if (string.IsNullOrEmpty(result))
        {
          result = string.Empty;
        }

        result = result.Trim();
        if (result.Equals(property, StringComparison.CurrentCultureIgnoreCase))
        {
          result = string.Empty;
        }
        //logger.Debug("GetProperty: "+property+" -> "+value) ;
      }
      catch (Exception ex)
      {
        result = string.Empty;
        logger.Error("GetProperty: " + ex);
      }
      return result;
    }
    #endregion

    public static bool PluginIsEnabled(string name)
    {
      int condition = GUIInfoManager.TranslateString("plugin.isenabled(" + name + ")");
      return GUIInfoManager.GetBool(condition, 0);
    }

    internal static string RemoveLeadingZeros(string s)
    {
      if (s != null)
      {
        char[] charsToTrim = { '0' };
        s = s.TrimStart(charsToTrim);
      }
      return s;
    }

    public static string GetThemeFolder(string path)
    {
      if (string.IsNullOrWhiteSpace(GUIGraphicsContext.ThemeName))
        return string.Empty;

      var tThemeDir = path + @"Themes\" + GUIGraphicsContext.ThemeName.Trim() + @"\";
      if (Directory.Exists(tThemeDir))
      {
        return tThemeDir;
      }
      tThemeDir = path + GUIGraphicsContext.ThemeName.Trim() + @"\";
      if (Directory.Exists(tThemeDir))
      {
        return tThemeDir;
      }
      return string.Empty;
    }

    internal static void ThreadToSleep()
    {
      Thread.Sleep(ThreadSleep);
    }

    /// <summary>
    /// Returns plugin version.
    /// </summary>
    internal static string GetAllVersionNumber()
    {
      return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
    }

    public static string GetGetDirectoryName(string filename)
    {
      var result = string.Empty;
      try
      {
        if (!string.IsNullOrWhiteSpace(filename))
        {
          result = Path.GetDirectoryName(filename);
        }
      }
      catch
      {
        result = string.Empty;
      }
      return result;
    }

    /// <summary>
    /// Get filename string.
    /// </summary>
    internal static string GetFilenameNoPath(string key)
    {
      if (string.IsNullOrEmpty(key))
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

    internal static string GetSentences(string Text, int num)
    {
      if (string.IsNullOrEmpty(Text) || num <=0)
        return Text;

      string result = string.Empty;
      try
      {
        int i = 1;
        string text = Text.Trim() + " ";
        string[] Sentences = text.Split(new string[] { ". " }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string sentence in Sentences)
        {
          result = result + sentence.Trim() + ". ";
          if (i >= num)
          {
            break;
          }
          i++;
        }
        result = result.Trim();
      }
      catch (Exception ex)
      {
        result = string.Empty;
        logger.Error("GetSentences: " + ex.ToString());
      }
      // logger.Debug("GetSentences: " + Text);
      // logger.Debug("GetSentences: " + result);
      return result;
    }

    internal static void SortLatests(ref LatestsCollection latests, LatestsFacadeType type, bool lefttoright)
    {
      switch (type)
      {
        case LatestsFacadeType.Latests:
          if (lefttoright)
          {
            latests.Sort(new LatestAddedComparerDesc());
          }
          else
          {
            latests.Sort(new LatestAddedComparerAsc());
          }
          break;
        case LatestsFacadeType.Rated:
          if (lefttoright)
          {
            latests.Sort(new LatestRatingComparerDesc());
          }
          else
          {
            latests.Sort(new LatestRatingComparerAsc());
          }
          break;
        case LatestsFacadeType.Watched:
          if (lefttoright)
          {
            latests.Sort(new LatestWathcedComparerDesc());
          }
          else
          {
            latests.Sort(new LatestWatchedComparerAsc());
          }
          break;
      }
    }

    internal static bool IsIdle()
    {
      return true;
      /*
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
      */
    }
 
    public static bool GetBool(string value)
    {
      if (string.IsNullOrWhiteSpace(value))
        return false;

      return (value.Equals("true", StringComparison.CurrentCultureIgnoreCase) || value.Equals("yes", StringComparison.CurrentCultureIgnoreCase));
    }

    public static string Check(bool Value, bool Box = true)
    {
      return (Box ? "[" : string.Empty) + (Value ? "x" : " ") + (Box ? "]" : string.Empty) ;
    }

    public static string Check(string Value, bool Box = true)
    {
      return Check(Value.Equals("true", StringComparison.CurrentCultureIgnoreCase) || Value.Equals("yes", StringComparison.CurrentCultureIgnoreCase), Box) ;
    }

    public static void SyncPointInit()
    {
      SyncPointReorg = 0;
      SyncPointRefresh = 0;
    }

    public static void HasNewInit()
    {
      NewDateTime = DateTime.Now;
      logger.Debug("New Latests after: " + NewDateTime) ;
      PipesArray = new string[1] { "|" };
    }

    public static string MusicTypeToConfig(LatestsFacadeType value)
    {
      switch (value)
      {
        case LatestsFacadeType.Latests:
          return Translation.PrefsLatestAddedMusic;
        case LatestsFacadeType.MostPlayed:
          return Translation.PrefsMostPlayedMusic;
        case LatestsFacadeType.Played:
          return Translation.PrefsLatestPlayedMusic;
      }
      return string.Empty;
    }

    public static LatestsFacadeType StringToMusicType(string value)
    {
      if (string.IsNullOrEmpty(value))
      {
        return LatestsFacadeType.Latests;
      }

      if (value == Translation.LatestAddedMusic || value == Translation.PrefsLatestAddedMusic)
      {
        value = "Latests";
      }
      else if (value == Translation.MostPlayedMusic || value == Translation.PrefsMostPlayedMusic)
      {
        value = "MostPlayed";
      }
      else if (value == Translation.LatestPlayedMusic || value == Translation.PrefsLatestPlayedMusic)
      {
        value = "Played";
      }

      LatestsFacadeType _type;
      if (Enum.TryParse(value, out _type))
      {
        return _type;
      }
      return LatestsFacadeType.Latests;
    }

    #region Settings 
    public static void LoadSettings(bool Conf = false)
    {
      FanartHandler = false;
      LatestPictures = true;
      LatestMusic = true;
      LatestMusicType = LatestsFacadeType.Latests;
      LatestTVSeriesView = 0;
      LatestTVSeriesType = 1;
      LatestPlotOutlineSentencesNum = 2;
      LatestMyVideos = true;
      LatestMyVideosWatched = true;
      LatestMovingPictures = false;
      LatestMovingPicturesWatched = true;
      LatestTVSeries = true;
      LatestTVSeriesWatched = true;
      LatestTVSeriesRatings = "1;1;1;1;1;1";
      LatestTVRecordings = false;
      LatestTVRecordingsWatched = true;
      LatestTVRecordingsUnfinished = true;
      LatestMyFilms = false;
      LatestMyFilmsWatched = true;
      LatestMvCentral = false;
      LatestMvCentralThumbType = 1;

      RefreshDbPicture = false;
      RefreshDbMusic = false;
      ReorgInterval = "1440";

      DateFormat = "yyyy-MM-dd";

      ScanDelay = 0;
      PreloadImages = true;
      PreloadImagesInThread = true;
      SkinUseFacades = true;

      try
      {
        logger.Debug("Load settings from: "+ConfigFilename);
        #region Load settings
        using (MediaPortal.Profile.Settings xmlreader = new MediaPortal.Profile.Settings(Config.GetFile(Config.Dir.Config, ConfigFilename)))
        {
          LatestPictures = GetBool(xmlreader.GetValueAsString("LatestMediaHandler", "latestPictures", LatestPictures.ToString()));
          LatestMusic = GetBool(xmlreader.GetValueAsString("LatestMediaHandler", "latestMusic", LatestMusic.ToString()));
          LatestMusicType = StringToMusicType(xmlreader.GetValueAsString("LatestMediaHandler", "latestMusicType", LatestMusicType.ToString()));
          LatestMyVideos = GetBool(xmlreader.GetValueAsString("LatestMediaHandler", "latestMyVideos", LatestMyVideos.ToString()));
          LatestMyVideosWatched = GetBool(xmlreader.GetValueAsString("LatestMediaHandler", "latestMyVideosWatched", LatestMyVideosWatched.ToString()));
          LatestMovingPictures = GetBool(xmlreader.GetValueAsString("LatestMediaHandler", "latestMovingPictures", LatestMovingPictures.ToString()));
          LatestMovingPicturesWatched = GetBool(xmlreader.GetValueAsString("LatestMediaHandler", "latestMovingPicturesWatched", LatestMovingPicturesWatched.ToString()));
          LatestTVSeries = GetBool(xmlreader.GetValueAsString("LatestMediaHandler", "latestTVSeries", LatestTVSeries.ToString()));
          LatestTVSeriesWatched = GetBool(xmlreader.GetValueAsString("LatestMediaHandler", "latestTVSeriesWatched", LatestTVSeriesWatched.ToString()));
          LatestTVSeriesRatings = xmlreader.GetValueAsString("LatestMediaHandler", "latestTVSeriesRatings", LatestTVSeriesRatings);
          LatestTVSeriesView = xmlreader.GetValueAsInt("LatestMediaHandler", "latestTVSeriesView", LatestTVSeriesView);
          LatestTVSeriesType = xmlreader.GetValueAsInt("LatestMediaHandler", "latestTVSeriesType", LatestTVSeriesType);
          LatestPlotOutlineSentencesNum = xmlreader.GetValueAsInt("LatestMediaHandler", "latestPlotOutlineSentencesNum", LatestPlotOutlineSentencesNum);
          LatestTVRecordings = GetBool(xmlreader.GetValueAsString("LatestMediaHandler", "latestTVRecordings", LatestTVRecordings.ToString()));
          LatestTVRecordingsWatched = GetBool(xmlreader.GetValueAsString("LatestMediaHandler", "latestTVRecordingsWatched", LatestTVRecordingsWatched.ToString()));
          LatestTVRecordingsUnfinished = GetBool(xmlreader.GetValueAsString("LatestMediaHandler", "latestTVRecordingsUnfinished", LatestTVRecordingsUnfinished.ToString()));
          LatestMyFilms = GetBool(xmlreader.GetValueAsString("LatestMediaHandler", "latestMyFilms", LatestMyFilms.ToString()));
          LatestMyFilmsWatched = GetBool(xmlreader.GetValueAsString("LatestMediaHandler", "latestMyFilmsWatched", LatestMyFilmsWatched.ToString()));
          LatestMvCentral = GetBool(xmlreader.GetValueAsString("LatestMediaHandler", "latestMvCentral", LatestMvCentral.ToString()));
          LatestMvCentralThumbType = xmlreader.GetValueAsInt("LatestMediaHandler", "latestMvCentralThumbType", LatestMvCentralThumbType);
          RefreshDbPicture = GetBool(xmlreader.GetValueAsString("LatestMediaHandler", "refreshDbPicture", RefreshDbPicture.ToString()));
          RefreshDbMusic = GetBool(xmlreader.GetValueAsString("LatestMediaHandler", "refreshDbMusic", RefreshDbMusic.ToString()));
          ReorgInterval = xmlreader.GetValueAsString("LatestMediaHandler", "reorgInterval", ReorgInterval);
          //useLatestMediaCache = xmlreader.GetValueAsString("LatestMediaHandler", "useLatestMediaCache", useLatestMediaCache);
          DateFormat = xmlreader.GetValueAsString("LatestMediaHandler", "dateFormat", DateFormat);
          ScanDelay = xmlreader.GetValueAsInt("LatestMediaHandler", "ScanDelay", ScanDelay);
          PreloadImages = xmlreader.GetValueAsBool("LatestMediaHandler", "PreloadImages", PreloadImages);
          PreloadImagesInThread = xmlreader.GetValueAsBool("LatestMediaHandler", "PreloadImagesInThread", PreloadImagesInThread);
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
      {
        if (!string.IsNullOrEmpty(LatestTVSeriesRatings))
        {
/*
          "TV-Y: This program is designed to be appropriate for all children");
          "TV-Y7: This program is designed for children age 7 and above.");
          "TV-G: Most parents would find this program suitable for all ages.");
          "TV-PG: This program contains material that parents may find unsuitable for younger children.");
          "TV-14: This program contains some material that many parents would find unsuitable for children under 14 years of age.");
          "TV-MA: This program is specifically designed to be viewed by adults and therefore may be unsuitable for children under 17.");            
*/
          string[] s = LatestTVSeriesRatings.Split(';');
          string[] r = DefTVSeriesRatings.Split(';');

          LatestTVSeriesRatings = string.Empty;
          for (int i = 0; i < s.Length; i++)
          {
            if (s[i].Equals("1"))
            {
              LatestTVSeriesRatings = LatestTVSeriesRatings + (string.IsNullOrEmpty(LatestTVSeriesRatings) ? string.Empty : ";") + r[i];
            }
          }
        }
        else
        {
          LatestTVSeriesRatings = DefTVSeriesRatings;
        }
      }
      #endregion

      #region Report Settings
      logger.Debug("Latest: " + Check(LatestPictures) + " Pictures, " + 
                                Check(LatestMusic) + " Music, " +
                                Check(LatestMyVideos) + Check(LatestMyVideosWatched) + " MyVideo, " + 
                                Check(LatestTVSeries) + Check(LatestTVSeriesWatched) + " TVSeries, " +
                                Check(LatestTVRecordings) + Check(LatestTVRecordingsWatched) + " TV Recordings, " +
                                Check(LatestMovingPictures) + Check(LatestMovingPicturesWatched) + " MovingPictures, " +
                                Check(LatestMyFilms) + Check(LatestMyFilmsWatched) + " MyFilms, " +
                                Check(LatestMvCentral) + " MvCentral");
      logger.Debug("Music Type: " + LatestMusicType) ;
      logger.Debug("TVSeries View: " + (LatestTVSeriesView == 0 ? "Latests" : (LatestTVSeriesType == 1 ? "Watched" : (LatestTVSeriesType == 2 ? "Rated" : "Next"))));
      logger.Debug("TVSeries Type: " + (LatestTVSeriesType == 2 ? "Series" : (LatestTVSeriesType == 1 ? "Seasons" : "Episodes")));
      logger.Debug("MvCentral Thumb Type: " + (LatestMvCentralThumbType == 2 ? "Album" : (LatestMvCentralThumbType == 1 ? "Artist" : "Track")));
      logger.Debug("TVSeries ratings: " + LatestTVSeriesRatings) ;
      logger.Debug("DB: " + Check(RefreshDbPicture) + " Pictures, " + 
                            Check(RefreshDbMusic) + " Music, "+
                            "Interval: " + ReorgInterval);
      logger.Debug("Date Format: " + DateFormat) ;
      logger.Debug("Scan Delay: " + ScanDelay + "s") ;
      logger.Debug("Plugin enabled: " + Check(PluginIsEnabled("Music")) + " Music, " +
                                        Check(PluginIsEnabled("Pictures")) + " Pictures, " +
                                        Check(PluginIsEnabled("Videos")) + " MyVideo, " +
                                        Check(PluginIsEnabled("MP-TV Series")) + " TVSeries, " +
                                        Check(PluginIsEnabled("Moving Pictures")) + " MovingPictures, " +
                                        Check(PluginIsEnabled("MyFilms")) + " MyFilms, " +
                                        Check(PluginIsEnabled(GetProperty("#mvCentral.Settings.HomeScreenName"))) + " MvCentral, " + 
                                        Check(PluginIsEnabled("FanartHandler") || PluginIsEnabled("Fanart Handler")) + " FanartHandler");
      logger.Debug("Image: " + Check(PreloadImages) + " Preload, " + Check(PreloadImagesInThread) + " In thread.");
      #endregion

      #region Post setting 
      if (!Conf)
      {
        FanartHandler = PluginIsEnabled("FanartHandler") || PluginIsEnabled("Fanart Handler");

        LatestMusic = PluginIsEnabled("Music") && LatestMusic;
        LatestPictures = PluginIsEnabled("Pictures") && LatestPictures;
        LatestMyVideos = PluginIsEnabled("Videos") && LatestMyVideos;
        LatestTVSeries = PluginIsEnabled("MP-TV Series") && LatestTVSeries;
        LatestMovingPictures = PluginIsEnabled("Moving Pictures") && LatestMovingPictures;
        LatestMyFilms = PluginIsEnabled("MyFilms") && LatestMyFilms;
        LatestMvCentral = PluginIsEnabled(GetProperty("#mvCentral.Settings.HomeScreenName")) && LatestMvCentral;
      }

      ScanDelay = ScanDelay * 1000;
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
          xmlwriter.SetValue("LatestMediaHandler", "latestPictures", LatestPictures);
          xmlwriter.SetValue("LatestMediaHandler", "latestMusic", LatestMusic);
          xmlwriter.SetValue("LatestMediaHandler", "latestMusicType", LatestMusicType.ToString());
          xmlwriter.SetValue("LatestMediaHandler", "latestMyVideos", LatestMyVideos);
          xmlwriter.SetValue("LatestMediaHandler", "latestMyVideosWatched", LatestMyVideosWatched);
          xmlwriter.SetValue("LatestMediaHandler", "latestMovingPictures", LatestMovingPictures);
          xmlwriter.SetValue("LatestMediaHandler", "latestMovingPicturesWatched", LatestMovingPicturesWatched);
          xmlwriter.SetValue("LatestMediaHandler", "latestTVSeries", LatestTVSeries);
          xmlwriter.SetValue("LatestMediaHandler", "latestTVSeriesWatched", LatestTVSeriesWatched);
          xmlwriter.SetValue("LatestMediaHandler", "latestTVSeriesRatings", LatestTVSeriesRatings);
          xmlwriter.SetValue("LatestMediaHandler", "latestTVSeriesView", LatestTVSeriesView);
          xmlwriter.SetValue("LatestMediaHandler", "latestTVSeriesType", LatestTVSeriesType);
          // xmlwriter.SetValue("LatestMediaHandler", "latestPlotOutlineSentencesNum", LatestPlotOutlineSentencesNum);
          xmlwriter.SetValue("LatestMediaHandler", "latestTVRecordings", LatestTVRecordings);
          xmlwriter.SetValue("LatestMediaHandler", "latestTVRecordingsWatched", LatestTVRecordingsWatched);
          xmlwriter.SetValue("LatestMediaHandler", "latestTVRecordingsUnfinished", LatestTVRecordingsUnfinished);
          xmlwriter.SetValue("LatestMediaHandler", "latestMyFilms", LatestMyFilms);
          xmlwriter.SetValue("LatestMediaHandler", "latestMyFilmsWatched", LatestMyFilmsWatched);
          xmlwriter.SetValue("LatestMediaHandler", "latestMvCentral", LatestMvCentral);
          xmlwriter.SetValue("LatestMediaHandler", "latestMvCentralThumbType", LatestMvCentralThumbType);
          xmlwriter.SetValue("LatestMediaHandler", "refreshDbPicture", RefreshDbPicture);
          xmlwriter.SetValue("LatestMediaHandler", "refreshDbMusic", RefreshDbMusic);
          xmlwriter.SetValue("LatestMediaHandler", "reorgInterval", ReorgInterval);
          xmlwriter.SetValue("LatestMediaHandler", "dateFormat", DateFormat);
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

    public static void LoadSkinSettings()
    {
      string skinSettings = GUIGraphicsContext.GetThemedSkinFile(@"\" + ConfigSkinFilename);
      if (!File.Exists(skinSettings))
      {
        return;
      }

      try
      {
        logger.Debug("Load Skin settings from file: {0}", skinSettings);

        XmlDocument doc = new XmlDocument();
        doc.Load(skinSettings);

        if (doc.DocumentElement != null)
        {
          XmlNodeList settingsList = doc.DocumentElement.SelectNodes("/settings");

          if (settingsList == null)
          {
            logger.Debug("Settings tag for file: {0} not exist. Skipped.", ConfigSkinFilename);
            return;
          }

          foreach (XmlNode nodeSetting in settingsList)
          {
            if (nodeSetting != null)
            {
              #region Main Skin settings
              XmlNode nodeSkinMain = nodeSetting.SelectSingleNode("main");
              if (nodeSkinMain != null)
              {
                logger.Debug("Load Skin Main settings from file: {0}", skinSettings);
                XmlNode nodeText = nodeSkinMain.SelectSingleNode("facades");
                if (nodeText != null && nodeText.InnerText != null)
                {
                  string innerText = nodeText.InnerText;
                  if (!string.IsNullOrWhiteSpace(innerText))
                  {
                    SkinUseFacades = Utils.GetBool(innerText);
                    logger.Debug("Skin use " + Utils.Check(SkinUseFacades) + " facades.");
                  }
                }

                nodeText = nodeSkinMain.SelectSingleNode("latestscount");
                if (nodeText != null && nodeText.InnerText != null)
                {
                  string innerText = nodeText.InnerText;
                  if (!string.IsNullOrWhiteSpace(innerText))
                  {
                    int innerInt = LatestsMaxNum;
                    if (Int32.TryParse(innerText, out innerInt))
                    {
                      if (innerInt <= 0)
                      {
                        innerInt = LatestsMaxNum;
                      }
                      if (innerInt > FacadeMaxNum)
                      {
                        innerInt = FacadeMaxNum;
                      }
                      LatestsMaxNum = innerInt;
                      logger.Debug("Skin - Latests items count: {0}", LatestsMaxNum);
                    }
                  }
                }
                nodeText = nodeSkinMain.SelectSingleNode("tvlatestscount");
                if (nodeText != null && nodeText.InnerText != null)
                {
                  string innerText = nodeText.InnerText;
                  if (!string.IsNullOrWhiteSpace(innerText))
                  {
                    int innerInt = LatestsMaxTVNum;
                    if (Int32.TryParse(innerText, out innerInt))
                    {
                      if (innerInt <= 0)
                      {
                        innerInt = LatestsMaxTVNum;
                      }
                      if (innerInt > FacadeMaxNum)
                      {
                        innerInt = FacadeMaxNum;
                      }
                      LatestsMaxTVNum = innerInt;
                      logger.Debug("Skin - TV Latests items count: {0}", LatestsMaxTVNum);
                    }
                  }
                }
              }
              #endregion

              #region Additional Play IDs
              XmlNode nodeIDs = nodeSetting.SelectSingleNode("playids");
              if (nodeIDs != null)
              {
                logger.Debug("Load Skin settings Additional Play IDs for Latests from file: {0}", skinSettings);

                // Videos Play IDs
                if (Utils.LatestMyVideos && LatestsMaxNum > 4)
                {
                  XmlNode nodeMain = nodeIDs.SelectSingleNode("video");
                  if (nodeMain != null)
                  {
                    LatestMyVideosHandler mainHandler = (LatestMyVideosHandler)LatestMediaHandlerSetup.GetMainHandler(LatestsCategory.Movies);
                    if (mainHandler != null)
                    {
                      // Additional Play IDs
                      XmlNodeList additionalList = nodeMain.SelectNodes("id");
                      foreach (XmlNode nodeAdditional in additionalList)
                      {
                        if (nodeAdditional != null && nodeAdditional.InnerText != null)
                        {
                          string innerText = nodeAdditional.InnerText;
                          if (!string.IsNullOrWhiteSpace(innerText))
                          {
                            int innerInt = 0;
                            if (Int32.TryParse(innerText, out innerInt))
                            {
                              if (mainHandler.ControlIDPlays.IndexOf(innerInt) < 0)
                              {
                                mainHandler.ControlIDPlays.Add(innerInt);
                                logger.Debug("Skin settings - Video, add additional ID: {0}", innerInt);
                              }
                            }
                          }
                        }
                      }
                    }
                  }
                }

                // MovingPictures Play IDs
                if (Utils.LatestMovingPictures && LatestsMaxNum > 4)
                {
                  XmlNode nodeMain = nodeIDs.SelectSingleNode("movingpictures");
                  if (nodeMain != null)
                  {
                    LatestMovingPicturesHandler mainHandler = (LatestMovingPicturesHandler)LatestMediaHandlerSetup.GetMainHandler(LatestsCategory.MovingPictures);
                    if (mainHandler != null)
                    {
                      // Additional Play IDs
                      XmlNodeList additionalList = nodeMain.SelectNodes("id");
                      foreach (XmlNode nodeAdditional in additionalList)
                      {
                        if (nodeAdditional != null && nodeAdditional.InnerText != null)
                        {
                          string innerText = nodeAdditional.InnerText;
                          if (!string.IsNullOrWhiteSpace(innerText))
                          {
                            int innerInt = 0;
                            if (Int32.TryParse(innerText, out innerInt))
                            {
                              if (mainHandler.ControlIDPlays.IndexOf(innerInt) < 0)
                              {
                                mainHandler.ControlIDPlays.Add(innerInt);
                                logger.Debug("Skin settings - MovingPictures, add additional ID: {0}", innerInt);
                              }
                            }
                          }
                        }
                      }
                    }
                  }
                }

                // MyFilms Play IDs
                if (Utils.LatestMyFilms && LatestsMaxNum > 4)
                {
                  XmlNode nodeMain = nodeIDs.SelectSingleNode("myfilms");
                  if (nodeMain != null)
                  {
                    LatestMyFilmsHandler mainHandler = (LatestMyFilmsHandler)LatestMediaHandlerSetup.GetMainHandler(LatestsCategory.MyFilms);
                    if (mainHandler != null)
                    {
                      // Additional Play IDs
                      XmlNodeList additionalList = nodeMain.SelectNodes("id");
                      foreach (XmlNode nodeAdditional in additionalList)
                      {
                        if (nodeAdditional != null && nodeAdditional.InnerText != null)
                        {
                          string innerText = nodeAdditional.InnerText;
                          if (!string.IsNullOrWhiteSpace(innerText))
                          {
                            int innerInt = 0;
                            if (Int32.TryParse(innerText, out innerInt))
                            {
                              if (mainHandler.ControlIDPlays.IndexOf(innerInt) < 0)
                              {
                                mainHandler.ControlIDPlays.Add(innerInt);
                                logger.Debug("Skin settings - MyFilms, add additional ID: {0}", innerInt);
                              }
                            }
                          }
                        }
                      }
                    }
                  }
                }

                // TVSeries Play IDs
                if (Utils.LatestTVSeries && LatestsMaxNum > 4)
                {
                  XmlNode nodeMain = nodeIDs.SelectSingleNode("tvseries");
                  if (nodeMain != null)
                  {
                    LatestTVSeriesHandler mainHandler = (LatestTVSeriesHandler)LatestMediaHandlerSetup.GetMainHandler(LatestsCategory.TVSeries);
                    if (mainHandler != null)
                    {
                      // Additional Play IDs
                      XmlNodeList additionalList = nodeMain.SelectNodes("id");
                      foreach (XmlNode nodeAdditional in additionalList)
                      {
                        if (nodeAdditional != null && nodeAdditional.InnerText != null)
                        {
                          string innerText = nodeAdditional.InnerText;
                          if (!string.IsNullOrWhiteSpace(innerText))
                          {
                            int innerInt = 0;
                            if (Int32.TryParse(innerText, out innerInt))
                            {
                              if (mainHandler.ControlIDPlays.IndexOf(innerInt) < 0)
                              {
                                mainHandler.ControlIDPlays.Add(innerInt);
                                logger.Debug("Skin settings - TVSeries, add additional ID: {0}", innerInt);
                              }
                            }
                          }
                        }
                      }
                    }
                  }
                }

                // MvCentral Play IDs
                if (Utils.LatestMvCentral && LatestsMaxNum > 4)
                {
                  XmlNode nodeMain = nodeIDs.SelectSingleNode("mvcentral");
                  if (nodeMain != null)
                  {
                    LatestMvCentralHandler mainHandler = (LatestMvCentralHandler)LatestMediaHandlerSetup.GetMainHandler(LatestsCategory.MvCentral);
                    if (mainHandler != null)
                    {
                      // Additional Play IDs
                      XmlNodeList additionalList = nodeMain.SelectNodes("id");
                      foreach (XmlNode nodeAdditional in additionalList)
                      {
                        if (nodeAdditional != null && nodeAdditional.InnerText != null)
                        {
                          string innerText = nodeAdditional.InnerText;
                          if (!string.IsNullOrWhiteSpace(innerText))
                          {
                            int innerInt = 0;
                            if (Int32.TryParse(innerText, out innerInt))
                            {
                              if (mainHandler.ControlIDPlays.IndexOf(innerInt) < 0)
                              {
                                mainHandler.ControlIDPlays.Add(innerInt);
                                logger.Debug("Skin settings - MvCentral, add additional ID: {0}", innerInt);
                              }
                            }
                          }
                        }
                      }
                    }
                  }
                }

                // Music Play IDs
                if (Utils.LatestMusic && LatestsMaxNum > 4)
                {
                  XmlNode nodeMain = nodeIDs.SelectSingleNode("music");
                  if (nodeMain != null)
                  {
                    LatestMusicHandler mainHandler = (LatestMusicHandler)LatestMediaHandlerSetup.GetMainHandler(LatestsCategory.Music);
                    if (mainHandler != null)
                    {
                      // Additional Play IDs
                      XmlNodeList additionalList = nodeMain.SelectNodes("id");
                      foreach (XmlNode nodeAdditional in additionalList)
                      {
                        if (nodeAdditional != null && nodeAdditional.InnerText != null)
                        {
                          string innerText = nodeAdditional.InnerText;
                          if (!string.IsNullOrWhiteSpace(innerText))
                          {
                            int innerInt = 0;
                            if (Int32.TryParse(innerText, out innerInt))
                            {
                              if (mainHandler.ControlIDPlays.IndexOf(innerInt) < 0)
                              {
                                mainHandler.ControlIDPlays.Add(innerInt);
                                logger.Debug("Skin settings - Music, add additional ID: {0}", innerInt);
                              }
                            }
                          }
                        }
                      }
                    }
                  }
                }

                // Pictures Play IDs
                if (Utils.LatestPictures && LatestsMaxNum > 4)
                {
                  XmlNode nodeMain = nodeIDs.SelectSingleNode("pictures");
                  if (nodeMain != null)
                  {
                    LatestPictureHandler mainHandler = (LatestPictureHandler)LatestMediaHandlerSetup.GetMainHandler(LatestsCategory.Pictures);
                    if (mainHandler != null)
                    {
                      // Additional Play IDs
                      XmlNodeList additionalList = nodeMain.SelectNodes("id");
                      foreach (XmlNode nodeAdditional in additionalList)
                      {
                        if (nodeAdditional != null && nodeAdditional.InnerText != null)
                        {
                          string innerText = nodeAdditional.InnerText;
                          if (!string.IsNullOrWhiteSpace(innerText))
                          {
                            int innerInt = 0;
                            if (Int32.TryParse(innerText, out innerInt))
                            {
                              if (mainHandler.ControlIDPlays.IndexOf(innerInt) < 0)
                              {
                                mainHandler.ControlIDPlays.Add(innerInt);
                                logger.Debug("Skin settings - Pictures, add additional ID: {0}", innerInt);
                              }
                            }
                          }
                        }
                      }
                    }
                  }
                }

                // TV Play IDs
                if (Utils.LatestPictures && LatestsMaxTVNum > 4)
                {
                  XmlNode nodeMain = nodeIDs.SelectSingleNode("tv");
                  if (nodeMain != null)
                  {
                    LatestTVAllRecordingsHandler mainHandler = (LatestTVAllRecordingsHandler)LatestMediaHandlerSetup.GetMainHandler(LatestsCategory.TV);
                    if (mainHandler != null)
                    {
                      // Additional Play IDs
                      XmlNodeList additionalList = nodeMain.SelectNodes("id");
                      foreach (XmlNode nodeAdditional in additionalList)
                      {
                        if (nodeAdditional != null && nodeAdditional.InnerText != null)
                        {
                          string innerText = nodeAdditional.InnerText;
                          if (!string.IsNullOrWhiteSpace(innerText))
                          {
                            int innerInt = 0;
                            if (Int32.TryParse(innerText, out innerInt))
                            {
                              if (mainHandler.ControlIDPlays.IndexOf(innerInt) < 0)
                              {
                                mainHandler.ControlIDPlays.Add(innerInt);
                                logger.Debug("Skin settings - TV, add additional ID: {0}", innerInt);
                              }
                            }
                          }
                        }
                      }
                    }
                  }
                }

              }
              #endregion

              #region Video Skin settings
              // Videos settings
              if (Utils.LatestMyVideos)
              {
                XmlNode node = nodeSetting.SelectSingleNode("video");
                if (node != null)
                {
                  logger.Debug("Load Skin settings MyVideo from file: {0}", skinSettings);

                  XmlNode nodeMain = node.SelectSingleNode("main");
                  if (nodeMain != null)
                  {
                    LatestMyVideosHandler mainHandler = (LatestMyVideosHandler)LatestMediaHandlerSetup.GetMainHandler(LatestsCategory.Movies);
                    if (mainHandler != null)
                    {
                      mainHandler.CurrentFacade.SetMainFacadeFromSkin(nodeMain);
                    }
                  }

                  // Additional Facades
                  XmlNodeList additionalList = node.SelectNodes("additional");
                  foreach (XmlNode nodeAdditional in additionalList)
                  {
                    if (nodeAdditional != null)
                    {
                      LatestsFacade facade = new LatestsFacade("MyVideo", nodeAdditional);
                      if (facade.ControlID == 0 || facade.ControlID == LatestMyVideosHandler.ControlID)
                      {
                        continue;
                      }
                      logger.Debug("Load Skin settings MyVideo Facade {0}: {1} - {2} - {3} - {4}", facade.ControlID, Check(facade.LeftToRight), Check(facade.UnWatched), facade.Type, facade.Layout);
                      LatestMediaHandlerSetup.Handlers.Add(new LatestMyVideosHandler(facade));
                    }
                  }
                }
              }
              #endregion

              #region MovingPictures Skin settings
              // MovingPictures settings
              if (Utils.LatestMovingPictures)
              {
                XmlNode node = nodeSetting.SelectSingleNode("movingpictures");
                if (node != null)
                {
                  logger.Debug("Load Skin settings MovingPictures from file: {0}", skinSettings);

                  XmlNode nodeMain = node.SelectSingleNode("main");
                  if (nodeMain != null)
                  {
                    LatestMovingPicturesHandler mainHandler = (LatestMovingPicturesHandler)LatestMediaHandlerSetup.GetMainHandler(LatestsCategory.MovingPictures);
                    if (mainHandler != null)
                    {
                      mainHandler.CurrentFacade.SetMainFacadeFromSkin(nodeMain);
                    }
                  }

                  // Additional Facades
                  XmlNodeList additionalList = node.SelectNodes("additional");
                  foreach (XmlNode nodeAdditional in additionalList)
                  {
                    if (nodeAdditional != null)
                    {
                      LatestsFacade facade = new LatestsFacade("MovingPicture", nodeAdditional);
                      if (facade.ControlID == 0 || facade.ControlID == LatestMovingPicturesHandler.ControlID)
                      {
                        continue;
                      }
                      logger.Debug("Load Skin settings MovingPictures Facade {0}: {1} - {2} - {3} - {4}", facade.ControlID, Check(facade.LeftToRight), Check(facade.UnWatched), facade.Type, facade.Layout);
                      LatestMediaHandlerSetup.Handlers.Add(new LatestMovingPicturesHandler(facade));
                    }
                  }
                }
              }
              #endregion

              #region MyFilms Skin settings
              // MyFilms settings
              if (Utils.LatestMyFilms)
              {
                XmlNode node = nodeSetting.SelectSingleNode("myfilms");
                if (node != null)
                {
                  logger.Debug("Load Skin settings MyFilms from file: {0}", skinSettings);

                  XmlNode nodeMain = node.SelectSingleNode("main");
                  if (nodeMain != null)
                  {
                    LatestMyFilmsHandler mainHandler = (LatestMyFilmsHandler)LatestMediaHandlerSetup.GetMainHandler(LatestsCategory.MyFilms);
                    if (mainHandler != null)
                    {
                      mainHandler.CurrentFacade.SetMainFacadeFromSkin(nodeMain);
                    }
                  }

                  // Additional Facades
                  XmlNodeList additionalList = node.SelectNodes("additional");
                  foreach (XmlNode nodeAdditional in additionalList)
                  {
                    if (nodeAdditional != null)
                    {
                      LatestsFacade facade = new LatestsFacade("MyFilms", nodeAdditional);
                      if (facade.ControlID == 0 || facade.ControlID == LatestMyFilmsHandler.ControlID)
                      {
                        continue;
                      }
                      logger.Debug("Load Skin settings MyFilms Facade {0}: {1} - {2} - {3} - {4}", facade.ControlID, Check(facade.LeftToRight), Check(facade.UnWatched), facade.Type, facade.Layout);
                      LatestMediaHandlerSetup.Handlers.Add(new LatestMyFilmsHandler(facade));
                    }
                  }
                }
              }
              #endregion

              #region TVSeries Skin settings
              // TVSeries settings
              if (Utils.LatestTVSeries)
              {
                XmlNode node = nodeSetting.SelectSingleNode("tvseries");
                if (node != null)
                {
                  logger.Debug("Load Skin settings TVSeries from file: {0}", skinSettings);

                  XmlNode nodeMain = node.SelectSingleNode("main");
                  if (nodeMain != null)
                  {
                    LatestTVSeriesHandler mainHandler = (LatestTVSeriesHandler)LatestMediaHandlerSetup.GetMainHandler(LatestsCategory.TVSeries);
                    if (mainHandler != null)
                    {
                      mainHandler.CurrentFacade.SetMainFacadeFromSkin(nodeMain);
                    }
                  }

                  // Additional Facades
                  XmlNodeList additionalList = node.SelectNodes("additional");
                  foreach (XmlNode nodeAdditional in additionalList)
                  {
                    if (nodeAdditional != null)
                    {
                      LatestsFacade facade = new LatestsFacade("TVSeries", nodeAdditional);
                      if (facade.ControlID == 0 || facade.ControlID == LatestTVSeriesHandler.ControlID)
                      {
                        continue;
                      }

                      if (facade.SubType == LatestsFacadeSubType.None)
                      {
                        switch (Utils.LatestTVSeriesType)
                        {
                          case 0:
                            facade.SubType = LatestsFacadeSubType.Episodes;
                            break;
                          case 1:
                            facade.SubType = LatestsFacadeSubType.Seasons;
                            break;
                          case 2:
                            facade.SubType = LatestsFacadeSubType.Series;
                            break;
                        }
                      }
                      logger.Debug("Load Skin settings TVSeries Facade {0}: {1} - {2} - {3} - {4} - {5}", facade.ControlID, Check(facade.LeftToRight), Check(facade.UnWatched), facade.Type, facade.SubType, facade.Layout);
                      LatestMediaHandlerSetup.Handlers.Add(new LatestTVSeriesHandler(facade));
                    }
                  }
                }
              }
              #endregion

              #region Music Skin settings
              // Music settings
              if (Utils.LatestMusic)
              {
                XmlNode node = nodeSetting.SelectSingleNode("music");
                if (node != null)
                {
                  logger.Debug("Load Skin settings Music from file: {0}", skinSettings);

                  XmlNode nodeMain = node.SelectSingleNode("main");
                  if (nodeMain != null)
                  {
                    LatestMusicHandler mainHandler = (LatestMusicHandler)LatestMediaHandlerSetup.GetMainHandler(LatestsCategory.Music);
                    if (mainHandler != null)
                    {
                      mainHandler.CurrentFacade.SetMainFacadeFromSkin(nodeMain);
                    }
                  }

                  // Additional Facades
                  XmlNodeList additionalList = node.SelectNodes("additional");
                  foreach (XmlNode nodeAdditional in additionalList)
                  {
                    if (nodeAdditional != null)
                    {
                      LatestsFacade facade = new LatestsFacade("Music", nodeAdditional);
                      if (facade.ControlID == 0 || facade.ControlID == LatestMusicHandler.ControlID)
                      {
                        continue;
                      }
                      logger.Debug("Load Skin settings Music Facade {0}: {1} - {2} - {3} - {4}", facade.ControlID, Check(facade.LeftToRight), Check(facade.UnWatched), facade.Type, facade.Layout);
                      LatestMediaHandlerSetup.Handlers.Add(new LatestMusicHandler(facade));
                    }
                  }
                }
              }
              #endregion

              #region MvCentral Skin settings
              // MvCentral settings
              if (Utils.LatestMvCentral)
              {
                XmlNode node = nodeSetting.SelectSingleNode("mvcentral");
                if (node != null)
                {
                  logger.Debug("Load Skin settings MvCentral from file: {0}", skinSettings);

                  XmlNode nodeMain = node.SelectSingleNode("main");
                  if (nodeMain != null)
                  {
                    LatestMvCentralHandler mainHandler = (LatestMvCentralHandler)LatestMediaHandlerSetup.GetMainHandler(LatestsCategory.MvCentral);
                    if (mainHandler != null)
                    {
                      mainHandler.CurrentFacade.SetMainFacadeFromSkin(nodeMain);
                    }
                  }

                  // Additional Facades
                  XmlNodeList additionalList = node.SelectNodes("additional");
                  foreach (XmlNode nodeAdditional in additionalList)
                  {
                    if (nodeAdditional != null)
                    {
                      LatestsFacade facade = new LatestsFacade("MvCentral", nodeAdditional);
                      if (facade.ControlID == 0 || facade.ControlID == LatestMusicHandler.ControlID)
                      {
                        continue;
                      }

                      if (facade.SubType == LatestsFacadeSubType.None)
                      {
                        switch (Utils.LatestMvCentralThumbType)
                        {
                          case 1:
                            facade.ThumbType = LatestsFacadeThumbType.Artist;
                            break;
                          case 2:
                            facade.ThumbType = LatestsFacadeThumbType.Album;
                            break;
                          case 3:
                            facade.ThumbType = LatestsFacadeThumbType.Track;
                            break;
                        }
                      }
                      logger.Debug("Load Skin settings MvCentral Facade {0}: {1} - {2} - {3} - {4} - {5}", facade.ControlID, Check(facade.LeftToRight), Check(facade.UnWatched), facade.Type, facade.SubType, facade.Layout);
                      LatestMediaHandlerSetup.Handlers.Add(new LatestMvCentralHandler(facade));
                    }
                  }
                }
              }
              #endregion

              #region TVRecordings Skin settings
              // TVRecordings settings
              if (Utils.LatestTVRecordings)
              {
                XmlNode node = nodeSetting.SelectSingleNode("tv");
                if (node != null)
                {
                  logger.Debug("Load Skin settings TVRecordings from file: {0}", skinSettings);

                  XmlNode nodeMain = node.SelectSingleNode("main");
                  if (nodeMain != null)
                  {
                    LatestTVAllRecordingsHandler mainHandler = (LatestTVAllRecordingsHandler)LatestMediaHandlerSetup.GetMainHandler(LatestsCategory.TV);
                    if (mainHandler != null)
                    {
                      mainHandler.CurrentFacade.SetMainFacadeFromSkin(nodeMain);
                    }
                  }

                  // Additional Facades
                  XmlNodeList additionalList = node.SelectNodes("additional");
                  foreach (XmlNode nodeAdditional in additionalList)
                  {
                    if (nodeAdditional != null)
                    {
                      LatestsFacade facade = new LatestsFacade("TVRecordings", nodeAdditional);
                      if (facade.ControlID == 0 || facade.ControlID == LatestMusicHandler.ControlID)
                      {
                        continue;
                      }
                      logger.Debug("Load Skin settings TVRecordings Facade {0}: {1} - {2} - {3} - {4}", facade.ControlID, Check(facade.LeftToRight), Check(facade.UnWatched), facade.Type, facade.Layout);
                      LatestMediaHandlerSetup.Handlers.Add(new LatestTVAllRecordingsHandler(facade));
                    }
                  }
                }
              }
              #endregion

              #region Pictures Skin settings
              // Pictures settings
              if (Utils.LatestPictures)
              {
                XmlNode node = nodeSetting.SelectSingleNode("pictures");
                if (node != null)
                {
                  logger.Debug("Load Skin settings Pictures from file: {0}", skinSettings);

                  XmlNode nodeMain = node.SelectSingleNode("main");
                  if (nodeMain != null)
                  {
                    LatestPictureHandler mainHandler = (LatestPictureHandler)LatestMediaHandlerSetup.GetMainHandler(LatestsCategory.Pictures);
                    if (mainHandler != null)
                    {
                      mainHandler.CurrentFacade.SetMainFacadeFromSkin(nodeMain);
                    }
                  }

                  // Additional Facades
                  XmlNodeList additionalList = node.SelectNodes("additional");
                  foreach (XmlNode nodeAdditional in additionalList)
                  {
                    if (nodeAdditional != null)
                    {
                      LatestsFacade facade = new LatestsFacade("Picture", nodeAdditional);
                      if (facade.ControlID == 0 || facade.ControlID == LatestMusicHandler.ControlID)
                      {
                        continue;
                      }
                      logger.Debug("Load Skin settings Picture Facade {0}: {1} - {2} - {3} - {4}", facade.ControlID, Check(facade.LeftToRight), Check(facade.UnWatched), facade.Type, facade.Layout);
                      LatestMediaHandlerSetup.Handlers.Add(new LatestPictureHandler(facade));
                    }
                  }
                }
              }
              #endregion
            }
          }
          logger.Debug("Load Skin settings from file: {0} complete.", Utils.ConfigSkinFilename);
        }
      }
      catch (Exception ex)
      {
        logger.Error("LoadSkinSettings: Error loading Skin settings from file: {0} - {1} ", Utils.ConfigSkinFilename, ex.Message);
      }
    }
    #endregion

    public enum Category
    {
      All,
      Video,
      Music,
      Pictures,
      TV,
    }

    public enum LatestsCategory
    {
      Music, 
      MvCentral, 
      Movies, 
      MovingPictures, 
      TVSeries, 
      MyFilms,
      Pictures,
      TV,   
    }

    public enum FanartTV
    {
      MusicThumb,
      MusicBackground,
      MusicCover,
      MusicClearArt, 
      MusicBanner, 
      MusicCDArt, 
      MusicLabel,
      MoviesPoster,
      MoviesBackground,
      MoviesClearArt, 
      MoviesBanner, 
      MoviesClearLogo, 
      MoviesCDArt,
      MoviesCollectionPoster,
      MoviesCollectionBackground,
      MoviesCollectionClearArt,
      MoviesCollectionBanner,
      MoviesCollectionClearLogo,
      MoviesCollectionCDArt,
      SeriesPoster,
      SeriesThumb,
      SeriesBackground,
      SeriesBanner,
      SeriesClearArt,
      SeriesClearLogo, 
      SeriesCDArt,
      SeriesSeasonPoster,
      SeriesSeasonThumb,
      SeriesSeasonBanner,
      SeriesSeasonCDArt,
      SeriesCharacter,
      None, 
    }

    public enum Animated
    {
      MoviesPoster,
      MoviesBackground,
      None,
    }

    public enum TVCategory
    {
      Latests,
      Recording,
    }

    public enum Priority
    {
      Lowest,
      BelowNormal,
    }

  }
}
