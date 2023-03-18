using System.Collections.Generic;
using GodotLauncher;
using Newtonsoft.Json;


public static class DataBuilder
{
	public static Dictionary<string, InstallerEntryData> BuildInstallerData()
	{
		var json = FileHelper.ReadAllText("res://Data/installers.json");
		var entries = LoadInstallerData(json);
		return entries;
	}

	public static Dictionary<string, InstallerEntryData> LoadInstallerData(string json)
	{
		if (json == null) return new Dictionary<string, InstallerEntryData>();

		var entries = JsonConvert.DeserializeObject<InstallerEntryData[]>(json);
		var entriesDict = new Dictionary<string, InstallerEntryData>();
		if (entries == null) return entriesDict;

		foreach (var entry in entries)
		{
			entriesDict[entry.VersionKey] = entry;
		}

		return entriesDict;
	}


	public static string GetProjectListJson(List<ProjectEntryData> projects)
	{
		return JsonConvert.SerializeObject(projects);
	}


	public static List<ProjectEntryData> LoadProjectListFromJson(string json)
	{
		return JsonConvert.DeserializeObject<List<ProjectEntryData>>(json);
	}


	public static string GetConfigJson(Config config)
	{
		return JsonConvert.SerializeObject(config);
	}


	public static Config LoadConfigFromJson(string json)
	{
		return JsonConvert.DeserializeObject<Config>(json);
	}
}
