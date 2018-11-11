//***********************************************************************
// Assembly         : LatestMediaHandler
// Author           : cul8er
// Created          : 05-09-2010
//
// Last Modified By : ajs
// Last Modified On : 10-05-2010
// Description      : 
//
// Copyright        : Open Source software licensed under the GNU/GPL agreement.
//***********************************************************************
using System.Collections.Generic;
using System.Globalization;

namespace LatestMediaHandler
{
  internal class RecordingsCollection : List<LatestRecording>
  {
  }

  internal class LatestRecordingsComparer : IComparer<LatestRecording>
  {
    public int Compare(LatestRecording latest1, LatestRecording latest2)
    {
      int returnValue = 1;
      if (latest1 is LatestRecording && latest2 is LatestRecording)
      {
        string s1 = latest1.StartDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture);
        string s2 = latest2.StartDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture);
        returnValue = s1.CompareTo(s2);
      }
      return returnValue;
    }
  }
}
