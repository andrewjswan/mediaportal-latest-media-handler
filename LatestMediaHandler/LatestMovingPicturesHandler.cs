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
extern alias RealCornerstone;

using LMHNLog.NLog;

using MediaPortal.Dialogs;
using MediaPortal.GUI.Library;
using MediaPortal.Plugins.MovingPictures;
using MediaPortal.Plugins.MovingPictures.Database;
using MediaPortal.Plugins.MovingPictures.MainUI;
using MediaPortal.Video.Database;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace LatestMediaHandler
{
  internal class LatestMovingPicturesHandler
  {
    #region declarations

    private Logger logger = LogManager.GetCurrentClassLogger();
    private MoviePlayer moviePlayer;

    private LatestsCollection latestMovies = null;
    private Hashtable latestMovingPictures;

    private ArrayList facadeCollection = new ArrayList();
    internal ArrayList images = new ArrayList();
    internal ArrayList imagesThumbs = new ArrayList();

    private int showFanart = 1;
    private bool needCleanup = false;
    private int needCleanupCount = 0;
    private int currentFacade = 0;

    private static Object lockObject = new object();
    #endregion

    public const int ControlID = 919199910;
    public const int Play1ControlID = 91919991;
    public const int Play2ControlID = 91919992;
    public const int Play3ControlID = 91919993;
    public const int Play4ControlID = 91919905;

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

    internal bool Restricted // MovingPicture restricted property
    {
      get { return MovingPictureIsRestricted(); }
    }

    internal LatestMovingPicturesHandler(int id = ControlID)
    {
      ControlIDFacades = new List<LatestsFacade>();
      ControlIDPlays = new List<int>();
      //
      ControlIDFacades.Add(new LatestsFacade(id, "MovingPicture"));
      if (id == ControlID)
      {
        ControlIDPlays.Add(Play1ControlID);
        ControlIDPlays.Add(Play2ControlID);
        ControlIDPlays.Add(Play3ControlID);
        ControlIDPlays.Add(Play4ControlID);
      }
      CurrentFacade.UnWatched = Utils.LatestMovingPicturesWatched;

      Utils.ClearSelectedMovieProperty(CurrentFacade);
      EmptyLatestMediaProperties();
    }

    internal LatestMovingPicturesHandler(LatestsFacade facade) : this (facade.ControlID)
    {
      ControlIDFacades[ControlIDFacades.Count - 1] = facade;
    }

    internal bool MovingPictureIsRestricted()
    {
      try
      {
        return MovingPicturesCore.Settings.ParentalControlsEnabled;
      }
      catch { }
      return false;
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

        // CurrentFacade.Facade = Utils.GetLatestsFacade(CurrentFacade.ControlID);
        switch (dlg.SelectedId)
        {
          case 1:
          {
            PlayMovingPicture(GUIWindowManager.GetWindow(Utils.ActiveWindow));
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
            MovingPictureUpdateLatest();
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
            MovingPictureUpdateLatest();
            break;
          }
          case 5:
          {
            MovingPictureUpdateLatest();
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
          idx = ControlIDPlays.IndexOf(FocusControlID);
        }
        //
        // CurrentFacade.Facade = Utils.GetLatestsFacade(CurrentFacade.ControlID);
        if (CurrentFacade.Facade != null && CurrentFacade.Facade.Focus && CurrentFacade.Facade.SelectedListItem != null)
        {
          idx = CurrentFacade.Facade.SelectedListItem.ItemId-1;
        }
        //
        if (idx >= 0)
        {
          string sHyp = "movieid:" + latestMovies[idx].Id;
          GUIWindowManager.ActivateWindow(96742, sHyp, false);
        }
      }
      catch (Exception ex)
      {
        logger.Error("ShowInfo: " + ex.ToString());
      }
    }

    /// <summary>
    /// Returns latest added movie thumbs from MovingPictures db.
    /// </summary>
    /// <param name="type">Type of data to fetch</param>
    /// <returns>Resultset of matching data</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private LatestsCollection GetLatestMovingPictures()
    {
      latestMovies = new LatestsCollection();
      latestMovingPictures = new Hashtable();

      LatestsCollection latests = new LatestsCollection();

      try
      {
        List<DBMovieInfo> vMovies = null;
        if (Restricted)
        {
          vMovies = MovingPicturesCore.Settings.ParentalControlsFilter.Filter(DBMovieInfo.GetAll()).ToList();
        }
        else
        {
          vMovies = DBMovieInfo.GetAll();
        }

        foreach (var item in vMovies)
        {
          if (CurrentFacade.Type == LatestsFacadeType.Watched)
          {
            if (item.UserSettings[0].WatchedCount == 0)
            {
              continue;
            }
          }
          else if (CurrentFacade.UnWatched && (item.UserSettings[0].WatchedCount > 0))
          {
            continue;
          }

          if (CurrentFacade.Type == LatestsFacadeType.Rated)
          {
            if (item.Score == 0.0)
            {
              continue;
            }
          }

          string fanart = item.CoverThumbFullPath;
          if (string.IsNullOrEmpty(fanart))
          {
            fanart = "DefaultVideoBig.png"; // "DefaultFolderBig.png";
          }
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
              () => fbanner = UtilsFanartHandler.GetFanartTVForLatestMedia(item.ImdbID, string.Empty, string.Empty, Utils.FanartTV.MoviesBanner),
              () => fclearart = UtilsFanartHandler.GetFanartTVForLatestMedia(item.ImdbID, string.Empty, string.Empty, Utils.FanartTV.MoviesClearArt),
              () => fclearlogo = UtilsFanartHandler.GetFanartTVForLatestMedia(item.ImdbID, string.Empty, string.Empty, Utils.FanartTV.MoviesClearLogo),
              () => fcd = UtilsFanartHandler.GetFanartTVForLatestMedia(item.ImdbID, string.Empty, string.Empty, Utils.FanartTV.MoviesCDArt),
              () => aposter = UtilsFanartHandler.GetAnimatedForLatestMedia(item.ImdbID, string.Empty, string.Empty, Utils.Animated.MoviesPoster),
              () => abg = UtilsFanartHandler.GetAnimatedForLatestMedia(item.ImdbID, string.Empty, string.Empty, Utils.Animated.MoviesBackground)
            );
          }

          latests.Add(new Latest()
          {
            DateTimeAdded = item.DateAdded,
            DateTimeWatched = item.WatchedHistory.Count > 0 ? item.WatchedHistory[item.WatchedHistory.Count - 1].DateWatched : DateTime.MinValue,
            Title = item.Title,
            Thumb = fanart,
            Fanart = item.BackdropFullPath,
            Genre = item.Genres.ToPrettyString(2),
            Rating = item.Score.ToString(CultureInfo.CurrentCulture),
            Classification = item.Certification,
            Runtime = GetMovieRuntime(item),
            Year = item.Year.ToString(CultureInfo.CurrentCulture),
            Summary = item.Summary,
            Banner = fbanner,
            ClearArt = fclearart,
            ClearLogo = fclearlogo,
            CD = fcd,
            AnimatedPoster = aposter,
            AnimatedBackground = abg,
            Playable = item,
            Id = item.ID.ToString(),
            DBId = item.ImdbID
          });

          Utils.ThreadToSleep();
        }
        if (vMovies != null)
        {
          vMovies.Clear();
        }
        vMovies = null;

        CurrentFacade.HasNew = false;
        Utils.SortLatests(ref latests, CurrentFacade.Type, CurrentFacade.LeftToRight);

        int x = 0;
        for (int x0 = 0; x0 < latests.Count; x0++)
        {
          DBMovieInfo Movie = (DBMovieInfo)latests[x0].Playable ;
          latests[x0].IsNew = ((latests[x0].DateTimeAdded > Utils.NewDateTime) && (Movie.UserSettings[0].WatchedCount <= 0));
          if (latests[x0].IsNew)
          {
            CurrentFacade.HasNew = true;
          }

          latestMovies.Add(latests[x0]);
          latestMovingPictures.Add(x0+1, latests[x0].Playable);
          Utils.ThreadToSleep();

          // logger.Debug("*** Latest [{7}] {0}:{1}:{2} - {3} - {4} {5} - {6}", x, CurrentFacade.Type, CurrentFacade.LeftToRight, latests[x0].Title, latests[x0].DateAdded, latests[x0].DateWatched, latests[x0].Rating, CurrentFacade.ControlID);
          x++;
          if (x == Utils.FacadeMaxNum)
            break;
        }
      }
      catch (Exception ex)
      {
        logger.Error("GetLatestMovingPictures: " + ex.ToString());
      }

      if (latests != null)
      {
        latests.Clear();
      }
      latests = null;

      if (latestMovies != null && !MainFacade)
      {
        logger.Debug("GetLatest: " + this.ToString() + ":" + CurrentFacade.ControlID + " - " + latestMovies.Count);
      }

      return latestMovies;
    }

    public Hashtable GetLatestsList()
    {
      Hashtable ht = new Hashtable();
      if (latestMovies != null)
      {
        for (int i = 0; i < latestMovies.Count; i++)
        {
          if (!ht.Contains(latestMovies[i].Id))
          {
            // logger.Debug("Make Latest List: MovingPictures: " + latestMovies[i].Id + " - " + latestMovies[i].Title);
            ht.Add(latestMovies[i].Id, latestMovies[i].Title) ;
          }
        }
      }
      return ht;
    }

    public List<MQTTItem> GetMQTTLatestsList()
    {
      List<MQTTItem> ht = new List<MQTTItem>();
      if (latestMovies != null)
      {
        for (int i = 0; i < latestMovies.Count; i++)
        {
          ht.Add(new MQTTItem(latestMovies[i]));
        }
      }
      return ht;
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

    internal void MovingPictureUpdateLatest()
    {
      if (Interlocked.CompareExchange(ref CurrentFacade.Update, 1, 0) != 0)
        return;
      
      if (!Utils.LatestMovingPictures)
      {
        EmptyLatestMediaProperties();
        CurrentFacade.Update = 0;
        return;
      }

      // Moving Pictures
      LatestsCollection hTable = GetLatestMovingPictures();
      LatestsToFilmStrip(latestMovies);

      if (MainFacade || CurrentFacade.AddProperties)
      {
        EmptyLatestMediaProperties();
        Utils.FillLatestsMovieProperty(CurrentFacade, hTable, MainFacade);
      }

      if ((latestMovies != null) && (latestMovies.Count > 0))
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
        Utils.UpdateLatestsUpdate(Utils.LatestsCategory.MovingPictures, DateTime.Now);
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
        try
        {
          movie.Rating = float.Parse(latests.Rating);
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
      if (!Utils.LatestMovingPictures)
      {
        return;
      }

      try
      {
        lock (lockObject)
        {
          // LatestsToFilmStrip(latestMovies);

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
      if (!Utils.LatestMovingPictures)
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
          Utils.FillSelectedMovieProperty(CurrentFacade, item, latestMovies[item.ItemId - 1]);

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
              Utils.SetProperty("#latestMediaHandler.movingpicture.selected.fanart1", _image);
              Utils.SetProperty("#latestMediaHandler.movingpicture.selected.showfanart1", "true");
              Utils.SetProperty("#latestMediaHandler.movingpicture.selected.showfanart2", "false");
              Thread.Sleep(1000);
              Utils.SetProperty("#latestMediaHandler.movingpicture.selected.fanart2", string.Empty);
              showFanart = 2;
            }
            else
            {
              Utils.SetProperty("#latestMediaHandler.movingpicture.selected.fanart2", _image);
              Utils.SetProperty("#latestMediaHandler.movingpicture.selected.showfanart2", "true");
              Utils.SetProperty("#latestMediaHandler.movingpicture.selected.showfanart1", "false");
              Thread.Sleep(1000);
              Utils.SetProperty("#latestMediaHandler.movingpicture.selected.fanart1", string.Empty);
              showFanart = 1;
            }
            // Utils.UnLoadImage(_image, ref images);
            CurrentFacade.SelectedImage = _id;
          }
        }
        else
        {
          Utils.SetProperty("#latestMediaHandler.movingpicture.selected.fanart1", " ");
          Utils.SetProperty("#latestMediaHandler.movingpicture.selected.fanart2", " ");
          Utils.SetProperty("#latestMediaHandler.movingpicture.selected.showfanart1", "false");
          Utils.SetProperty("#latestMediaHandler.movingpicture.selected.showfanart2", "false");
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
      try
      {
        UpdateSelectedProperties(item);
      }
      catch (Exception ex)
      {
        logger.Error("item_OnItemSelected: " + ex.ToString());
      }
    }

    internal bool PlayMovingPicture(GUIWindow fWindow)
    {
      try
      {
        int FocusControlID = fWindow.GetFocusControlId();
        if (ControlIDPlays.Contains(FocusControlID))
        {
          PlayMovingPicture(ControlIDPlays.IndexOf(FocusControlID)+1);
          return true;
        }
        //
        // CurrentFacade.Facade = Utils.GetLatestsFacade(CurrentFacade.ControlID);
        if (CurrentFacade.Facade != null && CurrentFacade.Facade.Focus && CurrentFacade.Facade.SelectedListItem != null)
        {
          PlayMovingPicture(CurrentFacade.Facade.SelectedListItem.ItemId);
          return true;
        }
      }
      catch (Exception ex)
      {
        logger.Error("Unable to play movie! " + ex.ToString());
        return true;
      }
      return false;
    }

    internal void PlayMovingPicture(int index)
    {
      if (moviePlayer == null)
        moviePlayer = new MoviePlayer(new MovingPicturesGUI());

      moviePlayer.Play((DBMovieInfo) latestMovingPictures[index]);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal void SetupReceivers()
    {
      if (!Utils.LatestMovingPictures)
      {
        return;
      }

      try
      {
        GUIWindowManager.Receivers += new SendMessageHandler(OnMessage);
        MovingPicturesCore.DatabaseManager.ObjectInserted +=  new RealCornerstone.Cornerstone.Database.DatabaseManager.ObjectAffectedDelegate(MovingPictureOnObjectInserted);
        MovingPicturesCore.DatabaseManager.ObjectDeleted += new RealCornerstone.Cornerstone.Database.DatabaseManager.ObjectAffectedDelegate(MovingPictureOnObjectDeleted);
      }
      catch (Exception ex)
      {
        logger.Error("SetupMovingPicturesLatest: " + ex.ToString());
      }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal void DisposeReceivers()
    {
      if (!Utils.LatestMovingPictures)
      {
        return;
      }

      try
      {
        GUIWindowManager.Receivers -= new SendMessageHandler(OnMessage);
        MovingPicturesCore.DatabaseManager.ObjectInserted -= new RealCornerstone.Cornerstone.Database.DatabaseManager.ObjectAffectedDelegate(MovingPictureOnObjectInserted);
        MovingPicturesCore.DatabaseManager.ObjectDeleted -= new RealCornerstone.Cornerstone.Database.DatabaseManager.ObjectAffectedDelegate(MovingPictureOnObjectDeleted);
      }
      catch (Exception ex)
      {
        logger.Error("DisposeMovingPicturesLatest: " + ex.ToString());
      }
    }

    private void MovingPictureOnObjectInserted(RealCornerstone.Cornerstone.Database.Tables.DatabaseTable obj)
    {
      try
      {
        if (obj.GetType() == typeof (DBMovieInfo) || obj.GetType() == typeof (DBWatchedHistory))
          GetLatestMediaInfoThread();
      }
      catch (Exception ex)
      {
        logger.Error("MovingPictureOnObjectInserted: " + ex.ToString());
      }
    }

    private void MovingPictureOnObjectDeleted(RealCornerstone.Cornerstone.Database.Tables.DatabaseTable obj)
    {
      try
      {
        if (obj.GetType() == typeof (DBMovieInfo) || obj.GetType() == typeof (DBWatchedHistory))
        {
          GetLatestMediaInfoThread();
        }
      }
      catch (Exception ex)
      {
        logger.Error("MovingPictureOnObjectDeleted: " + ex.ToString());
      }
    }

    private void OnMessage(GUIMessage message)
    {
      if (Utils.LatestMovingPictures)
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
      Utils.ThreadToSleep();
      switch (message.Message)
      {
        case GUIMessage.MessageType.GUI_MSG_PLAYBACK_ENDED:
        case GUIMessage.MessageType.GUI_MSG_PLAYBACK_STOPPED:
          logger.Debug("Playback End/Stop detected: Refreshing latest.");
          GetLatestMediaInfoThread();
          break;
      }
    }

    internal void GetLatestMediaInfoThread()
    {
      // Moving Pictures
      if (Utils.LatestMovingPictures)
      {
        try
        {
          RefreshWorker MyRefreshWorker = new RefreshWorker();
          MyRefreshWorker.RunWorkerCompleted += MyRefreshWorker.OnRunWorkerCompleted;
          MyRefreshWorker.RunWorkerAsync(this);
        }
        catch (Exception ex)
        {
          logger.Error("MovingPictureUpdateLatestThread: " + ex.ToString());
        }
      }
    }

    internal void UpdateImageTimer(GUIWindow fWindow, Object stateInfo, ElapsedEventArgs e)
    {
      if (Utils.LatestMovingPictures)
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
              Utils.SetProperty("#latestMediaHandler.movingpicture.selected.fanart1", " ");
              Utils.SetProperty("#latestMediaHandler.movingpicture.selected.fanart2", " ");
              Utils.UnLoadImages(ref images);
              ShowFanart = 1;
              CurrentFacade.SelectedImage = -1;
              NeedCleanup = false;
              NeedCleanupCount = 0;
            }
            else if (NeedCleanup && NeedCleanupCount == 0)
            {
              Utils.SetProperty("#latestMediaHandler.movingpicture.selected.showfanart1", "false");
              Utils.SetProperty("#latestMediaHandler.movingpicture.selected.showfanart2", "false");
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
          logger.Error("UpdateImageTimer (movingpicture): " + ex.ToString());
        }
      }
    }

    /// <summary>
    /// Get the runtime of a movie using its MediaInfo property
    /// </summary>
    /// <param name="movie"></param>
    /// <returns></returns>
    private string GetMovieRuntime(DBMovieInfo movie)
    {
      string minutes = string.Empty;
      try
      {
        if (movie == null)
        {
          return minutes;
        }

        if (MovingPicturesCore.Settings.DisplayActualRuntime && movie.ActualRuntime > 0)
        {
          // Actual Runtime or (MediaInfo result) is in milliseconds
          // convert to minutes
          if (movie.ActualRuntime > 0)
          {
            minutes = ((movie.ActualRuntime/1000)/60).ToString(CultureInfo.CurrentCulture);
          }
        }
        else
          minutes = movie.Runtime.ToString(CultureInfo.CurrentCulture);
      }
      catch (Exception ex)
      {
        logger.Error("GetMovieRuntime: " + ex.ToString());
      }
      return minutes;
    }

  }
}
