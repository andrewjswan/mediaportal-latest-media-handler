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

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Threading;
using System.Timers;
using System.Windows.Forms;

using MediaPortal.GUI.Video;
using MediaPortal.Util;
using MediaPortal.GUI.Library;
using MediaPortal.Dialogs;
using MediaPortal.Video.Database;

using RealNLog.NLog; 

namespace LatestMediaHandler
{
  internal class LatestMyVideosHandler
  {
    #region declarations

    private Logger logger = LogManager.GetCurrentClassLogger();

    private bool _isGetTypeRunningOnThisThread /* = false*/;

    private LatestsCollection latestMyVideos = null;
    private Hashtable latestMyVideosForPlay;

    private GUIFacadeControl facade = null;

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

    public const int ControlID = 919198710;
    public const int Play1ControlID = 91915991;
    public const int Play2ControlID = 91915992;
    public const int Play3ControlID = 91915993;

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

    internal bool IsGetTypeRunningOnThisThread
    {
      get { return _isGetTypeRunningOnThisThread; }
      set { _isGetTypeRunningOnThisThread = value; }
    }

    internal LatestMyVideosHandler()
    {
      ControlIDFacades = new List<int>();
      ControlIDPlays = new List<int>();
      //
      ControlIDFacades.Add(ControlID);
      ControlIDPlays.Add(Play1ControlID);
      ControlIDPlays.Add(Play2ControlID);
      ControlIDPlays.Add(Play3ControlID);
    }

    internal bool PlayMovie(GUIWindow fWindow)
    {
      try
      {
        int FocusControlID = fWindow.GetFocusControlId();
        if (ControlIDPlays.Contains(FocusControlID))
        {
          PlayMovie(ControlIDPlays.IndexOf(FocusControlID)+1);
          return true;
        }
        //
        facade = Utils.GetLatestsFacade(ControlID);
        if (facade != null && facade.Focus && facade.SelectedListItem != null)
        {
          PlayMovie(facade.SelectedListItem.ItemId);
          return true;
        }
      }
      catch (Exception ex)
      {
        logger.Error("Unable to play video! " + ex.ToString());
        return true;
      }
      return false;
    }

    internal void PlayMovie(int index)
    {
      GUIVideoFiles.Reset(); // reset pincode
      ArrayList files = new ArrayList();

      IMDBMovie movie = (IMDBMovie) latestMyVideosForPlay[index];
      VideoDatabase.GetFilesForMovie(movie.ID, ref files);

      if (files.Count > 1)
      {
        GUIVideoFiles.StackedMovieFiles = files;
        GUIVideoFiles.IsStacked = true;
      }
      else
      {
        GUIVideoFiles.IsStacked = false;
      }

      GUIVideoFiles.MovieDuration(files, false);
      GUIVideoFiles.PlayMovie(movie.ID, false);
    }

    internal void MyContextMenu()
    {
      try
      {
        GUIDialogMenu dlg = (GUIDialogMenu) GUIWindowManager.GetWindow((int) GUIWindow.Window.WINDOW_DIALOG_MENU);
        if (dlg == null) return;

        dlg.Reset();
        dlg.SetHeading(924);

        //Add Details Menu Item
        //Play Menu Item
        GUIListItem pItem = new GUIListItem(Translation.Play);
        dlg.Add(pItem);
        pItem.ItemId = 1;

        pItem = new GUIListItem(Translation.MovieDetails);
        dlg.Add(pItem);
        pItem.ItemId = 2;

        //Add Watched/Unwatched Filter Menu Item
        if (LatestMediaHandlerSetup.LatestMyVideosWatched.Equals("False", StringComparison.CurrentCulture))
        {
          pItem = new GUIListItem(Translation.ShowUnwatchedMovies);
          dlg.Add(pItem);
          pItem.ItemId = 3;
        }
        else
        {
          pItem = new GUIListItem(Translation.ShowAllMovies);
          dlg.Add(pItem);
          pItem.ItemId = 3;
        }

        pItem = new GUIListItem();
        pItem.Label = Translation.Update;
        pItem.ItemId = 4;
        dlg.Add(pItem);

        //Show Dialog
        dlg.DoModal(GUIWindowManager.ActiveWindow);

        if (dlg.SelectedLabel == -1)
          return;

        switch (dlg.SelectedId)
        {
          case 1:
          {
            PlayMovie(GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow));
            break;
          }
          case 2:
          {
            ShowInfo();
            break;
          }
          case 3:
          {
            LatestMediaHandlerSetup.LatestMyVideosWatched = (LatestMediaHandlerSetup.LatestMyVideosWatched.Equals("False", StringComparison.CurrentCulture)) ? "True" : "False" ;
            MyVideosUpdateLatest();
            break;
          }
          case 4:
          {
            MyVideosUpdateLatest();
            break;
          }
        }
      }
      catch (Exception ex)
      {
        logger.Error("MyContextMenu: " + ex.ToString());
      }
    }

    private void ShowInfo()
    {
      try
      {
        GUIWindow fWindow = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow);
        int FocusControlID = fWindow.GetFocusControlId();

        int idx = -1 ;
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
          IMDBMovie movie = (IMDBMovie) latestMyVideosForPlay[idx];

          // Open video info screen
          GUIVideoInfo videoInfo = (GUIVideoInfo) GUIWindowManager.GetWindow((int) GUIWindow.Window.WINDOW_VIDEO_INFO);
          videoInfo.Movie = movie;

          GUIWindowManager.ActivateWindow((int) GUIWindow.Window.WINDOW_VIDEO_INFO);
        }
      }
      catch (Exception ex)
      {
        logger.Error("ShowInfo: " + ex.ToString());
      }
    }

    /// <summary>
    /// Returns latest added movie thumbs from MyVideos db.
    /// </summary>
    /// <param name="type">Type of data to fetch</param>
    /// <returns>Resultset of matching data</returns>
    private LatestsCollection GetLatestMyVideos()
    {
      latestMyVideos = new LatestsCollection();
      latestMyVideosForPlay = new Hashtable();

      LatestsCollection latests = new LatestsCollection();

      int x = 0;
      string sTimestamp = string.Empty;
      try
      {
        Utils.HasNewMyVideos = false;

        ArrayList movies = new ArrayList();
        string orderClause = "order by movieinfo.dateAdded DESC limit 50";
        string fromClause  = "movie,movieinfo,path";
        string whereClause = "where movieinfo.idmovie=movie.idmovie and movie.idpath=path.idpath" + 
                                   (LatestMediaHandlerSetup.LatestMyVideosWatched.Equals("True", StringComparison.CurrentCulture) ? " and movieinfo.iswatched=0" : "");
        string sql         =  String.Format("select movieinfo.fRating,movieinfo.strCredits,movieinfo.strTagLine,movieinfo.strPlotOutline, " +
                                                   "movieinfo.strPlot,movieinfo.strPlotOutline,movieinfo.strVotes,movieinfo.strCast,movieinfo.iYear,movieinfo.strGenre,movieinfo.strPictureURL, " +
                                                   "movieinfo.strTitle,path.strPath,movie.discid,movieinfo.IMDBID,movieinfo.idMovie,path.cdlabel,movieinfo.mpaa,movieinfo.runtime, " + 
                                                   "movieinfo.iswatched, movieinfo.dateAdded from {0} {1} {2}", 
                                            fromClause, whereClause, orderClause);

        MediaPortal.Video.Database.VideoDatabase.GetMoviesByFilter(sql, out movies, false, true, false, false);

        foreach (IMDBMovie item in movies)
        {
          if (!CheckItem(item.Path))
          {
            sTimestamp = item.DateAdded;
            string titleExt = item.Title + "{" + item.ID + "}";
            string thumb = MediaPortal.Util.Utils.GetLargeCoverArtName(Thumbs.MovieTitle, titleExt); //item.ThumbURL;
            if (string.IsNullOrEmpty(thumb))
              thumb = "DefaultFolderBig.png";

            bool isnew = false;
            try
            {
              DateTime dTmp = DateTime.Parse(sTimestamp);
              isnew = ((dTmp > Utils.NewDateTime) && (item.Watched <= 0));
              if (isnew)
                Utils.HasNewMyVideos = true;
            }
            catch 
            { }

            latests.Add(new Latest(sTimestamp, thumb, GetFanart(item.Title, item.ID), item.Title, item.PlotOutline,  
                                   null, null, 
                                   item.Genre,
                                   item.Rating.ToString(CultureInfo.CurrentCulture),
                                   Math.Round(item.Rating, MidpointRounding.AwayFromZero).ToString(CultureInfo.CurrentCulture), 
                                   item.MPARating,
                                   (item.RunTime).ToString(),
                                   item.Year.ToString(CultureInfo.CurrentCulture), 
                                   null, null, null, 
                                   item, item.ID.ToString(), item.Plot, 
                                   null,
                                   isnew));

            Utils.ThreadToSleep();
            x++;
            if (x == Utils.FacadeMaxNum)
              break;
          }
        }
        if (movies != null)
          movies.Clear();
        movies = null;

        int i0 = 1;
        x = 0;
        latests.Sort(new LatestAddedComparer());
        for (int x0 = 0; x0 < latests.Count; x0++)
        {
          try
          {
            DateTime dTmp = DateTime.Parse(latests[x0].DateAdded);
            latests[x0].DateAdded = String.Format("{0:" + LatestMediaHandlerSetup.DateFormat + "}", dTmp);
          }
          catch {  }

          latestMyVideos.Add(latests[x0]);
          latestMyVideosForPlay.Add(i0, latests[x0].Playable);

          x++;
          i0++;
          if (x == Utils.FacadeMaxNum)
            break;
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
        logger.Error("GetLatestMyVideos: " + ex.ToString());
      }
      if (latests != null)
        latests.Clear();
      latests = null;

      return latestMyVideos;
    }

    private string GetFanart(string title, int id)
    {
      string _movieid = id.ToString();
      string fanart = UtilsFanartHandler.GetMyVideoFanartForLatest(_movieid);
      if (String.IsNullOrEmpty(fanart))
      {
        fanart = UtilsFanartHandler.GetMyVideoFanartForLatest(title);
      }
      return fanart;
    }

    private bool CheckItem(string path)
    {
      bool folderPinProtected = false;
      string directory = Path.GetDirectoryName(path); // item path

      if (directory != null)
      {
        VirtualDirectory vDir = new VirtualDirectory();
        // Get protected share paths for videos
        vDir.LoadSettings("movies");

        // Check if item belongs to protected shares
        string pincode;
        folderPinProtected = vDir.IsProtectedShare(directory, out pincode);
      }
      return folderPinProtected;
    }

    internal void EmptyLatestMediaPropsMyVideos()
    {
      Utils.SetProperty("#latestMediaHandler.myvideo.label", Translation.LabelLatestAdded);
      Utils.SetProperty("#latestMediaHandler.myvideo.latest.enabled", "false");
      Utils.SetProperty("#latestMediaHandler.myvideo.hasnew", "false");
      for (int z = 1; z < 4; z++)
      {
        Utils.SetProperty("#latestMediaHandler.myvideo.latest" + z + ".thumb", string.Empty);
        Utils.SetProperty("#latestMediaHandler.myvideo.latest" + z + ".fanart", string.Empty);
        Utils.SetProperty("#latestMediaHandler.myvideo.latest" + z + ".title", string.Empty);
        Utils.SetProperty("#latestMediaHandler.myvideo.latest" + z + ".dateAdded", string.Empty);
        Utils.SetProperty("#latestMediaHandler.myvideo.latest" + z + ".genre", string.Empty);
        Utils.SetProperty("#latestMediaHandler.myvideo.latest" + z + ".rating", string.Empty);
        Utils.SetProperty("#latestMediaHandler.myvideo.latest" + z + ".roundedRating", string.Empty);
        Utils.SetProperty("#latestMediaHandler.myvideo.latest" + z + ".classification", string.Empty);
        Utils.SetProperty("#latestMediaHandler.myvideo.latest" + z + ".runtime", string.Empty);
        Utils.SetProperty("#latestMediaHandler.myvideo.latest" + z + ".year", string.Empty);
        Utils.SetProperty("#latestMediaHandler.myvideo.latest" + z + ".id", string.Empty);
        Utils.SetProperty("#latestMediaHandler.myvideo.latest" + z + ".plot", string.Empty);
        Utils.SetProperty("#latestMediaHandler.myvideo.latest" + z + ".plotoutline", string.Empty);
        Utils.SetProperty("#latestMediaHandler.myvideo.latest" + z + ".new", "false");
      }
    }

    internal void MyVideosUpdateLatest()
    {
      int sync = Interlocked.CompareExchange(ref Utils.SyncPointMyVideosUpdate, 1, 0);
      if (sync != 0)
        return;

      if (!LatestMediaHandlerSetup.LatestMyVideos.Equals("True", StringComparison.CurrentCulture))
      {
        EmptyLatestMediaPropsMyVideos();
        return;
      }

      try
      {
        LatestsCollection hTable = GetLatestMyVideos();
        EmptyLatestMediaPropsMyVideos();

        if (hTable != null)
        {
          int z = 1;
          for (int i = 0; i < hTable.Count && i < Utils.LatestsMaxNum; i++)
          {
            logger.Info("Updating Latest Media Info: MyVideo: Video " + z + ": " + hTable[i].Title);

            Utils.SetProperty("#latestMediaHandler.myvideo.latest" + z + ".thumb", hTable[i].Thumb);
            Utils.SetProperty("#latestMediaHandler.myvideo.latest" + z + ".fanart", hTable[i].Fanart);
            Utils.SetProperty("#latestMediaHandler.myvideo.latest" + z + ".title", hTable[i].Title);
            Utils.SetProperty("#latestMediaHandler.myvideo.latest" + z + ".dateAdded", hTable[i].DateAdded);
            Utils.SetProperty("#latestMediaHandler.myvideo.latest" + z + ".genre", hTable[i].Genre);
            Utils.SetProperty("#latestMediaHandler.myvideo.latest" + z + ".rating", hTable[i].Rating);
            Utils.SetProperty("#latestMediaHandler.myvideo.latest" + z + ".roundedRating", hTable[i].RoundedRating);
            Utils.SetProperty("#latestMediaHandler.myvideo.latest" + z + ".classification", hTable[i].Classification);
            Utils.SetProperty("#latestMediaHandler.myvideo.latest" + z + ".runtime", hTable[i].Runtime);
            Utils.SetProperty("#latestMediaHandler.myvideo.latest" + z + ".year", hTable[i].Year);
            Utils.SetProperty("#latestMediaHandler.myvideo.latest" + z + ".id", hTable[i].Id);
            Utils.SetProperty("#latestMediaHandler.myvideo.latest" + z + ".plot", hTable[i].Summary);
            Utils.SetProperty("#latestMediaHandler.myvideo.latest" + z + ".plotoutline", hTable[i].Subtitle);
            Utils.SetProperty("#latestMediaHandler.myvideo.latest" + z + ".new", hTable[i].New);
            z++;
          }
          // hTable.Clear();
          Utils.SetProperty("#latestMediaHandler.myvideo.hasnew", Utils.HasNewMyVideos ? "true" : "false");
          logger.Debug("Updating Latest Media Info: MyVideo: Has new: " + (Utils.HasNewMyVideos ? "true" : "false"));
        }
        // hTable = null;
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
        logger.Error("MyVideosUpdateLatest: " + ex.ToString());
      }

      if ((latestMyVideos != null) && (latestMyVideos.Count > 0))
      {
        InitFacade();
        Utils.SetProperty("#latestMediaHandler.myvideo.latest.enabled", "true");
      }
      else
        EmptyLatestMediaPropsMyVideos();
      Utils.SyncPointMyVideosUpdate=0;
    }

    private void AddToFilmstrip(Latest latests, int x)
    {

      try
      {
        //Add to filmstrip
        IMDBMovie movie = new IMDBMovie();
        movie.Title = latests.Title;
        movie.File = "";
        try
        {
          movie.RunTime = Int32.Parse(latests.Runtime);
        }
        catch
        {
          movie.RunTime = 0;
        }
        try
        {
          movie.Year = Int32.Parse(latests.Year);
        }
        catch
        {
          movie.Year = 0;
        }
        movie.DVDLabel = latests.Fanart;
        try
        {
          movie.Rating = Int32.Parse(latests.RoundedRating);
        }
        catch
        {
          movie.Rating = 0;
        }
        movie.Watched = 0;

        Utils.LoadImage(latests.Thumb, ref imagesThumbs);

        GUIListItem item = new GUIListItem();
        item.ItemId = x;
        item.IconImage = latests.Thumb;
        item.IconImageBig = latests.Thumb;
        item.ThumbnailImage = latests.Thumb;
        item.Label = movie.Title;
        item.Label2 = latests.Genre;
        item.Label3 = latests.DateAdded;
        item.IsFolder = false;
        item.Path = movie.File;
        item.Duration = movie.RunTime; // *60;
        item.AlbumInfoTag = movie;
        item.Year = movie.Year;
        item.DVDLabel = movie.DVDLabel;
        item.Rating = movie.Rating;
        item.Path = latests.Summary;
        item.IsPlayed = movie.Watched > 0 ? true : false;
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
        LatestsToFilmStrip(latestMyVideos);

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
          Utils.SetProperty("#latestMediaHandler.myvideo.selected.thumb", item.IconImageBig);
          Utils.SetProperty("#latestMediaHandler.myvideo.selected.title", item.Label);
          Utils.SetProperty("#latestMediaHandler.myvideo.selected.dateAdded", item.Label3);
          Utils.SetProperty("#latestMediaHandler.myvideo.selected.genre", item.Label2);
          Utils.SetProperty("#latestMediaHandler.myvideo.selected.roundedRating", "" + item.Rating);
          Utils.SetProperty("#latestMediaHandler.myvideo.selected.classification", "");
          Utils.SetProperty("#latestMediaHandler.myvideo.selected.runtime", "" + item.Duration);
          Utils.SetProperty("#latestMediaHandler.myvideo.selected.year", "" + item.Year);
          Utils.SetProperty("#latestMediaHandler.myvideo.selected.id", "" + item.ItemId);
          Utils.SetProperty("#latestMediaHandler.myvideo.selected.plot", item.Path);
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
              Utils.SetProperty("#latestMediaHandler.myvideo.selected.fanart1", _image);
              Utils.SetProperty("#latestMediaHandler.myvideo.selected.showfanart1", "true");
              Utils.SetProperty("#latestMediaHandler.myvideo.selected.showfanart2", "false");
              Thread.Sleep(1000);
              Utils.SetProperty("#latestMediaHandler.myvideo.selected.fanart2", "");
              showFanart = 2;
            }
            else
            {
              Utils.SetProperty("#latestMediaHandler.myvideo.selected.fanart2", _image);
              Utils.SetProperty("#latestMediaHandler.myvideo.selected.showfanart2", "true");
              Utils.SetProperty("#latestMediaHandler.myvideo.selected.showfanart1", "false");
              Thread.Sleep(1000);
              Utils.SetProperty("#latestMediaHandler.myvideo.selected.fanart1", "");
              showFanart = 1;
            }
            Utils.UnLoadImage(_image, ref images);
            selectedFacadeItem2 = _id;
          }
        }
        else
        {
          Utils.SetProperty("#latestMediaHandler.myvideo.selected.fanart1", " ");
          Utils.SetProperty("#latestMediaHandler.myvideo.selected.fanart2", " ");
          Utils.SetProperty("#latestMediaHandler.myvideo.selected.showfanart1", "false");
          Utils.SetProperty("#latestMediaHandler.myvideo.selected.showfanart2", "false");
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

    private void item_OnItemSelected(GUIListItem item, GUIControl parent)
    {
      UpdateSelectedProperties(item);
    }

    internal void MyVideosUpdateLatestThread()
    {
      // MyVideo
      if (LatestMediaHandlerSetup.LatestMyVideos.Equals("True", StringComparison.CurrentCulture))
      {
        try
        {
          RefreshWorker MyRefreshWorker = new RefreshWorker();
          MyRefreshWorker.RunWorkerCompleted += MyRefreshWorker.OnRunWorkerCompleted;
          MyRefreshWorker.RunWorkerAsync(this);
        }
        catch (Exception ex)
        {
          logger.Error("MyVideosUpdateLatestThread: " + ex.ToString());
        }
      }
    }

    internal void SetupVideoLatest()
    {
      try
      {
        GUIWindowManager.Receivers += new SendMessageHandler(OnMessage);
      }
      catch (Exception ex)
      {
        logger.Error("SetupVideoLatest: " + ex.ToString());
      }
    }

    internal void DisposeVideoLatest()
    {
      try
      {
        GUIWindowManager.Receivers -= new SendMessageHandler(OnMessage);
      }
      catch (Exception ex)
      {
        logger.Error("DisposeVideoLatest: " + ex.ToString());
      }
    }

    private void OnMessage(GUIMessage message)
    {
      bool Update = false;
      if (LatestMediaHandlerSetup.LatestMyVideos.Equals("True", StringComparison.CurrentCulture))
      {
        try
        {
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
          }
        }
        catch { }
        if (Update)
        {
          try
          {
            MyVideosUpdateLatestThread() ;
          }
          catch (Exception ex)
          {
            logger.Error("GUIWindowManager_OnNewMessage: " + ex.ToString());
          }
        }
      }
    }

    internal void UpdateImageTimer(GUIWindow fWindow, Object stateInfo, ElapsedEventArgs e)
    {
      if (LatestMediaHandlerSetup.LatestMyVideos.Equals("True", StringComparison.CurrentCulture))
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
              Utils.SetProperty("#latestMediaHandler.myvideo.selected.fanart1", " ");
              Utils.SetProperty("#latestMediaHandler.myvideo.selected.fanart2", " ");
              Utils.UnLoadImage(ref images);
              ShowFanart = 1;
              SelectedFacadeItem2 = -1;
              SelectedFacadeItem2 = -1;
              NeedCleanup = false;
              NeedCleanupCount = 0;
            }
            else if (NeedCleanup && NeedCleanupCount == 0)
            {
              Utils.SetProperty("#latestMediaHandler.myvideo.selected.showfanart1", "false");
              Utils.SetProperty("#latestMediaHandler.myvideo.selected.showfanart2", "false");
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
          logger.Error("UpdateImageTimer (myvideo): " + ex.ToString());
        }
      }
    }

    private class LatestAddedComparer : IComparer<Latest>
    {
      public int Compare(Latest latest1, Latest latest2)
      {
        int returnValue = 1;
        if (latest1 is Latest && latest2 is Latest)
        {
          returnValue = latest2.DateAdded.CompareTo(latest1.DateAdded);
        }

        return returnValue;
      }
    }
  }
}