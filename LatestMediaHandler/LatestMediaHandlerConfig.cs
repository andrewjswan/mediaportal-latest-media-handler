//***********************************************************************
// Assembly         : LatestMediaHandler
// Author           : cul8er
// Created          : 05-09-2010
//
// Last Modified By : cul8er
// Last Modified On : 10-05-2010
// Description      : 
//
// Copyright        : Open Source software licensed under the GNU/GPL agreement.
//***********************************************************************
extern alias RealNLog;
using System;
using System.Windows.Forms;
using MediaPortal.Configuration;
using System.IO;
using RealNLog.NLog;
using RealNLog.NLog.Config;
using RealNLog.NLog.Targets;
using MediaPortal.Services;


namespace LatestMediaHandler
{
  partial class LatestMediaHandlerConfig : Form
  {
    private static Logger logger = LogManager.GetCurrentClassLogger();
    private const string LogFileName = "LatestMediaHandler_config.log";
    private const string OldLogFileName = "LatestMediaHandler_config.old.log";
    private string latestPictures = null;
    private string latestMusic = null;
    private string latestMyVideos = null;
    private string latestMyVideosWatched = null;
    private string latestMovingPictures = null;
    private string latestMovingPicturesWatched = null;
    private string latestTVSeries = null;
    private string latestTVSeriesWatched = null;
    private string latestTVSeriesRatings = null;
    private string latestTVRecordings = null;
    private string latestTVRecordingsWatched = null;
    private string latestMyFilms = null;
    private string latestMyFilmsWatched = null;
    private string refreshDbPicture = null;
    private string refreshDbMusic = null;
    private string reorgInterval = null;
    private string latestMusicType = null;
    private string dateFormat = null;
    private string latestMvCentral = null;

    public LatestMediaHandlerConfig()
    {
      InitializeComponent();
    }


    private void SetupConfigFile()
    {
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

      using (
        MediaPortal.Profile.Settings xmlwriter =
          new MediaPortal.Profile.Settings(Config.GetFile(Config.Dir.Config, "LatestMediaHandler.xml")))
      {
        xmlwriter.SetValue("LatestMediaHandler", "latestPictures", checkBox5.Checked ? true : false);
        xmlwriter.SetValue("LatestMediaHandler", "latestMusic", checkBox6.Checked ? true : false);
        xmlwriter.SetValue("LatestMediaHandler", "latestMyVideos", checkBox9.Checked ? true : false);
        xmlwriter.SetValue("LatestMediaHandler", "latestMyVideosWatched", checkBox8.Checked ? true : false);
        xmlwriter.SetValue("LatestMediaHandler", "latestMovingPictures", checkBox7.Checked ? true : false);
        xmlwriter.SetValue("LatestMediaHandler", "latestMovingPicturesWatched", checkBox10.Checked ? true : false);
        xmlwriter.SetValue("LatestMediaHandler", "latestTVSeries", checkBox2.Checked ? true : false);
        xmlwriter.SetValue("LatestMediaHandler", "latestTVSeriesWatched", checkBox11.Checked ? true : false);
        xmlwriter.SetValue("LatestMediaHandler", "latestTVSeriesRatings", GetTVSeriesRatings());
        xmlwriter.SetValue("LatestMediaHandler", "latestTVRecordings", checkBox3.Checked ? true : false);
        xmlwriter.SetValue("LatestMediaHandler", "latestTVRecordingsWatched", checkBox14.Checked ? true : false);
        xmlwriter.SetValue("LatestMediaHandler", "latestMyFilms", checkBox1.Checked ? true : false);
        xmlwriter.SetValue("LatestMediaHandler", "latestMyFilmsWatched", checkBox4.Checked ? true : false);
        xmlwriter.SetValue("LatestMediaHandler", "refreshDbPicture", checkBox12.Checked ? true : false);
        xmlwriter.SetValue("LatestMediaHandler", "refreshDbMusic", checkBox13.Checked ? true : false);
        xmlwriter.SetValue("LatestMediaHandler", "reorgInterval", comboBox1.SelectedItem);
        xmlwriter.SetValue("LatestMediaHandler", "latestMusicType", comboBox2.SelectedItem);
        xmlwriter.SetValue("LatestMediaHandler", "dateFormat", comboBox3.SelectedItem);
        xmlwriter.SetValue("LatestMediaHandler", "latestMvCentral", checkBox15.Checked ? true : false);

      }
      MessageBox.Show(
        "Settings is stored in memory. Make sure to press Ok when exiting MP Configuration. Pressing Cancel when exiting MP Configuration will result in these setting NOT being saved!");

    }

    private void buttonSave_Click(object sender, EventArgs e)
    {
      DoSave();
    }

    private void LatestMediaHandlerConfig_FormClosing(object sender, FormClosedEventArgs e)
    {
      if (!DesignMode)
      {
        DialogResult result = MessageBox.Show("Do you want to save your changes?", "Save Changes?",
          MessageBoxButtons.YesNo);

        if (result == DialogResult.No)
        {
          //do nothing
        }

        if (result == DialogResult.Yes)
        {
          DoSave();
        }
        logger.Info("Latest Media Handler configuration is stopped.");
        this.Close();
      }
    }

    private string[] ParseTVSeriesRatings(string s)
    {
      try
      {
        string[] sl = s.Split(';');
        return sl;
      }
      catch (Exception ex)
      {
        logger.Error("ParseTVSeriesRatings: " + ex);
      }
      return null;
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

      SetupConfigFile();

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

      comboBox2.Items.Add("Latest Added Music");
      comboBox2.Items.Add("Latest Played Music");
      comboBox2.Items.Add("Most Played Music");

      checkedListBox1.Items.Add("TV-Y: This program is designed to be appropriate for all children");
      checkedListBox1.Items.Add("TV-Y7: This program is designed for children age 7 and above.");
      checkedListBox1.Items.Add("TV-G: Most parents would find this program suitable for all ages.");
      checkedListBox1.Items.Add(
        "TV-PG: This program contains material that parents may find unsuitable for younger children.");
      checkedListBox1.Items.Add(
        "TV-14: This program contains some material that many parents would find unsuitable for children under 14 years of age.");
      checkedListBox1.Items.Add(
        "TV-MA: This program is specifically designed to be viewed by adults and therefore may be unsuitable for children under 17.");

      using (
        MediaPortal.Profile.Settings xmlreader =
          new MediaPortal.Profile.Settings(Config.GetFile(Config.Dir.Config, "LatestMediaHandler.xml")))
      {
        latestPictures = xmlreader.GetValueAsString("LatestMediaHandler", "latestPictures", String.Empty);
        latestMusic = xmlreader.GetValueAsString("LatestMediaHandler", "latestMusic", String.Empty);
        latestMyVideos = xmlreader.GetValueAsString("LatestMediaHandler", "latestMyVideos", String.Empty);
        latestMyVideosWatched = xmlreader.GetValueAsString("LatestMediaHandler", "latestMyVideosWatched", String.Empty);
        latestMovingPictures = xmlreader.GetValueAsString("LatestMediaHandler", "latestMovingPictures", String.Empty);
        latestMovingPicturesWatched = xmlreader.GetValueAsString("LatestMediaHandler", "latestMovingPicturesWatched",
          String.Empty);
        latestTVSeries = xmlreader.GetValueAsString("LatestMediaHandler", "latestTVSeries", String.Empty);
        latestTVSeriesWatched = xmlreader.GetValueAsString("LatestMediaHandler", "latestTVSeriesWatched", String.Empty);
        latestTVSeriesRatings = xmlreader.GetValueAsString("LatestMediaHandler", "latestTVSeriesRatings", String.Empty);
        latestMyFilms = xmlreader.GetValueAsString("LatestMediaHandler", "latestMyFilms", String.Empty);
        latestMyFilmsWatched = xmlreader.GetValueAsString("LatestMediaHandler", "latestMyFilmsWatched", String.Empty);
        latestTVRecordings = xmlreader.GetValueAsString("LatestMediaHandler", "latestTVRecordings", String.Empty);
        latestTVRecordingsWatched = xmlreader.GetValueAsString("LatestMediaHandler", "latestTVRecordingsWatched",
          String.Empty);
        refreshDbPicture = xmlreader.GetValueAsString("LatestMediaHandler", "refreshDbPicture", String.Empty);
        refreshDbMusic = xmlreader.GetValueAsString("LatestMediaHandler", "refreshDbMusic", String.Empty);
        reorgInterval = xmlreader.GetValueAsString("LatestMediaHandler", "reorgInterval", String.Empty);
        //useLatestMediaCache = xmlreader.GetValueAsString("LatestMediaHandler", "useLatestMediaCache", String.Empty);
        latestMusicType = xmlreader.GetValueAsString("LatestMediaHandler", "latestMusicType", String.Empty);
        dateFormat = xmlreader.GetValueAsString("LatestMediaHandler", "dateFormat", String.Empty);
        latestMvCentral = xmlreader.GetValueAsString("LatestMediaHandler", "latestMvCentral", String.Empty);
      }


      if (latestTVSeriesRatings != null && latestTVSeriesRatings.Length > 0)
      {
        string[] s = ParseTVSeriesRatings(latestTVSeriesRatings);
        for (int i = 0; i < s.Length; i++)
        {
          if (s[i].Equals("1"))
          {
            checkedListBox1.SetItemChecked(i, true);
          }
          else
          {
            checkedListBox1.SetItemChecked(i, false);
          }
        }
      }
      else
      {
        for (int i = 0; i < checkedListBox1.Items.Count; i++)
        {
          checkedListBox1.SetItemChecked(i, true);
        }
      }

      if (dateFormat != null && dateFormat.Length > 0)
      {
        comboBox3.SelectedItem = dateFormat;
      }
      else
      {
        comboBox3.SelectedItem = "yyyy-MM-dd";
      }

      if (reorgInterval != null && reorgInterval.Length > 0)
      {
        comboBox1.SelectedItem = reorgInterval;
      }
      else
      {
        comboBox1.SelectedItem = "1440";
      }

      if (latestMusicType != null && latestMusicType.Length > 0)
      {
        comboBox2.SelectedItem = latestMusicType;
      }
      else
      {
        comboBox2.SelectedItem = "Latest Added Music";
      }

      if (latestMyFilms != null && latestMyFilms.Length > 0)
      {
        if (latestMyFilms.Equals("True", StringComparison.CurrentCulture))
          checkBox1.Checked = true;
        else
          checkBox1.Checked = false;
      }
      else
      {
        latestMyFilms = "True";
        checkBox1.Checked = true;
      }


      if (latestPictures != null && latestPictures.Length > 0)
      {
        if (latestPictures.Equals("True", StringComparison.CurrentCulture))
          checkBox5.Checked = true;
        else
          checkBox5.Checked = false;
      }
      else
      {
        latestPictures = "False";
        checkBox5.Checked = false;
      }

      /*          if (useLatestMediaCache != null && useLatestMediaCache.Length > 0)
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

      if (latestMusic != null && latestMusic.Length > 0)
      {
        if (latestMusic.Equals("True", StringComparison.CurrentCulture))
          checkBox6.Checked = true;
        else
          checkBox6.Checked = false;
      }
      else
      {
        latestMusic = "True";
        checkBox6.Checked = true;
      }

      if (latestMyVideos != null && latestMyVideos.Length > 0)
      {
        if (latestMyVideos.Equals("True", StringComparison.CurrentCulture))
          checkBox9.Checked = true;
        else
          checkBox9.Checked = false;
      }
      else
      {
        latestMyVideos = "True";
        checkBox9.Checked = true;
      }

      if (latestMvCentral != null && latestMvCentral.Length > 0)
      {
        if (latestMvCentral.Equals("True", StringComparison.CurrentCulture))
          checkBox15.Checked = true;
        else
          checkBox15.Checked = false;
      }
      else
      {
        latestMvCentral = "False";
        checkBox15.Checked = false;
      }

      if (latestMyVideos.Equals("True") && latestMyVideosWatched != null && latestMyVideosWatched.Length > 0)
      {
        if (latestMyVideosWatched.Equals("True", StringComparison.CurrentCulture))
          checkBox8.Checked = true;
        else
          checkBox8.Checked = false;
      }
      else
      {
        latestMyVideosWatched = "False";
        checkBox8.Checked = true;
      }

      if (latestMovingPictures != null && latestMovingPictures.Length > 0)
      {
        if (latestMovingPictures.Equals("True", StringComparison.CurrentCulture))
          checkBox7.Checked = true;
        else
          checkBox7.Checked = false;
      }
      else
      {
        latestMovingPictures = "True";
        checkBox7.Checked = true;
      }

      if (latestMovingPictures.Equals("True") && latestMovingPicturesWatched != null &&
          latestMovingPicturesWatched.Length > 0)
      {
        if (latestMovingPicturesWatched.Equals("True", StringComparison.CurrentCulture))
          checkBox10.Checked = true;
        else
          checkBox10.Checked = false;
      }
      else
      {
        latestMovingPicturesWatched = "False";
        checkBox10.Checked = true;
      }

      if (latestTVSeries != null && latestTVSeries.Length > 0)
      {
        if (latestTVSeries.Equals("True", StringComparison.CurrentCulture))
          checkBox2.Checked = true;
        else
          checkBox2.Checked = false;
      }
      else
      {
        latestTVSeries = "True";
        checkBox2.Checked = true;
      }

      if (latestTVSeries.Equals("True") && latestTVSeriesWatched != null && latestTVSeriesWatched.Length > 0)
      {
        if (latestTVSeriesWatched.Equals("True", StringComparison.CurrentCulture))
          checkBox11.Checked = true;
        else
          checkBox11.Checked = false;
      }
      else
      {
        latestTVSeriesWatched = "False";
        checkBox11.Checked = true;
      }

      if (refreshDbPicture != null && refreshDbPicture.Length > 0)
      {
        if (refreshDbPicture.Equals("True", StringComparison.CurrentCulture))
          checkBox12.Checked = true;
        else
          checkBox12.Checked = false;
      }
      else
      {
        refreshDbPicture = "False";
        checkBox12.Checked = false;
      }

      if (refreshDbMusic != null && refreshDbMusic.Length > 0)
      {
        if (refreshDbMusic.Equals("True", StringComparison.CurrentCulture))
          checkBox13.Checked = true;
        else
          checkBox13.Checked = false;
      }
      else
      {
        refreshDbMusic = "False";
        checkBox13.Checked = false;
      }


      if (latestTVRecordings != null && latestTVRecordings.Length > 0)
      {
        if (latestTVRecordings.Equals("True", StringComparison.CurrentCulture))
          checkBox3.Checked = true;
        else
          checkBox3.Checked = false;
      }
      else
      {
        latestTVRecordings = "True";
        checkBox3.Checked = true;
      }

      if (latestTVRecordings.Equals("True") && latestTVRecordingsWatched != null && latestTVRecordingsWatched.Length > 0)
      {
        if (latestTVRecordingsWatched.Equals("True", StringComparison.CurrentCulture))
          checkBox14.Checked = true;
        else
          checkBox14.Checked = false;
      }
      else
      {
        latestTVRecordingsWatched = "False";
        checkBox14.Checked = true;
      }

      if (latestMyFilms.Equals("True") && latestMyFilmsWatched != null && latestMyFilmsWatched.Length > 0)
      {
        if (latestMyFilmsWatched.Equals("True", StringComparison.CurrentCulture))
          checkBox4.Checked = true;
        else
          checkBox4.Checked = false;
      }
      else
      {
        latestMyFilmsWatched = "False";
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
      fileTarget.Layout = "${date:format=dd-MMM-yyyy HH\\:mm\\:ss} " +
                          "${level:fixedLength=true:padding=5} " +
                          "[${logger:fixedLength=true:padding=20:shortName=true}]: ${message} " +
                          "${exception:format=tostring}";

      config.AddTarget("file", fileTarget);

      // Get current Log Level from MediaPortal 
      LogLevel logLevel;
      MediaPortal.Profile.Settings xmlreader =
        new MediaPortal.Profile.Settings(Config.GetFile(Config.Dir.Config, "MediaPortal.xml"));

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
      reorgInterval = comboBox1.SelectedItem.ToString();
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
      latestMusicType = comboBox2.SelectedItem.ToString();
    }

    private void label3_Click(object sender, EventArgs e)
    {

    }

    private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
    {
      dateFormat = comboBox3.SelectedItem.ToString();
    }

    private void checkBox9_CheckedChanged(object sender, EventArgs e)
    {

    }
  }
}
