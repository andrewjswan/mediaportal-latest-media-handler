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

namespace LatestMediaHandler
{
  extern alias RealNLog;
  using MediaPortal.GUI.Library;
  using MediaPortal.Music.Database;
  using MediaPortal.TagReader;
  using MediaPortal.Util;
  using RealNLog.NLog;
  using System;
  using System.Collections;
  using System.Collections.Generic;
  using System.IO;
  using System.Linq;
  using System.Threading;
  using MediaPortal.Profile;
  using MediaPortal.Services;
  using MediaPortal.Threading;
  using MediaPortal.Dialogs;


  /// <summary>
  /// Class handling all database access.
  /// </summary>
  internal class LatestMusicHandler
  {
    #region declarations

    private static Logger logger = LogManager.GetCurrentClassLogger();
    private readonly object lockObject = new object();
    private MusicDatabase m_db = null;
    private bool isInitialized /* = false*/;
    internal Hashtable latestMusicAlbums;
    private MediaPortal.Playlists.PlayListPlayer playlistPlayer;
    private ArrayList m_Shares = new ArrayList();
    private bool _run;
    //private bool _reorgRunning;
    //private MusicDatabaseSettings setting;
    private bool _usetime;
    private bool _stripArtistPrefixes;
    public Hashtable artistsWithImageMissing;
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

    internal LatestMusicHandler()
    {
      InitDB();
      artistsWithImageMissing = new Hashtable();
    }

    internal bool IsInitialized
    {
      get { return isInitialized; }
      set { isInitialized = value; }
    }

    /// <summary>
    /// Initiation of the DatabaseManager.
    /// </summary>
    internal void InitDB()
    {
      try
      {
        m_db = MusicDatabase.Instance;
        IsInitialized = true;
      }
      catch (Exception e)
      {
        logger.Error("InitDB: " + e.ToString());
      }
    }

    internal void DoScanMusicShares()
    {
      logger.Info("Scanning music collection for new tracks - starting");
      try
      {
        _run = true;
        Work work = new Work(new DoWorkHandler(this.Run));
        work.ThreadPriority = ThreadPriority.Lowest;
        work.Description = "MusicDBReorg Thread";
        GlobalServiceProvider.Get<IThreadPool>().Add(work, QueuePriority.Low);
      }
      catch
      {
      }
    }

    private void Run()
    {
      while (_run)
      {
        // Start the Music DB Reorganization
        //_reorgRunning = true;

        try
        {
          LoadShares();
          if (_usetime)
          {
            m_db.MusicDatabaseReorg(m_Shares); //, setting);                        
          }
        }
        catch
        {
        }

        if (_usetime)
        {
          // store last run
          using (Settings writer = new MPSettings())
          {
            writer.SetValue("musicdbreorg", "lastrun", DateTime.Now.Day);
          }
        }
        //_reorgRunning = false;
        _run = false;

      }
      GetLatestMediaInfo(false);
      logger.Info("Scanning music collection for new tracks - done");
    }

    private int LoadShares()
    {
      m_Shares.Clear();
      using (Settings xmlreader = new MPSettings())
      {
        _usetime = xmlreader.GetValueAsBool("musicfiles", "updateSinceLastImport", true);
        _stripArtistPrefixes = xmlreader.GetValueAsBool("musicfiles", "stripartistprefixes", false);
        if (_usetime)
        {
          for (int i = 0; i < MediaPortal.Util.VirtualDirectory.MaxSharesCount; i++)
          {
            string strSharePath = String.Format("sharepath{0}", i);
            string shareType = String.Format("sharetype{0}", i);
            string shareScan = String.Format("sharescan{0}", i);

            string ShareType = xmlreader.GetValueAsString("music", shareType, string.Empty);
            if (ShareType == "yes")
            {
              continue; // We can't monitor ftp shares
            }

            bool ShareScanData = xmlreader.GetValueAsBool("music", shareScan, true);
            if (!ShareScanData)
            {
              continue;
            }

            string SharePath = xmlreader.GetValueAsString("music", strSharePath, string.Empty);

            if (SharePath.Length > 0)
            {
              m_Shares.Add(SharePath);
            }
          }
        }
      }
      return 0;
    }

    internal void GetLatestMediaInfo(bool _onStartUp)
    {
      try
      {
        int P = LatestMediaHandlerSetup.SyncPointMusicUpdate ;
        int sync = Interlocked.CompareExchange(ref LatestMediaHandlerSetup.SyncPointMusicUpdate, 1, 0);
        logger.Debug("GetLatestMediaInfo: Music sync ["+P+"-"+sync+"-"+LatestMediaHandlerSetup.SyncPointMusicUpdate+"]");
        if (sync == 0)
        {
          int z = 1;
          artistsWithImageMissing = new Hashtable();
          string windowId = GUIWindowManager.ActiveWindow.ToString();
          logger.Debug("GetLatestMediaInfo: Music [" + LatestMediaHandlerSetup.LatestMusic + "] ID:" + windowId);
          if (LatestMediaHandlerSetup.LatestMusic.Equals("True", StringComparison.CurrentCulture) &&
              !(windowId.Equals("987656", StringComparison.CurrentCulture) || 
                windowId.Equals("504", StringComparison.CurrentCulture) || 
                windowId.Equals("501", StringComparison.CurrentCulture) ||
                windowId.Equals("500", StringComparison.CurrentCulture)
               )
             )
          {
            //Music
            try
            {
              LatestMediaHandler.LatestsCollection hTable = GetLatestMusic(_onStartUp, LatestMediaHandlerSetup.LatestMusicType);
              for (int i = 0; i < 3; i++)
              {
                LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.music.latest" + z + ".thumb", string.Empty);
                LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.music.latest" + z + ".artist", string.Empty);
                LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.music.latest" + z + ".album", string.Empty);
                LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.music.latest" + z + ".dateAdded", string.Empty);
                LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.music.latest" + z + ".fanart", string.Empty);
                LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.music.latest" + z + ".genre", string.Empty);
                z++;
              }
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.music.latest.enabled", "false");
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.music.latest.hasnew", "false");

              if (LatestMediaHandlerSetup.LatestMusicType.Equals("Latest Added Music"))
                LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.music.label", Translation.LabelLatestAdded);
              else if (LatestMediaHandlerSetup.LatestMusicType.Equals("Most Played Music"))
                LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.music.label", Translation.LabelMostPlayed);
              else if (LatestMediaHandlerSetup.LatestMusicType.Equals("Latest Played Music"))
                LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.music.label", Translation.LabelLatestPlayed);

              if (hTable != null)
              {
                z = 1;
                for (int i = 0; i < hTable.Count && i < 3; i++)
                {
                  logger.Info("Updating Latest Media Info: Latest music album " + z + ": " + hTable[i].Artist + " - " + hTable[i].Album);

                  LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.music.latest" + z + ".thumb", hTable[i].Thumb);
                  LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.music.latest" + z + ".artist", hTable[i].Artist);
                  LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.music.latest" + z + ".album", hTable[i].Album);
                  LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.music.latest" + z + ".dateAdded", hTable[i].DateAdded);
                  LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.music.latest" + z + ".fanart", hTable[i].Fanart);
                  LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.music.latest" + z + ".genre", hTable[i].Genre);
                  z++;
                }
                hTable.Clear();

                LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.music.latest.enabled", "true");
                LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.music.latest.hasnew", Utils.HasNewMusic ? "true" : "false");
                logger.Debug("Updating Latest Media Info: Latest music has new: " + (Utils.HasNewMusic ? "true" : "false"));
              }
              hTable = null;
              z = 1;

            }
            catch (Exception ex)
            {
              logger.Error("GetLatestMediaInfo (Music): " + ex.ToString());
            }
          }
          else
          {
            LatestMediaHandlerSetup.EmptyLatestMediaPropsMusic();
          }
          LatestMediaHandlerSetup.SyncPointMusicUpdate = 0;
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
        logger.Error("GetLatestMediaInfo (Music): " + ex.ToString());
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

        //Add Artist Details Menu Item
        pItem = new GUIListItem();
        pItem.Label = Translation.ArtistDetails;
        pItem.ItemId = 2;
        dlg.Add(pItem);

        //Add Album Details Menu Item
        pItem = new GUIListItem();
        pItem.Label = Translation.AlbumDetails;
        pItem.ItemId = 3;
        dlg.Add(pItem);

        //Add filter menu item "Latest Added Music", "Most Played Music", "Latest Played Music"
        if (LatestMediaHandlerSetup.LatestMusicType.Equals("Latest Added Music"))
        {
          LatestMediaHandlerSetup.LatestMusicType = "Most Played Music";
          pItem = new GUIListItem();
          pItem.Label = Translation.MostPlayedMusic;
        }
        else if (LatestMediaHandlerSetup.LatestMusicType.Equals("Most Played Music"))
        {
          LatestMediaHandlerSetup.LatestMusicType = "Latest Played Music";
          pItem = new GUIListItem();
          pItem.Label = Translation.LatestPlayedMusic;
        }
        else if (LatestMediaHandlerSetup.LatestMusicType.Equals("Latest Played Music"))
        {
          LatestMediaHandlerSetup.LatestMusicType = "Latest Added Music";
          pItem = new GUIListItem();
          pItem.Label = Translation.LatestAddedMusic;
        }
        pItem.ItemId = 4;
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
            GUIControl gc = gw.GetControl(919199970);
            facade = gc as GUIFacadeControl;
            if (facade != null)
            {
              PlayMusicAlbum(facade.SelectedListItem.ItemId);
            }
            break;
          }
          case 2:
          {
            GUIWindow gw = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow);
            GUIControl gc = gw.GetControl(919199970);
            facade = gc as GUIFacadeControl;
            if (facade != null)
            {
              ShowArtistInfo(facade.SelectedListItem.Label, facade.SelectedListItem.Label2);
            }
            break;
          }
          case 3:
          {
            GUIWindow gw = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow);
            GUIControl gc = gw.GetControl(919199970);
            facade = gc as GUIFacadeControl;
            if (facade != null)
            {
              ShowAlbumInfo(GUIWindowManager.ActiveWindow, facade.SelectedListItem.Label, facade.SelectedListItem.Label2);
                //, (facade.SelectedListItem.MusicTag as MusicTag).FileName, facade.SelectedListItem.MusicTag as MusicTag);
            }
            break;
          }
          case 4:
          {
            GetLatestMediaInfo(false);
            break;
          }
        }
      }
      catch (Exception ex)
      {
        logger.Error("MyContextMenu: " + ex.ToString());
      }
    }

    public void ShowAlbumInfo(int parentWindowID, string artistName, string albumName)
    {
      Log.Debug("Searching for album: {0} - {1}", albumName, artistName);

      var dlgProgress = (GUIDialogProgress) GUIWindowManager.GetWindow((int) GUIWindow.Window.WINDOW_DIALOG_PROGRESS);
      var pDlgOK = (GUIDialogOK) GUIWindowManager.GetWindow((int) GUIWindow.Window.WINDOW_DIALOG_OK);

      var errorEncountered = true;
      var album = new AlbumInfo();
      var albumInfo = new MusicAlbumInfo();
      if (m_db.GetAlbumInfo(albumName, artistName, ref album))
      {
        // we already have album info in database so just use that
        albumInfo.Set(album);
        errorEncountered = false;
      }
      else
      {
// lookup details.  start with artist

        if (null != pDlgOK && !Win32API.IsConnectedToInternet())
        {
          pDlgOK.SetHeading(703);
          pDlgOK.SetLine(1, 703);
          pDlgOK.SetLine(2, string.Empty);
          pDlgOK.DoModal(GUIWindowManager.ActiveWindow);
          return;
        }

        // show dialog box indicating we're searching the album
        if (dlgProgress != null)
        {
          dlgProgress.Reset();
          dlgProgress.SetHeading(326);
          dlgProgress.SetLine(1, albumName);
          dlgProgress.SetLine(2, artistName);
          dlgProgress.SetPercentage(0);
          dlgProgress.StartModal(GUIWindowManager.ActiveWindow);
          dlgProgress.Progress();
          dlgProgress.ShowProgressBar(true);
        }

        var scraper = new AllmusicSiteScraper();
        List<AllMusicArtistMatch> artists;
        var selectedMatch = new AllMusicArtistMatch();

        if (scraper.GetArtists(artistName, out artists))
        {
          if (null != dlgProgress)
          {
            dlgProgress.SetPercentage(20);
            dlgProgress.Progress();
          }
          if (artists.Count == 1)
          {
            // only have single match so no need to ask user
            Log.Debug("Single Artist Match Found");
            selectedMatch = artists[0];
          }
          else
          {
            // need to get user to choose which one to use
            Log.Debug("Muliple Artist Match Found ({0}) prompting user", artists.Count);
            var pDlg = (GUIDialogSelect2) GUIWindowManager.GetWindow((int) GUIWindow.Window.WINDOW_DIALOG_SELECT2);
            if (null != pDlg)
            {
              pDlg.Reset();
              pDlg.SetHeading(GUILocalizeStrings.Get(1303));

              foreach (var i in artists.Select(artistMatch => new GUIListItem
                                                              {
                                                                Label = artistMatch.Artist + " - " + artistMatch.Genre,
                                                                Label2 = artistMatch.YearsActive,
                                                                Path = artistMatch.ArtistUrl,
                                                                IconImage = artistMatch.ImageUrl
                                                              }))
              {
                pDlg.Add(i);
              }
              pDlg.DoModal(GUIWindowManager.ActiveWindow);

              // and wait till user selects one
              var iSelectedMatch = pDlg.SelectedLabel;
              if (iSelectedMatch < 0)
              {
                return;
              }
              selectedMatch = artists[iSelectedMatch];
            }

            if (null != dlgProgress)
            {
              dlgProgress.Reset();
              dlgProgress.SetHeading(326);
              dlgProgress.SetLine(1, albumName);
              dlgProgress.SetLine(2, artistName);
              dlgProgress.SetPercentage(40);
              dlgProgress.StartModal(GUIWindowManager.ActiveWindow);
              dlgProgress.ShowProgressBar(true);
              dlgProgress.Progress();
            }
          }

          string strAlbumUrl;
          if (scraper.GetAlbumUrl(albumName, selectedMatch.ArtistUrl, out strAlbumUrl))
          {
            if (null != dlgProgress)
            {
              dlgProgress.SetPercentage(60);
              dlgProgress.Progress();
            }
            if (albumInfo.Parse(strAlbumUrl))
            {
              if (null != dlgProgress)
              {
                dlgProgress.SetPercentage(80);
                dlgProgress.Progress();
              }
              m_db.AddAlbumInfo(albumInfo.Get());
              errorEncountered = false;
            }
          }
        }
      }

      if (null != dlgProgress)
      {
        dlgProgress.SetPercentage(100);
        dlgProgress.Progress();
        dlgProgress.Close();
        dlgProgress = null;
      }

      if (!errorEncountered)
      {
        var pDlgAlbumInfo =
          (MediaPortal.GUI.Music.GUIMusicInfo) GUIWindowManager.GetWindow((int) GUIWindow.Window.WINDOW_MUSIC_INFO);
        if (null != pDlgAlbumInfo)
        {
          pDlgAlbumInfo.Album = albumInfo;

          pDlgAlbumInfo.DoModal(parentWindowID);
          if (pDlgAlbumInfo.NeedsRefresh)
          {
            m_db.DeleteAlbumInfo(albumName, artistName);
            ShowAlbumInfo(parentWindowID, artistName, albumName);
            return;
          }
        }
      }
      else
      {
        Log.Debug("No Album Found");

        if (null != pDlgOK)
        {
          pDlgOK.SetHeading(187);
          pDlgOK.SetLine(1, 187);
          pDlgOK.SetLine(2, string.Empty);
          pDlgOK.DoModal(GUIWindowManager.ActiveWindow);
        }
      }
    }

    protected void ShowAlbumInfo(string artistName, string albumName)
    {
      ShowAlbumInfo(GUIWindowManager.ActiveWindow, artistName, albumName);
    }

    protected virtual void ShowArtistInfo(string artistName, string albumName)
    {
      Log.Debug("Looking up Artist: {0}", albumName);

      var dlgProgress = (GUIDialogProgress) GUIWindowManager.GetWindow((int) GUIWindow.Window.WINDOW_DIALOG_PROGRESS);
      var pDlgOK = (GUIDialogOK) GUIWindowManager.GetWindow((int) GUIWindow.Window.WINDOW_DIALOG_OK);

      var errorEncountered = true;
      var artist = new ArtistInfo();
      var artistInfo = new MusicArtistInfo();
      if (m_db.GetArtistInfo(artistName, ref artist))
      {
        // we already have artist info in database so just use that
        artistInfo.Set(artist);
        errorEncountered = false;
      }
      else
      {
        // lookup artist details

        if (null != pDlgOK && !Win32API.IsConnectedToInternet())
        {
          pDlgOK.SetHeading(703);
          pDlgOK.SetLine(1, 703);
          pDlgOK.SetLine(2, string.Empty);
          pDlgOK.DoModal(GUIWindowManager.ActiveWindow);
          return;
        }

        // show dialog box indicating we're searching the artist
        if (dlgProgress != null)
        {
          dlgProgress.Reset();
          dlgProgress.SetHeading(320);
          dlgProgress.SetLine(1, artistName);
          dlgProgress.SetLine(2, string.Empty);
          dlgProgress.SetPercentage(0);
          dlgProgress.StartModal(GUIWindowManager.ActiveWindow);
          dlgProgress.Progress();
          dlgProgress.ShowProgressBar(true);
        }

        var scraper = new AllmusicSiteScraper();
        List<AllMusicArtistMatch> artists;
        if (scraper.GetArtists(artistName, out artists))
        {
          var selectedMatch = new AllMusicArtistMatch();
          if (artists.Count == 1)
          {
            // only have single match so no need to ask user
            Log.Debug("Single Artist Match Found");
            selectedMatch = artists[0];
            errorEncountered = false;
          }
          else
          {
            // need to get user to choose which one to use
            Log.Debug("Muliple Artist Match Found ({0}) prompting user", artists.Count);
            var pDlg = (GUIDialogSelect2) GUIWindowManager.GetWindow((int) GUIWindow.Window.WINDOW_DIALOG_SELECT2);
            if (null != pDlg)
            {
              pDlg.Reset();
              pDlg.SetHeading(GUILocalizeStrings.Get(1303));
              foreach (var i in artists.Select(artistMatch => new GUIListItem
                                                              {
                                                                Label = artistMatch.Artist + " - " + artistMatch.Genre,
                                                                Label2 = artistMatch.YearsActive,
                                                                Path = artistMatch.ArtistUrl,
                                                                IconImage = artistMatch.ImageUrl
                                                              }))
              {
                pDlg.Add(i);
              }
              pDlg.DoModal(GUIWindowManager.ActiveWindow);

              // and wait till user selects one
              var iSelectedMatch = pDlg.SelectedLabel;
              if (iSelectedMatch < 0)
              {
                return;
              }
              selectedMatch = artists[iSelectedMatch];
            }

            if (null != dlgProgress)
            {
              dlgProgress.Reset();
              dlgProgress.SetHeading(320);
              dlgProgress.SetLine(1, artistName);
              dlgProgress.SetLine(2, string.Empty);
              dlgProgress.SetPercentage(40);
              dlgProgress.StartModal(GUIWindowManager.ActiveWindow);
              dlgProgress.ShowProgressBar(true);
              dlgProgress.Progress();
            }
          }

          if (null != dlgProgress)
          {
            dlgProgress.SetPercentage(60);
            dlgProgress.Progress();
          }
          if (artistInfo.Parse(selectedMatch.ArtistUrl))
          {
            if (null != dlgProgress)
            {
              dlgProgress.SetPercentage(80);
              dlgProgress.Progress();
            }
            m_db.AddArtistInfo(artistInfo.Get());
            errorEncountered = false;
          }
        }
      }


      if (null != dlgProgress)
      {
        dlgProgress.SetPercentage(100);
        dlgProgress.Progress();
        dlgProgress.Close();
        dlgProgress = null;
      }

      if (!errorEncountered)
      {
        var pDlgArtistInfo =
          (MediaPortal.GUI.Music.GUIMusicArtistInfo)
            GUIWindowManager.GetWindow((int) GUIWindow.Window.WINDOW_ARTIST_INFO);
        if (null != pDlgArtistInfo)
        {
          pDlgArtistInfo.Artist = artistInfo;
          pDlgArtistInfo.DoModal(GUIWindowManager.ActiveWindow);

          if (pDlgArtistInfo.NeedsRefresh)
          {
            m_db.DeleteArtistInfo(artistInfo.Artist);
            ShowArtistInfo(artistName, albumName);
          }
        }
      }
      else
      {
        Log.Debug("Unable to get artist details");

        if (null != pDlgOK)
        {
          pDlgOK.SetHeading(702);
          pDlgOK.SetLine(1, 702);
          pDlgOK.SetLine(2, string.Empty);
          pDlgOK.DoModal(GUIWindowManager.ActiveWindow);
        }
      }
    }

    /// <summary>
    /// Returns last music track info added to MP music database.
    /// </summary>
    /// <returns>Hashtable containg artist names</returns>
    internal LatestMediaHandler.LatestsCollection GetLatestMusic(bool _onStartUp, string type)
    {
      LatestsCollection result = new LatestsCollection();
      int x = 0;
      try
      {
        GUIWindow gw = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow);
        GUIControl gc = gw.GetControl(919199970);
        facade = gc as GUIFacadeControl;
        if (facade != null)
        {
          facade.Clear();
        }
        if (al != null)
        {
          al.Clear();
        }

        string sqlQuery = string.Empty;
        if (type.Equals("Latest Added Music"))
        {
          // sqlQuery = "select distinct strAlbumArtist, strAlbum, dateAdded, strGenre, strPath from tracks order by dateAdded desc limit 50;";
          sqlQuery = "select distinct strAlbumArtist, strAlbum, dateAdded, strGenre, RTRIM(strPath,REPLACE(strPath,'\','')) as strPath from tracks order by dateAdded desc limit 10;";
        }
        else if (type.Equals("Most Played Music"))
        {
          // sqlQuery = "select distinct strAlbumArtist, strAlbum, dateAdded, strGenre, strPath from tracks order by iTimesPlayed desc limit 50;";
          sqlQuery = "select distinct strAlbumArtist, strAlbum, dateAdded, strGenre, RTRIM(strPath,REPLACE(strPath,'\','')) as strPath from tracks order by iTimesPlayed desc limit 10;";
        }
        else if (type.Equals("Latest Played Music"))
        {
          // sqlQuery = "select distinct strAlbumArtist, strAlbum, dateAdded, strGenre, strPath from tracks order by dateLastPlayed desc limit 50;";
          sqlQuery = "select distinct strAlbumArtist, strAlbum, dateAdded, strGenre, RTRIM(strPath,REPLACE(strPath, '\','')) as strPath from tracks order by dateLastPlayed desc limit 10;";
        }

        List<Song> songInfo = new List<Song>();
        m_db.GetSongsByFilter(sqlQuery, out songInfo, "tracks");
        Hashtable ht = new Hashtable();

        string key = string.Empty;
        latestMusicAlbums = new Hashtable();

        logger.Debug ("GetLatestMusic: Mode: " + type + " - " + songInfo.Count) ;

        int i0 = 1;
        foreach (Song mySong in songInfo)
        {
          string fanart = mySong.AlbumArtist;
          string album = mySong.Album;
          string sPath = mySong.FileName;

          if (sPath != null && sPath.Length > 0) // && sPath.IndexOf("\\") > 0)
          {
            sPath = Path.GetDirectoryName(sPath);
          }

          string dateAdded = string.Empty;
          try
          {
            dateAdded = String.Format("{0:" + LatestMediaHandlerSetup.DateFormat + "}", mySong.DateTimeModified);
          }
          catch { }
          //string dateAdded = mySong.DateTimeModified.ToString(LatestMediaHandlerSetup.DateFormat, CultureInfo.CurrentCulture);

          if (album == null || album.Trim().Length == 0)
          {
            album = " ";
          }

          key = fanart + "#" + album;
          if (!ht.Contains(key))
          {
            //Get Fanart
            string CurrSelectedMusic = String.Empty;
            CurrSelectedMusic = String.Empty;
            string sFilename1 = "";
            string sFilename2 = "";
            try
            {
              Hashtable ht2 = UtilsFanartHandler.GetMusicFanartForLatest(mySong.AlbumArtist, mySong.Album);
              if (ht2 == null || ht2.Count < 1 && !_onStartUp)
              {
                UtilsFanartHandler.ScrapeFanartAndThumb(mySong.AlbumArtist, mySong.Album);
                ht2 = UtilsFanartHandler.GetMusicFanartForLatest(mySong.AlbumArtist, mySong.Album);
              }

              if (ht2 == null || ht2.Count < 1)
              {
                ht2 = UtilsFanartHandler.GetMusicFanartForLatest(mySong.Artist, mySong.Album);

                if (ht2 == null || ht2.Count < 1)
                {
                  UtilsFanartHandler.ScrapeFanartAndThumb(mySong.Artist, mySong.Album);
                  ht2 = UtilsFanartHandler.GetMusicFanartForLatest(mySong.Artist, mySong.Album);
                }
              }

              if (ht2 == null || ht2.Count < 1)
              {
                if (!artistsWithImageMissing.Contains(UtilsFanartHandler.GetFHArtistName(mySong.AlbumArtist)))
                {
                  artistsWithImageMissing.Add(UtilsFanartHandler.GetFHArtistName(mySong.AlbumArtist), UtilsFanartHandler.GetFHArtistName(mySong.AlbumArtist));
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

            string thumb = string.Empty;
            string sArtist = fanart;
            thumb = MediaPortal.Util.Utils.GetLargeCoverArtName(Thumbs.MusicAlbum, sArtist + "-" + mySong.Album);
            if (thumb == null || thumb.Length < 1 || !File.Exists(thumb))
            {
              if (sArtist != null && sArtist.Length > 0 && sArtist.IndexOf("|") > 0)
              {
                string[] sArtists = sArtist.Split('|');
                foreach (string _sArtist in sArtists)
                {
                  thumb = MediaPortal.Util.Utils.GetLargeCoverArtName(Thumbs.MusicArtists, _sArtist.Trim());
                  if (thumb != null && thumb.Length > 0 && File.Exists(thumb))
                  {
                    break;
                  }
                }
              }
              else
              {
                thumb = MediaPortal.Util.Utils.GetLargeCoverArtName(Thumbs.MusicArtists, sArtist);
              }
            }
            if (thumb == null || thumb.Length < 1 || !File.Exists(thumb))
            {
              thumb = "";
              if (!artistsWithImageMissing.Contains(UtilsFanartHandler.GetFHArtistName(sArtist)))
              {
                artistsWithImageMissing.Add(UtilsFanartHandler.GetFHArtistName(sArtist), UtilsFanartHandler.GetFHArtistName(sArtist));
              }
            }
            if (thumb == null || thumb.Length < 1)
            {
              thumb = "defaultAudioBig.png";
            }

            result.Add(new LatestMediaHandler.Latest(dateAdded, thumb, sFilename1, mySong.FileName, 
                       null, 
                       sArtist, mySong.Album, mySong.Genre.Replace("|", ","), 
                       null, null, null, null, null, null, null, null, null, null, null, null));
            ht.Add(key, key);
            latestMusicAlbums.Add(i0, sPath);
            /*if (x < 3)
                        {                                                     
                            
                        } */
            //if (facade != null)
            //{
            AddToFilmstrip(result[x], i0);
            //}
            x++;
            i0++;
          }

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

        if (ht != null)
        {
          ht.Clear();
        }
        ht = null;

      }
      catch (Exception ex)
      {
        logger.Error("GetLatestMusic: " + ex.ToString());
      }

      try
      {
        Utils.HasNewMusic = false;

        string sqlQuery = "select distinct strAlbumArtist, strAlbum, dateAdded, strGenre, strPath from tracks order by dateAdded desc limit 3;";

        List<Song> songInfo = new List<Song>();
        m_db.GetSongsByFilter(sqlQuery, out songInfo, "tracks");

        foreach (Song mySong in songInfo)
        {
          try
          {
            if (mySong.DateTimeModified > Utils.NewDateTime)
              Utils.HasNewMusic = true;
          }
          catch { }
        }
      }
      catch (Exception ex)
      {
        logger.Error("GetLatestMusic (HasNew): " + ex.ToString());
      }

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
        mt.FileName = latests.Title;
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
        //item.Path = latests.Summary;
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
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.music.selected.thumb", item.IconImageBig);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.music.selected.artist", item.Label);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.music.selected.album", item.Label2);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.music.selected.dateAdded", item.Label3);
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.music.selected.genre", item.Path);
          selectedFacadeItem1 = item.ItemId;

          GUIWindow gw = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow);
          GUIControl gc = gw.GetControl(919199970);
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
        GUIControl gc = gw.GetControl(919199970);
        facade = gc as GUIFacadeControl;
        if (facade != null && gw.GetFocusControlId() == 919199970 && facade.SelectedListItem != null)
        {
          int _id = facade.SelectedListItem.ItemId;
          String _image = facade.SelectedListItem.DVDLabel;
          if (selectedFacadeItem2 != _id)
          {
            Utils.LoadImage(_image, ref images);
            if (showFanart == 1)
            {
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.music.selected.fanart1", _image);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.music.selected.showfanart1", "true");
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.music.selected.showfanart2", "");
              Thread.Sleep(1000);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.music.selected.fanart2", "");
              showFanart = 2;
            }
            else
            {
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.music.selected.fanart2", _image);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.music.selected.showfanart2", "true");
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.music.selected.showfanart1", "");
              Thread.Sleep(1000);
              LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.music.selected.fanart1", "");
              showFanart = 1;
            }
            Utils.UnLoadImage(_image, ref images);
            selectedFacadeItem2 = _id;
          }
        }
        else
        {
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.music.selected.fanart1", " ");
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.music.selected.fanart2", " ");
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.music.selected.showfanart1", "true");
          LatestMediaHandlerSetup.SetProperty("#latestMediaHandler.music.selected.showfanart2", "");
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
        StartPlayback();
      }

    }

    private void StartPlayback()
    {
      // if we got a playlist start playing it
      if (playlistPlayer.GetPlaylist(MediaPortal.Playlists.PlayListType.PLAYLIST_MUSIC_TEMP).Count > 0)
      {
        playlistPlayer.CurrentPlaylistType = MediaPortal.Playlists.PlayListType.PLAYLIST_MUSIC_TEMP;
        playlistPlayer.Reset();
        playlistPlayer.Play(0);
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
              tag.AlbumArtist = MediaPortal.Util.Utils.FormatMultiItemMusicStringTrim(tag.AlbumArtist, _stripArtistPrefixes);
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
