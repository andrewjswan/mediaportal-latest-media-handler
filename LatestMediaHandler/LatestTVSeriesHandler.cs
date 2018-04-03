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

using LMHNLog.NLog;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

using WindowPlugins.GUITVSeries;
using System.Runtime.CompilerServices;

using ResultTypes = LatestMediaHandler.LatestsFacadeSubType;

namespace LatestMediaHandler
{
  internal class LatestTVSeriesHandler
  {
    #region declarations

    private Logger logger = LogManager.GetCurrentClassLogger();

    private VideoHandler episodePlayer = null;

    private int countTVSeries = 0;
    private LatestsCollection latestTVSeries = null;
    private Hashtable latestTVSeriesForPlay;

    private ArrayList facadeCollection = new ArrayList();
    internal ArrayList images = new ArrayList();
    internal ArrayList imagesThumbs = new ArrayList();

    private int showFanart = 1;
    private bool needCleanup = false;
    private int needCleanupCount = 0;
    private int currentFacade = 0;

    private static Object lockObject = new object();

    #endregion

    public const int ControlID = 919199940;
    public const int Play1ControlID = 91919994;
    public const int Play2ControlID = 91919995;
    public const int Play3ControlID = 91919996;
    public const int Play4ControlID = 91919904;

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

    public LatestTVSeriesHandler(int id = ControlID)
    {
      ControlIDFacades = new List<LatestsFacade>();
      ControlIDPlays = new List<int>();
      //
      ControlIDFacades.Add(new LatestsFacade(id, "TVSeries"));
      if (id == ControlID)
      {
        ControlIDPlays.Add(Play1ControlID);
        ControlIDPlays.Add(Play2ControlID);
        ControlIDPlays.Add(Play3ControlID);
        ControlIDPlays.Add(Play4ControlID);
      }

      CurrentFacade.UnWatched = Utils.LatestTVSeriesWatched;
      switch (Utils.LatestTVSeriesType)
      {
        case 0:
          CurrentFacade.SubType = ResultTypes.Episodes;
          break;
        case 1:
          CurrentFacade.SubType = ResultTypes.Seasons;
          break;
        case 2:
          CurrentFacade.SubType = ResultTypes.Series;
          break;
      }

      Utils.ClearSelectedTVSeriesProperty(CurrentFacade);
      EmptyLatestMediaProperties();
    }

    internal LatestTVSeriesHandler(LatestsFacade facade) : this (facade.ControlID)
    {
      ControlIDFacades[ControlIDFacades.Count - 1] = facade;
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

        pItem = new GUIListItem(Translation.ToSeries);
        dlg.Add(pItem);
        pItem.ItemId = 3;

        //Add Display Menu Item
        if (CurrentFacade.Type == LatestsFacadeType.Latests)
        {
          pItem = new GUIListItem(Translation.DisplayNextEpisodes);
          dlg.Add(pItem);
          pItem.ItemId = 4;
        }
        else
        {
          pItem = new GUIListItem(Translation.DisplayLatestEpisodes);
          dlg.Add(pItem);
          pItem.ItemId = 4;
        }

        //Add Watched/Unwatched Filter Menu Item
        if (!CurrentFacade.UnWatched)
        {
          pItem = new GUIListItem(Translation.ShowUnwatchedEpisodes);
          dlg.Add(pItem);
          pItem.ItemId = 5;
        }
        else
        {
          pItem = new GUIListItem(Translation.ShowAllEpisodes);
          dlg.Add(pItem);
          pItem.ItemId = 5;
        }

        //Add TV Series Latests SubType
        pItem = new GUIListItem();
        // pItem.Label = "[^] "+(resultType == ResultTypes.Episodes ? Translation.ShowLatestsEpisodes : (resultType == ResultTypes.Seasons ? Translation.ShowLatestsSeasons : Translation.ShowLatestsSeries));
        pItem.Label = "[^] " + CurrentFacade.Title + " " + CurrentFacade.SubTitle;
        pItem.ItemId = 6;
        dlg.Add(pItem);

        //Add TV Series Latests Type
        pItem = new GUIListItem();
        pItem.Label = "[^] " + CurrentFacade.Title;
        pItem.ItemId = 7;
        dlg.Add(pItem);

        // Update
        pItem = new GUIListItem();
        pItem.Label = Translation.Update;
        pItem.ItemId = 8;
        dlg.Add(pItem);

        //Show Dialog
        dlg.DoModal(Utils.ActiveWindow);

        if (dlg.SelectedLabel == -1)
          return;

        switch (dlg.SelectedId)
        {
          case 1:
          {
            PlayTVSeries(GUIWindowManager.GetWindow(Utils.ActiveWindow));
            break;
          }
          case 2:
          case 3:
          {
            ShowInfo(dlg.SelectedId == 3);
            break;
          }
          case 4:
          {
            CurrentFacade.Type = (CurrentFacade.Type == LatestsFacadeType.Latests ? LatestsFacadeType.Next : LatestsFacadeType.Latests);
            TVSeriesUpdateLatest();
            break;
          }
          case 5:
          {
            CurrentFacade.UnWatched = !CurrentFacade.UnWatched;
            TVSeriesUpdateLatest();
            break;
          }
          case 6:
          {
            IDialogbox ldlg = (GUIDialogMenu) GUIWindowManager.GetWindow((int) GUIWindow.Window.WINDOW_DIALOG_MENU);
            if (ldlg == null) return;

            ldlg.Reset();
            ldlg.SetHeading(924);

            //Add Details Menu Item
            // pItem = new GUIListItem(((Utils.LatestTVSeriesType == 0) ? "[x] " : string.Empty) + Translation.ShowLatestsEpisodes);
            pItem = new GUIListItem((CurrentFacade.SubType == ResultTypes.Episodes ? "[x] " : string.Empty) + Translation.ShowLatestsEpisodes);
            ldlg.Add(pItem);
            pItem.ItemId = 1;

            pItem = new GUIListItem((CurrentFacade.SubType == ResultTypes.Seasons ? "[x] " : string.Empty) + Translation.ShowLatestsSeasons);
            ldlg.Add(pItem);
            pItem.ItemId = 2;

            pItem = new GUIListItem((CurrentFacade.SubType == ResultTypes.Series ? "[x] " : string.Empty) + Translation.ShowLatestsSeries);
            ldlg.Add(pItem);
            pItem.ItemId = 3;

            //Show Dialog
            ldlg.DoModal(Utils.ActiveWindow);

            if (ldlg.SelectedLabel == -1)
              return;

            // Utils.LatestTVSeriesType = ldlg.SelectedId - 1;
            switch (ldlg.SelectedId - 1)
            {
              case 0:
                CurrentFacade.SubType = ResultTypes.Episodes;
                break;
              case 1:
                  CurrentFacade.SubType = ResultTypes.Seasons;
                break;
              case 2:
                  CurrentFacade.SubType = ResultTypes.Series;
                break;
            }
            TVSeriesUpdateLatest();
            break;
          }
          case 7:
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

            pItem = new GUIListItem((CurrentFacade.Type == LatestsFacadeType.Next ? "[x] " : string.Empty) + Translation.DisplayNextEpisodes);
            ldlg.Add(pItem);
            pItem.ItemId = 4;

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
              TVSeriesUpdateLatest();
            break;
          }
          case 8:
          {
            TVSeriesUpdateLatest();
            break;
          }
        }
      }
      catch (Exception ex)
      {
        logger.Error("MyContextMenu: " + ex.ToString());
      }
    }

    private void ShowInfo(bool toseries = false)
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
          string sHyp = string.Empty;
          if (toseries)
            sHyp = "seriesid:" + latestTVSeries[idx].SeriesIndex + 
                    ((CurrentFacade.SubType == ResultTypes.Seasons || CurrentFacade.SubType == ResultTypes.Episodes) ? 
                    "|seasonidx:" + Utils.RemoveLeadingZeros(latestTVSeries[idx].SeasonIndex) : string.Empty) ;
          else
            sHyp = "seriesid:" + latestTVSeries[idx].SeriesIndex + 
                    ((CurrentFacade.SubType == ResultTypes.Seasons || CurrentFacade.SubType == ResultTypes.Episodes) ? 
                    "|seasonidx:" + Utils.RemoveLeadingZeros(latestTVSeries[idx].SeasonIndex) : string.Empty) +
                    ((CurrentFacade.SubType == ResultTypes.Episodes) ? "|episodeidx:" + Utils.RemoveLeadingZeros(latestTVSeries[idx].EpisodeIndex) : string.Empty);
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
    /// <param name="watched">Only watched TVSeries</param>
    /// <returns>Resultset of matching data</returns>
    private LatestsCollection GetLatestTVSeries()
    {
      latestTVSeries = new LatestsCollection();
      latestTVSeriesForPlay = new Hashtable();

      return GetLatestTVSeriesSeries();
    }

    /// <summary>
    /// Returns latest added tvseries thumbs from TVSeries db.
    /// </summary>
    /// <param name="watched">Only watched TVSeries</param>
    /// <returns>Resultset of matching data</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private LatestsCollection GetLatestTVSeriesSeries()
    {
      Hashtable ht = new Hashtable();
      int i0 = 1;
      int x = 0;

      ResultTypes _resultType = CurrentFacade.SubType;
      try
      {
        List<DBEpisode> episodes = null;
        if (CurrentFacade.Type == LatestsFacadeType.Latests)
        {
          // Get all episodes in database
          SQLCondition conditions = new SQLCondition();
          conditions.Add(new DBOnlineEpisode(), DBOnlineEpisode.cSeriesID, 0, SQLConditionType.GreaterThan);
          conditions.Add(new DBOnlineEpisode(), DBOnlineEpisode.cHidden, 0, SQLConditionType.Equal);
          if (CurrentFacade.UnWatched)
          {
            conditions.Add(new DBOnlineEpisode(), DBOnlineEpisode.cWatched, 1, SQLConditionType.NotEqual);
          }
          conditions.AddOrderItem(DBEpisode.Q(DBEpisode.cFileDateAdded), SQLCondition.orderType.Descending);
          episodes = DBEpisode.Get(conditions, false);        
        }
        else if (CurrentFacade.Type == LatestsFacadeType.Rated)
        {
          SQLCondition conditions = new SQLCondition();
          conditions.Add(new DBOnlineEpisode(), DBOnlineEpisode.cSeriesID, 0, SQLConditionType.GreaterThan);
          conditions.Add(new DBOnlineEpisode(), DBOnlineEpisode.cHidden, 0, SQLConditionType.Equal);
          conditions.Add(new DBEpisode(), DBEpisode.cFilename, string.Empty, SQLConditionType.NotEqual);
          if (CurrentFacade.UnWatched)
          {
            conditions.Add(new DBOnlineEpisode(), DBOnlineEpisode.cWatched, 1, SQLConditionType.NotEqual);
          }
          if (CurrentFacade.SubType == LatestsFacadeSubType.Series)
          {
            conditions.Add(new DBOnlineSeries(), DBOnlineSeries.cRating, string.Empty, SQLConditionType.NotEqual);
            conditions.Add(new DBOnlineSeries(), DBOnlineSeries.cRating, 0, SQLConditionType.GreaterThan);
            conditions.AddOrderItem(DBOnlineSeries.Q(DBOnlineSeries.cRating), SQLCondition.orderType.Descending);
          }
          if (CurrentFacade.SubType == LatestsFacadeSubType.Seasons)
          {
            conditions.Add(new DBSeason(), DBSeason.cRating, string.Empty, SQLConditionType.NotEqual);
            conditions.Add(new DBSeason(), DBSeason.cRating, 0, SQLConditionType.GreaterThan);
            conditions.AddOrderItem(DBSeason.Q(DBSeason.cRating), SQLCondition.orderType.Descending);
          }
          if (CurrentFacade.SubType == LatestsFacadeSubType.Episodes)
          {
            conditions.Add(new DBOnlineEpisode(), DBOnlineEpisode.cRating, string.Empty, SQLConditionType.NotEqual);
            conditions.Add(new DBOnlineEpisode(), DBOnlineEpisode.cRating, 0, SQLConditionType.GreaterThan);
            conditions.AddOrderItem(DBOnlineEpisode.Q(DBOnlineEpisode.cRating), SQLCondition.orderType.Descending);
          }
          episodes = DBEpisode.Get(conditions, false);
        }
        else if (CurrentFacade.Type == LatestsFacadeType.Watched)
        {
          SQLCondition conditions = new SQLCondition();
          conditions.Add(new DBOnlineEpisode(), DBOnlineEpisode.cSeriesID, 0, SQLConditionType.GreaterThan);
          conditions.Add(new DBOnlineEpisode(), DBOnlineEpisode.cHidden, 0, SQLConditionType.Equal);
          conditions.Add(new DBOnlineEpisode(), DBOnlineEpisode.cWatched, 1, SQLConditionType.Equal);
          conditions.AddOrderItem(DBEpisode.Q(DBEpisode.cDateWatched), SQLCondition.orderType.Descending);
          episodes = DBEpisode.Get(conditions, false);
        }
        else // LatestsFacadeType.Next
        {
          episodes = DBEpisode.GetNextWatchingEpisodes(Utils.FacadeMaxNum);
          _resultType = ResultTypes.Episodes;
        }

        if (episodes != null)
        {
          foreach (DBEpisode episode in episodes)
          {
            DBSeries series = Helper.getCorrespondingSeries(episode[DBEpisode.cSeriesID]);
            if (series != null)
            {
              string contentRating = series[DBOnlineSeries.cContentRating];
              if (contentRating != null && Utils.LatestTVSeriesRatings.Contains(contentRating))
              {
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

                string key = "S" + seriesIdx + ((_resultType == ResultTypes.Seasons || _resultType == ResultTypes.Episodes) ? "S" + seasonIdx : string.Empty) + 
                                               ((_resultType == ResultTypes.Episodes) ? "E" + episodeIdx : string.Empty);
                if (!ht.Contains(key))
                {
                  ht.Add(key, key) ;
                  // Series
                  string seriesTitle = series.ToString();
                  string seriesGenre = series[DBOnlineSeries.cGenre];
                  string seriesThumb = ImageAllocator.GetSeriesPosterAsFilename(series);
                  string thumb = seriesThumb;
                  string fanart = Fanart.getFanart(episode[DBEpisode.cSeriesID]).FanartFilename;
                  string firstAired = series[DBOnlineSeries.cFirstAired];
                  string seriesRating = series[DBOnlineSeries.cRating];
                  string seriesSummary = series[DBOnlineSeries.cSummary];
                  //Season
                  string seasonTitle = string.Empty;
                  string seasonRating = string.Empty;
                  string seasonSummary = string.Empty;
                  string seasonThumb = string.Empty;
                  if (_resultType == ResultTypes.Seasons)
                  {
                    DBSeason season = Helper.getCorrespondingSeason(episode[DBEpisode.cSeriesID],episode[DBEpisode.cSeasonIndex]);
                    if (season != null)
                    {
                      seasonTitle = season[DBSeason.cTitle];
                      seasonRating = season[DBSeason.cRating];
                      seasonSummary = season[DBSeason.cSummary];
                      if (string.IsNullOrEmpty(seasonSummary))
                        seasonSummary = seriesSummary;
                      seasonThumb = ImageAllocator.GetSeasonBannerAsFilename(season);
                      if (!string.IsNullOrEmpty(seasonThumb))
                        thumb = seasonThumb;
                    }
                  }
                  // Episodes
                  string episodeThumb = string.Empty;
                  if (_resultType == ResultTypes.Episodes)
                  {
                    bool HideEpisodeImage = true;

                    if (episode[DBOnlineEpisode.cWatched] || !DBOption.GetOptions(DBOption.cHideUnwatchedThumbnail))
                    {
                      HideEpisodeImage = false;
                    }
                    if (!HideEpisodeImage && !String.IsNullOrEmpty(episode.Image) && System.IO.File.Exists(episode.Image))
                    {
                      // show episode image
                      episodeThumb = episode.Image;
                    }
                    else
                    {
                      // show a fanart thumb instead
                      Fanart _fanart = Fanart.getFanart(episode[DBOnlineEpisode.cSeriesID]);
                      episodeThumb = _fanart.FanartThumbFilename;
                    }

                    if (!string.IsNullOrEmpty(episodeThumb))
                    {
                      thumb = episodeThumb;
                    }
                    firstAired = episode[DBOnlineEpisode.cFirstAired];
                  }
                  string episodeTitle = episode[DBEpisode.cEpisodeName];
                  string episodeRating = episode[DBOnlineEpisode.cRating];
                  string episodeRuntime = episode[DBEpisode.cPrettyPlaytime];
                  string episodeSummary = episode[DBOnlineEpisode.cEpisodeSummary];
                  string dateAdded = episode[DBEpisode.cFileDateAdded];
                  //
                  bool isnew = false;
                  try
                  {
                    DateTime dTmp = DateTime.Parse(dateAdded);
                    dateAdded = String.Format("{0:" + Utils.DateFormat + "}", dTmp);

                    isnew = ((dTmp > Utils.NewDateTime) && (string.IsNullOrEmpty(episode[DBEpisode.cDateWatched])));
                    if (isnew)
                    {
                      CurrentFacade.HasNew = true;
                    }
                  }
                  catch
                  {   }
                  try
                  {
                    DateTime dTmp = DateTime.Parse(firstAired);
                    firstAired = String.Format("{0:" + Utils.DateFormat + "}", dTmp);
                  }
                  catch
                  {   }
                  CultureInfo ci = CultureInfo.InstalledUICulture;
                  NumberFormatInfo ni = (NumberFormatInfo) ci.NumberFormat.Clone();
                  ni.NumberDecimalSeparator = ".";
                  string latestRating = (_resultType == ResultTypes.Episodes ? episodeRating : (_resultType == ResultTypes.Seasons ? seasonRating : seriesRating));
                  string mathRoundToString = string.Empty;
                  if (!string.IsNullOrEmpty(latestRating))
                  {
                    try
                    {
                      latestRating = latestRating.Replace(",", ".");
                      mathRoundToString = Math.Round(double.Parse(latestRating, ni), MidpointRounding.AwayFromZero).ToString(CultureInfo.CurrentCulture);
                    }
                    catch
                    {   }
                  }
                  //
                  if (string.IsNullOrEmpty(thumb))
                  {
                    thumb = "DefaultVideoBig.png"; // "DefaultFolderBig.png";
                  }
                  else if (CurrentFacade.ThumbType != LatestsFacadeThumbType.None)
                  {
                    if (CurrentFacade.ThumbType == LatestsFacadeThumbType.Series && !string.IsNullOrEmpty(seriesThumb))
                    {
                      thumb = seriesThumb;
                    }
                    if (CurrentFacade.ThumbType == LatestsFacadeThumbType.Seasons && !string.IsNullOrEmpty(seasonThumb))
                    {
                      thumb = seriesThumb;
                    }
                    if (CurrentFacade.ThumbType == LatestsFacadeThumbType.Series && !string.IsNullOrEmpty(episodeThumb))
                    {
                      thumb = seriesThumb;
                    }
                  }
                  //
                  // string latestTitle = (resultType == ResultTypes.Episodes ? episodeTitle : (resultType == ResultTypes.Seasons ? seasonTitle : seriesTitle));
                  string latestTitle = (!string.IsNullOrEmpty(episodeTitle) ? episodeTitle : (!string.IsNullOrEmpty(seasonTitle) ? seasonTitle : seriesTitle));
                  string latestRuntime = (_resultType == ResultTypes.Episodes ? episodeRuntime : (_resultType == ResultTypes.Seasons ? string.Empty : string.Empty));
                  string latestSummary = (_resultType == ResultTypes.Episodes ? episodeSummary : (_resultType == ResultTypes.Seasons ? seasonSummary : seriesSummary));
                  // logger.Debug(i0+"|"+dateAdded+"|"+thumb+"|"+fanart+"|"+seriesTitle+"|"+latestTitle+"|"+seriesGenre+"|"+latestRating+"|"+mathRoundToString+"|"+contentRating+"|"+latestRuntime+"|"+firstAired+"|"+seasonIdx+"|"+episodeIdx+"|"+seriesThumb+"|"+latestSummary+"|"+seriesIdx+"|"+isnew);

                  string fbanner = string.Empty;
                  string fclearart = string.Empty;
                  string fclearlogo = string.Empty;
                  string fcd = string.Empty;

                  if (Utils.FanartHandler)
                  {
                    Parallel.Invoke
                    (
                      () => fbanner = UtilsFanartHandler.GetFanartTVForLatestMedia(seriesIdx, string.Empty, string.Empty, Utils.FanartTV.SeriesBanner),
                      () => fclearart = UtilsFanartHandler.GetFanartTVForLatestMedia(seriesIdx, string.Empty, string.Empty, Utils.FanartTV.SeriesClearArt),
                      () => fclearlogo = UtilsFanartHandler.GetFanartTVForLatestMedia(seriesIdx, string.Empty, string.Empty, Utils.FanartTV.SeriesClearLogo),
                      () => fcd = UtilsFanartHandler.GetFanartTVForLatestMedia(seriesIdx, string.Empty, string.Empty, Utils.FanartTV.SeriesCDArt)
                    );

                    if (_resultType == ResultTypes.Episodes || _resultType == ResultTypes.Seasons)
                    {
                      Parallel.Invoke
                      (
                        () =>
                        {
                          string fsbanner = UtilsFanartHandler.GetFanartTVForLatestMedia(seriesIdx, string.Empty, seasonIdx, Utils.FanartTV.SeriesSeasonBanner);
                          if (!string.IsNullOrEmpty(fsbanner))
                          {
                            fbanner = fsbanner;
                          }
                        },
                        () =>
                        {
                          string fscd = UtilsFanartHandler.GetFanartTVForLatestMedia(seriesIdx, string.Empty, seasonIdx, Utils.FanartTV.SeriesSeasonCDArt);
                          if (!string.IsNullOrEmpty(fscd))
                          {
                            fcd = fscd;
                          }
                        }
                      );
                    }
                  }

                  latestTVSeries.Add(new Latest(dateAdded, thumb, fanart, seriesTitle, latestTitle, 
                                                null, null, 
                                                seriesGenre, latestRating, mathRoundToString, contentRating, latestRuntime, firstAired, seasonIdx, episodeIdx, seriesThumb, 
                                                null, null, 
                                                latestSummary, seriesIdx,
                                                fbanner, fclearart, fclearlogo, fcd,
                                                isnew));
                  latestTVSeriesForPlay.Add(i0, episode);
                  Utils.ThreadToSleep();

                  i0++;
                  x++;
                  if (x == Utils.FacadeMaxNum)
                    break;
                }
              }
            }
          }
        }
      }
      catch (Exception ex)
      {
        logger.Error("GetLatestTVSeries: " + ex.ToString());
      }
      if (ht != null)
      {
        ht.Clear();
      }
      ht = null;

      if (!CurrentFacade.LeftToRight)
      {
        Utils.SortLatests(ref latestTVSeries, CurrentFacade.Type, CurrentFacade.LeftToRight);
      }

      if (latestTVSeries != null && !MainFacade)
      {
        logger.Debug("GetLatest: " + this.ToString() + ":" + CurrentFacade.ControlID + " - " + latestTVSeries.Count);
      }

      return latestTVSeries;
    }

    public Hashtable GetLatestsList()
    {
      Hashtable ht = new Hashtable();
      if (latestTVSeries != null)
      {
        for (int i = 0; i < latestTVSeries.Count; i++)
        {
          if (!ht.Contains(latestTVSeries[i].SeriesIndex))
          {
            // logger.Debug("Make Latest List: TVSeries: " + ((Utils.latestTVSeriesType == 0) ? "Episode" : (Utils.latestTVSeriesType == 1) ? "Season" : "Series") + " " + latestTVSeries[i].SeriesIndex + " - " + latestTVSeries[i].Title);
            ht.Add(latestTVSeries[i].SeriesIndex, latestTVSeries[i].Title) ;
          }
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

      Utils.ClearLatestsTVSeriesProperty(CurrentFacade, MainFacade);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal void TVSeriesUpdateLatest()
    {
      if (Interlocked.CompareExchange(ref CurrentFacade.Update, 1, 0) != 0)
        return;

      if (!Utils.LatestTVSeries)
      {
        EmptyLatestMediaProperties();
        CurrentFacade.Update = 0;
        return;
      }

      // TV Series
      LatestsCollection hTable = GetLatestTVSeries();
      LatestsToFilmStrip(latestTVSeries);

      if (MainFacade || CurrentFacade.AddProperties)
      {
        EmptyLatestMediaProperties();

        if (hTable != null)
        {
          Utils.FillLatestsTVSeriesProperty(CurrentFacade, hTable, MainFacade);
        }
      }

      if ((latestTVSeries != null) && (latestTVSeries.Count > 0))
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
        Utils.UpdateLatestsUpdate(Utils.LatestsCategory.TVSeries, DateTime.Now);
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
        item.IconImage = (CurrentFacade.SubType == ResultTypes.Seasons ? latests.Thumb : latests.ThumbSeries);
        item.IconImageBig = (CurrentFacade.SubType == ResultTypes.Seasons ? latests.Thumb : latests.ThumbSeries);
        item.ThumbnailImage = (CurrentFacade.SubType == ResultTypes.Series ? latests.ThumbSeries : latests.Thumb);
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
      if (!Utils.LatestTVSeries)
      {
        return;
      }

      try
      {
        lock(lockObject)
        {
          // LatestsToFilmStrip(latestTVSeries);

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
      if (!Utils.LatestTVSeries)
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
          Utils.FillSelectedTVSeriesProperty(CurrentFacade, latestTVSeries[item.ItemId - 1]);
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
              Utils.SetProperty("#latestMediaHandler.tvseries.selected.fanart1", _image);
              Utils.SetProperty("#latestMediaHandler.tvseries.selected.showfanart1", "true");
              Utils.SetProperty("#latestMediaHandler.tvseries.selected.showfanart2", "false");
              Thread.Sleep(1000);
              Utils.SetProperty("#latestMediaHandler.tvseries.selected.fanart2", string.Empty);
              showFanart = 2;
            }
            else
            {
              Utils.SetProperty("#latestMediaHandler.tvseries.selected.fanart2", _image);
              Utils.SetProperty("#latestMediaHandler.tvseries.selected.showfanart2", "true");
              Utils.SetProperty("#latestMediaHandler.tvseries.selected.showfanart1", "false");
              Thread.Sleep(1000);
              Utils.SetProperty("#latestMediaHandler.tvseries.selected.fanart1", string.Empty);
              showFanart = 1;
            }
            // Utils.UnLoadImage(_image, ref images);
            CurrentFacade.SelectedImage = _id;
          }
        }
        else
        {
          Utils.SetProperty("#latestMediaHandler.tvseries.selected.fanart1", " ");
          Utils.SetProperty("#latestMediaHandler.tvseries.selected.fanart2", " ");
          Utils.SetProperty("#latestMediaHandler.tvseries.selected.showfanart1", "false");
          Utils.SetProperty("#latestMediaHandler.tvseries.selected.showfanart2", "false");
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
        // CurrentFacade.Facade = Utils.GetLatestsFacade(CurrentFacade.ControlID);
        if (CurrentFacade.Facade != null && CurrentFacade.Facade.Focus && CurrentFacade.Facade.SelectedListItem != null)
        {
          PlayTVSeries(CurrentFacade.Facade.SelectedListItem.ItemId);
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
      if (CurrentFacade.SubType == ResultTypes.Episodes)
      {
        if (episodePlayer == null)
        {
          episodePlayer = new VideoHandler();
        }
        episodePlayer.ResumeOrPlay((DBEpisode) latestTVSeriesForPlay[index]);
      }
      else
      {
        index = index - 1;
        string sHyp = "seriesid:" + latestTVSeries[index].SeriesIndex + 
                      ((CurrentFacade.SubType == ResultTypes.Seasons || CurrentFacade.SubType == ResultTypes.Episodes) ? 
                      "|seasonidx:" + Utils.RemoveLeadingZeros(latestTVSeries[index].SeasonIndex) : string.Empty) ;
        GUIWindowManager.ActivateWindow(9811, sHyp, false);
      }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal void SetupReceivers()
    {
      if (!Utils.LatestTVSeries)
      {
        return;
      }

      try
      {
        GUIWindowManager.Receivers += new SendMessageHandler(OnMessage);
        OnlineParsing.OnlineParsingCompleted += new OnlineParsing.OnlineParsingCompletedHandler(TVSeriesOnObjectInserted);
        TVSeriesPlugin.ToggleWatched += new TVSeriesPlugin.ToggleWatchedEventDelegate(OnToggleWatched);
      }
      catch (Exception ex)
      {
        logger.Error("SetupTVSeriesLatest: " + ex.ToString());
      }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal void DisposeReceivers()
    {
      if (!Utils.LatestTVSeries)
      {
        return;
      }

      try
      {
        GUIWindowManager.Receivers -= new SendMessageHandler(OnMessage);
        OnlineParsing.OnlineParsingCompleted -= new OnlineParsing.OnlineParsingCompletedHandler(TVSeriesOnObjectInserted);
        TVSeriesPlugin.ToggleWatched -= new TVSeriesPlugin.ToggleWatchedEventDelegate(OnToggleWatched);
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
          GetLatestMediaInfoThread();
        }
      }
      catch (Exception ex)
      {
        logger.Error("TVSeriesOnObjectInserted: " + ex.ToString());
      }
    }

    internal void OnToggleWatched(DBSeries show, List<DBEpisode> episodes, bool watched)
    {
      try
      {
        if (ChangedEpisodeCount())
        {
          GetLatestMediaInfoThread();
        }
      }
      catch (Exception ex)
      {
        logger.Error("OnToggleWatched: " + ex.ToString());
      }
    }

    private void OnMessage(GUIMessage message)
    {
      if (Utils.LatestTVSeries)
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

    internal bool ChangedEpisodeCount()
    {
      bool b = false;

      try
      {
        SQLCondition condition = new SQLCondition();
        condition.Add(new DBEpisode(), DBEpisode.cFilename, string.Empty, SQLConditionType.NotEqual);

        List<DBEpisode> episodes = DBEpisode.Get(condition);

        if (countTVSeries != episodes.Count)
        {
          b = true;
          countTVSeries = episodes.Count;
        }
      }
      catch (Exception ex)
      {
        logger.Error("ChangedEpisodeCount: " + ex.ToString());
      }
      return b;
    }

    internal void GetLatestMediaInfoThread()
    {
      // TVSeries
      if (Utils.LatestTVSeries)
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
      if (Utils.LatestTVSeries)
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
              Utils.SetProperty("#latestMediaHandler.tvseries.selected.fanart1", " ");
              Utils.SetProperty("#latestMediaHandler.tvseries.selected.fanart2", " ");
              Utils.UnLoadImages(ref images);
              ShowFanart = 1;
              CurrentFacade.SelectedImage = -1;
              NeedCleanup = false;
              NeedCleanupCount = 0;
            }
            else if (NeedCleanup && NeedCleanupCount == 0)
            {
              Utils.SetProperty("#latestMediaHandler.tvseries.selected.showfanart1", "false");
              Utils.SetProperty("#latestMediaHandler.tvseries.selected.showfanart2", "false");
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
  }
}
