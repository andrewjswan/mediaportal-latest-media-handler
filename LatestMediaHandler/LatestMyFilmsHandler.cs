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

using LMHNLog.NLog;

using MediaPortal.Dialogs;
using MediaPortal.GUI.Library;
using MediaPortal.Video.Database;

using MyFilmsPlugin;
using MyFilmsPlugin.DataBase;
using MyFilmsPlugin.MyFilmsGUI;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace LatestMediaHandler
{
  internal class LatestMyFilmsHandler
  {
    #region declarations

    private Logger logger = LogManager.GetCurrentClassLogger();

    private LatestsCollection latestMyFilms;
    private Hashtable latestMovies;

    private ArrayList facadeCollection = new ArrayList();
    internal ArrayList images = new ArrayList();
    internal ArrayList imagesThumbs = new ArrayList();

    private int showFanart = 1;
    private bool needCleanup = false;
    private int needCleanupCount = 0;
    private int currentFacade = 0;

    private static Object lockObject = new object();

    #endregion

    public const int ControlID = 919199880;
    public const int Play1ControlID = 91919988;
    public const int Play2ControlID = 91919989;
    public const int Play3ControlID = 91919990;
    public const int Play4ControlID = 91919906;

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

    internal LatestMyFilmsHandler(int id = ControlID)
    {
      ControlIDFacades = new List<LatestsFacade>();
      ControlIDPlays = new List<int>();
      //
      ControlIDFacades.Add(new LatestsFacade(id, "MyFilms"));
      if (id == ControlID)
      {
        ControlIDPlays.Add(Play1ControlID);
        ControlIDPlays.Add(Play2ControlID);
        ControlIDPlays.Add(Play3ControlID);
        ControlIDPlays.Add(Play4ControlID);
      }
      CurrentFacade.UnWatched = Utils.LatestMyFilmsWatched;

      Utils.ClearSelectedMovieProperty(CurrentFacade);
      EmptyLatestMediaProperties();
    }

    internal LatestMyFilmsHandler(LatestsFacade facade) : this (facade.ControlID)
    {
      ControlIDFacades[ControlIDFacades.Count - 1] = facade;
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
        if (CurrentFacade.UnWatched)
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

        //Add Latests/Watched/Rated Menu Item
        pItem = new GUIListItem("[^] " + CurrentFacade.Title);
        dlg.Add(pItem);
        pItem.ItemId = 3;

        pItem = new GUIListItem();
        pItem.Label = Translation.Update;
        pItem.ItemId = 4;
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
            CurrentFacade.UnWatched = !CurrentFacade.UnWatched;
            MyFilmsUpdateLatest();
            break;
          }
          case 3:
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
            MyFilmsUpdateLatest();
            break;
          }
          case 4:
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
        List<MFMovie> movies = null;
        if (CurrentFacade.Type == LatestsFacadeType.Watched)
        {
          movies = BaseMesFilms.GetMostRecent(BaseMesFilms.MostRecentType.Watched, 999, Utils.FacadeMaxNum);
        }
        else if (CurrentFacade.Type == LatestsFacadeType.Rated)
        {
          movies = BaseMesFilms.GetMostRecent(BaseMesFilms.MostRecentType.Added, 999, 999);
        }
        else
        {
          movies = BaseMesFilms.GetMostRecent(BaseMesFilms.MostRecentType.Added, 999, Utils.FacadeMaxNum, CurrentFacade.UnWatched);
        }

        CurrentFacade.HasNew = false;
        if (movies != null)
        {
          int i0 = 1;
          foreach (MFMovie movie in movies)
          {
            if (CurrentFacade.Type == LatestsFacadeType.Rated)
            {
              if (movie.Rating == 0.0)
              {
                continue;
              }
            }

            string thumb = movie.Picture;
            if (string.IsNullOrEmpty(thumb))
            {
              thumb = "DefaultVideoBig.png"; // "DefaultFolderBig.png";
            }

            DateTime dTmp = DateTime.MinValue;
            bool isnew = false;
            try
            {
              dTmp = DateTime.Parse(movie.DateAdded);
              isnew = (dTmp > Utils.NewDateTime) && (movie.WatchedCount <= 0);
              if (isnew)
              {
                CurrentFacade.HasNew = true;
              }
            }
            catch
            {
              isnew = false;
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
                () => fbanner = UtilsFanartHandler.GetFanartTVForLatestMedia(movie.IMDBNumber, string.Empty, string.Empty, Utils.FanartTV.MoviesBanner),
                () => fclearart = UtilsFanartHandler.GetFanartTVForLatestMedia(movie.IMDBNumber, string.Empty, string.Empty, Utils.FanartTV.MoviesClearArt),
                () => fclearlogo = UtilsFanartHandler.GetFanartTVForLatestMedia(movie.IMDBNumber, string.Empty, string.Empty, Utils.FanartTV.MoviesClearLogo),
                () => fcd = UtilsFanartHandler.GetFanartTVForLatestMedia(movie.IMDBNumber, string.Empty, string.Empty, Utils.FanartTV.MoviesCDArt),
                () => aposter = UtilsFanartHandler.GetAnimatedForLatestMedia(movie.IMDBNumber, string.Empty, string.Empty, Utils.Animated.MoviesPoster),
                () => abg = UtilsFanartHandler.GetAnimatedForLatestMedia(movie.IMDBNumber, string.Empty, string.Empty, Utils.Animated.MoviesBackground)
              );
            }

            latestMyFilms.Add(new Latest()
            {
              DateTimeAdded = dTmp,
              DateTimeWatched = dTmp,
              Title = movie.Title,
              Genre = movie.Category,
              Thumb = thumb,
              Fanart = movie.Fanart,
              Rating = movie.Rating.ToString(),
              Runtime = movie.Length.ToString(),
              Year = movie.Year.ToString(),
              Banner = fbanner,
              ClearArt = fclearart,
              ClearLogo = fclearlogo,
              CD = fcd,
              AnimatedPoster = aposter,
              AnimatedBackground = abg,
              Playable = movie,
              Id = movie.ID.ToString(),
              DBId = movie.IMDBNumber,
              IsNew = isnew
            });

            latestMovies.Add(i0, movie);
            Utils.ThreadToSleep();

            i0++;
            if (i0 == Utils.FacadeMaxNum)
              break;
          }
        }
      }
      catch (Exception ex)
      {
        logger.Error("GetLatestMovies: " + ex.ToString());
      }

      Utils.SortLatests(ref latestMyFilms, CurrentFacade.Type, CurrentFacade.LeftToRight);

      if (latestMyFilms != null && !MainFacade)
      {
        logger.Debug("GetLatest: " + this.ToString() + ":" + CurrentFacade.ControlID + " - " + latestMyFilms.Count);
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

    public List<MQTTItem> GetMQTTLatestsList()
    {
      List<MQTTItem> ht = new List<MQTTItem>();
      if (latestMyFilms != null)
      {
        for (int i = 0; i < latestMyFilms.Count; i++)
        {
          ht.Add(new MQTTItem(latestMyFilms[i]));
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
      if (!Utils.LatestMyFilms)
      {
        return;
      }

      try
      {
        lock (lockObject)
        {
          // LatestsToFilmStrip(latestMyFilms);

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
      if (!Utils.LatestMyFilms)
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
          Utils.FillSelectedMovieProperty(CurrentFacade, item, latestMyFilms[item.ItemId - 1]);

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
              Utils.SetProperty("#latestMediaHandler.myfilms.selected.fanart1", _image);
              Utils.SetProperty("#latestMediaHandler.myfilms.selected.showfanart1", "true");
              Utils.SetProperty("#latestMediaHandler.myfilms.selected.showfanart2", "false");
              Thread.Sleep(1000);
              Utils.SetProperty("#latestMediaHandler.myfilms.selected.fanart2", string.Empty);
              showFanart = 2;
            }
            else
            {
              Utils.SetProperty("#latestMediaHandler.myfilms.selected.fanart2", _image);
              Utils.SetProperty("#latestMediaHandler.myfilms.selected.showfanart2", "true");
              Utils.SetProperty("#latestMediaHandler.myfilms.selected.showfanart1", "false");
              Thread.Sleep(1000);
              Utils.SetProperty("#latestMediaHandler.myfilms.selected.fanart1", string.Empty);
              showFanart = 1;
            }
            // Utils.UnLoadImage(_image, ref images);
            CurrentFacade.SelectedImage = _id;
          }
        }
        else
        {
          Utils.SetProperty("#latestMediaHandler.myfilms.selected.fanart1", " ");
          Utils.SetProperty("#latestMediaHandler.myfilms.selected.fanart2", " ");
          Utils.SetProperty("#latestMediaHandler.myfilms.selected.showfanart1", "false");
          Utils.SetProperty("#latestMediaHandler.myfilms.selected.showfanart2", "false");
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

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal void SetupReceivers()
    {
      if (!Utils.LatestMyFilms)
      {
        return;
      }

      try
      {
        GUIWindowManager.Receivers += new SendMessageHandler(OnMessage);
        MyFilms.ImportComplete += new MyFilms.ImportCompleteEventDelegate(OnImportComplete);
      }
      catch (FileNotFoundException) { }
      catch (MissingMethodException) { }
      catch (Exception ex)
      {
        logger.Error("DisposeMovieLatest: " + ex.ToString());
      }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal void DisposeReceivers()
    {
      if (!Utils.LatestMyFilms)
      {
        return;
      }

      try
      {
        GUIWindowManager.Receivers -= new SendMessageHandler(OnMessage);
        MyFilms.ImportComplete -= new MyFilms.ImportCompleteEventDelegate(OnImportComplete);
      }
      catch (FileNotFoundException) { }
      catch (MissingMethodException) { }
      catch (Exception ex)
      {
        logger.Error("DisposeMovieLatest: " + ex.ToString());
      }
    }

    internal void OnImportComplete()
    {
      GetLatestMediaInfoThread();
    }

    private void OnMessage(GUIMessage message)
    {
      if (Utils.LatestMyFilms)
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

    internal void EmptyLatestMediaProperties()
    {
      if (!MainFacade && !CurrentFacade.AddProperties)
      {
        Utils.SetProperty("#latestMediaHandler." + CurrentFacade.Handler.ToLowerInvariant() + (!MainFacade ? ".info." + CurrentFacade.ControlID.ToString() : string.Empty) + ".latest.enabled", "false");
        return;
      }

      Utils.ClearLatestsMovieProperty(CurrentFacade, MainFacade);
      for (int z = 1; z <= Utils.LatestsMaxNum; z++)
      {
        Utils.SetProperty("#latestMediaHandler.myfilms" + (!MainFacade ? ".info." + CurrentFacade.ControlID.ToString() : string.Empty) + ".latest" + z + ".poster", string.Empty);
      }
    }

    internal void GetLatestMediaInfoThread()
    {
      // MyFilms
      if (Utils.LatestMyFilms)
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
      if (Interlocked.CompareExchange(ref CurrentFacade.Update, 1, 0) != 0)
        return;

      if (!Utils.LatestMyFilms)
      {
        EmptyLatestMediaProperties();
        CurrentFacade.Update = 0;
        return;
      }

      // My Films
      LatestsCollection hTable = GetLatestMovies();
      LatestsToFilmStrip(latestMyFilms);

      if (MainFacade || CurrentFacade.AddProperties)
      {
        EmptyLatestMediaProperties();

        Utils.FillLatestsMovieProperty(CurrentFacade,hTable, MainFacade);
        if (hTable != null)
        {
          int z = 1;
          for (int i = 0; i < hTable.Count && i < Utils.LatestsMaxNum; i++)
          {
            Utils.SetProperty("#latestMediaHandler.myfilms" + (!MainFacade ? ".info." + CurrentFacade.ControlID.ToString() : string.Empty) + ".latest" + z + ".poster", hTable[i].Thumb);
            z++;
          }
        }
      }

      if ((latestMyFilms != null) && (latestMyFilms.Count > 0))
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
        Utils.UpdateLatestsUpdate(Utils.LatestsCategory.MyFilms, DateTime.Now);
      }

      CurrentFacade.Update = 0;
    }

    internal void UpdateImageTimer(GUIWindow fWindow, Object stateInfo, ElapsedEventArgs e)
    {
      if (Utils.LatestMyFilms)
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
              Utils.SetProperty("#latestMediaHandler.myfilms.selected.fanart1", " ");
              Utils.SetProperty("#latestMediaHandler.myfilms.selected.fanart2", " ");
              Utils.UnLoadImages(ref images);
              ShowFanart = 1;
              CurrentFacade.SelectedImage = -1;
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

  }
}
