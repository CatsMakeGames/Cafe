extends Label


func _process(delta):
	self.text = String(get_viewport().get_mouse_position())
