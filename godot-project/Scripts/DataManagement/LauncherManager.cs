//#define PRINT_DEBUG

using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using File = System.IO.File;
using Path = System.IO.Path;

namespace GodotLauncher;

public partial class LauncherManager : Control
{
	[Export] private bool useLocalData;

	private CheckBox installedOnlyToggle;
	private CheckBox classicToggle;
	private CheckBox monoToggle;
	private CheckBox preReleaseToggle;

	private FileDialog fileDialog;
	private MenuButton newProjectVersion;
	private string newProjectVersionKey;

	private Node projectsEntriesNode;
	private Node installersEntriesNode;
	private Control infoNode;
	private double infoNodeTimer;
	private bool showExtracting;

	private const string ConfigFileName = "config.json";
	private const string ProjectsFileName = "projects.json";

	private Downloader installersDownloader;
	private const string InstallersJson = "https://raw.githubusercontent.com/NathanWarden/ready-to-launch/master/godot-project/Data/installers.json";
	private const string LastInstallerList = "last-installers.json";

	private List<ProjectEntryData> projectEntries = new();
	private Dictionary<string, InstallerEntryData> installerEntries = new();
	private Dictionary<string, InstallerEntryData> previousInstallers = new();
	private Dictionary<string, Downloader> downloaders = new();

	private Config config;

	public override void _Ready()
	{
		GetWindow().Title = "Ready To Launch (Alpha)";
		GetWindow().FilesDropped += _onFilesDropped;

		DataPaths.CreateInstallationDirectory();

		var configJson = DataPaths.ReadFile(ConfigFileName, "{}");
		config = DataBuilder.LoadConfigFromJson(configJson);

		var projectsJson = DataPaths.ReadFile(ProjectsFileName, "[]");
		projectEntries = DataBuilder.LoadProjectListFromJson(projectsJson);

		fileDialog = GetNode<FileDialog>("FileDialog");
		newProjectVersion = GetNode<MenuButton>("ProjectsPanel/AddProjectsContainer/NewProjectVersionMenu");
		projectsEntriesNode = GetNode("ProjectsPanel/ProjectsList/ProjectEntries");
		installersEntriesNode = GetNode("InstallersPanel/InstallersList/InstallerEntries");
		infoNode = GetNode<Control>("Info");

		SetupToggles();

		if (OS.IsDebugBuild() && useLocalData)
		{
			installerEntries = DataBuilder.BuildInstallerData();
			BuildLists(false);
		}
		else
		{
			var json = DataPaths.ReadFile(LastInstallerList);
			installerEntries = DataBuilder.LoadInstallerData(json);
			BuildLists(false);

			installersDownloader = new Downloader(InstallersJson, this);
			installersDownloader.Start();
		}
	}

	public override void _Process(double delta)
	{
		if (CheckForQuit())
			return;

		if (infoNodeTimer > 0)
		{
			infoNodeTimer -= delta;
			if (infoNodeTimer <= 0)
				infoNode.Visible = false;
		}

		if (installersDownloader != null && installersDownloader.IsDone)
		{
			// If the downloader failed, use the last downloaded json data
			var previousJson = DataPaths.ReadFile(LastInstallerList);
			if (string.IsNullOrEmpty(previousJson))
			{
				// If that doesn't exist, use the builtin one
				previousJson = FileHelper.ReadAllText("res://Data/installers.json");
			}

			var json = installersDownloader.HasError ? previousJson : installersDownloader.ReadText();
			DataPaths.WriteFile(LastInstallerList, json);

			installerEntries = DataBuilder.LoadInstallerData(json);
			previousInstallers = DataBuilder.LoadInstallerData(previousJson);

			BuildLists(true);

			installersDownloader = null;
		}

		foreach (var dlPair in downloaders)
		{
			var key = dlPair.Key;
			var downloader = dlPair.Value;
			var entry = installerEntries[key];

			if (downloader == null)
				continue;
			if (!downloader.IsDone)
			{
				infoNode.Call("show_message", "Downloading Godot " + entry.version + $" ({entry.BuildType}) ...\n"
											+ downloader.SizeInMb.ToString("F2") + " MB");
				continue;
			}

			if (!showExtracting)
			{
				infoNode.Call("show_message", "Extracting...\n\nThis may take a few minutes.");
				showExtracting = true;
				return;
			}

			var data = downloader.ReadData();
			if (data != null)
			{
				string fileName = $"{key}.zip";
				DataPaths.WriteFile(fileName, data);
				DataPaths.ExtractArchive(fileName, entry);

				if (!GetNode<Control>("InstallersPanel").Visible)
				{
					BuildInstallersList(false);
				}

				bool installerExists = DataPaths.ExecutableExists(entry);
				installersEntriesNode.Call("_update_installer_button", entry.version, entry.BuildType, installerExists);

				downloaders.Remove(key);
				infoNode.Visible = false;
				showExtracting = false;

				BuildProjectsList();
				break;
			}

			if (downloader.HasError)
			{
				GD.Print(downloader.ErrorMessage);
				infoNode.Call("show_message", downloader.ErrorMessage);
				downloaders.Remove(key);
				infoNodeTimer = 3;
				break;
			}

			GD.Print("Data was null!");
		}
	}

	private bool CheckForQuit()
	{
		if (Input.IsActionPressed("Control") && Input.IsActionJustPressed("Quit"))
		{
			GetTree().Quit();
			return true;
		}

		return false;
	}

	private void SetupToggles()
	{
		var rootNode = GetNode("InstallersPanel/HBoxContainer");
		installedOnlyToggle = rootNode.GetNode<CheckBox>("InstalledOnlyToggle");
		classicToggle = rootNode.GetNode<CheckBox>("ClassicToggle");
		monoToggle = rootNode.GetNode<CheckBox>("MonoToggle");
		preReleaseToggle = rootNode.GetNode<CheckBox>("PreReleaseToggle");

		installedOnlyToggle.ButtonPressed = config.installedOnlyToggled;
		classicToggle.ButtonPressed = config.classicToggled;
		monoToggle.ButtonPressed = config.monoToggled;
		preReleaseToggle.ButtonPressed = config.preReleaseToggled;
	}

	private void BuildProjectsList()
	{
		var installers = GetInstalledVersions();
		var installerKeysList = new List<string>();
		var installerNamesList = new List<string>();

		foreach (var installer in installers)
		{
			installerKeysList.Add(installer.VersionKey);
			installerNamesList.Add(installer.version + " " + installer.BuildType);
		}

		var installerKeys = installerKeysList.ToArray();
		var installerNames = installerNamesList.ToArray();

		projectsEntriesNode.Call("_clear_project_buttons");

		projectEntries.Sort((x, y) =>
		{
			if (x.timestamp == y.timestamp)
				return 0;
			return x.timestamp < y.timestamp ? 1 : -1;
		});

		newProjectVersion.Call("_setup", "", installerKeys, installerNames);

		foreach (var entry in projectEntries)
		{
			string version = "";
			string buildType = "";

			if (installerEntries.TryGetValue(entry.versionKey, out var installer))
			{
				version = installer.version;
				buildType = installer.BuildType;
			}

			projectsEntriesNode.Call("_add_project_button", entry.path, version, buildType, false, installerKeys, installerNames);
		}
	}

	private List<InstallerEntryData> GetInstalledVersions()
	{
		var results = new List<InstallerEntryData>();
		foreach (var entry in installerEntries.Values)
		{
			bool installerExists = DataPaths.ExecutableExists(entry);
			if (installerExists)
				results.Add(entry);
		}

		return results;
	}

	private void BuildLists(bool showNewInstallers)
	{
		BuildInstallersList(showNewInstallers);
		BuildProjectsList();
	}

	private void BuildInstallersList(bool showNewInstallers)
	{
		installersEntriesNode.Call("_clear_installer_buttons");

		if (showNewInstallers)
		{
			foreach (var entry in installerEntries)
			{
				if (!previousInstallers.ContainsKey(entry.Key))
				{
					projectsEntriesNode.Call("_new_installer_available", entry.Value.version,
						entry.Value.BuildType);
				}
			}
		}

		foreach (var entry in GetFilteredEntries())
		{
			bool installerExists = DataPaths.ExecutableExists(entry);
			var path = DataPaths.GetExecutablePath(entry);
			installersEntriesNode.Call("_add_installer_button", entry.version, entry.BuildType, path, installerExists);
		}
	}

	private IEnumerable<InstallerEntryData> GetFilteredEntries()
	{
		foreach (var entry in installerEntries.Values)
		{
			if (config.installedOnlyToggled && !DataPaths.ExecutableExists(entry))
				continue;
			if (!config.preReleaseToggled && entry.preRelease)
				continue;
			if (!config.monoToggled && entry.mono)
				continue;
			if (!config.classicToggled && !entry.mono)
				continue;
			yield return entry;
		}
	}

	private void _onNewProjectPressed() => fileDialog.Visible = true;

	private void _onNewProjectVersionChanged(string versionKey) =>
		newProjectVersionKey = versionKey;

	private void _onFileDialogDirSelected(string directoryPath)
	{
		if (string.IsNullOrEmpty(newProjectVersionKey))
		{
			var installers = GetInstalledVersions();
			if (installers.Count == 0)
			{
				GD.Print("No version selected!!!");
				return;
			}

			newProjectVersionKey = installers[0].VersionKey;
		}

		DataPaths.EnsureProjectExists(directoryPath);
		var project = new ProjectEntryData
		{
			path = directoryPath,
			versionKey = newProjectVersionKey,
			timestamp = DateTime.UtcNow.Ticks
		};
		projectEntries.Add(project);
		LaunchProject(directoryPath, false);
	}

	private void _onFilesDropped(string[] files)
	{
		for (int i = 0; i < files.Length; i++)
		{
			string path = DataPaths.SanitizeProjectPath(files[i]);

			// Check for duplicates
			if (projectEntries.Any(t => t.path.Equals(path)))
				continue;

			var versionKey = File.ReadAllText(GodotVersionPath(path));
			projectEntries.Add(new ProjectEntryData
			{
				path = path,
				versionKey = versionKey,
				timestamp = 0
			});

			InstallVersion(versionKey);
		}

		SaveProjectsList();
		BuildProjectsList();
	}

	private void SaveConfig()
	{
		var json = DataBuilder.GetConfigJson(config);
		DataPaths.WriteFile(ConfigFileName, json);
	}

	private void SaveProjectsList()
	{
		var json = DataBuilder.GetProjectListJson(projectEntries);
		DataPaths.WriteFile(ProjectsFileName, json);
	}

	private string GodotVersionPath(string basePath) => Path.Combine(basePath, "godotversion.txt");

	private void _onProjectEntryPressed(string path) => LaunchProject(path, false);

	private void _onRunProject(string path) => LaunchProject(path, true);

	private void LaunchProject(string path, bool run)
	{
		for (int i = 0; i < projectEntries.Count; i++)
		{
			if (projectEntries[i].path.Equals(path) && installerEntries.TryGetValue(projectEntries[i].versionKey, out var entry))
			{
				var project = projectEntries[i];

#if PRINT_DEBUG
				GD.Print("Launch " + path);
#endif

				if (!run)
				{
					File.WriteAllText(GodotVersionPath(path), project.versionKey);
				}

				project.timestamp = DateTime.UtcNow.Ticks;
				SaveProjectsList();
				BuildProjectsList();

				if (entry.version.StartsWith("1.") || entry.version.StartsWith("2."))
				{
					LaunchInstaller(entry);
					return;
				}

				var additionalFlags = run ? "" : "-e";
				DataPaths.LaunchGodot(entry, additionalFlags + " --path \"" + path + "\"");
				//OS.WindowMinimized = config.minimizeOnLaunch;
				return;
			}
		}
	}

	private void _onProjectVersionChanged(string path, string versionKey)
	{
		foreach (var entry in projectEntries)
		{
			if (entry.path.Equals(path))
			{
				entry.versionKey = versionKey;
				break;
			}
		}

		SaveProjectsList();
	}

	private void _onShowInFolder(string path)
	{
		var fileInfo = new FileInfo(path);
		if (fileInfo.Exists)
		{
			path = fileInfo.DirectoryName;
		}
		DataPaths.ShowInFolder(path);
	}

	private void _onProjectDeletePressed(string path)
	{
		for (int i = 0; i < projectEntries.Count; i++)
		{
			if (!projectEntries[i].path.Equals(path))
				continue;
			projectEntries.RemoveAt(i);
			break;
		}

		SaveProjectsList();
		BuildProjectsList();
	}

	private void _onInstallerEntryPressed(string version, string buildType) =>
		InstallVersion(version + buildType);

	private void InstallVersion(string key)
	{
		var installerEntry = installerEntries[key];

		if (LaunchInstaller(installerEntry) || downloaders.ContainsKey(key))
			return;
		var entry = installerEntries[key];
		downloaders[key] = new Downloader(entry.Url, this);
		downloaders[key].Start();
		infoNode.Call("show_message", "Downloading Godot " + installerEntry.version + $" ({installerEntry.BuildType}) ...");
	}

	private bool LaunchInstaller(InstallerEntryData installerEntry)
	{
		bool installerExists = DataPaths.ExecutableExists(installerEntry);
		if (installerExists)
		{
			DataPaths.LaunchGodot(installerEntry);
			//OS.WindowMinimized = config.minimizeOnLaunch;
			return true;
		}

		return false;
	}

	private void _onInstallerDeletePressed(string version, string buildType)
	{
		DataPaths.DeleteVersion(version, buildType);
		BuildLists(false);
	}

	private void _onInstalledOnlyToggled(bool state)
	{
		config.installedOnlyToggled = state;
		BuildInstallersList(false);
		SaveConfig();
	}

	private void _onClassicToggled(bool state)
	{
		config.classicToggled = state;
		BuildInstallersList(false);
		SaveConfig();
	}

	private void _onMonoToggled(bool state)
	{
		config.monoToggled = state;
		BuildInstallersList(false);
		SaveConfig();
	}

	private void _onPreReleaseToggled(bool state)
	{
		config.preReleaseToggled = state;
		BuildInstallersList(false);
		SaveConfig();
	}

	private void _onDebugOsSelected(string os)
	{
		DataPaths.platformOverride = os;
		BuildInstallersList(false);
	}

	private void _onDownloadAllPressed()
	{
		foreach (var entry in installerEntries.Values)
		{
			if (DataPaths.ExecutableExists(entry) || string.IsNullOrEmpty(entry.Url))
				continue;
			var key = entry.VersionKey;
			downloaders[key] = new Downloader(entry.Url, this);
			downloaders[key].Start();
		}
	}
}