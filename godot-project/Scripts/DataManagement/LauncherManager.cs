//#define PRINT_DEBUG

using System;
using Godot;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Path = System.IO.Path;
using File = System.IO.File;


namespace GodotLauncher
{
	public class LauncherManager : Control
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
		private float infoNodeTimer;
		private bool showExtracting;

		private const string configFileName = "config.json";
		private const string projectsFileName = "projects.json";

		private Downloader installersDownloader;
		private const string installersJson = "https://raw.githubusercontent.com/NathanWarden/godot-launcher-installers/master/installers.json";
		private const string lastInstallerList = "last-installers.json";

		private List<ProjectEntryData> projectEntries = new List<ProjectEntryData>();
		private Dictionary<string, InstallerEntryData> installerEntries = new Dictionary<string, InstallerEntryData>();
		private Dictionary<string, InstallerEntryData> previousInstallers = new Dictionary<string, InstallerEntryData>();
		private Dictionary<string, Downloader> downloaders = new Dictionary<string, Downloader>();

		private Config config;


		public override void _Ready()
		{
			OS.SetWindowTitle("Ready To Launch (Alpha)");

			GetTree().Connect("files_dropped", this, nameof(_onFilesDropped));

			DataPaths.CreateInstallationDirectory();

			var configJson = DataPaths.ReadFile(configFileName, "{}");
			config = DataBuilder.LoadConfigFromJson(configJson);

			var projectsJson = DataPaths.ReadFile(projectsFileName, "[]");
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
				var json = DataPaths.ReadFile(lastInstallerList);
				installerEntries = DataBuilder.LoadInstallerData(json);
				BuildLists(false);

				installersDownloader = new Downloader(installersJson);
				installersDownloader.Start();
			}
		}

		public override void _Process(float delta)
		{
			if (CheckForQuit()) return;

			if (infoNodeTimer > 0)
			{
				infoNodeTimer -= delta;
				if (infoNodeTimer <= 0)
					infoNode.Visible = false;
			}

			if (installersDownloader != null && installersDownloader.IsDone)
			{
				// If the downloader failed, use the last downloaded json data
				var previousJson = DataPaths.ReadFile(lastInstallerList);
				if (string.IsNullOrEmpty(previousJson))
				{
					// If that doesn't exist, use the builtin one
					previousJson = FileHelper.ReadAllText("res://Data/installers.json");
				}

				var json = installersDownloader.HasError ? previousJson : installersDownloader.ReadText();
				DataPaths.WriteFile(lastInstallerList, json);

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

				if (downloader == null) continue;
				if (!downloader.IsDone) continue;

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

			installedOnlyToggle.Pressed = config.installedOnlyToggled;
			classicToggle.Pressed = config.classicToggled;
			monoToggle.Pressed = config.monoToggled;
			preReleaseToggle.Pressed = config.preReleaseToggled;
		}


		void BuildProjectsList()
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
				if (x.timestamp == y.timestamp) return 0;
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


		List<InstallerEntryData> GetInstalledVersions()
		{
			var results = new List<InstallerEntryData>();
			foreach (var entry in installerEntries.Values)
			{
				bool installerExists = DataPaths.ExecutableExists(entry);
				if (installerExists) results.Add(entry);
			}

			return results;
		}


		void BuildLists(bool showNewInstallers)
		{
			BuildInstallersList(showNewInstallers);
			BuildProjectsList();
		}

		void BuildInstallersList(bool showNewInstallers)
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


		IEnumerable<InstallerEntryData> GetFilteredEntries()
		{
			foreach (var entry in installerEntries.Values)
			{
				if (config.installedOnlyToggled && !DataPaths.ExecutableExists(entry)) continue;
				if (!config.preReleaseToggled && entry.preRelease) continue;
				if (!config.monoToggled && entry.mono) continue;
				if (!config.classicToggled && !entry.mono) continue;
				yield return entry;
			}
		}


		void _onNewProjectPressed()
		{
			fileDialog.Visible = true;
		}


		void _onNewProjectVersionChanged(string versionKey)
		{
			newProjectVersionKey = versionKey;
		}


		void _onFileDialogDirSelected(string directoryPath)
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


		void _onFilesDropped(string[] files, int screen)
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


		void SaveConfig()
		{
			var json = DataBuilder.GetConfigJson(config);
			DataPaths.WriteFile(configFileName, json);
		}


		void SaveProjectsList()
		{
			var json = DataBuilder.GetProjectListJson(projectEntries);
			DataPaths.WriteFile(projectsFileName, json);
		}


		string GodotVersionPath(string basePath) => Path.Combine(basePath, "godotversion.txt");


		void _onProjectEntryPressed(string path)
		{
			LaunchProject(path, false);
		}


		void _onRunProject(string path)
		{
			LaunchProject(path, true);
		}


		void LaunchProject(string path, bool run)
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
					OS.WindowMinimized = config.minimizeOnLaunch;
					return;
				}
			}
		}


		void _onProjectVersionChanged(string path, string versionKey)
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


		void _onShowInFolder(string path)
		{
			var fileInfo = new FileInfo(path);
			if (fileInfo.Exists)
			{
				path = fileInfo.DirectoryName;
			}
			DataPaths.ShowInFolder(path);
		}


		void _onProjectDeletePressed(string path)
		{
			for (int i = 0; i < projectEntries.Count; i++)
			{
				if (!projectEntries[i].path.Equals(path)) continue;
				projectEntries.RemoveAt(i);
				break;
			}

			SaveProjectsList();
			BuildProjectsList();
		}

		void _onInstallerEntryPressed(string version, string buildType)
		{
			InstallVersion(version + buildType);
		}

		void InstallVersion(string key)
		{
			var installerEntry = installerEntries[key];

			if (!LaunchInstaller(installerEntry) && !downloaders.ContainsKey(key))
			{
				var entry = installerEntries[key];
				downloaders[key] = new Downloader(entry.Url);
				downloaders[key].Start();
				infoNode.Call("show_message", "Downloading Godot " + installerEntry.version + $" ({installerEntry.BuildType}) ...");
			}
		}


		bool LaunchInstaller(InstallerEntryData installerEntry)
		{
			bool installerExists = DataPaths.ExecutableExists(installerEntry);
			if (installerExists)
			{
				DataPaths.LaunchGodot(installerEntry);
				OS.WindowMinimized = config.minimizeOnLaunch;
				return true;
			}

			return false;
		}


		void _onInstallerDeletePressed(string version, string buildType)
		{
			DataPaths.DeleteVersion(version, buildType);
			BuildLists(false);
		}


		void _onInstalledOnlyToggled(bool state)
		{
			config.installedOnlyToggled = state;
			BuildInstallersList(false);
			SaveConfig();
		}


		void _onClassicToggled(bool state)
		{
			config.classicToggled = state;
			BuildInstallersList(false);
			SaveConfig();
		}


		void _onMonoToggled(bool state)
		{
			config.monoToggled = state;
			BuildInstallersList(false);
			SaveConfig();
		}


		void _onPreReleaseToggled(bool state)
		{
			config.preReleaseToggled = state;
			BuildInstallersList(false);
			SaveConfig();
		}


		void _onDebugOsSelected(string os)
		{
			DataPaths.platformOverride = os;
			BuildInstallersList(false);
		}

		void _onDownloadAllPressed()
		{
			foreach (var entry in installerEntries.Values)
			{
				if (DataPaths.ExecutableExists(entry) || string.IsNullOrEmpty(entry.Url)) continue;
				var key = entry.VersionKey;
				downloaders[key] = new Downloader(entry.Url);
				downloaders[key].Start();
			}
		}
	}
}
