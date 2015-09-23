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
using System.Globalization;
using System.IO;

using RealNLog.NLog;

using MediaPortal.Util;
using TvControl;

namespace LatestMediaHandler
{
  internal class TVRecordingsThumbnailHandler
  {
    private static Logger logger = LogManager.GetCurrentClassLogger();

    internal static string GetThumb(string _filename)
    {
      string thumbNail = string.Empty;
      try
      {

        string previewThumb = string.Format(CultureInfo.CurrentCulture, "{0}\\{1}{2}", Thumbs.TVRecorded,
                                            Path.ChangeExtension(MediaPortal.Util.Utils.SplitFilename(_filename), null),
                                            MediaPortal.Util.Utils.GetThumbExtension());

        _filename = string.Format(CultureInfo.CurrentCulture, "{0}{1}", 
                                  Path.ChangeExtension(MediaPortal.Util.Utils.SplitFilename(_filename), null),
                                  MediaPortal.Util.Utils.GetThumbExtension());

        if (!MediaPortal.Util.Utils.FileExistsInCache(previewThumb))
        {
          try
          {
            byte[] thumbData = RemoteControl.Instance.GetRecordingThumbnail(_filename);

            if (thumbData.Length > 0)
            {
              using (FileStream fs = new FileStream(previewThumb, FileMode.Create))
              {
                fs.Write(thumbData, 0, thumbData.Length);
                fs.Close();
                fs.Dispose();
              }
              MediaPortal.Util.Utils.DoInsertExistingFileIntoCache(previewThumb);
            }
            else
            {
              logger.Debug("Thumbnail {0} not found on TV server", _filename);
            }
          }
          catch (Exception ex)
          {
            logger.Error("Error fetching thumbnail {0} from TV server - {1}", _filename, ex.Message);
          }
        }

        if (MediaPortal.Util.Utils.FileExistsInCache(previewThumb))
        {
          thumbNail = previewThumb;
        }
        else
        {
          thumbNail = "defaultTVBig.png";
        }
      }
      catch (Exception ex)
      {
        logger.Error("GetThumb: " + ex.ToString());
      }
      return thumbNail;
    }
  }
}
