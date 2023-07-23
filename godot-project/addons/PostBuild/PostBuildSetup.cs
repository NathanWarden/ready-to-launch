#if TOOLS
using Godot;

[Tool]
public partial class PostBuildSetup : EditorPlugin
{
	private PostBuild buildPlugin;


	public override void _EnterTree()
	{
		buildPlugin = new PostBuild();
		AddExportPlugin(buildPlugin);
	}


	public override void _ExitTree() => RemoveExportPlugin(buildPlugin);
}
#endif
