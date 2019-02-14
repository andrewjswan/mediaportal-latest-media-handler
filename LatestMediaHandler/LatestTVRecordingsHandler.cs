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
using MediaPortal.Util;

using LMHNLog.NLog;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;

using TvControl;
using TvDatabase;
using TvPlugin;

namespace LatestMediaHandler
{
  internal class LatestTVRecordingsHandler
  {
    #region declarations

    private Logger logger = LogManager.GetCurrentClassLogger();
    private Hashtable latestTVRecordings;
    private TvDatabase.Recording _rec;
    private String _filename;
    private ArrayList facadeCollection = new ArrayList();
    internal ArrayList images = new ArrayList();
    internal ArrayList imagesThumbs = new ArrayList();
    private LatestsCollection result = null;
    private LatestTVAllRecordingsHandler parent = null;

    #endregion

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

    public LatestTVRecordingsHandler(LatestTVAllRecordingsHandler P)
    {
      parent = P;
    }

    private Hashtable LatestTVRecordings
    {
      get { return latestTVRecordings; }
      set { latestTVRecordings = value; }
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
        if (!Utils.LatestTVRecordingsWatched)
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
        dlg.DoModal(Utils.ActiveWindow);

        if (dlg.SelectedLabel == -1)
          return;

        switch (dlg.SelectedId)
        {
          case 1:
          {
            // parent.CurrentFacade.Facade = Utils.GetLatestsFacade(parent.CurrentFacade.ControlID);
            if (parent.CurrentFacade.Facade != null)
            {
              PlayRecording(parent.CurrentFacade.Facade.SelectedListItem.ItemId);
            }
            break;
          }
          case 2:
          {
            Utils.LatestTVRecordingsWatched = !Utils.LatestTVRecordingsWatched;
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

          Utils.SetProperty("#latestMediaHandler.tvrecordings.active.count", "0");
          for (int z = 1; z <= Utils.LatestsMaxTVNum; z++)
          {
            Utils.SetProperty("#latestMediaHandler.tvrecordings.active" + z + ".title", string.Empty);
            Utils.SetProperty("#latestMediaHandler.tvrecordings.active" + z + ".genre", string.Empty);
            Utils.SetProperty("#latestMediaHandler.tvrecordings.active" + z + ".startTime", string.Empty);
            Utils.SetProperty("#latestMediaHandler.tvrecordings.active" + z + ".startDate", string.Empty);
            Utils.SetProperty("#latestMediaHandler.tvrecordings.active" + z + ".endTime", string.Empty);
            Utils.SetProperty("#latestMediaHandler.tvrecordings.active" + z + ".endDate", string.Empty);
            Utils.SetProperty("#latestMediaHandler.tvrecordings.active" + z + ".channel", string.Empty);
            Utils.SetProperty("#latestMediaHandler.tvrecordings.active" + z + ".channelLogo", string.Empty);
          }

          int i = 1;
          RecordingsCollection latestRecordings = new RecordingsCollection();
          foreach (TvDatabase.Recording rec in recordings)
          {
            string logoImagePath = MediaPortal.Util.Utils.GetCoverArt(Thumbs.TVChannel, rec.ReferencedChannel().DisplayName);

            if (string.IsNullOrEmpty(logoImagePath))
              logoImagePath = "defaultVideoBig.png";

            latestRecordings.Add(new LatestRecording(rec.Title, rec.Genre, rec.StartTime,
                                                     String.Format("{0:" + Utils.DateFormat + "}", rec.StartTime), 
                                                     rec.StartTime.ToString("HH:mm", CultureInfo.CurrentCulture),
                                                     String.Format("{0:" + Utils.DateFormat + "}", rec.EndTime),
                                                     rec.EndTime.ToString("HH:mm", CultureInfo.CurrentCulture),
                                                     rec.ReferencedChannel().DisplayName, 
                                                     logoImagePath));
            Utils.ThreadToSleep();
          }

          latestRecordings.Sort(new LatestRecordingsComparer());

          Utils.SetProperty("#latestMediaHandler.tvrecordings.active.count", latestRecordings.Count.ToString());
          for (int x0 = 0; x0 < latestRecordings.Count; x0++)
          {
            Utils.SetProperty("#latestMediaHandler.tvrecordings.active" + i + ".title", latestRecordings[x0].Title);
            Utils.SetProperty("#latestMediaHandler.tvrecordings.active" + i + ".genre", latestRecordings[x0].Genre);
            Utils.SetProperty("#latestMediaHandler.tvrecordings.active" + i + ".startTime", latestRecordings[x0].StartTime);
            Utils.SetProperty("#latestMediaHandler.tvrecordings.active" + i + ".startDate", latestRecordings[x0].StartDate);
            Utils.SetProperty("#latestMediaHandler.tvrecordings.active" + i + ".endTime", latestRecordings[x0].EndTime);
            Utils.SetProperty("#latestMediaHandler.tvrecordings.active" + i + ".endDate", latestRecordings[x0].EndDate);
            Utils.SetProperty("#latestMediaHandler.tvrecordings.active" + i + ".channel", latestRecordings[x0].Channel);
            Utils.SetProperty("#latestMediaHandler.tvrecordings.active" + i + ".channelLogo", latestRecordings[x0].ChannelLogo);
            if (i == Utils.LatestsMaxTVNum)
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
      UpdateSheduledTVRecordings();
    }

    internal void UpdateSheduledTVRecordings()
    {
      if (TVHome.Connected)
      {
        try
        {
          IList<TvDatabase.Schedule> schedules = TvDatabase.Schedule.ListAll();

          Utils.SetProperty("#latestMediaHandler.tvrecordings.scheduled.count", "0");
          for (int z = 1; z <= Utils.LatestsMaxTVNum; z++)
          {
            Utils.SetProperty("#latestMediaHandler.tvrecordings.scheduled" + z + ".title", string.Empty);
            Utils.SetProperty("#latestMediaHandler.tvrecordings.scheduled" + z + ".startTime", string.Empty);
            Utils.SetProperty("#latestMediaHandler.tvrecordings.scheduled" + z + ".startDate", string.Empty);
            Utils.SetProperty("#latestMediaHandler.tvrecordings.scheduled" + z + ".endTime", string.Empty);
            Utils.SetProperty("#latestMediaHandler.tvrecordings.scheduled" + z + ".endDate", string.Empty);
            Utils.SetProperty("#latestMediaHandler.tvrecordings.scheduled" + z + ".channel", string.Empty);
            Utils.SetProperty("#latestMediaHandler.tvrecordings.scheduled" + z + ".channelLogo", string.Empty);
          }

          int i = 1;
          RecordingsCollection latestRecordings = new RecordingsCollection();

          foreach (TvDatabase.Schedule schedule in schedules)
          {
            string logoImagePath = MediaPortal.Util.Utils.GetCoverArt(Thumbs.TVChannel, schedule.ReferencedChannel().DisplayName);
            if (string.IsNullOrEmpty(logoImagePath))
              logoImagePath = "defaultVideoBig.png";

            if (schedule.ScheduleType != (int) ScheduleRecordingType.Once)
            {
              IList<Schedule> seriesList = TVHome.Util.GetRecordingTimes(schedule);
              for (int serieNr = 0; serieNr < seriesList.Count; ++serieNr)
              {
                Schedule recSeries = seriesList[serieNr];
                if (DateTime.Now > recSeries.EndTime)
                  continue;
                if (recSeries.Canceled != Schedule.MinSchedule)
                  continue;

                //Program program = Program.RetrieveByTitleTimesAndChannel(schedule.ProgramName, schedule.StartTime,schedule.EndTime, schedule.IdChannel);                           

                latestRecordings.Add(new LatestRecording(recSeries.ProgramName, null, recSeries.StartTime,
                                                         String.Format("{0:" + Utils.DateFormat + "}", recSeries.StartTime),
                                                         recSeries.StartTime.ToString("HH:mm", CultureInfo.CurrentCulture),
                                                         String.Format("{0:" + Utils.DateFormat + "}", recSeries.EndTime),
                                                         recSeries.EndTime.ToString("HH:mm", CultureInfo.CurrentCulture),
                                                         recSeries.ReferencedChannel().DisplayName, 
                                                         logoImagePath));
                Utils.ThreadToSleep();
              }
            }
            else
            {
              if (schedule.IsSerieIsCanceled(schedule.StartTime, schedule.IdChannel))
                continue;
              //Test if this is an instance of a series recording, if so skip it.
              if (schedule.ReferencedSchedule() != null)
                continue;

              latestRecordings.Add(new LatestRecording(schedule.ProgramName, null, schedule.StartTime,
                                                       String.Format("{0:" + Utils.DateFormat + "}", schedule.StartTime),
                                                       schedule.StartTime.ToString("HH:mm", CultureInfo.CurrentCulture),
                                                       String.Format("{0:" + Utils.DateFormat + "}", schedule.EndTime),
                                                       schedule.EndTime.ToString("HH:mm", CultureInfo.CurrentCulture),
                                                       schedule.ReferencedChannel().DisplayName, 
                                                       logoImagePath));
              Utils.ThreadToSleep();
            }
          }

          latestRecordings.Sort(new LatestRecordingsComparer());

          Utils.SetProperty("#latestMediaHandler.tvrecordings.scheduled.count", latestRecordings.Count.ToString());
          for (int x0 = 0; x0 < latestRecordings.Count; x0++)
          {
            Utils.SetProperty("#latestMediaHandler.tvrecordings.scheduled" + i + ".title", latestRecordings[x0].Title);
            Utils.SetProperty("#latestMediaHandler.tvrecordings.scheduled" + i + ".startTime", latestRecordings[x0].StartTime);
            Utils.SetProperty("#latestMediaHandler.tvrecordings.scheduled" + i + ".startDate", latestRecordings[x0].StartDate);
            Utils.SetProperty("#latestMediaHandler.tvrecordings.scheduled" + i + ".endTime", latestRecordings[x0].EndTime);
            Utils.SetProperty("#latestMediaHandler.tvrecordings.scheduled" + i + ".endDate", latestRecordings[x0].EndDate);
            Utils.SetProperty("#latestMediaHandler.tvrecordings.scheduled" + i + ".channel", latestRecordings[x0].Channel);
            Utils.SetProperty("#latestMediaHandler.tvrecordings.scheduled" + i + ".channelLogo", latestRecordings[x0].ChannelLogo);

            if (i == Utils.LatestsMaxTVNum)
              break;
            i++;
          }
        }
        catch (Exception ex)
        {
          logger.Error("GetTVRecordings (Scheduled Recordings): " + ex.ToString());
        }
      }
    }

    internal LatestsCollection GetTVRecordings()
    {
      UpdateSheduledTVRecordings();
      if (TVHome.Connected)
      {
        LatestMediaHandler.LatestsCollection resultTmp = new LatestMediaHandler.LatestsCollection();
        LatestsCollection latests = new LatestsCollection();
        try
        {
          IList<TvDatabase.Recording> recordings = TvDatabase.Recording.ListAll();
          int x = 0;
          int i0 = 1;
          foreach (TvDatabase.Recording rec in recordings)
          {
            if (!Utils.LatestTVRecordingsUnfinished && IsRecordingActual(rec))
              continue ;
            if (Utils.LatestTVRecordingsWatched && (rec.TimesWatched > 0))
              continue ;

            latests.Add(new Latest(rec.StartTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture), "defaultTVBig.png", 
                                   null, 
                                   rec.Title, rec.EndTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture), 
                                   null, null, 
                                   rec.Genre, 
                                   null, null, null, null, null, null, 
                                   rec.EpisodeNum, 
                                   rec.EpisodeName, 
                                   rec, 
                                   null,
                                   rec.Description,
                                   rec.SeriesNum));
            Utils.ThreadToSleep();
          }

          // parent.CurrentFacade.Facade = Utils.GetLatestsFacade(parent.CurrentFacade.ControlID);
          if (parent.CurrentFacade.Facade != null)
          {
            Utils.ClearFacade(ref parent.CurrentFacade.Facade);
          }
          if (facadeCollection != null)
          {
            facadeCollection.Clear();
          }

          // latests.Sort(new LatestAddedComparerDesc());
          Utils.SortLatests(ref latests, parent.CurrentFacade.Type, parent.CurrentFacade.LeftToRight);

          latestTVRecordings = new Hashtable();
          for (int x0 = 0; x0 < latests.Count; x0++)
          {
            //latests[x0].DateAdded = latests[x0].DateAdded.Substring(0, 10);
            try
            {
              DateTime dTmp = DateTime.Parse(latests[x0].DateAdded);
              latests[x0].DateAdded = String.Format("{0:" + Utils.DateFormat + "}", dTmp);
            }
            catch
            {
            }
            string _filename = ((Recording) latests[x0].Playable).FileName;

            string thumbNail = string.Empty;
            if (LatestMediaHandlerSetup.MpVersion.CompareTo("1.03") > 0)
            {
              // MP 1.4
              logger.Debug("GetTVRecordings [" + LatestMediaHandlerSetup.MpVersion + "] Thumbs method: 1.4 and above ...");
              thumbNail = TVRecordingsThumbnailHandler.GetThumb(_filename);
            }
            else
            {
              // MP 1.3 or older
              logger.Debug("GetTVRecordings [" + LatestMediaHandlerSetup.MpVersion + "] Thumbs method: 1.3 or older ...");
              thumbNail = string.Format(CultureInfo.CurrentCulture, "{0}\\{1}{2}", Thumbs.TVRecorded,
                Path.ChangeExtension(MediaPortal.Util.Utils.SplitFilename(_filename), null),
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
                thumbNail = string.Format(CultureInfo.CurrentCulture, "{0}{1}", Path.ChangeExtension(_filename, null), MediaPortal.Util.Utils.GetThumbExtension());
              }
              if (!File.Exists(thumbNail))
              {
                thumbNail = "defaultTVBig.png";
              }
            }

            latests[x0].Fanart = (Utils.FanartHandler ? UtilsFanartHandler.GetFanartForLatest(latests[x0].Title) : string.Empty);
            latests[x0].Directory = Utils.GetGetDirectoryName(TVUtil.GetFileNameForRecording(((Recording) latests[x0].Playable)));
            latests[x0].Thumb = thumbNail;

            resultTmp.Add(latests[x0]);
            if (result == null || result.Count == 0)
              result = resultTmp;

            latestTVRecordings.Add(i0, latests[x].Playable);
            AddToFilmstrip(parent.CurrentFacade.Facade, latests[x], i0);

            x++;
            i0++;
            if (x == Utils.FacadeMaxNum)
              break;
          }
          Utils.UpdateFacade(ref parent.CurrentFacade.Facade, parent.CurrentFacade);

          if (latests != null)
            latests.Clear();
          latests = null;
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
          if (latests != null)
            latests.Clear();
          latests = null;
          logger.Error("GetTVRecordings: " + ex.ToString());
        }
        result = resultTmp;
      }
      return result;
    }

    private void AddToFilmstrip(GUIFacadeControl facade, Latest latests, int x)
    {
      try
      {
        Utils.LoadImage(latests.Thumb, ref imagesThumbs);

        //Add to filmstrip
        GUIListItem item = new GUIListItem();
        item.ItemId = x;
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
          facade.Add(item);
        facadeCollection.Add(item);

        if (x == 1)
          UpdateSelectedProperties(item);
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
        if (item != null && parent.CurrentFacade.SelectedItem != item.ItemId)
        {
          string summary = (string.IsNullOrEmpty(result[(item.ItemId - 1)].Summary) ? Translation.NoDescription : result[(item.ItemId - 1)].Summary);
          string summaryoutline = Utils.GetSentences(summary, Utils.LatestPlotOutlineSentencesNum);
          Utils.SetProperty("#latestMediaHandler.tvrecordings.selected.thumb", item.IconImageBig);
          Utils.SetProperty("#latestMediaHandler.tvrecordings.selected.title", item.Label);
          Utils.SetProperty("#latestMediaHandler.tvrecordings.selected.dateAdded", item.Label3);
          Utils.SetProperty("#latestMediaHandler.tvrecordings.selected.genre", item.Label2);
          Utils.SetProperty("#latestMediaHandler.tvrecordings.selected.startTime", item.Label3);
          Utils.SetProperty("#latestMediaHandler.tvrecordings.selected.endTime", result[(item.ItemId - 1)].Subtitle);
          Utils.SetProperty("#latestMediaHandler.tvrecordings.selected.summary", summary);
          Utils.SetProperty("#latestMediaHandler.tvrecordings.selected.summaryoutline", summaryoutline);
          Utils.SetProperty("#latestMediaHandler.tvrecordings.selected.directory", result[(item.ItemId - 1)].Directory);
          Utils.SetProperty("#latestMediaHandler.tvrecordings.selected.new", result[(item.ItemId - 1)].New);
          parent.CurrentFacade.SelectedItem = item.ItemId;

          // parent.CurrentFacade.Facade = Utils.GetLatestsFacade(parent.CurrentFacade.ControlID);
          if (parent.CurrentFacade.Facade != null)
          {
            parent.CurrentFacade.FocusedID = parent.CurrentFacade.Facade.SelectedListItemIndex;
          }
        }
      }
      catch (Exception ex)
      {
        logger.Error("UpdateSelectedProperties: " + ex.ToString());
      }
    }

    internal bool GetRecordingRedDot()
    {
      try
      {
        if (TVHome.Connected)
        {
          return TVHome.IsAnyCardRecording;
        }
      }
      catch (Exception ex)
      {
        logger.Error("GetRecordingRedDot: " + ex.ToString());
      }
      return false;
    }

    internal void UpdateSelectedImageProperties()
    {
      try
      {
        // parent.CurrentFacade.Facade = Utils.GetLatestsFacade(parent.CurrentFacade.ControlID);
        if (parent.CurrentFacade.Facade != null && parent.CurrentFacade.Facade.Focus && parent.CurrentFacade.Facade.SelectedListItem != null)
        {
          int _id = parent.CurrentFacade.Facade.SelectedListItem.ItemId;
          String _image = parent.CurrentFacade.Facade.SelectedListItem.DVDLabel;
          if (parent.CurrentFacade.SelectedImage != _id)
          {
            Utils.LoadImage(_image, ref images);
            if (parent.ShowFanart == 1)
            {
              Utils.SetProperty("#latestMediaHandler.tvrecordings.selected.fanart1", _image);
              Utils.SetProperty("#latestMediaHandler.tvrecordings.selected.showfanart1", "true");
              Utils.SetProperty("#latestMediaHandler.tvrecordings.selected.showfanart2", "false");
              Thread.Sleep(1000);
              Utils.SetProperty("#latestMediaHandler.tvrecordings.selected.fanart2", "");
              parent.ShowFanart = 2;
            }
            else
            {
              Utils.SetProperty("#latestMediaHandler.tvrecordings.selected.fanart2", _image);
              Utils.SetProperty("#latestMediaHandler.tvrecordings.selected.showfanart2", "true");
              Utils.SetProperty("#latestMediaHandler.tvrecordings.selected.showfanart1", "false");
              Thread.Sleep(1000);
              Utils.SetProperty("#latestMediaHandler.tvrecordings.selected.fanart1", "");
              parent.ShowFanart = 1;
            }
            // Utils.UnLoadImage(_image, ref images);
            parent.CurrentFacade.SelectedImage = _id;
          }
        }
        else
        {
          Utils.SetProperty("#latestMediaHandler.tvrecordings.selected.fanart1", " ");
          Utils.SetProperty("#latestMediaHandler.tvrecordings.selected.fanart2", " ");
          Utils.SetProperty("#latestMediaHandler.tvrecordings.selected.showfanart1", "false");
          Utils.SetProperty("#latestMediaHandler.tvrecordings.selected.showfanart2", "false");
          Utils.UnLoadImages(ref images);
          parent.ShowFanart = 1;
          parent.CurrentFacade.SelectedImage = -1;
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
  }
}
