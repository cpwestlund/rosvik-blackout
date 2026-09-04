extends "res://scripts/main_detail_storytelling_08.gd"

# Road Topology + Render Stability 09
# Roads are now authored as a graph of nodes + trimmed segments. Every junction
# has one dedicated asphalt patch, so road pieces meet instead of stacking on top
# of one another. This removes the obvious broken/T-crossing look and a major
# source of z-fighting.

var _road09_root: Node3D
var _road09_nodes: Dictionary = {}
var _road09_edges: Array[Dictionary] = []
var _road09_half: Dictionary = {}
var _road09_degree: Dictionary = {}
var _road09_line_mat: StandardMaterial3D
var _road09_sidewalk_mat: StandardMaterial3D
var _road09_curb_mat: StandardMaterial3D
var _road09_junctions: int = 0
var _road09_pieces: int = 0

func _ready() -> void:
	super._ready()
	print("ROSVIK_ROAD_TOPOLOGY_09_READY")
	print("ROSVIK_JUNCTION_GRAPH_READY junctions=",_road09_junctions)
	print("ROSVIK_ZFIGHT_CLEANUP_09_READY pieces=",_road09_pieces)
	print("ROSVIK_PEDESTRIAN_COHERENCE_09_READY")

# -------------------------------------------------------------------------
# ONE ROAD GRAPH, ONE SURFACE HEIGHT
# -------------------------------------------------------------------------
func _build_roads() -> void:
	_road09_root = Node3D.new()
	_road09_root.name = "RoadTopology09"
	add_child(_road09_root)

	_road09_line_mat = _mat(Color("c9c4b2"),0.95)
	_road09_sidewalk_mat = _textured_mat(Color("737b7d"),0.98,0.0,"noise",96,0.015)
	_road09_curb_mat = _mat(Color("666e70"),0.97)

	# Public/civic road graph. Nodes split every actual crossing so no two long
	# rectangles ever overlap each other at the same depth.
	_road09_nodes = {
		"WEST": Vector2(-105.0,-30.0),
		"MAIN_X": Vector2(-42.0,-30.0),
		"EAST": Vector2(115.0,-30.0),
		"NORTH": Vector2(-42.0,-92.0),
		"SCHOOL_X": Vector2(-42.0,10.0),
		"SOUTH": Vector2(-42.0,82.0),
		"SCHOOL_TURN": Vector2(31.0,10.0),
		"CIVIC_TURN": Vector2(31.0,36.0),
		"SPORT_TURN": Vector2(80.0,36.0),
		"HALA_X": Vector2(80.0,84.0),
		"HALA_END": Vector2(80.0,90.0),
		"HALA_PARK": Vector2(71.0,84.0)
	}

	_road09_edges = [
		{"a":"WEST","b":"MAIN_X","w":7.2,"line":true},
		{"a":"MAIN_X","b":"EAST","w":7.2,"line":true},
		{"a":"NORTH","b":"MAIN_X","w":6.0,"line":true},
		{"a":"MAIN_X","b":"SCHOOL_X","w":6.0,"line":true},
		{"a":"SCHOOL_X","b":"SOUTH","w":6.0,"line":true},
		{"a":"SCHOOL_X","b":"SCHOOL_TURN","w":5.4,"line":true},
		{"a":"SCHOOL_TURN","b":"CIVIC_TURN","w":4.8,"line":false},
		{"a":"CIVIC_TURN","b":"SPORT_TURN","w":5.0,"line":true},
		{"a":"SPORT_TURN","b":"HALA_X","w":5.2,"line":true},
		{"a":"HALA_X","b":"HALA_END","w":5.2,"line":true},
		{"a":"HALA_PARK","b":"HALA_X","w":4.6,"line":false}
	]

	_compute_road09_junctions()

	# Junction patches first; segments are trimmed exactly to their edges.
	for name: String in _road09_nodes.keys():
		var degree: int = int(_road09_degree.get(name,0))
		if degree < 2:
			continue
		var p: Vector2 = _road09_nodes[name]
		var half: float = float(_road09_half[name])
		_road09_patch(Vector3(p.x,0.0,p.y),half*2.0)
		_road09_junctions += 1

	for edge: Dictionary in _road09_edges:
		_draw_road09_edge(edge)

	# Parking areas are kept deliberately off the road surface, then connected by
	# short driveways instead of overlapping giant asphalt slabs.
	_parking_patch_clean(Vector3(22.0,0.0,20.5),Vector3(11.0,0.060,10.0),4)
	_parking_patch_clean(Vector3(52.0,0.0,44.0),Vector3(27.0,0.060,10.0),8)
	_parking_patch_clean(Vector3(55.0,0.0,85.0),Vector3(30.0,0.060,11.0),10)

	# Small non-overlapping driveway links.
	_road09_piece(Vector2(27.50,20.5),Vector2(28.55,20.5),3.6,false)
	_road09_piece(Vector2(52.0,39.05),Vector2(52.0,38.55),4.0,false)
	_road09_piece(Vector2(70.0,84.0),Vector2(71.0,84.0),4.0,false)

	_build_road09_walkways()
	_build_road09_crossings()

func _compute_road09_junctions() -> void:
	_road09_half.clear()
	_road09_degree.clear()
	for name: String in _road09_nodes.keys():
		_road09_half[name] = 0.0
		_road09_degree[name] = 0
	for edge: Dictionary in _road09_edges:
		var a_name: String = String(edge["a"])
		var b_name: String = String(edge["b"])
		var half: float = float(edge["w"])*0.5
		_road09_half[a_name] = maxf(float(_road09_half[a_name]),half)
		_road09_half[b_name] = maxf(float(_road09_half[b_name]),half)
		_road09_degree[a_name] = int(_road09_degree[a_name])+1
		_road09_degree[b_name] = int(_road09_degree[b_name])+1

func _draw_road09_edge(edge: Dictionary) -> void:
	var a_name: String = String(edge["a"])
	var b_name: String = String(edge["b"])
	var a: Vector2 = _road09_nodes[a_name]
	var b: Vector2 = _road09_nodes[b_name]
	var width: float = float(edge["w"])
	var line: bool = bool(edge["line"])
	var d: Vector2 = b-a
	if d.length() < 0.1:
		return
	var dir: Vector2 = d.normalized()

	# Only multi-edge nodes need a junction patch. Endpoints keep a square endcap.
	var trim_a: float = float(_road09_half[a_name]) if int(_road09_degree[a_name]) >= 2 else 0.0
	var trim_b: float = float(_road09_half[b_name]) if int(_road09_degree[b_name]) >= 2 else 0.0
	var ta: Vector2 = a+dir*trim_a
	var tb: Vector2 = b-dir*trim_b
	if ta.distance_to(tb) < 0.2:
		return
	_road09_piece(ta,tb,width,line)

func _road09_piece(a: Vector2, b: Vector2, width: float, center_line: bool) -> void:
	var d: Vector2 = b-a
	var length: float = d.length()
	if length < 0.1:
		return
	var mid: Vector2 = (a+b)*0.5
	var yaw: float = -atan2(d.y,d.x)
	var road: MeshInstance3D = _box(Vector3(length,0.060,width),asphalt_mat,Vector3(mid.x,0.055,mid.y),_road09_root)
	road.rotation.y = yaw
	_road09_pieces += 1

	# No shoulder mesh underneath the road: that was a recurring coplanar overlap.
	if center_line and width >= 5.0:
		var usable: float = maxf(0.0,length-2.0)
		var count: int = maxi(1,int(usable/7.0))
		for i: int in range(count):
			var t: float = (float(i)+0.5)/float(count)
			var p: Vector2 = a.lerp(b,t)
			var dash: MeshInstance3D = _box(Vector3(1.85,0.010,0.10),_road09_line_mat,Vector3(p.x,0.096,p.y),_road09_root)
			dash.rotation.y = yaw

func _road09_patch(pos: Vector3, size: float) -> void:
	# Slightly oversized relative to the widest incident road, but segments stop at
	# the patch boundary, so no surfaces occupy the same plane.
	_box(Vector3(size,0.060,size),asphalt_mat,pos+Vector3(0.0,0.055,0.0),_road09_root)

func _parking_patch_clean(pos: Vector3, size: Vector3, spaces: int) -> void:
	if _point_hits_building(Vector2(pos.x,pos.z),maxf(size.x,size.z)*0.18):
		push_error("Road Topology 09 blocked parking near building at %s" % pos)
		return
	_box(size,asphalt_mat,pos+Vector3(0.0,0.055,0.0),_road09_root)
	if spaces <= 0:
		return
	var usable_width: float = size.x-1.2
	var spacing: float = usable_width/float(spaces)
	for i: int in range(spaces+1):
		var x: float = -usable_width*0.5+float(i)*spacing
		_box(Vector3(0.055,0.010,size.z*0.34),_road09_line_mat,pos+Vector3(x,0.096,size.z*0.22),_road09_root)

# -------------------------------------------------------------------------
# PEDESTRIAN NETWORK: PATHS STOP AT ROAD EDGES, CROSSINGS HANDLE THE ASPHALT
# -------------------------------------------------------------------------
func _build_road09_walkways() -> void:
	# School-front pavement: between facade and civic road, never laid on asphalt.
	_sidewalk09(Vector2(-35.0,6.82),Vector2(26.5,6.82),1.15)
	# Sporthall pavement along the building-side edge of the z=36 civic road.
	_sidewalk09(Vector2(38.0,32.88),Vector2(67.0,32.88),1.05)
	# HALA pedestrian strip west of the eastern spine.
	_sidewalk09(Vector2(76.10,55.0),Vector2(76.10,79.0),1.05)
	# Entrance connectors only in snow/forecourt areas.
	_sidewalk09(Vector2(-4.45,6.82),Vector2(-4.45,7.25),1.85)
	_sidewalk09(Vector2(50.0,32.88),Vector2(50.0,33.45),1.85)
	_sidewalk09(Vector2(46.5,79.0),Vector2(46.5,81.6),1.65)

func _sidewalk09(a: Vector2, b: Vector2, width: float) -> void:
	var d: Vector2 = b-a
	var length: float = d.length()
	if length < 0.1:
		return
	var mid: Vector2 = (a+b)*0.5
	var path: MeshInstance3D = _box(Vector3(length,0.045,width),_road09_sidewalk_mat,Vector3(mid.x,0.083,mid.y),_road09_root)
	path.rotation.y = -atan2(d.y,d.x)

func _build_road09_crossings() -> void:
	_crossing09(Vector2(-4.45,10.0),0.0,5.4)
	_crossing09(Vector2(52.0,36.0),0.0,5.0)
	_crossing09(Vector2(76.0,84.0),PI/2.0,4.6)

func _crossing09(center: Vector2, yaw: float, road_width: float) -> void:
	for i: int in range(5):
		var offset: float = -1.15+float(i)*0.575
		var stripe: MeshInstance3D = _box(Vector3(0.30,0.010,road_width*0.68),_road09_line_mat,Vector3(center.x+offset,0.098,center.y),_road09_root)
		stripe.rotation.y = yaw

# -------------------------------------------------------------------------
# SAFER CIVIC DRESSING: SIGNS AND STREET FURNITURE LIVE BESIDE ROADS
# -------------------------------------------------------------------------
func _add_world_coherence_details() -> void:
	_coherence_root = Node3D.new()
	_coherence_root.name = "WorldCoherence09"
	add_child(_coherence_root)
	var sign_blue: StandardMaterial3D = _mat(Color("315e79"),0.78,0.05)

	# Roadside sign is outside the carriageway instead of floating across it.
	_solid_cylinder(0.045,2.25,metal_mat,Vector3(27.0,1.125,32.05),_coherence_root)
	_box(Vector3(2.45,0.46,0.08),sign_blue,Vector3(27.0,2.02,32.05),_coherence_root)
	_label3d("SPORTHALL  →",Vector3(27.0,2.02,32.10),_coherence_root,12)

	# Small snowbanks sit outside road/parking edges. No bus shelter until its exact
	# placement can be authored without clipping school, pavement or carriageway.
	for p: Vector3 in [Vector3(36.8,0.0,49.6),Vector3(67.0,0.0,49.4),Vector3(38.0,0.0,91.5),Vector3(71.5,0.0,91.2)]:
		_add_snowbank(p)

# No visible electrical strobe in this pass: remaining flicker should now be easy
# to identify as geometry rather than being confused with a deliberately bad lamp.
func _update_flicker() -> void:
	if _flicker_light != null:
		_flicker_light.light_energy = 0.20

# -------------------------------------------------------------------------
# VALIDATION AGAINST THE ACTUAL GRAPH DRAWN IN THIS BUILD
# -------------------------------------------------------------------------
func _validate_world_integrity() -> void:
	var invalid: int = 0
	for edge: Dictionary in _road09_edges:
		var a: Vector2 = _road09_nodes[String(edge["a"])]
		var b: Vector2 = _road09_nodes[String(edge["b"])]
		var width: float = float(edge["w"])
		if _segment_hits_building(a,b,width*0.5):
			invalid += 1
	if invalid > 0:
		push_error("Road Topology 09 failed: %d graph edges intersect structure footprints" % invalid)
