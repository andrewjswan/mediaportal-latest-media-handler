//***********************************************************************
// Assembly         : LatestMediaHandler
// Author           : cul8er
// Created          : 05-09-2010
//
// Last Modified By : cul8er
// Last Modified On : 01-11-2011
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
using MediaPortal.GUI.Library;
using MediaPortal.Dialogs;
using MediaPortal.Player;
using ForTheRecord.Client.Common;
using ForTheRecord.Entities;
using ForTheRecord.ServiceAgents;
using System.Globalization;
using System.Threading;


namespace LatestMediaHandler
{
  internal class Latest4TRRecordingsHandler
  {
    #region declarations

    private Logger logger = LogManager.GetCurrentClassLogger();
    private bool _isGetTypeRunningOnThisThread /* = false*/;
    private ForTheRecord.Entities.Recording _playingRecording;
    private string _playingRecordingFileName;
    private Hashtable latest4TRRecordings;
    private bool _is4TRversion1602orAbove = false;
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

    internal Latest4TRRecordingsHandler()
    {
    }

    private Hashtable Latest4TRRecordings
    {
      get { return latest4TRRecordings; }
      set { latest4TRRecordings = value; }
    }

    internal bool Is4TRversion1602orAbove
    {
      get { return _is4TRversion1602orAbove; }
      set { _is4TRversion1602orAbove = value; }
    }

    internal bool IsGetTypeRunningOnThisThread
    {
      get { return _isGetTypeRunningOnThisThread; }
      set { _isGetTypeRunningOnThisThread = value; }
    }

    internal Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
    {
      Assembly assembly = null;
      // Only process events from the thread that started it, not any other thread
      if (_isGetTypeRunningOnThisThread)
      {
        // Extract assembly name, and checking it's the same as args.Name
        // to prevent an infinite loop
        var an = new AssemblyName(args.Name);
        if (an.Name != args.Name)
          assembly = ((AppDomain) sender).Load(an.Name);
      }
      return assembly;
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
            Get4TRRecordings();
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
        if (ServiceChannelFactories.IsInitialized)
        {
          using (TvControlServiceAgent _tvControlAgent = new TvControlServiceAgent())
          {
            ActiveRecording[] recordings = null;
            recordings = null;
            recordings = _tvControlAgent.GetActiveRecordings();
            if (recordings != null)
            {
              TvSchedulerServiceAgent _tvSchedulerAgent = new TvSchedulerServiceAgent();
              foreach (ActiveRecording rec in recordings)
              {
                string logoImagePath = ChannelLogosCache.GetLogoPath(_tvSchedulerAgent, rec.Program.Channel.ChannelId,
                  rec.Program.Channel.DisplayName, 84, 84);
                if (logoImagePath == null || !System.IO.File.Exists(logoImagePath))
                {
                  logoImagePath = "defaultVideoBig.png";
                }
                latestRecordings.Add(new LatestRecording(rec.Program.Title, rec.Program.Category,
                  rec.Program.ActualStartTime,
                  String.Format("{0:" + LatestMediaHandlerSetup.DateFormat + "}", rec.Program.ActualStartTime),
                  rec.Program.ActualStartTime.ToString("HH:mm",
                    CultureInfo.
                      CurrentCulture),
                  String.Format("{0:" + LatestMediaHandlerSetup.DateFormat + "}", rec.Program.StopTime),
                  rec.Program.StopTime.ToString("HH:mm",
                    CultureInfo.CurrentCulture),
                  rec.Program.Channel.DisplayName, logoImagePath));
              }
            }
          }
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
      catch (Exception ex)
      {
        logger.Error("UpdateActiveRecordings: " + ex.ToString());
      }
    }

    internal LatestsCollection Get4TRRecordings()
    {
      try
      {
        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.scheduled1.title", string.Empty);
        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.scheduled1.startTime", string.Empty);
        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.scheduled1.startDate", string.Empty);
        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.scheduled1.endTime", string.Empty);
        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.scheduled1.endDate", string.Empty);
        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.scheduled1.channel", string.Empty);
        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.scheduled1.channelLogo", string.Empty);
        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.scheduled2.title", string.Empty);
        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.scheduled2.startTime", string.Empty);
        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.scheduled2.startDate", string.Empty);
        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.scheduled2.endTime", string.Empty);
        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.scheduled2.endDate", string.Empty);
        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.scheduled2.channel", string.Empty);
        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.scheduled2.channelLogo", string.Empty);
        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.scheduled3.title", string.Empty);
        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.scheduled3.startTime", string.Empty);
        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.scheduled3.startDate", string.Empty);
        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.scheduled3.endTime", string.Empty);
        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.scheduled3.endDate", string.Empty);
        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.scheduled3.channel", string.Empty);
        LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.tvrecordings.scheduled3.channelLogo", string.Empty);

        if (ServiceChannelFactories.IsInitialized)
        {
          using (TvControlServiceAgent _tvControlAgent = new TvControlServiceAgent())
          {
            UpcomingRecording[] recordings = null;
            recordings = null;
            recordings =
              _tvControlAgent.GetAllUpcomingRecordings(
                UpcomingRecordingsFilter.Recordings | UpcomingRecordingsFilter.CancelledByUser, false);
            int i = 1;
            RecordingsCollection latestRecordings = new RecordingsCollection();
            if (recordings != null)
            {
              TvSchedulerServiceAgent _tvSchedulerAgent = new TvSchedulerServiceAgent();
              foreach (UpcomingRecording rec in recordings)
              {
                string logoImagePath = ChannelLogosCache.GetLogoPath(_tvSchedulerAgent, rec.Program.Channel.ChannelId,
                  rec.Program.Channel.DisplayName, 84, 84);
                if (logoImagePath == null || !System.IO.File.Exists(logoImagePath))
                {
                  logoImagePath = "defaultVideoBig.png";
                }
                latestRecordings.Add(new LatestRecording(rec.Program.Title, null, rec.Program.ActualStartTime,
                  String.Format("{0:" + LatestMediaHandlerSetup.DateFormat + "}", rec.Program.ActualStartTime),
                  rec.Program.ActualStartTime.ToString("HH:mm", CultureInfo.CurrentCulture),
                  String.Format("{0:" + LatestMediaHandlerSetup.DateFormat + "}", rec.Program.ActualStopTime),
                  rec.Program.ActualStopTime.ToString("HH:mm", CultureInfo.CurrentCulture),
                  rec.Program.Channel.DisplayName, logoImagePath));
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
          }
        }
      }
      catch (Exception ex)
      {
        logger.Error("GetTVRecordings (Scheduled Recordings): " + ex.ToString());
      }

      LatestsCollection latests = new LatestsCollection();
      LatestMediaHandler.LatestsCollection resultTmp = new LatestMediaHandler.LatestsCollection();
      try
      {
        GUIWindow gw = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow);
        GUIControl gc = gw.GetControl(919199840);
        facade = gc as GUIFacadeControl;
        if (facade != null)
        {
          facade.Clear();
        }
        if (al != null)
        {
          al.Clear();
        }

        if (ServiceChannelFactories.IsInitialized)
        {
          using (TvControlServiceAgent _tvControlAgent = new TvControlServiceAgent())
          {
            RecordingSummary[] recordings = null;
            DateTime _time = DateTime.Now;
            while (latests.Count < 10 && _time > DateTime.Now.AddMonths(-3)) //go max three moth back
            {
              recordings = null;
              recordings = _tvControlAgent.GetRecordingsForOneDay(ChannelType.Television, _time, false);
              if (recordings != null)
              {
                foreach (RecordingSummary rec in recordings)
                {
                  if (rec.ChannelType == ChannelType.Television)
                  {
                    Recording recording = _tvControlAgent.GetRecordingById(rec.RecordingId);
                    string _summary = string.Empty;
                    if (recording != null)
                    {
                      _summary = recording.Description;
                    }
                    string thumbNail = rec.ThumbnailFileName;
                    if (!File.Exists(thumbNail))
                    {
                      thumbNail = "defaultTVBig.png";
                    }
                    if (LatestMediaHandlerSetup.LatestTVRecordingsWatched.Equals("True"))
                    {
                      if (!rec.LastWatchedTime.HasValue)
                      {
                        latests.Add(new Latest(
                          rec.StartTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture), thumbNail, null,
                          rec.Title, rec.StartTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture), null,
                          null, rec.Category, null, null, null, null, null, null, null, null, rec, null, _summary,
                          rec.StartTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture)));
                      }
                    }
                    else if (LatestMediaHandlerSetup.LatestTVRecordingsWatched.Equals("False"))
                    {
                      latests.Add(new Latest(rec.StartTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture),
                        thumbNail, null, rec.Title,
                        rec.StartTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture), null, null,
                        rec.Category, null, null, null, null, null, null, null, null, rec, null, _summary,
                        rec.StartTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture)));
                    }
                  }
                }
              }
              _time = _time.AddDays(-1);
            }
            int x = 0;
            int i0 = 1;
            if (latests != null && latests.Count > 0)
            {
              latests.Sort(new LatestAddedComparer());
              latest4TRRecordings = new Hashtable();
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
                latests[x0].Fanart = UtilsFanartHandler.GetFanartForLatest(latests[x0].Title);

                resultTmp.Add(latests[x0]);
                if (result == null || result.Count == 0)
                {
                  result = resultTmp;
                }
                latest4TRRecordings.Add(i0, latests[x].Playable);
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

        if (latests != null)
        {
          latests.Clear();
        }
        latests = null;
      }
      catch
      {
        if (latests != null)
        {
          latests.Clear();
        }
        latests = null;
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

    internal void PlayRecording(int index)
    {
      ForTheRecord.Entities.RecordingSummary recSummary =
        (ForTheRecord.Entities.RecordingSummary) latest4TRRecordings[index];
      ForTheRecord.Entities.Recording rec = null;

      using (TvControlServiceAgent _tvControlAgent = new TvControlServiceAgent())
      {
        rec = _tvControlAgent.GetRecordingById(recSummary.RecordingId);
      }


      int jumpToTime = 0;

      if (rec.LastWatchedPosition.HasValue)
      {
        if (rec.LastWatchedPosition.Value > 10)
        {
          jumpToTime = rec.LastWatchedPosition.Value;
        }
      }
      if (jumpToTime == 0)
      {
        DateTime startTime = DateTime.Now.AddSeconds(-10);
        if (rec.ProgramStartTime < startTime)
        {
          startTime = rec.ProgramStartTime;
        }
        TimeSpan preRecordSpan = startTime - rec.RecordingStartTime;
        jumpToTime = (int) preRecordSpan.TotalSeconds;
      }

      if (_is4TRversion1602orAbove)
      {
        //send a message to the 4tr plugin, to start playing the recording
        //I use this "GUI_MSG_RECORDER_VIEW_CHANNEL" event because it's a save one to use(no other components in mediaportal listen to this event)
        GUIMessage msg = new GUIMessage(GUIMessage.MessageType.GUI_MSG_RECORDER_VIEW_CHANNEL, 0, 0, 0, 0, 0, null);
        msg.Object = rec;
        msg.Param2 = jumpToTime;
        msg.Param1 = 5577; //just some indentification
        GUIGraphicsContext.SendMessage(msg);
        msg = null;
      }
      else
      {
        g_Player.Stop(true);
        string fileName = rec.RecordingFileName;

        RememberActiveRecordingPosition();

        g_Player.currentFileName = fileName;
        g_Player.currentTitle = rec.Title;

        g_Player.currentDescription = rec.CreateCombinedDescription(true);

        _playingRecording = rec;
        _playingRecordingFileName = fileName;

        if (g_Player.Play(fileName,
          rec.ChannelType == ChannelType.Television ? g_Player.MediaType.Recording : g_Player.MediaType.Radio))
        {
          if (MediaPortal.Util.Utils.IsVideo(fileName))
          {
            g_Player.ShowFullScreenWindow();

            if (jumpToTime > 0
                && jumpToTime > g_Player.Duration - 3)
            {
              jumpToTime = (int) g_Player.Duration - 3;
            }
            g_Player.SeekAbsolute(jumpToTime <= 0 ? 0 : jumpToTime);
          }
          g_Player.PlayBackStopped += new MediaPortal.Player.g_Player.StoppedHandler(OnPlayRecordingBackStopped);
          g_Player.PlayBackEnded += new MediaPortal.Player.g_Player.EndedHandler(OnPlayRecordingBackEnded);
        }
        else
        {
          _playingRecording = null;
          _playingRecordingFileName = null;
        }
      }
    }

    private void OnPlayRecordingBackStopped(MediaPortal.Player.g_Player.MediaType type, int stoptime, string filename)
    {
      if (g_Player.currentFileName == _playingRecordingFileName)
      {
        _playingRecordingFileName = null;
        _playingRecording = null;

        if (stoptime >= g_Player.Duration)
        {
          // Temporary workaround before end of stream gets properly implemented.
          stoptime = 0;
        }
        using (TvControlServiceAgent tvControlAgent = new TvControlServiceAgent())
        {
          tvControlAgent.SetRecordingLastWatchedPosition(filename, stoptime);
        }
      }
    }

    private void OnPlayRecordingBackEnded(MediaPortal.Player.g_Player.MediaType type, string filename)
    {
      if (g_Player.currentFileName == _playingRecordingFileName)
      {
        g_Player.Stop();
        _playingRecordingFileName = null;
        _playingRecording = null;

        using (TvControlServiceAgent tvControlAgent = new TvControlServiceAgent())
        {
          tvControlAgent.SetRecordingLastWatchedPosition(filename, 0);
        }
      }
    }


    private void RememberActiveRecordingPosition()
    {
      if (_playingRecording != null)
      {
        if (g_Player.IsTVRecording
            && g_Player.currentFileName == _playingRecordingFileName)
        {
          if (g_Player.CurrentPosition < g_Player.Duration)
          {
            using (TvControlServiceAgent tvControlAgent = new TvControlServiceAgent())
            {
              tvControlAgent.SetRecordingLastWatchedPosition(_playingRecordingFileName, (int) g_Player.CurrentPosition);
            }
          }
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
