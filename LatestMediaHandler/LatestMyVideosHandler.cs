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
    //private MoviePlayer moviePlayer;
    private Hashtable latestMyVideos;
    private GUIFacadeControl facade = null;
    private ArrayList al = new ArrayList();
    internal ArrayList images = new ArrayList();
    internal ArrayList imagesThumbs = new ArrayList();
    private int selectedFacadeItem1 = -1;
    private int selectedFacadeItem2 = -1;
    private int showFanart = 1;
    private bool needCleanup = false;
    private int needCleanupCount = 0;
    private LatestMediaHandler.LatestsCollection result = null;
    private int lastFocusedId = 0;

    #endregion

    public const int ControlID = 919198710;
    public const int Play1ControlID = 91915991;
    public const int Play2ControlID = 91915992;
    public const int Play3ControlID = 91915993;

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

    internal bool PlayMovie(GUIWindow fWindow)
    {
      try
      {
        if (fWindow.GetFocusControlId() == Play1ControlID)
        {
          PlayMovie(1);
          return true;
        }
        else if (fWindow.GetFocusControlId() == Play2ControlID)
        {
          PlayMovie(2);
          return true;
        }
        else if (fWindow.GetFocusControlId() == Play3ControlID)
        {
          PlayMovie(3);
          return true;
        }
      }
      catch (Exception ex)
      {
        MessageBox.Show("Unable to play video! " + ex.ToString());
        return true;
      }
      return false;
    }

    internal void PlayMovie(int index)
    {
      GUIVideoFiles.Reset(); // reset pincode
      ArrayList files = new ArrayList();

      IMDBMovie movie = (IMDBMovie) latestMyVideos[index];
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

        //Show Dialog
        dlg.DoModal(GUIWindowManager.ActiveWindow);

        if (dlg.SelectedLabel == -1)
          return;

        switch (dlg.SelectedId)
        {
          case 1:
          {
            GUIWindow gw = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow);
            GUIControl gc = gw.GetControl(ControlID);
            facade = gc as GUIFacadeControl;
            if (facade != null)
            {
              PlayMovie(facade.SelectedListItem.ItemId);
            }
            break;
          }
          case 2:
          {
            GUIWindow gw = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow);
            GUIControl gc = gw.GetControl(ControlID);
            facade = gc as GUIFacadeControl;
            if (facade != null)
            {
              IMDBMovie movie = (IMDBMovie) latestMyVideos[facade.SelectedListItemIndex];

              // Open video info screen
              GUIVideoInfo videoInfo = (GUIVideoInfo) GUIWindowManager.GetWindow((int) GUIWindow.Window.WINDOW_VIDEO_INFO);
              videoInfo.Movie = movie;

              GUIWindowManager.ActivateWindow((int) GUIWindow.Window.WINDOW_VIDEO_INFO);
            }
            break;
          }
          case 3:
          {
            LatestMediaHandlerSetup.LatestMyVideosWatched = (LatestMediaHandlerSetup.LatestMyVideosWatched.Equals("False", StringComparison.CurrentCulture)) ? "True" : "False" ;
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

    /// <summary>
    /// Returns latest added movie thumbs from MyVideos db.
    /// </summary>
    /// <param name="type">Type of data to fetch</param>
    /// <returns>Resultset of matching data</returns>
    private LatestsCollection GetLatestMyVideos()
    {
      LatestMediaHandler.LatestsCollection resultTmp = new LatestMediaHandler.LatestsCollection();
      LatestsCollection latests = new LatestsCollection();
      int x = 0;
      string sTimestamp = string.Empty;
      try
      {
        GUIWindow gw = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow);
        GUIControl gc = gw.GetControl(ControlID);
        facade = gc as GUIFacadeControl;
        if (facade != null)
        {
          facade.Clear();
        }
        if (al != null)
        {
          al.Clear();
        }

        Utils.HasNewMyVideos = false;

        ArrayList movies = new ArrayList();
        string orderClause = "order by movieinfo.dateAdded DESC limit 50";
        string fromClause  = "movie,movieinfo,path";
        string whereClause = "where movieinfo.idmovie=movie.idmovie and movie.idpath=path.idpath" + 
                                   (LatestMediaHandlerSetup.LatestMyVideosWatched.Equals("True", StringComparison.CurrentCulture) ? " and movieinfo.iswatched=0" : "");
        string sql         =  String.Format("select movieinfo.fRating,movieinfo.strCredits,movieinfo.strTagLine,movieinfo.strPlotOutline, " +
                                                   "movieinfo.strPlot,movieinfo.strVotes,movieinfo.strCast,movieinfo.iYear,movieinfo.strGenre,movieinfo.strPictureURL, " +
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
            if (thumb == null || thumb.Length < 1)
            {
              thumb = "DefaultFolderBig.png";
            }

            latests.Add(new Latest(sTimestamp, thumb, GetFanart(item.Title, item.ID), item.Title, 
                                   null, null, null, 
                                   item.Genre.Replace("/", ","),
                                   item.Rating.ToString(CultureInfo.CurrentCulture),
                                   Math.Round(item.Rating, MidpointRounding.AwayFromZero).ToString(CultureInfo.CurrentCulture), 
                                   item.MPARating,
                                   (item.RunTime).ToString(),
                                   item.Year.ToString(CultureInfo.CurrentCulture), 
                                   null, null, null, 
                                   item, item.ID.ToString(), item.Plot, 
                                   null));
            try
            {
              DateTime dTmp = DateTime.Parse(sTimestamp);
              if (dTmp > Utils.NewDateTime)
                Utils.HasNewMyVideos = true;
            }
            catch 
            { }

            x++;
            if (x == 10)
            {
              break;
            }
          }
        }

        if (movies != null)
        {
          movies.Clear();
        }
        movies = null;

        latestMyVideos = new Hashtable();
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

          resultTmp.Add(latests[x0]);
          if (result == null || result.Count == 0)
          {
            result = resultTmp;
          }
          latestMyVideos.Add(i0, latests[x0].Playable);
          AddToFilmstrip(latests[x0], i0);
          x++;
          i0++;
          if (x == 10)
          {
            break;
          }
        }

        if (facade != null)
        {
          facade.SelectedListItemIndex = LastFocusedId;
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
          if (!facade.Focus)
          {
            facade.Visible = false;
          }
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
        if (latests != null)
        {
          latests.Clear();
        }
        latests = null;
        logger.Error("GetLatestMyVideos: " + ex.ToString());
      }

      if (latests != null)
      {
        latests.Clear();
      }
      latests = null;
      result = resultTmp;
      return result;
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

    private void AddToFilmstrip(Latest latests, int x)
    {

      try
      {
        //Add to filmstrip
        GUIListItem item = new GUIListItem();
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
        item.ItemId = x;
        Utils.LoadImage(latests.Thumb, ref imagesThumbs);
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
        if (facade != null)
        {
          facade.Add(item);
        }
        al.Add(item);
        if (x == 1)
        {
          UpdateSelectedProperties(item);
        }
      }
      catch (Exception ex)
      {
        logger.Error("AddToFilmstrip: " + ex.ToString());
      }
    }

    private void UpdateSelectedProperties(GUIListItem item)
    {
      try
      {
        if (item != null && selectedFacadeItem1 != item.ItemId)
        {
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myvideo.selected.thumb", item.IconImageBig);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myvideo.selected.title", item.Label);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myvideo.selected.dateAdded", item.Label3);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myvideo.selected.genre", item.Label2);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myvideo.selected.roundedRating", "" + item.Rating);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myvideo.selected.classification", "");
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myvideo.selected.runtime", "" + item.Duration);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myvideo.selected.year", "" + item.Year);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myvideo.selected.id", "" + item.ItemId);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myvideo.selected.plot", item.Path);
          selectedFacadeItem1 = item.ItemId;

          GUIWindow gw = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow);
          GUIControl gc = gw.GetControl(ControlID);
          facade = gc as GUIFacadeControl;
          if (facade != null)
          {
            lastFocusedId = facade.SelectedListItemIndex;
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
        GUIWindow gw = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow);
        GUIControl gc = gw.GetControl(ControlID);
        facade = gc as GUIFacadeControl;
        if (facade != null && gw.GetFocusControlId() == ControlID && facade.SelectedListItem != null)
        {
          int _id = facade.SelectedListItem.ItemId;
          String _image = facade.SelectedListItem.DVDLabel;
          if (selectedFacadeItem2 != _id)
          {
            Utils.LoadImage(_image, ref images);
            if (showFanart == 1)
            {
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myvideo.selected.fanart1", _image);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myvideo.selected.showfanart1", "true");
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myvideo.selected.showfanart2", "false");
              Thread.Sleep(1000);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myvideo.selected.fanart2", "");
              showFanart = 2;
            }
            else
            {
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myvideo.selected.fanart2", _image);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myvideo.selected.showfanart2", "true");
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myvideo.selected.showfanart1", "false");
              Thread.Sleep(1000);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myvideo.selected.fanart1", "");
              showFanart = 1;
            }
            Utils.UnLoadImage(_image, ref images);
            selectedFacadeItem2 = _id;
          }
        }
        else
        {
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myvideo.selected.fanart1", " ");
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myvideo.selected.fanart2", " ");
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myvideo.selected.showfanart1", "false");
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myvideo.selected.showfanart2", "false");
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

    internal void EmptyLatestMediaPropsMyVideos()
    {
      LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myvideo.label", Translation.LabelLatestAdded);
      LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myvideo.latest.enabled", "false");
      LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myvideo.hasnew", "false");
      for (int z = 1; z < 4; z++)
      {
        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myvideo.latest" + z + ".thumb", string.Empty);
        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myvideo.latest" + z + ".fanart", string.Empty);
        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myvideo.latest" + z + ".title", string.Empty);
        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myvideo.latest" + z + ".dateAdded", string.Empty);
        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myvideo.latest" + z + ".genre", string.Empty);
        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myvideo.latest" + z + ".rating", string.Empty);
        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myvideo.latest" + z + ".roundedRating", string.Empty);
        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myvideo.latest" + z + ".classification", string.Empty);
        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myvideo.latest" + z + ".runtime", string.Empty);
        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myvideo.latest" + z + ".year", string.Empty);
        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myvideo.latest" + z + ".id", string.Empty);
        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myvideo.latest" + z + ".plot", string.Empty);
      }
    }

    internal void MyVideosUpdateLatest()
    {
      try
      {
        if (LatestMediaHandlerSetup.LatestMyVideos.Equals("True", StringComparison.CurrentCulture))
        {
          try
          {
            GetLatestMyVideos();
          }
          catch (Exception ex)
          {
            logger.Error("MyVideosUpdateLatest: " + ex.ToString());
          }
          EmptyLatestMediaPropsMyVideos();

          if (result != null)
          {
            int z = 1;
            for (int i = 0; i < result.Count && i < 3; i++)
            {
              logger.Info("Updating Latest Media Info: Latest myvideo " + z + ": " + result[i].Title);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myvideo.latest" + z + ".thumb", result[i].Thumb);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myvideo.latest" + z + ".fanart", result[i].Fanart);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myvideo.latest" + z + ".title", result[i].Title);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myvideo.latest" + z + ".dateAdded", result[i].DateAdded);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myvideo.latest" + z + ".genre", result[i].Genre);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myvideo.latest" + z + ".rating", result[i].Rating);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myvideo.latest" + z + ".roundedRating", result[i].RoundedRating);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myvideo.latest" + z + ".classification", result[i].Classification);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myvideo.latest" + z + ".runtime", result[i].Runtime);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myvideo.latest" + z + ".year", result[i].Year);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myvideo.latest" + z + ".id", result[i].Id);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myvideo.latest" + z + ".plot", result[i].Summary);
              z++;
            }
            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myvideo.latest.enabled", "true");
            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myvideo.latest.hasnew", Utils.HasNewMyVideos ? "true" : "false");
            logger.Debug("Updating Latest Media Info: Latest myvideo has new: " + (Utils.HasNewMyVideos ? "true" : "false"));
          }
        }
        else
        {
          EmptyLatestMediaPropsMyVideos();
        }
      }
      catch (Exception ex)
      {
        EmptyLatestMediaPropsMyVideos();
        logger.Error("MyVideosUpdateLatest: " + ex.ToString());
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
      if (LatestMediaHandlerSetup.LatestMyVideos.Equals("True", StringComparison.CurrentCulture))
      {
        try
        {
          switch (message.Message)
          {
            case GUIMessage.MessageType.GUI_MSG_VIDEOINFO_REFRESH:
            {
              logger.Debug("VideoInfo refresh detected: Refreshing latest.");
              try
              {
                MyVideosUpdateLatest() ;
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
                MyVideosUpdateLatest() ;
              }
              catch (Exception ex)
              {
                logger.Error("GUIWindowManager_OnNewMessage: " + ex.ToString());
              }
              break;
            }
          }
        }
        catch { }
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
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myvideo.selected.fanart1", " ");
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myvideo.selected.fanart2", " ");
              Utils.UnLoadImage(ref images);
              ShowFanart = 1;
              SelectedFacadeItem2 = -1;
              SelectedFacadeItem2 = -1;
              NeedCleanup = false;
              NeedCleanupCount = 0;
            }
            else if (NeedCleanup && NeedCleanupCount == 0)
            {
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myvideo.selected.showfanart1", "false");
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myvideo.selected.showfanart2", "false");
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