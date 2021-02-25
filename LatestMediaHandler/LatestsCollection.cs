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
using System;
using System.Collections.Generic;

namespace LatestMediaHandler
{
  internal class LatestsCollection : List<Latest>
  {
  }

  internal class LatestAddedComparerDesc : IComparer<Latest>
  {
    public int Compare(Latest latest1, Latest latest2)
    {
      int returnValue = 0;
      if (latest1 is Latest && latest2 is Latest)
      {
        returnValue = latest2.DateTimeAdded.CompareTo(latest1.DateTimeAdded);
      }
      return returnValue;
    }
  }

  internal class LatestAddedComparerAsc : IComparer<Latest>
  {
    public int Compare(Latest latest1, Latest latest2)
    {
      int returnValue = 0;
      if (latest1 is Latest && latest2 is Latest)
      {
        returnValue = latest1.DateTimeAdded.CompareTo(latest2.DateTimeAdded);
      }
      return returnValue;
    }
  }

  internal class LatestRatingComparerDesc : IComparer<Latest>
  {
    public int Compare(Latest latest1, Latest latest2)
    {
      int returnValue = 0;
      if (latest1 is Latest && latest2 is Latest)
      {
        returnValue = latest2.DoubleRating.CompareTo(latest1.DoubleRating);
      }
      return returnValue;
    }
  }

  internal class LatestRatingComparerAsc : IComparer<Latest>
  {
    public int Compare(Latest latest1, Latest latest2)
    {
      int returnValue = 0;
      if (latest1 is Latest && latest2 is Latest)
      {
        returnValue = latest1.DoubleRating.CompareTo(latest2.DoubleRating);
      }
      return returnValue;
    }
  }

  internal class LatestWathcedComparerDesc : IComparer<Latest>
  {
    public int Compare(Latest latest1, Latest latest2)
    {
      int returnValue = 0;
      if (latest1 is Latest && latest2 is Latest)
      {
        returnValue = latest2.DateTimeWatched.CompareTo(latest1.DateTimeWatched);
      }
      return returnValue;
    }
  }

  internal class LatestWatchedComparerAsc : IComparer<Latest>
  {
    public int Compare(Latest latest1, Latest latest2)
    {
      int returnValue = 0;
      if (latest1 is Latest && latest2 is Latest)
      {
        returnValue = latest1.DateTimeWatched.CompareTo(latest2.DateTimeWatched);
      }
      return returnValue;
    }
  }
}
