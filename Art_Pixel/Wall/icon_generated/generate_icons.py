#path to the source image file
source_file_path = "res://Art_Pixel/Wall/"
#name of the source image itself
source_filename = "Room_Builder_3d_walls_32x32.png"
#total count of the sets on line
line_count = 3
#total count of the sets on row
row_count = 8
tile_size = 32
#how many tiles are horizontally in the set
tileset_ver_count = 7
#how many tiles are vertically in the set
tileset_hor_count = 8
#total width of the set
width = tileset_hor_count * tile_size
#total height of the set
height = tileset_ver_count * tile_size

#start location
start_x = 96
start_y = 64

ind = 0
for y in range(start_y,row_count * height,7 * tile_size):
    for x in range(start_x,line_count * width,8 * tile_size):
        with open((".//WallIcon{}.tres").format(ind), 'w') as f:
            f.write(("""
[gd_resource type="AtlasTexture" load_steps=2 format=2]

[ext_resource path="{}{}" type="Texture" id=1]

[resource]
atlas = ExtResource( 1 )
region = Rect2( {}, {}, 64, 64 )

""").format(source_file_path,source_filename,x,y))
            ind += 1