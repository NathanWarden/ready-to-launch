#if TOOLS
using Godot;

[Tool]
public class PostBuildSetup : EditorPlugin
{
	private PostBuild buildPlugin;


	public override void _EnterTree()
	{
		buildPlugin = new PostBuild();
		AddExportPlugin(buildPlugin);
	}


	public override void _ExitTree()
	{
		RemoveExportPlugin(buildPlugin);
	}
}
#endif
