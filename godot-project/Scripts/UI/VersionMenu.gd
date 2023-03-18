extends MenuButton

var keys:PoolStringArray
var names:PoolStringArray
var signalConnected:bool = false

func _setup(versionKey:String, installerKeys:PoolStringArray, installerNames:PoolStringArray):
	var popup:PopupMenu = get_popup()

	keys = installerKeys
	names = installerNames

	popup.clear()
	for name in installerNames:
		popup.add_item(name)

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

	var err = popup.connect("index_pressed", self, "_onIndexPressed")
	if err != OK:
		print("Connection failed!")
		return

	signalConnected = true


func _onIndexPressed(index:int):
	text = names[index]
	get_node("../ProjectButton")._onVersionChanged(keys[index])
