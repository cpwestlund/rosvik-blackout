extends "res://scripts/main_rosvik_accuracy_02.gd"

var _arch_cutaway: Array[Node3D] = []
var _classroom_cutaway: Array[Array] = [[], [], []]
var _school_arch_root: Node3D

func _ready() -> void:
	super._ready()
	print("ROSVIK_ARCHITECTURAL_COHESION_READY")
	print("ROSVIK_DOOR_ALIGNMENT_READY")
	print("ROSVIK_WALL_TOPOLOGY_READY")
	print("ROSVIK_CAMERA_COHESION_READY")

func _build_school() -> void:
	_school_cutaway.clear()
	_arch_cutaway.clear()
	for group: Array in _classroom_cutaway:
		group.clear()

	_school_arch_root = Node3D.new()
	_school_arch_root.name = "RosviksSkola"
	add_child(_school_arch_root)
	var root: Node3D = _school_arch_root

	var wall_mat: StandardMaterial3D = _textured_mat(Color("aab2b3"),0.92,0.0,"horizontal",96,0.026)
	var inner_mat: StandardMaterial3D = _textured_mat(Color("c7c8c1"),0.96,0.0,"horizontal",96,0.010)
	var floor_hall: StandardMaterial3D = _textured_mat(Color("6c7476"),0.94,0.0,"noise",96,0.016)
	var floor_room: StandardMaterial3D = _textured_mat(Color("8c8172"),0.93,0.0,"horizontal",96,0.010)
	var floor_office: StandardMaterial3D = _textured_mat(Color("77746d"),0.94,0.0,"horizontal",96,0.010)
	var door_mat: StandardMaterial3D = _textured_mat(Color("6c5949"),0.88,0.02,"vertical",64,0.018)
	var frame_mat: StandardMaterial3D = _mat(Color("343d42"),0.84,0.08)
	var desk_mat: StandardMaterial3D = _mat(Color("98724f"),0.92)
	var chair_mat: StandardMaterial3D = _mat(Color("465159"),0.90)
	var locker_mat: StandardMaterial3D = _mat(Color("65777e"),0.86,0.08)
	var board_mat: StandardMaterial3D = _mat(Color("e2e3dc"),0.80)
	var rubber_mat: StandardMaterial3D = _textured_mat(Color("30363a"),0.98,0.0,"noise",64,0.025)

	# ---------------------------------------------------------------------
	# ONE FLOOR PLAN, ONE MEASURE SYSTEM
	# Exterior rectangle: x -12..13, z -6.25..6.25. Wall thickness 0.28 m.
	# Corridor: z 0.30..2.70. Three classrooms south, foyer/office north.
	# ---------------------------------------------------------------------
	_wall_z(root,-12.0,-6.25,6.25,4.05,wall_mat,false)
	var east_wall: MeshInstance3D = _wall_z(root,13.0,-6.25,6.25,4.05,wall_mat,true)
	_arch_cutaway.append(east_wall)
	_wall_x(root,-6.25,-12.0,13.0,4.05,wall_mat,false)

	# Front facade is split around the real entrance gap x -5.85..-3.05.
	var front_a: MeshInstance3D = _wall_x(root,6.25,-12.0,-5.85,4.05,wall_mat,true)
	var front_b: MeshInstance3D = _wall_x(root,6.25,-3.05,13.0,4.05,wall_mat,true)
	_arch_cutaway.append(front_a)
	_arch_cutaway.append(front_b)

	# Roof is one coherent cap, not overlapping slabs. Hidden indoors.
	var roof: MeshInstance3D = _box(Vector3(25.5,0.34,12.95),roof_mat,Vector3(0.5,4.23,0.0),root)
	var roof_snow: MeshInstance3D = _box(Vector3(25.1,0.12,12.55),packed_snow_mat,Vector3(0.5,4.46,0.0),root)
	_arch_cutaway.append(roof)
	_arch_cutaway.append(roof_snow)

	# Lower annexes remain exterior volumes and do not intrude into the plan.
	_solid_box(Vector3(7.8,3.10,8.6),wall_mat,Vector3(-16.15,1.55,0.65),root)
	_solid_box(Vector3(6.4,3.35,6.4),_textured_mat(Color("8e989b"),0.92,0.0,"horizontal",96,0.020),Vector3(16.8,1.675,-2.0),root)
	_box(Vector3(8.2,0.28,9.0),roof_mat,Vector3(-16.15,3.22,0.65),root)
	_box(Vector3(6.8,0.28,6.8),roof_mat,Vector3(16.8,3.48,-2.0),root)

	# Continuous floor areas.
	_box(Vector3(24.4,0.055,2.35),floor_hall,Vector3(0.5,0.05,1.50),root)
	_box(Vector3(3.9,0.055,3.55),floor_hall,Vector3(-4.45,0.05,4.45),root)
	_box(Vector3(7.15,0.055,5.85),floor_room,Vector3(-7.70,0.05,-2.72),root)
	_box(Vector3(7.15,0.055,5.85),floor_room,Vector3(-0.50,0.05,-2.72),root)
	_box(Vector3(7.15,0.055,5.85),floor_room,Vector3(6.70,0.05,-2.72),root)
	_box(Vector3(5.4,0.055,3.2),floor_office,Vector3(0.0,0.05,4.35),root)
	_box(Vector3(5.8,0.055,3.2),floor_office,Vector3(8.9,0.05,4.35),root)
	_box(Vector3(3.0,0.022,1.18),rubber_mat,Vector3(-4.45,0.085,5.72),root)

	# ---------------------------------------------------------------------
	# CORRIDOR WALLS WITH DOOR OPENINGS CUT INTO THE SAME WALL SYSTEM
	# ---------------------------------------------------------------------
	# South corridor wall: three rooms, one centered doorway in each bay.
	_wall_x(root,0.30,-12.0,-8.35,3.45,inner_mat,false)
	_wall_x(root,0.30,-7.05,-4.10,3.45,inner_mat,false)
	_wall_x(root,0.30,-4.10,-1.15,3.45,inner_mat,false)
	_wall_x(root,0.30,0.15,3.10,3.45,inner_mat,false)
	_wall_x(root,0.30,3.10,6.05,3.45,inner_mat,false)
	_wall_x(root,0.30,7.35,13.0,3.45,inner_mat,false)
	_doorway_x(root,-7.70,0.30,1.30,"KLASSRUM 1",door_mat,frame_mat,0)
	_doorway_x(root,-0.50,0.30,1.30,"KLASSRUM 2",door_mat,frame_mat,1)
	_doorway_x(root,6.70,0.30,1.30,"KLASSRUM 3",door_mat,frame_mat,2)

	# Classroom divider walls run exactly from south exterior to corridor wall.
	_wall_z(root,-4.10,-6.25,0.30,3.45,inner_mat,false)
	_wall_z(root,3.10,-6.25,0.30,3.45,inner_mat,false)

	# North corridor wall: foyer opening + expedition + staff/resource door.
	_wall_x(root,2.70,-12.0,-5.95,3.45,inner_mat,false)
	_wall_x(root,2.70,-2.95,-0.70,3.45,inner_mat,false)
	_wall_x(root,2.70,0.60,6.45,3.45,inner_mat,false)
	_wall_x(root,2.70,7.75,13.0,3.45,inner_mat,false)
	_doorway_x(root,-4.45,2.70,3.0,"",door_mat,frame_mat,-1,false)
	_doorway_x(root,-0.05,2.70,1.30,"EXPEDITION",door_mat,frame_mat,-1)
	_doorway_x(root,7.10,2.70,1.30,"PERSONAL / WC",door_mat,frame_mat,-1)

	# Foyer walls connect front entrance to corridor without dead-end boxes.
	_wall_z(root,-5.95,2.70,6.25,3.45,inner_mat,false)
	_wall_z(root,-2.95,2.70,6.25,3.45,inner_mat,false)

	# Office / staff rooms north of corridor.
	_wall_z(root,2.70,2.70,6.25,3.45,inner_mat,false)
	_wall_z(root,6.20,2.70,6.25,3.45,inner_mat,false)
	_wall_x(root,6.05,-2.95,2.70,3.45,inner_mat,false)
	_wall_x(root,6.05,2.70,6.20,3.45,inner_mat,false)
	_wall_x(root,6.05,6.20,12.85,3.45,inner_mat,false)

	# Entrance frame and double doors use the exact same 2.8 m facade opening.
	_box(Vector3(3.70,0.24,2.45),roof_mat,Vector3(-4.45,3.06,7.78),root)
	_doorway_x(root,-4.45,6.25,2.80,"ENTRÉ",glass_mat,frame_mat,-1,true)
	_label3d("ROSVIKS SKOLA",Vector3(6.0,3.78,6.50),root,28)

	# Front windows live in their own groups so cutaway never leaves glass floating.
	for xw: float in [-10.0,-8.1,1.8,4.3,6.8,9.3,11.6]:
		var wg: Node3D = _window_group(root,Vector3(xw,2.15,6.44),Vector3(1.30,1.00,0.10))
		_arch_cutaway.append(wg)

	# ---------------------------------------------------------------------
	# FURNITURE - ANCHORED TO ROOMS, CLEAR OF EVERY DOOR SWING
	# ---------------------------------------------------------------------
	for i: int in range(7):
		var xl: float = 0.8 + float(i)*0.55
		_solid_box(Vector3(0.48,1.70,0.34),locker_mat,Vector3(xl,0.85,2.48),root)
	_box(Vector3(2.1,0.92,0.06),_mat(Color("927554"),0.94),Vector3(-11.82,1.55,1.40),root)
	_add_bench(Vector3(-10.3,0.0,1.48),root)

	# Expedition room.
	_solid_box(Vector3(1.45,0.10,0.66),desk_mat,Vector3(0.85,0.75,5.05),root)
	_solid_box(Vector3(0.48,1.80,1.45),_mat(Color("7a817d"),0.92),Vector3(2.35,0.90,4.65),root)
	_box(Vector3(0.07,0.82,1.45),glass_mat,Vector3(2.58,1.55,3.80),root)

	# Staff/resource room.
	_solid_box(Vector3(1.60,0.10,0.72),desk_mat,Vector3(10.55,0.75,5.05),root)
	_solid_box(Vector3(0.50,1.85,1.60),_mat(Color("758078"),0.92),Vector3(12.20,0.93,4.60),root)
	_box(Vector3(1.40,0.85,0.06),board_mat,Vector3(8.45,1.65,6.05),root)

	_build_cohesive_classroom(root,Vector3(-7.70,0.0,-2.72),"KLASSRUM 1",Color("98714f"),0)
	_build_cohesive_classroom(root,Vector3(-0.50,0.0,-2.72),"KLASSRUM 2",Color("8e785d"),1)
	_build_cohesive_classroom(root,Vector3(6.70,0.0,-2.72),"KLASSRUM 3",Color("90704f"),2)

	# Human-scale lighting centered over circulation and rooms.
	var fixture_mat: StandardMaterial3D = _mat(Color("e8e8df"),0.58)
	fixture_mat.emission_enabled = true
	fixture_mat.emission = Color("ffefc6")
	fixture_mat.emission_energy_multiplier = 0.72
	for x_light: float in [-9.0,-5.0,-1.0,3.0,7.0,11.0]:
		_add_ceiling_light(root,Vector3(x_light,3.20,1.50),fixture_mat,0.46)
	for rx: float in [-7.70,-0.50,6.70]:
		_add_ceiling_light(root,Vector3(rx-1.5,3.18,-2.72),fixture_mat,0.52)
		_add_ceiling_light(root,Vector3(rx+1.5,3.18,-2.72),fixture_mat,0.52)
	_add_ceiling_light(root,Vector3(-4.45,3.15,4.55),fixture_mat,0.55)

	# Exterior schoolyard kept from the successful physical/circulation pass.
	_box(Vector3(34.0,0.07,20.0),packed_snow_mat,Vector3(-1.0,0.03,19.0),root)
	_box(Vector3(13.0,0.05,8.0),slush_mat,Vector3(-4.0,0.05,11.5),root)
	_box(Vector3(4.0,0.05,12.0),slush_mat,Vector3(-4.45,0.05,16.0),root)
	_add_fence(Vector3(-18.0,0.0,13.0),Vector3(-6.7,0.0,13.0),root)
	_add_fence(Vector3(-2.3,0.0,13.0),Vector3(15.0,0.0,13.0),root)
	_gate_posts(Vector3(-6.7,0.0,13.0),Vector3(-2.3,0.0,13.0),root)
	_add_fence(Vector3(-18.0,0.0,13.0),Vector3(-18.0,0.0,31.0),root)
	_add_fence(Vector3(15.0,0.0,13.0),Vector3(15.0,0.0,18.8),root)
	_add_fence(Vector3(15.0,0.0,23.1),Vector3(15.0,0.0,31.0),root)
	_gate_posts(Vector3(15.0,0.0,18.8),Vector3(15.0,0.0,23.1),root)
	_add_fence(Vector3(-18.0,0.0,31.0),Vector3(15.0,0.0,31.0),root)
	_add_bench(Vector3(-11.0,0.0,18.0),root)
	_add_goal(Vector3(-9.0,0.0,26.0),root)
	_add_basket(Vector3(8.0,0.0,25.0),root)
	_add_flag(Vector3(11.0,0.0,10.3),root)
	_add_dumpster(Vector3(-16.0,0.0,9.0),root)

func _wall_x(root: Node3D, z: float, x0: float, x1: float, height: float, mat: Material, cutaway: bool) -> MeshInstance3D:
	var node: MeshInstance3D = _solid_box(Vector3(x1-x0,height,0.28),mat,Vector3((x0+x1)*0.5,height*0.5,z),root)
	if cutaway:
		_arch_cutaway.append(node)
	return node

func _wall_z(root: Node3D, x: float, z0: float, z1: float, height: float, mat: Material, cutaway: bool) -> MeshInstance3D:
	var node: MeshInstance3D = _solid_box(Vector3(0.28,height,z1-z0),mat,Vector3(x,height*0.5,(z0+z1)*0.5),root)
	if cutaway:
		_arch_cutaway.append(node)
	return node

func _doorway_x(root: Node3D, center_x: float, z: float, width: float, title: String, leaf_mat: Material, frame_mat: Material, room_index: int, double_door: bool = false) -> void:
	# Frame sits exactly on the wall opening. No collision crosses the opening.
	_box(Vector3(0.08,2.35,0.18),frame_mat,Vector3(center_x-width*0.5,1.18,z),root)
	_box(Vector3(0.08,2.35,0.18),frame_mat,Vector3(center_x+width*0.5,1.18,z),root)
	_box(Vector3(width+0.08,0.10,0.18),frame_mat,Vector3(center_x,2.31,z),root)
	if title != "":
		_label3d(title,Vector3(center_x,2.62,z+0.02),root,11)
	if double_door:
		var left_root: Node3D = Node3D.new()
		left_root.position = Vector3(center_x-width*0.5,0.0,z)
		left_root.rotation.y = -0.70
		root.add_child(left_root)
		_box(Vector3(width*0.48,2.20,0.07),leaf_mat,Vector3(width*0.24,1.10,0.0),left_root)
		var right_root: Node3D = Node3D.new()
		right_root.position = Vector3(center_x+width*0.5,0.0,z)
		right_root.rotation.y = 0.70
		root.add_child(right_root)
		_box(Vector3(width*0.48,2.20,0.07),leaf_mat,Vector3(-width*0.24,1.10,0.0),right_root)
	else:
		var hinge: Node3D = Node3D.new()
		hinge.position = Vector3(center_x-width*0.5,0.0,z)
		hinge.rotation.y = -0.82
		root.add_child(hinge)
		_box(Vector3(width*0.92,2.18,0.07),leaf_mat,Vector3(width*0.46,1.09,0.0),hinge)

func _window_group(root: Node3D, pos: Vector3, size: Vector3) -> Node3D:
	var group: Node3D = Node3D.new()
	group.position = pos
	root.add_child(group)
	_box(Vector3(size.x+0.22,size.y+0.22,0.09),dark_mat,Vector3(0.0,0.0,-0.04),group)
	var wm: StandardMaterial3D = glass_mat.duplicate() as StandardMaterial3D
	var pane: MeshInstance3D = _box(size,wm,Vector3(0.0,0.0,0.03),group)
	warm_windows.append(pane)
	_box(Vector3(size.x+0.30,0.07,0.18),concrete_mat,Vector3(0.0,-size.y*0.5-0.10,0.02),group)
	return group

func _build_cohesive_classroom(root: Node3D, center: Vector3, title: String, wood_color: Color, variant: int) -> void:
	var dm: StandardMaterial3D = _mat(wood_color,0.92)
	var cm: StandardMaterial3D = _mat(Color("465159"),0.90)
	var bm: StandardMaterial3D = _mat(Color("e4e5de"),0.80)
	# Board and teacher zone stay on the south wall; doorway is north and always clear.
	_box(Vector3(2.45,1.10,0.07),bm,center+Vector3(0.0,1.82,-2.82),root)
	_box(Vector3(2.35,0.06,0.12),metal_mat,center+Vector3(0.0,1.15,-2.76),root)
	_solid_box(Vector3(1.35,0.10,0.62),dm,center+Vector3(2.15,0.74,-1.85),root)
	for row: int in range(2):
		for col: int in range(3):
			var px: float = -2.15+float(col)*1.55
			var pz: float = -0.25-float(row)*1.45
			_solid_box(Vector3(1.00,0.07,0.52),dm,center+Vector3(px,0.68,pz),root)
			_box(Vector3(0.42,0.07,0.40),cm,center+Vector3(px,0.41,pz+0.50),root)
			_box(Vector3(0.42,0.40,0.06),cm,center+Vector3(px,0.63,pz+0.68),root)
	if variant == 0:
		_solid_box(Vector3(0.44,1.65,1.45),_mat(Color("78817d"),0.92),center+Vector3(-3.10,0.83,-1.55),root)
	elif variant == 1:
		_box(Vector3(1.8,0.90,0.06),_mat(Color("927554"),0.94),center+Vector3(-2.55,1.60,-2.82),root)
	else:
		_solid_box(Vector3(1.45,0.70,0.42),dm,center+Vector3(-2.75,0.36,-1.95),root)

func _add_ceiling_light(root: Node3D, pos: Vector3, fixture_mat: Material, energy: float) -> void:
	_box(Vector3(0.82,0.045,0.16),fixture_mat,pos,root)
	var light: OmniLight3D = OmniLight3D.new()
	light.position = pos-Vector3(0.0,0.25,0.0)
	light.light_color = Color("ffe7ba")
	light.light_energy = energy
	light.omni_range = 3.8
	root.add_child(light)

func _update_school_cutaway() -> void:
	if player == null:
		return
	var p: Vector3 = player.global_position
	var inside: bool = p.x > -11.85 and p.x < 12.85 and p.z > -6.10 and p.z < 6.35
	_inside_school = inside
	for node: Node3D in _arch_cutaway:
		if is_instance_valid(node):
			node.visible = not inside

func _update_camera(delta: float) -> void:
	if player == null or camera == null:
		return
	var p: Vector3 = player.global_position
	var lead: Vector3 = Vector3(player.velocity.x,0.0,player.velocity.z)
	if lead.length() > 0.15:
		lead = lead.normalized()
	var inside: bool = p.x > -11.85 and p.x < 12.85 and p.z > -6.10 and p.z < 6.35
	var focus: Vector3 = p+Vector3(0.0,0.95,0.0)
	if inside:
		focus += lead*0.30
		# High enough to clear 3.45 m interior walls, but still visibly isometric.
		target_camera_pos = focus+Vector3(5.8,9.4,6.6)
		camera.fov = lerp(camera.fov,39.5,1.0-exp(-5.2*delta))
	else:
		focus += lead*1.25
		target_camera_pos = focus+Vector3(11.2,7.8,11.2)
		camera.fov = lerp(camera.fov,43.0,1.0-exp(-4.0*delta))
	camera.global_position = camera.global_position.lerp(target_camera_pos,1.0-exp(-5.2*delta))
	camera.look_at(focus,Vector3.UP)

func _build_ui() -> void:
	var ui: CanvasLayer = CanvasLayer.new()
	add_child(ui)
	var panel: ColorRect = ColorRect.new()
	panel.position = Vector2(18.0,18.0)
	panel.size = Vector2(330.0,110.0)
	panel.color = Color(0.015,0.025,0.032,0.80)
	ui.add_child(panel)
	var label: Label = Label.new()
	label.position = Vector2(14.0,10.0)
	label.text = "ROSVIK: BLACKOUT\nARCHITECTURAL COHESION 03\n\nVäggar • dörrar • planritning • cutaway"
	label.add_theme_font_size_override("font_size",14)
	panel.add_child(label)
	var hint: Label = Label.new()
	hint.position = Vector2(18.0,852.0)
	hint.text = "WASD / pilar: gå     Shift: spring"
	hint.add_theme_font_size_override("font_size",13)
	ui.add_child(hint)
