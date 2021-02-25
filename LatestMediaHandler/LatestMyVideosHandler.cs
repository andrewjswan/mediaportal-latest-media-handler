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

using MediaPortal.Dialogs;
using MediaPortal.GUI.Library;
using MediaPortal.GUI.Video;
using MediaPortal.Util;
using MediaPortal.Video.Database;

using LMHNLog.NLog;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Timers;
using System.Linq;
using System.Threading.Tasks;

namespace LatestMediaHandler
{
  internal class LatestMyVideosHandler
  {
    #region declarations

    private Logger logger = LogManager.GetCurrentClassLogger();

    private LatestsCollection latestMyVideos = null;
    private Hashtable latestMyVideosForPlay;

    private ArrayList facadeCollection = new ArrayList();
    internal ArrayList images = new ArrayList();
    internal ArrayList imagesThumbs = new ArrayList();

    private int showFanart = 1;
    private bool needCleanup = false;
    private int needCleanupCount = 0;
    private int currentFacade = 0;

    private static Object lockObject = new object();

    #endregion

    public const int ControlID = 919198710;
    public const int Play1ControlID = 91915991;
    public const int Play2ControlID = 91915992;
    public const int Play3ControlID = 91915993;
    public const int Play4ControlID = 91919902;

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

    internal LatestMyVideosHandler(int id = ControlID)
    {
      ControlIDFacades = new List<LatestsFacade>();
      ControlIDPlays = new List<int>();
      //
      ControlIDFacades.Add(new LatestsFacade(id, "MyVideo"));
      if (id == ControlID)
      {
        ControlIDPlays.Add(Play1ControlID);
        ControlIDPlays.Add(Play2ControlID);
        ControlIDPlays.Add(Play3ControlID);
        ControlIDPlays.Add(Play4ControlID);
      }
      CurrentFacade.UnWatched = Utils.LatestMyVideosWatched;

      Utils.ClearSelectedMovieProperty(CurrentFacade);
      EmptyLatestMediaProperties();
    }

    internal LatestMyVideosHandler(LatestsFacade facade) : this (facade.ControlID)
    {
      ControlIDFacades[ControlIDFacades.Count - 1] = facade;
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
        // CurrentFacade.Facade = Utils.GetLatestsFacade(CurrentFacade.ControlID);
        if (CurrentFacade.Facade != null && CurrentFacade.Facade.Focus && CurrentFacade.Facade.SelectedListItem != null)
        {
          PlayMovie(CurrentFacade.Facade.SelectedListItem.ItemId);
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
        if (CurrentFacade.UnWatched)
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

        //Add Latests/Watched/Rated Menu Item
        pItem = new GUIListItem("[^] " + CurrentFacade.Title);
        dlg.Add(pItem);
        pItem.ItemId = 4;

        pItem = new GUIListItem();
        pItem.Label = Translation.Update;
        pItem.ItemId = 5;
        dlg.Add(pItem);

        //Show Dialog
        dlg.DoModal(Utils.ActiveWindow);

        if (dlg.SelectedLabel == -1)
          return;

        switch (dlg.SelectedId)
        {
          case 1:
          {
            PlayMovie(GUIWindowManager.GetWindow(Utils.ActiveWindow));
            break;
          }
          case 2:
          {
            ShowInfo();
            break;
          }
          case 3:
          {
            CurrentFacade.UnWatched = !CurrentFacade.UnWatched;
            MyVideosUpdateLatest();
            break;
          }
          case 4:
          {
            IDialogbox ldlg = (GUIDialogMenu)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            if (ldlg == null) return;

            ldlg.Reset();
            ldlg.SetHeading(924);

            //Add Types Menu Items
            pItem = new GUIListItem((CurrentFacade.Type == LatestsFacadeType.Latests ? "[x] " : string.Empty) + Translation.LabelLatestAdded);
            ldlg.Add(pItem);
            pItem.ItemId = 1;

            pItem = new GUIListItem((CurrentFacade.Type == LatestsFacadeType.Watched ? "[x] " : string.Empty) + Translation.LabelLatestWatched);
            ldlg.Add(pItem);
            pItem.ItemId = 2;

            pItem = new GUIListItem((CurrentFacade.Type == LatestsFacadeType.Rated ? "[x] " : string.Empty) + Translation.LabelHighestRated);
            ldlg.Add(pItem);
            pItem.ItemId = 3;

            //Show Dialog
            ldlg.DoModal(Utils.ActiveWindow);

            if (ldlg.SelectedLabel == -1)
              return;

            switch (ldlg.SelectedId - 1)
            {
              case 0:
                CurrentFacade.Type = LatestsFacadeType.Latests;
                break;
              case 1:
                CurrentFacade.Type = LatestsFacadeType.Watched;
                break;
              case 2:
                CurrentFacade.Type = LatestsFacadeType.Rated;
                break;
            }
            MyVideosUpdateLatest();
            break;
          }
          case 5:
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
        GUIWindow fWindow = GUIWindowManager.GetWindow(Utils.ActiveWindow);
        int FocusControlID = fWindow.GetFocusControlId();

        int idx = -1 ;
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

      try
      {
        CurrentFacade.HasNew = false;

        ArrayList movies = new ArrayList();
        string orderClause = "order by movieinfo.";
        switch (CurrentFacade.Type)
        {
          case LatestsFacadeType.Rated:
            orderClause = orderClause + "fRating";
            break;
          case LatestsFacadeType.Watched:
            orderClause = orderClause + "dateWatched";
            break;
          default:
            orderClause = orderClause + "dateAdded";
            break;
        }
        orderClause = orderClause + " DESC limit 50";

        string fromClause  = "movie,movieinfo,path";
        string whereClause = "where movieinfo.idmovie=movie.idmovie and movie.idpath=path.idpath";
        if (CurrentFacade.Type == LatestsFacadeType.Watched)
        {
          whereClause = whereClause + " and movieinfo.iswatched=1";
        }
        else if (CurrentFacade.UnWatched)
        {
          whereClause = whereClause + " and movieinfo.iswatched=0";
        }
        string sql = String.Format("select movieinfo.fRating,movieinfo.strCredits,movieinfo.strTagLine,movieinfo.strPlotOutline, " +
                                          "movieinfo.strPlot,movieinfo.strPlotOutline,movieinfo.strVotes,movieinfo.strCast,movieinfo.iYear,movieinfo.strGenre,movieinfo.strPictureURL, " +
                                          "movieinfo.strTitle,path.strPath,movie.discid,movieinfo.IMDBID,movieinfo.idMovie,path.cdlabel,movieinfo.mpaa,movieinfo.runtime, " +
                                          "movieinfo.iswatched, movieinfo.dateAdded,movieinfo.dateWatched,movieinfo.studios from {0} {1} {2}", 
                                           fromClause, whereClause, orderClause);

        VideoDatabase.GetMoviesByFilter(sql, out movies, false, true, false, false);

        int x = 0;
        foreach (IMDBMovie item in movies)
        {
          if (item.IsEmpty)
          {
            continue;
          }

          if (!CheckItem(item.Path))
          {
            DateTime dTmp = DateTime.MinValue;
            DateTime dwTmp = DateTime.MinValue;
            string titleExt = item.Title + "{" + item.ID + "}";
            string thumb = MediaPortal.Util.Utils.GetLargeCoverArtName(Thumbs.MovieTitle, titleExt); //item.ThumbURL;
            if (string.IsNullOrEmpty(thumb))
            {
              thumb = "DefaultVideoBig.png"; // "DefaultFolderBig.png";
            }
            bool isnew = false;
            try
            {
              dTmp = DateTime.Parse(item.DateAdded);
              isnew = ((dTmp > Utils.NewDateTime) && (item.Watched <= 0));
              if (isnew)
              {
                CurrentFacade.HasNew = true;
              }
            }
            catch 
            {
              isnew = false;
            }
            try
            {
              dwTmp = DateTime.Parse(item.DateWatched);
            }
            catch
            { }

            string fbanner = string.Empty;
            string fclearart = string.Empty;
            string fclearlogo = string.Empty;
            string fcd = string.Empty;
            string aposter = string.Empty;
            string abg = string.Empty;

            if (Utils.FanartHandler)
            {
              Parallel.Invoke
              (
                () => fbanner = UtilsFanartHandler.GetFanartTVForLatestMedia(item.IMDBNumber, string.Empty, string.Empty, Utils.FanartTV.MoviesBanner),
                () => fclearart = UtilsFanartHandler.GetFanartTVForLatestMedia(item.IMDBNumber, string.Empty, string.Empty, Utils.FanartTV.MoviesClearArt),
                () => fclearlogo = UtilsFanartHandler.GetFanartTVForLatestMedia(item.IMDBNumber, string.Empty, string.Empty, Utils.FanartTV.MoviesClearLogo),
                () => fcd = UtilsFanartHandler.GetFanartTVForLatestMedia(item.IMDBNumber, string.Empty, string.Empty, Utils.FanartTV.MoviesCDArt),
                () => aposter = UtilsFanartHandler.GetAnimatedForLatestMedia(item.IMDBNumber, string.Empty, string.Empty, Utils.Animated.MoviesPoster),
                () => abg = UtilsFanartHandler.GetAnimatedForLatestMedia(item.IMDBNumber, string.Empty, string.Empty, Utils.Animated.MoviesBackground)
              );
            }

            latests.Add(new Latest()
            {
              DateTimeAdded = dTmp,
              DateTimeWatched = dwTmp,
              Title = item.Title,
              Subtitle = item.PlotOutline,
              Genre = item.Genre,
              Thumb = thumb,
              Fanart = GetFanart(item.Title, item.ID),
              Rating = item.Rating.ToString(CultureInfo.CurrentCulture),
              Classification = item.MPARating,
              Runtime = item.RunTime.ToString(),
              Year = item.Year.ToString(),
              Summary = item.Plot,
              Studios = item.Studios,
              Banner = fbanner,
              ClearArt = fclearart,
              ClearLogo = fclearlogo,
              CD = fcd,
              AnimatedPoster = aposter,
              AnimatedBackground = abg,
              Playable = item,
              Id = item.ID.ToString(),
              DBId = item.IMDBNumber,
              IsNew = isnew
            });

            Utils.ThreadToSleep();
            x++;
            if (x == Utils.FacadeMaxNum)
              break;
          }
        }
        if (movies != null)
        {
          movies.Clear();
        }
        movies = null;

        Utils.SortLatests(ref latests, CurrentFacade.Type, CurrentFacade.LeftToRight);

        for (int x0 = 0; x0 < latests.Count; x0++)
        {
          latestMyVideos.Add(latests[x0]);
          latestMyVideosForPlay.Add(x0+1, latests[x0].Playable);
        }
      }
      catch (Exception ex)
      {
        logger.Error("GetLatestMyVideos: " + ex.ToString());
      }
      if (latests != null)
      {
        latests.Clear();
      }
      latests = null;

      if (latestMyVideos != null && !MainFacade)
      {
        logger.Debug("GetLatest: " + this.ToString() + ":" + CurrentFacade.ControlID + " - " + latestMyVideos.Count);
      }

      return latestMyVideos;
    }

    public Hashtable GetLatestsList()
    {
      Hashtable ht = new Hashtable();
      if (latestMyVideos != null)
      {
        for (int i = 0; i < latestMyVideos.Count; i++)
        {
          if (!ht.Contains(latestMyVideos[i].Id))
          {
            // logger.Debug("Make Latest List: MyVideo: " + latestMyVideos[i].Id + " - " + latestMyVideos[i].Title);
            ht.Add(latestMyVideos[i].Id, latestMyVideos[i].Title) ;
          }
        }
      }
      return ht;
    }

    public List<MQTTItem> GetMQTTLatestsList()
    {
      List<MQTTItem> ht = new List<MQTTItem>();
      if (latestMyVideos != null)
      {
        for (int i = 0; i < latestMyVideos.Count; i++)
        {
          ht.Add(new MQTTItem(latestMyVideos[i]));
        }
      }
      return ht;
    }

    private string GetFanart(string title, int id)
    {
      string fanart = string.Empty;

      if (Utils.FanartHandler)
      {
        string _movieid = id.ToString();
        fanart = UtilsFanartHandler.GetMyVideoFanartForLatest(_movieid);
        if (String.IsNullOrEmpty(fanart))
        {
          fanart = UtilsFanartHandler.GetMyVideoFanartForLatest(title);
        }
      }

      if (String.IsNullOrEmpty(fanart))
      {
        for (int i = 0; i < 3; i++)
        {
          string fanartFilename = FanArt.SetFanArtFileName(id, i);
          if (!string.IsNullOrEmpty(fanartFilename) && File.Exists(fanartFilename))
          {
            fanart = fanartFilename;
            break; 
          }
        }
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

    internal void EmptyLatestMediaProperties()
    {
      if (!MainFacade && !CurrentFacade.AddProperties)
      {
        Utils.SetProperty("#latestMediaHandler." + CurrentFacade.Handler.ToLowerInvariant() + (!MainFacade ? ".info." + CurrentFacade.ControlID.ToString() : string.Empty) + ".latest.enabled", "false");
        return;
      }

      Utils.ClearLatestsMovieProperty(CurrentFacade, MainFacade);
    }

    internal void MyVideosUpdateLatest()
    {
      if (Interlocked.CompareExchange(ref CurrentFacade.Update, 1, 0) != 0)
        return;

      if (!Utils.LatestMyVideos)
      {
        EmptyLatestMediaProperties();
        CurrentFacade.Update = 0;
        return;
      }

      //MyVideo
      LatestsCollection hTable = GetLatestMyVideos();
      LatestsToFilmStrip(latestMyVideos);

      if (MainFacade || CurrentFacade.AddProperties)
      {
        EmptyLatestMediaProperties();
        Utils.FillLatestsMovieProperty(CurrentFacade, hTable, MainFacade);
      }

      if ((latestMyVideos != null) && (latestMyVideos.Count > 0))
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
        Utils.UpdateLatestsUpdate(Utils.LatestsCategory.Movies, DateTime.Now);
      }

      CurrentFacade.Update = 0;
    }

    private void AddToFilmstrip(Latest latests, int x)
    {

      try
      {
        //Add to filmstrip
        IMDBMovie movie = new IMDBMovie();
        movie.Title = latests.Title;
        movie.File = string.Empty;
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
        movie.Rating = latests.RoundedRating;
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
      if (!Utils.LatestMusic)
      {
        return;
      }

      try
      {
        lock (lockObject)
        {
          // LatestsToFilmStrip(latestMyVideos);

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
      if (!Utils.LatestMusic)
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
          Utils.FillSelectedMovieProperty(CurrentFacade, item, latestMyVideos[item.ItemId - 1]);

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
              Utils.SetProperty("#latestMediaHandler.myvideo.selected.fanart1", _image);
              Utils.SetProperty("#latestMediaHandler.myvideo.selected.showfanart1", "true");
              Utils.SetProperty("#latestMediaHandler.myvideo.selected.showfanart2", "false");
              Thread.Sleep(1000);
              Utils.SetProperty("#latestMediaHandler.myvideo.selected.fanart2", string.Empty);
              showFanart = 2;
            }
            else
            {
              Utils.SetProperty("#latestMediaHandler.myvideo.selected.fanart2", _image);
              Utils.SetProperty("#latestMediaHandler.myvideo.selected.showfanart2", "true");
              Utils.SetProperty("#latestMediaHandler.myvideo.selected.showfanart1", "false");
              Thread.Sleep(1000);
              Utils.SetProperty("#latestMediaHandler.myvideo.selected.fanart1", string.Empty);
              showFanart = 1;
            }
            // Utils.UnLoadImage(_image, ref images);
            CurrentFacade.SelectedImage = _id;
          }
        }
        else
        {
          Utils.SetProperty("#latestMediaHandler.myvideo.selected.fanart1", " ");
          Utils.SetProperty("#latestMediaHandler.myvideo.selected.fanart2", " ");
          Utils.SetProperty("#latestMediaHandler.myvideo.selected.showfanart1", "false");
          Utils.SetProperty("#latestMediaHandler.myvideo.selected.showfanart2", "false");
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

    private void item_OnItemSelected(GUIListItem item, GUIControl parent)
    {
      UpdateSelectedProperties(item);
    }

    internal void GetLatestMediaInfoThread()
    {
      // MyVideo
      if (Utils.LatestMyVideos)
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

    internal void SetupReceivers()
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

    internal void DisposeReceivers()
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
      if (Utils.LatestMyVideos)
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
          logger.Debug("VideoInfo refresh detected: Refreshing latest.");
          Update = true;
          break;
        case GUIMessage.MessageType.GUI_MSG_PLAYBACK_ENDED:
        case GUIMessage.MessageType.GUI_MSG_PLAYBACK_STOPPED:
          logger.Debug("Playback End/Stop detected: Refreshing latest.");
          Update = true;
          break;
        case GUIMessage.MessageType.GUI_MSG_SETFOCUS:
          if (ControlIDFacades.Any(facade => facade.ControlID == message.TargetControlId))
          {
            logger.Debug("Focus (MyVideo) - {0}", message.TargetControlId);
          }
          break;
      }

      if (Update)
      {
        GetLatestMediaInfoThread();
      }
    }

    internal void UpdateImageTimer(GUIWindow fWindow, Object stateInfo, ElapsedEventArgs e)
    {
      if (Utils.LatestMyVideos)
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
              Utils.SetProperty("#latestMediaHandler.myvideo.selected.fanart1", " ");
              Utils.SetProperty("#latestMediaHandler.myvideo.selected.fanart2", " ");
              Utils.UnLoadImages(ref images);
              ShowFanart = 1;
              CurrentFacade.SelectedImage = -1;
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
          logger.Error("UpdateImageTimer (MyVideo): " + ex.ToString());
        }
      }
    }
  }
}