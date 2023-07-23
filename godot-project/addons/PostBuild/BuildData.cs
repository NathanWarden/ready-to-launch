using Newtonsoft.Json;
using System;

[Serializable]
public class BuildData
{
	public const string BuildDataPath = "res://Data/BuildData.json";

	public int buildNumber;
	public string version;
	public string storeFlag;

	public static BuildData Load()
	{
		var json = FileHelper.ReadAllText(BuildDataPath);
		if (string.IsNullOrEmpty(json))
			return new BuildData();
		return JsonConvert.DeserializeObject<BuildData>(json);
	}

	public void Save() =>
		FileHelper.WriteAllText(BuildDataPath, JsonConvert.SerializeObject(this, Formatting.Indented));
}