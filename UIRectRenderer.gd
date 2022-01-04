extends Control

export var Draw = true;

export var color: Color = Color(255,0,0,255);
# Called when the node enters the scene tree for the first time.
func _ready():
	pass # Replace with function body.


func _draw():
	if Draw:
		draw_rect(self.get_rect(),color);
	return;
