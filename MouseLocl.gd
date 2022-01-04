extends Label


func _process(_delta):
	self.text = String(get_viewport().get_mouse_position())
