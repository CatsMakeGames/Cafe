#generates atlas texture resources

#path to the source image file
source_file_path = "res://Art_Pixel/Floors/"
#name of the source image itself
source_filename = "Room_Builder_Floors_32x32.png"
tile_size = 32
#total count of the sets on line
line_count = 4
#total count of the sets on row
row_count = 18
#how many tiles are in one block 
block_tile_hor_count = 4
#how many tiles are in one block 
block_tile_ver_count = 2
#id of the start
start_y = tile_size * 3
start_x = tile_size
#we start with 0 in this case because the atlas used has shadows as first object
id = 0
print("Generating files...\n")
for x in range(start_x, line_count * tile_size * block_tile_hor_count, tile_size*block_tile_hor_count):
    for y in range(start_y, row_count * tile_size * block_tile_ver_count, tile_size*block_tile_ver_count):
        with open(("./generated/FloorTexture{}.tres").format(id), 'w') as f:
            f.write(("""
[gd_resource type="AtlasTexture" load_steps=2 format=2]

[ext_resource path="{}{}" type="Texture" id=1]

[resource]
atlas = ExtResource( 1 )
region = Rect2( {}, {}, {}, {} )""").format(source_file_path, source_filename, x, y, tile_size, tile_size))
            print(("./generated/FloorTexture{}.tres").format(id))
            id += 1
