// Type: LatestMediaHandler.LatestMediaHandlerHelper
// Assembly: LatestMediaHandler, Version=3.1.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 073E8D78-B6AE-4F86-BDE9-3E09A337833B
// Assembly location: D:\Mes documents\Desktop\LatestMediaHandler.dll

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
