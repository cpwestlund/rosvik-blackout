extends "res://scripts/main_world_coherence_05.gd"

# Spatial Sanity 06: one explicit world grammar for roads, buildings, cars and
# background dressing. The goal is to stop visual progress from being undermined
# by impossible placement: roads through buildings, cars indoors and cutaway
# props floating without a wall.

var _sanity_root: Node3D
var _cutaway_floaters: Array[Node3D] = []
var _validated_roads: int = 0
var _skipped_placements: int = 0

# x/z rectangles in world metres. These are conservative public-building masks,
# not exact private property boundaries.
var _building_zones: Array[Rect2] = [
	Rect2(-20.2,-6.6,40.4,13.1),   # Rosviks skola core + annex envelope
	Rect2(37.6,9.4,29.0,23.4),    # Rosvik Sporthall incl. entrance projection
	Rect2(35.4,51.9,39.4,27.4)    # HALA Hallen incl. public entrance
]

# Roads are declared once and reused both for rendering and placement guards.
# Each entry: [Vector2 start, Vector2 end, width, centre line].
var _road_layout: Array = [
	[Vector2(-105.0,-30.0),Vector2(115.0,-30.0),7.2,true],
	[Vector2(-42.0,-92.0),Vector2(-42.0,82.0),6.0,true],
	# School access remains close to the school, but terminates before the sporthall.
	[Vector2(-42.0,10.0),Vector2(31.0,10.0),5.4,true],
	# The civic route turns south before the sporthall instead of cutting through it.
	[Vector2(31.0,10.0),Vector2(31.0,35.0),4.8,false],
	[Vector2(31.0,35.0),Vector2(80.0,35.0),5.0,true],
	# Eastern spine passes outside both public halls.
	[Vector2(80.0,35.0),Vector2(80.0,90.0),5.2,true],
	# HALA parking access comes in from the east, south of the building shell.
	[Vector2(80.0,84.0),Vector2(71.0,84.0),4.6,false]
]

func _ready() -> void:
	super._ready()
	_register_cutaway_floaters()
	_add_spatial_repairs()
	_validate_spatial_layout()
	print("ROSVIK_SPATIAL_SANITY_06_READY")
	print("ROSVIK_BUILDING_MASKS_READY zones=",_building_zones.size())
	print("ROSVIK_SAFE_PLACEMENT_READY skipped=",_skipped_placements)
	print("ROSVIK_CUTAWAY_REPAIR_READY floaters=",_cutaway_floaters.size())
	print("ROSVIK_ROAD_AVOIDANCE_READY roads=",_validated_roads)

# -------------------------------------------------------------------------
# ROAD NETWORK: EVERY SEGMENT IS CHECKED AGAINST BUILDING FOOTPRINTS
# -------------------------------------------------------------------------
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

	# Parking is deliberately outside the building masks and aligned with entrances.
	_parking_patch_safe(Vector3(23.5,0.0,18.5),Vector3(12.0,0.055,12.0),0.0,4)
	_parking_patch_safe(Vector3(52.0,0.0,40.5),Vector3(27.0,0.055,10.0),0.0,8)
	_parking_patch_safe(Vector3(55.0,0.0,85.0),Vector3(30.0,0.055,11.0),0.0,10)

	# Pedestrian circulation has its own layer and never masquerades as a road.
	_walk_strip(Vector3(-4.45,0.0,8.7),Vector3(-4.45,0.0,13.0),2.0)
	_walk_strip(Vector3(15.0,0.0,21.0),Vector3(28.5,0.0,21.0),1.8)
	_walk_strip(Vector3(28.5,0.0,21.0),Vector3(31.0,0.0,29.0),1.8)
	_walk_strip(Vector3(50.0,0.0,32.0),Vector3(50.0,0.0,35.0),2.0)
	_walk_strip(Vector3(46.5,0.0,77.8),Vector3(46.5,0.0,84.0),1.9)

	_crossing(Vector3(-4.45,0.0,10.0),Vector3(1.0,0.0,0.0),5.4)
	_crossing(Vector3(31.0,0.0,35.0),Vector3(0.0,0.0,1.0),5.0)
	_crossing(Vector3(80.0,0.0,84.0),Vector3(1.0,0.0,0.0),5.2)

func _road_segment(a: Vector3, b: Vector3, width: float, center_line: bool) -> void:
	if _segment_hits_building(Vector2(a.x,a.z),Vector2(b.x,b.z),width*0.5):
		push_error("Spatial sanity blocked road through building: %s -> %s" % [a,b])
		return
	_validated_roads += 1
	super._road_segment(a,b,width,center_line)

func _parking_patch_safe(pos: Vector3, size: Vector3, yaw: float, spaces: int) -> void:
	# Parking patches in this pass are axis-aligned. Reject them if any corner falls
	# inside a public-building exclusion mask.
	var half: Vector2 = Vector2(size.x,size.z)*0.5
	for zone: Rect2 in _building_zones:
		var expanded: Rect2 = Rect2(zone.position-half,zone.size+half*2.0)
		if expanded.has_point(Vector2(pos.x,pos.z)):
			push_error("Spatial sanity blocked parking inside building envelope at %s" % pos)
			return
	_parking_patch(pos,size,yaw,spaces)

# -------------------------------------------------------------------------
# PLACEMENT POLICY: BACKGROUND AND VEHICLES RESPECT ROADS + BUILDINGS
# -------------------------------------------------------------------------
func _add_house(pos: Vector3, color: Color, yaw: float) -> void:
	if not _placement_clear(Vector2(pos.x,pos.z),5.2,true):
		_skipped_placements += 1
		return
	super._add_house(pos,color,yaw)

func _add_tree(pos: Vector3) -> void:
	if not _placement_clear(Vector2(pos.x,pos.z),1.15,true):
		_skipped_placements += 1
		return
	super._add_tree(pos)

func _add_car(pos: Vector3, color: Color, yaw: float, open_door: bool) -> void:
	# Cars may stand on roads/parking, but never inside a building volume.
	if _point_hits_building(Vector2(pos.x,pos.z),2.35):
		_skipped_placements += 1
		return
	super._add_car(pos,color,yaw,open_door)

func _build_props() -> void:
	# Explicit outdoor parking points. The old first car was at (-7,-5), which is
	# literally inside the school footprint and caused the "car in classroom" bug.
	_add_car(Vector3(23.0,0.0,17.5),Color("56646e"),0.06,true)
	_add_car(Vector3(28.0,0.0,20.5),Color("836b59"),PI,false)
	_add_car(Vector3(45.0,0.0,40.5),Color("6d6259"),0.02,false)
	_add_car(Vector3(57.0,0.0,40.5),Color("445866"),PI,false)
	_add_car(Vector3(48.0,0.0,85.0),Color("5b6066"),0.03,false)
	_add_car(Vector3(61.0,0.0,85.0),Color("596c72"),PI,false)
	_add_lamp(Vector3(-15.0,0.0,10.0))
	_add_lamp(Vector3(25.0,0.0,10.0))
	_add_lamp(Vector3(39.0,0.0,35.0))
	_add_lamp(Vector3(69.0,0.0,35.0))
	_add_lamp(Vector3(80.0,0.0,58.0))
	_add_lamp(Vector3(72.0,0.0,84.0))

func _placement_clear(point: Vector2, radius: float, avoid_roads: bool) -> bool:
	if _point_hits_building(point,radius):
		return false
	if avoid_roads:
		for definition: Array in _road_layout:
			var a: Vector2 = definition[0]
			var b: Vector2 = definition[1]
			var half_width: float = float(definition[2])*0.5
			if _distance_point_to_segment(point,a,b) < half_width+radius:
				return false
	return true

func _point_hits_building(point: Vector2, margin: float) -> bool:
	for zone: Rect2 in _building_zones:
		var expanded: Rect2 = Rect2(zone.position-Vector2.ONE*margin,zone.size+Vector2.ONE*margin*2.0)
		if expanded.has_point(point):
			return true
	return false

func _segment_hits_building(a: Vector2, b: Vector2, margin: float) -> bool:
	var length: float = a.distance_to(b)
	var steps: int = maxi(2,int(ceil(length/0.75)))
	for i: int in range(steps+1):
		var p: Vector2 = a.lerp(b,float(i)/float(steps))
		if _point_hits_building(p,margin):
			return true
	return false

func _distance_point_to_segment(p: Vector2, a: Vector2, b: Vector2) -> float:
	var ab: Vector2 = b-a
	var denom: float = ab.length_squared()
	if denom < 0.0001:
		return p.distance_to(a)
	var t: float = clampf((p-a).dot(ab)/denom,0.0,1.0)
	return p.distance_to(a+ab*t)

# -------------------------------------------------------------------------
# APOCALYPSE DRESSING RE-ANCHORED TO THE CURRENT FLOOR PLAN
# -------------------------------------------------------------------------
func _add_apocalypse_dressing() -> void:
	var school: Node3D = get_node_or_null("RosviksSkola") as Node3D
	if school != null:
		var school_lights: Array[OmniLight3D] = []
		_collect_omni_lights(school,school_lights)
		for i: int in range(school_lights.size()):
			school_lights[i].light_energy *= 0.42 if i % 4 == 0 else 0.11
		if school_lights.size() > 2:
			_flicker_light = school_lights[2]

		var emergency: OmniLight3D = OmniLight3D.new()
		emergency.position = Vector3(-4.45,2.25,4.65)
		emergency.light_color = Color("d65f43")
		emergency.light_energy = 0.52
		emergency.omni_range = 3.7
		school.add_child(emergency)

		# Corridor litter is placed on actual corridor floor, clear of door openings.
		var paper_mat: StandardMaterial3D = _mat(Color("c9c5b8"),0.98)
		var papers: Array[Vector3] = [
			Vector3(-10.1,0.092,1.55),Vector3(-8.8,0.092,1.25),Vector3(-5.9,0.092,1.82),
			Vector3(2.1,0.092,1.30),Vector3(4.1,0.092,1.80),Vector3(9.1,0.092,1.30)
		]
		for i: int in range(papers.size()):
			var paper: MeshInstance3D = _box(Vector3(0.30,0.008,0.21),paper_mat,papers[i],school)
			paper.rotation.y = 0.37+float(i)*0.81

		# One classroom is disturbed, but the prop is fully inside the room.
		_add_toppled_school_chair(school,Vector3(-9.65,0.10,-3.75))
		var bag_mat: StandardMaterial3D = _mat(Color("242a2d"),0.99)
		_sphere_local(0.30,bag_mat,Vector3(11.70,0.30,5.20),school,Vector3(0.92,1.18,0.84))
		_sphere_local(0.23,bag_mat,Vector3(11.15,0.23,5.35),school,Vector3(1.04,1.06,0.90))
		var bucket_mat: StandardMaterial3D = _mat(Color("8b3e32"),0.84,0.05)
		_cylinder(0.24,0.40,bucket_mat,Vector3(4.85,0.21,2.18),school)
		var mop: MeshInstance3D = _cylinder(0.023,1.48,wood_mat,Vector3(5.02,0.88,2.20),school)
		mop.rotation.z = 0.20

		# Emergency note is anchored to a wall that remains visible from the corridor.
		var warning_mat: StandardMaterial3D = _mat(Color("a87135"),0.90)
		_box(Vector3(1.35,0.68,0.030),warning_mat,Vector3(-11.82,1.58,1.65),school)
		_label3d("STRÖM BORTA\n16:08",Vector3(-11.78,1.58,1.68),school,12)

	# Exterior disorder is kept on shoulders/parking, never inside public buildings.
	var cone_mat: StandardMaterial3D = _mat(Color("b85e2d"),0.88)
	for p: Vector3 in [Vector3(26.5,0.0,12.8),Vector3(28.0,0.0,13.4),Vector3(48.0,0.0,36.8)]:
		_cone(0.18,0.55,cone_mat,p+Vector3(0.0,0.275,0.0),self)
	var broken_lamp: Node3D = Node3D.new()
	broken_lamp.position = Vector3(24.5,0.0,13.6)
	broken_lamp.rotation.z = 0.30
	add_child(broken_lamp)
	_cylinder(0.06,3.8,dark_mat,Vector3(0.0,1.9,0.0),broken_lamp)
	_box(Vector3(0.62,0.08,0.12),dark_mat,Vector3(0.28,3.65,0.0),broken_lamp)

# -------------------------------------------------------------------------
# CUTAWAY REPAIR: KEEP ROOM BOUNDARIES LEGIBLE AND HIDE WALL-ATTACHED FLOATERS
# -------------------------------------------------------------------------
func _register_cutaway_floaters() -> void:
	_cutaway_floaters.clear()
	var school: Node3D = get_node_or_null("RosviksSkola") as Node3D
	if school == null:
		return
	_collect_front_visuals(school,school)

func _collect_front_visuals(node: Node, school: Node3D) -> void:
	for child: Node in node.get_children():
		if child is VisualInstance3D and child is Node3D:
			var n: Node3D = child as Node3D
			var lp: Vector3 = school.to_local(n.global_position)
			var on_cutaway_face: bool = (lp.z > 6.10 and lp.y > 0.35) or (lp.x > 12.72 and lp.y > 0.35)
			if on_cutaway_face and not _belongs_to_door_hinge(n) and not _arch_cutaway.has(n):
				_cutaway_floaters.append(n)
		_collect_front_visuals(child,school)

func _belongs_to_door_hinge(node: Node) -> bool:
	var cursor: Node = node
	while cursor != null:
		if String(cursor.name).begins_with("DoorHinge"):
			return true
		cursor = cursor.get_parent()
	return false

func _add_spatial_repairs() -> void:
	_sanity_root = Node3D.new()
	_sanity_root.name = "SpatialSanity06"
	add_child(_sanity_root)
	var stub_mat: StandardMaterial3D = _textured_mat(Color("9da6a7"),0.95,0.0,"horizontal",64,0.014)
	# Low inside stubs remain when camera-facing exterior walls cut away, so rooms
	# still have a readable boundary instead of looking like missing geometry.
	_box(Vector3(6.10,0.52,0.16),stub_mat,Vector3(-8.90,0.26,6.07),_sanity_root)
	_box(Vector3(15.95,0.52,0.16),stub_mat,Vector3(4.98,0.26,6.07),_sanity_root)
	_box(Vector3(0.16,0.52,12.10),stub_mat,Vector3(12.82,0.26,0.0),_sanity_root)

	# Simple kerb/edge cues where asphalt meets snow; these are visual, not walls.
	var curb_mat: StandardMaterial3D = _mat(Color("686f70"),0.96)
	_box(Vector3(49.0,0.09,0.16),curb_mat,Vector3(55.5,0.095,32.38),_sanity_root)
	_box(Vector3(49.0,0.09,0.16),curb_mat,Vector3(55.5,0.095,37.62),_sanity_root)

func _update_school_cutaway() -> void:
	if player == null:
		return
	var p: Vector3 = player.global_position
	# Require the player to be genuinely inside the shell; merely standing in the
	# entrance recess no longer makes half the facade vanish.
	var inside: bool = p.x > -11.85 and p.x < 12.75 and p.z > -6.05 and p.z < 6.02
	_inside_school = inside
	for node: Node3D in _arch_cutaway:
		if is_instance_valid(node):
			node.visible = not inside
	for node: Node3D in _cutaway_floaters:
		if is_instance_valid(node):
			node.visible = not inside

# -------------------------------------------------------------------------
# VALIDATION
# -------------------------------------------------------------------------
func _validate_spatial_layout() -> void:
	var bad_roads: int = 0
	for definition: Array in _road_layout:
		var a: Vector2 = definition[0]
		var b: Vector2 = definition[1]
		var width: float = float(definition[2])
		if _segment_hits_building(a,b,width*0.5):
			bad_roads += 1
	if bad_roads > 0:
		push_error("Spatial Sanity 06 validation failed: %d road segments cross buildings" % bad_roads)

func _build_ui() -> void:
	super._build_ui()
	var ui_layer: CanvasLayer = null
	for child: Node in get_children():
		if child is CanvasLayer:
			ui_layer = child as CanvasLayer
	if ui_layer == null:
		return
	var badge: Label = Label.new()
	badge.position = Vector2(18.0,142.0)
	badge.text = "SPATIAL SANITY 06  •  vägar / zoner / placement"
	badge.add_theme_font_size_override("font_size",12)
	badge.add_theme_color_override("font_color",Color("cbd4d0"))
	ui_layer.add_child(badge)
