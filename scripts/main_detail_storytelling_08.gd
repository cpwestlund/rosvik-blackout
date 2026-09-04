extends "res://scripts/main_world_integrity_07.gd"

# Detail & Storytelling 08 adds the everyday clutter and vehicle detail that makes
# the blackout feel inhabited rather than procedurally empty. Props are anchored
# deliberately to walls, service edges and parking areas instead of scattered.

var _detail_root: Node3D

func _ready() -> void:
	super._ready()
	_add_detail_storytelling()
	print("ROSVIK_DETAIL_STORYTELLING_08_READY")
	print("ROSVIK_VEHICLE_DETAIL_READY")
	print("ROSVIK_WALL_DRESSING_READY")
	print("ROSVIK_ENVIRONMENTAL_STORY_READY")

# -------------------------------------------------------------------------
# VEHICLES: STILL STYLISED, BUT NO LONGER A SINGLE BODY BLOCK
# -------------------------------------------------------------------------
func _add_car(pos: Vector3, color: Color, yaw: float, open_door: bool) -> void:
	if _point_hits_building(Vector2(pos.x,pos.z),2.35):
		_skipped_placements += 1
		return

	var root: Node3D = Node3D.new()
	root.position = pos
	root.rotation.y = yaw
	root.name = "DetailedCar"
	add_child(root)

	var body_mat: StandardMaterial3D = _mat(color,0.68,0.16)
	var darker_body: StandardMaterial3D = _mat(color.darkened(0.14),0.72,0.14)
	var tyre_mat: StandardMaterial3D = _mat(Color("141718"),1.0)
	var hub_mat: StandardMaterial3D = _mat(Color("7a8285"),0.50,0.48)
	var trim_mat: StandardMaterial3D = _mat(Color("242b2f"),0.72,0.20)
	var plate_mat: StandardMaterial3D = _mat(Color("dddccf"),0.88)
	var head_mat: StandardMaterial3D = _mat(Color("e3d7ad"),0.46)
	head_mat.emission_enabled = true
	head_mat.emission = Color("d7c684")
	head_mat.emission_energy_multiplier = 0.18
	var tail_mat: StandardMaterial3D = _mat(Color("8f332c"),0.56)
	tail_mat.emission_enabled = true
	tail_mat.emission = Color("7a201d")
	tail_mat.emission_energy_multiplier = 0.10

	# Lower shell and shaped nose/trunk.
	var lower: MeshInstance3D = _capsule_local(0.54,3.78,body_mat,Vector3(0.0,0.67,0.0),root)
	lower.rotation.z = PI/2.0
	lower.scale.z = 1.46
	var bonnet: MeshInstance3D = _box(Vector3(1.30,0.24,1.58),body_mat,Vector3(1.47,0.88,0.0),root)
	bonnet.rotation.z = -0.10
	var trunk: MeshInstance3D = _box(Vector3(0.88,0.25,1.58),darker_body,Vector3(-1.61,0.90,0.0),root)
	trunk.rotation.z = 0.08

	# Cabin, glazing and pillars.
	var cabin: MeshInstance3D = _capsule_local(0.49,1.96,body_mat,Vector3(-0.24,1.20,0.0),root)
	cabin.rotation.z = PI/2.0
	cabin.scale.z = 1.28
	_box(Vector3(1.58,0.46,1.44),glass_mat,Vector3(-0.23,1.31,0.0),root)
	_box(Vector3(0.10,0.58,1.48),trim_mat,Vector3(0.56,1.27,0.0),root)
	_box(Vector3(0.10,0.58,1.48),trim_mat,Vector3(-1.02,1.27,0.0),root)
	# Side window dividers and door seams.
	for side: float in [-1.0,1.0]:
		_box(Vector3(0.055,0.48,0.035),trim_mat,Vector3(-0.22,1.30,0.75*side),root)
		_box(Vector3(1.05,0.025,0.035),trim_mat,Vector3(-0.25,0.84,0.79*side),root)
		_box(Vector3(0.15,0.10,0.09),body_mat,Vector3(0.53,1.28,0.87*side),root)

	# Wheels get visible hubs and arches read better against the body.
	for xw: float in [-1.38,1.36]:
		for zw: float in [-0.86,0.86]:
			var wheel: MeshInstance3D = _cylinder(0.34,0.26,tyre_mat,Vector3(xw,0.40,zw),root)
			wheel.rotation.x = PI/2.0
			var hub: MeshInstance3D = _cylinder(0.18,0.275,hub_mat,Vector3(xw,0.40,zw),root)
			hub.rotation.x = PI/2.0

	# Bumpers, grille, lamps and plates.
	_box(Vector3(0.14,0.18,1.46),trim_mat,Vector3(2.02,0.56,0.0),root)
	_box(Vector3(0.10,0.14,1.34),trim_mat,Vector3(-2.00,0.58,0.0),root)
	_box(Vector3(0.035,0.18,0.72),trim_mat,Vector3(2.08,0.75,0.0),root)
	for side: float in [-1.0,1.0]:
		_box(Vector3(0.055,0.18,0.30),head_mat,Vector3(2.08,0.82,0.52*side),root)
		_box(Vector3(0.055,0.18,0.28),tail_mat,Vector3(-2.05,0.82,0.50*side),root)
	_box(Vector3(0.045,0.18,0.48),plate_mat,Vector3(2.095,0.58,0.0),root)
	_box(Vector3(0.045,0.18,0.48),plate_mat,Vector3(-2.065,0.58,0.0),root)

	# Winter accumulation is irregular rather than one huge white slab.
	_box(Vector3(1.68,0.045,1.06),packed_snow_mat,Vector3(-0.22,1.73,0.0),root)
	_box(Vector3(0.72,0.035,1.15),packed_snow_mat,Vector3(1.43,1.02,0.0),root)

	_collision_box(root,Vector3(4.35,1.55,1.84),Vector3(0.0,0.77,0.0))

	if open_door:
		var door_hinge: Node3D = Node3D.new()
		door_hinge.position = Vector3(-0.62,0.74,0.90)
		door_hinge.rotation.y = -0.72
		root.add_child(door_hinge)
		_box(Vector3(1.05,0.82,0.075),body_mat,Vector3(0.48,0.42,0.0),door_hinge)
		_box(Vector3(0.66,0.33,0.035),glass_mat,Vector3(0.49,0.67,0.045),door_hinge)
		_box(Vector3(0.17,0.06,0.035),trim_mat,Vector3(0.82,0.37,0.05),door_hinge)
		# Slightly raised bonnet hints at a failed start / inspection.
		var raised_hood: MeshInstance3D = _box(Vector3(1.22,0.08,1.52),body_mat,Vector3(1.56,1.09,0.0),root)
		raised_hood.rotation.z = -0.34

# -------------------------------------------------------------------------
# WALL-ANCHORED CLUTTER / ENVIRONMENTAL STORYTELLING
# -------------------------------------------------------------------------
func _add_detail_storytelling() -> void:
	_detail_root = Node3D.new()
	_detail_root.name = "DetailStorytelling08"
	add_child(_detail_root)

	# School service/front edge: bags, pallet and a snow shovel. They are placed
	# against the exterior wall but away from the entrance circulation.
	_add_trash_bag_cluster(Vector3(10.6,0.0,6.88),0.0,4)
	_add_pallet(Vector3(8.6,0.0,7.05),0.0)
	_add_wall_shovel(Vector3(11.9,0.0,6.86),-0.18)
	_add_utility_cabinet(Vector3(12.55,0.0,4.25),PI/2.0)

	# Sporthall service side: mundane municipal clutter tells the story better than
	# random dramatic debris.
	_add_trash_bag_cluster(Vector3(39.4,0.0,33.45),PI,3)
	_add_recycling_pair(Vector3(42.2,0.0,33.35),PI)
	_add_pallet(Vector3(45.0,0.0,33.55),PI/2.0)

	# Ice hall edge: an abandoned delivery stack and two bags in the lee of a wall.
	_add_pallet(Vector3(39.0,0.0,80.05),0.0)
	_add_cardboard_stack(Vector3(40.7,0.0,80.10),0.0)
	_add_trash_bag_cluster(Vector3(43.0,0.0,80.0),0.0,2)

	# Interior corridor: backpacks and coat hooks sit on a real existing wall.
	var school: Node3D = get_node_or_null("RosviksSkola") as Node3D
	if school != null:
		_add_coat_row(school,Vector3(-11.78,1.20,1.65))
		_add_school_backpack(school,Vector3(-11.62,0.82,1.20),Color("40566a"))
		_add_school_backpack(school,Vector3(-11.62,0.82,2.08),Color("6e4a43"))
		_add_school_backpack(school,Vector3(-11.62,0.82,2.82),Color("586744"))

func _add_trash_bag_cluster(pos: Vector3, yaw: float, count: int) -> void:
	var root: Node3D = Node3D.new()
	root.position = pos
	root.rotation.y = yaw
	_detail_root.add_child(root)
	var bag_mat: StandardMaterial3D = _mat(Color("202427"),0.995)
	for i: int in range(count):
		var x: float = float(i%2)*0.48
		var z: float = float(i/2)*0.38
		var radius: float = 0.25+float(i%3)*0.025
		_sphere_local(radius,bag_mat,Vector3(x,0.27,z),root,Vector3(0.92,1.18,0.88))
		var knot: MeshInstance3D = _cone(0.075,0.14,bag_mat,Vector3(x,0.62,z),root)
		knot.rotation.z = 0.16 if i%2 == 0 else -0.12
		_box(Vector3(0.20,0.018,0.18),packed_snow_mat,Vector3(x,0.54,z-0.02),root)

func _add_pallet(pos: Vector3, yaw: float) -> void:
	var root: Node3D = Node3D.new()
	root.position = pos
	root.rotation.y = yaw
	_detail_root.add_child(root)
	var pallet_mat: StandardMaterial3D = _textured_mat(Color("78604a"),0.98,0.0,"horizontal",64,0.020)
	for z: float in [-0.42,-0.14,0.14,0.42]:
		_box(Vector3(1.35,0.085,0.16),pallet_mat,Vector3(0.0,0.19,z),root)
	for x: float in [-0.50,0.0,0.50]:
		_box(Vector3(0.18,0.16,1.00),pallet_mat,Vector3(x,0.08,0.0),root)

func _add_wall_shovel(pos: Vector3, lean: float) -> void:
	var root: Node3D = Node3D.new()
	root.position = pos
	root.rotation.z = lean
	_detail_root.add_child(root)
	var handle: MeshInstance3D = _cylinder(0.024,1.48,wood_mat,Vector3(0.0,0.84,0.0),root)
	handle.rotation.z = 0.0
	var blade_mat: StandardMaterial3D = _mat(Color("5f6b70"),0.82,0.12)
	var blade: MeshInstance3D = _box(Vector3(0.52,0.38,0.07),blade_mat,Vector3(0.0,0.18,0.0),root)
	blade.rotation.z = 0.02

func _add_utility_cabinet(pos: Vector3, yaw: float) -> void:
	var root: Node3D = Node3D.new()
	root.position = pos
	root.rotation.y = yaw
	_detail_root.add_child(root)
	var cabinet_mat: StandardMaterial3D = _mat(Color("667174"),0.82,0.18)
	_solid_box(Vector3(0.72,1.28,0.34),cabinet_mat,Vector3(0.0,0.64,0.0),root)
	_box(Vector3(0.55,0.02,0.22),dark_mat,Vector3(0.0,0.84,0.18),root)
	_box(Vector3(0.10,0.04,0.04),metal_mat,Vector3(0.25,0.64,0.19),root)
	var warn: StandardMaterial3D = _mat(Color("c5a341"),0.78)
	_box(Vector3(0.20,0.20,0.018),warn,Vector3(-0.16,0.63,0.19),root)

func _add_recycling_pair(pos: Vector3, yaw: float) -> void:
	var root: Node3D = Node3D.new()
	root.position = pos
	root.rotation.y = yaw
	_detail_root.add_child(root)
	for i: int in range(2):
		var x: float = float(i)*0.86
		var bin_mat: StandardMaterial3D = _mat(Color("4b6360") if i == 0 else Color("4f5966"),0.92)
		var body: MeshInstance3D = _capsule_local(0.35,0.98,bin_mat,Vector3(x,0.56,0.0),root)
		body.scale.z = 0.84
		_box(Vector3(0.72,0.12,0.62),dark_mat,Vector3(x,1.10,0.0),root)
		for wx: float in [-0.24,0.24]:
			var wheel: MeshInstance3D = _cylinder(0.10,0.10,dark_mat,Vector3(x+wx,0.12,-0.31),root)
			wheel.rotation.z = PI/2.0

func _add_cardboard_stack(pos: Vector3, yaw: float) -> void:
	var root: Node3D = Node3D.new()
	root.position = pos
	root.rotation.y = yaw
	_detail_root.add_child(root)
	var card: StandardMaterial3D = _textured_mat(Color("9b7856"),0.98,0.0,"horizontal",64,0.025)
	_box(Vector3(0.82,0.55,0.62),card,Vector3(0.0,0.28,0.0),root)
	_box(Vector3(0.68,0.46,0.55),card,Vector3(0.08,0.78,0.02),root)
	_box(Vector3(0.54,0.36,0.48),card,Vector3(-0.06,1.17,-0.02),root)
	var tape: StandardMaterial3D = _mat(Color("776a55"),0.88)
	for y: float in [0.28,0.78,1.17]:
		_box(Vector3(0.08,0.025,0.64),tape,Vector3(0.0,y+0.02,0.0),root)

func _add_coat_row(parent: Node3D, pos: Vector3) -> void:
	var rail_mat: StandardMaterial3D = _mat(Color("64605a"),0.90)
	_box(Vector3(0.08,0.10,2.65),rail_mat,pos,parent)
	for i: int in range(6):
		var z: float = pos.z-1.05+float(i)*0.42
		var hook: MeshInstance3D = _cylinder(0.018,0.17,metal_mat,Vector3(pos.x+0.07,pos.y-0.08,z),parent)
		hook.rotation.z = PI/2.0

func _add_school_backpack(parent: Node3D, pos: Vector3, color: Color) -> void:
	var root: Node3D = Node3D.new()
	root.position = pos
	parent.add_child(root)
	var bag_mat: StandardMaterial3D = _mat(color,0.94)
	var body: MeshInstance3D = _capsule_local(0.28,0.64,bag_mat,Vector3(0.0,0.0,0.0),root)
	body.scale = Vector3(0.78,1.0,0.58)
	var strap: MeshInstance3D = _cylinder(0.018,0.62,dark_mat,Vector3(0.07,0.02,0.20),root)
	strap.rotation.z = 0.18

func _build_ui() -> void:
	super._build_ui()
	var ui_layer: CanvasLayer = null
	for child: Node in get_children():
		if child is CanvasLayer:
			ui_layer = child as CanvasLayer
			break
	if ui_layer == null:
		return
	for child: Node in ui_layer.get_children():
		if child is ColorRect:
			for inner: Node in child.get_children():
				if inner is Label:
					var l: Label = inner as Label
					if l.text.contains("ARCHITECTURAL COHESION 03"):
						l.text = "ROSVIK: BLACKOUT\nWORLD INTEGRITY 07 + DETAIL 08\n\nFootprints • cutaway • detaljer • stämning"
	var badge: Label = Label.new()
	badge.position = Vector2(18.0,158.0)
	badge.text = "DETAIL 08  •  fordon / väggprops / environmental storytelling"
	badge.add_theme_font_size_override("font_size",12)
	badge.add_theme_color_override("font_color",Color("c9d2cd"))
	ui_layer.add_child(badge)
