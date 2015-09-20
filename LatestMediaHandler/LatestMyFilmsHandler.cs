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
using MediaPortal.Video.Database;
using RealNLog.NLog;
using MyFilmsPlugin.MyFilms;
using MyFilmsPlugin.MyFilms.MyFilmsGUI;
using MediaPortal.Dialogs;
using System.Globalization;
using MediaPortal.GUI.Library;
using System.Threading;



namespace LatestMediaHandler
{
  internal class LatestMyFilmsHandler
  {
    #region declarations

    private Logger logger = LogManager.GetCurrentClassLogger();
    private bool _isGetTypeRunningOnThisThread /* = false*/;
//        private VideoHandler episodePlayer = null;
    private Hashtable latestMovies;
    private GUIFacadeControl facade = null;
    private ArrayList al = new ArrayList();
    internal ArrayList images = new ArrayList();
    internal ArrayList imagesThumbs = new ArrayList();
    private int selectedFacadeItem1 = -1;
    private int selectedFacadeItem2 = -1;
    private int showFanart = 1;
    private bool needCleanup = false;
    private int needCleanupCount = 0;
    private LatestMediaHandler.LatestsCollection result;
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

    public LatestMyFilmsHandler()
    {
    }

    private bool IsGetTypeRunningOnThisThread
    {
      get { return _isGetTypeRunningOnThisThread; }
      set { _isGetTypeRunningOnThisThread = value; }
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
        GUIListItem pItem = new GUIListItem(Translation.Play);
        dlg.Add(pItem);
        pItem.ItemId = 1;

        //Add Watched/Unwatched Filter Menu Item
        if (LatestMediaHandlerSetup.LatestMyFilmsWatched.Equals("False"))
        {
          pItem = new GUIListItem(Translation.ShowUnwatchedMovies);
          dlg.Add(pItem);
          pItem.ItemId = 2;
        }
        else
        {
          pItem = new GUIListItem(Translation.ShowAllMovies);
          dlg.Add(pItem);
          pItem.ItemId = 2;
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
            GUIControl gc = gw.GetControl(919199940);
            facade = gc as GUIFacadeControl;
            if (facade != null)
            {
              PlayMovie(facade.SelectedListItem.ItemId);
            }
            break;
          }
          case 2:
          {
            if (LatestMediaHandlerSetup.LatestMyFilmsWatched.Equals("False"))
            {
              LatestMediaHandlerSetup.LatestMyFilmsWatched = "True";
            }
            else
            {
              LatestMediaHandlerSetup.LatestMyFilmsWatched = "False";
            }
            MyFilmsUpdateLatest();
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
    /// Returns latest added movies from MyFilms plugin.
    /// </summary>
    /// <param name="type">Type of data to fetch</param>
    /// <returns>Resultset of matching data</returns>
    private LatestsCollection GetLatestMovies()
    {
      var resultTmp = new LatestsCollection();
      try
      {
        GUIWindow gw = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow);
        GUIControl gc = gw.GetControl(919199880);
        facade = gc as GUIFacadeControl;
        if (facade != null)
        {
          facade.Clear();
        }
        if (al != null)
        {
          al.Clear();
        }
        latestMovies = new Hashtable();
        int i0 = 1;
        int x0 = 0;
        List<MFMovie> movies = BaseMesFilms.GetMostRecent(BaseMesFilms.MostRecentType.Added, 999, 10,
          LatestMediaHandlerSetup.LatestMyFilmsWatched.Equals("True"));

        if (movies != null)
        {
          foreach (MFMovie movie in movies)
          {
            string thumb = movie.Picture;
            if (string.IsNullOrEmpty(thumb))
            {
              thumb = "DefaultFolderBig.png";
            }
            string tDate = movie.DateAdded;
            try
            {
              DateTime dTmp = DateTime.Parse(tDate);
              tDate = String.Format("{0:" + LatestMediaHandlerSetup.DateFormat + "}", dTmp);
            }
            catch
            {
            }
            resultTmp.Add(new Latest(tDate, thumb, movie.Fanart, movie.Title, null, null, null, movie.Category,
              movie.Rating.ToString(),
              Math.Round(movie.Rating, MidpointRounding.AwayFromZero).ToString(CultureInfo.CurrentCulture), null,
              movie.Length.ToString(), movie.Year.ToString(), null, null, null, movie, movie.ID.ToString(), null, null));
            if (result == null || result.Count == 0)
            {
              result = resultTmp;
            }
            latestMovies.Add(i0, movie);
            //if (facade != null)
            //{
            AddToFilmstrip(resultTmp[x0], i0);
            //}
            i0++;
            x0++;
            if (i0 == 10)
            {
              break;
            }
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
        logger.Error("GetLatestMovies: " + ex.ToString());
      }
      result = resultTmp;
      return result;
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
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myfilms.selected.thumb", item.IconImageBig);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myfilms.selected.title", item.Label);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myfilms.selected.dateAdded", item.Label3);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myfilms.selected.genre", item.Label2);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myfilms.selected.roundedRating", "" + item.Rating);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myfilms.selected.classification", "");
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myfilms.selected.runtime", "" + item.Duration);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myfilms.selected.year", "" + item.Year);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myfilms.selected.id", "" + item.ItemId);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myfilms.selected.plot", item.Path);
          selectedFacadeItem1 = item.ItemId;

          GUIWindow gw = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow);
          GUIControl gc = gw.GetControl(919199880);
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
        GUIControl gc = gw.GetControl(919199880);
        facade = gc as GUIFacadeControl;
        if (facade != null && gw.GetFocusControlId() == 919199880 && facade.SelectedListItem != null)
        {
          int _id = facade.SelectedListItem.ItemId;
          String _image = facade.SelectedListItem.DVDLabel;
          if (selectedFacadeItem2 != _id)
          {
            Utils.LoadImage(_image, ref images);
            if (showFanart == 1)
            {
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myfilms.selected.fanart1", _image);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myfilms.selected.showfanart1", "true");
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myfilms.selected.showfanart2", "");
              Thread.Sleep(1000);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myfilms.selected.fanart2", "");
              showFanart = 2;
            }
            else
            {
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myfilms.selected.fanart2", _image);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myfilms.selected.showfanart2", "true");
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myfilms.selected.showfanart1", "");
              Thread.Sleep(1000);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myfilms.selected.fanart1", "");
              showFanart = 1;
            }
            Utils.UnLoadImage(_image, ref images);
            selectedFacadeItem2 = _id;
          }
        }
        else
        {
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myfilms.selected.fanart1", " ");
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myfilms.selected.fanart2", " ");
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myfilms.selected.showfanart1", "true");
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myfilms.selected.showfanart2", "");
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

    internal void PlayMovie(int index)
    {
      MyFilmsPlugin.MyFilms.MFMovie mov = (MyFilmsPlugin.MyFilms.MFMovie) latestMovies[index];
      string loadingParameter = string.Format("config:{0}|movieid:{1}|play:{2}", mov.Config, mov.ID, "true");
      GUIWindowManager.ActivateWindow(7986, loadingParameter);
    }

    internal void SetupMovieLatest()
    {
      MyFilms.ImportComplete += new MyFilms.ImportCompleteEventDelegate(OnImportComplete);
    }

    internal void DisposeMovieLatest()
    {
      MyFilms.ImportComplete -= new MyFilms.ImportCompleteEventDelegate(OnImportComplete);
    }

    internal void OnImportComplete()
    {
      MyFilmsUpdateLatest();
    }


    internal void MyFilmsUpdateLatest()
    {
      int z = 1;
      if (LatestMediaHandlerSetup.LatestMyFilms.Equals("True", StringComparison.CurrentCulture))
      {
        LatestMediaHandler.LatestsCollection ht = null;
        try
        {
          ht = GetLatestMovies();
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
          logger.Error("MyFilmsUpdateLatest: " + ex.ToString());
        }
        for (int i = 0; i < 3; i++)
        {
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myfilms.latest" + z + ".poster", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myfilms.latest" + z + ".fanart", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myfilms.latest" + z + ".title", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myfilms.latest" + z + ".dateAdded", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myfilms.latest" + z + ".rating", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myfilms.latest" + z + ".roundedRating", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myfilms.latest" + z + ".year", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myfilms.latest" + z + ".id", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myfilms.latest" + z + ".genre", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myfilms.latest" + z + ".runtime", string.Empty);
          z++;
        }
        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myfilms.latest.enabled", "false");
        if (ht != null)
        {
          /*for (int i = 0; i < ht.Count && i < 3; i++)
                    {
                        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myfilms.latest" + z + ".poster", string.Empty);
                        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myfilms.latest" + z + ".fanart", string.Empty);
                        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myfilms.latest" + z + ".title", string.Empty);
                        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myfilms.latest" + z + ".dateAdded", string.Empty);                        
                        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myfilms.latest" + z + ".rating", string.Empty);
                        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myfilms.latest" + z + ".roundedRating", string.Empty);                        
                        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myfilms.latest" + z + ".year", string.Empty);
                        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myfilms.latest" + z + ".id", string.Empty);
                        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myfilms.latest" + z + ".genre", string.Empty);
                        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myfilms.latest" + z + ".runtime", string.Empty);
                        z++;
                    }*/
          z = 1;
          //ArrayList _al = new ArrayList();
          for (int i = 0; i < ht.Count && i < 3; i++)
          {
            logger.Info("Updating Latest Media Info: Latest MyFilms " + z + ": " + ht[i].Title);
            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myfilms.latest" + z + ".poster", ht[i].Thumb);
            //  _al.Add(ht[i].Fanart);                                                
            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myfilms.latest" + z + ".fanart", ht[i].Fanart);
            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myfilms.latest" + z + ".title", ht[i].Title);
            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myfilms.latest" + z + ".dateAdded", ht[i].DateAdded);
            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myfilms.latest" + z + ".rating", ht[i].Rating);
            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myfilms.latest" + z + ".roundedRating",
              ht[i].RoundedRating);
            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myfilms.latest" + z + ".year", ht[i].Year);
            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myfilms.latest" + z + ".id", ht[i].Id);
            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myfilms.latest" + z + ".genre", ht[i].Genre);
            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myfilms.latest" + z + ".runtime", ht[i].Runtime);
            z++;
          }
          /*LatestMediaHandlerSetup.UpdateLatestCache(ref LatestMediaHandlerSetup.LatestMyFilmsHash, _al);
                    if (_al != null)
                    {
                        _al.Clear();
                    }
                    _al = null;                            */
          ht.Clear();
        }
        ht = null;
        z = 1;
        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.myfilms.latest.enabled", "true");
        logger.Debug("Updating Latest Media Info: Latest MyFilms has new: " + (Utils.HasNewMyFilms ? "true" : "false"));
      }
      else
      {
        LatestMediaHandlerSetup.EmptyLatestMediaPropsMyFilms();
      }
    }


    /*private class LatestAddedComparer : IComparer<Latest>
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
    }*/
  }
}
