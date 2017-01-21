extern alias RealCornerstone;
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
using MediaPortal.Plugins.MovingPictures;
using MediaPortal.Plugins.MovingPictures.Database;
using MediaPortal.Plugins.MovingPictures.MainUI;
using MediaPortal.Video.Database;

using LMHNLog.NLog;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Timers;

namespace LatestMediaHandler
{
  internal class LatestMovingPicturesHandler
  {
    #region declarations

    private Logger logger = LogManager.GetCurrentClassLogger();
    private bool _isGetTypeRunningOnThisThread /* = false*/;
    private MoviePlayer moviePlayer;

    private LatestsCollection latestMovies = null;
    private Hashtable latestMovingPictures;

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

    public const int ControlID = 919199910;
    public const int Play1ControlID = 91919991;
    public const int Play2ControlID = 91919992;
    public const int Play3ControlID = 91919993;
    public const int Play4ControlID = 91919905;

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

    internal bool Restricted // MovingPicture restricted property
    {
      get { return MovingPictureIsRestricted(); }
    }

    internal LatestMovingPicturesHandler()
    {
      ControlIDFacades = new List<int>();
      ControlIDPlays = new List<int>();
      //
      ControlIDFacades.Add(ControlID);
      ControlIDPlays.Add(Play1ControlID);
      ControlIDPlays.Add(Play2ControlID);
      ControlIDPlays.Add(Play3ControlID);
      ControlIDPlays.Add(Play4ControlID);
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
        if (LatestMediaHandlerSetup.LatestMovingPicturesWatched.Equals("False", StringComparison.CurrentCulture))
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

        facade = Utils.GetLatestsFacade(ControlID);
        switch (dlg.SelectedId)
        {
          case 1:
          {
            PlayMovingPicture(GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow));
            break;
          }
          case 2:
          {
            ShowInfo();
            break;
          }
          case 3:
          {
            LatestMediaHandlerSetup.LatestMovingPicturesWatched = (LatestMediaHandlerSetup.LatestMovingPicturesWatched.Equals("False", StringComparison.CurrentCulture)) ? "True" : "False" ;
            MovingPictureUpdateLatest();
            break;
          }
          case 4:
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
        GUIWindow fWindow = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow);
        int FocusControlID = fWindow.GetFocusControlId();

        int idx = -1 ;
        if (ControlIDPlays.Contains(FocusControlID))
        {
          idx = ControlIDPlays.IndexOf(FocusControlID);
        }
        //
        facade = Utils.GetLatestsFacade(ControlID);
        if (facade != null && facade.Focus && facade.SelectedListItem != null)
        {
          idx = facade.SelectedListItem.ItemId-1;
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
    private LatestsCollection GetLatestMovingPictures()
    {
      latestMovies = new LatestsCollection();
      latestMovingPictures = new Hashtable();

      LatestsCollection latests = new LatestsCollection();

      int x = 0;
      string sTimestamp = string.Empty;
      try
      {
        if (Restricted)
        {
          var vMovies = MovingPicturesCore.Settings.ParentalControlsFilter.Filter(DBMovieInfo.GetAll());
          foreach (var item in vMovies)
          {
            if ((LatestMediaHandlerSetup.LatestMovingPicturesWatched.Equals("True", StringComparison.CurrentCulture)) && (item.UserSettings[0].WatchedCount > 0))
              continue ;

            sTimestamp = item.DateAdded.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture);
            string fanart = item.CoverThumbFullPath;
            if (string.IsNullOrEmpty(fanart))
              fanart = "DefaultFolderBig.png";
            
            latests.Add(new Latest(sTimestamp, fanart, item.BackdropFullPath, item.Title, 
                                   null, null, null,
                                   item.Genres.ToPrettyString(2), item.Score.ToString(CultureInfo.CurrentCulture),
                                   Math.Round(item.Score, MidpointRounding.AwayFromZero).ToString(CultureInfo.CurrentCulture),
                                   item.Certification, GetMovieRuntime(item), item.Year.ToString(CultureInfo.CurrentCulture), 
                                   null, null, null, 
                                   item, item.ID.ToString(), item.Summary, 
                                   null));
            Utils.ThreadToSleep();
          }
          if (vMovies != null)
            vMovies.Clear();
          vMovies = null;
        }
        else
        {
          var vMovies = DBMovieInfo.GetAll();
          foreach (var item in vMovies)
          {
            if ((LatestMediaHandlerSetup.LatestMovingPicturesWatched.Equals("True", StringComparison.CurrentCulture)) && (item.UserSettings[0].WatchedCount > 0))
              continue ;

            sTimestamp = item.DateAdded.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture);
            string fanart = item.CoverThumbFullPath;
            if (string.IsNullOrEmpty(fanart))
              fanart = "DefaultFolderBig.png";

            latests.Add(new Latest(sTimestamp, fanart, item.BackdropFullPath, item.Title, 
                                   null, null, null,
                                   item.Genres.ToPrettyString(2), item.Score.ToString(CultureInfo.CurrentCulture),
                                   Math.Round(item.Score, MidpointRounding.AwayFromZero).ToString(CultureInfo.CurrentCulture),
                                   item.Certification, GetMovieRuntime(item), item.Year.ToString(CultureInfo.CurrentCulture), 
                                   null, null, null, 
                                   item, item.ID.ToString(), item.Summary, 
                                   null));
            Utils.ThreadToSleep();
          }
          if (vMovies != null)
            vMovies.Clear();
          vMovies = null;
        }

        Utils.HasNewMovingPictures = false;
        latests.Sort(new LatestAddedComparer());

        int i0 = 1;
        for (int x0 = 0; x0 < latests.Count; x0++)
        {
          try
          {
            DateTime dTmp = DateTime.Parse(latests[x0].DateAdded);
            latests[x0].DateAdded = String.Format("{0:" + LatestMediaHandlerSetup.DateFormat + "}", dTmp);

            DBMovieInfo Movie = (DBMovieInfo)latests[x0].Playable ;
            latests[x0].New = (((dTmp > Utils.NewDateTime) && (Movie.UserSettings[0].WatchedCount <= 0)) ? "true" : "false");
            if ((dTmp > Utils.NewDateTime) && (Movie.UserSettings[0].WatchedCount <= 0))
              Utils.HasNewMovingPictures = true;
          }
          catch
          { }

          latestMovies.Add(latests[x0]);
          latestMovingPictures.Add(i0, latests[x0].Playable);
          Utils.ThreadToSleep();

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
        logger.Error("GetLatestMovingPictures: " + ex.ToString());
      }

      if (latests != null)
        latests.Clear();
      latests = null;

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

    internal void EmptyLatestMediaPropsMovingPictures()
    {
      Utils.SetProperty("#latestMediaHandler.movingpicture.label", Translation.LabelLatestAdded);
      Utils.SetProperty("#latestMediaHandler.movingpicture.latest.enabled", "false");
      Utils.SetProperty("#latestMediaHandler.movingpicture.hasnew", "false");
      for (int z = 1; z < 4; z++)
      {
        Utils.SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".thumb", string.Empty);
        Utils.SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".fanart", string.Empty);
        Utils.SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".title", string.Empty);
        Utils.SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".dateAdded", string.Empty);
        Utils.SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".genre", string.Empty);
        Utils.SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".rating", string.Empty);
        Utils.SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".roundedRating", string.Empty);
        Utils.SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".classification", string.Empty);
        Utils.SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".runtime", string.Empty);
        Utils.SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".year", string.Empty);
        Utils.SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".id", string.Empty);
        Utils.SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".plot", string.Empty);
        Utils.SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".plotoutline", string.Empty);
        Utils.SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".new", "false");
      }
    }

    internal void MovingPictureUpdateLatest()
    {
      int sync = Interlocked.CompareExchange(ref Utils.SyncPointMovingPicturesUpdate, 1, 0);
      if (sync != 0)
        return;
      
      if (!LatestMediaHandlerSetup.LatestMovingPictures.Equals("True", StringComparison.CurrentCulture)) // && !(windowId.Equals("96742")))
      {
        EmptyLatestMediaPropsMovingPictures();
        return;
      }

      // Moving Pictures
      try
      {
        LatestsCollection hTable = GetLatestMovingPictures();
        EmptyLatestMediaPropsMovingPictures();

        if (hTable != null)
        {
          int z = 1;
          for (int i = 0; i < hTable.Count && i < Utils.LatestsMaxNum; i++)
          {
            logger.Info("Updating Latest Media Info: MovingPictures: Movie " + z + ": " + hTable[i].Title + " " + hTable[i].DateAdded);

            string plot = (string.IsNullOrEmpty(hTable[i].Summary) ? Translation.NoDescription : hTable[i].Summary);
            string plotoutline = Utils.GetSentences(plot, Utils.latestPlotOutlineSentencesNum);
            Utils.SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".thumb", hTable[i].Thumb);
            Utils.SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".fanart", hTable[i].Fanart);
            Utils.SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".title", hTable[i].Title);
            Utils.SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".dateAdded", hTable[i].DateAdded);
            Utils.SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".genre", hTable[i].Genre);
            Utils.SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".rating", hTable[i].Rating);
            Utils.SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".roundedRating", hTable[i].RoundedRating);
            Utils.SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".classification", hTable[i].Classification);
            Utils.SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".runtime", hTable[i].Runtime);
            Utils.SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".year", hTable[i].Year);
            Utils.SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".id", hTable[i].Id);
            Utils.SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".plot", plot);
            Utils.SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".plotoutline", plotoutline);
            Utils.SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".new", hTable[i].New);
            z++;
          }
          // hTable.Clear();
          Utils.SetProperty("#latestMediaHandler.movingpicture.hasnew", Utils.HasNewMovingPictures ? "true" : "false");
          logger.Debug("Updating Latest Media Info: MovingPictures: Has new: " + (Utils.HasNewMovingPictures ? "true" : "false"));
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
        logger.Error("MovingPictureOnObjectInserted: " + ex.ToString());
      }

      if ((latestMovies != null) && (latestMovies.Count > 0))
      {
        // if (System.Windows.Forms.Form.ActiveForm.InvokeRequired)
        // {
        //   System.Windows.Forms.Form.ActiveForm.Invoke(InitFacade);
        // }
        // else
        // {
          InitFacade();
        // }
        Utils.SetProperty("#latestMediaHandler.movingpicture.latest.enabled", "true");
      }
      else
        EmptyLatestMediaPropsMovingPictures();
      Utils.UpdateLatestsUpdate(Utils.LatestsCategory.MovingPictures, DateTime.Now);
      Utils.SyncPointMovingPicturesUpdate=0;
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
        LatestsToFilmStrip(latestMovies);

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
          Utils.SetProperty("#latestMediaHandler.movingpicture.selected.thumb", item.IconImageBig);
          Utils.SetProperty("#latestMediaHandler.movingpicture.selected.title", item.Label);
          Utils.SetProperty("#latestMediaHandler.movingpicture.selected.dateAdded", item.Label3);
          Utils.SetProperty("#latestMediaHandler.movingpicture.selected.genre", item.Label2);
          //decimal d = 0;
          string _rating = "0";
          string _roundedRating = "0";
          try
          {
            _rating = item.Rating.ToString(CultureInfo.CurrentCulture);
          }
          catch
          {   }
          try
          {
            _roundedRating = Math.Round(item.Rating, MidpointRounding.AwayFromZero).ToString(CultureInfo.CurrentCulture);
          }
          catch
          {   }
          Utils.SetProperty("#latestMediaHandler.movingpicture.selected.rating", "" + _rating);
          Utils.SetProperty("#latestMediaHandler.movingpicture.selected.roundedRating", "" + _roundedRating);
          Utils.SetProperty("#latestMediaHandler.movingpicture.selected.classification", "");
          Utils.SetProperty("#latestMediaHandler.movingpicture.selected.runtime", "" + item.Duration);
          Utils.SetProperty("#latestMediaHandler.movingpicture.selected.year", "" + item.Year);
          Utils.SetProperty("#latestMediaHandler.movingpicture.selected.id", "" + item.ItemId);

          int i = item.ItemId - 1;
          string plot = (string.IsNullOrEmpty(latestMovies[i].Summary) ? Translation.NoDescription : latestMovies[i].Summary);
          string plotoutline = Utils.GetSentences(plot, Utils.latestPlotOutlineSentencesNum);
          Utils.SetProperty("#latestMediaHandler.movingpicture.selected.plot", plot);
          Utils.SetProperty("#latestMediaHandler.movingpicture.selected.plotoutline", plotoutline);
          Utils.SetProperty("#latestMediaHandler.movingpicture.selected.new", latestMovies[i].New);
          selectedFacadeItem1 = item.ItemId;

          facade = Utils.GetLatestsFacade(ControlID);
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
              Utils.SetProperty("#latestMediaHandler.movingpicture.selected.fanart1", _image);
              Utils.SetProperty("#latestMediaHandler.movingpicture.selected.showfanart1", "true");
              Utils.SetProperty("#latestMediaHandler.movingpicture.selected.showfanart2", "false");
              Thread.Sleep(1000);
              Utils.SetProperty("#latestMediaHandler.movingpicture.selected.fanart2", "");
              showFanart = 2;
            }
            else
            {
              Utils.SetProperty("#latestMediaHandler.movingpicture.selected.fanart2", _image);
              Utils.SetProperty("#latestMediaHandler.movingpicture.selected.showfanart2", "true");
              Utils.SetProperty("#latestMediaHandler.movingpicture.selected.showfanart1", "false");
              Thread.Sleep(1000);
              Utils.SetProperty("#latestMediaHandler.movingpicture.selected.fanart1", "");
              showFanart = 1;
            }
            Utils.UnLoadImage(_image, ref images);
            selectedFacadeItem2 = _id;
          }
        }
        else
        {
          Utils.SetProperty("#latestMediaHandler.movingpicture.selected.fanart1", " ");
          Utils.SetProperty("#latestMediaHandler.movingpicture.selected.fanart2", " ");
          Utils.SetProperty("#latestMediaHandler.movingpicture.selected.showfanart1", "false");
          Utils.SetProperty("#latestMediaHandler.movingpicture.selected.showfanart2", "false");
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
        facade = Utils.GetLatestsFacade(ControlID);
        if (facade != null && facade.Focus && facade.SelectedListItem != null)
        {
          PlayMovingPicture(facade.SelectedListItem.ItemId);
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

    internal void SetupMovingPicturesLatest()
    {
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

    internal void DisposeMovingPicturesLatest()
    {
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
          MovingPictureUpdateLatestThread();
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
          MovingPictureUpdateLatestThread();
        }
      }
      catch (Exception ex)
      {
        logger.Error("MovingPictureOnObjectDeleted: " + ex.ToString());
      }
    }

    private void OnMessage(GUIMessage message)
    {
      Utils.ThreadToSleep();
      if (LatestMediaHandlerSetup.LatestMovingPictures.Equals("True", StringComparison.CurrentCulture))
      {
        try
        {
          switch (message.Message)
          {
            case GUIMessage.MessageType.GUI_MSG_PLAYBACK_ENDED:
            case GUIMessage.MessageType.GUI_MSG_PLAYBACK_STOPPED:
            {
              logger.Debug("Playback End/Stop detected: Refreshing latest.");
              try
              {
                MovingPictureUpdateLatestThread();
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

    internal void MovingPictureUpdateLatestThread()
    {
      // Moving Pictures
      if (LatestMediaHandlerSetup.LatestMovingPictures.Equals("True", StringComparison.CurrentCulture))
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
      if (LatestMediaHandlerSetup.LatestMovingPictures.Equals("True", StringComparison.CurrentCulture))
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
              Utils.SetProperty("#latestMediaHandler.movingpicture.selected.fanart1", " ");
              Utils.SetProperty("#latestMediaHandler.movingpicture.selected.fanart2", " ");
              Utils.UnLoadImage(ref images);
              ShowFanart = 1;
              SelectedFacadeItem2 = -1;
              SelectedFacadeItem2 = -1;
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
