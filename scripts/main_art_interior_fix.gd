extends "res://scripts/main_art.gd"

func _ready() -> void:
	super._ready()
	print("ROSVIK_INTERIOR_CAMERA_READY")
	print("ROSVIK_CLASSROOM_READY")

func _build_school() -> void:
	var root: Node3D = Node3D.new()
	root.name = "RosviksSkola"
	root.position = Vector3.ZERO
	add_child(root)

	# Exterior shell, now carved around two actual playable interior spaces:
	# entrance/corridor and a small classroom/meeting room to the right.
	_solid_box(Vector3(4.6,4.3,12.5), school_mat, Vector3(-9.7,2.15,0.0), root)
	_solid_box(Vector3(11.6,4.3,12.5), school_mat, Vector3(9.2,2.15,0.0), root)
	_solid_box(Vector3(5.2,4.3,5.1), school_mat, Vector3(0.8,2.15,3.70), root)
	_solid_box(Vector3(5.2,4.3,1.3), school_mat, Vector3(0.8,2.15,-5.60), root)
	_solid_box(Vector3(1.6,4.3,7.45), school_mat, Vector3(-6.6,2.15,-2.525), root)
	# Right corridor wall is split so there is a genuine doorway into the room.
	_solid_box(Vector3(1.6,4.3,1.90), school_mat, Vector3(-2.6,2.15,0.25), root)
	_solid_box(Vector3(1.6,4.3,3.15), school_mat, Vector3(-2.6,2.15,-4.675), root)
	_solid_box(Vector3(11.0,3.2,10.0), school_mat, Vector3(-15.8,1.6,1.3), root)
	_solid_box(Vector3(8.0,3.5,7.0), _textured_mat(Color("8f9a9e"),0.9,0.0,"horizontal",96,0.03), Vector3(16.5,1.75,-1.8), root)

	# Roof is segmented too. No roof sits above the corridor or the classroom,
	# so the isometric camera can actually see the player indoors.
	_box(Vector3(4.9,0.34,13.4), roof_mat, Vector3(-9.7,4.46,0.0), root)
	_box(Vector3(11.9,0.34,13.4), roof_mat, Vector3(9.2,4.46,0.0), root)
	_box(Vector3(5.4,0.34,5.3), roof_mat, Vector3(0.8,4.46,3.70), root)
	_box(Vector3(5.4,0.34,1.5), roof_mat, Vector3(0.8,4.46,-5.60), root)
	_box(Vector3(1.9,0.34,7.8), roof_mat, Vector3(-6.6,4.46,-2.5), root)
	_box(Vector3(1.9,0.34,1.9), roof_mat, Vector3(-2.6,4.46,0.25), root)
	_box(Vector3(1.9,0.34,3.15), roof_mat, Vector3(-2.6,4.46,-4.675), root)
	_box(Vector3(12.0,0.30,10.8), roof_mat, Vector3(-15.8,3.36,1.3), root)
	_box(Vector3(4.6,0.12,13.0), packed_snow_mat, Vector3(-9.7,4.68,0.0), root)
	_box(Vector3(11.6,0.12,13.0), packed_snow_mat, Vector3(9.2,4.68,0.0), root)
	_box(Vector3(5.1,0.12,5.0), packed_snow_mat, Vector3(0.8,4.68,3.70), root)
	_box(Vector3(5.1,0.12,1.2), packed_snow_mat, Vector3(0.8,4.68,-5.60), root)
	_box(Vector3(11.6,0.12,10.4), packed_snow_mat, Vector3(-15.8,3.57,1.3), root)

	# Exterior facade and windows.
	_box(Vector3(27.2,0.9,0.18), concrete_mat, Vector3(1.5,0.48,6.32), root)
	for x_value: float in [-10.4,-8.6,0.5,3.2,5.9,8.6,11.3,14.0]:
		_add_window(root, Vector3(x_value,2.25,6.39), Vector3(1.45,1.15,0.12))

	# Main entrance, with a flush visual threshold only.
	_solid_box(Vector3(0.42,3.0,2.3), dark_mat, Vector3(-7.15,1.5,7.0), root)
	_solid_box(Vector3(0.42,3.0,2.3), dark_mat, Vector3(-2.05,1.5,7.0), root)
	_box(Vector3(5.9,0.28,3.0), roof_mat, Vector3(-4.6,3.05,8.2), root)
	_box(Vector3(1.35,2.55,0.10), glass_mat, Vector3(-6.30,1.30,8.18), root)
	_box(Vector3(1.35,2.55,0.10), glass_mat, Vector3(-2.90,1.30,8.18), root)
	var open_door: MeshInstance3D = _box(Vector3(1.15,2.42,0.10), glass_mat, Vector3(-5.45,1.25,8.42), root)
	open_door.rotation.y = -0.78
	var second_door: MeshInstance3D = _box(Vector3(1.15,2.42,0.10), glass_mat, Vector3(-3.75,1.25,8.42), root)
	second_door.rotation.y = 0.78
	_box(Vector3(5.8,0.035,3.0), concrete_mat, Vector3(-4.6,0.018,9.0), root)

	# Interior materials and floors.
	var interior_floor: StandardMaterial3D = _textured_mat(Color("737b7d"),0.9,0.0,"noise",96,0.025)
	var classroom_floor: StandardMaterial3D = _textured_mat(Color("8a8174"),0.9,0.0,"horizontal",96,0.015)
	var interior_wall: StandardMaterial3D = _textured_mat(Color("c4c7c3"),0.93,0.0,"horizontal",96,0.018)
	var locker_mat: StandardMaterial3D = _mat(Color("6d7d83"),0.82,0.12)
	var notice_mat: StandardMaterial3D = _mat(Color("9a7754"),0.92)
	var desk_mat: StandardMaterial3D = _mat(Color("a8815d"),0.9)
	_box(Vector3(5.55,0.08,5.1), interior_floor, Vector3(-4.6,0.06,3.7), root)
	_box(Vector3(2.35,0.08,7.75), interior_floor, Vector3(-4.6,0.06,-2.52), root)
	_box(Vector3(5.0,0.08,5.65), classroom_floor, Vector3(0.75,0.06,-1.90), root)

	# Corridor finishes. Right side has a clear 2.4 m doorway into the classroom.
	_box(Vector3(0.08,3.6,11.4), interior_wall, Vector3(-5.84,1.85,-0.05), root)
	_box(Vector3(0.08,3.6,1.90), interior_wall, Vector3(-3.36,1.85,0.25), root)
	_box(Vector3(0.08,3.6,3.15), interior_wall, Vector3(-3.36,1.85,-4.675), root)
	_solid_box(Vector3(2.4,3.6,0.18), interior_wall, Vector3(-4.6,1.8,-6.18), root)
	_label3d("KLASSRUM",Vector3(-3.28,2.75,-1.90),root,18)

	# Classroom finish faces.
	_box(Vector3(0.08,3.55,5.65), interior_wall, Vector3(3.32,1.82,-1.90), root)
	_box(Vector3(5.0,3.55,0.08), interior_wall, Vector3(0.75,1.82,-4.82), root)
	_box(Vector3(5.0,3.55,0.08), interior_wall, Vector3(0.75,1.82,1.02), root)
	var board_mat: StandardMaterial3D = _mat(Color("e4e6df"),0.76)
	_box(Vector3(0.08,1.35,2.55), board_mat, Vector3(3.25,1.85,-1.85), root)

	# Corridor furniture.
	for i: int in range(5):
		_solid_box(Vector3(0.36,1.75,0.48), locker_mat, Vector3(-5.55,0.88,-3.9+float(i)*0.72), root)
		_box(Vector3(0.02,0.08,0.36), dark_mat, Vector3(-5.34,1.0,-3.9+float(i)*0.72), root)
	_add_bench(Vector3(-4.65,0.0,2.35),root)
	_box(Vector3(1.7,1.0,0.08), notice_mat, Vector3(-3.30,1.55,3.15), root)
	_box(Vector3(1.45,0.12,0.42), wood_mat, Vector3(-3.9,1.05,-0.15), root)
	_box(Vector3(1.45,0.9,0.10), glass_mat, Vector3(-3.34,1.50,-0.15), root)
	_label3d("EXPEDITION",Vector3(-3.31,2.20,-0.15),root,20)
	_label3d("ENTRÉ",Vector3(-4.6,2.72,8.35),root,22)

	# Classroom furniture: teacher desk plus four student tables and chairs.
	_solid_box(Vector3(1.35,0.10,0.65), desk_mat, Vector3(2.10,0.78,-1.90), root)
	_solid_box(Vector3(0.10,0.72,0.10), metal_mat, Vector3(1.55,0.38,-2.15), root)
	_solid_box(Vector3(0.10,0.72,0.10), metal_mat, Vector3(2.65,0.38,-2.15), root)
	for row: int in range(2):
		for col: int in range(2):
			var px: float = -0.35 + float(col) * 1.45
			var pz: float = -0.55 + float(row) * 1.65
			_solid_box(Vector3(1.05,0.08,0.55), desk_mat, Vector3(px,0.72,pz), root)
			_solid_box(Vector3(0.48,0.08,0.48), dark_mat, Vector3(px,0.45,pz+0.62), root)
			_solid_box(Vector3(0.08,0.72,0.08), dark_mat, Vector3(px,0.36,pz+0.62), root)

	# Interior lighting.
	var fixture_mat: StandardMaterial3D = _mat(Color("eef2da"),0.55)
	fixture_mat.emission_enabled = true
	fixture_mat.emission = Color("fff1be")
	fixture_mat.emission_energy_multiplier = 1.3
	for z_value: float in [4.6,2.8,0.4,-1.8,-4.0]:
		_box(Vector3(1.15,0.06,0.32),fixture_mat,Vector3(-4.6,3.65,z_value),root)
		var light: OmniLight3D = OmniLight3D.new()
		light.position = Vector3(-4.6,3.35,z_value)
		light.light_color = Color("ffe8b8")
		light.light_energy = 1.0
		light.omni_range = 4.5
		root.add_child(light)
	for room_z: float in [-0.5,-3.2]:
		_box(Vector3(1.35,0.06,0.32),fixture_mat,Vector3(0.75,3.55,room_z),root)
		var room_light: OmniLight3D = OmniLight3D.new()
		room_light.position = Vector3(0.75,3.25,room_z)
		room_light.light_color = Color("ffe8b8")
		room_light.light_energy = 1.15
		room_light.omni_range = 4.8
		root.add_child(room_light)

	# Yard surfaces and circulation gates.
	_box(Vector3(33.0,0.08,20.0), packed_snow_mat, Vector3(-1.0,0.03,19.0), root)
	_box(Vector3(13.0,0.06,8.0), slush_mat, Vector3(-4.0,0.055,11.5), root)
	_box(Vector3(4.2,0.065,12.5), slush_mat, Vector3(-4.6,0.058,16.2), root)
	_box(Vector3(4.2,0.065,8.0), slush_mat, Vector3(15.7,0.058,21.0), root)
	_add_fence(Vector3(-18.0,0.0,13.0),Vector3(-6.7,0.0,13.0),root)
	_add_fence(Vector3(-2.5,0.0,13.0),Vector3(15.0,0.0,13.0),root)
	_gate_posts(Vector3(-6.7,0.0,13.0),Vector3(-2.5,0.0,13.0),root)
	_add_fence(Vector3(-18.0,0.0,13.0),Vector3(-18.0,0.0,31.0),root)
	_add_fence(Vector3(15.0,0.0,13.0),Vector3(15.0,0.0,18.7),root)
	_add_fence(Vector3(15.0,0.0,23.0),Vector3(15.0,0.0,31.0),root)
	_gate_posts(Vector3(15.0,0.0,18.7),Vector3(15.0,0.0,23.0),root)
	_add_fence(Vector3(-18.0,0.0,31.0),Vector3(15.0,0.0,31.0),root)
	for i: int in range(7):
		var rack: MeshInstance3D = _box(Vector3(0.07,0.55,1.45), metal_mat, Vector3(-7.0+float(i)*0.55,0.30,11.7), root)
		rack.rotation.z = 0.18
	_add_bench(Vector3(-11.0,0.0,18.0),root)
	_add_bench(Vector3(9.0,0.0,18.0),root)
	_add_goal(Vector3(-9.0,0.0,26.0),root)
	_add_basket(Vector3(8.0,0.0,25.0),root)
	_add_flag(Vector3(11.0,0.0,10.3),root)
	_add_dumpster(Vector3(-16.0,0.0,9.0),root)
	_label3d("ROSVIKS SKOLA",Vector3(5.4,3.95,6.48),root,34)

func _build_ui() -> void:
	var ui: CanvasLayer = CanvasLayer.new()
	add_child(ui)
	var panel: ColorRect = ColorRect.new()
	panel.position = Vector2(18.0,18.0)
	panel.size = Vector2(300.0,112.0)
	panel.color = Color(0.018,0.028,0.035,0.78)
	ui.add_child(panel)
	var label: Label = Label.new()
	label.position = Vector2(14.0,10.0)
	label.text = "ROSVIK: BLACKOUT\nINTERIOR / CHARACTER FIX 01.2\n\nEntré + korridor + klassrum är spelbara."
	label.add_theme_font_size_override("font_size",14)
	panel.add_child(label)
	var hint: Label = Label.new()
	hint.position = Vector2(18.0,852.0)
	hint.text = "WASD / pilar: gå     Shift: spring"
	hint.add_theme_font_size_override("font_size",13)
	ui.add_child(hint)

func _update_camera(delta: float) -> void:
	if player == null or camera == null:
		return
	var lead: Vector3 = Vector3(player.velocity.x,0.0,player.velocity.z)
	if lead.length() > 0.15:
		lead = lead.normalized()*1.2
	var focus: Vector3 = player.global_position+lead+Vector3(0.0,1.0,0.0)
	var p: Vector3 = player.global_position
	var indoors: bool = p.x > -6.2 and p.x < 3.5 and p.z < 8.7 and p.z > -6.45
	if indoors:
		# Higher and closer, looking through the intentional cutaway roof openings.
		target_camera_pos = focus+Vector3(4.9,9.0,5.3)
		camera.fov = lerp(camera.fov,38.0,1.0-exp(-5.0*delta))
	else:
		target_camera_pos = focus+Vector3(11.8,8.2,11.8)
		camera.fov = lerp(camera.fov,43.0,1.0-exp(-4.0*delta))
	camera.global_position = camera.global_position.lerp(target_camera_pos,1.0-exp(-5.2*delta))
	camera.look_at(focus,Vector3.UP)
