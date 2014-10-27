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
using System.Globalization;
using System.Threading;




namespace LatestMediaHandler
{
  internal class LatestTVSeriesHandler
  {
    #region declarations

    private Logger logger = LogManager.GetCurrentClassLogger();
    private bool _isGetTypeRunningOnThisThread /* = false*/;
    private VideoHandler episodePlayer = null;
    private Hashtable latestTVSeries;
    private int tVSeriesCount = 0;
    private GUIFacadeControl facade = null;
    private LatestMediaHandler.LatestsCollection result = null;
    private ArrayList al = new ArrayList();
    internal ArrayList images = new ArrayList();
    internal ArrayList imagesThumbs = new ArrayList();
    //internal ArrayList thumbs = new ArrayList();
    private int selectedFacadeItem1 = -1;
    private int selectedFacadeItem2 = -1;
    private int showFanart = 1;
    private bool needCleanup = false;
    private int needCleanupCount = 0;
    private Types currentType = Types.Latest;
    private int lastFocusedId = 0;

    #endregion

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

    public LatestTVSeriesHandler()
    {
      currentType = Types.Latest;
    }

    private bool IsGetTypeRunningOnThisThread
    {
      get { return _isGetTypeRunningOnThisThread; }
      set { _isGetTypeRunningOnThisThread = value; }
    }

    internal void tt()
    {

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
        if (currentType == Types.Latest)
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
        if (LatestMediaHandlerSetup.LatestTVSeriesWatched.Equals("False"))
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
            GUIWindow gw = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow);
            GUIControl gc = gw.GetControl(919199940);
            facade = gc as GUIFacadeControl;
            if (facade != null)
            {
              PlayTVSeries(facade.SelectedListItem.ItemId);
            }
            break;
          }
          case 2:
          {
            GUIWindow gw = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow);
            GUIControl gc = gw.GetControl(919199940);
            facade = gc as GUIFacadeControl;
            if (facade != null)
            {
              string sHyp = "seriesid:" + result[(facade.SelectedListItemIndex)].SeriesIndex + "|seasonidx:" +
                            Utils.RemoveLeadingZeros(result[(facade.SelectedListItemIndex)].SeasonIndex) +
                            "|episodeidx:" +
                            Utils.RemoveLeadingZeros(result[(facade.SelectedListItemIndex)].EpisodeIndex);
              GUIWindowManager.ActivateWindow(9811, sHyp, false);
            }
            break;
          }
          case 3:
          {
            if (currentType == Types.Latest)
            {
              if (LatestMediaHandlerSetup.LatestTVSeriesWatched.Equals("True"))
              {
                TVSeriesUpdateLatest(Types.Watched, true);
              }
              else
              {
                TVSeriesUpdateLatest(Types.Watched, false);
              }
              currentType = Types.Watched;
            }
            else
            {
              if (LatestMediaHandlerSetup.LatestTVSeriesWatched.Equals("True"))
              {
                TVSeriesUpdateLatest(Types.Latest, true);
              }
              else
              {
                TVSeriesUpdateLatest(Types.Latest, false);
              }
              currentType = Types.Latest;
            }
            break;
          }
          case 4:
          {
            if (LatestMediaHandlerSetup.LatestTVSeriesWatched.Equals("False"))
            {
              if (currentType == Types.Latest)
              {
                TVSeriesUpdateLatest(Types.Latest, true);
              }
              else
              {
                TVSeriesUpdateLatest(Types.Watched, true);
              }
              LatestMediaHandlerSetup.LatestTVSeriesWatched = "True";
            }
            else
            {
              if (currentType == Types.Latest)
              {
                TVSeriesUpdateLatest(Types.Latest, false);
              }
              else
              {
                TVSeriesUpdateLatest(Types.Watched, false);
              }
              LatestMediaHandlerSetup.LatestTVSeriesWatched = "False";
            }
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
    /// Returns latest added tvseries thumbs from TVSeries db.
    /// </summary>
    /// <param name="type">Type of data to fetch</param>
    /// <returns>Resultset of matching data</returns>
    private LatestsCollection GetLatestTVSeries(Types type, bool onlyNew)
    {
      LatestMediaHandler.LatestsCollection resultTmp = new LatestMediaHandler.LatestsCollection();
      try
      {
        GUIWindow gw = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow);
        GUIControl gc = gw.GetControl(919199940);
        facade = gc as GUIFacadeControl;
        ArrayList alTmp = new ArrayList();
        /*if (facade != null)
                {
                    facade.Clear();
                }
                if (al != null)
                {
                    al.Clear();
                }*/
        latestTVSeries = new Hashtable();
        int i0 = 1;
        int x0 = 0;

        List<DBEpisode> episodes = null;
        if (type == Types.Latest)
        {
          episodes = DBEpisode.GetMostRecent(MostRecentType.Created, 300, 30, onlyNew);
        }
        else
        {
          episodes = DBEpisode.GetNextWatchingEpisodes(10);
        }

        if (episodes != null)
        {
          //if (episodes.Count > 3) episodes.RemoveRange(3, episodes.Count - 3);
          //episodes.Reverse();

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
                {
                  seasonIdx = seasonIdx.PadLeft(2, '0');
                }
                if (episodeIdx != null)
                {
                  episodeIdx = episodeIdx.PadLeft(2, '0');
                }
                string seriesTitle = series.ToString();
                string thumb = episode.Image.Trim(); // ImageAllocator.GetEpisodeImage(episode);
                string thumbSeries = ImageAllocator.GetSeriesPosterAsFilename(series).Trim();
                  //ImageAllocator.GetSeriesPoster(series,false);
                string fanart = Fanart.getFanart(episode[DBEpisode.cSeriesID]).FanartFilename;
                string dateAdded = episode[DBEpisode.cFileDateAdded];
                try
                {
                  DateTime dTmp = DateTime.Parse(dateAdded);
                  dateAdded = String.Format("{0:" + LatestMediaHandlerSetup.DateFormat + "}", dTmp);
                }
                catch
                {
                }
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
                {
                }
                string episodeSummary = episode[DBOnlineEpisode.cEpisodeSummary];
                System.Globalization.CultureInfo ci = System.Globalization.CultureInfo.InstalledUICulture;
                System.Globalization.NumberFormatInfo ni =
                  (System.Globalization.NumberFormatInfo) ci.NumberFormat.Clone();
                ni.NumberDecimalSeparator = ".";
                string mathRoundToString = string.Empty;
                if (episodeRating != null && episodeRating.Length > 0)
                {
                  try
                  {
                    episodeRating = episodeRating.Replace(",", ".");
                    mathRoundToString =
                      Math.Round(double.Parse(episodeRating, ni), MidpointRounding.AwayFromZero)
                        .ToString(CultureInfo.CurrentCulture);
                  }
                  catch
                  {
                  }
                }
                if (thumb == null || thumb.Length < 1)
                {
                  thumb = "DefaultFolderBig.png";
                }

                resultTmp.Add(new LatestMediaHandler.Latest(dateAdded, thumb, fanart, seriesTitle, episodeTitle, null,
                  null, seriesGenre, episodeRating, mathRoundToString, contentRating, episodeRuntime, episodeFirstAired,
                  seasonIdx, episodeIdx, thumbSeries, null, null, episodeSummary, seriesIdx));
                if (result == null || result.Count == 0)
                {
                  result = resultTmp;
                }
                latestTVSeries.Add(i0, episode);
                /*if (x0 < 3)
                                {                                
                                
                                }*/
                //if (facade != null)
                //{
                AddToFilmstrip(resultTmp[x0], i0, ref alTmp);
                //}
                x0++;
                i0++;
              }
            }
            series = null;
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

          if (episodes != null)
          {
            episodes.Clear();
          }
          episodes = null;
        }
      }
      catch (Exception ex)
      {
        logger.Error("GetLatestTVSeries: " + ex.ToString());
      }
      result = resultTmp;
      return result;
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
          movie.Rating = Int32.Parse(latests.RoundedRating);
        }
        catch
        {
          movie.Rating = 0;
        }
        movie.Watched = 0;
        item.ItemId = x;
        Utils.LoadImage(latests.ThumbSeries, ref imagesThumbs);
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
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.selected.thumb",
            result[(item.ItemId - 1)].Thumb);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.selected.serieThumb",
            result[(item.ItemId - 1)].ThumbSeries);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.selected.serieName",
            result[(item.ItemId - 1)].Title);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.selected.seasonIndex",
            result[(item.ItemId - 1)].SeasonIndex);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.selected.episodeName",
            result[(item.ItemId - 1)].Subtitle);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.selected.episodeIndex",
            result[(item.ItemId - 1)].EpisodeIndex);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.selected.dateAdded",
            result[(item.ItemId - 1)].DateAdded);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.selected.genre",
            result[(item.ItemId - 1)].Genre);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.selected.rating",
            result[(item.ItemId - 1)].Rating);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.selected.roundedRating",
            result[(item.ItemId - 1)].RoundedRating);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.selected.classification",
            result[(item.ItemId - 1)].Classification);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.selected.runtime",
            result[(item.ItemId - 1)].Runtime);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.selected.firstAired",
            result[(item.ItemId - 1)].Year);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.selected.plot",
            result[(item.ItemId - 1)].Summary);
          selectedFacadeItem1 = item.ItemId;

          GUIWindow gw = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow);
          GUIControl gc = gw.GetControl(919199940);
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
        GUIControl gc = gw.GetControl(919199940);
        facade = gc as GUIFacadeControl;
        if (facade != null && gw.GetFocusControlId() == 919199940 && facade.SelectedListItem != null)
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
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.selected.showfanart2", "");
              Thread.Sleep(1000);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.selected.fanart2", "");
              showFanart = 2;
            }
            else
            {
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.selected.fanart2", _image);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.selected.showfanart2", "true");
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.selected.showfanart1", "");
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
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.selected.showfanart1", "true");
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.selected.showfanart2", "");
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

    internal void PlayTVSeries(int index)
    {
      if (episodePlayer == null)
      {
        episodePlayer = new VideoHandler();
      }

      episodePlayer.ResumeOrPlay((DBEpisode) latestTVSeries[index]);
    }

    internal void SetupTVSeriesLatest()
    {
      try
      {
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
          if (currentType == Types.Latest)
          {
            if (LatestMediaHandlerSetup.LatestTVSeriesWatched.Equals("True"))
            {
              TVSeriesUpdateLatest(Types.Latest, true);
            }
            else
            {
              TVSeriesUpdateLatest(Types.Latest, false);
            }
          }
          else
          {
            if (LatestMediaHandlerSetup.LatestTVSeriesWatched.Equals("True"))
            {
              TVSeriesUpdateLatest(Types.Watched, true);
            }
            else
            {
              TVSeriesUpdateLatest(Types.Watched, false);
            }
          }
        }
      }
      catch (Exception ex)
      {
        logger.Error("TVSeriesOnObjectInserted: " + ex.ToString());
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

    internal void TVSeriesUpdateLatest(Types type, bool onlyNew)
    {
      try
      {
        int z = 1;
        if (LatestMediaHandlerSetup.LatestTVSeries.Equals("True", StringComparison.CurrentCulture))
        {
          //LatestMediaHandler.LatestsCollection ht = null;
          GetLatestTVSeries(type, onlyNew);
          for (int i = 0; i < 3; i++)
          {
            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest" + z + ".thumb", string.Empty);
            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest" + z + ".serieThumb", string.Empty);
            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest" + z + ".fanart", string.Empty);
            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest" + z + ".serieName", string.Empty);
            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest" + z + ".seasonIndex", string.Empty);
            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest" + z + ".episodeName", string.Empty);
            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest" + z + ".episodeIndex",
              string.Empty);
            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest" + z + ".dateAdded", string.Empty);
            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest" + z + ".genre", string.Empty);
            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest" + z + ".rating", string.Empty);
            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest" + z + ".roundedRating",
              string.Empty);
            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest" + z + ".classification",
              string.Empty);
            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest" + z + ".runtime", string.Empty);
            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest" + z + ".firstAired", string.Empty);
            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest" + z + ".plot", string.Empty);
            //OLD
            LatestMediaHandlerSetup.SetProperty("#fanarthandler.tvseries.latest" + z + ".thumb", string.Empty);
            LatestMediaHandlerSetup.SetProperty("#fanarthandler.tvseries.latest" + z + ".serieThumb", string.Empty);
            LatestMediaHandlerSetup.SetProperty("#fanarthandler.tvseries.latest" + z + ".fanart", string.Empty);
            LatestMediaHandlerSetup.SetProperty("#fanarthandler.tvseries.latest" + z + ".serieName", string.Empty);
            LatestMediaHandlerSetup.SetProperty("#fanarthandler.tvseries.latest" + z + ".seasonIndex", string.Empty);
            LatestMediaHandlerSetup.SetProperty("#fanarthandler.tvseries.latest" + z + ".episodeName", string.Empty);
            LatestMediaHandlerSetup.SetProperty("#fanarthandler.tvseries.latest" + z + ".episodeIndex", string.Empty);
            LatestMediaHandlerSetup.SetProperty("#fanarthandler.tvseries.latest" + z + ".dateAdded", string.Empty);
            LatestMediaHandlerSetup.SetProperty("#fanarthandler.tvseries.latest" + z + ".genre", string.Empty);
            LatestMediaHandlerSetup.SetProperty("#fanarthandler.tvseries.latest" + z + ".rating", string.Empty);
            LatestMediaHandlerSetup.SetProperty("#fanarthandler.tvseries.latest" + z + ".roundedRating", string.Empty);
            LatestMediaHandlerSetup.SetProperty("#fanarthandler.tvseries.latest" + z + ".classification", string.Empty);
            LatestMediaHandlerSetup.SetProperty("#fanarthandler.tvseries.latest" + z + ".runtime", string.Empty);
            LatestMediaHandlerSetup.SetProperty("#fanarthandler.tvseries.latest" + z + ".firstAired", string.Empty);
            LatestMediaHandlerSetup.SetProperty("#fanarthandler.tvseries.latest" + z + ".plot", string.Empty);
            z++;
          }
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest.enabled", "false");
          //OLD
          LatestMediaHandlerSetup.SetProperty("#fanarthandler.tvseries.latest.enabled", "false");
          if (result != null)
          {
            /*for (int i = 0; i < result.Count && i < 3; i++)
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
                            //OLD
                            LatestMediaHandlerSetup.SetProperty("#fanarthandler.tvseries.latest" + z + ".thumb", string.Empty);
                            LatestMediaHandlerSetup.SetProperty("#fanarthandler.tvseries.latest" + z + ".serieThumb", string.Empty);
                            LatestMediaHandlerSetup.SetProperty("#fanarthandler.tvseries.latest" + z + ".fanart", string.Empty);
                            LatestMediaHandlerSetup.SetProperty("#fanarthandler.tvseries.latest" + z + ".serieName", string.Empty);
                            LatestMediaHandlerSetup.SetProperty("#fanarthandler.tvseries.latest" + z + ".seasonIndex", string.Empty);
                            LatestMediaHandlerSetup.SetProperty("#fanarthandler.tvseries.latest" + z + ".episodeName", string.Empty);
                            LatestMediaHandlerSetup.SetProperty("#fanarthandler.tvseries.latest" + z + ".episodeIndex", string.Empty);
                            LatestMediaHandlerSetup.SetProperty("#fanarthandler.tvseries.latest" + z + ".dateAdded", string.Empty);
                            LatestMediaHandlerSetup.SetProperty("#fanarthandler.tvseries.latest" + z + ".genre", string.Empty);
                            LatestMediaHandlerSetup.SetProperty("#fanarthandler.tvseries.latest" + z + ".rating", string.Empty);
                            LatestMediaHandlerSetup.SetProperty("#fanarthandler.tvseries.latest" + z + ".roundedRating", string.Empty);
                            LatestMediaHandlerSetup.SetProperty("#fanarthandler.tvseries.latest" + z + ".classification", string.Empty);
                            LatestMediaHandlerSetup.SetProperty("#fanarthandler.tvseries.latest" + z + ".runtime", string.Empty);
                            LatestMediaHandlerSetup.SetProperty("#fanarthandler.tvseries.latest" + z + ".firstAired", string.Empty);
                            LatestMediaHandlerSetup.SetProperty("#fanarthandler.tvseries.latest" + z + ".plot", string.Empty);
                            z++;
                        }*/
            z = 1;
            //ArrayList _al = new ArrayList();
            for (int i = 0; i < result.Count && i < 3; i++)
            {
              logger.Info("Updating Latest Media Info: Latest episode " + z + ": " + result[i].Title + " - " +
                          result[i].Subtitle);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest" + z + ".thumb", result[i].Thumb);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest" + z + ".serieThumb",
                result[i].ThumbSeries);
              //  _al.Add(result[i].Fanart);                                                
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest" + z + ".fanart",
                result[i].Fanart);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest" + z + ".serieName",
                result[i].Title);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest" + z + ".seasonIndex",
                result[i].SeasonIndex);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest" + z + ".episodeName",
                result[i].Subtitle);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest" + z + ".episodeIndex",
                result[i].EpisodeIndex);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest" + z + ".dateAdded",
                result[i].DateAdded);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest" + z + ".genre", result[i].Genre);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest" + z + ".rating",
                result[i].Rating);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest" + z + ".roundedRating",
                result[i].RoundedRating);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest" + z + ".classification",
                result[i].Classification);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest" + z + ".runtime",
                result[i].Runtime);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest" + z + ".firstAired",
                result[i].Year);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest" + z + ".plot", result[i].Summary);
              //OLD
              LatestMediaHandlerSetup.SetProperty("#fanarthandler.tvseries.latest" + z + ".thumb", result[i].Thumb);
              LatestMediaHandlerSetup.SetProperty("#fanarthandler.tvseries.latest" + z + ".serieThumb",
                result[i].ThumbSeries);
              LatestMediaHandlerSetup.SetProperty("#fanarthandler.tvseries.latest" + z + ".fanart", result[i].Fanart);
              LatestMediaHandlerSetup.SetProperty("#fanarthandler.tvseries.latest" + z + ".serieName", result[i].Title);
              LatestMediaHandlerSetup.SetProperty("#fanarthandler.tvseries.latest" + z + ".seasonIndex",
                result[i].SeasonIndex);
              LatestMediaHandlerSetup.SetProperty("#fanarthandler.tvseries.latest" + z + ".episodeName",
                result[i].Subtitle);
              LatestMediaHandlerSetup.SetProperty("#fanarthandler.tvseries.latest" + z + ".episodeIndex",
                result[i].EpisodeIndex);
              LatestMediaHandlerSetup.SetProperty("#fanarthandler.tvseries.latest" + z + ".dateAdded",
                result[i].DateAdded);
              LatestMediaHandlerSetup.SetProperty("#fanarthandler.tvseries.latest" + z + ".genre", result[i].Genre);
              LatestMediaHandlerSetup.SetProperty("#fanarthandler.tvseries.latest" + z + ".rating", result[i].Rating);
              LatestMediaHandlerSetup.SetProperty("#fanarthandler.tvseries.latest" + z + ".roundedRating",
                result[i].RoundedRating);
              LatestMediaHandlerSetup.SetProperty("#fanarthandler.tvseries.latest" + z + ".classification",
                result[i].Classification);
              LatestMediaHandlerSetup.SetProperty("#fanarthandler.tvseries.latest" + z + ".runtime", result[i].Runtime);
              LatestMediaHandlerSetup.SetProperty("#fanarthandler.tvseries.latest" + z + ".firstAired", result[i].Year);
              LatestMediaHandlerSetup.SetProperty("#fanarthandler.tvseries.latest" + z + ".plot", result[i].Summary);
              z++;
            }
            /*LatestMediaHandlerSetup.UpdateLatestCache(ref LatestMediaHandlerSetup.LatestTVSeriesHash, _al);
                        if (_al != null)
                        {
                            _al.Clear();
                        }
                        _al = null;                            */
            //ht.Clear();
          }
          //ht = null;
          z = 1;
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvseries.latest.enabled", "true");
          //OLD
          LatestMediaHandlerSetup.SetProperty("#fanarthandler.tvseries.latest.enabled", "true");
        }
        else
        {
          LatestMediaHandlerSetup.EmptyLatestMediaPropsTVSeries();
        }
      }
      catch (Exception ex)
      {
        logger.Error("TVSeriesUpdateLatest: " + ex.ToString());
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
