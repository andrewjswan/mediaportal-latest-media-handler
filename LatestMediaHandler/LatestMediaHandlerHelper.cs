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


using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace LatestMediaHandler
{
  internal class LatestMediaHandlerHelper
  {
    public string fileVersion(string fileToCheck)
    {
      if (File.Exists(fileToCheck))
        return FileVersionInfo.GetVersionInfo(fileToCheck).FileVersion;
      else
        return "0.0.0.0";
    }

    public static bool IsAssemblyAvailable(string name, Version ver)
    {
      return IsAssemblyAvailable(name, ver, null);
    }

    public static bool IsAssemblyAvailable(string name, Version ver, string filename)
    {
      var flag = false;
      foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
      {
        try
        {
          if (assembly.GetName().Name == name)
            return assembly.GetName().Version >= ver;
        }
        catch
        {
          flag = false;
        }
      }
      if (!flag)
      {
        try
        {
          if (string.IsNullOrEmpty(filename))
          {
            if (Assembly.ReflectionOnlyLoad(name).GetName().Version >= ver)
              flag = true;
          }
          else if (Assembly.ReflectionOnlyLoadFrom(filename).GetName().Version >= ver)
            flag = true;
        }
        catch
        {
          flag = false;
        }
      }
      return flag;
    }
  }
}
