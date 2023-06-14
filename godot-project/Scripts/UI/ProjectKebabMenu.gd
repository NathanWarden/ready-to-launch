extends MenuButton


var popup:PopupMenu
var signalConnected:bool = false

var showInFolder:String = "Show In Folder"
var runProject:String = "Run"
var remove:String = "Remove"


func _ready():
	popup = get_popup()
	popup.clear()

	popup.add_item(showInFolder)
	popup.add_item(runProject)
	popup.add_separator()
	popup.add_item(remove)

	if signalConnected:
		return

	var err = popup.index_pressed.connect(_onIndexPressed)
	if err != OK:
		print("Connection failed!")
		return
	signalConnected = true

func _onIndexPressed(index:int):
	var projectButton = get_parent().get_node("ProjectButton")
	match popup.get_item_text(index):
		showInFolder:
			projectButton._show_in_folder()
		runProject:
			projectButton._run_project()
		remove:
			projectButton._on_delete()
