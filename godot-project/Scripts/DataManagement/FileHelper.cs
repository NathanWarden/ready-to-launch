using System.Collections.Generic;
using System.Text;
using Godot;
using Path = System.IO.Path;

public static class FileHelper
{
	public static string BasePath => "user://";
	public static string CachePath => PathCombine(BasePath, "Cache");
	public const string InternalDataPath = "res://Data/";


	static FileHelper()
	{
		CreateDirectoryForUser("Data");
		CreateDirectoryForUser("Cache");
	}


	public static string PathCombine(params string[] path)
	{
		var pathList = new List<string>();

		foreach (var element in path)
		{
			if (!string.IsNullOrEmpty(element))
				pathList.Add(element);
		}

		path = pathList.ToArray();

		if (path.Length <= 0 || string.IsNullOrEmpty(path[0]))
			return null;

		StringBuilder result = new StringBuilder(path[0]);
		bool addNext = path[0][path[0].Length-1] != '/';

		for (int i = 1; i < path.Length; i++)
		{
			if (!string.IsNullOrEmpty(path[i]) && path[i][0] != '/')
			{
				if (addNext) result.Append("/");
				result.Append(path[i]);

				addNext = path[i][path[i].Length-1] != '/';
			}
		}

		return result.ToString();
	}


	public static string ReadAllText(string path)
	{
		var file = new File();
		file.Open(path, File.ModeFlags.Read);
		var len = file.GetLen();
		var data = file.GetBuffer((int)len);
		var json = Encoding.UTF8.GetString(data);
		file.Close();

		return json;
	}


	public static void WriteAllText(string path, string json)
	{
		var file = new File();
		file.Open(path, File.ModeFlags.Write);
		file.StoreString(json);
		file.Close();
	}


	public static bool FileExists(string path)
	{
		var file = new File();
		return file.FileExists(path);
	}


	public static string ReadUserText(string path)
	{
		return ReadAllText(PathCombine(BasePath, path));
	}


	public static void WriteUserText(string path, string json)
	{
		WriteAllText(PathCombine(BasePath, path), json);
	}


	public static bool UserFileExists(string path)
	{
		return FileExists(PathCombine(BasePath, path));
	}


	public static void Delete(string path)
	{
		GD.Print("*********** Delete file!");
	}


	private static void CreateDirectoryForUser(string directory)
	{
		var dir = new Directory();
		var path = Path.Combine(BasePath, directory);

		if (!dir.DirExists(path))
		{
			dir.MakeDirRecursive(path);
		}
	}
}