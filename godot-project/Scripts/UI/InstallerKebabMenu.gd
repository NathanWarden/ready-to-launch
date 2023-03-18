extends MenuButton


var popup:PopupMenu
var signalConnected:bool = false

var showInFolder:String = "Show In Folder"
var remove:String = "Uninstall"


func _ready():
	popup = get_popup()
	popup.clear()

	popup.add_item(showInFolder)
	popup.add_separator()
	popup.add_item(remove)

	if signalConnected:
		return

	var err = popup.connect("index_pressed", self, "_onIndexPressed")
	if err != OK:
		print("Connection failed!")
		return
	signalConnected = true


func _onIndexPressed(index:int):
	var launchButton = get_parent().get_node("LaunchButton")
	match popup.get_item_text(index):
		showInFolder:
			launchButton._show_in_folder()
		remove:
			launchButton._on_delete()
