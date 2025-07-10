using System;
using Microsoft.Extensions.Configuration;

namespace Chetch.Utilities;

public static class Config
{
    public static IConfiguration GetConfiguration(String[] settingsFiles)
    {
        foreach (var f in settingsFiles)
        {
            if (File.Exists(f))
            {
                var configBuilder = new ConfigurationBuilder().AddJsonFile(f, false, false);
                return configBuilder.Build();
            }
        }
        throw new Exception("Cannot file configuration file");
    }
}
