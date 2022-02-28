extends Node2D


export var texture: Texture;

export var texture1: Texture;

var rid: RID;
# Called when the node enters the scene tree for the first time.
func _ready():
	if(texture.get_rid() == texture1.get_rid()):
		print("Texture and Texture1 are the same!")
		return;
	if(texture != null):
		rid = VisualServer.canvas_item_create();
		VisualServer.canvas_item_set_parent(rid,get_canvas_item());
		VisualServer.canvas_item_add_texture_rect(rid,Rect2(0,0,128,128),texture.get_rid(),true);
	pass # Replace with function body.
