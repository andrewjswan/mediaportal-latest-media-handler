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
extern alias RealNLog;

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Timers;
using System.Windows.Forms;

using RealNLog.NLog;

using MediaPortal.GUI.Library;
using MediaPortal.Music.Database;
using MediaPortal.Player;
using MediaPortal.TagReader;
using MediaPortal.Util;
using MediaPortal.Profile;
using MediaPortal.Services;
using MediaPortal.Threading;
using MediaPortal.Dialogs;

namespace LatestMediaHandler
{
  /// <summary>
  /// Class handling all database access.
  /// </summary>
  internal class LatestMusicHandler
  {
    #region declarations

    private static Logger logger = LogManager.GetCurrentClassLogger();
    // private readonly object lockObject = new object();
    private bool isInitialized /* = false*/;
    private MusicDatabase m_db = null;

    private LatestsCollection latestMusicAlbums;
    internal Hashtable latestMusicAlbumsFolders;
    public Hashtable artistsWithImageMissing;

    private MediaPortal.Playlists.PlayListPlayer playlistPlayer;
    private ArrayList m_Shares = new ArrayList();
    private bool _run;
    //private bool _reorgRunning;
    //private MusicDatabaseSettings setting;
    private bool _usetime;
    private bool _stripArtistPrefixes;

    private ArrayList al = new ArrayList();
    internal ArrayList images = new ArrayList();
    internal ArrayList imagesThumbs = new ArrayList();

    private GUIFacadeControl facade = null;
    private int selectedFacadeItem1 = -1;
    private int selectedFacadeItem2 = -1;
    private int showFanart = 1;
    private bool needCleanup = false;
    private int needCleanupCount = 0;
    private int lastFocusedId = 0;

    #endregion

    public const int ControlID = 919199970;
    public const int Play1ControlID = 91919997;
    public const int Play2ControlID = 91919998;
    public const int Play3ControlID = 91919999;

    public const string MusicTypeLatestAdded = "Latest Added Music";
    public const string MusicTypeMostPlayed = "Most Played Music";
    public const string MusicTypeLatestPlayed = "Latest Played Music";

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

    internal LatestMusicHandler()
    {
      InitDB();
      artistsWithImageMissing = new Hashtable();

      ControlIDFacades = new List<int>();
      ControlIDPlays = new List<int>();
      //
      ControlIDFacades.Add(ControlID);
      ControlIDPlays.Add(Play1ControlID);
      ControlIDPlays.Add(Play2ControlID);
      ControlIDPlays.Add(Play3ControlID);
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
      {   }
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
        {   }

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

    internal void EmptyLatestMediaPropsMusic()
    {
      Utils.SetProperty("#latestMediaHandler.music.label", Translation.LabelLatestAdded);
      if (LatestMediaHandlerSetup.LatestMusicType.Equals(MusicTypeMostPlayed, StringComparison.CurrentCulture))
        Utils.SetProperty("#latestMediaHandler.music.label", Translation.LabelMostPlayed);
      else if (LatestMediaHandlerSetup.LatestMusicType.Equals(MusicTypeLatestPlayed, StringComparison.CurrentCulture))
        Utils.SetProperty("#latestMediaHandler.music.label", Translation.LabelLatestPlayed);

      Utils.SetProperty("#latestMediaHandler.music.latest.enabled", "false");
      Utils.SetProperty("#latestMediaHandler.music.hasnew", "false");
      for (int z = 1; z < 4; z++)
      {
        Utils.SetProperty("#latestMediaHandler.music.latest" + z + ".thumb", string.Empty);
        Utils.SetProperty("#latestMediaHandler.music.latest" + z + ".artist", string.Empty);
        Utils.SetProperty("#latestMediaHandler.music.latest" + z + ".artistbio", string.Empty);
        Utils.SetProperty("#latestMediaHandler.music.latest" + z + ".artistbiooutline", string.Empty);
        Utils.SetProperty("#latestMediaHandler.music.latest" + z + ".album", string.Empty);
        Utils.SetProperty("#latestMediaHandler.music.latest" + z + ".year", string.Empty);
        Utils.SetProperty("#latestMediaHandler.music.latest" + z + ".dateAdded", string.Empty);
        Utils.SetProperty("#latestMediaHandler.music.latest" + z + ".fanart", string.Empty);
        Utils.SetProperty("#latestMediaHandler.music.latest" + z + ".genre", string.Empty);
        Utils.SetProperty("#latestMediaHandler.music.latest" + z + ".new", "false");
      }
    }

    internal void GetLatestMediaInfo(bool _onStartUp)
    {
      int sync = Interlocked.CompareExchange(ref Utils.SyncPointMusicUpdate, 1, 0);
      if (sync != 0)
        return ;

      artistsWithImageMissing = new Hashtable();
      if (!LatestMediaHandlerSetup.LatestMusic.Equals("True", StringComparison.CurrentCulture))
      {
        EmptyLatestMediaPropsMusic();
        return;
      }

      //Music
      try
      {
        LatestsCollection hTable = GetLatestMusic(_onStartUp, LatestMediaHandlerSetup.LatestMusicType);
        EmptyLatestMediaPropsMusic();

        if (hTable != null)
        {
          int z = 1;
          for (int i = 0; i < hTable.Count && i < Utils.LatestsMaxNum; i++)
          {
            logger.Info("Updating Latest Media Info: Music: " + LatestMediaHandlerSetup.LatestMusicType + " Album " + z + ": " + hTable[i].Artist + " - " + hTable[i].Album + " [" + hTable[i].DateAdded + "/" + hTable[i].Id +"] - " + hTable[i].Fanart);

            string artistbio = (string.IsNullOrEmpty(hTable[i].Summary) ? Translation.NoDescription : hTable[i].Summary);
            string artistbiooutline = Utils.GetSentences(artistbio, Utils.latestPlotOutlineSentencesNum);
            Utils.SetProperty("#latestMediaHandler.music.latest" + z + ".thumb", hTable[i].Thumb);
            Utils.SetProperty("#latestMediaHandler.music.latest" + z + ".artist", hTable[i].Artist);
            Utils.SetProperty("#latestMediaHandler.music.latest" + z + ".artistbio", artistbio);
            Utils.SetProperty("#latestMediaHandler.music.latest" + z + ".artistbiooutline", artistbiooutline);
            Utils.SetProperty("#latestMediaHandler.music.latest" + z + ".album", hTable[i].Album);
            Utils.SetProperty("#latestMediaHandler.music.latest" + z + ".year", hTable[i].SeriesIndex);
            Utils.SetProperty("#latestMediaHandler.music.latest" + z + ".dateAdded", hTable[i].DateAdded);
            Utils.SetProperty("#latestMediaHandler.music.latest" + z + ".fanart", hTable[i].Fanart);
            Utils.SetProperty("#latestMediaHandler.music.latest" + z + ".genre", hTable[i].Genre);
            Utils.SetProperty("#latestMediaHandler.music.latest" + z + ".new", hTable[i].New);
            z++;
          }
          Utils.SetProperty("#latestMediaHandler.music.hasnew", Utils.HasNewMusic ? "true" : "false");
          logger.Debug("Updating Latest Media Info: Music: Has new: " + (Utils.HasNewMusic ? "true" : "false"));
        }
        // hTable = null;
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
        Utils.SetProperty("#latestMediaHandler.music.latest.enabled", "true");
      }
      else
        EmptyLatestMediaPropsMusic();
      Utils.SyncPointMusicUpdate = 0;
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

        /*
        //Add filter menu item "Latest Added Music", "Most Played Music", "Latest Played Music"
        if (LatestMediaHandlerSetup.LatestMusicType.Equals(MusicTypeLatestAdded, StringComparison.CurrentCulture))
        {
          pItem = new GUIListItem();
          pItem.Label = Translation.MostPlayedMusic;
        }
        else if (LatestMediaHandlerSetup.LatestMusicType.Equals(MusicTypeMostPlayed, StringComparison.CurrentCulture))
        {
          pItem = new GUIListItem();
          pItem.Label = Translation.LatestPlayedMusic;
        }
        else if (LatestMediaHandlerSetup.LatestMusicType.Equals(MusicTypeLatestPlayed, StringComparison.CurrentCulture))
        {
          pItem = new GUIListItem();
          pItem.Label = Translation.LatestAddedMusic;
        }
        pItem.ItemId = 4;
        dlg.Add(pItem);
        */

        // Music Types
        pItem = new GUIListItem();
        pItem.Label = "[^] " + (LatestMediaHandlerSetup.LatestMusicType.Equals(MusicTypeLatestAdded, StringComparison.CurrentCulture) ? Translation.LatestAddedMusic : ((LatestMediaHandlerSetup.LatestMusicType.Equals(MusicTypeLatestPlayed, StringComparison.CurrentCulture) ? Translation.LatestPlayedMusic  : Translation.MostPlayedMusic)));
        pItem.ItemId = 4;
        dlg.Add(pItem);

        pItem = new GUIListItem();
        pItem.Label = Translation.Update;
        pItem.ItemId = 5;
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
            pItem.Label = (LatestMediaHandlerSetup.LatestMusicType.Equals(MusicTypeLatestAdded, StringComparison.CurrentCulture) ? "[X] " : "") + Translation.LatestAddedMusic;
            pItem.ItemId = 1;
            ldlg.Add(pItem);

            // "Latest Played Music"
            pItem = new GUIListItem();
            pItem.Label = (LatestMediaHandlerSetup.LatestMusicType.Equals(MusicTypeLatestPlayed, StringComparison.CurrentCulture) ? "[X] " : "") + Translation.LatestPlayedMusic;
            pItem.ItemId = 2;
            ldlg.Add(pItem);

            // "Most Played Music"
            pItem = new GUIListItem();
            pItem.Label = (LatestMediaHandlerSetup.LatestMusicType.Equals(MusicTypeMostPlayed, StringComparison.CurrentCulture) ? "[X] " : "") + Translation.MostPlayedMusic;
            pItem.ItemId = 3;
            ldlg.Add(pItem);

            //Show Dialog
            ldlg.DoModal(GUIWindowManager.ActiveWindow);

            if (ldlg.SelectedLabel < 0)
            {
              return;
            }

            if (ldlg.SelectedId == 1)
            {
              LatestMediaHandlerSetup.LatestMusicType = MusicTypeLatestAdded;
            }
            else if (ldlg.SelectedId == 2)
            {
              LatestMediaHandlerSetup.LatestMusicType = MusicTypeLatestPlayed;
            }
            else if (ldlg.SelectedId == 3)
            {
              LatestMediaHandlerSetup.LatestMusicType = MusicTypeMostPlayed;
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
        GUIWindow fWindow = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow);
        int FocusControlID = fWindow.GetFocusControlId();

        int idx = -1;
        if (ControlIDPlays.Contains(FocusControlID))
        {
          idx = ControlIDPlays.IndexOf(FocusControlID);
        }
        //
        facade = Utils.GetLatestsFacade(ControlID);
        if (facade != null && facade.Focus && facade.SelectedListItem != null)
        {
          idx = facade.SelectedListItem.ItemId-1;
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
        var pDlgArtistInfo = (MediaPortal.GUI.Music.GUIMusicArtistInfo) GUIWindowManager.GetWindow((int) GUIWindow.Window.WINDOW_ARTIST_INFO);
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
    internal LatestsCollection GetLatestMusic(bool _onStartUp, string type)
    {
      latestMusicAlbums = new LatestsCollection();
      latestMusicAlbumsFolders = new Hashtable();

      int x = 0;
      try
      {
        // string sqlQuery = "SELECT DISTINCT strArtist, strAlbumArtist, strAlbum ...
        // MySQL: GROUP_CONCAT(... SEPARATOR '|')
        // string sqlQuery = @"SELECT strAlbumArtist, strAlbum, strFileType, (SELECT MAX(dateAdded) FROM tracks WHERE strAlbumArtist=T.strAlbumArtist AND strAlbum=T.strAlbum AND strFileType=T.strFileType) as dateAdded, GROUP_CONCAT(strGenre,'|') as strGenre, GROUP_CONCAT(RTRIM(strPath,REPLACE(strPath,'\','')),'|') as strPath FROM tracks T GROUP BY strAlbumArtist, strAlbum, strFileType ORDER BY {0} DESC LIMIT {1}";
        // string sqlQuery = "SELECT strAlbumArtist, strAlbum, strFileType, MAX(dateAdded) as dateAdded, CAST(ROUND(AVG(iTimesPlayed)) AS INTEGER) as iTimesPlayed, MAX(dateLastPlayed) as dateLastPlayed, "+
        //                          "GROUP_CONCAT(strGenre,'|') as strGenre, GROUP_CONCAT(strPath,'|') as strPath "+
        //                   "FROM tracks "+
        //                   "GROUP BY strAlbumArtist, strAlbum, strFileType "+
        //                   "ORDER BY {0} DESC LIMIT {1}";
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
        if (type.Equals(MusicTypeLatestAdded, StringComparison.CurrentCulture))
        {
          sqlOrder = "dateAdded";
        }
        else if (type.Equals(MusicTypeMostPlayed, StringComparison.CurrentCulture))
        {
          sqlOrder = "AVG(iTimesPlayed)";
        }
        else if (type.Equals(MusicTypeLatestPlayed, StringComparison.CurrentCulture))
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

        logger.Debug ("GetLatestMusic: Mode: " + type + " Received: " + songInfo.Count + " songs.") ;

        int i0 = 1;
        foreach (Song mySong in songInfo)
        {
          string artist    = mySong.AlbumArtist;
          string album     = mySong.Album;
          string sFileType = mySong.FileType;

          // logger.Debug ("*** GetLatestMusic: "+Utils.Check(isnew)+" AlbumArtist: "+artist+ " Album: "+album+" Date: "+mySong.DateTimeModified+"/"+mySong.DateTimePlayed+" sPath: "+sPaths.Length+" Genre:"+sGenres);
          
          key = artist + "#" + ((string.IsNullOrEmpty(album)) ? "-" : album) + "#" + ((string.IsNullOrEmpty(sFileType)) ? "-" : sFileType);
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
              Hashtable ht2 = UtilsFanartHandler.GetMusicFanartForLatest(mySong.Artist, mySong.AlbumArtist, mySong.Album);
              if ((ht2 == null || ht2.Count < 1) && !_onStartUp)
              {
                UtilsFanartHandler.ScrapeFanartAndThumb(mySong.AlbumArtist, mySong.Album);
                ht2 = UtilsFanartHandler.GetMusicFanartForLatest(mySong.Artist, mySong.AlbumArtist, mySong.Album);
              }

              if (ht2 == null || ht2.Count < 1)
              {
                if (!artistsWithImageMissing.Contains(UtilsFanartHandler.GetFHArtistName(mySong.AlbumArtist)))
                  artistsWithImageMissing.Add(UtilsFanartHandler.GetFHArtistName(mySong.AlbumArtist), UtilsFanartHandler.GetFHArtistName(mySong.AlbumArtist));
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
            catch
            {   }

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
              thumb = "defaultAudioBig.png";
              if (!artistsWithImageMissing.Contains(UtilsFanartHandler.GetFHArtistName(sArtist)))
                artistsWithImageMissing.Add(UtilsFanartHandler.GetFHArtistName(sArtist), UtilsFanartHandler.GetFHArtistName(sArtist));
            }

            try
            {
              dateAdded = String.Format("{0:" + LatestMediaHandlerSetup.DateFormat + "}", mySong.DateTimeModified);
              isnew = ((mySong.DateTimeModified > Utils.NewDateTime) && (mySong.TimesPlayed <= 0));
            }
            catch 
            { 
              dateAdded = string.Empty;
              isnew = false;
            }

            latestMusicAlbums.Add(new LatestMediaHandler.Latest(dateAdded, thumb, sFilename1, 
                                                                sPaths, sFileType, // FileType 
                                                                sArtist, mySong.Album, sGenres, 
                                                                null, null, 
                                                                sFileType, 
                                                                null, null, null, null, null, null, 
                                                                mySong.DateTimePlayed.ToString(), 
                                                                mySong.Lyrics, // Artist.BIO
                                                                sYear, // Year
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
        Utils.HasNewMusic = false;

        string sqlQuery = "select distinct strAlbumArtist, strAlbum, dateAdded, iTimesPlayed, strGenre, strPath from tracks order by dateAdded desc limit 1;";

        List<Song> songInfo = new List<Song>();
        m_db.GetSongsByFilter(sqlQuery, out songInfo, "tracks");

        foreach (Song mySong in songInfo)
        {
          try
          {
            if ((mySong.DateTimeModified > Utils.NewDateTime) && (mySong.TimesPlayed <= 0))
              Utils.HasNewMusic = true;
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
          Utils.SetProperty("#latestMediaHandler.music.selected.thumb", item.IconImageBig);
          Utils.SetProperty("#latestMediaHandler.music.selected.artist", item.Label);
          Utils.SetProperty("#latestMediaHandler.music.selected.album", item.Label2);
          Utils.SetProperty("#latestMediaHandler.music.selected.dateAdded", item.Label3);
          Utils.SetProperty("#latestMediaHandler.music.selected.genre", item.Path);

          int i = item.ItemId - 1;
          string artistbio = (string.IsNullOrEmpty(latestMusicAlbums[i].Summary) ? Translation.NoDescription : latestMusicAlbums[i].Summary);
          string artistbiooutline = Utils.GetSentences(artistbio, Utils.latestPlotOutlineSentencesNum);
          Utils.SetProperty("#latestMediaHandler.music.selected.artistbio", artistbio);
          Utils.SetProperty("#latestMediaHandler.music.selected.artistbiooutline", artistbiooutline);
          Utils.SetProperty("#latestMediaHandler.music.selected.year", latestMusicAlbums[i].SeriesIndex);
          Utils.SetProperty("#latestMediaHandler.music.selected.new", latestMusicAlbums[i].New);

          selectedFacadeItem1 = item.ItemId;

          facade = Utils.GetLatestsFacade(ControlID);
          if (facade != null)
            lastFocusedId = facade.SelectedListItemIndex;
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
              Utils.SetProperty("#latestMediaHandler.music.selected.fanart1", _image);
              Utils.SetProperty("#latestMediaHandler.music.selected.showfanart1", "true");
              Utils.SetProperty("#latestMediaHandler.music.selected.showfanart2", "false");
              Thread.Sleep(1000);
              Utils.SetProperty("#latestMediaHandler.music.selected.fanart2", "");
              showFanart = 2;
            }
            else
            {
              Utils.SetProperty("#latestMediaHandler.music.selected.fanart2", _image);
              Utils.SetProperty("#latestMediaHandler.music.selected.showfanart2", "true");
              Utils.SetProperty("#latestMediaHandler.music.selected.showfanart1", "false");
              Thread.Sleep(1000);
              Utils.SetProperty("#latestMediaHandler.music.selected.fanart1", "");
              showFanart = 1;
            }
            Utils.UnLoadImage(_image, ref images);
            selectedFacadeItem2 = _id;
          }
        }
        else
        {
          Utils.SetProperty("#latestMediaHandler.music.selected.fanart1", " ");
          Utils.SetProperty("#latestMediaHandler.music.selected.fanart2", " ");
          Utils.SetProperty("#latestMediaHandler.music.selected.showfanart1", "false");
          Utils.SetProperty("#latestMediaHandler.music.selected.showfanart2", "false");
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

    internal void SetupMusicLatest()
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

    internal void DisposeMusicLatest()
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
          if (LatestMediaHandlerSetup.LatestMusicType.Equals(MusicTypeMostPlayed, StringComparison.CurrentCulture) || 
              LatestMediaHandlerSetup.LatestMusicType.Equals(MusicTypeLatestPlayed, StringComparison.CurrentCulture))
            GetLatestMediaInfoThread() ;
      }
      catch (Exception ex)
      {
        logger.Error("OnPlayBackEnded: " + ex.ToString());
      }
    }

    private void OnMessage(GUIMessage message)
    {
      Utils.ThreadToSleep();
      if (LatestMediaHandlerSetup.LatestMusic.Equals("True", StringComparison.CurrentCulture))
      {
        bool Update = false;
        try
        {
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
        }
        catch { }
        //
        if (Update)
          GetLatestMediaInfoThread() ;
      }
    }

    internal void UpdateImageTimer(GUIWindow fWindow, Object stateInfo, ElapsedEventArgs e)
    {
      if (LatestMediaHandlerSetup.LatestMusic.Equals("True", StringComparison.CurrentCulture))
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
              Utils.SetProperty("#latestMediaHandler.music.selected.fanart1", " ");
              Utils.SetProperty("#latestMediaHandler.music.selected.fanart2", " ");
              Utils.UnLoadImage(ref images);
              ShowFanart = 1;
              SelectedFacadeItem2 = -1;
              SelectedFacadeItem2 = -1;
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
      /*
      string _songFolder = latestMusicAlbumsFolders[index].ToString().Trim();
       
      // if (Directory.Exists(_songFolder))
      if (!string.IsNullOrEmpty(_songFolder))
      {
        logger.Debug("PlayMusicAlbum: Try Play Album [" + index + "] from Folder(s): " + _songFolder);
        LoadSongsFromFolder(_songFolder, false);
        logger.Debug("PlayMusicAlbum: Try Play Album [" + index + "] from PlayList: " + playlistPlayer.GetPlaylist(MediaPortal.Playlists.PlayListType.PLAYLIST_MUSIC_TEMP).Count);
        StartPlayback();
      }
      else
        logger.Debug("PlayMusicAlbum: Folder for index [" + index + "] empty.");
      */
      string SongsFiles = latestMusicAlbumsFolders[index].ToString().Trim();
      if (!string.IsNullOrEmpty(SongsFiles))
      {
        LoadSongsFromList (SongsFiles);
        StartPlayback();
      }
    }

    private bool IsMusicFile(string fileName)
    {
      string supportedExtensions = MediaPortal.Util.Utils.AudioExtensionsDefault;
      // ".mp3,.wma,.ogg,.flac,.wav,.cda,.m4a,.m4p,.mp4,.wv,.ape,.mpc,.aif,.aiff";
      return (supportedExtensions.IndexOf(Path.GetExtension(fileName).ToLower()) > -1);
    }

    private void GetFiles(string folder, ref List<string> foundFiles, bool recursive)
    {
      if (!Directory.Exists(folder))
        return;

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
        string[] sFolders = folder.Split(Utils.PipesArray, StringSplitOptions.RemoveEmptyEntries);
        foreach (string sFolder in sFolders)
          GetFiles(sFolder, ref files, includeSubFolders);

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
        logger.Debug("LoadSongsFromFolder: Folder: " + folder + " Sub: " + Utils.Check(includeSubFolders) + " - " + numSongs);
      }
      catch //(Exception ex)
      {
        logger.Error("LoadSongsFromFolder: Error retrieving songs from folder.");
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
