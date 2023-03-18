extends Button

var controller:Node


func _ready():
	controller = get_node("../../..")


func _onVersionChanged(p_versionKey:String):
	controller.call("_onNewProjectVersionChanged", p_versionKey)
