//#define PRINT_DEBUG

using System.IO;
using Godot;
using Mono.Unix;
using Directory = System.IO.Directory;
using Environment = System.Environment;
using File = System.IO.File;
using Path = System.IO.Path;

namespace GodotLauncher
{
	public class DataPaths
	{
		static string AppDataPath => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
		static string BasePath => Path.Combine(AppDataPath, "ReadyToLaunch");
		public static string platformOverride;


		public static string SanitizeProjectPath(string path)
		{
			if (File.Exists(path))
			{
				path = new FileInfo(path).DirectoryName;
			}

			return path;
		}


		public static void EnsureProjectExists(string path)
		{
			var filePath = Path.Combine(path, "project.godot");
			if (!File.Exists(filePath)) File.WriteAllText(filePath, "");
		}


		public static string GetExecutablePath(InstallerEntryData installerEntryData)
		{
			string platformName = GetPlatformName();
			string path = Path.Combine(BasePath, platformName, installerEntryData.BuildType, installerEntryData.version);

			path = Path.Combine(path, installerEntryData.ExecutableName);

			return path;
		}


		public static string GetPlatformName()
		{
			if (!string.IsNullOrEmpty(platformOverride)) return platformOverride;
			return OS.GetName();
		}


		public static void WriteFile(string fileName, byte[] data)
		{
			var path = Path.Combine(BasePath, fileName);
			#if PRINT_DEBUG
			GD.Print("Writing: " + path);
			#endif
			File.WriteAllBytes(path, data);
		}


		public static void WriteFile(string fileName, string data)
		{
			var path = Path.Combine(BasePath, fileName);
			#if PRINT_DEBUG
			GD.Print("Writing: " + path);
			#endif
			File.WriteAllText(path, data);
		}


		public static string ReadFile(string fileName, string defaultData = null)
		{
			var path = Path.Combine(BasePath, fileName);
			if (File.Exists(path))
			{
				#if PRINT_DEBUG
				GD.Print("Reading: " + path);
				#endif
				return File.ReadAllText(path);
			}

			#if PRINT_DEBUG
			GD.Print("File not found: " + path);
			#endif
			return defaultData;
		}


		public static bool ExecutableExists(InstallerEntryData installerEntryData)
		{
			string path = GetExecutablePath(installerEntryData);
			bool exists = File.Exists(path);
			#if PRINT_DEBUG
			GD.Print("Checking if path exists: " + path + " exists=" + exists);
			#endif
			return exists;
		}


		public static void ExtractArchive(string fileName, InstallerEntryData installerEntryData)
		{
			string source = Path.Combine(BasePath, fileName);
			string dest = Path.Combine(BasePath, GetPlatformName(), installerEntryData.BuildType, installerEntryData.version);
			if (!Directory.Exists(dest)) System.IO.Compression.ZipFile.ExtractToDirectory(source, dest);
			File.Delete(source);
		}


		public static void DeleteVersion(string version, string buildType)
		{
			Directory.Delete(Path.Combine(BasePath, GetPlatformName(), buildType, version), true);
		}


		public static void LaunchGodot(InstallerEntryData installerEntryData, string arguments = "")
		{
			string path = GetExecutablePath(installerEntryData);
			#if PRINT_DEBUG
			GD.Print("Launching: " + path);
			#endif
			if (!OS.GetName().Equals("Windows"))
			{
				var unixFile = new UnixFileInfo(path);
				unixFile.FileAccessPermissions |= FileAccessPermissions.UserExecute
												  | FileAccessPermissions.GroupExecute
												  | FileAccessPermissions.OtherExecute;
			}

			using var process = new System.Diagnostics.Process();
			process.StartInfo.FileName = path;
			process.StartInfo.WorkingDirectory = BasePath;
			process.StartInfo.Arguments = arguments;
			process.Start();
		}


		public static void CreateInstallationDirectory()
		{
			MoveOldInstallationDirectory("ReadyForLaunch");
			MoveOldInstallationDirectory("GodotLauncher");
			Directory.CreateDirectory(BasePath);
		}


		static void MoveOldInstallationDirectory(string oldName)
		{
			var oldPath = Path.Combine(AppDataPath, oldName);
			if (!Directory.Exists(oldPath) || Directory.Exists(BasePath))
				return;
			Directory.Move(oldPath, BasePath);
		}


		public static void ShowInFolder(string filePath)
		{
			filePath = "\"" + filePath + "\"";
			switch (OS.GetName())
			{
				case "Linux":
					System.Diagnostics.Process.Start("xdg-open", filePath);
					break;
				case "Windows":
					string argument = "/select, " + filePath;
					System.Diagnostics.Process.Start("explorer.exe", argument);
					break;
				case "OSX":
					System.Diagnostics.Process.Start("open", filePath);
					break;
				default:
					GD.Print(OS.GetName() + " not implemented!");
					break;
			}
		}
	}
}
