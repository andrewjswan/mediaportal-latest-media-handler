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

using RealNLog.NLog;

using SQLite.NET;

using System;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using System.Threading;
using System.Timers;
using System.Windows.Forms;

using MediaPortal.Picture.Database;
using MediaPortal.Util;
using MediaPortal.Profile;
using MediaPortal.GUI.Library;
using MediaPortal.GUI.Pictures;
using MediaPortal.Configuration;
using MediaPortal.Dialogs;
using MediaPortal.Player;

namespace LatestMediaHandler
{
  /// <summary>
  /// Class handling all external (not Latest Media Handler db) database access.
  /// </summary>
  internal class LatestPictureHandler
  {
    #region declarations

    private static Logger logger = LogManager.GetCurrentClassLogger();

    private SQLiteClient dbClient;
    private bool noLargeThumbnails = true;
    private VirtualDirectory virtualDirectory = new VirtualDirectory();
    private GUIFacadeControl facade = null;

    private LatestsCollection latestPictures;
    internal Hashtable latestPicturesFiles;

    private ArrayList al = new ArrayList();
    internal ArrayList images = new ArrayList();
    internal ArrayList imagesThumbs = new ArrayList();

    private int selectedFacadeItem1 = -1;
    private int selectedFacadeItem2 = -1;
    private int showFanart = 1;
    private bool needCleanup = false;
    private int needCleanupCount = 0;
    private int lastFocusedId = 0;

    #endregion

    public const int ControlID = 919199710;
    public const int Play1ControlID = 91919971;
    public const int Play2ControlID = 91919972;
    public const int Play3ControlID = 91919973;

    public List<int> ControlIDFacades;
    public List<int> ControlIDPlays;

    public int LastFocusedId
    {
      get { return lastFocusedId; }
      set { lastFocusedId = value; }
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

    public int ShowFanart
    {
      get { return showFanart; }
      set { showFanart = value; }
    }

    public int SelectedFacadeItem2
    {
      get { return selectedFacadeItem2; }
      set { selectedFacadeItem2 = value; }
    }

    public int SelectedFacadeItem1
    {
      get { return selectedFacadeItem1; }
      set { selectedFacadeItem1 = value; }
    }

    public ArrayList Images
    {
      get { return images; }
      set { images = value; }
    }

    public ArrayList Al
    {
      get { return al; }
      set { al = value; }
    }

    public GUIFacadeControl Facade
    {
      get { return facade; }
      set { facade = value; }
    }

    internal LatestPictureHandler()
    {
      ControlIDFacades = new List<int>();
      ControlIDPlays = new List<int>();
      //
      ControlIDFacades.Add(ControlID);
      ControlIDPlays.Add(Play1ControlID);
      ControlIDPlays.Add(Play2ControlID);
      ControlIDPlays.Add(Play3ControlID);
    }

    /// <summary>
    /// Initiation of the DatabaseManager.
    /// </summary>
    /// <param name="dbFilename">Database filename</param>
    /// <returns>if database was successfully or not</returns>
    private bool InitDB(string dbFilename)
    {
      try
      {
        String path = Config.GetFolder(Config.Dir.Database) + @"\" + dbFilename;
        if (File.Exists(path))
        {
          FileInfo f = new FileInfo(path);
          if (f.Length > 0)
          {
            dbClient = new SQLiteClient(path);
            //dbClient.Execute("PRAGMA synchronous=OFF");
            return true;
          }
        }
      }
      catch //(Exception e)
      {
        //logger.Error("initDB: Could Not Open Database: " + dbFilename + ". " + e.ToString());
        dbClient = null;
      }

      return false;
    }

    /// <summary>
    /// Close the database client.
    /// </summary>
    private void Close()
    {
      try
      {
        if (dbClient != null)
        {
          dbClient.Close();
        }

        dbClient = null;
      }
      catch (Exception ex)
      {
        logger.Error("close: " + ex.ToString());
      }
    }

    internal void RebuildPictureDatabase()
    {
      logger.Info("Scanning picture collection for new pictures - starting");
      try
      {
        ArrayList paths = new ArrayList();

        using (Settings xmlreader = new MPSettings())
        {
          noLargeThumbnails = xmlreader.GetValueAsBool("thumbnails", "picturenolargethumbondemand", true);
          for (int i = 0; i < VirtualDirectory.MaxSharesCount; i++)
          {
            string sPath = String.Format("sharepath{0}", i);
            sPath = xmlreader.GetValueAsString("pictures", sPath, string.Empty);
            paths.Add(sPath);
          }
        }

        // get all pictures from the path
        ArrayList availableFiles = new ArrayList();
        foreach (string path in paths)
        {
          CountFiles(path, ref availableFiles);
        }

        int count = 1;
        int totalFiles = availableFiles.Count;

        // treat each picture file one by one
        foreach (string file in availableFiles)
        {
          // create thumb if not created and add file to db if not already there         
          CreateThumbsAndAddPictureToDB(file);
          count++;
        }
        logger.Info("Scanning picture collection for new pictures - done");
      }
      catch
      {   }
    }

    private void CountFiles(string path, ref ArrayList availableFiles)
    {
      //
      // Count the files in the current directory
      //
      try
      {
        VirtualDirectory dir = new VirtualDirectory();
        dir.SetExtensions(MediaPortal.Util.Utils.PictureExtensions);
        List<GUIListItem> items = dir.GetDirectoryUnProtectedExt(path, true);
        foreach (GUIListItem item in items)
        {
          if (item.IsFolder)
          {
            if (item.Label != "..")
            {
              CountFiles(item.Path, ref availableFiles);
            }
          }
          else
          {
            availableFiles.Add(item.Path);
          }
        }
      }
      catch
      {
      }
    }

    internal void MyContextMenu()
    {
      try
      {
        GUIDialogMenu dlg = (GUIDialogMenu) GUIWindowManager.GetWindow((int) GUIWindow.Window.WINDOW_DIALOG_MENU);
        if (dlg == null) return;

        dlg.Reset();
        dlg.SetHeading(924);

        //Play Menu Item
        GUIListItem pItem = new GUIListItem();
        pItem.Label = Translation.View;
        pItem.ItemId = 1;
        dlg.Add(pItem);

        pItem = new GUIListItem();
        pItem.Label = GUILocalizeStrings.Get(940);
        pItem.ItemId = 2;
        dlg.Add(pItem);

        pItem = new GUIListItem();
        pItem.Label = Translation.Update;
        pItem.ItemId = 3;
        dlg.Add(pItem);

        //Show Dialog
        dlg.DoModal(GUIWindowManager.ActiveWindow);

        if (dlg.SelectedLabel < 0)
        {
          return;
        }

        switch (dlg.SelectedId)
        {
          case 1:
          {
            PlayPictures(GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow));
            break;
          }
          case 2:
          {
            InfoPictures();
            break;
          }
          case 3:
          {
            GetLatestPictures();
            break;
          }
        }
      }
      catch (Exception ex)
      {
        logger.Error("MyContextMenu: " + ex.ToString());
      }
    }

    internal void InfoPictures()
    {
      try
      {
        GUIWindow fWindow = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow);
        int FocusControlID = fWindow.GetFocusControlId();

        int idx = -1;
        if (ControlIDPlays.Contains(FocusControlID))
        {
          idx = ControlIDPlays.IndexOf(FocusControlID)+1;
        }
        //
        facade = Utils.GetLatestsFacade(ControlID);
        if (facade != null && facade.Focus && facade.SelectedListItem != null)
        {
          idx = facade.SelectedListItem.ItemId;
        }
        //
        if (idx > 0)
        {
          GUIDialogExif exifDialog = (GUIDialogExif)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_EXIF);
          // Needed to set GUIDialogExif
          exifDialog.Restore();
          exifDialog = (GUIDialogExif)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_EXIF);
          exifDialog.FileName = latestPicturesFiles[idx].ToString();
          exifDialog.DoModal(fWindow.GetID);
          exifDialog.Restore();
        }
      }
      catch (Exception ex)
      {
        logger.Error("Unable to Info picture! " + ex.ToString());
      }
    }

    internal bool PlayPictures(GUIWindow fWindow)
    {
      try
      {
        /*
        if (fWindow.GetFocusControlId() == Play1ControlID)
        {
          PlayPictures(1);
          return true;
        }
        else if (fWindow.GetFocusControlId() == Play2ControlID)
        {
          PlayPictures(2);
          return true;
        }
        else if (fWindow.GetFocusControlId() == Play3ControlID)
        {
          PlayPictures(3);
          return true;
        }
        */
        int FocusControlID = fWindow.GetFocusControlId();
        if (ControlIDPlays.Contains(FocusControlID))
        {
          PlayPictures(ControlIDPlays.IndexOf(FocusControlID)+1);
          return true;
        }
        //
        facade = Utils.GetLatestsFacade(ControlID);
        if (facade != null && facade.Focus && facade.SelectedListItem != null)
        {
          PlayPictures(facade.SelectedListItem.ItemId);
          return true;
        }
      }
      catch (Exception ex)
      {
        logger.Error("Unable to play picture! " + ex.ToString());
        return true;
      }
      return false;
    }

    internal void PlayPictures(int index)
    {
      // Stop video playback before starting show picture to avoid MP freezing
      if (g_Player.MediaInfo != null && g_Player.MediaInfo.hasVideo || g_Player.IsTV || g_Player.IsVideo)
        g_Player.Stop();

      GUISlideShow SlideShow = (GUISlideShow)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_SLIDESHOW);
      if (SlideShow == null)
        return;

      // if (SlideShow._returnedFromVideoPlayback)
      //  SlideShow._returnedFromVideoPlayback = false;

      SlideShow.Reset();
      SlideShow.Add(latestPicturesFiles[index].ToString());
      if (SlideShow.Count > 0)
      {
        GUIWindowManager.ActivateWindow((int)GUIWindow.Window.WINDOW_SLIDESHOW);
        SlideShow.Select(latestPicturesFiles[index].ToString());
      }
    }

    private void CreateThumbsAndAddPictureToDB(string file)
    {
      int iRotate = PictureDatabase.GetRotation(file);

      string thumbnailImage = String.Format(@"{0}\{1}.jpg", Thumbs.Pictures, MediaPortal.Util.Utils.EncryptLine(file));

      if (!File.Exists(thumbnailImage))
      {
        if (MediaPortal.Util.Picture.CreateThumbnail(file, thumbnailImage, (int) Thumbs.ThumbResolution, (int) Thumbs.ThumbResolution, iRotate, Thumbs.SpeedThumbsSmall))
        {  }
      }

      if (!noLargeThumbnails)
      {
        thumbnailImage = String.Format(@"{0}\{1}L.jpg", Thumbs.Pictures, MediaPortal.Util.Utils.EncryptLine(file));
        //if (recreateThumbs || !File.Exists(thumbnailImage))
        if (!File.Exists(thumbnailImage))
        {
          if (MediaPortal.Util.Picture.CreateThumbnail(file, thumbnailImage, (int) Thumbs.ThumbLargeResolution, (int) Thumbs.ThumbLargeResolution, iRotate, Thumbs.SpeedThumbsLarge))
          {  }
        }
      }
    }

    /// <summary>
    /// Returns latest added Pictures db.
    /// </summary>
    /// <param name="type">Type of data to fetch</param>
    /// <returns>Resultset of matching data</returns>
    private LatestsCollection GetLatestPictures()
    {
      latestPictures = new LatestsCollection();
      latestPicturesFiles = new Hashtable();

      int x = 0;
      try
      {
        Utils.HasNewPictures = false;

        string sqlQuery = "select strFile, strDateTaken from picture where strFile not like '%kindgirls%' order by strDateTaken desc limit "+Utils.FacadeMaxNum+";";
        SQLiteResultSet resultSet = dbClient.Execute(sqlQuery);
        if (resultSet != null)
        {
          if (resultSet.Rows.Count > 0)
          {
            int i0 = 1;
            for (int i = 0; i < resultSet.Rows.Count; i++)
            {
              string tmpThumb = resultSet.GetField(i, 0);
              string thumb = String.Format(@"{0}\{1}L.jpg", Thumbs.Pictures, MediaPortal.Util.Utils.EncryptLine(tmpThumb));
              if (!File.Exists(thumb))
              {
                thumb = String.Format(@"{0}\{1}.jpg", Thumbs.Pictures, MediaPortal.Util.Utils.EncryptLine(tmpThumb));
                if (!File.Exists(thumb))
                  thumb = tmpThumb;
              }

              string dateAdded = resultSet.GetField(i, 1);
              bool isnew = false;
              try
              {
                DateTime dTmp = DateTime.Parse(dateAdded);
                dateAdded = String.Format("{0:" + LatestMediaHandlerSetup.DateFormat + "}", dTmp);

                isnew = (dTmp > Utils.NewDateTime);
                if (isnew)
                  Utils.HasNewPictures = true;
              }
              catch {   }

              string title = Utils.GetFilenameNoPath(tmpThumb).ToUpperInvariant();
              if (thumb != null && thumb.Trim().Length > 0)
              {
                if (File.Exists(thumb))
                {
                  latestPictures.Add(new LatestMediaHandler.Latest(dateAdded, thumb, tmpThumb, title, 
                                                                   null, null, null, null, null, null, 
                                                                   null, null, null, null, null, null, 
                                                                   null, null, null, null,
                                                                   isnew));
                  latestPicturesFiles.Add(i0, tmpThumb);
                  Utils.ThreadToSleep();

                  x++;
                  i0++;
                }
              }
              if (x == Utils.FacadeMaxNum)
                break;
            }
          }
        }
        resultSet = null;
      }
      catch (Exception ex)
      {
        logger.Error("GetLatestPictures: " + ex.ToString());
      }
      return latestPictures;
    }

    private void AddToFilmstrip(Latest latests, int x)
    {
      try
      {
        //Add to filmstrip
        Utils.LoadImage(latests.Thumb, ref imagesThumbs);

        GUIListItem item = new GUIListItem();
        item.ItemId = x;
        item.IconImage = latests.Thumb;
        item.IconImageBig = latests.Thumb;
        item.ThumbnailImage = latests.Thumb;
        item.Label = latests.Title;
        item.Label2 = latests.DateAdded;
        item.IsFolder = false;
        item.DVDLabel = latests.Fanart;
        item.OnItemSelected += new GUIListItem.ItemSelectedHandler(item_OnItemSelected);

        al.Add(item);
      }
      catch (Exception ex)
      {
        logger.Error("AddToFilmstrip: " + ex.ToString());
      }
    }

    internal void LatestsToFilmStrip(LatestsCollection lTable)
    {
      if (lTable != null)
      {
        if (al != null)
          al.Clear();

        for (int i = 0; i < lTable.Count; i++)
          AddToFilmstrip(lTable[i], i+1);
      }
    }

    internal void InitFacade(bool OnActivate = false)
    {
      try
      {
        LatestsToFilmStrip(latestPictures);

        facade = Utils.GetLatestsFacade(ControlID);
        if (facade != null)
        {
          Utils.ClearFacade(ref facade);
          if (al != null)
          {
            int selected = ((LastFocusedId <= 0) || (LastFocusedId > al.Count)) ? 1 : LastFocusedId;
            for (int i = 0; i < al.Count; i++)
            {
              GUIListItem _gc = al[i] as GUIListItem;
              Utils.LoadImage(_gc.IconImage, ref imagesThumbs);
              facade.Add(_gc);
              if ((i+1) == selected)
                UpdateSelectedProperties(_gc);
            }
          }
          Utils.UpdateFacade(ref facade, LastFocusedId);
          if (OnActivate)
            facade.Visible = false;
        }
      }
      catch (Exception ex)
      {
        logger.Error("InitFacade: " + ex.ToString());
      }
    }

    internal void DeInitFacade()
    {
      try
      {
        facade = Utils.GetLatestsFacade(ControlID);
        if (facade != null)
        {
          facade.Clear();
          Utils.UnLoadImage(ref images);
          Utils.UnLoadImage(ref imagesThumbs);
        }
      }
      catch (Exception ex)
      {
        logger.Error("DeInitFacade: " + ex.ToString());
      }
    }

    private void UpdateSelectedProperties(GUIListItem item)
    {
      try
      {
        if (item != null && selectedFacadeItem1 != item.ItemId)
        {
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.picture.selected.thumb", item.IconImageBig);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.picture.selected.title", item.Label);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.picture.selected.dateAdded", item.Label2);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.picture.selected.filename", item.Label);
          selectedFacadeItem1 = item.ItemId;

          facade = Utils.GetLatestsFacade(ControlID);
          if (facade != null)
            lastFocusedId = facade.SelectedListItemIndex;
        }
      }
      catch (Exception ex)
      {
        logger.Error("UpdateSelectedProperties: " + ex.ToString());
      }
    }

    internal void UpdateSelectedImageProperties()
    {
      try
      {
        facade = Utils.GetLatestsFacade(ControlID);
        if (facade != null && facade.Focus && facade.SelectedListItem != null)
        {
          int _id = facade.SelectedListItem.ItemId;
          String _image = facade.SelectedListItem.DVDLabel;
          if (selectedFacadeItem2 != _id)
          {
            Utils.LoadImage(_image, ref images);
            if (showFanart == 1)
            {
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.picture.selected.fanart1", _image);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.picture.selected.showfanart1", "true");
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.picture.selected.showfanart2", "false");
              Thread.Sleep(1000);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.picture.selected.fanart2", "");
              showFanart = 2;
            }
            else
            {
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.picture.selected.fanart2", _image);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.picture.selected.showfanart2", "true");
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.picture.selected.showfanart1", "false");
              Thread.Sleep(1000);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.picture.selected.fanart1", "");
              showFanart = 1;
            }
            Utils.UnLoadImage(_image, ref images);
            selectedFacadeItem2 = _id;
          }
        }
        else
        {
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.picture.selected.fanart1", " ");
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.picture.selected.fanart2", " ");
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.picture.selected.showfanart1", "false");
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.picture.selected.showfanart2", "false");
          Utils.UnLoadImage(ref images);
          showFanart = 1;
          selectedFacadeItem2 = -1;
          selectedFacadeItem2 = -1;
        }
      }
      catch (Exception ex)
      {
        logger.Error("UpdateSelectedImageProperties: " + ex.ToString());
      }
    }
 
    internal void SetupPicturesLatest()
    {
      try
      {
        GUIWindowManager.Receivers += new SendMessageHandler(OnMessage);
      }
      catch (Exception ex)
      {
        logger.Error("SetupPicturesLatest: " + ex.ToString());
      }
    }

    internal void DisposePicturesLatest()
    {
      try
      {
        GUIWindowManager.Receivers -= new SendMessageHandler(OnMessage);
      }
      catch (Exception ex)
      {
        logger.Error("DisposePicturesLatest: " + ex.ToString());
      }
    }

    private void OnMessage(GUIMessage message)
    {
      bool Update = false;
      if (LatestMediaHandlerSetup.LatestPictures.Equals("True", StringComparison.CurrentCulture))
      {
        try
        {
          switch (message.Message)
          {
            case GUIMessage.MessageType.GUI_MSG_VIDEOINFO_REFRESH:
            {
              logger.Debug("VideoInfo refresh detected: Refreshing Latest.");
              Update = true;
              break;
            }
          case GUIMessage.MessageType.GUI_MSG_PLAYBACK_ENDED:
          case GUIMessage.MessageType.GUI_MSG_PLAYBACK_STOPPED:
            {
              logger.Debug("Playback End/Stop detected: Refreshing latest.");
              Update = true;
              break;
            }
            case GUIMessage.MessageType.GUI_MSG_START_SLIDESHOW:
            {
              logger.Debug("Slideshow detected: Refreshing latest.");
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
            GetLatestMediaInfoThread();
          }
          catch (Exception ex)
          {
            logger.Error("GUIWindowManager_OnNewMessage: " + ex.ToString());
          }
        }
      }
    }

    private void item_OnItemSelected(GUIListItem item, GUIControl parent)
    {
      UpdateSelectedProperties(item);
    }

    internal void UpdateImageTimer(GUIWindow fWindow, Object stateInfo, ElapsedEventArgs e)
    {
      if (LatestMediaHandlerSetup.LatestPictures.Equals("True", StringComparison.CurrentCulture))
      {
        try
        {
          if (fWindow.GetFocusControlId() == ControlID)
          {
            UpdateSelectedImageProperties();
            NeedCleanup = true;
          }
          else
          {
            if (NeedCleanup && NeedCleanupCount >= 5)
            {
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.picture.selected.fanart1", " ");
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.picture.selected.fanart2", " ");
              Utils.UnLoadImage(ref images);
              ShowFanart = 1;
              SelectedFacadeItem2 = -1;
              SelectedFacadeItem2 = -1;
              NeedCleanup = false;
              NeedCleanupCount = 0;
            }
            else if (NeedCleanup && NeedCleanupCount == 0)
            {
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.picture.selected.showfanart1", "false");
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.picture.selected.showfanart2", "false");
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
          logger.Error("UpdateImageTimer (picture): " + ex.ToString());
        }
      }
    }

    internal void GetLatestMediaInfoThread()
    {
      // Pictures
      if (LatestMediaHandlerSetup.LatestPictures.Equals("True", StringComparison.CurrentCulture))
      {
        try
        {
          RefreshWorker MyRefreshWorker = new RefreshWorker();
          MyRefreshWorker.RunWorkerCompleted += MyRefreshWorker.OnRunWorkerCompleted;
          MyRefreshWorker.RunWorkerAsync(this);
        }
        catch (Exception ex)
        {
          logger.Error("GetLatestMediaInfoThread: " + ex.ToString());
        }
      }
    }

    internal void EmptyLatestMediaPropsPictures()
    {
      LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.picture.label", Translation.LabelLatestAdded);
      LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.picture.latest.enabled", "false");
      LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.picture.hasnew", "false");
      for (int z = 1; z < 4; z++)
      {
        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.picture.latest" + z + ".title", string.Empty);
        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.picture.latest" + z + ".thumb", string.Empty);
        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.picture.latest" + z + ".filename", string.Empty);
        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.picture.latest" + z + ".fanart", string.Empty);
        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.picture.latest" + z + ".dateAdded", string.Empty);
        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.picture.latest" + z + ".new", "false");
      }
    }

    internal void GetLatestMediaInfo()
    {
      int sync = Interlocked.CompareExchange(ref Utils.SyncPointPicturesUpdate, 1, 0);
      if (sync != 0)
        return;

      if (!LatestMediaHandlerSetup.LatestPictures.Equals("True", StringComparison.CurrentCulture))
      {
        EmptyLatestMediaPropsPictures();
        return;
      }

      //Pictures            
      try
      {
        if (InitDB("PictureDatabase.db3"))
        {
          LatestsCollection ht = GetLatestPictures();
          EmptyLatestMediaPropsPictures();
          if (ht != null)
          {
            int z = 1;
            for (int i = 0; i < ht.Count && i < Utils.LatestsMaxNum; i++)
            {
              logger.Info("Updating Latest Media Info: Pictures: Picture " + z + ": " + ht[i].Fanart);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.picture.latest" + z + ".title", ht[i].Title);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.picture.latest" + z + ".thumb", ht[i].Thumb);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.picture.latest" + z + ".filename", ht[i].Fanart);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.picture.latest" + z + ".fanart", ht[i].Fanart);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.picture.latest" + z + ".dateAdded", ht[i].DateAdded);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.picture.latest" + z + ".new", ht[i].New);
              z++;
            }
            // ht.Clear();
            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.picture.hasnew", Utils.HasNewPictures ? "true" : "false");
            logger.Debug("Updating Latest Media Info: Pictures: Has new: " + (Utils.HasNewPictures ? "true" : "false"));
          }
          // ht = null;
        }
        try
        {
          Close();
        }
        catch
        {   }
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
        logger.Error("GetLatestMediaInfo (Pictures): " + ex.ToString());
      }

      if ((latestPictures != null) && (latestPictures.Count > 0))
      {
        InitFacade();
        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.picture.latest.enabled", "true");
      }
      else
        EmptyLatestMediaPropsPictures();
      Utils.SyncPointPicturesUpdate=0;
    }
  }
}
