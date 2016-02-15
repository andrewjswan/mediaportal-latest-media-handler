//***********************************************************************
// Assembly         : LatestMediaHandler
// Author           : cul8er
// Created          : 05-09-2010
//
// Last Modified By : ajs
// Last Modified On : 30-09-2015
// Description      : 
//
// Copyright        : Open Source software licensed under the GNU/GPL agreement. Thanks to MediaPortal that created many of the functions used here.
//***********************************************************************
extern alias RealCornerstone;
extern alias RealNLog;

using MediaPortal.Dialogs;
using MediaPortal.GUI.Library;
using MediaPortal.Profile;
using MediaPortal.TagReader;

using mvCentral;
using mvCentral.Database;

using RealNLog.NLog;

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Timers;

namespace LatestMediaHandler
{
  internal class LatestMvCentralHandler
  {
    #region declarations    

    private static Logger logger = LogManager.GetCurrentClassLogger();
    private readonly object lockObject = new object();
    private bool isInitialized /* = false*/;

    private LatestsCollection latestMusicAlbums = null;
    internal Hashtable latestMusicAlbumsVideos;
    public Hashtable artistsWithImageMissing;

    private MediaPortal.Playlists.PlayListPlayer playlistPlayer;
    private ArrayList m_Shares = new ArrayList();
    private bool _stripArtistPrefixes;

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

    public const int ControlID = 919299280;
    public const int Play1ControlID = 91929928;
    public const int Play2ControlID = 91929929;
    public const int Play3ControlID = 91929930;
    public const int Play4ControlID = 91919907;

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

    internal LatestMvCentralHandler()
    {
      artistsWithImageMissing = new Hashtable();

      ControlIDFacades = new List<int>();
      ControlIDPlays = new List<int>();
      //
      ControlIDFacades.Add(ControlID);
      ControlIDPlays.Add(Play1ControlID);
      ControlIDPlays.Add(Play2ControlID);
      ControlIDPlays.Add(Play3ControlID);
      ControlIDPlays.Add(Play4ControlID);
    }

    internal bool IsInitialized
    {
      get { return isInitialized; }
      set { isInitialized = value; }
    }

    internal void GetLatestMediaInfoThread()
    {
      // logger.Debug("*** mvCentral: GetLatestMediaInfoThread...") ;
      // mvCentral
      if (LatestMediaHandlerSetup.LatestMvCentral.Equals("True", StringComparison.CurrentCulture))
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

    internal void EmptyLatestMediaPropsMvCentral()
    {
      Utils.SetProperty("#latestMediaHandler.mvcentral.label", Translation.LabelLatestAdded);
      Utils.SetProperty("#latestMediaHandler.mvcentral.latest.enabled", "false");
      Utils.SetProperty("#latestMediaHandler.mvcentral.hasnew", "false");
      for (int z = 1; z < 4; z++)
      {
        Utils.SetProperty("#latestMediaHandler.mvcentral.latest" + z + ".thumb", string.Empty);
        Utils.SetProperty("#latestMediaHandler.mvcentral.latest" + z + ".artist", string.Empty);
        Utils.SetProperty("#latestMediaHandler.mvcentral.latest" + z + ".artistbio", string.Empty);
        Utils.SetProperty("#latestMediaHandler.mvcentral.latest" + z + ".artistbiooutline", string.Empty);
        Utils.SetProperty("#latestMediaHandler.mvcentral.latest" + z + ".album", string.Empty);
        Utils.SetProperty("#latestMediaHandler.mvcentral.latest" + z + ".track", string.Empty);
        Utils.SetProperty("#latestMediaHandler.mvcentral.latest" + z + ".dateAdded", string.Empty);
        Utils.SetProperty("#latestMediaHandler.mvcentral.latest" + z + ".fanart", string.Empty);
        Utils.SetProperty("#latestMediaHandler.mvcentral.latest" + z + ".genre", string.Empty);
        Utils.SetProperty("#latestMediaHandler.mvcentral.latest" + z + ".new", "false");
      }
    }

    internal void GetLatestMediaInfo(bool _onStartUp)
    {
      // logger.Debug("*** mvCentral: GetLatestMediaInfo...") ;
      int sync = Interlocked.CompareExchange(ref Utils.SyncPointMvCMusicUpdate, 1, 0);
      if (sync != 0)
        return;

      // logger.Debug("*** mvCentral: GetLatestMediaInfo...") ;
      artistsWithImageMissing = new Hashtable();
      if (!LatestMediaHandlerSetup.LatestMvCentral.Equals("True", StringComparison.CurrentCulture))
      {
        EmptyLatestMediaPropsMvCentral();
        return;    
      }

      // logger.Debug("*** mvCentral: GetLatestMediaInfo...") ;
      //MvCentral
      try
      {
        LatestsCollection hTable = GetLatestMusic(_onStartUp);
        EmptyLatestMediaPropsMvCentral() ;
        if (hTable != null)
        {
          int z = 1;
          for (int i = 0; i < hTable.Count && i < Utils.LatestsMaxNum; i++)
          {
            logger.Info("Updating Latest Media Info: MvCental: Video " + z + ": " + hTable[i].Artist + " - " + hTable[i].Album);

            string artistbio = (string.IsNullOrEmpty(hTable[i].Summary) ? Translation.NoDescription : hTable[i].Summary);
            string artistbiooutline = Utils.GetSentences(artistbio, Utils.latestPlotOutlineSentencesNum);
            Utils.SetProperty("#latestMediaHandler.mvcentral.latest" + z + ".thumb", hTable[i].Thumb);
            Utils.SetProperty("#latestMediaHandler.mvcentral.latest" + z + ".artist", hTable[i].Artist);
            Utils.SetProperty("#latestMediaHandler.mvcentral.latest" + z + ".artistbio", artistbio);
            Utils.SetProperty("#latestMediaHandler.mvcentral.latest" + z + ".artistbiooutline", artistbiooutline);
            Utils.SetProperty("#latestMediaHandler.mvcentral.latest" + z + ".album", hTable[i].Album);
            Utils.SetProperty("#latestMediaHandler.mvcentral.latest" + z + ".track", hTable[i].Title);
            Utils.SetProperty("#latestMediaHandler.mvcentral.latest" + z + ".dateAdded", hTable[i].DateAdded);
            Utils.SetProperty("#latestMediaHandler.mvcentral.latest" + z + ".fanart", hTable[i].Fanart);
            Utils.SetProperty("#latestMediaHandler.mvcentral.latest" + z + ".genre", hTable[i].Genre);
            Utils.SetProperty("#latestMediaHandler.mvcentral.latest" + z + ".new", hTable[i].New);
            z++;
          }
          // hTable.Clear();
          Utils.SetProperty("#latestMediaHandler.mvcentral.hasnew", Utils.HasNewMvCentral ? "true" : "false");
          logger.Debug("Updating Latest Media Info: MvCentral: Has new: " + (Utils.HasNewMvCentral ? "true" : "false"));
        }
        // hTable = null;
      }
      catch (FileNotFoundException)
      {
        //do nothing    
      }
      catch (MissingMethodException)
      {
        // logger.Debug("*** mvCentral: GetLatestMediaInfo... MissingMethodException") ;
        //do nothing    
      }
      catch (Exception ex)
      {
        logger.Error("GetLatestMediaInfo (MvCentral): " + ex.ToString());
      }

      if ((latestMusicAlbums != null) && (latestMusicAlbums.Count > 0))
      {
        // if (System.Windows.Forms.Form.ActiveForm.InvokeRequired)
        // {
        //   System.Windows.Forms.Form.ActiveForm.Invoke(InitFacade);
        // }
        // else
        // {
          InitFacade();
        // }
        Utils.SetProperty("#latestMediaHandler.mvcentral.latest.enabled", "true");
      }
      else
        EmptyLatestMediaPropsMvCentral();
      Utils.SyncPointMvCMusicUpdate = 0;
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

        //Show Dialog
        dlg.DoModal(GUIWindowManager.ActiveWindow);

        if (dlg.SelectedLabel < 0)
        {
          return;
        }

        switch (dlg.SelectedId)
        {
          case 1:
          {
            PlayMusicAlbum(GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow));
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
        Utils.HasNewMvCentral = false;

        List<DBTrackInfo> allTracks = DBTrackInfo.GetAll();
        // logger.Debug("*** mvCentral: GetLatestMusic: "+allTracks.Count) ;
        if (allTracks.Count > 0)
        {
          allTracks.Sort(delegate(DBTrackInfo p1, DBTrackInfo p2) { return p2.DateAdded.CompareTo(p1.DateAdded); });
          foreach (DBTrackInfo allTrack in allTracks)
          {
            string dateAdded = string.Empty;
            string sArtist = allTrack.ArtistInfo[0].Artist;
            // string sArtistBio = mvCentralUtils.bioNoiseFilter(allTrack.ArtistInfo[0].bioContent);
            string sArtistBio = allTrack.ArtistInfo[0].bioSummary;
            // logger.Debug("*** "+allTrack.ArtistInfo[0].bioContent);
            // logger.Debug("*** "+allTrack.ArtistInfo[0].bioSummary);
            bool isnew = false;
            try
            {
              dateAdded = String.Format("{0:" + LatestMediaHandlerSetup.DateFormat + "}", allTrack.DateAdded);
              isnew = (allTrack.DateAdded > Utils.NewDateTime);
              if (isnew)
                Utils.HasNewMvCentral = true;
            }
            catch 
            {   }

            string thumb = allTrack.ArtistInfo[0].ArtThumbFullPath;
            if (string.IsNullOrEmpty(thumb))
              thumb = "defaultAudioBig.png";

            string sFilename1 = "";
            string sFilename2 = "";
            try
            {
              Hashtable ht2 = (Utils.FanartHandler ? UtilsFanartHandler.GetMusicFanartForLatest(sArtist) : null);
              if (ht2 == null || ht2.Count < 1 && !_onStartUp)
              {
                if (Utils.FanartHandler)
                {
                  UtilsFanartHandler.ScrapeFanartAndThumb(sArtist, "");
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
            catch { }

            string tmpArtist = string.Empty;
            string tmpAlbum = string.Empty;
            String tmpGenre = string.Empty;
            if (allTrack.AlbumInfo != null && allTrack.AlbumInfo.Count > 0)
              tmpAlbum = allTrack.AlbumInfo[0].Album;
            if (allTrack.ArtistInfo != null && allTrack.ArtistInfo.Count > 0)
            {
              tmpArtist = allTrack.ArtistInfo[0].Artist;
              tmpGenre = allTrack.ArtistInfo[0].Genre;
            }

            latestMusicAlbums.Add(new LatestMediaHandler.Latest(dateAdded, thumb, sFilename1, allTrack.Track,
                                                                allTrack.LocalMedia[0].File.FullName,
                                                                tmpArtist, tmpAlbum,
                                                                tmpGenre,
                                                                allTrack.Rating.ToString(),
                                                                allTrack.Rating.ToString(), 
                                                                null, null, null, null, null, null, null, null,
                                                                sArtistBio, 
                                                                null,
                                                                isnew)); 

            latestMusicAlbumsVideos.Add(i0, allTrack.LocalMedia[0].File.FullName);
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
        item.IconImage = latests.Thumb;
        item.IconImageBig = latests.Thumb;
        item.ThumbnailImage = latests.Thumb;
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
        LatestsToFilmStrip(latestMusicAlbums);

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
          string artistbio = (string.IsNullOrEmpty(latestMusicAlbums[i].Summary) ? Translation.NoDescription : latestMusicAlbums[i].Summary);
          string artistbiooutline = Utils.GetSentences(artistbio, Utils.latestPlotOutlineSentencesNum);
          Utils.SetProperty("#latestMediaHandler.mvcentral.selected.thumb", latestMusicAlbums[i].Thumb);
          Utils.SetProperty("#latestMediaHandler.mvcentral.selected.artist", latestMusicAlbums[i].Artist);
          Utils.SetProperty("#latestMediaHandler.mvcentral.selected.album", latestMusicAlbums[i].Album);
          Utils.SetProperty("#latestMediaHandler.mvcentral.selected.track", latestMusicAlbums[i].Title);
          Utils.SetProperty("#latestMediaHandler.mvcentral.selected.dateAdded", latestMusicAlbums[i].DateAdded);
          Utils.SetProperty("#latestMediaHandler.mvcentral.selected.genre", latestMusicAlbums[i].Genre);
          Utils.SetProperty("#latestMediaHandler.mvcentral.selected.artistbio", artistbio);
          Utils.SetProperty("#latestMediaHandler.mvcentral.selected.artistbiooutline", artistbiooutline);
          Utils.SetProperty("#latestMediaHandler.mvcentral.selected.new", latestMusicAlbums[i].New);
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
              Utils.SetProperty("#latestMediaHandler.mvcentral.selected.fanart1", _image);
              Utils.SetProperty("#latestMediaHandler.mvcentral.selected.showfanart1", "true");
              Utils.SetProperty("#latestMediaHandler.mvcentral.selected.showfanart2", "false");
              Thread.Sleep(1000);
              Utils.SetProperty("#latestMediaHandler.mvcentral.selected.fanart2", "");
              showFanart = 2;
            }
            else
            {
              Utils.SetProperty("#latestMediaHandler.mvcentral.selected.fanart2", _image);
              Utils.SetProperty("#latestMediaHandler.mvcentral.selected.showfanart2", "true");
              Utils.SetProperty("#latestMediaHandler.mvcentral.selected.showfanart1", "false");
              Thread.Sleep(1000);
              Utils.SetProperty("#latestMediaHandler.mvcentral.selected.fanart1", "");
              showFanart = 1;
            }
            Utils.UnLoadImage(_image, ref images);
            selectedFacadeItem2 = _id;
          }
        }
        else
        {
          Utils.SetProperty("#latestMediaHandler.mvcentral.selected.fanart1", " ");
          Utils.SetProperty("#latestMediaHandler.mvcentral.selected.fanart2", " ");
          Utils.SetProperty("#latestMediaHandler.mvcentral.selected.showfanart1", "false");
          Utils.SetProperty("#latestMediaHandler.mvcentral.selected.showfanart2", "false");
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

    internal bool PlayMusicAlbum(GUIWindow fWindow)
    {
      try
      {
        /*
        if (fWindow.GetFocusControlId() == Play1ControlID)
        {
          PlayMusicAlbum(1);
          return true;
        }
        else if (fWindow.GetFocusControlId() == Play2ControlID)
        {
          PlayMusicAlbum(2);
          return true;
        }
        else if (fWindow.GetFocusControlId() == Play3ControlID)
        {
          PlayMusicAlbum(3);
          return true;
        }
        */
        int FocusControlID = fWindow.GetFocusControlId();
        if (ControlIDPlays.Contains(FocusControlID))
        {
          PlayMusicAlbum(ControlIDPlays.IndexOf(FocusControlID)+1);
          return true;
        }
        //
        facade = Utils.GetLatestsFacade(ControlID);
        if (facade != null && facade.Focus && facade.SelectedListItem != null)
        {
          PlayMusicAlbum(facade.SelectedListItem.ItemId);
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

    internal void SetupMvCentralsLatest()
    {
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

    internal void DisposeMvCentralsLatest()
    {
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
      Utils.ThreadToSleep();
      if (LatestMediaHandlerSetup.LatestMvCentral.Equals("True", StringComparison.CurrentCulture))
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
                GetLatestMediaInfoThread();
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

    internal void UpdateImageTimer(GUIWindow fWindow, Object stateInfo, ElapsedEventArgs e)
    {
      if (LatestMediaHandlerSetup.LatestMvCentral.Equals("True", StringComparison.CurrentCulture))
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
              Utils.SetProperty("#latestMediaHandler.mvcentral.selected.fanart1", " ");
              Utils.SetProperty("#latestMediaHandler.mvcentral.selected.fanart2", " ");
              Utils.UnLoadImage(ref images);
              ShowFanart = 1;
              SelectedFacadeItem2 = -1;
              SelectedFacadeItem2 = -1;
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
