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
extern alias LMHNLog;

using MediaPortal.Dialogs;
using MediaPortal.GUI.Library;
using MediaPortal.Music.Database;
using MediaPortal.Player;
using MediaPortal.Profile;
using MediaPortal.Services;
using MediaPortal.TagReader;
using MediaPortal.Threading;
using MediaPortal.Util;

using LMHNLog.NLog;

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Timers;
using System.Threading.Tasks;

namespace LatestMediaHandler
{
  /// <summary>
  /// Class handling all database access.
  /// </summary>
  internal class LatestMusicHandler
  {
    #region declarations

    private static Logger logger = LogManager.GetCurrentClassLogger();
    private MusicDatabase m_db = null;

    private LatestsCollection latestMusicAlbums;
    internal Hashtable latestMusicAlbumsFolders;
    public Hashtable artistsWithImageMissing;

    private MediaPortal.Playlists.PlayListPlayer playlistPlayer;
    private ArrayList m_Shares = new ArrayList();
    private bool _run;
    private bool _usetime;
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

    public const int ControlID = 919199970;
    public const int Play1ControlID = 91919997;
    public const int Play2ControlID = 91919998;
    public const int Play3ControlID = 91919999;
    public const int Play4ControlID = 91919901;

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

    internal LatestMusicHandler(int id = ControlID)
    {
      InitDB();
      artistsWithImageMissing = new Hashtable();

      ControlIDFacades = new List<LatestsFacade>();
      ControlIDPlays = new List<int>();
      //
      ControlIDFacades.Add(new LatestsFacade(id, "Music"));
      if (id == ControlID)
      {
        ControlIDPlays.Add(Play1ControlID);
        ControlIDPlays.Add(Play2ControlID);
        ControlIDPlays.Add(Play3ControlID);
        ControlIDPlays.Add(Play4ControlID);
      }
      CurrentFacade.Type = Utils.LatestMusicType;

      Utils.ClearSelectedMusicProperty(CurrentFacade);
      EmptyLatestMediaProperties();
    }

    internal LatestMusicHandler(LatestsFacade facade) : this (facade.ControlID)
    {
      ControlIDFacades[ControlIDFacades.Count - 1] = facade;
    }

    /// <summary>
    /// Initiation of the DatabaseManager.
    /// </summary>
    internal void InitDB()
    {
      try
      {
        m_db = MusicDatabase.Instance;
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
      {   }
    }

    private void Run()
    {
      while (_run)
      {
        try
        {
          LoadShares();
          if (_usetime)
          {
            m_db.MusicDatabaseReorg(m_Shares); //, setting);                        
          }
        }
        catch
        {   }

        if (_usetime)
        {
          // store last run
          using (Settings writer = new MPSettings())
          {
            writer.SetValue("musicdbreorg", "lastrun", DateTime.Now.Day);
          }
        }
        _run = false;
      }
      GetLatestMediaInfoThread();
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

            if (!string.IsNullOrEmpty(SharePath))
            {
              m_Shares.Add(SharePath);
            }
          }
        }
      }
      return 0;
    }

    internal void GetLatestMediaInfoThread()
    {
      // Music
      try
      {
        RefreshWorker MyRefreshWorker = new RefreshWorker();
        MyRefreshWorker.RunWorkerCompleted += MyRefreshWorker.OnRunWorkerCompleted;
        // System.Threading.SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
        MyRefreshWorker.RunWorkerAsync(this);
      }
      catch (Exception ex)
      {
        logger.Error("GetLatestMediaInfoThread: " + ex.ToString());
      }
    }

    internal void EmptyLatestMediaProperties()
    {
      if (!MainFacade && !CurrentFacade.AddProperties)
      {
        Utils.SetProperty("#latestMediaHandler." + CurrentFacade.Handler.ToLowerInvariant() + (!MainFacade ? ".info." + CurrentFacade.ControlID.ToString() : string.Empty) + ".latest.enabled", "false");
        return;
      }

      Utils.ClearLatestsMusicProperty(CurrentFacade, CurrentFacade.MusicTitle, MainFacade);
    }

    internal void GetLatestMediaInfo(bool _onStartUp)
    {
      if (Interlocked.CompareExchange(ref CurrentFacade.Update, 1, 0) != 0)
        return ;

      artistsWithImageMissing = new Hashtable();
      if (!Utils.LatestMusic)
      {
        EmptyLatestMediaProperties();
        CurrentFacade.Update = 0;
        return;
      }

      //Music
      LatestsCollection hTable = GetLatestMusic(_onStartUp);
      LatestsToFilmStrip(latestMusicAlbums);

      if (MainFacade || CurrentFacade.AddProperties)
      {
        EmptyLatestMediaProperties();

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
        Utils.UpdateLatestsUpdate(Utils.LatestsCategory.Music, DateTime.Now);
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

        // Music Types
        pItem = new GUIListItem();
        pItem.Label = "[^] " + CurrentFacade.MusicTitle;
        pItem.ItemId = 4;
        dlg.Add(pItem);

        // Update
        pItem = new GUIListItem();
        pItem.Label = Translation.Update;
        pItem.ItemId = 5;
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
          case 3:
          {
            ShowInfo(dlg.SelectedId == 2);
            break;
          }
          case 4:
          {
            GUIDialogMenu ldlg = (GUIDialogMenu) GUIWindowManager.GetWindow((int) GUIWindow.Window.WINDOW_DIALOG_MENU);
            if (ldlg == null) return;

            ldlg.Reset();
            ldlg.SetHeading(924);

            // "Latest Added Music"
            pItem = new GUIListItem();
            pItem.Label = (CurrentFacade.Type == LatestsFacadeType.Latests ? "[X] " : string.Empty) + Translation.LatestAddedMusic;
            pItem.ItemId = 1;
            ldlg.Add(pItem);

            // "Latest Played Music"
            pItem = new GUIListItem();
            pItem.Label = (CurrentFacade.Type == LatestsFacadeType.Played ? "[X] " : string.Empty) + Translation.LatestPlayedMusic;
            pItem.ItemId = 2;
            ldlg.Add(pItem);

            // "Most Played Music"
            pItem = new GUIListItem();
            pItem.Label = (CurrentFacade.Type == LatestsFacadeType.MostPlayed ? "[X] " : string.Empty) + Translation.MostPlayedMusic;
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
              CurrentFacade.Type = LatestsFacadeType.Latests;
            }
            else if (ldlg.SelectedId == 2)
            {
              CurrentFacade.Type = LatestsFacadeType.Played;
            }
            else if (ldlg.SelectedId == 3)
            {
              CurrentFacade.Type = LatestsFacadeType.MostPlayed;
            }
            GetLatestMediaInfoThread();
            break;
          }
          case 5:
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

    public void ShowInfo(bool artist = true)
    {
      try
      {
        GUIWindow fWindow = GUIWindowManager.GetWindow(Utils.ActiveWindow);
        int FocusControlID = fWindow.GetFocusControlId();

        int idx = -1;
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
          string Artist = latestMusicAlbums[idx].Artist;
          string Album = latestMusicAlbums[idx].Album;

          if (artist && !string.IsNullOrEmpty(Artist))
          {
            ShowArtistInfo(Artist, Album);
          }
          if (!artist && !string.IsNullOrEmpty(Artist) && !string.IsNullOrEmpty(Album))
          {
            ShowAlbumInfo(Artist, Album);
          }
        }
      }
      catch (Exception ex)
      {
        logger.Error("ShowInfo: " + ex.ToString());
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
          pDlgOK.DoModal(Utils.ActiveWindow);
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
          dlgProgress.StartModal(Utils.ActiveWindow);
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
              pDlg.DoModal(Utils.ActiveWindow);

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
              dlgProgress.StartModal(Utils.ActiveWindow);
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
        var pDlgAlbumInfo = (MediaPortal.GUI.Music.GUIMusicInfo) GUIWindowManager.GetWindow((int) GUIWindow.Window.WINDOW_MUSIC_INFO);
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
          pDlgOK.DoModal(Utils.ActiveWindow);
        }
      }
    }

    protected void ShowAlbumInfo(string artistName, string albumName)
    {
      ShowAlbumInfo(Utils.ActiveWindow, artistName, albumName);
    }

    protected virtual void ShowArtistInfo(string artistName, string albumName)
    {
      Log.Debug("Looking up Artist: {0}", artistName);

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
          pDlgOK.DoModal(Utils.ActiveWindow);
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
          dlgProgress.StartModal(Utils.ActiveWindow);
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
              pDlg.DoModal(Utils.ActiveWindow);

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
              dlgProgress.StartModal(Utils.ActiveWindow);
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
        var pDlgArtistInfo = (MediaPortal.GUI.Music.GUIMusicArtistInfo) GUIWindowManager.GetWindow((int) GUIWindow.Window.WINDOW_ARTIST_INFO);
        if (null != pDlgArtistInfo)
        {
          pDlgArtistInfo.Artist = artistInfo;
          pDlgArtistInfo.DoModal(Utils.ActiveWindow);

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
          pDlgOK.DoModal(Utils.ActiveWindow);
        }
      }
    }

    /// <summary>
    /// Returns last music track info added to MP music database.
    /// </summary>
    /// <returns>Hashtable containg artist names</returns>
    internal LatestsCollection GetLatestMusic(bool _onStartUp)
    {
      latestMusicAlbums = new LatestsCollection();
      latestMusicAlbumsFolders = new Hashtable();

      int x = 0;
      try
      {
        string sqlQuery = "SELECT strAlbumArtist, strAlbum, iYear, strFileType, dateAdded, iTimesPlayed, dateLastPlayed, strGenre, strPath, "+
                                 "(SELECT strAMGBio FROM artistinfo WHERE LOWER(TRIM(T.strAlbumArtist,'| ')) = LOWER(TRIM(strArtist))) AS strLyrics "+
                          "FROM "+
                            "(SELECT strAlbumArtist, strAlbum, iYear, strFileType, "+
                                    "MAX(dateAdded) AS dateAdded, "+
                                    "CAST(ROUND(AVG(iTimesPlayed)) AS INTEGER) AS iTimesPlayed, "+
                                    "MAX(dateLastPlayed) AS dateLastPlayed, "+
                                    "GROUP_CONCAT(strGenre,'|') AS strGenre, "+
                                    "GROUP_CONCAT(strPath,'|') AS strPath "+
                             "FROM tracks "+
                             "WHERE strAlbumArtist IS NOT null AND TRIM(strAlbumArtist) <> '' "+
                             "GROUP BY strAlbumArtist, strAlbum, strFileType "+
                             "ORDER BY {0} DESC LIMIT {1}) AS T";
        // Add artistinfo.strAMGBio to strLyrics
        string sqlOrder = string.Empty;
        if (CurrentFacade.Type == LatestsFacadeType.Latests)
        {
          sqlOrder = "dateAdded";
        }
        else if (CurrentFacade.Type == LatestsFacadeType.MostPlayed)
        {
          sqlOrder = "AVG(iTimesPlayed)";
        }
        else if (CurrentFacade.Type == LatestsFacadeType.Played)
        {
          sqlOrder = "dateLastPlayed";
        }
        if (string.IsNullOrEmpty(sqlOrder))
          return null;

        sqlQuery = string.Format(sqlQuery, sqlOrder, Utils.FacadeMaxNum);

        string key = string.Empty;
        Hashtable ht = new Hashtable();

        List<Song> songInfo = new List<Song>();
        m_db.GetSongsByFilter(sqlQuery, out songInfo, "tracks");

        logger.Debug ("GetLatestMusic: Mode: " + CurrentFacade.Type + " Received: " + songInfo.Count + " songs.") ;

        int i0 = 1;
        foreach (Song mySong in songInfo)
        {
          string artist    = mySong.AlbumArtist;
          string album     = mySong.Album;
          string sFileType = mySong.FileType;

          key = artist + "#" + ((string.IsNullOrEmpty(album)) ? "-" : album) + "#" + ((string.IsNullOrEmpty(sFileType)) ? "-" : sFileType);
          // logger.Debug ("*** GetLatestMusic: "+Utils.Check(isnew)+" AlbumArtist: "+artist+ " Album: "+album+" Date: "+mySong.DateTimeModified+"/"+mySong.DateTimePlayed+" sPath: "+sPaths.Length+" Genre:"+sGenres);
          // logger.Debug ("*** GetLatestMusic: Key: "+key+" - "+ht.Contains(key));
          if (!ht.Contains(key))
          {
            ht.Add(key, key);
            //
            string sPaths    = Utils.GetDistinct(mySong.FileName);
            string sGenres   = Utils.GetDistinct(mySong.Genre != null ? mySong.Genre.Replace(",", "|") : string.Empty);
            string sYear     = (mySong.Year == 0 ? string.Empty : (mySong.Year == 1900 ? string.Empty : mySong.Year.ToString()));
            string dateAdded = string.Empty;
            bool   isnew     = false;

            //Get Fanart
            string CurrSelectedMusic = string.Empty;
            string sFilename1 = string.Empty;
            string sFilename2 = string.Empty;
            try
            {
              Hashtable ht2 = (Utils.FanartHandler ? UtilsFanartHandler.GetMusicFanartForLatest(mySong.Artist, mySong.AlbumArtist, mySong.Album) : null);
              if (Utils.FanartHandler)
              {
                if ((ht2 == null || ht2.Count < 1) && !_onStartUp)
                {
                  UtilsFanartHandler.ScrapeFanartAndThumb(mySong.AlbumArtist, mySong.Album);
                  ht2 = UtilsFanartHandler.GetMusicFanartForLatest(mySong.Artist, mySong.AlbumArtist, mySong.Album);
                }

                if (ht2 == null || ht2.Count < 1)
                {
                  if (!artistsWithImageMissing.Contains(UtilsFanartHandler.GetFHArtistName(mySong.AlbumArtist)))
                  {
                    artistsWithImageMissing.Add(UtilsFanartHandler.GetFHArtistName(mySong.AlbumArtist), UtilsFanartHandler.GetFHArtistName(mySong.AlbumArtist));
                  }
                }
              }
              else
              {
                if (!artistsWithImageMissing.Contains(mySong.AlbumArtist))
                {
                  artistsWithImageMissing.Add(mySong.AlbumArtist, mySong.AlbumArtist);
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
            catch
            {   }
            if (string.IsNullOrWhiteSpace(sFilename1))
            {
              sFilename1 = sFilename2;
            }

            string thumb = string.Empty;
            string sArtist = artist;
            string sAlbum = mySong.Album;
            if (!string.IsNullOrEmpty(sAlbum))
            {
              thumb = MediaPortal.Util.Utils.GetLargeCoverArtName(Thumbs.MusicAlbum, MediaPortal.Util.Utils.MakeFileName(sArtist) + "-" + MediaPortal.Util.Utils.MakeFileName(sAlbum));
            }
            if (string.IsNullOrEmpty(thumb) || !File.Exists(thumb))
            {
              if (!string.IsNullOrEmpty(sArtist))
              {
                string[] sArtists = sArtist.Split(Utils.PipesArray, StringSplitOptions.RemoveEmptyEntries);
                foreach (string _sArtist in sArtists)
                {
                  if (string.IsNullOrEmpty(_sArtist))
                  {
                    continue;
                  }
                  if (!string.IsNullOrEmpty(sAlbum))
                  {
                    thumb = MediaPortal.Util.Utils.GetLargeCoverArtName(Thumbs.MusicAlbum, MediaPortal.Util.Utils.MakeFileName(_sArtist.Trim()) + "-" + MediaPortal.Util.Utils.MakeFileName(sAlbum));
                  }
                  if (string.IsNullOrEmpty(thumb) || !File.Exists(thumb))
                  {
                    thumb = MediaPortal.Util.Utils.GetLargeCoverArtName(Thumbs.MusicArtists, MediaPortal.Util.Utils.MakeFileName(_sArtist.Trim()));
                  }
                  if (!string.IsNullOrEmpty(thumb) && File.Exists(thumb))
                    break;
                }
              }
            }
            if (string.IsNullOrEmpty(thumb) || !File.Exists(thumb))
            {
              thumb = "DefaultAudioBig.png";
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

            try
            {
              dateAdded = String.Format("{0:" + Utils.DateFormat + "}", mySong.DateTimeModified);
              isnew = ((mySong.DateTimeModified > Utils.NewDateTime) && (mySong.TimesPlayed <= 0));
            }
            catch 
            { 
              dateAdded = string.Empty;
              isnew = false;
            }

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

              if (mySong.DiscId > 0)
              {
                string fcdid = UtilsFanartHandler.GetFanartTVForLatestMedia(sArtist, sAlbum, mySong.DiscId.ToString(), Utils.FanartTV.MusicCDArt);
                if (!string.IsNullOrEmpty(fcdid))
                {
                  fcd = fcdid;
                }
              }
            }

            latestMusicAlbums.Add(new Latest(dateAdded, thumb, sFilename1, 
                                             sPaths, sFileType, // FileType 
                                             sArtist, sAlbum, sGenres, 
                                             null, null, 
                                             sFileType, 
                                             null, 
                                             sYear, 
                                             null, null, null, null, 
                                             mySong.DateTimePlayed.ToString(), 
                                             mySong.Lyrics, // Artist.BIO
                                             null,
                                             fbanner, fclearart, fclearlogo, fcd,
                                             isnew));
            latestMusicAlbumsFolders.Add(i0, sPaths);
            Utils.ThreadToSleep();
            //
            x++;
            i0++;
          }

          if (x == Utils.FacadeMaxNum)
            break;
        }

        if (ht != null)
          ht.Clear();
        ht = null;
      }
      catch (Exception ex)
      {
        logger.Error("GetLatestMusic: " + ex.ToString());
      }

      try
      {
        CurrentFacade.HasNew = false;

        string sqlQuery = "select distinct strAlbumArtist, strAlbum, dateAdded, iTimesPlayed, strGenre, strPath from tracks order by dateAdded desc limit 1;";

        List<Song> songInfo = new List<Song>();
        m_db.GetSongsByFilter(sqlQuery, out songInfo, "tracks");

        foreach (Song mySong in songInfo)
        {
          try
          {
            if ((mySong.DateTimeModified > Utils.NewDateTime) && (mySong.TimesPlayed <= 0))
            {
              CurrentFacade.HasNew = true;
            }
          }
          catch { }
        }
      }
      catch (Exception ex)
      {
        logger.Error("GetLatestMusic (HasNew): " + ex.ToString());
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
            // logger.Debug("Make Latest List: Music: " + LatestMediaHandlerSetup.LatestMusicType + ": " + latestMusicAlbums[i].Artist + " - " + latestMusicAlbums[i].Album);
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
        mt.FileName = latests.Title;
        mt.FileType = latests.Classification;
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
      if (!Utils.LatestMusic)
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
      if (!Utils.LatestMusic)
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
            CurrentFacade.FocusedID = CurrentFacade.Facade.SelectedListItemIndex;
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
              Utils.SetProperty("#latestMediaHandler.music.selected.fanart1", _image);
              Utils.SetProperty("#latestMediaHandler.music.selected.showfanart1", "true");
              Utils.SetProperty("#latestMediaHandler.music.selected.showfanart2", "false");
              Thread.Sleep(1000);
              Utils.SetProperty("#latestMediaHandler.music.selected.fanart2", string.Empty);
              showFanart = 2;
            }
            else
            {
              Utils.SetProperty("#latestMediaHandler.music.selected.fanart2", _image);
              Utils.SetProperty("#latestMediaHandler.music.selected.showfanart2", "true");
              Utils.SetProperty("#latestMediaHandler.music.selected.showfanart1", "false");
              Thread.Sleep(1000);
              Utils.SetProperty("#latestMediaHandler.music.selected.fanart1", string.Empty);
              showFanart = 1;
            }
            // Utils.UnLoadImage(_image, ref images);
            CurrentFacade.SelectedImage = _id;
          }
        }
        else
        {
          Utils.SetProperty("#latestMediaHandler.music.selected.fanart1", " ");
          Utils.SetProperty("#latestMediaHandler.music.selected.fanart2", " ");
          Utils.SetProperty("#latestMediaHandler.music.selected.showfanart1", "false");
          Utils.SetProperty("#latestMediaHandler.music.selected.showfanart2", "false");
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

    internal void SetupReceivers()
    {
      try
      {
        GUIWindowManager.Receivers += new SendMessageHandler(OnMessage);
        g_Player.PlayBackChanged += new g_Player.ChangedHandler(OnPlayBackChanged);
      }
      catch (Exception ex)
      {
        logger.Error("SetupMusicLatest: " + ex.ToString());
      }
    }

    internal void DisposeReceivers()
    {
      try
      {
        g_Player.PlayBackChanged -= new g_Player.ChangedHandler(OnPlayBackChanged);
        GUIWindowManager.Receivers -= new SendMessageHandler(OnMessage);
      }
      catch (Exception ex)
      {
        logger.Error("DisposeMusicLatest: " + ex.ToString());
      }
    }

    private void OnPlayBackChanged(g_Player.MediaType type, int stoptime, string filename)
    {
      try
      {
        if (type == g_Player.MediaType.Music)
        {
          if (CurrentFacade.Type == LatestsFacadeType.Played || CurrentFacade.Type == LatestsFacadeType.MostPlayed)
          {
            GetLatestMediaInfoThread();
          }
        }
      }
      catch (Exception ex)
      {
        logger.Error("OnPlayBackEnded: " + ex.ToString());
      }
    }

    private void OnMessage(GUIMessage message)
    {
      if (Utils.LatestMusic)
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
      bool Update = false;

      Utils.ThreadToSleep();
      switch (message.Message)
      {
        case GUIMessage.MessageType.GUI_MSG_PLAYBACK_ENDED:
        case GUIMessage.MessageType.GUI_MSG_PLAYBACK_STOPPED:
        {
          logger.Debug("Playback End/Stop detected: Refreshing latest.");
          Update = true;
          break;
        }
        case GUIMessage.MessageType.GUI_MSG_DATABASE_SCAN_ENDED:
        {
          logger.Debug("DB Scan end detected: Refreshing latest.");
          Update = true;
          break;
        }
      }

      if (Update)
      {
        GetLatestMediaInfoThread();
      }
    }

    internal void UpdateImageTimer(GUIWindow fWindow, Object stateInfo, ElapsedEventArgs e)
    {
      if (Utils.LatestMusic)
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
              Utils.SetProperty("#latestMediaHandler.music.selected.fanart1", " ");
              Utils.SetProperty("#latestMediaHandler.music.selected.fanart2", " ");
              Utils.UnLoadImages(ref images);
              ShowFanart = 1;
              CurrentFacade.SelectedImage = -1;
              NeedCleanup = false;
              NeedCleanupCount = 0;
            }
            else if (NeedCleanup && NeedCleanupCount == 0)
            {
              Utils.SetProperty("#latestMediaHandler.music.selected.showfanart1", "false");
              Utils.SetProperty("#latestMediaHandler.music.selected.showfanart2", "false");
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
          logger.Error("UpdateImageTimer (music): " + ex.ToString());
        }
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
      string SongsFiles = latestMusicAlbumsFolders[index].ToString().Trim();
      if (!string.IsNullOrEmpty(SongsFiles))
      {
        LoadSongsFromList (SongsFiles);
        StartPlayback();
      }
    }

    private void LoadSongsFromList(string SongsFiles)
    {
      // clear current playlist
      playlistPlayer = MediaPortal.Playlists.PlayListPlayer.SingletonPlayer;
      playlistPlayer.GetPlaylist(MediaPortal.Playlists.PlayListType.PLAYLIST_MUSIC_TEMP).Clear();
      int numSongs = 0;
      try
      {
        string[] sSongsFiles = SongsFiles.Split(Utils.PipesArray, StringSplitOptions.RemoveEmptyEntries);
        foreach (string sSongsFile in sSongsFiles)
        {
          MediaPortal.Playlists.PlayListItem item = new MediaPortal.Playlists.PlayListItem();
          item.FileName = sSongsFile;
          item.Type = MediaPortal.Playlists.PlayListItem.PlayListItemType.Audio;
          MusicTag tag = TagReader.ReadTag(sSongsFile);
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
        logger.Debug("LoadSongsFromList: Complete: " + numSongs);
      }
      catch (Exception ex)
      {
        logger.Error("LoadSongsFromList: " + ex.ToString());
      }
    }

    private void StartPlayback()
    {
      // if we got a playlist start playing it
      int Count = playlistPlayer.GetPlaylist(MediaPortal.Playlists.PlayListType.PLAYLIST_MUSIC_TEMP).Count;
      if (Count > 0)
      {
        playlistPlayer.CurrentPlaylistType = MediaPortal.Playlists.PlayListType.PLAYLIST_MUSIC_TEMP;
        playlistPlayer.Reset();
        playlistPlayer.Play(0);
        // MediaPortal.GUI.Music.GUIMusicBaseWindow.DoPlayNowJumpTo(Count);

        LatestGUIMusicBaseWindow latestGUIMusicBaseWindow = new LatestGUIMusicBaseWindow();
        latestGUIMusicBaseWindow.DoPlayNowJumpTo(Count);
      }
    }
  }

  internal class LatestGUIMusicBaseWindow : MediaPortal.GUI.Music.GUIMusicBaseWindow
  {
    public new bool DoPlayNowJumpTo(int playlistItemCount)
    {
      return base.DoPlayNowJumpTo(playlistItemCount);
    }
  }
}
