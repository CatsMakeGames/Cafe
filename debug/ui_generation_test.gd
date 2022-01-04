extends Control


export var Count = 10;
export var DebugText = "10 items"
export var ItemsPerLine = 5;

func _ready():
	printraw("Generation... [")
	#this is meant to replicate how ui it constructed in og project
	var vBox:VBoxContainer = get_node("VBoxContainer");
	
	var currentContainer = HBoxContainer.new();
	vBox.add_child(currentContainer);
	currentContainer.rect_min_size = Vector2(192,192);
	for i in Count:
		var button = Button.new();
		button.rect_min_size = Vector2(128,128);
		
		currentContainer.add_child(button);
		if i % ItemsPerLine == 0:
			currentContainer = HBoxContainer.new();
			vBox.add_child(currentContainer);
			currentContainer.rect_min_size = Vector2(128,192);
		printraw("*")
	printraw("]")
	return 
