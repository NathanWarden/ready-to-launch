extends VBoxContainer

var entry:Node
var nodesDict = {}
var maxLength:int = 25
var firstNewInstaller:bool = true


func _ready():
	entry = get_node("Entry")
	entry.visible = false


func _clear_project_buttons():
	for key in nodesDict:
		nodesDict[key].queue_free()
	nodesDict = {}


func _new_installer_available(version:String, buildType:String):
	var installerEntry = get_parent().get_parent().get_node("NewInstallerEntry")
	installerEntry.visible = true
	var label = installerEntry.get_node("Label")
	if firstNewInstaller:
		label.text = ""
		firstNewInstaller = false
	else:
		label.text += "\n"
		installerEntry.rect_min_size.y += 23

	label.text += "New installer available: " + version + " (" + buildType + ")"


func _add_project_button(path:String, version:String, buildType:String, favorite:bool, installerKeys:PackedStringArray, installerNames:PackedStringArray):
	var newEntry = entry.duplicate()
	self.add_child(newEntry)
	newEntry.visible = true
	newEntry.get_node("Label").text = get_adjusted_path(path)
	newEntry.get_node("ProjectButton").set_data(path)
	newEntry.get_node("ProjectButton").tooltip_text = path
	var key = path
	nodesDict[key] = newEntry
	nodesDict[key].get_node("VersionMenu").call("_setup", version+buildType, installerKeys, installerNames)
	update_ui_elements(path, favorite)


func get_adjusted_path(path:String):
	var delimiter:String = "\\"
	var firstSlash:int = path.rfind(delimiter)
	if firstSlash == -1:
		delimiter = "/"
		firstSlash = path.rfind(delimiter)

	firstSlash = path.rfind(delimiter, firstSlash-1)
	return "..." + path.substr(firstSlash)


func update_ui_elements(path:String, favorite:bool):
	var key = path
	nodesDict[key].get_node("Favorite").button_pressed = favorite
	nodesDict[key].get_node("Favorite").visible = false
