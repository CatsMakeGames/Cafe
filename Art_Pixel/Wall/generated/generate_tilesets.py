#i just copied existing tilemap and replaced stuff that was different between files with code :O
import linecache

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
id = 0
for y in range(0,row_count):
    for x in range(0,line_count):
        with open(("./Wall_Tileset{}.tres").format(id), 'w') as f:
            f.write(("""
[gd_resource type="TileSet" load_steps=2 format=2]

[ext_resource path="{}{}" type="Texture" id=1]

[resource]
0/name = "{} 0"
0/texture = ExtResource( 1 )
0/tex_offset = Vector2( 0, 0 )
0/modulate = Color( 1, 1, 1, 1 )
0/region = Rect2( {}, {}, {}, {} )
0/tile_mode = 2
0/autotile/icon_coordinate = Vector2( 0, 0 )
0/autotile/tile_size = Vector2( 32, 32 )
0/autotile/spacing = 0
0/autotile/occluder_map = [  ]
0/autotile/navpoly_map = [  ]
0/autotile/priority_map = [  ]
0/autotile/z_index_map = [  ]
0/occluder_offset = Vector2( 0, 0 )
0/navigation_offset = Vector2( 0, 0 )
0/shape_offset = Vector2( 0, 0 )
0/shape_transform = Transform2D( 1, 0, 0, 1, 0, 0 )
0/shape_one_way = false
0/shape_one_way_margin = 0.0
0/shapes = [  ]
0/z_index = 0
""").format(source_file_path,source_filename,source_filename,x * width,y * height,width,height))
            id += 1
