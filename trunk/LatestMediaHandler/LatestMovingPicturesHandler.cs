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
extern alias RealCornerstone;
using System;
using System.Collections;
using System.Collections.Generic;
using RealNLog.NLog; 
using MediaPortal.GUI.Library;
using MediaPortal.Dialogs;
using MediaPortal.Video.Database;
using MediaPortal.Plugins.MovingPictures;
using MediaPortal.Plugins.MovingPictures.Database;
using MediaPortal.Plugins.MovingPictures.MainUI;
using System.Globalization;
using System.Threading;



namespace LatestMediaHandler
{
  internal class LatestMovingPicturesHandler
  {
    #region declarations

    private Logger logger = LogManager.GetCurrentClassLogger();
    private bool _isGetTypeRunningOnThisThread /* = false*/;
    private MoviePlayer moviePlayer;
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
    private LatestMediaHandler.LatestsCollection result = null;
    private int lastFocusedId = 0;

    #endregion

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

    internal int MovingPictureIsRestricted()
    {
      try
      {
        if (MovingPicturesCore.Settings.ParentalControlsEnabled)
        {
          return 0;
        }
        else
        {
          return 1;
        }

      }
      catch
      {
      }
      return 0;
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
        if (LatestMediaHandlerSetup.LatestMovingPicturesWatched.Equals("False"))
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
            GUIControl gc = gw.GetControl(919199910);
            facade = gc as GUIFacadeControl;
            if (facade != null)
            {
              PlayMovingPicture(facade.SelectedListItem.ItemId);
            }
            break;
          }
          case 2:
          {
            GUIWindow gw = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow);
            GUIControl gc = gw.GetControl(919199910);
            facade = gc as GUIFacadeControl;
            if (facade != null)
            {
              string sHyp = "movieid:" + result[(facade.SelectedListItemIndex)].Id;
              GUIWindowManager.ActivateWindow(96742, sHyp, false);
            }
            break;
          }
          case 3:
          {
            if (LatestMediaHandlerSetup.LatestMovingPicturesWatched.Equals("False"))
            {
              LatestMediaHandlerSetup.LatestMovingPicturesWatched = "True";
            }
            else
            {
              LatestMediaHandlerSetup.LatestMovingPicturesWatched = "False";
            }
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

    /// <summary>
    /// Returns latest added movie thumbs from MovingPictures db.
    /// </summary>
    /// <param name="type">Type of data to fetch</param>
    /// <returns>Resultset of matching data</returns>
    private LatestsCollection GetLatestMovingPictures()
    {
      try
      {
        LatestMediaHandler.LatestsCollection resultTmp = new LatestMediaHandler.LatestsCollection();
        LatestsCollection latests = new LatestsCollection();
        int x = 0;
        string sTimestamp = string.Empty;
        try
        {
          GUIWindow gw = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow);
          GUIControl gc = gw.GetControl(919199910);
          ArrayList alTmp = new ArrayList();
          facade = gc as GUIFacadeControl;
          /*if (facade != null)
                    {
                        facade.Clear();
                    }
                    if (al != null)
                    {
                        al.Clear();
                    }*/
          if (MovingPicturesCore.Settings.ParentalControlsEnabled)
          {

            var vMovies = MovingPicturesCore.Settings.ParentalControlsFilter.Filter(DBMovieInfo.GetAll());

            foreach (var item in vMovies)
            {
              if (LatestMediaHandlerSetup.LatestMovingPicturesWatched.Equals("True"))
              {
                if (item.UserSettings[0].WatchedCount < 1)
                {
                  sTimestamp = item.DateAdded.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture);
                  string fanart = item.CoverThumbFullPath;
                  if (fanart == null || fanart.Length < 1)
                  {
                    fanart = "DefaultFolderBig.png";
                  }
                  latests.Add(new Latest(sTimestamp, fanart, item.BackdropFullPath, item.Title, null, null, null,
                    item.Genres.ToPrettyString(2), item.Score.ToString(CultureInfo.CurrentCulture),
                    Math.Round(item.Score, MidpointRounding.AwayFromZero).ToString(CultureInfo.CurrentCulture),
                    item.Certification, GetMovieRuntime(item), item.Year.ToString(CultureInfo.CurrentCulture), null,
                    null, null, item, item.ID.ToString(), item.Summary, null));
                }
              }
              else
              {
                sTimestamp = item.DateAdded.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture);
                string fanart = item.CoverThumbFullPath;
                if (fanart == null || fanart.Length < 1)
                {
                  fanart = "DefaultFolderBig.png";
                }
                latests.Add(new Latest(sTimestamp, fanart, item.BackdropFullPath, item.Title, null, null, null,
                  item.Genres.ToPrettyString(2), item.Score.ToString(CultureInfo.CurrentCulture),
                  Math.Round(item.Score, MidpointRounding.AwayFromZero).ToString(CultureInfo.CurrentCulture),
                  item.Certification, GetMovieRuntime(item), item.Year.ToString(CultureInfo.CurrentCulture), null, null,
                  null, item, item.ID.ToString(), item.Summary, null));
              }
            }
            if (vMovies != null)
            {
              vMovies.Clear();
            }
            vMovies = null;
            latestMovingPictures = new Hashtable();
            int i0 = 1;
            latests.Sort(new LatestAddedComparer());
            for (int x0 = 0; x0 < latests.Count; x0++)
            {
              try
              {
                DateTime dTmp = DateTime.Parse(latests[x0].DateAdded);
                latests[x0].DateAdded = String.Format("{0:" + LatestMediaHandlerSetup.DateFormat + "}", dTmp);
              }
              catch
              {
              }
              resultTmp.Add(latests[x0]);
              if (result == null || result.Count == 0)
              {
                result = resultTmp;
              }
              latestMovingPictures.Add(i0, latests[x0].Playable);
              //if (facade != null)
              //{
              AddToFilmstrip(latests[x0], i0, ref alTmp);
              //}
              x++;
              i0++;
              if (x == 10)
              {
                break;
              }

            }
          }
          else
          {
            var vMovies = DBMovieInfo.GetAll();
            foreach (var item in vMovies)
            {
              if (LatestMediaHandlerSetup.LatestMovingPicturesWatched.Equals("True"))
              {
                if (item.UserSettings[0].WatchedCount < 1)
                {
                  sTimestamp = item.DateAdded.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture);
                  string fanart = item.CoverThumbFullPath;
                  if (fanart == null || fanart.Length < 1)
                  {
                    fanart = "DefaultFolderBig.png";
                  }
                  latests.Add(new Latest(sTimestamp, fanart, item.BackdropFullPath, item.Title, null, null, null,
                    item.Genres.ToPrettyString(2), item.Score.ToString(CultureInfo.CurrentCulture),
                    Math.Round(item.Score, MidpointRounding.AwayFromZero).ToString(CultureInfo.CurrentCulture),
                    item.Certification, GetMovieRuntime(item), item.Year.ToString(CultureInfo.CurrentCulture), null,
                    null, null, item, item.ID.ToString(), item.Summary, null));
                }
              }
              else
              {
                sTimestamp = item.DateAdded.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture);
                string fanart = item.CoverThumbFullPath;
                if (fanart == null || fanart.Length < 1)
                {
                  fanart = "DefaultFolderBig.png";
                }
                latests.Add(new Latest(sTimestamp, fanart, item.BackdropFullPath, item.Title, null, null, null,
                  item.Genres.ToPrettyString(2), item.Score.ToString(CultureInfo.CurrentCulture),
                  Math.Round(item.Score, MidpointRounding.AwayFromZero).ToString(CultureInfo.CurrentCulture),
                  item.Certification, GetMovieRuntime(item), item.Year.ToString(CultureInfo.CurrentCulture), null, null,
                  null, item, item.ID.ToString(), item.Summary, null));
              }
            }
            if (vMovies != null)
            {
              vMovies.Clear();
            }
            vMovies = null;
            latestMovingPictures = new Hashtable();
            int i0 = 1;
            latests.Sort(new LatestAddedComparer());
            for (int x0 = 0; x0 < latests.Count; x0++)
            {
              try
              {
                DateTime dTmp = DateTime.Parse(latests[x0].DateAdded);
                latests[x0].DateAdded = String.Format("{0:" + LatestMediaHandlerSetup.DateFormat + "}", dTmp);
              }
              catch
              {
              }
              resultTmp.Add(latests[x0]);
              if (result == null || result.Count == 0)
              {
                result = resultTmp;
              }
              latestMovingPictures.Add(i0, latests[x0].Playable);
              //if (facade != null)
              //{
              AddToFilmstrip(latests[x0], i0, ref alTmp);
              //}
              x++;
              i0++;
              if (x == 10)
              {
                break;
              }
            }

          }

          if (al != null)
          {
            al.Clear();
          }

          if (facade != null)
          {
            if (facade != null)
            {
              facade.Clear();
            }

            foreach (GUIListItem item in alTmp)
            {
              facade.Add(item);
            }
          }
          al = alTmp;
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
        catch (Exception ex)
        {
          if (latests != null)
          {
            latests.Clear();
          }
          latests = null;
          logger.Error("GetLatestMovingPictures: " + ex.ToString());
        }
        if (latests != null)
        {
          latests.Clear();
        }
        latests = null;
        result = resultTmp;
        return result;
      }
      catch (Exception ex)
      {
        logger.Error("GetLatestMovingPictures: " + ex.ToString());
      }
      return null;
    }

    private void AddToFilmstrip(Latest latests, int x, ref ArrayList alTmp)
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
          movie.Rating = float.Parse(latests.Rating);
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
        /*if (facade != null)
            {
                facade.Add(item);
            }
            al.Add(item);            */
        alTmp.Add(item);
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
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.selected.thumb", item.IconImageBig);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.selected.title", item.Label);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.selected.dateAdded", item.Label3);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.selected.genre", item.Label2);
          //decimal d = 0;
          string _rating = "0";
          string _roundedRating = "0";
          try
          {
            _rating = item.Rating.ToString(CultureInfo.CurrentCulture);
          }
          catch
          {
          }
          try
          {
            _roundedRating = Math.Round(item.Rating, MidpointRounding.AwayFromZero).ToString(CultureInfo.CurrentCulture);
          }
          catch
          {
          }
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.selected.rating", "" + _rating);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.selected.roundedRating",
            "" + _roundedRating);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.selected.classification", "");
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.selected.runtime", "" + item.Duration);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.selected.year", "" + item.Year);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.selected.id", "" + item.ItemId);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.selected.plot", item.Path);
          selectedFacadeItem1 = item.ItemId;

          GUIWindow gw = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow);
          GUIControl gc = gw.GetControl(919199910);
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
        GUIControl gc = gw.GetControl(919199910);
        facade = gc as GUIFacadeControl;
        if (facade != null && gw.GetFocusControlId() == 919199910 && facade.SelectedListItem != null)
        {
          int _id = facade.SelectedListItem.ItemId;
          String _image = facade.SelectedListItem.DVDLabel;
          if (selectedFacadeItem2 != _id)
          {
            Utils.LoadImage(_image, ref images);
            if (showFanart == 1)
            {
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.selected.fanart1", _image);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.selected.showfanart1", "true");
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.selected.showfanart2", "");
              Thread.Sleep(1000);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.selected.fanart2", "");
              showFanart = 2;
            }
            else
            {
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.selected.fanart2", _image);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.selected.showfanart2", "true");
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.selected.showfanart1", "");
              Thread.Sleep(1000);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.selected.fanart1", "");
              showFanart = 1;
            }
            Utils.UnLoadImage(_image, ref images);
            selectedFacadeItem2 = _id;
          }
        }
        else
        {
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.selected.fanart1", " ");
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.selected.fanart2", " ");
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.selected.showfanart1", "true");
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.selected.showfanart2", "");
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


    internal void PlayMovingPicture(int index)
    {
      if (moviePlayer == null)
      {
        moviePlayer = new MoviePlayer(new MovingPicturesGUI());
      }

      moviePlayer.Play((DBMovieInfo) latestMovingPictures[index]);
    }

    internal void SetupMovingPicturesLatest()
    {
      try
      {
        MovingPicturesCore.DatabaseManager.ObjectInserted +=
          new RealCornerstone.Cornerstone.Database.DatabaseManager.ObjectAffectedDelegate(MovingPictureOnObjectInserted);
        MovingPicturesCore.DatabaseManager.ObjectDeleted +=
          new RealCornerstone.Cornerstone.Database.DatabaseManager.ObjectAffectedDelegate(MovingPictureOnObjectDeleted);
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
        MovingPicturesCore.DatabaseManager.ObjectInserted -=
          new RealCornerstone.Cornerstone.Database.DatabaseManager.ObjectAffectedDelegate(MovingPictureOnObjectInserted);
        MovingPicturesCore.DatabaseManager.ObjectDeleted -=
          new RealCornerstone.Cornerstone.Database.DatabaseManager.ObjectAffectedDelegate(MovingPictureOnObjectDeleted);
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
        {
          MovingPictureUpdateLatest();
        }
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
          MovingPictureUpdateLatest();
        }
      }
      catch (Exception ex)
      {
        logger.Error("MovingPictureOnObjectDeleted: " + ex.ToString());
      }
    }

    internal void MovingPictureUpdateLatest()
    {
      try
      {
        int z = 1;
        for (int i = 0; i < 3; i++)
        {
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".thumb", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".fanart", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".title", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".dateAdded",
            string.Empty);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".genre", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".rating", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".roundedRating",
            string.Empty);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".classification",
            string.Empty);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".runtime", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".year", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".id", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".plot", string.Empty);
          //OLD
          LatestMediaHandlerSetup.SetProperty("#fanarthandler.movingpicture.latest" + z + ".thumb", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#fanarthandler.movingpicture.latest" + z + ".fanart", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#fanarthandler.movingpicture.latest" + z + ".title", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#fanarthandler.movingpicture.latest" + z + ".dateAdded", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#fanarthandler.movingpicture.latest" + z + ".genre", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#fanarthandler.movingpicture.latest" + z + ".rating", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#fanarthandler.movingpicture.latest" + z + ".roundedRating", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#fanarthandler.movingpicture.latest" + z + ".classification",
            string.Empty);
          LatestMediaHandlerSetup.SetProperty("#fanarthandler.movingpicture.latest" + z + ".runtime", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#fanarthandler.movingpicture.latest" + z + ".year", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#fanarthandler.movingpicture.latest" + z + ".id", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#fanarthandler.movingpicture.latest" + z + ".plot", string.Empty);
          z++;
        }
        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.latest.enabled", "false");
        //OLD
        LatestMediaHandlerSetup.SetProperty("#fanarthandler.movingpicture.latest.enabled", "false");
        if (LatestMediaHandlerSetup.LatestMovingPictures.Equals("True", StringComparison.CurrentCulture))
          // && !(windowId.Equals("96742")))
        {
          GetLatestMovingPictures();
          if (result != null)
          {
            /*for (int i = 0; i < result.Count && i < 3; i++)
                        {
                            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".thumb", string.Empty);
                            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".fanart", string.Empty);
                            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".title", string.Empty);
                            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".dateAdded", string.Empty);
                            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".genre", string.Empty);
                            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".rating", string.Empty);
                            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".roundedRating", string.Empty);
                            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".classification", string.Empty);
                            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".runtime", string.Empty);
                            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".year", string.Empty);
                            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".id", string.Empty);
                            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".plot", string.Empty);
                            //OLD
                            LatestMediaHandlerSetup.SetProperty("#fanarthandler.movingpicture.latest" + z + ".thumb", string.Empty);
                            LatestMediaHandlerSetup.SetProperty("#fanarthandler.movingpicture.latest" + z + ".fanart", string.Empty);
                            LatestMediaHandlerSetup.SetProperty("#fanarthandler.movingpicture.latest" + z + ".title", string.Empty);
                            LatestMediaHandlerSetup.SetProperty("#fanarthandler.movingpicture.latest" + z + ".dateAdded", string.Empty);
                            LatestMediaHandlerSetup.SetProperty("#fanarthandler.movingpicture.latest" + z + ".genre", string.Empty);
                            LatestMediaHandlerSetup.SetProperty("#fanarthandler.movingpicture.latest" + z + ".rating", string.Empty);
                            LatestMediaHandlerSetup.SetProperty("#fanarthandler.movingpicture.latest" + z + ".roundedRating", string.Empty);
                            LatestMediaHandlerSetup.SetProperty("#fanarthandler.movingpicture.latest" + z + ".classification", string.Empty);
                            LatestMediaHandlerSetup.SetProperty("#fanarthandler.movingpicture.latest" + z + ".runtime", string.Empty);
                            LatestMediaHandlerSetup.SetProperty("#fanarthandler.movingpicture.latest" + z + ".year", string.Empty);
                            LatestMediaHandlerSetup.SetProperty("#fanarthandler.movingpicture.latest" + z + ".id", string.Empty);
                            LatestMediaHandlerSetup.SetProperty("#fanarthandler.movingpicture.latest" + z + ".plot", string.Empty);
                            z++;
                        }*/
            z = 1;
            //ArrayList _al = new ArrayList();
            for (int i = 0; i < result.Count && i < 3; i++)
            {
              logger.Info("Updating Latest Media Info: Latest movie " + z + ": " + result[i].Title);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".thumb",
                result[i].Thumb);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".fanart",
                result[i].Fanart);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".title",
                result[i].Title);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".dateAdded",
                result[i].DateAdded);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".genre",
                result[i].Genre);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".rating",
                result[i].Rating);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".roundedRating",
                result[i].RoundedRating);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".classification",
                result[i].Classification);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".runtime",
                result[i].Runtime);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".year",
                result[i].Year);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".id", result[i].Id);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.latest" + z + ".plot",
                result[i].Summary);
              //OLD
              LatestMediaHandlerSetup.SetProperty("#fanarthandler.movingpicture.latest" + z + ".thumb", result[i].Thumb);
              LatestMediaHandlerSetup.SetProperty("#fanarthandler.movingpicture.latest" + z + ".fanart",
                result[i].Fanart);
              LatestMediaHandlerSetup.SetProperty("#fanarthandler.movingpicture.latest" + z + ".title", result[i].Title);
              LatestMediaHandlerSetup.SetProperty("#fanarthandler.movingpicture.latest" + z + ".dateAdded",
                result[i].DateAdded);
              LatestMediaHandlerSetup.SetProperty("#fanarthandler.movingpicture.latest" + z + ".genre", result[i].Genre);
              LatestMediaHandlerSetup.SetProperty("#fanarthandler.movingpicture.latest" + z + ".rating",
                result[i].Rating);
              LatestMediaHandlerSetup.SetProperty("#fanarthandler.movingpicture.latest" + z + ".roundedRating",
                result[i].RoundedRating);
              LatestMediaHandlerSetup.SetProperty("#fanarthandler.movingpicture.latest" + z + ".classification",
                result[i].Classification);
              LatestMediaHandlerSetup.SetProperty("#fanarthandler.movingpicture.latest" + z + ".runtime",
                result[i].Runtime);
              LatestMediaHandlerSetup.SetProperty("#fanarthandler.movingpicture.latest" + z + ".year", result[i].Year);
              LatestMediaHandlerSetup.SetProperty("#fanarthandler.movingpicture.latest" + z + ".id", result[i].Id);
              LatestMediaHandlerSetup.SetProperty("#fanarthandler.movingpicture.latest" + z + ".plot", result[i].Summary);
              z++;
            }
          }
          z = 1;
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.movingpicture.latest.enabled", "true");
          //OLD
          LatestMediaHandlerSetup.SetProperty("#fanarthandler.movingpicture.latest.enabled", "true");
        }
        else
        {
          LatestMediaHandlerSetup.EmptyLatestMediaPropsMovingPictures();
        }
      }
      catch (Exception ex)
      {
        LatestMediaHandlerSetup.EmptyLatestMediaPropsMovingPictures();
        logger.Error("MovingPictureOnObjectInserted: " + ex.ToString());
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
