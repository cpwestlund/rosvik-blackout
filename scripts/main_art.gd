extends "res://scripts/main_circulation.gd"

const ART_PLAYER_SCRIPT: Script = preload("res://scripts/player_art.gd")

func _ready() -> void:
	seed(24011984)
	_build_materials()
	_build_environment()
	_build_ground()
	_build_roads()
	_build_school()
	_build_arena()
	_build_background()
	_build_props()
	_build_art_details()
	_build_player()
	_build_ui()
	print("ROSVIK_ART_READY solids=", solid_count)
	print("ROSVIK_ENTRANCE_FIX_READY")

func _build_environment() -> void:
	var env_node: WorldEnvironment = WorldEnvironment.new()
	var env: Environment = Environment.new()
	var sky_material: ProceduralSkyMaterial = ProceduralSkyMaterial.new()
	sky_material.sky_top_color = Color("253944")
	sky_material.sky_horizon_color = Color("8a99a0")
	sky_material.ground_bottom_color = Color("394b53")
	sky_material.ground_horizon_color = Color("9aa5aa")
	sky_material.sun_angle_max = 18.0
	var sky: Sky = Sky.new()
	sky.sky_material = sky_material
	env.background_mode = Environment.BG_SKY
	env.sky = sky
	env.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	env.ambient_light_color = Color("81939d")
	env.ambient_light_energy = 0.42
	env.fog_enabled = true
	env.fog_light_color = Color("6c7b82")
	env.fog_density = 0.007
	env.fog_height = 0.4
	env.fog_height_density = 0.085
	env.tonemap_mode = Environment.TONE_MAPPER_FILMIC
	env.tonemap_exposure = 0.62
	env_node.environment = env
	add_child(env_node)

	var sun: DirectionalLight3D = DirectionalLight3D.new()
	sun.rotation_degrees = Vector3(-39.0, -34.0, 0.0)
	sun.light_color = Color("f3c69f")
	sun.light_energy = 0.82
	sun.shadow_enabled = true
	sun.directional_shadow_max_distance = 115.0
	add_child(sun)

func _build_school() -> void:
	var root: Node3D = Node3D.new()
	root.name = "RosviksSkola"
	root.position = Vector3.ZERO
	add_child(root)

	# The main body is deliberately segmented. The central slice is a real walk-in
	# lobby/corridor instead of a solid decorative box.
	_solid_box(Vector3(4.6,4.3,12.5), school_mat, Vector3(-9.7,2.15,0.0), root)
	_solid_box(Vector3(16.8,4.3,12.5), school_mat, Vector3(6.6,2.15,0.0), root)
	_solid_box(Vector3(1.6,4.3,7.45), school_mat, Vector3(-6.6,2.15,-2.525), root)
	_solid_box(Vector3(1.6,4.3,7.45), school_mat, Vector3(-2.6,2.15,-2.525), root)
	_solid_box(Vector3(11.0,3.2,10.0), school_mat, Vector3(-15.8,1.6,1.3), root)
	_solid_box(Vector3(8.0,3.5,7.0), _textured_mat(Color("8f9a9e"),0.9,0.0,"horizontal",96,0.03), Vector3(16.5,1.75,-1.8), root)

	# Cutaway roof: exterior mass remains, while the lobby/corridor stays visible.
	_box(Vector3(4.9,0.34,13.4), roof_mat, Vector3(-9.7,4.46,0.0), root)
	_box(Vector3(17.1,0.34,13.4), roof_mat, Vector3(6.6,4.46,0.0), root)
	_box(Vector3(1.9,0.34,7.8), roof_mat, Vector3(-6.6,4.46,-2.5), root)
	_box(Vector3(1.9,0.34,7.8), roof_mat, Vector3(-2.6,4.46,-2.5), root)
	_box(Vector3(12.0,0.30,10.8), roof_mat, Vector3(-15.8,3.36,1.3), root)
	_box(Vector3(4.6,0.12,13.0), packed_snow_mat, Vector3(-9.7,4.68,0.0), root)
	_box(Vector3(16.8,0.12,13.0), packed_snow_mat, Vector3(6.6,4.68,0.0), root)
	_box(Vector3(11.6,0.12,10.4), packed_snow_mat, Vector3(-15.8,3.57,1.3), root)

	# Exterior facade and windows.
	_box(Vector3(27.2,0.9,0.18), concrete_mat, Vector3(1.5,0.48,6.32), root)
	for x_value: float in [-10.4,-8.6,0.5,3.2,5.9,8.6,11.3,14.0]:
		_add_window(root, Vector3(x_value,2.25,6.39), Vector3(1.45,1.15,0.12))

	# Entrance frame. The centre is physically open.
	_solid_box(Vector3(0.42,3.0,2.3), dark_mat, Vector3(-7.15,1.5,7.0), root)
	_solid_box(Vector3(0.42,3.0,2.3), dark_mat, Vector3(-2.05,1.5,7.0), root)
	_box(Vector3(5.9,0.28,3.0), roof_mat, Vector3(-4.6,3.05,8.2), root)
	_box(Vector3(1.35,2.55,0.10), glass_mat, Vector3(-6.30,1.30,8.18), root)
	_box(Vector3(1.35,2.55,0.10), glass_mat, Vector3(-2.90,1.30,8.18), root)
	var open_door: MeshInstance3D = _box(Vector3(1.15,2.42,0.10), glass_mat, Vector3(-5.45,1.25,8.42), root)
	open_door.rotation.y = -0.78
	var second_door: MeshInstance3D = _box(Vector3(1.15,2.42,0.10), glass_mat, Vector3(-3.75,1.25,8.42), root)
	second_door.rotation.y = 0.78
	# The threshold is visual only and flush with the surrounding floor. A 16 cm
	# collision lip here previously behaved like an invisible wall for CharacterBody3D.
	_box(Vector3(5.8,0.035,3.0), concrete_mat, Vector3(-4.6,0.018,9.0), root)

	# Actual interior: foyer + corridor.
	var interior_floor: StandardMaterial3D = _textured_mat(Color("737b7d"),0.9,0.0,"noise",96,0.025)
	var interior_wall: StandardMaterial3D = _textured_mat(Color("c4c7c3"),0.93,0.0,"horizontal",96,0.018)
	var locker_mat: StandardMaterial3D = _mat(Color("6d7d83"),0.82,0.12)
	var notice_mat: StandardMaterial3D = _mat(Color("9a7754"),0.92)
	_box(Vector3(5.55,0.08,5.1), interior_floor, Vector3(-4.6,0.06,3.7), root)
	_box(Vector3(2.35,0.08,7.75), interior_floor, Vector3(-4.6,0.06,-2.52), root)
	# Finish panels make the corridor read as an interior instead of exposed exterior geometry.
	_box(Vector3(0.08,3.6,11.4), interior_wall, Vector3(-5.84,1.85,-0.05), root)
	_box(Vector3(0.08,3.6,11.4), interior_wall, Vector3(-3.36,1.85,-0.05), root)
	_solid_box(Vector3(2.4,3.6,0.18), interior_wall, Vector3(-4.6,1.8,-6.18), root)

	# Lockers, coat hooks, bench, bulletin board and reception detail.
	for i: int in range(5):
		_solid_box(Vector3(0.36,1.75,0.48), locker_mat, Vector3(-5.55,0.88,-3.9+float(i)*0.72), root)
		_box(Vector3(0.02,0.08,0.36), dark_mat, Vector3(-5.34,1.0,-3.9+float(i)*0.72), root)
	_add_bench(Vector3(-4.65,0.0,2.35),root)
	_box(Vector3(1.7,1.0,0.08), notice_mat, Vector3(-3.30,1.55,3.15), root)
	_box(Vector3(1.45,0.12,0.42), wood_mat, Vector3(-3.9,1.05,-0.15), root)
	_box(Vector3(1.45,0.9,0.10), glass_mat, Vector3(-3.34,1.50,-0.15), root)
	_label3d("EXPEDITION",Vector3(-3.31,2.20,-0.15),root,20)
	_label3d("ENTRÉ",Vector3(-4.6,2.72,8.35),root,22)

	# Warm fluorescent ceiling fixtures; no roof directly above the cutaway section.
	var fixture_mat: StandardMaterial3D = _mat(Color("eef2da"),0.55)
	fixture_mat.emission_enabled = true
	fixture_mat.emission = Color("fff1be")
	fixture_mat.emission_energy_multiplier = 1.3
	for z_value: float in [4.6,2.8,0.4,-1.8,-4.0]:
		_box(Vector3(1.15,0.06,0.32),fixture_mat,Vector3(-4.6,3.65,z_value),root)
		var light: OmniLight3D = OmniLight3D.new()
		light.position = Vector3(-4.6,3.35,z_value)
		light.light_color = Color("ffe8b8")
		light.light_energy = 1.1
		light.omni_range = 4.5
		root.add_child(light)

	# Yard surfaces and circulation gates retained from the circulation pass.
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

func _build_art_details() -> void:
	# Tire tracks and compressed snow make the exterior less pristine.
	var track_mat: StandardMaterial3D = _textured_mat(Color("818b8f"),0.94,0.0,"asphalt",64,0.025)
	for x_offset: float in [-0.65,0.65]:
		var track: MeshInstance3D = _box(Vector3(0.18,0.018,31.0),track_mat,Vector3(2.5+x_offset,0.075,24.0),self)
		track.rotation.y = -0.03
	for i: int in range(16):
		var footprint: MeshInstance3D = _box(Vector3(0.13,0.018,0.30),track_mat,Vector3(-4.9+0.20*float(i%2),0.08,12.5+float(i)*0.55),self)
		footprint.rotation.y = -0.18 if i % 2 == 0 else 0.14
	# A small Rosvik place sign anchors the scene without inventing a private property.
	var sign_root: Node3D = Node3D.new()
	sign_root.position = Vector3(-39.0,0.0,-25.0)
	add_child(sign_root)
	_solid_box(Vector3(0.10,2.2,0.10),metal_mat,Vector3(-1.6,1.1,0.0),sign_root)
	_solid_box(Vector3(0.10,2.2,0.10),metal_mat,Vector3(1.6,1.1,0.0),sign_root)
	var blue: StandardMaterial3D = _mat(Color("315f8b"),0.78,0.08)
	_box(Vector3(3.5,0.82,0.10),blue,Vector3(0.0,1.8,0.0),sign_root)
	_label3d("ROSVIK",Vector3(0.0,1.82,0.08),sign_root,26)

func _build_player() -> void:
	player = CharacterBody3D.new()
	player.set_script(ART_PLAYER_SCRIPT)
	player.position = Vector3(-4.6,0.0,11.0)
	add_child(player)
	camera = Camera3D.new()
	camera.fov = 43.0
	camera.current = true
	camera.near = 0.1
	camera.far = 300.0
	add_child(camera)
	_update_camera(1.0)

func _build_ui() -> void:
	var ui: CanvasLayer = CanvasLayer.new()
	add_child(ui)
	var panel: ColorRect = ColorRect.new()
	panel.position = Vector2(18.0,18.0)
	panel.size = Vector2(275.0,98.0)
	panel.color = Color(0.018,0.028,0.035,0.78)
	ui.add_child(panel)
	var label: Label = Label.new()
	label.position = Vector2(14.0,10.0)
	label.text = "ROSVIK: BLACKOUT\nART / INTERIOR PASS 01.1\n\nRättvänd locomotion + fri skolentré."
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
		lead = lead.normalized()*1.7
	var focus: Vector3 = player.global_position+lead+Vector3(0.0,1.0,0.0)
	target_camera_pos = focus+Vector3(11.8,8.2,11.8)
	camera.global_position = camera.global_position.lerp(target_camera_pos,1.0-exp(-5.2*delta))
	camera.look_at(focus,Vector3.UP)
