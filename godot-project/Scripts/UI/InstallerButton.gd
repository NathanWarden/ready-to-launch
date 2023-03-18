extends Button

var controller:Node
var version:String
var buildType:String
var path:String


func _ready():
	controller = get_node("../../../../..")


func set_data(p_version:String, p_buildType:String, p_path:String):
	version = p_version
	buildType = p_buildType
	path = p_path


func _on_pressed():
	controller.call("_onInstallerEntryPressed", version, buildType)


func _show_in_folder():
	controller.call("_onShowInFolder", path)


func _on_delete():
	controller.call("_onInstallerDeletePressed", version, buildType)
