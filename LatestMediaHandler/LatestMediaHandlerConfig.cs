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
extern alias RealNLog;

using System;
using System.IO;
using System.Windows.Forms;

using RealNLog.NLog;
using RealNLog.NLog.Config;
using RealNLog.NLog.Targets;

using MediaPortal.Services;
using MediaPortal.Configuration;

namespace LatestMediaHandler
{
  partial class LatestMediaHandlerConfig : Form
  {
    private static Logger logger = LogManager.GetCurrentClassLogger();
    private const string LogFileName = "LatestMediaHandler_config.log";
    private const string OldLogFileName = "LatestMediaHandler_config.bak";

    private string DateFormat
    {
      get { return Utils.dateFormat; }
      set { Utils.dateFormat = value; }
    }

    private string ReorgInterval
    {
      get { return Utils.reorgInterval; }
      set { Utils.reorgInterval = value; }
    }

    private string RefreshDbPicture
    {
      get { return Utils.refreshDbPicture; }
      set { Utils.refreshDbPicture = value; }
    }

    private string RefreshDbMusic
    {
      get { return Utils.refreshDbMusic; }
      set { Utils.refreshDbMusic = value; }
    }

    private string LatestTVRecordings
    {
      get { return Utils.latestTVRecordings; }
      set { Utils.latestTVRecordings = value; }
    }

    private string LatestTVRecordingsWatched
    {
      get { return Utils.latestTVRecordingsWatched; }
      set { Utils.latestTVRecordingsWatched = value; }
    }

    private string LatestTVRecordingsUnfinished
    {
      get { return Utils.latestTVRecordingsUnfinished; }
      set { Utils.latestTVRecordingsUnfinished = value; }
    }

    private string LatestTVSeries
    {
      get { return Utils.latestTVSeries; }
      set { Utils.latestTVSeries = value; }
    }

    private string LatestTVSeriesWatched
    {
      get { return Utils.latestTVSeriesWatched; }
      set { Utils.latestTVSeriesWatched = value; }
    }

    private int LatestTVSeriesType
    {
      get { return Utils.latestTVSeriesType; }
      set { Utils.latestTVSeriesType = value; }
    }

    private string LatestTVSeriesRatings
    {
      get { return Utils.latestTVSeriesRatings; }
      set { Utils.latestTVSeriesRatings = value; }
    }

    private string LatestMyVideos
    {
      get { return Utils.latestMyVideos; }
      set { Utils.latestMyVideos = value; }
    }

    private string LatestMyVideosWatched
    {
      get { return Utils.latestMyVideosWatched; }
      set { Utils.latestMyVideosWatched = value; }
    }

    private string LatestMvCentral
    {
      get { return Utils.latestMvCentral; }
      set { Utils.latestMvCentral = value; }
    }

    private int LatestMvCentralThumbType
    {
      get { return Utils.latestMvCentralThumbType; }
      set { Utils.latestMvCentralThumbType = value; }
    }

    private string LatestMovingPictures
    {
      get { return Utils.latestMovingPictures; }
      set { Utils.latestMovingPictures = value; }
    }

    private string LatestMovingPicturesWatched
    {
      get { return Utils.latestMovingPicturesWatched; }
      set { Utils.latestMovingPicturesWatched = value; }
    }

    private string LatestMusic
    {
      get { return Utils.latestMusic; }
      set { Utils.latestMusic = value; }
    }

    private string LatestMusicType
    {
      get { return Utils.latestMusicType; }
      set { Utils.latestMusicType = value; }
    }

    private string LatestPictures
    {
      get { return Utils.latestPictures; }
      set { Utils.latestPictures = value; }
    }

    private string LatestMyFilms
    {
      get { return Utils.latestMyFilms; }
      set { Utils.latestMyFilms = value; }
    }

    private string LatestMyFilmsWatched
    {
      get { return Utils.latestMyFilmsWatched; }
      set { Utils.latestMyFilmsWatched = value; }
    }

    public LatestMediaHandlerConfig()
    {
      InitializeComponent();
    }


    private void SetupConfigFile()
    {
      /*
      try
      {
        String path = Config.GetFile(Config.Dir.Config, "LatestMediaHandler.xml");
        String pathOrg = Config.GetFile(Config.Dir.Config, "LatestMediaHandler.org");
        if (File.Exists(path))
        {
          //do nothing
        }
        else
        {
          File.Copy(pathOrg, path);
        }
      }
      catch (Exception ex)
      {
        logger.Error("setupConfigFile: " + ex);
      }
      */
    }

    private string GetTVSeriesRatings()
    {
      string s = string.Empty;
      for (int i = 0; i < checkedListBox1.Items.Count; i++)
      {
        string isChecked = string.Empty;
        if (checkedListBox1.GetItemChecked(i))
        {
          isChecked = "1";
        }
        else
        {
          isChecked = "0";
        }

        if (i == 0)
        {
          s = isChecked;
        }
        else
        {
          s = s + ";" + isChecked;
        }
      }
      return s;
    }

    private void DoSave()
    {
      try
      {
        LatestPictures = checkBox5.Checked ? "True" : "False";
        LatestMusic = checkBox6.Checked ? "True" : "False";
        LatestMyVideos = checkBox9.Checked ? "True" : "False";
        LatestMyVideosWatched = checkBox8.Checked ? "True" : "False";
        LatestMovingPictures = checkBox7.Checked ? "True" : "False";
        LatestMovingPicturesWatched = checkBox10.Checked ? "True" : "False";
        LatestTVSeries = checkBox2.Checked ? "True" : "False";
        LatestTVSeriesWatched = checkBox11.Checked ? "True" : "False";
        LatestTVSeriesRatings = GetTVSeriesRatings();
        LatestTVSeriesType = comboBoxTVSeriesType.SelectedIndex;
        LatestTVRecordings = checkBox3.Checked ? "True" : "False";
        LatestTVRecordingsWatched = checkBox14.Checked ? "True" : "False";
        LatestTVRecordingsUnfinished = checkBoxRecordingsUnfinished.Checked ? "True" : "False";
        LatestMyFilms = checkBox1.Checked ? "True" : "False";
        LatestMyFilmsWatched = checkBox4.Checked ? "True" : "False";
        RefreshDbPicture = checkBox12.Checked ? "True" : "False";
        RefreshDbMusic = checkBox13.Checked ? "True" : "False";
        LatestMvCentral = checkBox15.Checked ? "True" : "False";
        LatestMvCentralThumbType = comboBoxMvCThumbType.SelectedIndex + 1;
      }
      catch (Exception ex)
      {
        logger.Error("DoSave: " + ex);
      }
      try
      {
        ReorgInterval = comboBox1.SelectedItem.ToString();
        LatestMusicType = comboBox2.SelectedItem.ToString();
        DateFormat = comboBox3.SelectedItem.ToString();
      }
      catch (Exception ex)
      {
        logger.Error("DoSave: " + ex);
      }

      if (LatestMusicType == Translation.PrefsMostPlayedMusic)
        LatestMusicType = LatestMusicHandler.MusicTypeMostPlayed;
      else if (LatestMusicType == Translation.PrefsLatestPlayedMusic)
        LatestMusicType = LatestMusicHandler.MusicTypeLatestPlayed;
      else // if (LatestMusicType == Translation.PrefsLatestAddedMusic)
        LatestMusicType = LatestMusicHandler.MusicTypeLatestAdded;

      Utils.SaveSettings();

      // MessageBox.Show("Settings is stored in memory. Make sure to press Ok when exiting MP Configuration. Pressing Cancel when exiting MP Configuration will result in these setting NOT being saved!");
      MessageBox.Show(Translation.PrefsSaveChangesMsgBox);
    }

    private void buttonSave_Click(object sender, EventArgs e)
    {
      DoSave();
    }

    private void LatestMediaHandlerConfig_FormClosing(object sender, FormClosedEventArgs e)
    {
      if (!DesignMode)
      {
        // DialogResult result = MessageBox.Show("Do you want to save your changes?", 
        //                                       "Save Changes?",
        //                                       MessageBoxButtons.YesNo);
        DialogResult result = MessageBox.Show(Translation.PrefsSaveChangesDialog, 
                                              Translation.PrefsSaveChanges,
                                              MessageBoxButtons.YesNo);
        if (result == DialogResult.Yes)      
        {
          DoSave();
        }
        logger.Info("Latest Media Handler configuration is stopped.");
        this.Close();
      }
    }

    private void LatestMediaHandlerConfig_WindowInit()
    {
      comboBox1.Items.Add("30");
      comboBox1.Items.Add("60");
      comboBox1.Items.Add("120");
      comboBox1.Items.Add("240");
      comboBox1.Items.Add("480");
      comboBox1.Items.Add("720");
      comboBox1.Items.Add("1440");

      comboBox3.Items.Add("yyyy-MM-dd");
      comboBox3.Items.Add("dd.MM.yyyy");
      comboBox3.Items.Add("MM/dd/yyyy");
      comboBox3.Items.Add("dd/MM/yyyy");
      comboBox3.Items.Add("MM/dd/yy");
      comboBox3.Items.Add("dd/MM/yy");

      /*
      comboBox2.Items.Add("Latest Added Music");
      comboBox2.Items.Add("Latest Played Music");
      comboBox2.Items.Add("Most Played Music");
      */
      comboBox2.Items.Add(Translation.PrefsLatestAddedMusic);
      comboBox2.Items.Add(Translation.PrefsLatestPlayedMusic);
      comboBox2.Items.Add(Translation.PrefsMostPlayedMusic);

      comboBoxTVSeriesType.Items.Add(Translation.PrefsLatestEpisodes);
      comboBoxTVSeriesType.Items.Add(Translation.PrefsLatestSeasons);
      comboBoxTVSeriesType.Items.Add(Translation.PrefsLatestSeries);

      comboBoxMvCThumbType.Items.Add(Translation.PrefsMvCThumbArtist);
      comboBoxMvCThumbType.Items.Add(Translation.PrefsMvCThumbAlbum);
      comboBoxMvCThumbType.Items.Add(Translation.PrefsMvCThumbTrack);
      /*
      checkedListBox1.Items.Add("TV-Y: This program is designed to be appropriate for all children");
      checkedListBox1.Items.Add("TV-Y7: This program is designed for children age 7 and above.");
      checkedListBox1.Items.Add("TV-G: Most parents would find this program suitable for all ages.");
      checkedListBox1.Items.Add("TV-PG: This program contains material that parents may find unsuitable for younger children.");
      checkedListBox1.Items.Add("TV-14: This program contains some material that many parents would find unsuitable for children under 14 years of age.");
      checkedListBox1.Items.Add("TV-MA: This program is specifically designed to be viewed by adults and therefore may be unsuitable for children under 17.");
      */
      label3.Text =  Translation.PrefsRatingDesc;

      checkedListBox1.Items.Add(Translation.PrefsRatingTV_Y);
      checkedListBox1.Items.Add(Translation.PrefsRatingTV_Y7);
      checkedListBox1.Items.Add(Translation.PrefsRatingTV_G);
      checkedListBox1.Items.Add(Translation.PrefsRatingTV_PG);
      checkedListBox1.Items.Add(Translation.PrefsRatingTV_14);
      checkedListBox1.Items.Add(Translation.PrefsRatingTV_MA);
      // 
      tabPage20.Text = Translation.PrefsTabLMH;
      tabPage4.Text = Translation.PrefsTabAbout;
      richTextBox1.AppendText(Translation.PrefsDescription.Replace("\r\n", Environment.NewLine).Replace("\n", Environment.NewLine));
      //
      groupBox11.Text = Translation.PrefsLMHOptions ;
      groupBox13.Text = Translation.PrefsUpdateDB;
      groupBox1.Text = Translation.PrefsMiscOptions;
      //
      label31.Text = Translation.PrefsLMHOptionsDesc;
      label4.Text = Translation.PrefsDateFormat;
      label2.Text =  Translation.PrefsMinutes;
      checkBox7.Text =  Translation.PrefsMovingPictures;
      checkBox4.Text =  Translation.PrefsMovingPicturesWatched;
      checkBox6.Text =  Translation.PrefsMusic;
      checkBox13.Text =  Translation.PrefsMusic;
      checkBox15.Text =  Translation.PrefsMvCentral;
      checkBox1.Text =  Translation.PrefsMyFilms;
      checkBox4.Text =  Translation.PrefsMyFilmsWatched;
      checkBox9.Text =  Translation.PrefsMyVideos;
      checkBox8.Text =  Translation.PrefsMyVideosWatched;
      checkBox12.Text =  Translation.PrefsPictures;
      checkBox5.Text =  Translation.PrefsPictures;
      checkBox3.Text =  Translation.PrefsRecordings;
      checkBox14.Text =  Translation.PrefsRecordingsWatched;
      checkBoxRecordingsUnfinished.Text = Translation.PrefsRecordingsUnfinished;
      label1.Text =  Translation.PrefsRefreshInterval;
      checkBox2.Text =  Translation.PrefsTVSeries;
      checkBox11.Text =  Translation.PrefsTVSeriesWatched;
      label35.Text =  Translation.PrefsUpdateDBDesc;
    }

    private void LatestMediaHandlerConfig_Load(object sender, EventArgs e)
    {
      try
      {
        InitLogger();
        logger.Info("Latest Media Handler configuration is starting.");
        logger.Info("Latest Media Handler version is " + Utils.GetAllVersionNumber());
      }
      catch (Exception ex)
      {
        logger.Error("LatestMediaHandlerConfig_Load: " + ex);
      }

      label11.Text = "Version " + Utils.GetAllVersionNumber();
      Text = Text + " v" + Utils.GetAllVersionNumber();

      Translation.Init();
      LatestMediaHandlerConfig_WindowInit();

      SetupConfigFile();
      Utils.LoadSettings(true);

      if (!string.IsNullOrEmpty(LatestTVSeriesRatings))
      {
        string[] s = LatestTVSeriesRatings.Split(';');
        for (int i = 0; i < s.Length; i++)
        {
          checkedListBox1.SetItemChecked(i, s[i].Equals("1"));
        }
      }
      else
      {
        for (int i = 0; i < checkedListBox1.Items.Count; i++)
        {
          checkedListBox1.SetItemChecked(i, true);
        }
      }

      if (!string.IsNullOrEmpty(DateFormat))
      {
        comboBox3.SelectedItem = DateFormat;
      }
      else
      {
        comboBox3.SelectedItem = "yyyy-MM-dd";
      }

      if (!string.IsNullOrEmpty(ReorgInterval))
      {
        comboBox1.SelectedItem = ReorgInterval;
      }
      else
      {
        comboBox1.SelectedItem = "1440";
      }

      if (!string.IsNullOrEmpty(LatestMusicType))
      {
        comboBox2.SelectedItem = LatestMusicType;
      }
      else
      {
        comboBox2.SelectedItem = Translation.PrefsLatestAddedMusic;
      }

      if (!string.IsNullOrEmpty(LatestMyFilms))
      {
        if (LatestMyFilms.Equals("True", StringComparison.CurrentCulture))
          checkBox1.Checked = true;
        else
          checkBox1.Checked = false;
      }
      else
      {
        LatestMyFilms = "True";
        checkBox1.Checked = true;
      }

      if (!string.IsNullOrEmpty(LatestPictures))
      {
        if (LatestPictures.Equals("True", StringComparison.CurrentCulture))
          checkBox5.Checked = true;
        else
          checkBox5.Checked = false;
      }
      else
      {
        LatestPictures = "False";
        checkBox5.Checked = false;
      }

      /*          if (!string.IsNullOrEmpty(useLatestMediaCache))
            {
                if (useLatestMediaCache.Equals("True", StringComparison.CurrentCulture))
                    checkBox16.Checked = true;
                else
                    checkBox16.Checked = false;
            }
            else
            {
                useLatestMediaCache = "True";
                checkBox16.Checked = true;
            }
*/

      if (!string.IsNullOrEmpty(LatestMusic))
      {
        if (LatestMusic.Equals("True", StringComparison.CurrentCulture))
          checkBox6.Checked = true;
        else
          checkBox6.Checked = false;
      }
      else
      {
        LatestMusic = "True";
        checkBox6.Checked = true;
      }

      if (!string.IsNullOrEmpty(LatestMyVideos))
      {
        if (LatestMyVideos.Equals("True", StringComparison.CurrentCulture))
          checkBox9.Checked = true;
        else
          checkBox9.Checked = false;
      }
      else
      {
        LatestMyVideos = "True";
        checkBox9.Checked = true;
      }

      if (!string.IsNullOrEmpty(LatestMvCentral))
      {
        if (LatestMvCentral.Equals("True", StringComparison.CurrentCulture))
          checkBox15.Checked = true;
        else
          checkBox15.Checked = false;
      }
      else
      {
        LatestMvCentral = "False";
        checkBox15.Checked = false;
      }

      if (LatestMvCentralThumbType > 0 && LatestMvCentralThumbType <= 3)
      {
        comboBoxMvCThumbType.SelectedIndex = LatestMvCentralThumbType - 1;
      }
      else
      {
        comboBoxMvCThumbType.SelectedIndex = 0;
      }

      if (LatestMyVideos.Equals("True", StringComparison.CurrentCulture) && !string.IsNullOrEmpty(LatestMyVideosWatched))
      {
        if (LatestMyVideosWatched.Equals("True", StringComparison.CurrentCulture))
          checkBox8.Checked = true;
        else
          checkBox8.Checked = false;
      }
      else
      {
        LatestMyVideosWatched = "False";
        checkBox8.Checked = true;
      }

      if (!string.IsNullOrEmpty(LatestMovingPictures))
      {
        if (LatestMovingPictures.Equals("True", StringComparison.CurrentCulture))
          checkBox7.Checked = true;
        else
          checkBox7.Checked = false;
      }
      else
      {
        LatestMovingPictures = "True";
        checkBox7.Checked = true;
      }

      if (LatestMovingPictures.Equals("True", StringComparison.CurrentCulture) && !string.IsNullOrEmpty(LatestMovingPicturesWatched))
      {
        if (LatestMovingPicturesWatched.Equals("True", StringComparison.CurrentCulture))
          checkBox10.Checked = true;
        else
          checkBox10.Checked = false;
      }
      else
      {
        LatestMovingPicturesWatched = "False";
        checkBox10.Checked = true;
      }

      if (!string.IsNullOrEmpty(LatestTVSeries))
      {
        if (LatestTVSeries.Equals("True", StringComparison.CurrentCulture))
          checkBox2.Checked = true;
        else
          checkBox2.Checked = false;
      }
      else
      {
        LatestTVSeries = "True";
        checkBox2.Checked = true;
      }

      if (LatestTVSeries.Equals("True", StringComparison.CurrentCulture) && !string.IsNullOrEmpty(LatestTVSeriesWatched))
      {
        if (LatestTVSeriesWatched.Equals("True", StringComparison.CurrentCulture))
          checkBox11.Checked = true;
        else
          checkBox11.Checked = false;
      }
      else
      {
        LatestTVSeriesWatched = "False";
        checkBox11.Checked = true;
      }

      comboBoxTVSeriesType.SelectedIndex = LatestTVSeriesType;

      if (!string.IsNullOrEmpty(RefreshDbPicture))
      {
        if (RefreshDbPicture.Equals("True", StringComparison.CurrentCulture))
          checkBox12.Checked = true;
        else
          checkBox12.Checked = false;
      }
      else
      {
        RefreshDbPicture = "False";
        checkBox12.Checked = false;
      }

      if (!string.IsNullOrEmpty(RefreshDbMusic))
      {
        if (RefreshDbMusic.Equals("True", StringComparison.CurrentCulture))
          checkBox13.Checked = true;
        else
          checkBox13.Checked = false;
      }
      else
      {
        RefreshDbMusic = "False";
        checkBox13.Checked = false;
      }

      if (!string.IsNullOrEmpty(LatestTVRecordings))
      {
        if (LatestTVRecordings.Equals("True", StringComparison.CurrentCulture))
          checkBox3.Checked = true;
        else
          checkBox3.Checked = false;
      }
      else
      {
        LatestTVRecordings = "True";
        checkBox3.Checked = true;
      }

      if (LatestTVRecordings.Equals("True", StringComparison.CurrentCulture) && !string.IsNullOrEmpty(LatestTVRecordingsWatched))
      {
        checkBox14.Checked = LatestTVRecordingsWatched.Equals("True", StringComparison.CurrentCulture) ;
      }
      else
      {
        LatestTVRecordingsWatched = "False";
        checkBox14.Checked = true;
      }

      if (LatestTVRecordings.Equals("True", StringComparison.CurrentCulture) && !string.IsNullOrEmpty(LatestTVRecordingsUnfinished))
      {
        checkBoxRecordingsUnfinished.Checked = LatestTVRecordingsUnfinished.Equals("True", StringComparison.CurrentCulture);
      }
      else
      {
        LatestTVRecordingsUnfinished = "True";
        checkBoxRecordingsUnfinished.Checked = true;
      }

      if (LatestMyFilms.Equals("True", StringComparison.CurrentCulture) && !string.IsNullOrEmpty(LatestMyFilmsWatched))
      {
        if (LatestMyFilmsWatched.Equals("True", StringComparison.CurrentCulture))
          checkBox4.Checked = true;
        else
          checkBox4.Checked = false;
      }
      else
      {
        LatestMyFilmsWatched = "False";
        checkBox4.Checked = true;
      }

      try
      {
        logger.Info("Latest Media Handler configuration is started.");
      }
      catch (Exception ex)
      {
        logger.Error("LatestMediaHandlerConfig_Load: " + ex);
      }

    }

    /// <summary>
    /// Setup logger. This funtion made by the team behind Moving Pictures 
    /// (http://code.google.com/p/moving-pictures/)
    /// </summary>
    private void InitLogger()
    {
      //LoggingConfiguration config = new LoggingConfiguration();
      LoggingConfiguration config = LogManager.Configuration ?? new LoggingConfiguration();

      try
      {
        FileInfo logFile = new FileInfo(Config.GetFile(Config.Dir.Log, LogFileName));
        if (logFile.Exists)
        {
          if (File.Exists(Config.GetFile(Config.Dir.Log, OldLogFileName)))
            File.Delete(Config.GetFile(Config.Dir.Log, OldLogFileName));

          logFile.CopyTo(Config.GetFile(Config.Dir.Log, OldLogFileName));
          logFile.Delete();
        }
      }
      catch (Exception)
      {
      }

      FileTarget fileTarget = new FileTarget();
      fileTarget.FileName = Config.GetFile(Config.Dir.Log, LogFileName);
      fileTarget.Encoding = "utf-8";
      fileTarget.Layout = "${date:format=dd-MMM-yyyy HH\\:mm\\:ss} " +
                          "${level:fixedLength=true:padding=5} " +
                          "[${logger:fixedLength=true:padding=20:shortName=true}]: ${message} " +
                          "${exception:format=tostring}";

      config.AddTarget("latestmedia-handler", fileTarget);

      // Get current Log Level from MediaPortal 
      LogLevel logLevel;
      MediaPortal.Profile.Settings xmlreader = new MediaPortal.Profile.Settings(Config.GetFile(Config.Dir.Config, "MediaPortal.xml"));

      switch ((Level) xmlreader.GetValueAsInt("general", "loglevel", 0))
      {
        case Level.Error:
          logLevel = LogLevel.Error;
          break;
        case Level.Warning:
          logLevel = LogLevel.Warn;
          break;
        case Level.Information:
          logLevel = LogLevel.Info;
          break;
        case Level.Debug:
        default:
          logLevel = LogLevel.Debug;
          break;
      }

#if DEBUG
            logLevel = LogLevel.Debug;
#endif

      LoggingRule rule = new LoggingRule("*", logLevel, fileTarget);
      config.LoggingRules.Add(rule);

      LogManager.Configuration = config;
    }

    private void checkBox3_CheckedChanged(object sender, EventArgs e)
    {
      if (checkBox3.Checked)
      {
        checkBox14.Enabled = true;
      }
      else
      {
        checkBox14.Enabled = false;
      }
    }

    private void checkBox7_CheckedChanged(object sender, EventArgs e)
    {
      if (checkBox7.Checked)
      {
        checkBox10.Enabled = true;
      }
      else
      {
        checkBox10.Enabled = false;
      }
    }

    private void checkBox2_CheckedChanged(object sender, EventArgs e)
    {
      if (checkBox2.Checked)
      {
        checkBox11.Enabled = true;
      }
      else
      {
        checkBox11.Enabled = false;
      }
    }

    private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
    {
      ReorgInterval = comboBox1.SelectedItem.ToString();
    }

    private void checkBox13_CheckedChanged(object sender, EventArgs e)
    {

    }

    private void groupBox1_Enter(object sender, EventArgs e)
    {

    }


    private void checkBox1_CheckedChanged(object sender, EventArgs e)
    {
      if (checkBox1.Checked)
      {
        checkBox4.Enabled = true;
      }
      else
      {
        checkBox4.Enabled = false;
      }
    }

    private void checkBox5_CheckedChanged(object sender, EventArgs e)
    {

    }

    private void checkBox6_CheckedChanged(object sender, EventArgs e)
    {

    }

    private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
    {
      LatestMusicType = comboBox2.SelectedItem.ToString();
    }

    private void label3_Click(object sender, EventArgs e)
    {

    }

    private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
    {
      DateFormat = comboBox3.SelectedItem.ToString();
    }

    private void checkBox9_CheckedChanged(object sender, EventArgs e)
    {

    }
  }
}
