extends Button

var controller:Node
var path:String


func _ready():
	controller = get_node("../../../../..")


func set_data(p_path:String):
	path = p_path


func _on_pressed():
	controller.call("_onProjectEntryPressed", path)


func _onVersionChanged(p_versionKey:String):
	controller.call("_onProjectVersionChanged", path, p_versionKey)


func _show_in_folder():
	controller.call("_onShowInFolder", path)


func _run_project():
	controller.call("_onRunProject", path)


func _on_delete():
	controller.call("_onProjectDeletePressed", path)
