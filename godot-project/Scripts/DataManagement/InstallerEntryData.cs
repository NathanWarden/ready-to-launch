using Godot;
using Newtonsoft.Json;
using System;

namespace GodotLauncher;

public class InstallerEntryData
{
	public string version;
	public bool mono;
	public bool preRelease;
	public PlatformData linux;
	public PlatformData mac;
	public PlatformData win;

	[JsonIgnore] public string VersionKey => version + BuildType;
	[JsonIgnore] public string BuildType => mono ? "mono" : "classic";

	[JsonIgnore]
	public PlatformData PlatformData
	{
		get
		{
			switch (DataPaths.GetPlatformName())
			{
				case "macOS":
					return mac;

				case "Linux":
					return linux;

				case "Windows":
					return win;

				default:
					throw new Exception("OS not defined! " + OS.GetName());
			}
		}
	}

	[JsonIgnore] public string Url => PlatformData.url;
	[JsonIgnore] public string ExecutableName => PlatformData.executableName;
}

public struct PlatformData
{
	public string url;
	public string executableName;
}