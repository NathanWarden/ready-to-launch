extends Control


func show_message(message:String):
	get_node("Panel/Label").text = message
	visible = true
