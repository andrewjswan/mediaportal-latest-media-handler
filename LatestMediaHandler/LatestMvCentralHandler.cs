//***********************************************************************
// Assembly         : LatestMediaHandler
// Author           : cul8er
// Created          : 05-09-2010
//
// Last Modified By : cul8er
// Last Modified On : 10-05-2010
// Description      : 
//
// Copyright        : Open Source software licensed under the GNU/GPL agreement. Thanks to MediaPortal that created many of the functions used here.
//***********************************************************************
using System.Collections;
using System.IO;
using System.Threading;
using MediaPortal.Dialogs;
using MediaPortal.GUI.Library;
using MediaPortal.TagReader;
using mvCentral.Database;

namespace LatestMediaHandler
{
  extern alias RealNLog;
  using System;
  using System.Collections.Generic;
  using RealNLog.NLog;

  internal class LatestMvCentralHandler
  {
    #region declarations    

    private static Logger logger = LogManager.GetCurrentClassLogger();
    private readonly object lockObject = new object();
    private bool isInitialized /* = false*/;
    internal Hashtable latestMusicAlbums;
    private MediaPortal.Playlists.PlayListPlayer playlistPlayer;
    private ArrayList m_Shares = new ArrayList();
    private bool _stripArtistPrefixes;
    public Hashtable artistsWithImageMissing;
    private LatestMediaHandler.LatestsCollection result = null;
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
    }

    internal bool IsInitialized
    {
      get { return isInitialized; }
      set { isInitialized = value; }
    }

    internal void GetLatestMediaInfo(bool _onStartUp)
    {
      try
      {
        int sync = Interlocked.CompareExchange(ref LatestMediaHandlerSetup.SyncPointMusicUpdate, 1, 0);
        if (sync == 0)
        {
          int z = 1;
          artistsWithImageMissing = new Hashtable();
          string windowId = GUIWindowManager.ActiveWindow.ToString();
          if (LatestMediaHandlerSetup.LatestMvCentral.Equals("True", StringComparison.CurrentCulture))
          {
            //Music
            try
            {
              GetLatestMusic(_onStartUp, LatestMediaHandlerSetup.LatestMusicType);
              for (int i = 0; i < 3; i++)
              {
                LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.mvcentral.latest" + z + ".thumb", string.Empty);
                LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.mvcentral.latest" + z + ".artist", string.Empty);
                LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.mvcentral.latest" + z + ".album", string.Empty);
                LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.mvcentral.latest" + z + ".track", string.Empty);
                LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.mvcentral.latest" + z + ".dateAdded",
                  string.Empty);
                LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.mvcentral.latest" + z + ".fanart", string.Empty);
                LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.mvcentral.latest" + z + ".genre", string.Empty);
                z++;
              }
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.mvcentral.latest.enabled", "false");
              if (result != null)
              {
                z = 1;
                for (int i = 0; i < result.Count && i < 3; i++)
                {
                  logger.Info("Updating Latest Media Info: Latest MvCental " + z + ": " + result[i].Artist + " - " +
                              result[i].Album);
                  LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.mvcentral.latest" + z + ".thumb",
                    result[i].Thumb);
                  LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.mvcentral.latest" + z + ".artist",
                    result[i].Artist);
                  LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.mvcentral.latest" + z + ".album",
                    result[i].Album);
                  LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.mvcentral.latest" + z + ".track",
                    result[i].Title);
                  LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.mvcentral.latest" + z + ".dateAdded",
                    result[i].DateAdded);
                  LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.mvcentral.latest" + z + ".fanart",
                    result[i].Fanart);
                  LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.mvcentral.latest" + z + ".genre",
                    result[i].Genre);
                  z++;
                }
                //hTable.Clear();
                LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.mvcentral.latest.enabled", "true");
              }
              //hTable = null;
              z = 1;

            }
            catch (Exception ex)
            {
              logger.Error("GetLatestMediaInfo (MvCentral): " + ex.ToString());
            }
          }
          else
          {
            LatestMediaHandlerSetup.EmptyLatestMediaPropsMvCentral();
          }
          LatestMediaHandlerSetup.SyncPointMusicUpdate = 0;
        }
      }
      catch (Exception ex)
      {
        logger.Error("GetLatestMediaInfo (MvCentral): " + ex.ToString());
        LatestMediaHandlerSetup.SyncPointMusicUpdate = 0;
      }
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
            GUIWindow gw = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow);
            GUIControl gc = gw.GetControl(919299280);
            facade = gc as GUIFacadeControl;
            if (facade != null)
            {
              PlayMusicAlbum(facade.SelectedListItem.ItemId);
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
    /// Returns last music track info added to MP music database.
    /// </summary>
    /// <returns>Hashtable containg artist names</returns>
    internal LatestMediaHandler.LatestsCollection GetLatestMusic(bool _onStartUp, string type)
    {
      LatestsCollection resultTmp = new LatestsCollection();

      int x = 0;
      try
      {
        GUIWindow gw = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow);
        GUIControl gc = gw.GetControl(919299280);
        facade = gc as GUIFacadeControl;
        if (facade != null)
        {
          facade.Clear();
        }
        if (al != null)
        {
          al.Clear();
        }

        latestMusicAlbums = new Hashtable();
        int i0 = 1;
        List<DBTrackInfo> allTracks = DBTrackInfo.GetAll();
        if (allTracks.Count > 0)
        {
          allTracks.Sort(delegate(DBTrackInfo p1, DBTrackInfo p2) { return p2.DateAdded.CompareTo(p1.DateAdded); });
          foreach (DBTrackInfo allTrack in allTracks)
          {
            string dateAdded = string.Empty;
            string sArtist = allTrack.ArtistInfo[0].Artist;
            try
            {
              dateAdded = String.Format("{0:" + LatestMediaHandlerSetup.DateFormat + "}",
                allTrack.DateAdded);
            }
            catch
            {
            }

            String sPath = allTrack.LocalMedia[0].File.FullName;

            if (sPath != null && sPath.Length > 0) // && sPath.IndexOf("\\") > 0)
            {
              sPath = Path.GetDirectoryName(sPath);
            }

            string thumb = allTrack.ArtistInfo[0].ArtThumbFullPath;

            if (thumb == null || thumb.Length < 1)
            {
              thumb = "defaultAudioBig.png";
            }

            string sFilename1 = "";
            string sFilename2 = "";
            try
            {
              Hashtable ht2 = UtilsFanartHandler.GetMusicFanartForLatest(sArtist);
              if (ht2 == null || ht2.Count < 1 && !_onStartUp)
              {
                UtilsFanartHandler.ScrapeFanartAndThumb(sArtist, "");
                ht2 = UtilsFanartHandler.GetMusicFanartForLatest(sArtist);
              }

              if (ht2 == null || ht2.Count < 1)
              {
                if (!artistsWithImageMissing.Contains(UtilsFanartHandler.GetFHArtistName(sArtist)))
                {
                  artistsWithImageMissing.Add(UtilsFanartHandler.GetFHArtistName(sArtist),
                    UtilsFanartHandler.GetFHArtistName(sArtist));
                }
              }
              IDictionaryEnumerator _enumerator = ht2.GetEnumerator();

              int i = 0;
              while (_enumerator.MoveNext())
              {
                if (i == 0)
                {
                  sFilename1 = _enumerator.Value.ToString();
                }
                if (i == 1)
                {
                  sFilename2 = _enumerator.Value.ToString();
                }
                i++;
              }
            }
            catch
            {
            }

            string tmpArtist = string.Empty;
            string tmpAlbum = string.Empty;
            String tmpGenre = string.Empty;
            if (allTrack.AlbumInfo != null && allTrack.AlbumInfo.Count > 0)
            {
              tmpAlbum = allTrack.AlbumInfo[0].Album;
            }
            if (allTrack.ArtistInfo != null && allTrack.ArtistInfo.Count > 0)
            {
              tmpArtist = allTrack.ArtistInfo[0].Artist;
              tmpGenre = allTrack.ArtistInfo[0].Genre.Replace("|", ",");
            }

            resultTmp.Add(new LatestMediaHandler.Latest(dateAdded, thumb, sFilename1, allTrack.Track,
              allTrack.LocalMedia[0].File.FullName,
              tmpArtist, tmpAlbum,
              tmpGenre,
              allTrack.Rating.ToString(),
              allTrack.Rating.ToString(), null, null, null, null,
              null, null, null, null, null, null));
            if (result == null || result.Count == 0)
            {
              result = resultTmp;
            }
            latestMusicAlbums.Add(i0, sPath);
            AddToFilmstrip(resultTmp[x], i0);

            x++;
            i0++;

            if (x == 10)
            {
              break;
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

      }
      catch (Exception ex)
      {
        logger.Error("GetLatestMusic: " + ex.ToString());
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
        item.ItemId = x;
        Utils.LoadImage(latests.Thumb, ref imagesThumbs);
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
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.mvcentral.selected.thumb",
            result[(item.ItemId - 1)].Thumb);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.mvcentral.selected.artist",
            result[(item.ItemId - 1)].Artist);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.mvcentral.selected.album",
            result[(item.ItemId - 1)].Album);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.mvcentral.selected.track",
            result[(item.ItemId - 1)].Title);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.mvcentral.selected.dateAdded",
            result[(item.ItemId - 1)].DateAdded);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.mvcentral.selected.genre",
            result[(item.ItemId - 1)].Genre);
          selectedFacadeItem1 = item.ItemId;

          GUIWindow gw = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow);
          GUIControl gc = gw.GetControl(919299280);
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
        GUIControl gc = gw.GetControl(919299280);
        facade = gc as GUIFacadeControl;
        if (facade != null && gw.GetFocusControlId() == 919299280 && facade.SelectedListItem != null)
        {
          int _id = facade.SelectedListItem.ItemId;
          String _image = facade.SelectedListItem.DVDLabel;
          if (selectedFacadeItem2 != _id)
          {
            Utils.LoadImage(_image, ref images);
            if (showFanart == 1)
            {
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.mvcentral.selected.fanart1", _image);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.mvcentral.selected.showfanart1", "true");
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.mvcentral.selected.showfanart2", "");
              Thread.Sleep(1000);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.mvcentral.selected.fanart2", "");
              showFanart = 2;
            }
            else
            {
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.mvcentral.selected.fanart2", _image);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.mvcentral.selected.showfanart2", "true");
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.mvcentral.selected.showfanart1", "");
              Thread.Sleep(1000);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.mvcentral.selected.fanart1", "");
              showFanart = 1;
            }
            Utils.UnLoadImage(_image, ref images);
            selectedFacadeItem2 = _id;
          }
        }
        else
        {
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.mvcentral.selected.fanart1", " ");
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.mvcentral.selected.fanart2", " ");
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.mvcentral.selected.showfanart1", "true");
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.mvcentral.selected.showfanart2", "");
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

    internal void PlayMusicAlbum(int index)
    {
      string _songFolder = latestMusicAlbums[index].ToString();
      if (Directory.Exists(_songFolder))
      {
        LoadSongsFromFolder(_songFolder, false);
        StartPlayback(index);
      }

    }

    private void StartPlayback(int index)
    {
      // if we got a playlist start playing it
      if (playlistPlayer.GetPlaylist(MediaPortal.Playlists.PlayListType.PLAYLIST_MUSIC_TEMP).Count > 0)
      {
        playlistPlayer.CurrentPlaylistType = MediaPortal.Playlists.PlayListType.PLAYLIST_MUSIC_TEMP;
        playlistPlayer.Reset();
        playlistPlayer.Play((index - 1));
      }
    }

    private bool IsMusicFile(string fileName)
    {
      string supportedExtensions = MediaPortal.Util.Utils.AudioExtensionsDefault;
        // ".mp3,.wma,.ogg,.flac,.wav,.cda,.m4a,.m4p,.mp4,.wv,.ape,.mpc,.aif,.aiff";
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

    private void LoadSongsFromFolder(string folder, bool includeSubFolders)
    {
      // clear current playlist
      playlistPlayer = MediaPortal.Playlists.PlayListPlayer.SingletonPlayer;
      //playlistPlayer.GetPlaylist(MediaPortal.Playlists.PlayListType.PLAYLIST_MUSIC).Clear();
      playlistPlayer.GetPlaylist(MediaPortal.Playlists.PlayListType.PLAYLIST_MUSIC_TEMP).Clear();
      int numSongs = 0;
      try
      {
        List<string> files = new List<string>();
        GetFiles(folder, ref files, includeSubFolders);
        foreach (string file in files)
        {
          if (IsMusicFile(file))
          {
            MediaPortal.Playlists.PlayListItem item = new MediaPortal.Playlists.PlayListItem();
            item.FileName = file;
            item.Type = MediaPortal.Playlists.PlayListItem.PlayListItemType.Audio;
            MusicTag tag = TagReader.ReadTag(file);
            if (tag != null)
            {
              tag.Artist = MediaPortal.Util.Utils.FormatMultiItemMusicStringTrim(tag.Artist, _stripArtistPrefixes);
              tag.AlbumArtist = MediaPortal.Util.Utils.FormatMultiItemMusicStringTrim(tag.AlbumArtist,
                _stripArtistPrefixes);
              tag.Genre = MediaPortal.Util.Utils.FormatMultiItemMusicStringTrim(tag.Genre, false);
              tag.Composer = MediaPortal.Util.Utils.FormatMultiItemMusicStringTrim(tag.Composer, _stripArtistPrefixes);
              item.Description = tag.Title;
              item.MusicTag = tag;
              item.Duration = tag.Duration;
            }
            playlistPlayer.GetPlaylist(MediaPortal.Playlists.PlayListType.PLAYLIST_MUSIC_TEMP).Add(item);
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
