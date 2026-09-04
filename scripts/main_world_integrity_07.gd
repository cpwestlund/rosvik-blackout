extends "res://scripts/main_spatial_sanity_06_1.gd"

# World Integrity 07 turns the remaining placement rules into one explicit world
# grammar. Roads are validated against every authored building, background houses
# are created from known safe footprints, and school cutaway keeps readable wall
# boundaries instead of visually deleting whole rooms.

var _world_structure_zones: Array[Rect2] = []
var _integrity_root: Node3D
var _background_house_specs: Array[Dictionary] = []

func _enter_tree() -> void:
	super._enter_tree()
	_background_house_specs = [
		{"pos":Vector3(-25.0,0.0,-12.0),"color":Color("8c5b4d"),"garage":true},
		{"pos":Vector3(-8.0,0.0,-15.0),"color":Color("b7a268"),"garage":false},
		{"pos":Vector3(18.0,0.0,-15.0),"color":Color("657b85"),"garage":true},
		{"pos":Vector3(34.0,0.0,-12.0),"color":Color("8a7965"),"garage":false},
		{"pos":Vector3(-61.0,0.0,25.0),"color":Color("667d68"),"garage":true},
		{"pos":Vector3(-63.0,0.0,-5.0),"color":Color("835f52"),"garage":false},
		{"pos":Vector3(94.0,0.0,18.0),"color":Color("6f7f87"),"garage":true},
		{"pos":Vector3(95.0,0.0,56.0),"color":Color("98836d"),"garage":false}
	]
	_world_structure_zones.clear()
	for zone: Rect2 in _building_zones:
		_world_structure_zones.append(zone)
	for spec: Dictionary in _background_house_specs:
		var p: Vector3 = spec["pos"]
		# Conservative envelope includes small garage/porch projections.
		_world_structure_zones.append(Rect2(Vector2(p.x-6.4,p.z-4.8),Vector2(12.8,9.6)))

func _ready() -> void:
	super._ready()
	_validate_world_integrity()
	print("ROSVIK_WORLD_INTEGRITY_07_READY")
	print("ROSVIK_STRUCTURE_FOOTPRINTS_READY zones=",_world_structure_zones.size())
	print("ROSVIK_CUTAWAY_BOUNDARIES_READY")
	print("ROSVIK_BACKGROUND_SAFE_READY houses=",_background_house_specs.size())

# -------------------------------------------------------------------------
# FULL STRUCTURE FOOTPRINTS
# -------------------------------------------------------------------------
func _point_hits_building(point: Vector2, margin: float) -> bool:
	for zone: Rect2 in _world_structure_zones:
		var expanded: Rect2 = Rect2(zone.position-Vector2.ONE*margin,zone.size+Vector2.ONE*margin*2.0)
		if expanded.has_point(point):
			return true
	return false

func _segment_hits_building(a: Vector2, b: Vector2, margin: float) -> bool:
	var length: float = a.distance_to(b)
	var steps: int = maxi(2,int(ceil(length/0.55)))
	for i: int in range(steps+1):
		var p: Vector2 = a.lerp(b,float(i)/float(steps))
		if _point_hits_building(p,margin):
			return true
	return false

func _parking_patch_safe(pos: Vector3, size: Vector3, yaw: float, spaces: int) -> void:
	var half: Vector2 = Vector2(size.x,size.z)*0.5
	for zone: Rect2 in _world_structure_zones:
		var expanded: Rect2 = Rect2(zone.position-half,zone.size+half*2.0)
		if expanded.has_point(Vector2(pos.x,pos.z)):
			push_error("World Integrity blocked parking inside structure envelope at %s" % pos)
			return
	_parking_patch(pos,size,yaw,spaces)

# -------------------------------------------------------------------------
# BACKGROUND BUILDINGS: AUTHORED SAFE LOCATIONS, NOT FREE-FLOATING BLOCKS
# -------------------------------------------------------------------------
func _build_background() -> void:
	for spec: Dictionary in _background_house_specs:
		_add_integrity_house(spec["pos"],spec["color"],bool(spec["garage"]))

	# Trees still get variation, but inherited placement guards reject roads and all
	# structure footprints before anything is drawn.
	for i: int in range(46):
		var side: int = -1 if i % 2 == 0 else 1
		_add_tree(Vector3(randf_range(58.0,110.0)*float(side),0.0,randf_range(-72.0,82.0)))
	for i: int in range(24):
		_add_tree(Vector3(randf_range(-88.0,105.0),0.0,randf_range(66.0,92.0)))

func _add_integrity_house(pos: Vector3, color: Color, garage: bool) -> void:
	var root: Node3D = Node3D.new()
	root.position = pos
	add_child(root)
	var wall: StandardMaterial3D = _textured_mat(color,0.94,0.0,"horizontal",64,0.030)
	var garage_wall: StandardMaterial3D = _textured_mat(color.darkened(0.04),0.95,0.0,"horizontal",64,0.028)
	var trim: StandardMaterial3D = _mat(Color("d1d0c7"),0.92)
	var dark_roof: StandardMaterial3D = _textured_mat(Color("343b3f"),0.90,0.04,"noise",64,0.025)

	_solid_box(Vector3(10.0,3.0,7.0),wall,Vector3(0.0,1.5,0.0),root)
	# Two pitched roof planes remove the flat-box silhouette while keeping geometry cheap.
	var roof_l: MeshInstance3D = _box(Vector3(10.8,0.28,4.15),dark_roof,Vector3(0.0,3.42,-1.78),root)
	roof_l.rotation.x = -0.24
	var roof_r: MeshInstance3D = _box(Vector3(10.8,0.28,4.15),dark_roof,Vector3(0.0,3.42,1.78),root)
	roof_r.rotation.x = 0.24
	var snow_l: MeshInstance3D = _box(Vector3(10.55,0.08,3.95),packed_snow_mat,Vector3(0.0,3.59,-1.72),root)
	snow_l.rotation.x = -0.24
	var snow_r: MeshInstance3D = _box(Vector3(10.55,0.08,3.95),packed_snow_mat,Vector3(0.0,3.59,1.72),root)
	snow_r.rotation.x = 0.24

	_add_window(root,Vector3(-2.65,1.65,3.56),Vector3(1.35,1.05,0.10))
	_add_window(root,Vector3(0.55,1.65,3.56),Vector3(1.35,1.05,0.10))
	_box(Vector3(1.10,2.08,0.12),dark_mat,Vector3(3.35,1.05,3.57),root)
	_box(Vector3(1.26,0.10,0.28),trim,Vector3(3.35,2.10,3.62),root)
	_solid_box(Vector3(0.42,1.00,0.42),_mat(Color("5e5750"),0.94),Vector3(-2.9,3.70,-0.8),root)

	if garage:
		var garage_root: Node3D = Node3D.new()
		garage_root.position = Vector3(6.25,0.0,-0.4)
		root.add_child(garage_root)
		_solid_box(Vector3(3.5,2.45,5.4),garage_wall,Vector3(0.0,1.225,0.0),garage_root)
		_box(Vector3(3.15,2.05,0.12),_mat(Color("555f63"),0.90),Vector3(0.0,1.05,2.76),garage_root)
		_box(Vector3(3.8,0.24,5.8),dark_roof,Vector3(0.0,2.56,0.0),garage_root)
		_box(Vector3(3.55,0.08,5.55),packed_snow_mat,Vector3(0.0,2.72,0.0),garage_root)

# -------------------------------------------------------------------------
# CUTAWAY: REMOVE OCCLUSION, NOT THE IDEA OF A WALL
# -------------------------------------------------------------------------
func _add_spatial_repairs() -> void:
	_sanity_root = Node3D.new()
	_sanity_root.name = "WorldIntegrity07"
	add_child(_sanity_root)
	_integrity_root = _sanity_root
	var stub_mat: StandardMaterial3D = _textured_mat(Color("9fa7a7"),0.95,0.0,"horizontal",64,0.012)
	var cap_mat: StandardMaterial3D = _mat(Color("727a7b"),0.94)

	# 1.12 m cutaway walls are tall enough to read as actual room boundaries but low
	# enough for the isometric camera to see desks and the player over them.
	_add_cutaway_stub_x(-12.0,-5.85,6.08,1.12,stub_mat,cap_mat)
	_add_cutaway_stub_x(-3.05,13.0,6.08,1.12,stub_mat,cap_mat)
	_add_cutaway_stub_z(13.0,-6.12,6.12,1.12,stub_mat,cap_mat)

	# Entrance jambs stay legible when the rest of the front facade cuts away.
	_box(Vector3(0.18,2.55,0.34),cap_mat,Vector3(-5.94,1.28,6.10),_integrity_root)
	_box(Vector3(0.18,2.55,0.34),cap_mat,Vector3(-2.96,1.28,6.10),_integrity_root)

	# Edge cues are offset vertically from asphalt so they cannot shimmer.
	var curb_mat: StandardMaterial3D = _mat(Color("686f70"),0.96)
	_box(Vector3(49.0,0.09,0.16),curb_mat,Vector3(55.5,0.115,33.28),_integrity_root)
	_box(Vector3(49.0,0.09,0.16),curb_mat,Vector3(55.5,0.115,38.72),_integrity_root)

func _add_cutaway_stub_x(x0: float, x1: float, z: float, h: float, wall_mat: Material, cap_mat: Material) -> void:
	var length: float = x1-x0
	var center_x: float = (x0+x1)*0.5
	_box(Vector3(length,h,0.18),wall_mat,Vector3(center_x,h*0.5,z),_integrity_root)
	_box(Vector3(length+0.02,0.055,0.21),cap_mat,Vector3(center_x,h+0.025,z),_integrity_root)

func _add_cutaway_stub_z(x: float, z0: float, z1: float, h: float, wall_mat: Material, cap_mat: Material) -> void:
	var length: float = z1-z0
	var center_z: float = (z0+z1)*0.5
	_box(Vector3(0.18,h,length),wall_mat,Vector3(x,h*0.5,center_z),_integrity_root)
	_box(Vector3(0.21,0.055,length+0.02),cap_mat,Vector3(x,h+0.025,center_z),_integrity_root)

func _update_school_cutaway() -> void:
	if player == null:
		return
	var p: Vector3 = player.global_position
	var inside: bool = p.x > -11.85 and p.x < 12.75 and p.z > -6.05 and p.z < 6.02
	_inside_school = inside
	for node: Node3D in _arch_cutaway:
		if is_instance_valid(node):
			node.visible = not inside
	for node: Node3D in _cutaway_floaters:
		if is_instance_valid(node):
			node.visible = not inside
	if _integrity_root != null:
		_integrity_root.visible = true

func _update_camera(delta: float) -> void:
	if player == null or camera == null:
		return
	var p: Vector3 = player.global_position
	var lead: Vector3 = Vector3(player.velocity.x,0.0,player.velocity.z)
	if lead.length() > 0.15:
		lead = lead.normalized()
	var inside: bool = p.x > -11.85 and p.x < 12.75 and p.z > -6.05 and p.z < 6.02
	var focus: Vector3 = p+Vector3(0.0,0.95,0.0)
	if inside:
		focus += lead*0.32
		target_camera_pos = focus+Vector3(6.8,8.8,7.1)
		camera.fov = lerp(camera.fov,40.5,1.0-exp(-5.0*delta))
	else:
		focus += lead*1.25
		target_camera_pos = focus+Vector3(11.2,7.8,11.2)
		camera.fov = lerp(camera.fov,43.0,1.0-exp(-4.0*delta))
	camera.global_position = camera.global_position.lerp(target_camera_pos,1.0-exp(-5.2*delta))
	camera.look_at(focus,Vector3.UP)

func _validate_world_integrity() -> void:
	var invalid: int = 0
	for definition: Array in _road_layout:
		var a: Vector2 = definition[0]
		var b: Vector2 = definition[1]
		var width: float = float(definition[2])
		if _segment_hits_building(a,b,width*0.5):
			invalid += 1
	if invalid > 0:
		push_error("World Integrity 07 failed: %d roads intersect structure footprints" % invalid)
