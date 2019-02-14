//***********************************************************************
// Assembly         : LatestMediaHandler
// Author           : cul8er
// Created          : 05-09-2010
//
// Last Modified By : ajs
// Last Modified On : 20-06-2016
// Description      : 
//
// Copyright        : Open Source software licensed under the GNU/GPL agreement. Thanks to MediaPortal that created many of the functions used here.
//***********************************************************************
extern alias LMHNLog;

using LMHNLog.NLog;

using MediaPortal.Dialogs;
using MediaPortal.GUI.Library;
using MediaPortal.Profile;
using MediaPortal.TagReader;

using mvCentral.Database;

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace LatestMediaHandler
{
  internal class LatestMvCentralHandler
  {
    #region declarations    

    private static Logger logger = LogManager.GetCurrentClassLogger();

    private LatestsCollection latestMusicAlbums = null;
    internal Hashtable latestMusicAlbumsVideos;
    public Hashtable artistsWithImageMissing;

    private MediaPortal.Playlists.PlayListPlayer playlistPlayer;
    private bool _stripArtistPrefixes;

    private ArrayList facadeCollection = new ArrayList();
    internal ArrayList images = new ArrayList();
    internal ArrayList imagesThumbs = new ArrayList();

    private int showFanart = 1;
    private bool needCleanup = false;
    private int needCleanupCount = 0;
    private int currentFacade = 0;

    private static Object lockObject = new object();

    #endregion

    public const int ControlID = 919299280;
    public const int Play1ControlID = 91929928;
    public const int Play2ControlID = 91929929;
    public const int Play3ControlID = 91929930;
    public const int Play4ControlID = 91919907;

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

    internal LatestMvCentralHandler(int id = ControlID)
    {
      artistsWithImageMissing = new Hashtable();

      ControlIDFacades = new List<LatestsFacade>();
      ControlIDPlays = new List<int>();
      //
      ControlIDFacades.Add(new LatestsFacade(id, "MvCentral"));
      if (id == ControlID)
      {
        ControlIDPlays.Add(Play1ControlID);
        ControlIDPlays.Add(Play2ControlID);
        ControlIDPlays.Add(Play3ControlID);
        ControlIDPlays.Add(Play4ControlID);
      }

      switch (Utils.LatestMvCentralThumbType)
      {
        case 1:
          CurrentFacade.ThumbType = LatestsFacadeThumbType.Artist;
          break;
        case 2:
          CurrentFacade.ThumbType = LatestsFacadeThumbType.Album;
          break;
        case 3:
          CurrentFacade.ThumbType = LatestsFacadeThumbType.Track;
          break;
      }

      Utils.ClearSelectedMusicProperty(CurrentFacade);
      EmptyLatestMediaProperties();
    }

    internal LatestMvCentralHandler(LatestsFacade facade) : this (facade.ControlID)
    {
      ControlIDFacades[ControlIDFacades.Count - 1] = facade;
    }

    internal void GetLatestMediaInfoThread()
    {
      // logger.Debug("*** mvCentral: GetLatestMediaInfoThread...") ;
      // mvCentral
      if (Utils.LatestMvCentral)
      {
        try
        {
          RefreshWorker MyRefreshWorker = new RefreshWorker();
          MyRefreshWorker.RunWorkerCompleted += MyRefreshWorker.OnRunWorkerCompleted;
          MyRefreshWorker.RunWorkerAsync(this);
        }
        catch (Exception ex)
        {
          logger.Error("GetLatestMediaInfoThread: " + ex.ToString());
        }
      }
    }

    internal void EmptyLatestMediaProperties()
    {
      if (!MainFacade && !CurrentFacade.AddProperties)
      {
        Utils.SetProperty("#latestMediaHandler." + CurrentFacade.Handler.ToLowerInvariant() + (!MainFacade ? ".info." + CurrentFacade.ControlID.ToString() : string.Empty) + ".latest.enabled", "false");
        return;
      }

      Utils.ClearLatestsMusicProperty(CurrentFacade, MainFacade);
    }

    internal void GetLatestMediaInfo(bool _onStartUp)
    {
      // logger.Debug("*** mvCentral: GetLatestMediaInfo...") ;
      if (Interlocked.CompareExchange(ref CurrentFacade.Update, 1, 0) != 0)
        return;

      // logger.Debug("*** mvCentral: GetLatestMediaInfo...") ;
      artistsWithImageMissing = new Hashtable();
      if (!Utils.LatestMvCentral)
      {
        EmptyLatestMediaProperties();
        CurrentFacade.Update = 0;
        return;    
      }

      //MvCentral
      LatestsCollection hTable = GetLatestMusic(_onStartUp);
      LatestsToFilmStrip(latestMusicAlbums);

      if (MainFacade || CurrentFacade.AddProperties)
      { 
        EmptyLatestMediaProperties() ;

        if (hTable != null)
        {
          Utils.FillLatestsMusicProperty(CurrentFacade, hTable, MainFacade);
        }
      }

      if ((latestMusicAlbums != null) && (latestMusicAlbums.Count > 0))
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
        Utils.UpdateLatestsUpdate(Utils.LatestsCategory.MvCentral, DateTime.Now);
      }

      CurrentFacade.Update = 0;
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
        GUIListItem pItem = new GUIListItem();
        pItem.Label = Translation.Play;
        pItem.ItemId = 1;
        dlg.Add(pItem);

        // Thumb Types
        pItem = new GUIListItem();
        pItem.Label = "[^] " + CurrentFacade.ThumbType;
        pItem.ItemId = 2;
        dlg.Add(pItem);

        // Update
        pItem = new GUIListItem();
        pItem.Label = Translation.Update;
        pItem.ItemId = 3;
        dlg.Add(pItem);

        //Show Dialog
        dlg.DoModal(Utils.ActiveWindow);

        if (dlg.SelectedLabel < 0)
        {
          return;
        }

        switch (dlg.SelectedId)
        {
          case 1:
          {
            PlayMusicAlbum(GUIWindowManager.GetWindow(Utils.ActiveWindow));
            break;
          }
          case 2:
          {
            GUIDialogMenu ldlg = (GUIDialogMenu)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            if (ldlg == null) return;

            ldlg.Reset();
            ldlg.SetHeading(924);

            // Artist Thumb
            pItem = new GUIListItem();
            pItem.Label = (CurrentFacade.ThumbType == LatestsFacadeThumbType.Artist ? "[X] " : string.Empty) + Translation.MvCThumbArtist;
            pItem.ItemId = 1;
            ldlg.Add(pItem);

            // Album Thumb
            pItem = new GUIListItem();
            pItem.Label = (CurrentFacade.ThumbType == LatestsFacadeThumbType.Album ? "[X] " : string.Empty) + Translation.MvCThumbAlbum;
            pItem.ItemId = 2;
            ldlg.Add(pItem);

            // Track Thumb
            pItem = new GUIListItem();
            pItem.Label = (CurrentFacade.ThumbType == LatestsFacadeThumbType.Track ? "[X] " : string.Empty) + Translation.MvCThumbTrack;
            pItem.ItemId = 3;
            ldlg.Add(pItem);

            //Show Dialog
            ldlg.DoModal(Utils.ActiveWindow);

            if (ldlg.SelectedLabel < 0)
            {
              return;
            }

            if (ldlg.SelectedId == 1)
            {
              CurrentFacade.ThumbType = LatestsFacadeThumbType.Artist;
            }
            else if (ldlg.SelectedId == 2)
            {
              CurrentFacade.ThumbType = LatestsFacadeThumbType.Album;
            }
            else if (ldlg.SelectedId == 3)
            {
              CurrentFacade.ThumbType = LatestsFacadeThumbType.Track;
            }
            GetLatestMediaInfoThread();
            break;
          }
          case 3:
          {
            GetLatestMediaInfoThread();
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
    /// Returns last music track info added to MP music database.
    /// </summary>
    /// <returns>Hashtable containg artist names</returns>
    internal LatestsCollection GetLatestMusic(bool _onStartUp)
    {
      // logger.Debug("*** mvCentral: GetLatestMusic...") ;
      latestMusicAlbums = new LatestsCollection();
      latestMusicAlbumsVideos = new Hashtable();

      int x = 0;
      try
      {
        int i0 = 1;
        CurrentFacade.HasNew = false;

        List<DBTrackInfo> allTracks = DBTrackInfo.GetAll();
        // logger.Debug("*** mvCentral: GetLatestMusic: "+allTracks.Count) ;
        if (allTracks.Count > 0)
        {
          allTracks.Sort(delegate(DBTrackInfo p1, DBTrackInfo p2) { return p2.DateAdded.CompareTo(p1.DateAdded); });
          foreach (DBTrackInfo allTrack in allTracks)
          {
            bool isnew = false;

            string sArtist = string.Empty;
            string sArtistBio = string.Empty;
            string sArtistTag = string.Empty;
            string sGenre = string.Empty;
            string sAlbum = string.Empty;
            string sAlbumTag = string.Empty;
            string sYear = string.Empty;

            string artistThumb = string.Empty;
            string albumThumb = string.Empty;
            string trackThumb = string.Empty;

            string thumb = string.Empty;
            string sFileName = string.Empty;

            // Artist
            if (allTrack.ArtistInfo != null && allTrack.ArtistInfo.Count > 0)
            {
              sArtist = allTrack.ArtistInfo[0].Artist;
              sArtistBio = allTrack.ArtistInfo[0].bioSummary;
              sGenre = allTrack.ArtistInfo[0].Genre;
              artistThumb = allTrack.ArtistInfo[0].ArtFullPath; // ArtThumbFullPath

              foreach (string tag in allTrack.ArtistInfo[0].Tag)
              {
                sArtistTag += tag + "|";
              }
              sArtistTag = Utils.GetDistinct(sArtistTag);
            }

            // Album
            if (allTrack.AlbumInfo != null && allTrack.AlbumInfo.Count > 0)
            {
              sAlbum = allTrack.AlbumInfo[0].Album;
              albumThumb = allTrack.AlbumInfo[0].ArtFullPath;

              foreach (string tag in allTrack.AlbumInfo[0].Tag)
              {
                sAlbumTag += tag + "|";
              }
              sAlbumTag = Utils.GetDistinct(sAlbumTag);

              sYear = allTrack.AlbumInfo[0].YearReleased;
            }

            // Filename
            if (allTrack.LocalMedia != null && allTrack.LocalMedia.Count > 0)
            {
              sFileName = allTrack.LocalMedia[0].File.FullName;
            }

            // Genres or Tags: Artists genres -> Album tags -> Artist tags
            if (string.IsNullOrWhiteSpace(sGenre))
            {
              sGenre = !string.IsNullOrWhiteSpace(sAlbumTag) ? sAlbumTag : sArtistTag;
            }

            // Thumb
            trackThumb = allTrack.ArtFullPath;
            switch (CurrentFacade.ThumbType)
            {
              case LatestsFacadeThumbType.Artist:
                thumb = !string.IsNullOrEmpty(artistThumb) ? artistThumb : "DefaultArtistBig.png";
                break;
              case LatestsFacadeThumbType.Album:
                thumb = !string.IsNullOrEmpty(albumThumb) ? albumThumb : !string.IsNullOrEmpty(artistThumb) ? artistThumb : string.Empty;
                break;
              case LatestsFacadeThumbType.Track:
                thumb = !string.IsNullOrEmpty(trackThumb) ? trackThumb : !string.IsNullOrEmpty(albumThumb) ? artistThumb : !string.IsNullOrEmpty(artistThumb) ? artistThumb : string.Empty;
                break;
            }
            if (string.IsNullOrEmpty(thumb))
            {
              thumb = "DefaultAudioBig.png";
            }

            // Fanart
            string sFilename1 = string.Empty;
            string sFilename2 = string.Empty;
            try
            {
              Hashtable ht2 = (Utils.FanartHandler ? UtilsFanartHandler.GetMusicFanartForLatest(sArtist) : null);
              if (ht2 == null || ht2.Count < 1 && !_onStartUp)
              {
                if (Utils.FanartHandler)
                {
                  UtilsFanartHandler.ScrapeFanartAndThumb(sArtist, string.Empty);
                  ht2 = UtilsFanartHandler.GetMusicFanartForLatest(sArtist);
                }
              }

              if (ht2 == null || ht2.Count < 1)
              {
                if (Utils.FanartHandler)
                {
                  if (!artistsWithImageMissing.Contains(UtilsFanartHandler.GetFHArtistName(sArtist)))
                  {
                    artistsWithImageMissing.Add(UtilsFanartHandler.GetFHArtistName(sArtist), UtilsFanartHandler.GetFHArtistName(sArtist));
                  }
                }
                else
                {
                  if (!artistsWithImageMissing.Contains(sArtist))
                  {
                    artistsWithImageMissing.Add(sArtist, sArtist);
                  }
                }
              }

              if (ht2 != null)
              {
                IDictionaryEnumerator _enumerator = ht2.GetEnumerator();
                int i = 0;
                while (_enumerator.MoveNext())
                {
                  if (i == 0)
                    sFilename1 = _enumerator.Value.ToString();
                  if (i == 1)
                    sFilename2 = _enumerator.Value.ToString();
                  i++;
                  if (i > 1)
                    break;
                }
              }
            }
            catch { }
            if (string.IsNullOrWhiteSpace(sFilename1))
            {
              sFilename1 = sFilename2;
            }

            // Date added
            string dateAdded = string.Empty;
            try
            {
              dateAdded = String.Format("{0:" + Utils.DateFormat + "}", allTrack.DateAdded);
              isnew = (allTrack.DateAdded > Utils.NewDateTime);
              if (isnew)
              {
                CurrentFacade.HasNew = true;
              }
            }
            catch { }

            string fbanner = string.Empty;
            string fclearart = string.Empty;
            string fclearlogo = string.Empty;
            string fcd = string.Empty;

            if (Utils.FanartHandler)
            {
              Parallel.Invoke
              (
                () => fbanner = UtilsFanartHandler.GetFanartTVForLatestMedia(sArtist, string.Empty, string.Empty, Utils.FanartTV.MusicBanner),
                () => fclearart = UtilsFanartHandler.GetFanartTVForLatestMedia(sArtist, string.Empty, string.Empty, Utils.FanartTV.MusicClearArt),
                // () => fclearlogo = UtilsFanartHandler.GetFanartTVForLatestMedia(sArtist, string.Empty, string.Empty, Utils.FanartTV.MusicClearArt),
                () => fcd = UtilsFanartHandler.GetFanartTVForLatestMedia(sArtist, sAlbum, string.Empty, Utils.FanartTV.MusicCDArt)
              );
              fclearlogo = fclearart;
            }

            // Add to latest
            latestMusicAlbums.Add(new Latest(dateAdded, thumb, sFilename1, allTrack.Track,
                                             sFileName,
                                             sArtist, sAlbum,
                                             sGenre,
                                             allTrack.Rating.ToString(), allTrack.Rating.ToString(), 
                                             null, null, 
                                             sYear, 
                                             artistThumb, albumThumb, trackThumb, 
                                             null, null,
                                             sArtistBio, 
                                             null,
                                             fbanner, fclearart, fclearlogo, fcd,
                                             isnew)); 
            latestMusicAlbumsVideos.Add(i0, sFileName);
            Utils.ThreadToSleep();

            x++;
            i0++;
            if (x == Utils.FacadeMaxNum)
              break;
          }
        }
      }
      catch (Exception ex)
      {
        logger.Error("GetLatestMusic: " + ex.ToString());
      }
      return latestMusicAlbums;
    }

    public Hashtable GetLatestsList()
    {
      Hashtable ht = new Hashtable();
      if (latestMusicAlbums != null)
      {
        for (int i = 0; i < latestMusicAlbums.Count; i++)
        {
          var key = latestMusicAlbums[i].Artist + "#" + latestMusicAlbums[i].Album;
          if (!ht.Contains(key))
          {
            // logger.Debug("Make Latest List: MvCentral: " + latestMusicAlbums[i].Artist + " - " + latestMusicAlbums[i].Album);
            ht.Add(key, new string[2] { latestMusicAlbums[i].Artist, latestMusicAlbums[i].Album } ) ;
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
        MusicTag mt = new MusicTag();
        mt.Album = latests.Album;
        mt.Artist = latests.Artist;
        mt.AlbumArtist = latests.Artist;
        mt.FileName = latests.Subtitle;
        mt.Genre = latests.Genre;
        try
        {
          mt.Duration = Int32.Parse(latests.Runtime);
        }
        catch
        {
          mt.Duration = 0;
        }
        try
        {
          mt.Year = Int32.Parse(latests.Year);
        }
        catch
        {
          mt.Year = 0;
        }
        try
        {
          mt.Rating = Int32.Parse(latests.RoundedRating);
        }
        catch
        {
          mt.Rating = 0;
        }

        Utils.LoadImage(latests.Thumb, ref imagesThumbs);

        GUIListItem item = new GUIListItem();
        item.ItemId = x;
        item.IconImage = latests.Thumb; // .seasonIndex; // Artist Thumb
        item.IconImageBig = latests.Thumb; // .episodeIndex; // Album Thumb
        item.ThumbnailImage = latests.Thumb; // .thumbSeries; // Track Thumb
        item.Label = mt.Artist;
        item.Label2 = mt.Album;
        item.Label3 = latests.DateAdded;
        item.IsFolder = false;
        item.Path = latests.Genre;
        item.Duration = mt.Duration;
        item.MusicTag = mt;
        item.Year = mt.Year;
        item.DVDLabel = latests.Fanart;
        item.Rating = mt.Rating;
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
          AddToFilmstrip(lTable[i], i+1);
      }
    }

    internal void InitFacade()
    {
      if (!Utils.LatestMvCentral)
      {
        return;
      }

      try
      {
        lock (lockObject)
        {
          // LatestsToFilmStrip(latestMusicAlbums);

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
      if (!Utils.LatestMvCentral)
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
          Utils.FillSelectedMusicProperty(CurrentFacade, item, latestMusicAlbums[item.ItemId - 1]);
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
              Utils.SetProperty("#latestMediaHandler.mvcentral.selected.fanart1", _image);
              Utils.SetProperty("#latestMediaHandler.mvcentral.selected.showfanart1", "true");
              Utils.SetProperty("#latestMediaHandler.mvcentral.selected.showfanart2", "false");
              Thread.Sleep(1000);
              Utils.SetProperty("#latestMediaHandler.mvcentral.selected.fanart2", string.Empty);
              showFanart = 2;
            }
            else
            {
              Utils.SetProperty("#latestMediaHandler.mvcentral.selected.fanart2", _image);
              Utils.SetProperty("#latestMediaHandler.mvcentral.selected.showfanart2", "true");
              Utils.SetProperty("#latestMediaHandler.mvcentral.selected.showfanart1", "false");
              Thread.Sleep(1000);
              Utils.SetProperty("#latestMediaHandler.mvcentral.selected.fanart1", string.Empty);
              showFanart = 1;
            }
            // Utils.UnLoadImage(_image, ref images);
            CurrentFacade.SelectedImage = _id;
          }
        }
        else
        {
          Utils.SetProperty("#latestMediaHandler.mvcentral.selected.fanart1", " ");
          Utils.SetProperty("#latestMediaHandler.mvcentral.selected.fanart2", " ");
          Utils.SetProperty("#latestMediaHandler.mvcentral.selected.showfanart1", "false");
          Utils.SetProperty("#latestMediaHandler.mvcentral.selected.showfanart2", "false");
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

    internal bool PlayMusicAlbum(GUIWindow fWindow)
    {
      try
      {
        int FocusControlID = fWindow.GetFocusControlId();
        if (ControlIDPlays.Contains(FocusControlID))
        {
          PlayMusicAlbum(ControlIDPlays.IndexOf(FocusControlID)+1);
          return true;
        }
        //
        // CurrentFacade.Facade = Utils.GetLatestsFacade(CurrentFacade.ControlID);
        if (CurrentFacade.Facade != null && CurrentFacade.Facade.Focus && CurrentFacade.Facade.SelectedListItem != null)
        {
          PlayMusicAlbum(CurrentFacade.Facade.SelectedListItem.ItemId);
          return true;
        }
      }
      catch (Exception ex)
      {
        logger.Error("Unable to play album! " + ex.ToString());
        return true;
      }
      return false;
    }

    internal void PlayMusicAlbum(int index)
    {
      string _songFolder = latestMusicAlbumsVideos[index].ToString();
      if (!string.IsNullOrEmpty(_songFolder))
        _songFolder = Path.GetDirectoryName(_songFolder);
      if (Directory.Exists(_songFolder))
      {
        LoadSongsFromFolder(latestMusicAlbumsVideos[index].ToString(), _songFolder, false);
        StartPlayback(index);
      }
    }

    private void StartPlayback(int index)
    {
      // if we got a playlist start playing it
      if (playlistPlayer.GetPlaylist(MediaPortal.Playlists.PlayListType.PLAYLIST_MUSIC_VIDEO).Count > 0)
      {
        playlistPlayer.CurrentPlaylistType = MediaPortal.Playlists.PlayListType.PLAYLIST_MUSIC_VIDEO;
        playlistPlayer.Reset();
        playlistPlayer.Play(0);
      }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal void SetupReceivers()
    {
      if (!Utils.LatestMvCentral)
      {
        return;
      }

      try
      {
        GUIWindowManager.Receivers += new SendMessageHandler(OnMessage);

        // mvCentralCore.DatabaseManager.ObjectInserted += new RealCornerstone.Cornerstone.Database.DatabaseManager.ObjectAffectedDelegate(MvCentralOnObjectInserted);
        // mvCentralCore.DatabaseManager.ObjectDeleted += new RealCornerstone.Cornerstone.Database.DatabaseManager.ObjectAffectedDelegate(MvCentralOnObjectDeleted);
      }
      catch (Exception ex)
      {
        logger.Error("SetupMvCentralsLatest: " + ex.ToString());
      }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal void DisposeReceivers()
    {
      if (!Utils.LatestMvCentral)
      {
        return;
      }

      try
      {
        GUIWindowManager.Receivers -= new SendMessageHandler(OnMessage);

        // mvCentralCore.DatabaseManager.ObjectInserted -= new RealCornerstone.Cornerstone.Database.DatabaseManager.ObjectAffectedDelegate(MvCentralOnObjectInserted);
        // mvCentralCore.DatabaseManager.ObjectDeleted -= new RealCornerstone.Cornerstone.Database.DatabaseManager.ObjectAffectedDelegate(MvCentralOnObjectDeleted);
      }
      catch (Exception ex)
      {
        logger.Error("DisposeMvCentralsLatest: " + ex.ToString());
      }
    }
/*
    private void MvCentralOnObjectInserted(RealCornerstone.Cornerstone.Database.Tables.DatabaseTable obj)
    {
      try
      {
        if (obj.GetType() == typeof (DBMovieInfo) || obj.GetType() == typeof (DBWatchedHistory))
        {
          GetLatestMediaInfoThread();
        }
      }
      catch (Exception ex)
      {
        logger.Error("MvCentralOnObjectInserted: " + ex.ToString());
      }
    }

    private void MvCentralOnObjectDeleted(RealCornerstone.Cornerstone.Database.Tables.DatabaseTable obj)
    {
      try
      {
        if (obj.GetType() == typeof (DBMovieInfo) || obj.GetType() == typeof (DBWatchedHistory))
        {
          GetLatestMediaInfoThread();
        }
      }
      catch (Exception ex)
      {
        logger.Error("MvCentralOnObjectDeleted: " + ex.ToString());
      }
    }
*/
    private void OnMessage(GUIMessage message)
    {
      if (Utils.LatestMvCentral)
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

    internal void UpdateImageTimer(GUIWindow fWindow, Object stateInfo, ElapsedEventArgs e)
    {
      if (Utils.LatestMvCentral)
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
              Utils.SetProperty("#latestMediaHandler.mvcentral.selected.fanart1", " ");
              Utils.SetProperty("#latestMediaHandler.mvcentral.selected.fanart2", " ");
              Utils.UnLoadImages(ref images);
              ShowFanart = 1;
              CurrentFacade.SelectedImage = -1;
              NeedCleanup = false;
              NeedCleanupCount = 0;
            }
            else if (NeedCleanup && NeedCleanupCount == 0)
            {
              Utils.SetProperty("#latestMediaHandler.mvcentral.selected.showfanart1", "false");
              Utils.SetProperty("#latestMediaHandler.mvcentral.selected.showfanart2", "false");
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
          logger.Error("UpdateImageTimer (mvcentral): " + ex.ToString());
        }
      }
    }

    
    private bool IsMusicFile(string fileName)
    {
      string supportedExtensions = MediaPortal.Util.Utils.VideoExtensionsDefault;
      if (supportedExtensions.IndexOf(Path.GetExtension(fileName).ToLower()) > -1)
      {
        return true;
      }
      return false;
    }

    private void GetFiles(string folder, ref List<string> foundFiles, bool recursive)
    {
      try
      {
        string[] files = Directory.GetFiles(folder);
        foundFiles.AddRange(files);

        if (recursive)
        {
          string[] subFolders = Directory.GetDirectories(folder);
          for (int i = 0; i < subFolders.Length; ++i)
          {
            GetFiles(subFolders[i], ref foundFiles, recursive);
          }
        }
      }
      catch (Exception ex)
      {
        logger.Error("GetFiles: " + ex.ToString());
      }
    }

    private void LoadSongsFromFolder(string startfile, string folder, bool includeSubFolders)
    {
      using (Settings xmlreader = new MPSettings())
      {
        _stripArtistPrefixes = xmlreader.GetValueAsBool("musicfiles", "stripartistprefixes", false);
      }

      // clear current playlist
      playlistPlayer = MediaPortal.Playlists.PlayListPlayer.SingletonPlayer;
      //playlistPlayer.GetPlaylist(MediaPortal.Playlists.PlayListType.PLAYLIST_VIDEO_TEMP).Clear();
      playlistPlayer.GetPlaylist(MediaPortal.Playlists.PlayListType.PLAYLIST_MUSIC_VIDEO).Clear();
      int numSongs = 0;
      try
      {
        List<string> files = new List<string>();
        files.Add(startfile);
        GetFiles(folder, ref files, includeSubFolders);
        List<string> unique = files.Distinct().ToList();
        foreach (string file in unique)
        {
          if (IsMusicFile(file))
          {
            MediaPortal.Playlists.PlayListItem item = new MediaPortal.Playlists.PlayListItem();

            item.FileName = file;
            item.Type = MediaPortal.Playlists.PlayListItem.PlayListItemType.Video;
            /*
            MusicTag tag = TagReader.ReadTag(file);
            if (tag != null)
            {
              tag.Artist = MediaPortal.Util.Utils.FormatMultiItemMusicStringTrim(tag.Artist, _stripArtistPrefixes);
              tag.AlbumArtist = MediaPortal.Util.Utils.FormatMultiItemMusicStringTrim(tag.AlbumArtist, _stripArtistPrefixes);
              tag.Genre = MediaPortal.Util.Utils.FormatMultiItemMusicStringTrim(tag.Genre, false);
              tag.Composer = MediaPortal.Util.Utils.FormatMultiItemMusicStringTrim(tag.Composer, _stripArtistPrefixes);
              item.Description = tag.Title;
              item.MusicTag = tag;
              item.Duration = tag.Duration;
            }
            */
            playlistPlayer.GetPlaylist(MediaPortal.Playlists.PlayListType.PLAYLIST_MUSIC_VIDEO).Add(item);
            numSongs++;
          }
        }
      }
      catch //(Exception ex)
      {
        logger.Error("Error retrieving songs from folder.");
      }
    }
  }
}
