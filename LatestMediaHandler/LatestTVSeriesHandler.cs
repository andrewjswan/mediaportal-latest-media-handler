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
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Globalization;
using System.Threading;
using System.Timers;

using RealNLog.NLog;

//using MediaPortal.Configuration;
using MediaPortal.GUI.Library;
using MediaPortal.Dialogs;
using MediaPortal.Video.Database;
//using MediaPortal.Util;
//using MediaPortal.Player;
//using MediaPortal.Playlists;

//using Cornerstone.Database;
//using Cornerstone.Database.Tables;

using WindowPlugins.GUITVSeries;

namespace LatestMediaHandler
{
  internal class LatestTVSeriesHandler
  {
    #region declarations

    private Logger logger = LogManager.GetCurrentClassLogger();

    private bool _isGetTypeRunningOnThisThread /* = false*/;

    private VideoHandler episodePlayer = null;
    private GUIFacadeControl facade = null;

    private int tVSeriesCount = 0;
    private LatestsCollection latestTVSeries = null;
    private Hashtable latestTVSeriesEpisodes;

    private ArrayList al = new ArrayList();
    internal ArrayList images = new ArrayList();
    internal ArrayList imagesThumbs = new ArrayList();
    //internal ArrayList thumbs = new ArrayList();

    private int selectedFacadeItem1 = -1;
    private int selectedFacadeItem2 = -1;
    private int showFanart = 1;
    private bool needCleanup = false;
    private int needCleanupCount = 0;
    private int lastFocusedId = 0;

    private Types currentType = Types.Latest;
    private ResultTypes resultType = ResultTypes.Episodes;

    #endregion

    public const int ControlID = 919199940;
    public const int Play1ControlID = 91919994;
    public const int Play2ControlID = 91919995;
    public const int Play3ControlID = 91919996;

    public List<int> ControlIDFacades;
    public List<int> ControlIDPlays;

    internal Types CurrentType
    {
      get { return currentType; }
      set { currentType = value; }
    }

    public int LastFocusedId
    {
      get { return lastFocusedId; }
      set { lastFocusedId = value; }
    }

    internal enum Types
    {
      Latest,
      Watched
    }

    internal enum ResultTypes
    {
      Episodes,
      Seasons,
      Series
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

    /* public ArrayList Thumbs
        {
            get { return thumbs; }
            set { thumbs = value; }
        }*/

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

    public LatestTVSeriesHandler()
    {
      currentType = Types.Latest;
      resultType = ResultTypes.Episodes;

      ControlIDFacades = new List<int>();
      ControlIDPlays = new List<int>();
      //
      ControlIDFacades.Add(ControlID);
      ControlIDPlays.Add(Play1ControlID);
      ControlIDPlays.Add(Play2ControlID);
      ControlIDPlays.Add(Play3ControlID);
    }

    internal void MyContextMenu()
    {
      try
      {
        IDialogbox dlg = (GUIDialogMenu) GUIWindowManager.GetWindow((int) GUIWindow.Window.WINDOW_DIALOG_MENU);
        if (dlg == null) return;

        dlg.Reset();
        dlg.SetHeading(924);

        //Add Details Menu Item
        //Play Menu Item
        GUIListItem pItem = new GUIListItem(Translation.Play);
        dlg.Add(pItem);
        pItem.ItemId = 1;

        pItem = new GUIListItem(Translation.EpisodeDetails);
        dlg.Add(pItem);
        pItem.ItemId = 2;

        //Add Display Menu Item
        if (CurrentType == Types.Latest)
        {
          pItem = new GUIListItem(Translation.DisplayNextEpisodes);
          dlg.Add(pItem);
          pItem.ItemId = 3;
        }
        else
        {
          pItem = new GUIListItem(Translation.DisplayLatestEpisodes);
          dlg.Add(pItem);
          pItem.ItemId = 3;
        }

        //Add Watched/Unwatched Filter Menu Item
        if (LatestMediaHandlerSetup.LatestTVSeriesWatched.Equals("False", StringComparison.CurrentCulture))
        {
          pItem = new GUIListItem(Translation.ShowUnwatchedEpisodes);
          dlg.Add(pItem);
          pItem.ItemId = 4;
        }
        else
        {
          pItem = new GUIListItem(Translation.ShowAllEpisodes);
          dlg.Add(pItem);
          pItem.ItemId = 4;
        }

        //Show Dialog
        dlg.DoModal(GUIWindowManager.ActiveWindow);

        if (dlg.SelectedLabel == -1)
          return;

        switch (dlg.SelectedId)
        {
          case 1:
          {
            PlayTVSeries(GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow));
            break;
          }
          case 2:
          {
            ShowInfo();
            break;
          }
          case 3:
          {
            CurrentType = (CurrentType == Types.Latest) ? Types.Watched : Types.Latest;
            TVSeriesUpdateLatest(CurrentType, LatestMediaHandlerSetup.LatestTVSeriesWatched.Equals("True", StringComparison.CurrentCulture));
            break;
          }
          case 4:
          {
            LatestMediaHandlerSetup.LatestTVSeriesWatched = LatestMediaHandlerSetup.LatestTVSeriesWatched.Equals("True", StringComparison.CurrentCulture) ? "False" : "True" ;
            TVSeriesUpdateLatest(CurrentType, LatestMediaHandlerSetup.LatestTVSeriesWatched.Equals("True", StringComparison.CurrentCulture));
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
        if (idx > 0)
        {
          string sHyp = "seriesid:" + latestTVSeries[idx].SeriesIndex + 
                        ((resultType == ResultTypes.Seasons || resultType == ResultTypes.Episodes) ? "|seasonidx:" + Utils.RemoveLeadingZeros(latestTVSeries[idx].SeasonIndex) : "") +
                        ((resultType == ResultTypes.Episodes) ? "|episodeidx:" + Utils.RemoveLeadingZeros(latestTVSeries[idx].EpisodeIndex) : "");
          GUIWindowManager.ActivateWindow(9811, sHyp, false);
        }
      }
      catch (Exception ex)
      {
        logger.Error("ShowInfo: " + ex.ToString());
      }
    }

    /// <summary>
    /// Returns latest added tvseries thumbs from TVSeries db.
    /// </summary>
    /// <param name="type">Type of data to fetch</param>
    /// <returns>Resultset of matching data</returns>
    private LatestsCollection GetLatestTVSeries(Types type, bool onlyNew)
    {
      latestTVSeries = new LatestsCollection();
      latestTVSeriesEpisodes = new Hashtable();

      try
      {
        int i0 = 1;
        int x = 0;

        List<DBEpisode> episodes = null;
        if (type == Types.Latest)
        {
          episodes = DBEpisode.GetMostRecent(MostRecentType.Created, 300, 3*Utils.FacadeMaxNum, onlyNew);
        }
        else
        {
          episodes = DBEpisode.GetNextWatchingEpisodes(Utils.FacadeMaxNum);
        }

        Utils.HasNewTVSeries = false ;
        if (episodes != null)
        {
          foreach (DBEpisode episode in episodes)
          {
            DBSeries series = Helper.getCorrespondingSeries(episode[DBEpisode.cSeriesID]);
            if (series != null)
            {
              string contentRating = series[DBOnlineSeries.cContentRating];

              if (contentRating != null && LatestMediaHandlerSetup.LatestTVSeriesRatings.Contains(contentRating))
              {
                string episodeTitle = episode[DBEpisode.cEpisodeName];
                string seriesIdx = episode[DBEpisode.cSeriesID];
                string seasonIdx = episode[DBEpisode.cSeasonIndex];
                string episodeIdx = episode[DBEpisode.cEpisodeIndex];
                if (seasonIdx != null)
                  seasonIdx = seasonIdx.PadLeft(2, '0');
                if (episodeIdx != null)
                  episodeIdx = episodeIdx.PadLeft(2, '0');
                string seriesTitle = series.ToString();
                string thumb = episode.Image.Trim(); // ImageAllocator.GetEpisodeImage(episode);
                string thumbSeries = ImageAllocator.GetSeriesPosterAsFilename(series).Trim(); //ImageAllocator.GetSeriesPoster(series,false);
                string fanart = Fanart.getFanart(episode[DBEpisode.cSeriesID]).FanartFilename;
                string dateAdded = episode[DBEpisode.cFileDateAdded];
                bool isnew = false;
                try
                {
                  DateTime dTmp = DateTime.Parse(dateAdded);
                  dateAdded = String.Format("{0:" + LatestMediaHandlerSetup.DateFormat + "}", dTmp);

                  isnew = ((dTmp > Utils.NewDateTime) && (string.IsNullOrEmpty(episode[DBEpisode.cDateWatched])));
                  if (isnew)
                    Utils.HasNewTVSeries = true;
                }
                catch
                {   }
                string seriesGenre = series[DBOnlineSeries.cGenre];
                string episodeRating = episode[DBOnlineEpisode.cRating];
                string episodeRuntime = episode[DBEpisode.cPrettyPlaytime];
                string episodeFirstAired = episode[DBOnlineEpisode.cFirstAired];
                try
                {
                  DateTime dTmp = DateTime.Parse(episodeFirstAired);
                  episodeFirstAired = String.Format("{0:" + LatestMediaHandlerSetup.DateFormat + "}", dTmp);
                }
                catch
                {   }
                string episodeSummary = episode[DBOnlineEpisode.cEpisodeSummary];
                System.Globalization.CultureInfo ci = System.Globalization.CultureInfo.InstalledUICulture;
                System.Globalization.NumberFormatInfo ni = (System.Globalization.NumberFormatInfo) ci.NumberFormat.Clone();
                ni.NumberDecimalSeparator = ".";
                string mathRoundToString = string.Empty;
                if (episodeRating != null && episodeRating.Length > 0)
                {
                  try
                  {
                    episodeRating = episodeRating.Replace(",", ".");
                    mathRoundToString = Math.Round(double.Parse(episodeRating, ni), MidpointRounding.AwayFromZero).ToString(CultureInfo.CurrentCulture);
                  }
                  catch
                  {   }
                }
                if (thumb == null || thumb.Length < 1)
                  thumb = "DefaultFolderBig.png";

                latestTVSeries.Add(new LatestMediaHandler.Latest(dateAdded, thumb, fanart, seriesTitle, episodeTitle, null,
                                                            null, seriesGenre, episodeRating, mathRoundToString, contentRating, episodeRuntime, episodeFirstAired,
                                                            seasonIdx, episodeIdx, thumbSeries, 
                                                            null, null, 
                                                            episodeSummary, seriesIdx, isnew));
                latestTVSeriesEpisodes.Add(i0, episode);
                Utils.ThreadToSleep();

                i0++;
                x++;
                if (x == Utils.FacadeMaxNum)
                  break;
              }
            }
            series = null;
          }

          if (episodes != null)
            episodes.Clear();
          episodes = null;
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
        logger.Error("GetLatestTVSeries: " + ex.ToString());
      }
      return latestTVSeries;
    }

    internal void EmptyLatestMediaPropsTVSeries()
    {
      LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.label", Translation.LabelLatestAdded);
      LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest.enabled", "false");
      LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.hasnew", "false");
      for (int z = 1; z < 4; z++)
      {
        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest" + z + ".thumb", string.Empty);
        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest" + z + ".serieThumb", string.Empty);
        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest" + z + ".fanart", string.Empty);
        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest" + z + ".serieName", string.Empty);
        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest" + z + ".seasonIndex", string.Empty);
        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest" + z + ".episodeName", string.Empty);
        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest" + z + ".episodeIndex", string.Empty);
        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest" + z + ".dateAdded", string.Empty);
        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest" + z + ".genre", string.Empty);
        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest" + z + ".rating", string.Empty);
        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest" + z + ".roundedRating", string.Empty);
        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest" + z + ".classification", string.Empty);
        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest" + z + ".runtime", string.Empty);
        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest" + z + ".firstAired", string.Empty);
        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest" + z + ".plot", string.Empty);
        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest" + z + ".new", "false");
      }
    }

    internal void TVSeriesUpdateLatest(Types type, bool onlyNew)
    {
      int sync = Interlocked.CompareExchange(ref Utils.SyncPointTVSeriesUpdate, 1, 0);
      if (sync != 0)
        return;

      if (!LatestMediaHandlerSetup.LatestTVSeries.Equals("True", StringComparison.CurrentCulture))
      {
        EmptyLatestMediaPropsTVSeries();
        return;
      }

      // TV Series
      try
      {
        LatestsCollection hTable = GetLatestTVSeries(type, onlyNew);
        EmptyLatestMediaPropsTVSeries();
        if (hTable != null)
        {
          int z = 1;
          for (int i = 0; i < hTable.Count && i < Utils.LatestsMaxNum; i++)
          {
            logger.Info("Updating Latest Media Info: TVSeries: Episode " + z + ": " + hTable[i].Title + " - " + hTable[i].Subtitle);

            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest" + z + ".thumb", hTable[i].Thumb);
            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest" + z + ".serieThumb", hTable[i].ThumbSeries); //  _al.Add(hTable[i].Fanart);                                                
            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest" + z + ".fanart", hTable[i].Fanart);
            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest" + z + ".serieName", hTable[i].Title);
            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest" + z + ".seasonIndex", hTable[i].SeasonIndex);
            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest" + z + ".episodeName", hTable[i].Subtitle);
            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest" + z + ".episodeIndex", hTable[i].EpisodeIndex);
            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest" + z + ".dateAdded", hTable[i].DateAdded);
            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest" + z + ".genre", hTable[i].Genre);
            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest" + z + ".rating", hTable[i].Rating);
            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest" + z + ".roundedRating", hTable[i].RoundedRating);
            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest" + z + ".classification", hTable[i].Classification);
            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest" + z + ".runtime", hTable[i].Runtime);
            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest" + z + ".firstAired", hTable[i].Year);
            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest" + z + ".plot", hTable[i].Summary);
            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest" + z + ".new", hTable[i].New);
            z++;
          }
          // hTable.Clear();
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.hasnew", Utils.HasNewTVSeries ? "true" : "false");
          logger.Debug("Updating Latest Media Info: TVSeries: Has new: " + (Utils.HasNewTVSeries ? "true" : "false"));
        }
        // hTable = null;
      }
      catch (Exception ex)
      {
        logger.Error("TVSeriesUpdateLatest: " + ex.ToString());
      }

      if ((latestTVSeries != null) && (latestTVSeries.Count > 0))
      {
        InitFacade();
        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest.enabled", "true");
      }
      else
        EmptyLatestMediaPropsTVSeries();
      Utils.SyncPointTVSeriesUpdate=0;
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

        Utils.LoadImage(latests.ThumbSeries, ref imagesThumbs);

        GUIListItem item = new GUIListItem();
        item.ItemId = x;
        item.IconImage = latests.ThumbSeries;
        item.IconImageBig = latests.ThumbSeries;
        item.ThumbnailImage = latests.ThumbSeries;
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
        LatestsToFilmStrip(latestTVSeries);

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
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.selected.thumb", latestTVSeries[(item.ItemId - 1)].Thumb);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.selected.serieThumb", latestTVSeries[(item.ItemId - 1)].ThumbSeries);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.selected.serieName", latestTVSeries[(item.ItemId - 1)].Title);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.selected.seasonIndex", latestTVSeries[(item.ItemId - 1)].SeasonIndex);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.selected.episodeName", latestTVSeries[(item.ItemId - 1)].Subtitle);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.selected.episodeIndex", latestTVSeries[(item.ItemId - 1)].EpisodeIndex);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.selected.dateAdded", latestTVSeries[(item.ItemId - 1)].DateAdded);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.selected.genre", latestTVSeries[(item.ItemId - 1)].Genre);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.selected.rating", latestTVSeries[(item.ItemId - 1)].Rating);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.selected.roundedRating", latestTVSeries[(item.ItemId - 1)].RoundedRating);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.selected.classification", latestTVSeries[(item.ItemId - 1)].Classification);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.selected.runtime", latestTVSeries[(item.ItemId - 1)].Runtime);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.selected.firstAired", latestTVSeries[(item.ItemId - 1)].Year);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.selected.plot", latestTVSeries[(item.ItemId - 1)].Summary);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.selected.new", latestTVSeries[(item.ItemId - 1)].New);
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
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.selected.fanart1", _image);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.selected.showfanart1", "true");
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.selected.showfanart2", "false");
              Thread.Sleep(1000);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.selected.fanart2", "");
              showFanart = 2;
            }
            else
            {
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.selected.fanart2", _image);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.selected.showfanart2", "true");
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.selected.showfanart1", "false");
              Thread.Sleep(1000);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.selected.fanart1", "");
              showFanart = 1;
            }
            Utils.UnLoadImage(_image, ref images);
            selectedFacadeItem2 = _id;
          }
        }
        else
        {
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.selected.fanart1", " ");
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.selected.fanart2", " ");
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.selected.showfanart1", "false");
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.selected.showfanart2", "false");
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

    internal bool PlayTVSeries(GUIWindow fWindow)
    {
      try
      {
        int FocusControlID = fWindow.GetFocusControlId();
        if (ControlIDPlays.Contains(FocusControlID))
        {
          PlayTVSeries(ControlIDPlays.IndexOf(FocusControlID)+1);
          return true;
        }
        //
        facade = Utils.GetLatestsFacade(ControlID);
        if (facade != null && facade.Focus && facade.SelectedListItem != null)
        {
          PlayTVSeries(facade.SelectedListItem.ItemId);
          return true;
        }
      }
      catch (Exception ex)
      {
        logger.Error("Unable to play episode! " + ex.ToString());
        return true;
      }
      return false;
    }

    internal void PlayTVSeries(int index)
    {
      if (episodePlayer == null)
      {
        episodePlayer = new VideoHandler();
      }

      episodePlayer.ResumeOrPlay((DBEpisode) latestTVSeriesEpisodes[index]);
    }

    internal void SetupTVSeriesLatest()
    {
      try
      {
        GUIWindowManager.Receivers += new SendMessageHandler(OnMessage);
        OnlineParsing.OnlineParsingCompleted += new OnlineParsing.OnlineParsingCompletedHandler(TVSeriesOnObjectInserted);
      }
      catch (Exception ex)
      {
        logger.Error("SetupTVSeriesLatest: " + ex.ToString());
      }
    }

    internal void DisposeTVSeriesLatest()
    {
      try
      {
        GUIWindowManager.Receivers -= new SendMessageHandler(OnMessage);
        OnlineParsing.OnlineParsingCompleted -= new OnlineParsing.OnlineParsingCompletedHandler(TVSeriesOnObjectInserted);
      }
      catch (Exception ex)
      {
        logger.Error("DisposeTVSeriesLatest: " + ex.ToString());
      }
    }

    internal void TVSeriesOnObjectInserted(bool dataUpdated)
    {
      try
      {
        if (ChangedEpisodeCount())
        {
          TVSeriesUpdateLatestThread();
        }
      }
      catch (Exception ex)
      {
        logger.Error("TVSeriesOnObjectInserted: " + ex.ToString());
      }
    }

    private void OnMessage(GUIMessage message)
    {
      if (LatestMediaHandlerSetup.LatestTVSeries.Equals("True", StringComparison.CurrentCulture))
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
                TVSeriesUpdateLatestThread();
                ChangedEpisodeCount();
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

    internal bool ChangedEpisodeCount()
    {
      bool b = false;

      try
      {
        SQLCondition condition = new SQLCondition();
        condition.Add(new DBEpisode(), DBEpisode.cFilename, string.Empty, SQLConditionType.NotEqual);

        List<DBEpisode> episodes = DBEpisode.Get(condition);

        if (tVSeriesCount != episodes.Count)
        {
          b = true;
          tVSeriesCount = episodes.Count;
        }
      }
      catch (Exception ex)
      {
        logger.Error("ChangedEpisodeCount: " + ex.ToString());
      }
      return b;
    }

    internal void TVSeriesUpdateLatestThread()
    {
      // TVSeries
      if (LatestMediaHandlerSetup.LatestTVSeries.Equals("True", StringComparison.CurrentCulture))
      {
        try
        {
          RefreshWorker MyRefreshWorker = new RefreshWorker();
          MyRefreshWorker.RunWorkerCompleted += MyRefreshWorker.OnRunWorkerCompleted;
          MyRefreshWorker.RunWorkerAsync(this);
        }
        catch (Exception ex)
        {
          logger.Error("TVSeriesUpdateLatestThread: " + ex.ToString());
        }
      }
    }

    internal void UpdateImageTimer(GUIWindow fWindow, Object stateInfo, ElapsedEventArgs e)
    {
      if (LatestMediaHandlerSetup.LatestTVSeries.Equals("True", StringComparison.CurrentCulture))
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
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.selected.fanart1", " ");
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.selected.fanart2", " ");
              Utils.UnLoadImage(ref images);
              ShowFanart = 1;
              SelectedFacadeItem2 = -1;
              SelectedFacadeItem2 = -1;
              NeedCleanup = false;
              NeedCleanupCount = 0;
            }
            else if (NeedCleanup && NeedCleanupCount == 0)
            {
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.selected.showfanart1", "false");
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.selected.showfanart2", "false");
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
          logger.Error("UpdateImageTimer (tvseries latest): " + ex.ToString());
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
