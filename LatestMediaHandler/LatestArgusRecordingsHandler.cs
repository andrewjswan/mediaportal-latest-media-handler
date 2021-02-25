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
// The methods GetLogoPath and MakeValidFileName, and the string _cacheBasePath
// and its initiation in the constructor are slightly modified copies of the
// methods of the same name in ChannelLogosCache.cs in ArgusTV.Client.Common
//***********************************************************************

extern alias LMHNLog;

using ArgusTV.DataContracts;
using ArgusTV.ServiceProxy;

using MediaPortal.Dialogs;
using MediaPortal.GUI.Library;
using MediaPortal.Player;

using LMHNLog.NLog;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace LatestMediaHandler
{
  internal class LatestArgusRecordingsHandler
  {
    #region declarations

    private Logger logger = LogManager.GetCurrentClassLogger();
    private bool _isGetTypeRunningOnThisThread /* = false*/;
    private ArgusTV.DataContracts.Recording _playingRecording;
    private string _playingRecordingFileName;
    private Hashtable latestArgusRecordings;
    private ArrayList facadeCollection = new ArrayList();
    internal ArrayList images = new ArrayList();
    internal ArrayList imagesThumbs = new ArrayList();
    private LatestsCollection result = null;
    private LatestTVAllRecordingsHandler parent = null;
    private static string _cacheBasePath;
    
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

    internal LatestArgusRecordingsHandler(LatestTVAllRecordingsHandler P)
    {
      parent = P;
      _cacheBasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"ARGUS TV\LogosCache");
    }

    private Hashtable LatestArgusRecordings
    {
      get { return latestArgusRecordings; }
      set { latestArgusRecordings = value; }
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
            GetArgusRecordings();
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
        if (Proxies.IsInitialized)
        {
          List<ActiveRecording> recordings = null;
          recordings = null;
          recordings = Proxies.ControlService.GetActiveRecordings().Result;
          if (recordings != null)
          {
            foreach (ActiveRecording rec in recordings)
            {
              string logoImagePath = GetLogoPath(rec.Program.Channel.ChannelId, rec.Program.Channel.DisplayName, 84, 84);
              if (string.IsNullOrEmpty(logoImagePath) || !System.IO.File.Exists(logoImagePath))
              {
                logoImagePath = "defaultVideoBig.png";
              }
              latestRecordings.Add(new LatestRecording(rec.Program.Title, rec.Program.Category,
                                                       rec.Program.ActualStartTime,
                                                       String.Format("{0:" + Utils.DateFormat + "}", rec.Program.ActualStartTime),
                                                       rec.Program.ActualStartTime.ToString("HH:mm", CultureInfo.CurrentCulture),
                                                       String.Format("{0:" + Utils.DateFormat + "}", rec.Program.StopTime),
                                                       rec.Program.StopTime.ToString("HH:mm", CultureInfo.CurrentCulture),
                                                       rec.Program.Channel.DisplayName, logoImagePath));
              Utils.ThreadToSleep();
            }
          }
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
      catch (Exception ex)
      {
        logger.Error("UpdateActiveRecordings: " + ex.ToString());
      }
      UpdateSheduledTVRecordings();
    }

    internal void UpdateSheduledTVRecordings()
    {
      try
      {
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
        if (Proxies.IsInitialized)
        {
          List<UpcomingRecording> recordings = null;
          recordings = Proxies.ControlService.GetAllUpcomingRecordings(UpcomingRecordingsFilter.Recordings | UpcomingRecordingsFilter.CancelledByUser, false).Result;

          int i = 1;
          RecordingsCollection latestRecordings = new RecordingsCollection();
          if (recordings != null)
          {
            foreach (UpcomingRecording rec in recordings)
            {
              string logoImagePath = GetLogoPath(rec.Program.Channel.ChannelId, rec.Program.Channel.DisplayName, 84, 84);
              if (string.IsNullOrEmpty(logoImagePath) || !System.IO.File.Exists(logoImagePath))
                logoImagePath = "defaultVideoBig.png";

              latestRecordings.Add(new LatestRecording(rec.Program.Title, null, rec.Program.ActualStartTime,
                                                       String.Format("{0:" + Utils.DateFormat + "}", rec.Program.ActualStartTime),
                                                       rec.Program.ActualStartTime.ToString("HH:mm", CultureInfo.CurrentCulture),
                                                       String.Format("{0:" + Utils.DateFormat + "}", rec.Program.ActualStopTime),
                                                       rec.Program.ActualStopTime.ToString("HH:mm", CultureInfo.CurrentCulture),
                                                       rec.Program.Channel.DisplayName, logoImagePath));
              Utils.ThreadToSleep();
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
        }
      }
      catch (Exception ex)
      {
        logger.Error("GetTVRecordings (Scheduled Recordings): " + ex.ToString());
      }
    }

    internal LatestsCollection GetArgusRecordings()
    {
      UpdateSheduledTVRecordings();
      LatestsCollection latests = new LatestsCollection();
      LatestsCollection resultTmp = new LatestsCollection();
      try
      {
        // parent.CurrentFacade.Facade = Utils.GetLatestsFacade(parent.CurrentFacade.ControlID);
        if (parent.CurrentFacade.Facade != null)
        {
          Utils.ClearFacade(ref parent.CurrentFacade.Facade);
        }
        if (facadeCollection != null)
        {
          facadeCollection.Clear();
        }
        if (Proxies.IsInitialized)
        {
          List<RecordingSummary> recordings = null;
          DateTime _time = DateTime.Now;
          List<Channel> channels = new List<Channel>();
          channels = Proxies.SchedulerService.GetAllChannels(ChannelType.Television).Result;
          List<Guid> chids = new List<Guid>();
          foreach (Channel ch in channels)
          {
            chids.Add(ch.ChannelId);
          }
          IEnumerable<Guid> iechids = chids;
          List<List<RecordingSummary>> rr = new List<List<RecordingSummary>>();
          rr = Proxies.ControlService.GetRecordingsOnChannels(iechids).Result;
          recordings = rr.SelectMany(z => z).ToList();
          recordings.Sort(new RecordingsComparer());
          if (recordings != null)
          {
            foreach (RecordingSummary rec in recordings)
            {
              logger.Debug("Recording: " + rec.ChannelId + " " + rec.RecordingStartTime.ToLocalTime() + " " + rec.Title);
              if (latests.Count > Utils.FacadeMaxNum)
              {
                break;
              }
              if (rec.ChannelType == ChannelType.Television)
              {
                Recording recording = Proxies.ControlService.GetRecordingById(rec.RecordingId).Result;
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

                if (Utils.LatestTVRecordingsWatched)
                {
                  if (!rec.LastWatchedTime.HasValue)
                  {
                    latests.Add(new Latest()
                    {
                      DateTimeAdded = rec.StartTime,
                      DateTimeWatched = rec.StartTime,
                      Thumb = thumbNail,
                      Title = rec.Title,
                      Genre = rec.Category,
                      Summary = _summary,
                      Playable = rec
                    });
                  }
                }
                else
                {
                  latests.Add(new Latest()
                  {
                    DateTimeAdded = rec.StartTime,
                    DateTimeWatched = rec.StartTime,
                    Thumb = thumbNail,
                    Title = rec.Title,
                    Genre = rec.Category,
                    Summary = _summary,
                    Playable = rec
                  }) ;
                }
              }
              Utils.ThreadToSleep();
            }
          }

          int x = 0;
          int i0 = 1;
          if (latests != null && latests.Count > 0)
          {
            // latests.Sort(new LatestAddedComparerDesc());
            Utils.SortLatests(ref latests, parent.CurrentFacade.Type, parent.CurrentFacade.LeftToRight);
            latestArgusRecordings = new Hashtable();
            for (int x0 = 0; x0 < latests.Count; x0++)
            {
              //latests[x0].DateAdded = latests[x0].DateAdded.Substring(0, 10);
              latests[x0].Directory = Utils.GetGetDirectoryName(((RecordingSummary) latests[x0].Playable).RecordingFileName);
              latests[x0].Fanart = (Utils.FanartHandler ? UtilsFanartHandler.GetFanartForLatest(latests[x0].Title) : string.Empty);

              resultTmp.Add(latests[x0]);
              if (result == null || result.Count == 0)
              {
                result = resultTmp;
              }
              latestArgusRecordings.Add(i0, latests[x].Playable);
              //if (facade != null)
              //{
              AddToFilmstrip(parent.CurrentFacade.Facade, latests[x], i0);
              //}
              x++;
              i0++;
              if (x == Utils.FacadeMaxNum)
              {
                break;
              }
            }
          }
        }
        Utils.UpdateFacade(ref parent.CurrentFacade.Facade, parent.CurrentFacade);

        if (latests != null)
          latests.Clear();
        latests = null;
      }
      catch
      {
        if (latests != null)
          latests.Clear();
        latests = null;
      }
      result = resultTmp;
      return result;
    }

    private static string GetLogoPath(Guid channelId, string channelDisplayName, int width, int height)
    {
      string cachePath = Path.Combine(_cacheBasePath, width.ToString(CultureInfo.InvariantCulture) + "x" + height.ToString(CultureInfo.InvariantCulture));
      Directory.CreateDirectory(cachePath);

      string logoImagePath = Path.Combine(cachePath, MakeValidFileName(channelDisplayName) + ".png");

      DateTime modifiedDateTime = DateTime.MinValue;
      if (File.Exists(logoImagePath))
      {
        modifiedDateTime = File.GetLastWriteTime(logoImagePath);
      }

      byte[] imageBytes = Proxies.SchedulerService.GetChannelLogo(channelId, width, height, modifiedDateTime).Result;
      if (imageBytes == null)
      {
        if (File.Exists(logoImagePath))
        {
          File.Delete(logoImagePath);
        }
      }
      else if (imageBytes.Length > 0)
      {
        using (FileStream imageStream = new FileStream(logoImagePath, FileMode.Create))
        {
          imageStream.Write(imageBytes, 0, imageBytes.Length);
          imageStream.Close();
        }
      }

      return File.Exists(logoImagePath) ? logoImagePath : null;
    }

    private static string MakeValidFileName(string fileName)
    {
      foreach (char c in Path.GetInvalidFileNameChars())
      {
        fileName = fileName.Replace(c.ToString(), String.Empty);
      }
      return fileName.TrimEnd('.', ' ');
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
        item.Label3 = latests.DateTimeAdded.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture);
        item.IsFolder = false;
        item.DVDLabel = latests.Fanart;
        item.OnItemSelected += new GUIListItem.ItemSelectedHandler(item_OnItemSelected);
        if (facade != null)
        {
          facade.Add(item);
        }
        facadeCollection.Add(item);
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
        if (item != null && parent.CurrentFacade.SelectedItem != item.ItemId)
        {
          string summary = (string.IsNullOrEmpty(result[(item.ItemId - 1)].Summary) ? Translation.NoDescription : result[(item.ItemId - 1)].Summary);
          string summaryoutline = Utils.GetSentences(summary, Utils.LatestPlotOutlineSentencesNum);
          Utils.SetProperty("#latestMediaHandler.tvrecordings.selected.thumb", item.IconImageBig);
          Utils.SetProperty("#latestMediaHandler.tvrecordings.selected.title", item.Label);
          Utils.SetProperty("#latestMediaHandler.tvrecordings.selected.dateAdded", item.Label3);
          Utils.SetProperty("#latestMediaHandler.tvrecordings.selected.genre", item.Label2);
          Utils.SetProperty("#latestMediaHandler.tvrecordings.selected.startTime", item.Label3);
          Utils.SetProperty("#latestMediaHandler.tvrecordings.selected.endTime", result[(item.ItemId - 1)].DateTimeWatched.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture));
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
        return false;
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
              Utils.SetProperty("#latestMediaHandler.tvrecordings.selected.fanart2", string.Empty);
              parent.ShowFanart = 2;
            }
            else
            {
              Utils.SetProperty("#latestMediaHandler.tvrecordings.selected.fanart2", _image);
              Utils.SetProperty("#latestMediaHandler.tvrecordings.selected.showfanart2", "true");
              Utils.SetProperty("#latestMediaHandler.tvrecordings.selected.showfanart1", "false");
              Thread.Sleep(1000);
              Utils.SetProperty("#latestMediaHandler.tvrecordings.selected.fanart1", string.Empty);
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

    internal void PlayRecording(int index)
    {
      RecordingSummary recSummary = (RecordingSummary) latestArgusRecordings[index];
      Recording rec = null;

      rec = Proxies.ControlService.GetRecordingById(recSummary.RecordingId).Result;

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


      //send a message to the Argus plugin, to start playing the recording
      //I use this "GUI_MSG_RECORDER_VIEW_CHANNEL" event because it's a save one to use(no other components in mediaportal listen to this event)
      GUIMessage msg = new GUIMessage(GUIMessage.MessageType.GUI_MSG_RECORDER_VIEW_CHANNEL, 0, 0, 0, 0, 0, null);
      msg.Object = rec;
      msg.Param2 = jumpToTime;
      msg.Param1 = 5577; //just some indentification
      GUIGraphicsContext.SendMessage(msg);
      msg = null;
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
        Proxies.ControlService.SetRecordingLastWatchedPosition(filename, stoptime);
      }
    }

    private void OnPlayRecordingBackEnded(MediaPortal.Player.g_Player.MediaType type, string filename)
    {
      if (g_Player.currentFileName == _playingRecordingFileName)
      {
        g_Player.Stop();
        _playingRecordingFileName = null;
        _playingRecording = null;

        Proxies.ControlService.SetRecordingLastWatchedPosition(filename, 0);
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
            Proxies.ControlService.SetRecordingLastWatchedPosition(_playingRecordingFileName, (int) g_Player.CurrentPosition);
          }
        }
      }
    }
  
    private class RecordingsComparer : IComparer<RecordingSummary>
    {
      public int Compare(RecordingSummary rec1, RecordingSummary rec2)
      {
        int returnValue = 1;
        if (rec1 is RecordingSummary && rec2 is RecordingSummary)
        {
          if (rec1.StartTime.CompareTo(rec2.StartTime) > 0) return -1;
          else if (rec1.StartTime.CompareTo(rec2.StartTime) < 0) return 1;
          else return 0;
        }
        return returnValue;
      }
    }
  }
}
