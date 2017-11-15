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
using MediaPortal.Video.Database;

using MyFilmsPlugin;
using MyFilmsPlugin.DataBase;
using MyFilmsPlugin.MyFilmsGUI;

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
  internal class LatestMyFilmsHandler
  {
    #region declarations

    private Logger logger = LogManager.GetCurrentClassLogger();
    private bool _isGetTypeRunningOnThisThread /* = false*/;
//        private VideoHandler episodePlayer = null;

    private LatestsCollection latestMyFilms;
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
    private int lastFocusedId = 0;

    #endregion
    
    public const int ControlID = 919199880;
    public const int Play1ControlID = 91919988;
    public const int Play2ControlID = 91919989;
    public const int Play3ControlID = 91919990;
    public const int Play4ControlID = 91919906;

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

    private bool IsGetTypeRunningOnThisThread
    {
      get { return _isGetTypeRunningOnThisThread; }
      set { _isGetTypeRunningOnThisThread = value; }
    }

    internal LatestMyFilmsHandler()
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
        if (LatestMediaHandlerSetup.LatestMyFilmsWatched.Equals("False", StringComparison.CurrentCulture))
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

        pItem = new GUIListItem();
        pItem.Label = Translation.Update;
        pItem.ItemId = 3;
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
            LatestMediaHandlerSetup.LatestMyFilmsWatched = (LatestMediaHandlerSetup.LatestMyFilmsWatched.Equals("False", StringComparison.CurrentCulture)) ? "True" : "False";
            MyFilmsUpdateLatest();
            break;
          }
          case 3:
          {
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
      latestMyFilms = new LatestsCollection();
      latestMovies = new Hashtable();

      try
      {
        int i0 = 1;
        int x0 = 0;

        List<MFMovie> movies = BaseMesFilms.GetMostRecent(BaseMesFilms.MostRecentType.Added, 999, Utils.FacadeMaxNum, LatestMediaHandlerSetup.LatestMyFilmsWatched.Equals("True", StringComparison.CurrentCulture));
        Utils.HasNewMyFilms = false;
        if (movies != null)
        {
          foreach (MFMovie movie in movies)
          {
            string thumb = movie.Picture;
            if (string.IsNullOrEmpty(thumb))
              thumb = "DefaultFolderBig.png";
            string tDate = movie.DateAdded;
            bool isnew = false;
            try
            {
              DateTime dTmp = DateTime.Parse(tDate);
              tDate = String.Format("{0:" + LatestMediaHandlerSetup.DateFormat + "}", dTmp);

              isnew = (dTmp > Utils.NewDateTime) && (movie.WatchedCount <= 0);
              if (isnew)
                Utils.HasNewMyFilms = true;
            }
            catch
            {   }

            string fbanner = UtilsFanartHandler.GetFanartTVForLatestMedia(string.Empty, string.Empty, movie.IMDBNumber, Utils.FanartTV.MoviesBanner);
            string fclearart = UtilsFanartHandler.GetFanartTVForLatestMedia(string.Empty, string.Empty, movie.IMDBNumber, Utils.FanartTV.MoviesClearArt);
            string fclearlogo = UtilsFanartHandler.GetFanartTVForLatestMedia(string.Empty, string.Empty, movie.IMDBNumber, Utils.FanartTV.MoviesClearLogo);
            string fcd = UtilsFanartHandler.GetFanartTVForLatestMedia(string.Empty, string.Empty, movie.IMDBNumber, Utils.FanartTV.MoviesCDArt);

            latestMyFilms.Add(new Latest(tDate, thumb, movie.Fanart, movie.Title, 
                                     null, null, null, 
                                     movie.Category,
                                     movie.Rating.ToString(),
                                     Math.Round(movie.Rating, MidpointRounding.AwayFromZero).ToString(CultureInfo.CurrentCulture), 
                                     null,
                                     movie.Length.ToString(), movie.Year.ToString(), 
                                     null, null, null, 
                                     movie, movie.ID.ToString(), 
                                     null, null,
                                     fbanner, fclearart, fclearlogo, fcd,
                                     isnew));

            latestMovies.Add(i0, movie);
            Utils.ThreadToSleep();

            i0++;
            x0++;
            if (i0 == Utils.FacadeMaxNum)
              break;
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
      return latestMyFilms;
    }

    public Hashtable GetLatestsList()
    {
      Hashtable ht = new Hashtable();
      if (latestMyFilms != null)
      {
        for (int i = 0; i < latestMyFilms.Count; i++)
        {
          if (!ht.Contains(latestMyFilms[i].Id))
          {
            // logger.Debug("Make Latest List: MyFilms: " + latestMyFilms[i].Id + " - " + latestMyFilms[i].Title);
            ht.Add(latestMyFilms[i].Id, latestMyFilms[i].Title) ;
          }
        }
      }
      return ht;
    }

    private void AddToFilmstrip(Latest latests, int x)
    {
      try
      {
        //Add to filmstrip
        IMDBMovie movie = new IMDBMovie();
        movie.Title = latests.Title;
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
        LatestsToFilmStrip(latestMyFilms);

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
          int i = item.ItemId - 1;
          string plot = (string.IsNullOrEmpty(latestMyFilms[i].Summary) ? Translation.NoDescription : latestMyFilms[i].Summary);
          string plotoutline = Utils.GetSentences(plot, Utils.latestPlotOutlineSentencesNum);
          Utils.SetProperty("#latestMediaHandler.myfilms.selected.thumb", item.IconImageBig);
          Utils.SetProperty("#latestMediaHandler.myfilms.selected.title", item.Label);
          Utils.SetProperty("#latestMediaHandler.myfilms.selected.dateAdded", item.Label3);
          Utils.SetProperty("#latestMediaHandler.myfilms.selected.genre", item.Label2);
          Utils.SetProperty("#latestMediaHandler.myfilms.selected.roundedRating", "" + item.Rating);
          Utils.SetProperty("#latestMediaHandler.myfilms.selected.classification", "");
          Utils.SetProperty("#latestMediaHandler.myfilms.selected.runtime", "" + item.Duration);
          Utils.SetProperty("#latestMediaHandler.myfilms.selected.year", "" + item.Year);
          Utils.SetProperty("#latestMediaHandler.myfilms.selected.id", "" + item.ItemId);
          Utils.SetProperty("#latestMediaHandler.myfilms.selected.rating", latestMyFilms[i].Rating);
          Utils.SetProperty("#latestMediaHandler.myfilms.selected.plot", plot);
          Utils.SetProperty("#latestMediaHandler.myfilms.selected.plotoutline", plotoutline);
          Utils.SetProperty("#latestMediaHandler.myfilms.selected.banner", latestMyFilms[i].Banner);
          Utils.SetProperty("#latestMediaHandler.myfilms.selected.clearart", latestMyFilms[i].ClearArt);
          Utils.SetProperty("#latestMediaHandler.myfilms.selected.clearlogo", latestMyFilms[i].ClearLogo);
          Utils.SetProperty("#latestMediaHandler.myfilms.selected.cd", latestMyFilms[i].CD);
          Utils.SetProperty("#latestMediaHandler.myfilms.selected.new", latestMyFilms[i].New);
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
              Utils.SetProperty("#latestMediaHandler.myfilms.selected.fanart1", _image);
              Utils.SetProperty("#latestMediaHandler.myfilms.selected.showfanart1", "true");
              Utils.SetProperty("#latestMediaHandler.myfilms.selected.showfanart2", "false");
              Thread.Sleep(1000);
              Utils.SetProperty("#latestMediaHandler.myfilms.selected.fanart2", "");
              showFanart = 2;
            }
            else
            {
              Utils.SetProperty("#latestMediaHandler.myfilms.selected.fanart2", _image);
              Utils.SetProperty("#latestMediaHandler.myfilms.selected.showfanart2", "true");
              Utils.SetProperty("#latestMediaHandler.myfilms.selected.showfanart1", "false");
              Thread.Sleep(1000);
              Utils.SetProperty("#latestMediaHandler.myfilms.selected.fanart1", "");
              showFanart = 1;
            }
            Utils.UnLoadImage(_image, ref images);
            selectedFacadeItem2 = _id;
          }
        }
        else
        {
          Utils.SetProperty("#latestMediaHandler.myfilms.selected.fanart1", " ");
          Utils.SetProperty("#latestMediaHandler.myfilms.selected.fanart2", " ");
          Utils.SetProperty("#latestMediaHandler.myfilms.selected.showfanart1", "false");
          Utils.SetProperty("#latestMediaHandler.myfilms.selected.showfanart2", "false");
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

    internal bool PlayMovie(GUIWindow fWindow)
    {
      try
      {
        /*
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
        */
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
        logger.Error("Unable to play film! " + ex.ToString());
        return true;
      }
      return false;
    }

    internal void PlayMovie(int index)
    {
      MFMovie mov = (MFMovie) latestMovies[index];
      string loadingParameter = string.Format("config:{0}|movieid:{1}|play:{2}", mov.Config, mov.ID, "true");
      GUIWindowManager.ActivateWindow(7986, loadingParameter);
    }

    internal void SetupMovieLatest()
    {
      GUIWindowManager.Receivers += new SendMessageHandler(OnMessage);
      MyFilms.ImportComplete += new MyFilms.ImportCompleteEventDelegate(OnImportComplete);
    }

    internal void DisposeMovieLatest()
    {
      GUIWindowManager.Receivers -= new SendMessageHandler(OnMessage);
      MyFilms.ImportComplete -= new MyFilms.ImportCompleteEventDelegate(OnImportComplete);
    }

    internal void OnImportComplete()
    {
      MyFilmsUpdateLatestThread();
    }

    private void OnMessage(GUIMessage message)
    {
      Utils.ThreadToSleep();
      if (LatestMediaHandlerSetup.LatestMyFilms.Equals("True", StringComparison.CurrentCulture))
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
                MyFilmsUpdateLatestThread();
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

    internal void EmptyLatestMediaPropsMyFilms()
    {
      Utils.SetProperty("#latestMediaHandler.myfilms.label", Translation.LabelLatestAdded);
      Utils.SetProperty("#latestMediaHandler.myfilms.latest.enabled", "false");
      Utils.SetProperty("#latestMediaHandler.myfilms.hasnew", "false");
      for (int z = 1; z < 4; z++)
      {
        Utils.SetProperty("#latestMediaHandler.myfilms.latest" + z + ".poster", string.Empty);
        Utils.SetProperty("#latestMediaHandler.myfilms.latest" + z + ".fanart", string.Empty);
        Utils.SetProperty("#latestMediaHandler.myfilms.latest" + z + ".title", string.Empty);
        Utils.SetProperty("#latestMediaHandler.myfilms.latest" + z + ".dateAdded", string.Empty);
        Utils.SetProperty("#latestMediaHandler.myfilms.latest" + z + ".rating", string.Empty);
        Utils.SetProperty("#latestMediaHandler.myfilms.latest" + z + ".roundedRating", string.Empty);
        Utils.SetProperty("#latestMediaHandler.myfilms.latest" + z + ".year", string.Empty);
        Utils.SetProperty("#latestMediaHandler.myfilms.latest" + z + ".id", string.Empty);
        Utils.SetProperty("#latestMediaHandler.myfilms.latest" + z + ".plot", string.Empty);
        Utils.SetProperty("#latestMediaHandler.myfilms.latest" + z + ".plotoutline", string.Empty);
        Utils.SetProperty("#latestMediaHandler.myfilms.latest" + z + ".banner", string.Empty);
        Utils.SetProperty("#latestMediaHandler.myfilms.latest" + z + ".clearart", string.Empty);
        Utils.SetProperty("#latestMediaHandler.myfilms.latest" + z + ".clearlogo", string.Empty);
        Utils.SetProperty("#latestMediaHandler.myfilms.latest" + z + ".cd", string.Empty);
        Utils.SetProperty("#latestMediaHandler.myfilms.latest" + z + ".new", "false");
      }
    }

    internal void MyFilmsUpdateLatestThread()
    {
      // MyFilms
      if (LatestMediaHandlerSetup.LatestMyFilms.Equals("True", StringComparison.CurrentCulture))
      {
        try
        {
          RefreshWorker MyRefreshWorker = new RefreshWorker();
          MyRefreshWorker.RunWorkerCompleted += MyRefreshWorker.OnRunWorkerCompleted;
          MyRefreshWorker.RunWorkerAsync(this);
        }
        catch (Exception ex)
        {
          logger.Error("MyFilmsUpdateLatestThread: " + ex.ToString());
        }
      }
    }

    internal void MyFilmsUpdateLatest()
    {
      int sync = Interlocked.CompareExchange(ref Utils.SyncPointMyFilmsUpdate, 1, 0);
      if (sync != 0)
        return;

      if (!LatestMediaHandlerSetup.LatestMyFilms.Equals("True", StringComparison.CurrentCulture))
      {
        EmptyLatestMediaPropsMyFilms();
        return;
      }

      // My Films
      try
      {
        LatestsCollection ht = GetLatestMovies();
        EmptyLatestMediaPropsMyFilms();
        if (ht != null)
        {
          int z = 1;
          for (int i = 0; i < ht.Count && i < Utils.LatestsMaxNum; i++)
          {
            logger.Info("Updating Latest Media Info: MyFilms: Films " + z + ": " + ht[i].Title);

            string plot = (string.IsNullOrEmpty(ht[i].Summary) ? Translation.NoDescription : ht[i].Summary);
            string plotoutline = Utils.GetSentences(plot, Utils.latestPlotOutlineSentencesNum);
            Utils.SetProperty("#latestMediaHandler.myfilms.latest" + z + ".poster", ht[i].Thumb); //  _al.Add(ht[i].Fanart);                                                
            Utils.SetProperty("#latestMediaHandler.myfilms.latest" + z + ".fanart", ht[i].Fanart);
            Utils.SetProperty("#latestMediaHandler.myfilms.latest" + z + ".title", ht[i].Title);
            Utils.SetProperty("#latestMediaHandler.myfilms.latest" + z + ".dateAdded", ht[i].DateAdded);
            Utils.SetProperty("#latestMediaHandler.myfilms.latest" + z + ".rating", ht[i].Rating);
            Utils.SetProperty("#latestMediaHandler.myfilms.latest" + z + ".roundedRating", ht[i].RoundedRating);
            Utils.SetProperty("#latestMediaHandler.myfilms.latest" + z + ".year", ht[i].Year);
            Utils.SetProperty("#latestMediaHandler.myfilms.latest" + z + ".id", ht[i].Id);
            Utils.SetProperty("#latestMediaHandler.myfilms.latest" + z + ".genre", ht[i].Genre);
            Utils.SetProperty("#latestMediaHandler.myfilms.latest" + z + ".runtime", ht[i].Runtime);
            Utils.SetProperty("#latestMediaHandler.myfilms.latest" + z + ".plot", plot);
            Utils.SetProperty("#latestMediaHandler.myfilms.latest" + z + ".plotoutline", plotoutline);
            Utils.SetProperty("#latestMediaHandler.myfilms.latest" + z + ".banner", ht[i].Banner);
            Utils.SetProperty("#latestMediaHandler.myfilms.latest" + z + ".clearart", ht[i].ClearArt);
            Utils.SetProperty("#latestMediaHandler.myfilms.latest" + z + ".clearlogo", ht[i].ClearLogo);
            Utils.SetProperty("#latestMediaHandler.myfilms.latest" + z + ".cd", ht[i].CD);
            Utils.SetProperty("#latestMediaHandler.myfilms.latest" + z + ".new", ht[i].New);
            z++;
          }
          // ht.Clear();
          Utils.SetProperty("#latestMediaHandler.myfilms.hasnew", Utils.HasNewMyFilms ? "true" : "false");
          logger.Debug("Updating Latest Media Info: MyFilms: Has new: " + (Utils.HasNewMyFilms ? "true" : "false"));
        }
        // ht = null;
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

      if ((latestMyFilms != null) && (latestMyFilms.Count > 0))
      {
        // if (System.Windows.Forms.Form.ActiveForm.InvokeRequired)
        // {
        //   System.Windows.Forms.Form.ActiveForm.Invoke(InitFacade);
        // }
        // else
        // {
          InitFacade();
        // }
        Utils.SetProperty("#latestMediaHandler.myfilms.latest.enabled", "true");
      }
      else
        EmptyLatestMediaPropsMyFilms();
      Utils.UpdateLatestsUpdate(Utils.LatestsCategory.MyFilms, DateTime.Now);
      Utils.SyncPointMyFilmsUpdate=0;
    }

    internal void UpdateImageTimer(GUIWindow fWindow, Object stateInfo, ElapsedEventArgs e)
    {
      if (LatestMediaHandlerSetup.LatestMyFilms.Equals("True", StringComparison.CurrentCulture))
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
              Utils.SetProperty("#latestMediaHandler.myfilms.selected.fanart1", " ");
              Utils.SetProperty("#latestMediaHandler.myfilms.selected.fanart2", " ");
              Utils.UnLoadImage(ref images);
              ShowFanart = 1;
              SelectedFacadeItem2 = -1;
              SelectedFacadeItem2 = -1;
              NeedCleanup = false;
              NeedCleanupCount = 0;
            }
            else if (NeedCleanup && NeedCleanupCount == 0)
            {
              Utils.SetProperty("#latestMediaHandler.myfilms.selected.showfanart1", "false");
              Utils.SetProperty("#latestMediaHandler.myfilms.selected.showfanart2", "false");
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
          logger.Error("UpdateImageTimer (myfilms): " + ex.ToString());
        }
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
