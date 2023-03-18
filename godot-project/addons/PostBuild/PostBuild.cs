#if TOOLS
using System.IO;
using Godot;
using Path = System.IO.Path;


[Tool]
public class PostBuild : EditorExportPlugin
{
	private string[] features;
	private bool isDebug;
	private string path;


	public override void _ExportBegin(string[] features, bool isDebug, string path, int flags)
	{
		this.features = features;
		this.isDebug = isDebug;
		this.path = path;
	}


	public override void _ExportEnd()
	{
		foreach (var feat in features)
		{
			if (feat.Equals("OSX"))
			{
				//PostMacBuild();
			}
		}
	}


	private void PostMacBuild()
	{
		var fileInfo = new FileInfo(path);
		var buildDirectory = fileInfo.Directory;
		System.IO.Compression.ZipFile.ExtractToDirectory(fileInfo.FullName, buildDirectory.FullName);
	}
}
#endif
