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
using MediaPortal.Dialogs;
using MediaPortal.GUI.Library;
using MediaPortal.GUI.Pictures;
using MediaPortal.Picture.Database;
using MediaPortal.Player;
using MediaPortal.Profile;
using MediaPortal.Util;

using LMHNLog.NLog;

using SQLite.NET;

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Timers;
using MediaPortal.Database;

namespace LatestMediaHandler
{
  /// <summary>
  /// Class handling all external (not Latest Media Handler db) database access.
  /// </summary>
  internal class LatestPictureHandler
  {
    #region declarations

    private static Logger logger = LogManager.GetCurrentClassLogger();

    private bool noLargeThumbnails = true;
    private VirtualDirectory virtualDirectory = new VirtualDirectory();

    private LatestsCollection latestPictures;
    internal Hashtable latestPicturesFiles;

    private ArrayList facadeCollection = new ArrayList();
    internal ArrayList images = new ArrayList();
    internal ArrayList imagesThumbs = new ArrayList();

    private int showFanart = 1;
    private bool needCleanup = false;
    private int needCleanupCount = 0;
    private int currentFacade = 0;

    private static Object lockObject = new object();

    #endregion

    public const int ControlID = 919199710;
    public const int Play1ControlID = 91919971;
    public const int Play2ControlID = 91919972;
    public const int Play3ControlID = 91919973;
    public const int Play4ControlID = 91919903;

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

    public ArrayList Images
    {
      get { return images; }
      set { images = value; }
    }

    public ArrayList FacadeCollection
    {
      get { return facadeCollection; }
      set { facadeCollection = value; }
    }

    internal LatestPictureHandler(int id = ControlID)
    {
      ControlIDFacades = new List<LatestsFacade>();
      ControlIDPlays = new List<int>();
      //
      ControlIDFacades.Add(new LatestsFacade(id, "Picture"));
      if (id == ControlID)
      {
        ControlIDPlays.Add(Play1ControlID);
        ControlIDPlays.Add(Play2ControlID);
        ControlIDPlays.Add(Play3ControlID);
        ControlIDPlays.Add(Play4ControlID);
      }

      Utils.ClearSelectedPicturesProperty(CurrentFacade);
      EmptyLatestMediaProperties();
    }

    internal LatestPictureHandler(LatestsFacade facade) : this (facade.ControlID)
    {
      ControlIDFacades[ControlIDFacades.Count - 1] = facade;
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
          if (file.ToLowerInvariant().Contains(@"folder.jpg"))
          {
            continue;
          }

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
        dlg.DoModal(Utils.ActiveWindow);

        if (dlg.SelectedLabel < 0)
        {
          return;
        }

        switch (dlg.SelectedId)
        {
          case 1:
          {
            PlayPictures(GUIWindowManager.GetWindow(Utils.ActiveWindow));
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
        GUIWindow fWindow = GUIWindowManager.GetWindow(Utils.ActiveWindow);
        int FocusControlID = fWindow.GetFocusControlId();

        int idx = -1;
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
        if (idx > 0)
        {
          if (File.Exists(GUIGraphicsContext.GetThemedSkinFile(@"\PictureExifInfo.xml")))
          {
            GUIPicureExif pictureExif = (GUIPicureExif)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_PICTURE_EXIF);
            pictureExif.Picture = latestPicturesFiles[idx].ToString();
            GUIWindowManager.ActivateWindow((int)GUIWindow.Window.WINDOW_PICTURE_EXIF);
          }
          else
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
        int FocusControlID = fWindow.GetFocusControlId();
        if (ControlIDPlays.Contains(FocusControlID))
        {
          PlayPictures(ControlIDPlays.IndexOf(FocusControlID)+1);
          return true;
        }
        //
        // CurrentFacade.Facade = Utils.GetLatestsFacade(CurrentFacade.ControlID);
        if (CurrentFacade.Facade != null && CurrentFacade.Facade.Focus && CurrentFacade.Facade.SelectedListItem != null)
        {
          PlayPictures(CurrentFacade.Facade.SelectedListItem.ItemId);
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
      if (g_Player.MediaInfo != null && g_Player.MediaInfo.HasVideo || g_Player.IsTV || g_Player.IsVideo)
      {
        g_Player.Stop();
      }

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

      string thumbnailImage = MediaPortal.Util.Utils.GetPicturesThumbPathname(file);

      if (!File.Exists(thumbnailImage))
      {
        MediaPortal.Util.Picture.CreateThumbnail(file, thumbnailImage, (int)Thumbs.ThumbResolution, (int)Thumbs.ThumbResolution, iRotate, Thumbs.SpeedThumbsSmall, false, false);
      }

      if (!noLargeThumbnails)
      {
        thumbnailImage = MediaPortal.Util.Utils.GetPicturesLargeThumbPathname(file);
        //if (recreateThumbs || !File.Exists(thumbnailImage))
        if (!File.Exists(thumbnailImage))
        {
          MediaPortal.Util.Picture.CreateThumbnail(file, thumbnailImage, (int)Thumbs.ThumbLargeResolution, (int)Thumbs.ThumbLargeResolution, iRotate, Thumbs.SpeedThumbsLarge, true, false);
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

      if (!PictureDatabase.DbHealth)
      {
        return latestPictures;
      }

      int x = 0;
      try
      {
        CurrentFacade.HasNew = false;

        string sqlQuery = "SELECT strFile, strDateTaken FROM picture" +
                                  (PictureDatabase.FilterPrivate ? " WHERE idPicture NOT IN (SELECT DISTINCT idPicture FROM picturekeywords WHERE strKeyword = 'Private')" : string.Empty) +
                                 " ORDER BY strDateTaken DESC LIMIT " + Utils.FacadeMaxNum + ";";
        List<PictureData> pictures = PictureDatabase.GetPicturesByFilter(sqlQuery, "pictures");

        if (pictures != null)
        {
          if (pictures.Count > 0)
          {
            int i0 = 1;
            for (int i = 0; i < pictures.Count; i++)
            {
              string filename = pictures[i].FileName;
              if (string.IsNullOrEmpty(filename))
              {
                continue;
              }

              // string thumb = String.Format(@"{0}\{1}L.jpg", Thumbs.Pictures, MediaPortal.Util.Utils.EncryptLine(filename));
              string thumb = MediaPortal.Util.Utils.GetPicturesLargeThumbPathname(filename);
              if (!File.Exists(thumb))
              {
                // thumb = String.Format(@"{0}\{1}.jpg", Thumbs.Pictures, MediaPortal.Util.Utils.EncryptLine(filename));
                thumb = MediaPortal.Util.Utils.GetPicturesThumbPathname(filename);
                if (!File.Exists(thumb))
                {
                  // thumb = "DefaultPictureBig.png";
                  thumb = filename;
                }
              }
              if (!File.Exists(thumb))
              {
                continue;
              }

              bool isnew = false;
              isnew = (pictures[i].DateTaken > Utils.NewDateTime);
              if (isnew)
              {
                CurrentFacade.HasNew = true;
              }

              string title = Path.GetFileNameWithoutExtension(Utils.GetFilenameNoPath(filename)).ToUpperInvariant();

              string exif = string.Empty;
              string exifoutline =string.Empty;
              if (File.Exists(filename))
              {
                using (ExifMetadata extractor = new ExifMetadata())
                {
                  ExifMetadata.Metadata metaData = extractor.GetExifMetadata(filename);

                  if (!metaData.IsEmpty())
                  {
                    exif = metaData.ToString();
                    exifoutline = metaData.ToShortString();
                  }
                }
              }

              latestPictures.Add(new Latest()
              {
                DateTimeAdded = pictures[i].DateTaken,
                Title = title,
                Subtitle = exifoutline,
                Thumb = thumb,
                Fanart = filename,
                Classification = exif,
                IsNew = isnew
              });

              latestPicturesFiles.Add(i0, filename);
              Utils.ThreadToSleep();

              x++;
              i0++;
              if (x == Utils.FacadeMaxNum)
                break;
            }
          }
        }
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

        facadeCollection.Add(item);
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
        if (facadeCollection != null)
        {
          facadeCollection.Clear();
        }

        for (int i = 0; i < lTable.Count; i++)
        {
          AddToFilmstrip(lTable[i], i + 1);
        }
      }
    }

    internal void InitFacade()
    {
      if (!Utils.LatestPictures)
      {
        return;
      }

      try
      {
        lock(lockObject)
        {
          // LatestsToFilmStrip(latestPictures);

          CurrentFacade.Facade = Utils.GetLatestsFacade(CurrentFacade.ControlID);
          if (CurrentFacade.Facade != null)
          {
            Utils.ClearFacade(ref CurrentFacade.Facade);
            if (facadeCollection != null)
            {
              for (int i = 0; i < facadeCollection.Count; i++)
              {
                GUIListItem item = facadeCollection[i] as GUIListItem;
                CurrentFacade.Facade.Add(item);
              }
            }
            Utils.UpdateFacade(ref CurrentFacade.Facade, CurrentFacade);
            UpdateSelectedProperties();
          }
        }
      }
      catch (Exception ex)
      {
        logger.Error("InitFacade: " + ex.ToString());
      }
    }

    internal void DeInitFacade()
    {
      if (!Utils.LatestPictures)
      {
        return;
      }

      try
      {
        CurrentFacade.Facade = Utils.GetLatestsFacade(CurrentFacade.ControlID, true);
        if (CurrentFacade.Facade != null)
        {
          Utils.ClearFacade(ref CurrentFacade.Facade);
          Utils.UnLoadImages(ref images);
          Utils.UnLoadImages(ref imagesThumbs);
        }
      }
      catch (Exception ex)
      {
        logger.Error("DeInitFacade: " + ex.ToString());
      }
    }

    private void UpdateSelectedProperties()
    {
      if (facadeCollection == null || facadeCollection.Count <= 0)
      {
        return;
      }

      int selected = ((CurrentFacade.FocusedID < 0) || (CurrentFacade.FocusedID >= facadeCollection.Count)) ? (CurrentFacade.LeftToRight ? 0 : facadeCollection.Count - 1) : CurrentFacade.FocusedID;
      UpdateSelectedProperties(facadeCollection[selected] as GUIListItem);
    }

    private void UpdateSelectedProperties(GUIListItem item)
    {
      try
      {
        if (item != null && CurrentFacade.SelectedItem != item.ItemId)
        {
          Utils.FillSelectedPicturesProperty(CurrentFacade, item, latestPictures[item.ItemId - 1]);
          CurrentFacade.SelectedItem = item.ItemId;

          // CurrentFacade.Facade = Utils.GetLatestsFacade(CurrentFacade.ControlID);
          if (CurrentFacade.Facade != null)
          {
            CurrentFacade.FocusedID = CurrentFacade.Facade.SelectedListItemIndex;
          }
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
        // CurrentFacade.Facade = Utils.GetLatestsFacade(CurrentFacade.ControlID);
        if (CurrentFacade.Facade != null && CurrentFacade.Facade.Focus && CurrentFacade.Facade.SelectedListItem != null)
        {
          int _id = CurrentFacade.Facade.SelectedListItem.ItemId;
          String _image = CurrentFacade.Facade.SelectedListItem.DVDLabel;
          if (CurrentFacade.SelectedImage != _id)
          {
            Utils.LoadImage(_image, ref images);
            if (showFanart == 1)
            {
              Utils.SetProperty("#latestMediaHandler.picture.selected.fanart1", _image);
              Utils.SetProperty("#latestMediaHandler.picture.selected.showfanart1", "true");
              Utils.SetProperty("#latestMediaHandler.picture.selected.showfanart2", "false");
              Thread.Sleep(1000);
              Utils.SetProperty("#latestMediaHandler.picture.selected.fanart2", string.Empty);
              showFanart = 2;
            }
            else
            {
              Utils.SetProperty("#latestMediaHandler.picture.selected.fanart2", _image);
              Utils.SetProperty("#latestMediaHandler.picture.selected.showfanart2", "true");
              Utils.SetProperty("#latestMediaHandler.picture.selected.showfanart1", "false");
              Thread.Sleep(1000);
              Utils.SetProperty("#latestMediaHandler.picture.selected.fanart1", string.Empty);
              showFanart = 1;
            }
            // Utils.UnLoadImage(_image, ref images);
            CurrentFacade.SelectedImage = _id;
          }
        }
        else
        {
          Utils.SetProperty("#latestMediaHandler.picture.selected.fanart1", " ");
          Utils.SetProperty("#latestMediaHandler.picture.selected.fanart2", " ");
          Utils.SetProperty("#latestMediaHandler.picture.selected.showfanart1", "false");
          Utils.SetProperty("#latestMediaHandler.picture.selected.showfanart2", "false");
          Utils.UnLoadImages(ref images);
          showFanart = 1;
          CurrentFacade.SelectedImage = -1;
        }
      }
      catch (Exception ex)
      {
        logger.Error("UpdateSelectedImageProperties: " + ex.ToString());
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
        logger.Error("SetupPicturesLatest: " + ex.ToString());
      }
    }

    internal void DisposeReceivers()
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
      if (Utils.LatestPictures)
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
        case GUIMessage.MessageType.GUI_MSG_VIDEOINFO_REFRESH:
        {
          logger.Debug("VideoInfo refresh detected: Refreshing latest.");
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

      if (Update)
      {
        GetLatestMediaInfoThread();
      }
    }

    private void item_OnItemSelected(GUIListItem item, GUIControl parent)
    {
      UpdateSelectedProperties(item);
    }

    internal void UpdateImageTimer(GUIWindow fWindow, Object stateInfo, ElapsedEventArgs e)
    {
      if (Utils.LatestPictures)
      {
        try
        {
          if (fWindow.GetFocusControlId() == CurrentFacade.ControlID)
          {
            UpdateSelectedImageProperties();
            NeedCleanup = true;
          }
          else
          {
            if (NeedCleanup && NeedCleanupCount >= 5)
            {
              Utils.SetProperty("#latestMediaHandler.picture.selected.fanart1", " ");
              Utils.SetProperty("#latestMediaHandler.picture.selected.fanart2", " ");
              Utils.UnLoadImages(ref images);
              ShowFanart = 1;
              CurrentFacade.SelectedImage = -1;
              NeedCleanup = false;
              NeedCleanupCount = 0;
            }
            else if (NeedCleanup && NeedCleanupCount == 0)
            {
              Utils.SetProperty("#latestMediaHandler.picture.selected.showfanart1", "false");
              Utils.SetProperty("#latestMediaHandler.picture.selected.showfanart2", "false");
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
      if (Utils.LatestPictures)
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

    internal void EmptyLatestMediaProperties()
    {
      if (!MainFacade && !CurrentFacade.AddProperties)
      {
        Utils.SetProperty("#latestMediaHandler." + CurrentFacade.Handler.ToLowerInvariant() + (!MainFacade ? ".info." + CurrentFacade.ControlID.ToString() : string.Empty) + ".latest.enabled", "false");
        return;
      }

      Utils.ClearLatestsPicturesProperty(CurrentFacade, MainFacade);
    }

    internal void GetLatestMediaInfo()
    {
      if (Interlocked.CompareExchange(ref CurrentFacade.Update, 1, 0) != 0)
        return;

      if (!Utils.LatestPictures)
      {
        EmptyLatestMediaProperties();
        CurrentFacade.Update = 0;
        return;
      }

      //Pictures            
      try
      {
        LatestsCollection ht = GetLatestPictures();
        LatestsToFilmStrip(latestPictures);

        if (MainFacade || CurrentFacade.AddProperties)
        {
          EmptyLatestMediaProperties();

          if (ht != null)
          {
            Utils.FillLatestsPicturesProperty(CurrentFacade, ht, MainFacade);
          }
        }
      }
      catch (Exception ex)
      {
        logger.Error("GetLatestMediaInfo (Pictures): " + ex.ToString());
      }

      if ((latestPictures != null) && (latestPictures.Count > 0))
      {
        InitFacade();
        Utils.SetProperty("#latestMediaHandler." + CurrentFacade.Handler.ToLowerInvariant() + (!MainFacade ? ".info." + CurrentFacade.ControlID.ToString() : string.Empty) + ".latest.enabled", "true");
      }
      else
      {
        EmptyLatestMediaProperties();
      }

      if (MainFacade)
      {
        Utils.UpdateLatestsUpdate(Utils.LatestsCategory.Pictures, DateTime.Now);
      }

      CurrentFacade.Update = 0;
    }
  }
}
