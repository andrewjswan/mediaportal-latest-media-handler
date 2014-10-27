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

namespace LatestMediaHandler
{
  extern alias RealNLog;
  using MediaPortal.GUI.Library;
  using RealNLog.NLog;
  using System;
  using System.Collections;
  using System.Drawing;
  using System.IO;
  using System.Runtime.InteropServices;
  using System.Reflection;


    /// <summary>
  /// Utility class used by the Latest Media Handler plugin.
  /// </summary>
  internal static class Utils
  {
    #region declarations

    private static Logger logger = LogManager.GetCurrentClassLogger();
    private const string RXMatchNonWordCharacters = @"[^\w|;]";
    private const string RXMatchMPvs = @"({)([0-9]+)(})$"; // MyVideos fanart scraper filename index
    private const string RXMatchMPvs2 = @"(\()([0-9]+)(\))$"; // MyVideos fanart scraper filename index
    public const string GetMajorMinorVersionNumber = "1.6.2.0"; //Holds current pluginversion.
    private static bool isStopping /* = false*/; //is the plugin about to stop, then this will be true
    private static Hashtable delayStop = null;
    private static bool used4TRTV = false;
    private static bool usedArgus = false;
    private static DateTime lastRefreshRecording;

    internal static DateTime LastRefreshRecording
    {
      get { return Utils.lastRefreshRecording; }
      set { Utils.lastRefreshRecording = value; }
    }

    internal static bool Used4TRTV
    {
      get { return Utils.used4TRTV; }
      set { Utils.used4TRTV = value; }
    }

    internal static bool UsedArgus
    {
      get { return Utils.usedArgus; }
      set { Utils.usedArgus = value; }
    }

    #endregion

    /// <summary>
    /// Return value.
    /// </summary>


    internal static Hashtable DelayStop
    {
      get { return Utils.delayStop; }
      set { Utils.delayStop = value; }
    }

    internal static string RemoveLeadingZeros(string s)
    {
      if (s != null)
      {
        char[] charsToTrim = {'0'};
        s = s.TrimStart(charsToTrim);
      }
      return s;
    }

    internal static bool GetDelayStop()
    {
      if (DelayStop.Count == 0)
      {
        return false;
      }
      else
      {
        int i = 0;
        foreach (DictionaryEntry de in DelayStop)
        {
          logger.Debug("DelayStop (" + i + "):" + de.Key.ToString());
          i++;
        }
        return true;
      }
    }

    internal static void LogDevMsg(string msg)
    {
      logger.Debug("DEV MSG: " + msg);
    }

    internal static void AllocateDelayStop(string key)
    {
      if (DelayStop.Contains(key))
      {
        DelayStop[key] = "1";
      }
      else
      {
        DelayStop.Add(key, "1");
      }
    }

    internal static void ReleaseDelayStop(string key)
    {
      if (DelayStop.Contains(key))
      {
        DelayStop.Remove(key);
      }
    }


    /// <summary>
    /// Return value.
    /// </summary>
    internal static bool GetIsStopping()
    {
      return isStopping;
    }

    /// <summary>
    /// Set value.
    /// </summary>
    internal static void SetIsStopping(bool b)
    {
      isStopping = b;
    }

    /// <summary>
    /// Returns plugin version.
    /// </summary>
    internal static string GetAllVersionNumber()
    {
      return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
    }

    internal static bool ShouldRefreshRecording()
    {
      try
      {
        TimeSpan ts = DateTime.Now - LastRefreshRecording;
        if (ts.TotalMilliseconds >= 300000)
        {
          return true;
        }
      }
      catch (Exception ex)
      {
        logger.Error("ShouldRefreshRecording: " + ex.ToString());
      }
      return false;
    }

    /// <summary>
    /// Load image
    /// </summary>
    internal static void LoadImage(string filename)
    {
      if (isStopping == false)
      {
        try
        {
          if (filename != null && filename.Length > 0)
          {
            GUITextureManager.Load(filename, 0, 0, 0, true);
          }
        }
        catch (Exception ex)
        {
          if (isStopping == false)
          {
            logger.Error("LoadImage (" + filename + "): " + ex.ToString());
          }

        }
      }
    }

    internal static void UNLoadImage(string name)
    {
      try
      {
        GUITextureManager.ReleaseTexture(name);
      }
      catch (Exception ex)
      {
        logger.Error("UnLoadImage: " + ex.ToString());
      }
    }

    [DllImport("gdiplus.dll", CharSet = CharSet.Unicode)]
    private static extern int GdipLoadImageFromFile(string filename, out IntPtr image);

    // Loads an Image from a File by invoking GDI Plus instead of using build-in 
    // .NET methods, or falls back to Image.FromFile. GDI Plus should be faster.
    //Method from MovingPictures plugin.
    internal static Image LoadImageFastFromFile(string filename)
    {
      IntPtr imagePtr = IntPtr.Zero;
      Image image = null;

      try
      {
        if (GdipLoadImageFromFile(filename, out imagePtr) != 0)
        {
          logger.Warn("gdiplus.dll method failed. Will degrade performance.");
          image = Image.FromFile(filename);
        }

        else
          image =
            (Image)
              typeof (Bitmap).InvokeMember("FromGDIplus",
                BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.InvokeMethod, null, null,
                new object[] {imagePtr});
      }
      catch (Exception)
      {
        logger.Error("Failed to load image from " + filename);
        image = null;
      }

      return image;

    }

    /// <summary>
    /// Get filename string.
    /// </summary>
    internal static string GetFilenameNoPath(string key)
    {
      if (key == null)
      {
        return string.Empty;
      }

      if (File.Exists(key))
      {
        key = Path.GetFileName(key);
      }

      key = key.Replace("/", "\\");
      if (key.LastIndexOf("\\", StringComparison.CurrentCulture) >= 0)
      {
        key = key.Substring(key.LastIndexOf("\\", StringComparison.CurrentCulture) + 1);
      }
      return key;
    }


    internal static void LoadImage(string name, ref ArrayList Images)
    {
      try
      {
        if (name == null)
          name = "";

        //load images as MP resource
        if (name != null && name.Length > 0)
        {
          if (Images != null && !Images.Contains(name))
          {
            try
            {
              Images.Add(name);
            }
            catch (Exception ex)
            {
              logger.Error("LoadImage: " + ex.ToString());
            }
            Utils.LoadImage(name);
          }
        }
      }
      catch (Exception ex)
      {
        logger.Error("LoadImage: " + ex.ToString());
      }
    }

    internal static void UnLoadImage(string name, ref ArrayList Images)
    {
      try
      {
        if (name == null)
          name = "";

        //load images as MP resource
        if (name != null && name.Length > 0)
        {
          if (Images != null)
          {
            foreach (Object image in Images)
            {
              //unload old image to free MP resource
              if (image != null && !image.ToString().Equals(name))
              {
                UNLoadImage(image.ToString());
              }
            }
            Images.Clear();
            Images.Add(name);
          }
        }
      }
      catch (Exception ex)
      {
        logger.Error("UnLoadImage: " + ex.ToString());
      }
    }

    internal static void UnLoadImage(ref ArrayList Images)
    {
      try
      {
        if (Images != null)
        {
          foreach (Object image in Images)
          {
            //unload old image to free MP resource
            if (image != null)
            {
              UNLoadImage(image.ToString());
            }
          }
          Images.Clear();
        }
      }
      catch (Exception ex)
      {
        logger.Error("UnLoadImage: " + ex.ToString());
      }
    }

    internal static bool IsIdle()
    {
      try
      {
        TimeSpan ts = DateTime.Now - GUIGraphicsContext.LastActivity;
        if (ts.TotalMilliseconds >= 350)
        {
          return true;
        }
      }
      catch (Exception ex)
      {
        logger.Error("IsIdle: " + ex.ToString());
      }
      return false;
    }



    /// <summary>
    /// Decide if image is corropt or not
    /// </summary>
    internal static bool IsFileValid(string filename)
    {
      if (filename == null)
      {
        return false;
      }

      Image checkImage = null;
      try
      {
        checkImage = Utils.LoadImageFastFromFile(filename);
        if (checkImage != null && checkImage.Width > 0)
        {
          checkImage.Dispose();
          checkImage = null;
          return true;
        }
        if (checkImage != null)
        {
          checkImage.Dispose();
        }
        checkImage = null;
      }
      catch //(Exception ex)
      {
        checkImage = null;
      }
      return false;
    }
  }
}
