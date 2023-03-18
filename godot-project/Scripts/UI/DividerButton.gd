extends Button

var majorMinorVersion:String

func set_data(p_majorMinorVersion:String):
	majorMinorVersion = p_majorMinorVersion


func _onDividerButtonPressed():
	get_parent().update_divider_button(majorMinorVersion)
