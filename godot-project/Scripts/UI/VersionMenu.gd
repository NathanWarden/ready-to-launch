extends MenuButton

var keys:PackedStringArray
var names:PackedStringArray
var signalConnected:bool = false

func _setup(versionKey:String, installerKeys:PackedStringArray, installerNames:PackedStringArray):
	var popup:PopupMenu = get_popup()

	keys = installerKeys
	names = installerNames

	popup.clear()
	for installerName in installerNames:
		popup.add_item(installerName)

	if popup.get_item_count() > 0:
		var selectedItem = 0
		var selectedText = installerNames[0]
		for i in popup.get_item_count():
			if installerKeys[i] == versionKey:
				popup.set_item_checked(i, true)
				selectedText = installerNames[i]
				break

		popup.set_item_checked(selectedItem, true)
		text = selectedText

	if signalConnected:
		return

	var err = popup.index_pressed.connect(_onIndexPressed)
	if err != OK:
		print("Connection failed!")
		return

	signalConnected = true


func _onIndexPressed(index:int):
	text = names[index]
	get_node("../ProjectButton")._onVersionChanged(keys[index])
