extends VBoxContainer

var dividerButton:Button
var entry:Node
var dividerButtonsDict = {}
var dividersToggledDict = {}
var dividerInstallersDict = {}
var nodesDict = {}


func _ready():
	dividerButton = get_node("DividerButton")
	dividerButton.visible = false

	entry = get_node("Entry")
	entry.visible = false


func _clear_installer_buttons():
	for key in nodesDict:
		nodesDict[key].queue_free()
	for key in dividerButtonsDict:
		dividerButtonsDict[key].queue_free()
	dividerButtonsDict.clear()
	dividerInstallersDict.clear()
	nodesDict = {}


func _add_installer_button(version:String, buildType:String, path:String, installerExists:bool):
	if version == "hack":
		return

	var majorMinorVersion:String = get_major_minor_version(version)
	if not dividerButtonsDict.has(majorMinorVersion):
		var newDivider = dividerButton.duplicate()
		dividerButtonsDict[majorMinorVersion] = newDivider
		dividerInstallersDict[majorMinorVersion] = Array()
		newDivider.text = majorMinorVersion
		newDivider.visible = true
		newDivider.set_data(majorMinorVersion)
		self.add_child(newDivider)

	if not dividersToggledDict.has(majorMinorVersion):
		dividersToggledDict[majorMinorVersion] = false

	var newEntry = entry.duplicate()
	self.add_child(newEntry)
	newEntry.visible = dividersToggledDict[majorMinorVersion]
	newEntry.get_node("Label").text = version + "\n(" + buildType + ")"
	newEntry.get_node("LaunchButton").set_data(version, buildType, path)
	var key = version + buildType
	nodesDict[key] = newEntry
	dividerInstallersDict[majorMinorVersion].append(newEntry)
	update_ui_elements(version, buildType, installerExists)


func get_major_minor_version(version:String):
	var delimiter:String = "."
	version = version.split("-")[0]
	var dotPosition:int = version.find(delimiter)
	dotPosition = version.find(delimiter, dotPosition+1)
	return version.substr(0, dotPosition)


func _update_installer_button(version:String, buildType:String, installerExists:bool):
	update_ui_elements(version, buildType, installerExists)


func update_divider_button(majorMinorVersion:String):
	dividersToggledDict[majorMinorVersion] = not dividersToggledDict[majorMinorVersion]
	for installer in dividerInstallersDict[majorMinorVersion]:
		installer.visible = dividersToggledDict[majorMinorVersion]


func update_ui_elements(version:String, buildType:String, installerExists:bool):
	var key = version + buildType
	if !nodesDict.has(key):
		return
	nodesDict[key].get_node("Downloaded").pressed = installerExists
	nodesDict[key].get_node("KebabMenu").visible = installerExists
