extends "res://scripts/main_art.gd"

var _milestone_clock: float = 0.0
var _school_inside_light: OmniLight3D

func _ready() -> void:
	super._ready()
	_add_rosvik_identity()
	_add_arena_identity()
	print("ROSVIK_VISUAL_FOUNDATION_READY")
	print("ROSVIK_INTERIOR_CLEANUP_READY")
	print("ROSVIK_SCALE_PASS_READY")
	print("ROSVIK_CAMERA_PASS_READY")

func _process(delta: float) -> void:
	super._process(delta)
	_milestone_clock += delta
	if _school_inside_light != null:
		_school_inside_light.light_energy = 0.95 + sin(_milestone_clock * 0.65) * 0.025

func _build_environment() -> void:
	var env_node: WorldEnvironment = WorldEnvironment.new()
	var env: Environment = Environment.new()
	var sky_material: ProceduralSkyMaterial = ProceduralSkyMaterial.new()
	sky_material.sky_top_color = Color("2a3d47")
	sky_material.sky_horizon_color = Color("71838b")
	sky_material.ground_bottom_color = Color("3d4c52")
	sky_material.ground_horizon_color = Color("8d9a9e")
	sky_material.sun_angle_max = 18.0
	var sky: Sky = Sky.new()
	sky.sky_material = sky_material
	env.background_mode = Environment.BG_SKY
	env.sky = sky
	env.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	env.ambient_light_color = Color("7d9099")
	env.ambient_light_energy = 0.36
	env.fog_enabled = true
	env.fog_light_color = Color("66767d")
	env.fog_density = 0.0065
	env.fog_height = 0.25
	env.fog_height_density = 0.08
	env.tonemap_mode = Environment.TONE_MAPPER_FILMIC
	env.tonemap_exposure = 0.58
	env_node.environment = env
	add_child(env_node)

	var sun: DirectionalLight3D = DirectionalLight3D.new()
	sun.rotation_degrees = Vector3(-38.0,-35.0,0.0)
	sun.light_color = Color("f1c69f")
	sun.light_energy = 0.78
	sun.shadow_enabled = true
	sun.directional_shadow_max_distance = 120.0
	add_child(sun)

func _build_school() -> void:
	var root: Node3D = Node3D.new()
	root.name = "RosviksSkola"
	add_child(root)

	# --- Exterior massing ----------------------------------------------------
	# 1 Godot unit = 1 metre. The centre is deliberately carved into a real
	# foyer/corridor/classroom rather than filled with overlapping props.
	_solid_box(Vector3(5.2,4.25,12.5),school_mat,Vector3(-9.5,2.125,0.0),root)
	_solid_box(Vector3(11.6,4.25,12.5),school_mat,Vector3(9.0,2.125,0.0),root)
	_solid_box(Vector3(9.6,3.20,9.6),school_mat,Vector3(-16.4,1.60,1.1),root)
	_solid_box(Vector3(6.8,3.45,7.0),_textured_mat(Color("8e989c"),0.91,0.0,"horizontal",96,0.025),Vector3(17.0,1.725,-1.8),root)

	# Corridor walls. Right side is split to create an actual classroom doorway.
	_solid_box(Vector3(0.34,3.55,11.3),school_mat,Vector3(-6.35,1.78,-0.10),root)
	_solid_box(Vector3(0.34,3.55,4.15),school_mat,Vector3(-2.95,1.78,2.90),root)
	_solid_box(Vector3(0.34,3.55,3.25),school_mat,Vector3(-2.95,1.78,-4.45),root)
	# Classroom shell to the right of the corridor.
	_solid_box(Vector3(0.34,3.55,5.85),school_mat,Vector3(3.25,1.78,-1.90),root)
	_solid_box(Vector3(6.15,3.55,0.34),school_mat,Vector3(0.15,1.78,0.95),root)
	_solid_box(Vector3(6.15,3.55,0.34),school_mat,Vector3(0.15,1.78,-4.82),root)

	# Roof/parapet only over exterior masses. Interior zones stay open for cutaway camera.
	_box(Vector3(5.55,0.34,13.25),roof_mat,Vector3(-9.5,4.42,0.0),root)
	_box(Vector3(11.95,0.34,13.25),roof_mat,Vector3(9.0,4.42,0.0),root)
	_box(Vector3(10.1,0.30,10.1),roof_mat,Vector3(-16.4,3.34,1.1),root)
	_box(Vector3(5.25,0.12,12.95),packed_snow_mat,Vector3(-9.5,4.65,0.0),root)
	_box(Vector3(11.65,0.12,12.95),packed_snow_mat,Vector3(9.0,4.65,0.0),root)
	_box(Vector3(9.85,0.12,9.85),packed_snow_mat,Vector3(-16.4,3.55,1.1),root)

	# Exterior facade/base/windows.
	_box(Vector3(28.2,0.86,0.18),concrete_mat,Vector3(1.4,0.45,6.32),root)
	for x_value: float in [-10.5,-8.5,4.4,7.0,9.6,12.2,14.8]:
		_add_window(root,Vector3(x_value,2.20,6.40),Vector3(1.45,1.10,0.12))
	for x_value: float in [-11.6,13.4]:
		_solid_cylinder(0.05,3.8,metal_mat,Vector3(x_value,1.90,6.55),root)

	# Main entrance. Wide clear opening, no raised collision lip.
	_solid_box(Vector3(0.38,3.05,2.30),dark_mat,Vector3(-6.85,1.525,7.05),root)
	_solid_box(Vector3(0.38,3.05,2.30),dark_mat,Vector3(-2.45,1.525,7.05),root)
	_box(Vector3(5.45,0.25,2.75),roof_mat,Vector3(-4.65,3.08,8.10),root)
	_box(Vector3(1.12,2.42,0.10),glass_mat,Vector3(-6.05,1.26,8.23),root)
	_box(Vector3(1.12,2.42,0.10),glass_mat,Vector3(-3.25,1.26,8.23),root)
	var door_l: MeshInstance3D = _box(Vector3(0.96,2.35,0.10),glass_mat,Vector3(-5.25,1.23,8.42),root)
	door_l.rotation.y = -0.72
	var door_r: MeshInstance3D = _box(Vector3(0.96,2.35,0.10),glass_mat,Vector3(-4.05,1.23,8.42),root)
	door_r.rotation.y = 0.72
	_box(Vector3(5.2,0.025,2.8),concrete_mat,Vector3(-4.65,0.015,9.0),root)
	_label3d("ROSVIKS SKOLA",Vector3(5.0,3.88,6.52),root,32)
	_label3d("ENTRÉ",Vector3(-4.65,2.72,8.38),root,18)

	# --- Interior surfaces ---------------------------------------------------
	var foyer_floor: StandardMaterial3D = _textured_mat(Color("666f72"),0.92,0.0,"noise",96,0.020)
	var corridor_floor: StandardMaterial3D = _textured_mat(Color("70787a"),0.92,0.0,"noise",96,0.018)
	var classroom_floor: StandardMaterial3D = _textured_mat(Color("8b8276"),0.92,0.0,"horizontal",96,0.014)
	var interior_wall: StandardMaterial3D = _textured_mat(Color("c7c8c2"),0.95,0.0,"horizontal",96,0.012)
	var skirting: StandardMaterial3D = _mat(Color("555d60"),0.92)
	var locker_mat: StandardMaterial3D = _mat(Color("65777e"),0.86,0.08)
	var notice_mat: StandardMaterial3D = _mat(Color("927554"),0.94)
	var desk_mat: StandardMaterial3D = _mat(Color("9a7556"),0.92)
	var chair_mat: StandardMaterial3D = _mat(Color("465159"),0.90)

	# Foyer / corridor / room floors are flush and continuous.
	_box(Vector3(4.9,0.055,4.7),foyer_floor,Vector3(-4.65,0.055,4.25),root)
	_box(Vector3(3.0,0.055,7.5),corridor_floor,Vector3(-4.65,0.055,-1.65),root)
	_box(Vector3(5.85,0.055,5.45),classroom_floor,Vector3(0.15,0.055,-1.90),root)
	_box(Vector3(3.15,0.025,1.35),_textured_mat(Color("2f3538"),0.98,0.0,"noise",64,0.025),Vector3(-4.65,0.095,6.25),root)

	# Interior finish faces and low skirting.
	_box(Vector3(0.07,3.45,11.0),interior_wall,Vector3(-6.16,1.75,-0.15),root)
	_box(Vector3(0.07,3.45,4.0),interior_wall,Vector3(-3.13,1.75,2.95),root)
	_box(Vector3(0.07,3.45,3.1),interior_wall,Vector3(-3.13,1.75,-4.45),root)
	_box(Vector3(0.07,3.45,5.45),interior_wall,Vector3(3.06,1.75,-1.90),root)
	_box(Vector3(5.82,3.45,0.07),interior_wall,Vector3(0.15,1.75,0.76),root)
	_box(Vector3(5.82,3.45,0.07),interior_wall,Vector3(0.15,1.75,-4.63),root)
	for z_value: float in [3.0,-0.2,-3.7]:
		_box(Vector3(0.08,0.14,1.2),skirting,Vector3(-6.10,0.14,z_value),root)
	_box(Vector3(0.08,0.14,5.2),skirting,Vector3(3.00,0.14,-1.90),root)

	# --- Corridor furniture anchored to walls -------------------------------
	# Lockers stay against the left wall, leaving >2 m clear corridor width.
	for i: int in range(5):
		var z_locker: float = -4.00 + float(i) * 0.72
		_solid_box(Vector3(0.36,1.70,0.55),locker_mat,Vector3(-5.88,0.85,z_locker),root)
		_box(Vector3(0.03,0.06,0.28),metal_mat,Vector3(-5.67,0.95,z_locker),root)

	# Foyer bench on the right, notice board and reception opening above it.
	_solid_box(Vector3(1.65,0.12,0.42),desk_mat,Vector3(-3.42,0.48,3.15),root)
	for x_value: float in [-3.92,-2.92]:
		_solid_box(Vector3(0.08,0.48,0.08),chair_mat,Vector3(x_value,0.24,3.15),root)
	_box(Vector3(0.07,0.95,1.55),notice_mat,Vector3(-3.08,1.52,4.15),root)
	for i: int in range(4):
		_box(Vector3(0.025,0.22,0.20),_mat(Color("d8d4c5").darkened(float(i)*0.035),0.96),Vector3(-3.03,1.35+float(i%2)*0.28,3.70+float(i/2)*0.50),root)
	_box(Vector3(0.06,0.84,1.35),glass_mat,Vector3(-3.07,1.52,1.85),root)
	_box(Vector3(0.34,0.08,1.45),desk_mat,Vector3(-3.28,0.98,1.85),root)
	_label3d("EXPEDITION",Vector3(-3.02,2.18,1.85),root,15)

	# Coat rail, radiator, extinguisher and exit sign are wall-mounted and scaled.
	_box(Vector3(0.08,0.09,2.10),wood_mat,Vector3(-6.05,1.55,4.05),root)
	for i: int in range(6):
		var hook: MeshInstance3D = _cylinder(0.025,0.14,metal_mat,Vector3(-5.96,1.45,3.30+float(i)*0.30),root)
		hook.rotation.z = PI/2.0
	for z_rad: float in [1.1,-1.2]:
		_box(Vector3(0.12,0.46,1.05),_mat(Color("d5d5ce"),0.93),Vector3(-6.03,0.47,z_rad),root)
	var extinguisher: MeshInstance3D = _cylinder(0.11,0.48,_mat(Color("a23830"),0.82),Vector3(-3.22,0.58,4.72),root)
	extinguisher.rotation.z = 0.0
	var exit_mat: StandardMaterial3D = _mat(Color("34745a"),0.78)
	exit_mat.emission_enabled = true
	exit_mat.emission = Color("4ba777")
	exit_mat.emission_energy_multiplier = 0.45
	_box(Vector3(0.06,0.30,0.75),exit_mat,Vector3(-3.08,2.72,4.90),root)
	_label3d("UT",Vector3(-3.02,2.72,4.90),root,12)

	# --- Classroom -----------------------------------------------------------
	var board_mat: StandardMaterial3D = _mat(Color("e2e3dc"),0.80)
	_box(Vector3(0.07,1.25,2.35),board_mat,Vector3(2.98,1.85,-1.75),root)
	_box(Vector3(0.12,0.07,2.30),metal_mat,Vector3(2.90,1.10,-1.75),root)
	_label3d("KLASSRUM",Vector3(-2.82,2.68,-1.92),root,14)

	# Teacher desk on far wall, safely out of the doorway.
	_solid_box(Vector3(1.35,0.10,0.62),desk_mat,Vector3(1.85,0.75,-3.75),root)
	for x_value: float in [1.35,2.35]:
		_solid_box(Vector3(0.08,0.68,0.08),metal_mat,Vector3(x_value,0.36,-3.75),root)
	var screen_mat: StandardMaterial3D = _mat(Color("253139"),0.48,0.05)
	screen_mat.emission_enabled = true
	screen_mat.emission = Color("6d93a4")
	screen_mat.emission_energy_multiplier = 0.20
	_box(Vector3(0.08,0.48,0.64),screen_mat,Vector3(1.85,1.10,-3.74),root)
	_box(Vector3(0.28,0.04,0.17),dark_mat,Vector3(1.85,0.84,-3.74),root)

	# Four student desks in a clean 2x2 layout with a central aisle.
	for row: int in range(2):
		for col: int in range(2):
			var px: float = -1.35 + float(col) * 1.75
			var pz: float = -0.70 - float(row) * 1.55
			_solid_box(Vector3(1.05,0.08,0.56),desk_mat,Vector3(px,0.70,pz),root)
			for leg_x: float in [-0.40,0.40]:
				_solid_box(Vector3(0.06,0.65,0.06),metal_mat,Vector3(px+leg_x,0.34,pz),root)
			# Chairs are visual-only for now so the room stays pleasant to navigate.
			_box(Vector3(0.44,0.08,0.42),chair_mat,Vector3(px,0.43,pz+0.58),root)
			_box(Vector3(0.44,0.45,0.07),chair_mat,Vector3(px,0.68,pz+0.76),root)

	# Storage against the back wall: nothing floats in the middle of the room.
	_solid_box(Vector3(0.48,1.85,1.70),_mat(Color("7b817d"),0.92),Vector3(2.70,0.93,0.02),root)
	for y_value: float in [0.42,0.88,1.34,1.72]:
		_box(Vector3(0.52,0.05,1.58),dark_mat,Vector3(2.44,y_value,0.02),root)
	for i: int in range(8):
		var book_color: Color = [Color("4d6a7a"),Color("8b5148"),Color("b09659")][i%3]
		_box(Vector3(0.10,0.30,0.16),_mat(book_color,0.90),Vector3(2.38,0.61+float(i%3)*0.43,-0.55+float(i%4)*0.36),root)

	# Small, correctly scaled fluorescent fixtures instead of giant white beams.
	var fixture_mat: StandardMaterial3D = _mat(Color("e6e8df"),0.58)
	fixture_mat.emission_enabled = true
	fixture_mat.emission = Color("fff0c8")
	fixture_mat.emission_energy_multiplier = 0.85
	for z_light: float in [4.65,2.45,0.15,-2.15,-4.25]:
		_box(Vector3(0.78,0.045,0.16),fixture_mat,Vector3(-4.65,3.35,z_light),root)
		var c_light: OmniLight3D = OmniLight3D.new()
		c_light.position = Vector3(-4.65,3.05,z_light)
		c_light.light_color = Color("ffe8bd")
		c_light.light_energy = 0.62
		c_light.omni_range = 3.8
		root.add_child(c_light)
	for room_z: float in [-0.65,-3.20]:
		_box(Vector3(0.92,0.045,0.16),fixture_mat,Vector3(0.15,3.30,room_z),root)
		var r_light: OmniLight3D = OmniLight3D.new()
		r_light.position = Vector3(0.15,3.00,room_z)
		r_light.light_color = Color("ffe7ba")
		r_light.light_energy = 0.72
		r_light.omni_range = 4.1
		root.add_child(r_light)
	_school_inside_light = OmniLight3D.new()
	_school_inside_light.position = Vector3(-4.65,2.55,5.25)
	_school_inside_light.light_color = Color("ffd3a2")
	_school_inside_light.light_energy = 0.95
	_school_inside_light.omni_range = 5.0
	root.add_child(_school_inside_light)

	# --- Schoolyard with two deliberate circulation gates -------------------
	_box(Vector3(33.0,0.08,20.0),packed_snow_mat,Vector3(-1.0,0.03,19.0),root)
	_box(Vector3(13.0,0.06,8.0),slush_mat,Vector3(-4.0,0.055,11.5),root)
	_box(Vector3(4.2,0.06,12.5),slush_mat,Vector3(-4.65,0.058,16.2),root)
	_add_fence(Vector3(-18.0,0.0,13.0),Vector3(-6.8,0.0,13.0),root)
	_add_fence(Vector3(-2.4,0.0,13.0),Vector3(15.0,0.0,13.0),root)
	_gate_posts(Vector3(-6.8,0.0,13.0),Vector3(-2.4,0.0,13.0),root)
	_add_fence(Vector3(-18.0,0.0,13.0),Vector3(-18.0,0.0,31.0),root)
	_add_fence(Vector3(15.0,0.0,13.0),Vector3(15.0,0.0,18.8),root)
	_add_fence(Vector3(15.0,0.0,23.1),Vector3(15.0,0.0,31.0),root)
	_gate_posts(Vector3(15.0,0.0,18.8),Vector3(15.0,0.0,23.1),root)
	_add_fence(Vector3(-18.0,0.0,31.0),Vector3(15.0,0.0,31.0),root)
	for i: int in range(6):
		var rack: MeshInstance3D = _box(Vector3(0.06,0.50,1.20),metal_mat,Vector3(-7.2+float(i)*0.50,0.27,11.8),root)
		rack.rotation.z = 0.17
	_add_bench(Vector3(-11.0,0.0,18.0),root)
	_add_bench(Vector3(9.0,0.0,18.0),root)
	_add_goal(Vector3(-9.0,0.0,26.0),root)
	_add_basket(Vector3(8.0,0.0,25.0),root)
	_add_flag(Vector3(11.0,0.0,10.3),root)
	_add_dumpster(Vector3(-16.0,0.0,9.0),root)

func _build_arena() -> void:
	# Keep the public-coordinate relationship from the physical pass, but improve
	# the civic/ice-rink identity and entrance hierarchy.
	var root: Node3D = Node3D.new()
	root.name = "NorrbottenStalArena"
	root.position = Vector3(62.5,0.0,15.5)
	add_child(root)
	var hall_mat: StandardMaterial3D = _textured_mat(Color("59666c"),0.78,0.08,"vertical",96,0.028)
	var accent_mat: StandardMaterial3D = _mat(Color("324c5b"),0.76,0.06)
	var service_mat: StandardMaterial3D = _textured_mat(Color("414a4f"),0.84,0.10,"vertical",64,0.022)
	_solid_box(Vector3(39.0,7.0,21.0),hall_mat,Vector3(0.0,3.5,0.0),root)
	_solid_box(Vector3(10.0,3.15,5.1),service_mat,Vector3(-9.0,1.575,12.0),root)
	_solid_box(Vector3(9.0,3.8,4.2),service_mat,Vector3(9.8,1.90,12.0),root)
	_box(Vector3(40.0,0.36,22.0),roof_mat,Vector3(0.0,7.18,0.0),root)
	_box(Vector3(39.5,0.13,21.5),packed_snow_mat,Vector3(0.0,7.41,0.0),root)
	_box(Vector3(35.0,0.48,0.14),accent_mat,Vector3(0.0,5.78,10.76),root)
	for i: int in range(7):
		_add_window(root,Vector3(-15.0+float(i)*4.5,4.65,10.72),Vector3(2.1,0.90,0.12))
	# Public entrance.
	_box(Vector3(5.5,0.28,2.8),roof_mat,Vector3(-9.0,3.25,14.0),root)
	_box(Vector3(3.2,2.5,0.10),glass_mat,Vector3(-9.0,1.32,14.68),root)
	_box(Vector3(1.28,2.38,0.11),service_mat,Vector3(-10.0,1.25,14.78),root)
	_box(Vector3(1.28,2.38,0.11),service_mat,Vector3(-8.0,1.25,14.78),root)
	_label3d("ENTRÉ",Vector3(-9.0,3.46,14.82),root,18)
	_label3d("NORRBOTTEN STÅL ARENA",Vector3(0.0,5.92,10.86),root,34)
	_label3d("ROSVIK IK",Vector3(11.8,3.65,10.86),root,18)
	# Service doors / ventilation / parking.
	for x_value: float in [8.2,11.1,14.0]:
		_box(Vector3(2.20,2.75,0.12),service_mat,Vector3(x_value,1.42,10.80),root)
	for x_value: float in [-13.0,-8.0,-3.0,2.0,7.0,12.0]:
		_box(Vector3(0.80,0.50,0.28),metal_mat,Vector3(x_value,2.35,-10.60),root)
	_box(Vector3(52.0,0.07,19.0),packed_snow_mat,Vector3(0.0,0.035,21.5),root)
	_box(Vector3(38.0,0.055,11.0),slush_mat,Vector3(0.0,0.060,19.2),root)
	var line_mat: StandardMaterial3D = _mat(Color("b9b9ae"),0.92)
	for i: int in range(12):
		_box(Vector3(0.10,0.018,2.35),line_mat,Vector3(-16.5+float(i)*3.0,0.09,19.7),root)
	_add_dumpster(Vector3(14.5,0.0,13.2),root)
	_add_bench(Vector3(-4.3,0.0,14.3),root)
	_add_flag(Vector3(-14.0,0.0,14.8),root)

func _add_rosvik_identity() -> void:
	# The school's official address is Skolgränd 7B. A street-name sign is a small,
	# concrete identity cue instead of a generic fantasy-town prop.
	var blue: StandardMaterial3D = _mat(Color("315d7a"),0.76,0.05)
	var sign: Node3D = Node3D.new()
	sign.name = "SkolgrandSign"
	sign.position = Vector3(-17.0,0.0,8.7)
	add_child(sign)
	_solid_cylinder(0.045,2.25,metal_mat,Vector3(0.0,1.125,0.0),sign)
	_box(Vector3(2.3,0.48,0.08),blue,Vector3(0.0,1.95,0.0),sign)
	_label3d("SKOLGRÄND",Vector3(0.0,1.95,0.07),sign,14)
	# Building-number plate by the entrance.
	var plate: Node3D = Node3D.new()
	plate.position = Vector3(-2.30,1.95,8.34)
	var school_root: Node3D = get_node_or_null("RosviksSkola") as Node3D
	if school_root != null:
		school_root.add_child(plate)
		_box(Vector3(0.05,0.38,0.48),_mat(Color("e4e7e4"),0.82),Vector3.ZERO,plate)
		_label3d("7B",Vector3(0.03,0.0,0.0),plate,12)

func _add_arena_identity() -> void:
	var arena_root: Node3D = get_node_or_null("NorrbottenStalArena") as Node3D
	if arena_root == null:
		return
	# Hockey sticks and a practice goal stay outside the clear entrance path.
	for i: int in range(5):
		var stick: MeshInstance3D = _box(Vector3(0.045,1.35,0.045),dark_mat,Vector3(-13.0+float(i)*0.18,0.70,13.10),arena_root)
		stick.rotation.z = -0.10+float(i)*0.035
	var white: StandardMaterial3D = _mat(Color("d8dddf"),0.86)
	_box(Vector3(2.1,0.07,0.07),white,Vector3(-15.0,1.05,15.1),arena_root)
	for x_value: float in [-16.05,-13.95]:
		_box(Vector3(0.07,1.10,0.07),white,Vector3(x_value,0.55,15.1),arena_root)

func _build_ui() -> void:
	var ui: CanvasLayer = CanvasLayer.new()
	add_child(ui)
	var panel: ColorRect = ColorRect.new()
	panel.position = Vector2(18.0,18.0)
	panel.size = Vector2(320.0,106.0)
	panel.color = Color(0.015,0.025,0.032,0.80)
	ui.add_child(panel)
	var label: Label = Label.new()
	label.position = Vector2(14.0,10.0)
	label.text = "ROSVIK: BLACKOUT\nVISUAL FOUNDATION 01\n\nStädad skola • bättre kamera • tydligare Rosvik"
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
	var p: Vector3 = player.global_position
	var move_lead: Vector3 = Vector3(player.velocity.x,0.0,player.velocity.z)
	if move_lead.length() > 0.15:
		move_lead = move_lead.normalized()
	var in_foyer: bool = p.x > -6.35 and p.x < -2.95 and p.z < 8.65 and p.z > 1.0
	var in_corridor: bool = p.x > -6.35 and p.x < -2.95 and p.z <= 1.0 and p.z > -5.95
	var in_classroom: bool = p.x >= -2.95 and p.x < 3.30 and p.z < 1.05 and p.z > -4.85
	var focus: Vector3 = p+Vector3(0.0,0.95,0.0)
	if in_classroom:
		focus += move_lead*0.35+Vector3(0.25,0.0,-0.10)
		target_camera_pos = focus+Vector3(5.0,7.6,5.2)
		camera.fov = lerp(camera.fov,40.0,1.0-exp(-5.2*delta))
	elif in_corridor:
		focus += move_lead*0.25+Vector3(0.0,0.0,-0.15)
		target_camera_pos = focus+Vector3(4.6,7.8,4.7)
		camera.fov = lerp(camera.fov,40.5,1.0-exp(-5.2*delta))
	elif in_foyer:
		focus += move_lead*0.35
		target_camera_pos = focus+Vector3(5.4,7.2,5.6)
		camera.fov = lerp(camera.fov,41.0,1.0-exp(-5.0*delta))
	else:
		focus += move_lead*1.35
		target_camera_pos = focus+Vector3(11.2,7.7,11.2)
		camera.fov = lerp(camera.fov,43.0,1.0-exp(-4.0*delta))
	camera.global_position = camera.global_position.lerp(target_camera_pos,1.0-exp(-5.0*delta))
	camera.look_at(focus,Vector3.UP)
