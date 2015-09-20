using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Text.RegularExpressions;
using MediaPortal.Configuration;
using MediaPortal.GUI.Library;

namespace LatestMediaHandler
{
  extern alias RealNLog;
  using RealNLog.NLog;

  internal class Translation
  {
    #region Private variables

    private static Logger logger = LogManager.GetCurrentClassLogger();

    private static Dictionary<string, string> _translations;
    private static Dictionary<string, string> DynamicTranslations = new Dictionary<string, string>();
    private static readonly string _path = string.Empty;
    private static readonly DateTimeFormatInfo _info;

    #endregion

    #region Constructor

    static Translation()
    {
      _info = DateTimeFormatInfo.GetInstance(CultureInfo.CurrentUICulture);
      _path = Config.GetSubFolder(Config.Dir.Language, "LatestMediaHandler");
    }

    #endregion

    #region Public Properties

    public static Dictionary<string, string> FixedTranslations = new Dictionary<string, string>();

    /// <summary>
    /// Gets the translated strings collection in the active language
    /// </summary>
    public static Dictionary<string, string> Strings
    {
      get
      {
        if (_translations == null)
        {
          _translations = new Dictionary<string, string>();
          Type transType = typeof (Translation);
          var fields = transType.GetFields(BindingFlags.Public | BindingFlags.Static).Where(p => p.FieldType == typeof (string));

          foreach (var field in fields)
          {
            if (DynamicTranslations.ContainsKey(field.Name))
            {
              if (field.GetValue(transType).ToString() != string.Empty)
                _translations.Add(field.Name + ":" + DynamicTranslations[field.Name],
                  field.GetValue(transType).ToString());
            }
            else
            {
              if (field.GetValue(transType).ToString() != string.Empty)
                _translations.Add(field.Name, field.GetValue(transType).ToString());
            }
          }
        }
        return _translations;
      }
    }

    #endregion

    #region Public Methods

    public static void Init()
    {
      // reset active translations
      _translations = null;
      FixedTranslations.Clear();

      string lang = string.Empty;
      try
      {
        lang = GUILocalizeStrings.GetCultureName(GUILocalizeStrings.CurrentLanguage());
      }
      catch (Exception)
      {
        lang = CultureInfo.CurrentUICulture.Name;
      }


      if (!System.IO.Directory.Exists(_path))
        System.IO.Directory.CreateDirectory(_path);

      LoadTranslations(lang);
    }

    public static int LoadTranslations(string lang)
    {
      XmlDocument doc = new XmlDocument();
      Dictionary<string, string> TranslatedStrings = new Dictionary<string, string>();
      string langPath = "";

      try
      {
        langPath = Path.Combine(_path, lang + ".xml");
        logger.Debug(string.Format("LatestMediaHandler Translation: Try load Translation file {0}.", langPath));
        doc.Load(langPath);
        logger.Info( string.Format("LatestMediaHandler Translation: Translation file loaded {0}.", langPath));
      }
      catch (Exception e)
      {
        if (lang == "en")
          return 0; // otherwise we are in an endless loop!

        if (e.GetType() == typeof (FileNotFoundException))
        {
          Log.Info( string.Format("LatestMediaHandler Translation: Cannot find translation file {0}.  Failing back to English", langPath));
          logger.Info( string.Format("LatestMediaHandler Translation: Cannot find translation file {0}.  Failing back to English", langPath));
        }
        else
        {
          Log.Info(string.Format("LatestMediaHandler Translation: Error in translation xml file: {0}. Failing back to English", lang));
          Log.Info("LatestMediaHandler Translation:" + e.ToString());
          logger.Info(string.Format("LatestMediaHandler Translation: Error in translation xml file: {0}. Failing back to English", lang));
          logger.Info("LatestMediaHandler Translation:" + e.ToString());
        }

        return LoadTranslations("en");
      }

      foreach (XmlNode stringEntry in doc.DocumentElement.ChildNodes)
      {
        if (stringEntry.NodeType == XmlNodeType.Element)
          try
          {
            if (stringEntry.Attributes.GetNamedItem("Field").Value.StartsWith("#"))
            {
              FixedTranslations.Add(stringEntry.Attributes.GetNamedItem("Field").Value, stringEntry.InnerText);
            }
            else
              TranslatedStrings.Add(stringEntry.Attributes.GetNamedItem("Field").Value, stringEntry.InnerText);
          }
          catch (Exception ex)
          {
            Log.Error("LatestMediaHandler Translation: Error in Translation Engine");
            Log.Error("LatestMediaHandler Translation:" + ex.ToString());
            logger.Error("LatestMediaHandler Translation: Error in Translation Engine");
            logger.Error("LatestMediaHandler Translation:" + ex.ToString());
          }
      }

      Type TransType = typeof (Translation);
      var fieldInfos = TransType.GetFields(BindingFlags.Public | BindingFlags.Static).Where(p => p.FieldType == typeof (string));

      foreach (var fi in fieldInfos)
      {
        if (TranslatedStrings != null && TranslatedStrings.ContainsKey(fi.Name))
          TransType.InvokeMember(fi.Name, BindingFlags.SetField, null, TransType,
            new object[] {TranslatedStrings[fi.Name]});
        else
        {
          // There is no hard-coded translation so create one
          Log.Info(string.Format("LatestMediaHandler Translation: Translation not found for field: {0}.  Using hard-coded English default.", fi.Name));
          logger.Info(string.Format("LatestMediaHandler Translation: Translation not found for field: {0}.  Using hard-coded English default.", fi.Name));
        }
      }
      return TranslatedStrings.Count;
    }

    public static string GetByName(string name)
    {
      if (!Strings.ContainsKey(name))
        return name;

      return Strings[name];
    }

    public static string GetByName(string name, params object[] args)
    {
      return String.Format(GetByName(name), args);
    }

    /// <summary>
    /// Takes an input string and replaces all ${named} variables with the proper translation if available
    /// </summary>
    /// <param name="input">a string containing ${named} variables that represent the translation keys</param>
    /// <returns>translated input string</returns>
    public static string ParseString(string input)
    {
      Regex replacements = new Regex(@"\$\{([^\}]+)\}");
      MatchCollection matches = replacements.Matches(input);
      foreach (Match match in matches)
      {
        input = input.Replace(match.Value, GetByName(match.Groups[1].Value));
      }
      return input;
    }

    #endregion

    #region Translations / Strings

    /// <summary>
    /// These will be loaded with the language files content
    /// if the selected lang file is not found, it will first try to load en(us).xml as a backup
    /// if that also fails it will use the hardcoded strings as a last resort.
    /// </summary>

    //Episodes
    public static string EpisodeDetails = "Episode Details";

    public static string DisplayNextEpisodes = "Display Next Episodes For Last Watched Episode";
    public static string DisplayLatestEpisodes = "Display Latest Added Episodes";
    public static string ShowUnwatchedEpisodes = "Show Only Unwatched Episodes";
    public static string ShowAllEpisodes = "Show All Episodes";

    //Movies
    public static string MovieDetails = "Movie Details";
    public static string ShowUnwatchedMovies = "Show Only Unwatched Movies";
    public static string ShowAllMovies = "Show All Movies";

    //Recordings
    public static string ShowUnwatchedRecordings = "Show Only Unwatched Recordings";
    public static string ShowAllRecordings = "Show All Recordings";

    //Music
    public static string ArtistDetails = "Artist Details";
    public static string AlbumDetails = "Album Details";
    public static string MostPlayedMusic = "Most Played Music";
    public static string LatestPlayedMusic = "Latest Played Music";
    public static string LatestAddedMusic = "Latest Added Music";

    //All
    public static string Play = "Play";

    //Label
    public static string LabelLatestAdded  = "Latest added";
    public static string LabelLatestPlayed = "Latest played";
    public static string LabelMostPlayed   = "Most played";

    //Settings
    public static string PrefsDateFormat = "Date format";
    public static string PrefsDescription = "Latest Media Handller 2 is a plugin for MediaPortal 1.10 (or later). The purpose of the plugin is to deliver information about latest\n* Pictures\n* Music Albums\n* Moving Pictures\n* TVSeries\n* Recordings\nto MediaPortal skins so that they can display things like latest movie title, poster and backdrops.";
    public static string PrefsLatestAddedMusic = "Latest Added Music";
    public static string PrefsLatestPlayedMusic = "Latest Played Music";
    public static string PrefsMostPlayedMusic = "Most Played Music";
    public static string PrefsLMHOptions = "Latest Media Handler Options";
    public static string PrefsLMHOptionsDesc = "Enable Latest Media Support for:";
    public static string PrefsMinutes = "minutes";
    public static string PrefsMiscOptions = "Misc Options";
    public static string PrefsMovingPictures = "Moving Pictures";
    public static string PrefsMovingPicturesWatched = "Only Return Unwatched Movies";
    public static string PrefsMusic = "Music";
    public static string PrefsMvCentral = "MvCentral";
    public static string PrefsMyFilms = "MyFilms";
    public static string PrefsMyFilmsWatched = "Only Return Unwatched Movies";
    public static string PrefsMyVideos = "MyVideos";
    public static string PrefsMyVideosWatched = "Only Return Unwatched Videos";
    public static string PrefsPictures = "Pictures";
    public static string PrefsRatingFilter = "Rating Filter";
    public static string PrefsRatingDesc = "Only Return The Following Content Ratings for supported Plugins (TV Series)";
    public static string PrefsRatingTV_Y = "TV-Y: This program is designed to be appropriate for all children";
    public static string PrefsRatingTV_Y7 = "TV-Y7: This program is designed for children age 7 and above.";
    public static string PrefsRatingTV_G = "TV-G: Most parents would find this program suitable for all ages.";
    public static string PrefsRatingTV_PG = "TV-PG: This program contains material that parents may find unsuitable for younger children.";
    public static string PrefsRatingTV_14 = "TV-14: This program contains some material that many parents would find unsuitable for children under 14 years of age.";
    public static string PrefsRatingTV_MA = "TV-MA: This program is specifically designed to be viewed by adults and therefore may be unsuitable for children under 17.";
    public static string PrefsRecordings = "TV Recordings";
    public static string PrefsRecordingsWatched = "Only Return Unwatched Recordings";
    public static string PrefsRecordingsUnfinished = "Return Unfinished Recordings";
    public static string PrefsRefreshInterval = "Refresh Interval";
    public static string PrefsSaveChanges = "Save changes?";
    public static string PrefsSaveChangesMsgBox = "Settings are stored in memory. Make sure to press Ok when exiting MP Configuration. Pressing Cancel when exiting MP Configuration will result in these setting NOT being saved!";
    public static string PrefsSaveChangesDialog = "Do you want to save your changes?";
    public static string PrefsTabAbout = "About";
    public static string PrefsTabDB = "Database";
    public static string PrefsTabLMH = "Latest Media";
    public static string PrefsTabMisc = "Misc";
    public static string PrefsTabRatings = "Ratings";
    public static string PrefsTVSeries = "TV Series";
    public static string PrefsTVSeriesWatched = "Only Return Unwatched Series";
    public static string PrefsUpdateDB = "Query/Update Databases";
    public static string PrefsUpdateDBDesc = "Enable automatic database updates for:";
    #endregion
  }
}
