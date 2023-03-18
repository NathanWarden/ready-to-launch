using Godot;

public class PreBuildProcess : SceneTree
{
	const string configPath = "res://export_presets.cfg";
	const string androidConfigSection = "preset.4.options";


	public override void _Initialize()
	{
		var args = OS.GetCmdlineArgs();

		for (int i = 0; i < args.Length; i++)
		{
			if (args[i].Equals("--increment"))
			{
				UpdateBuildNumber();
			}

			if (args[i].Equals("--store-flag"))
			{
				var store = args[i + 1];
				var buildData = BuildData.Load();
				buildData.storeFlag = store;
				buildData.Save();
			}
		}

		Quit();
	}


	private void UpdateBuildNumber()
	{
		var buildData = BuildData.Load();
		buildData.buildNumber++;
		buildData.Save();

		var cfg = new ConfigFile();
		cfg.Load(configPath);
		cfg.SetValue(androidConfigSection, "version/code", buildData.buildNumber);
		cfg.Save(configPath);
		GD.Print("Build with code: " + buildData.buildNumber);
	}
}
