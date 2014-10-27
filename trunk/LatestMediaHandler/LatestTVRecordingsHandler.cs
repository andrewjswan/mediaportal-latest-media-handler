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
using System.Linq;
using System.IO;
using System.Reflection;
using System.Text;
using RealNLog.NLog;
//using MediaPortal.Configuration;
using MediaPortal.GUI.Library;
using MediaPortal.Util;
using MediaPortal.Dialogs;
//using MediaPortal.Player;
//using MediaPortal.Playlists;
//using TvDatabase;
using System.Globalization;
using TvDatabase;
using TvPlugin;
using TvControl;
using System.Threading;



namespace LatestMediaHandler
{
  internal class LatestTVRecordingsHandler
  {
    #region declarations

    private Logger logger = LogManager.GetCurrentClassLogger();
    private bool _isGetTypeRunningOnThisThread /* = false*/;
    private Hashtable latestTVRecordings;
    private TvDatabase.Recording _rec;
    private String _filename;
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
    private LatestMediaHandler.LatestsCollection result = null;

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

    public LatestTVRecordingsHandler()
    {
    }

    private Hashtable LatestTVRecordings
    {
      get { return latestTVRecordings; }
      set { latestTVRecordings = value; }
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
        if (LatestMediaHandlerSetup.LatestTVRecordingsWatched.Equals("False"))
        {
          pItem = new GUIListItem(Translation.ShowUnwatchedRecordings);
          dlg.Add(pItem);
          pItem.ItemId = 2;
        }
        else
        {
          pItem = new GUIListItem(Translation.ShowAllRecordings);
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
            GUIControl gc = gw.GetControl(919199840);
            facade = gc as GUIFacadeControl;
            if (facade != null)
            {
              PlayRecording(facade.SelectedListItem.ItemId);
            }
            break;
          }
          case 2:
          {
            if (LatestMediaHandlerSetup.LatestTVRecordingsWatched.Equals("False"))
            {
              LatestMediaHandlerSetup.LatestTVRecordingsWatched = "True";
            }
            else
            {
              LatestMediaHandlerSetup.LatestTVRecordingsWatched = "False";
            }
            GetTVRecordings();
            break;
          }
        }
      }
      catch (Exception ex)
      {
        logger.Error("MyContextMenu: " + ex.ToString());
      }
    }


    internal void UpdateActiveRecordings()
    {
      try
      {
        if (TVHome.Connected)
        {
          IList<TvDatabase.Recording> recordings = TvDatabase.Recording.ListAllActive();
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.active1.title", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.active1.genre", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.active1.startTime", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.active1.startDate", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.active1.endTime", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.active1.endDate", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.active1.channel", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.active1.channelLogo", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.active2.title", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.active2.genre", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.active2.startTime", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.active2.startDate", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.active2.endTime", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.active2.endDate", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.active2.channel", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.active2.channelLogo", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.active3.title", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.active3.genre", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.active3.startTime", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.active3.startDate", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.active3.endTime", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.active3.endDate", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.active3.channel", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.active3.channelLogo", string.Empty);
          int i = 1;
          RecordingsCollection latestRecordings = new RecordingsCollection();
          foreach (TvDatabase.Recording rec in recordings)
          {
            string logoImagePath = MediaPortal.Util.Utils.GetCoverArt(Thumbs.TVChannel,
              rec.ReferencedChannel().DisplayName);
            if (string.IsNullOrEmpty(logoImagePath))
            {
              logoImagePath = "defaultVideoBig.png";
            }
            latestRecordings.Add(new LatestRecording(rec.Title, rec.Genre, rec.StartTime,
              String.Format(
                "{0:" + LatestMediaHandlerSetup.DateFormat + "}",
                rec.StartTime),
              rec.StartTime.ToString("HH:mm",
                CultureInfo.CurrentCulture),
              String.Format(
                "{0:" + LatestMediaHandlerSetup.DateFormat + "}", rec.EndTime),
              rec.EndTime.ToString("HH:mm",
                CultureInfo.CurrentCulture),
              rec.ReferencedChannel().DisplayName, logoImagePath));
          }

          latestRecordings.Sort(new LatestRecordingsComparer());
          for (int x0 = 0; x0 < latestRecordings.Count; x0++)
          {
            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.active" + i + ".title",
              latestRecordings[x0].Title);
            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.active" + i + ".genre",
              latestRecordings[x0].Genre);
            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.active" + i + ".startTime",
              latestRecordings[x0].StartTime);
            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.active" + i + ".startDate",
              latestRecordings[x0].StartDate);
            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.active" + i + ".endTime",
              latestRecordings[x0].EndTime);
            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.active" + i + ".endDate",
              latestRecordings[x0].EndDate);
            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.active" + i + ".channel",
              latestRecordings[x0].Channel);
            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.active" + i + ".channelLogo",
              latestRecordings[x0].ChannelLogo);
            if (i == 3)
            {
              break;
            }
            i++;
          }

        }
      }
      catch (Exception ex)
      {
        logger.Error("UpdateActiveRecordings: " + ex.ToString());
      }
    }



    internal LatestsCollection GetTVRecordings()
    {
      if (TVHome.Connected)
      {
        try
        {
          IList<TvDatabase.Schedule> schedules = TvDatabase.Schedule.ListAll();
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.scheduled1.title", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.scheduled1.startTime", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.scheduled1.startDate", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.scheduled1.endTime", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.scheduled1.endDate", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.scheduled1.channel", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.scheduled1.channelLogo",
            string.Empty);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.scheduled2.title", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.scheduled2.startTime", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.scheduled2.startDate", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.scheduled2.endTime", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.scheduled2.endDate", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.scheduled2.channel", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.scheduled2.channelLogo",
            string.Empty);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.scheduled3.title", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.scheduled3.startTime", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.scheduled3.startDate", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.scheduled3.endTime", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.scheduled3.endDate", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.scheduled3.channel", string.Empty);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.scheduled3.channelLogo",
            string.Empty);
          int i = 1;
          RecordingsCollection latestRecordings = new RecordingsCollection();

          foreach (TvDatabase.Schedule schedule in schedules)
          {
            string logoImagePath = MediaPortal.Util.Utils.GetCoverArt(Thumbs.TVChannel,
              schedule.ReferencedChannel().
                DisplayName);
            if (string.IsNullOrEmpty(logoImagePath))
            {
              logoImagePath = "defaultVideoBig.png";
            }

            if (schedule.ScheduleType != (int) ScheduleRecordingType.Once)
            {
              IList<Schedule> seriesList = TVHome.Util.GetRecordingTimes(schedule);
              for (int serieNr = 0; serieNr < seriesList.Count; ++serieNr)
              {
                Schedule recSeries = seriesList[serieNr];
                if (DateTime.Now > recSeries.EndTime)
                {
                  continue;
                }
                if (recSeries.Canceled != Schedule.MinSchedule)
                {
                  continue;
                }

                //Program program = Program.RetrieveByTitleTimesAndChannel(schedule.ProgramName, schedule.StartTime,schedule.EndTime, schedule.IdChannel);                           


                latestRecordings.Add(new LatestRecording(recSeries.ProgramName, null, recSeries.StartTime,
                  String.Format(
                    "{0:" + LatestMediaHandlerSetup.DateFormat + "}",
                    recSeries.StartTime),
                  recSeries.StartTime.ToString("HH:mm",
                    CultureInfo.CurrentCulture),
                  String.Format(
                    "{0:" + LatestMediaHandlerSetup.DateFormat + "}",
                    recSeries.EndTime),
                  recSeries.EndTime.ToString("HH:mm",
                    CultureInfo.CurrentCulture),
                  recSeries.ReferencedChannel().DisplayName, logoImagePath));
              }
            }
            else
            {
              if (schedule.IsSerieIsCanceled(schedule.StartTime, schedule.IdChannel))
              {
                continue;
              }
              //Test if this is an instance of a series recording, if so skip it.
              if (schedule.ReferencedSchedule() != null)
              {
                continue;
              }

              latestRecordings.Add(new LatestRecording(schedule.ProgramName, null, schedule.StartTime,
                String.Format(
                  "{0:" + LatestMediaHandlerSetup.DateFormat + "}",
                  schedule.StartTime),
                schedule.StartTime.ToString("HH:mm",
                  CultureInfo.CurrentCulture),
                String.Format(
                  "{0:" + LatestMediaHandlerSetup.DateFormat + "}",
                  schedule.EndTime),
                schedule.EndTime.ToString("HH:mm",
                  CultureInfo.CurrentCulture),
                schedule.ReferencedChannel().DisplayName, logoImagePath));
            }
          }

          latestRecordings.Sort(new LatestRecordingsComparer());
          for (int x0 = 0; x0 < latestRecordings.Count; x0++)
          {
            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.scheduled" + i + ".title",
              latestRecordings[x0].Title);
            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.scheduled" + i + ".startTime",
              latestRecordings[x0].StartTime);
            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.scheduled" + i + ".startDate",
              latestRecordings[x0].StartDate);
            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.scheduled" + i + ".endTime",
              latestRecordings[x0].EndTime);
            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.scheduled" + i + ".endDate",
              latestRecordings[x0].EndDate);
            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.scheduled" + i + ".channel",
              latestRecordings[x0].Channel);
            LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.scheduled" + i + ".channelLogo",
              latestRecordings[x0].ChannelLogo);
            if (i == 3)
            {
              break;
            }
            i++;
          }
        }
        catch (Exception ex)
        {
          logger.Error("GetTVRecordings (Scheduled Recordings): " + ex.ToString());
        }

        LatestMediaHandler.LatestsCollection resultTmp = new LatestMediaHandler.LatestsCollection();
        LatestsCollection latests = new LatestsCollection();
        try
        {
          IList<TvDatabase.Recording> recordings = TvDatabase.Recording.ListAll();
          int x = 0;
          int i0 = 1;
          foreach (TvDatabase.Recording rec in recordings)
          {
            if (LatestMediaHandlerSetup.LatestTVRecordingsWatched.Equals("True"))
            {
              if (rec.TimesWatched < 1)
              {
                latests.Add(new Latest(rec.StartTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture),
                  "thumbNail", null, rec.Title,
                  rec.EndTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture), null,
                  null, rec.Genre, null, null, null, null, null, null, null, null, rec, null,
                  rec.Description,
                  rec.StartTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture)));
              }
            }
            else if (LatestMediaHandlerSetup.LatestTVRecordingsWatched.Equals("False"))
            {
              latests.Add(new Latest(rec.StartTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture),
                "thumbNail", null, rec.Title,
                rec.EndTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture), null,
                null, rec.Genre, null, null, null, null, null, null, null, null, rec, null,
                rec.Description,
                rec.StartTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture)));
            }
          }

          GUIWindow gw = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow);
          //GUIControl gc = gw.GetControl(919199840);
          facade = gw.GetControl(919199840) as GUIFacadeControl;
          if (facade != null)
          {
            facade.Clear();
          }
          if (al != null)
          {
            al.Clear();
          }

          latests.Sort(new LatestAddedComparer());
          latestTVRecordings = new Hashtable();
          for (int x0 = 0; x0 < latests.Count; x0++)
          {
            //latests[x0].DateAdded = latests[x0].DateAdded.Substring(0, 10);
            try
            {
              DateTime dTmp = DateTime.Parse(latests[x0].DateAdded);
              latests[x0].DateAdded = String.Format("{0:" + LatestMediaHandlerSetup.DateFormat + "}", dTmp);
            }
            catch
            {
            }
            string _filename = ((TvDatabase.Recording) latests[x0].Playable).FileName;
            string thumbNail = string.Empty;

            if (LatestMediaHandlerSetup.MpVersion.CompareTo("1.3.000") > 0)
            {
              //MP 1.4
              thumbNail = TVRecordingsThumbnailHandler.GetThumb(_filename);
            }
            else
            {
              //MP 1.3 or older
              thumbNail = string.Format(CultureInfo.CurrentCulture, "{0}\\{1}{2}", Thumbs.TVRecorded,
                Path.ChangeExtension(MediaPortal.Util.Utils.SplitFilename(_filename),
                  null),
                MediaPortal.Util.Utils.GetThumbExtension());
              if (File.Exists(thumbNail))
              {
                string tmpThumbNail = MediaPortal.Util.Utils.ConvertToLargeCoverArt(thumbNail);
                if (File.Exists(tmpThumbNail))
                {
                  thumbNail = tmpThumbNail;
                }
              }
              if (!File.Exists(thumbNail))
              {
                thumbNail = string.Format(CultureInfo.CurrentCulture, "{0}{1}",
                  Path.ChangeExtension(_filename, null),
                  MediaPortal.Util.Utils.GetThumbExtension());
              }
              if (!File.Exists(thumbNail))
              {
                thumbNail = "defaultTVBig.png";
              }
            }

            latests[x0].Fanart = UtilsFanartHandler.GetFanartForLatest(latests[x0].Title);
            latests[x0].Thumb = thumbNail;
            resultTmp.Add(latests[x0]);
            if (result == null || result.Count == 0)
            {
              result = resultTmp;
            }
            latestTVRecordings.Add(i0, latests[x].Playable);
            //if (facade != null)
            //{
            AddToFilmstrip(latests[x], i0);
            //}
            x++;
            i0++;
            if (x == 10)
            {
              break;
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

          if (latests != null)
          {
            latests.Clear();
          }
          latests = null;
        }
        catch //(Exception ex)
        {
          if (latests != null)
          {
            latests.Clear();
          }
          latests = null;
          //logger.Error("GetTVRecordings: " + ex.ToString());
        }
        result = resultTmp;
      }
      return result;
    }

    private void AddToFilmstrip(Latest latests, int x)
    {
      try
      {
        //Add to filmstrip
        GUIListItem item = new GUIListItem();
        item.ItemId = x;
        Utils.LoadImage(latests.Thumb, ref imagesThumbs);
        item.IconImage = latests.Thumb;
        item.IconImageBig = latests.Thumb;
        item.ThumbnailImage = latests.Thumb;
        item.Label = latests.Title;
        item.Label2 = latests.Genre;
        item.Label3 = latests.DateAdded;
        item.IsFolder = false;
        item.DVDLabel = latests.Fanart;
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
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.selected.thumb", item.IconImageBig);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.selected.title", item.Label);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.selected.dateAdded", item.Label3);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.selected.genre", item.Label2);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.selected.startTime",
            result[(item.ItemId - 1)].SeriesIndex);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.selected.endTime",
            result[(item.ItemId - 1)].Subtitle);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.selected.summary",
            result[(item.ItemId - 1)].Summary);

          selectedFacadeItem1 = item.ItemId;

          GUIWindow gw = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow);
          GUIControl gc = gw.GetControl(919199840);
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
        GUIControl gc = gw.GetControl(919199840);
        facade = gc as GUIFacadeControl;
        if (facade != null && gw.GetFocusControlId() == 919199840 && facade.SelectedListItem != null)
        {
          int _id = facade.SelectedListItem.ItemId;
          String _image = facade.SelectedListItem.DVDLabel;
          if (selectedFacadeItem2 != _id)
          {
            Utils.LoadImage(_image, ref images);
            if (showFanart == 1)
            {
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.selected.fanart1", _image);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.selected.showfanart1", "true");
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.selected.showfanart2", "");
              Thread.Sleep(1000);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.selected.fanart2", "");
              showFanart = 2;
            }
            else
            {
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.selected.fanart2", _image);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.selected.showfanart2", "true");
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.selected.showfanart1", "");
              Thread.Sleep(1000);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.selected.fanart1", "");
              showFanart = 1;
            }
            Utils.UnLoadImage(_image, ref images);
            selectedFacadeItem2 = _id;
          }
        }
        else
        {
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.selected.fanart1", " ");
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.selected.fanart2", " ");
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.selected.showfanart1", "true");
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.selected.showfanart2", "");
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

    private bool IsRecordingActual(TvDatabase.Recording aRecording)
    {
      return aRecording.IsRecording;
    }

    internal bool PlayRecording(int index)
    {
      TvDatabase.Recording rec = (TvDatabase.Recording) latestTVRecordings[index];
      _rec = rec;
      _filename = rec.FileName;

      bool _bIsLiveRecording = false;
      IList<TvDatabase.Recording> itemlist = TvDatabase.Recording.ListAll();

      TvServer server = new TvServer();
      foreach (TvDatabase.Recording recItem in itemlist)
      {
        if (rec.IdRecording == recItem.IdRecording && IsRecordingActual(recItem))
        {
          _bIsLiveRecording = true;
          break;
        }
      }

      int stoptime = rec.StopTime;
      if (_bIsLiveRecording)
      {
        stoptime = -1;
      }

      if (TVHome.Card != null)
      {
        TVHome.Card.StopTimeShifting();
      }

      return TVUtil.PlayRecording(rec, stoptime);
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

    private class LatestRecordingsComparer : IComparer<LatestRecording>
    {
      public int Compare(LatestRecording latest1, LatestRecording latest2)
      {
        int returnValue = 1;
        if (latest1 is LatestRecording && latest2 is LatestRecording)
        {
          string s1 = latest1.StartDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture);
          string s2 = latest2.StartDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture);

          if (s1.CompareTo(s2) > 0) return 1;
          else if (s1.CompareTo(s2) < 0) return -1;
          else return 0;
          //returnValue = latest2.StartTime.CompareTo(latest1.StartTime);
        }

        return returnValue;
      }
    }

    private class LatestRecording
    {
      private string title;
      private string genre;
      private DateTime startDateTime;
      private string startTime;
      private string startDate;
      private string endTime;
      private string endDate;
      private string channel;
      private string channelLogo;

      internal string Title
      {
        get { return title; }
        set { title = value; }
      }

      internal string Genre
      {
        get { return genre; }
        set { genre = value; }
      }

      internal DateTime StartDateTime
      {
        get { return startDateTime; }
        set { startDateTime = value; }
      }

      internal string StartTime
      {
        get { return startTime; }
        set { startTime = value; }
      }

      internal string StartDate
      {
        get { return startDate; }
        set { startDate = value; }
      }

      internal string EndTime
      {
        get { return endTime; }
        set { endTime = value; }
      }

      internal string EndDate
      {
        get { return endDate; }
        set { endDate = value; }
      }

      internal string Channel
      {
        get { return channel; }
        set { channel = value; }
      }

      internal string ChannelLogo
      {
        get { return channelLogo; }
        set { channelLogo = value; }
      }

      internal LatestRecording(string title, string genre, DateTime startDateTime, string startDate, string startTime,
        string endDate, string endTime, string channel, string channelLogo)
      {
        this.title = title;
        if (genre != null && genre.Length > 0)
        {
          this.genre = genre.Replace("|", ",");
        }
        else
        {
          this.genre = genre;
        }
        this.startDateTime = startDateTime;
        this.startTime = startTime;
        this.startDate = startDate;
        this.endTime = endTime;
        this.endDate = endDate;
        this.channel = channel;
        this.channelLogo = channelLogo;
      }

    }



    private class RecordingsCollection : List<LatestRecording>
    {
    }
  }
}
