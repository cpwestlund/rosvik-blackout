extends "res://scripts/main_spatial_sanity_06.gd"

# Tiny corrective layer discovered by the Spatial Sanity validator itself. The
# public road runs one metre farther south so its full asphalt width clears the
# sporthall entrance envelope instead of merely clearing the wall centreline.

func _enter_tree() -> void:
	_road_layout[3] = [Vector2(31.0,10.0),Vector2(31.0,36.0),4.8,false]
	_road_layout[4] = [Vector2(31.0,36.0),Vector2(80.0,36.0),5.0,true]
	_road_layout[5] = [Vector2(80.0,36.0),Vector2(80.0,90.0),5.2,true]

func _ready() -> void:
	super._ready()
	print("ROSVIK_SPATIAL_SANITY_06_1_READY")

func _build_roads() -> void:
	_road_marking_mat = _mat(Color("c7c4b5"),0.94)
	_sidewalk_mat = _textured_mat(Color("777f80"),0.98,0.0,"noise",96,0.018)
	_reflector_mat = _mat(Color("d7d3c0"),0.76)

	for definition: Array in _road_layout:
		var av: Vector2 = definition[0]
		var bv: Vector2 = definition[1]
		var width: float = float(definition[2])
		var centre_line: bool = bool(definition[3])
		_road_segment(Vector3(av.x,0.0,av.y),Vector3(bv.x,0.0,bv.y),width,centre_line)

	_parking_patch_safe(Vector3(23.5,0.0,18.5),Vector3(12.0,0.055,12.0),0.0,4)
	# Sporthall parking is moved with the road to prevent overlapping asphalt planes.
	_parking_patch_safe(Vector3(52.0,0.0,43.0),Vector3(27.0,0.055,10.0),0.0,8)
	_parking_patch_safe(Vector3(55.0,0.0,85.0),Vector3(30.0,0.055,11.0),0.0,10)

	_walk_strip(Vector3(-4.45,0.0,8.7),Vector3(-4.45,0.0,13.0),2.0)
	_walk_strip(Vector3(15.0,0.0,21.0),Vector3(28.5,0.0,21.0),1.8)
	_walk_strip(Vector3(28.5,0.0,21.0),Vector3(31.0,0.0,29.0),1.8)
	_walk_strip(Vector3(50.0,0.0,32.0),Vector3(50.0,0.0,36.0),2.0)
	_walk_strip(Vector3(46.5,0.0,77.8),Vector3(46.5,0.0,84.0),1.9)

	_crossing(Vector3(-4.45,0.0,10.0),Vector3(1.0,0.0,0.0),5.4)
	_crossing(Vector3(31.0,0.0,36.0),Vector3(0.0,0.0,1.0),5.0)
	_crossing(Vector3(80.0,0.0,84.0),Vector3(1.0,0.0,0.0),5.2)
