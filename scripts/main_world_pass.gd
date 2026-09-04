extends Node3D

const PLAYER_SCRIPT: Script = preload("res://scripts/player.gd")

var player: CharacterBody3D
var camera: Camera3D
var target_camera_pos: Vector3 = Vector3.ZERO
var time_of_day: float = 16.7

var snow_mat: StandardMaterial3D
var packed_snow_mat: StandardMaterial3D
var asphalt_mat: StandardMaterial3D
var slush_mat: StandardMaterial3D
var dark_mat: StandardMaterial3D
var school_mat: StandardMaterial3D
var arena_mat: StandardMaterial3D
var glass_mat: StandardMaterial3D
var metal_mat: StandardMaterial3D
var wood_mat: StandardMaterial3D
var concrete_mat: StandardMaterial3D
var roof_mat: StandardMaterial3D
var warm_windows: Array[MeshInstance3D] = []
var lamps: Array[OmniLight3D] = []

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
	_build_player()
	_build_ui()
	print("ROSVIK_WORLD_PASS_READY")

func _process(delta: float) -> void:
	time_of_day = fmod(time_of_day + delta * 0.0015, 24.0)
	_update_camera(delta)
	_update_world()

func _build_materials() -> void:
	snow_mat = _textured_mat(Color("cbd7dd"), 0.985, 0.0, "snow", 96, 0.045)
	packed_snow_mat = _textured_mat(Color("b9c6cc"), 0.97, 0.0, "snow", 96, 0.035)
	asphalt_mat = _textured_mat(Color("34383b"), 0.96, 0.0, "asphalt", 96, 0.055)
	slush_mat = _textured_mat(Color("6d7478"), 0.93, 0.0, "asphalt", 96, 0.045)
	dark_mat = _mat(Color("22292d"), 0.88)
	school_mat = _textured_mat(Color("a7b0b3"), 0.90, 0.0, "horizontal", 96, 0.035)
	arena_mat = _textured_mat(Color("5e686d"), 0.76, 0.10, "vertical", 96, 0.035)
	glass_mat = _mat(Color("587081"), 0.18, 0.04)
	metal_mat = _mat(Color("58646a"), 0.60, 0.32)
	wood_mat = _textured_mat(Color("6f4e39"), 0.94, 0.0, "horizontal", 96, 0.05)
	concrete_mat = _textured_mat(Color("888f92"), 0.95, 0.0, "noise", 96, 0.04)
	roof_mat = _textured_mat(Color("3b4246"), 0.86, 0.08, "noise", 96, 0.035)

func _build_environment() -> void:
	var env_node: WorldEnvironment = WorldEnvironment.new()
	var env: Environment = Environment.new()
	env.background_mode = Environment.BG_COLOR
	env.background_color = Color("4b5b65")
	env.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	env.ambient_light_color = Color("8498a4")
	env.ambient_light_energy = 0.48
	env.fog_enabled = true
	env.fog_light_color = Color("657680")
	env.fog_density = 0.009
	env.fog_height = 0.3
	env.fog_height_density = 0.10
	env.tonemap_mode = Environment.TONE_MAPPER_FILMIC
	env.tonemap_exposure = 0.72
	env_node.environment = env
	add_child(env_node)

	var sun: DirectionalLight3D = DirectionalLight3D.new()
	sun.rotation_degrees = Vector3(-42.0, -31.0, 0.0)
	sun.light_color = Color("ffd0aa")
	sun.light_energy = 0.95
	sun.shadow_enabled = true
	sun.directional_shadow_max_distance = 95.0
	add_child(sun)

func _build_ground() -> void:
	_box(Vector3(240.0, 0.32, 200.0), snow_mat, Vector3(0.0, -0.17, 6.0), self)

	# Uneven wind-scoured snow fields.
	var fields: Array[Vector3] = [
		Vector3(-48.0, 0.01, -11.0), Vector3(-9.0, 0.01, 43.0),
		Vector3(35.0, 0.01, -19.0), Vector3(69.0, 0.01, 38.0),
		Vector3(8.0, 0.01, -47.0), Vector3(-66.0, 0.01, 42.0)
	]
	for p: Vector3 in fields:
		var patch: MeshInstance3D = _box(Vector3(randf_range(18.0, 34.0), 0.045, randf_range(10.0, 20.0)), packed_snow_mat, p, self)
		patch.rotation.y = randf_range(-0.25, 0.25)

func _build_roads() -> void:
	_add_road(Vector3(-100.0, 0.0, -30.0), Vector3(100.0, 0.0, -30.0), 7.2)
	_add_road(Vector3(-42.0, 0.0, -88.0), Vector3(-42.0, 0.0, 78.0), 6.0)
	_add_road(Vector3(-42.0, 0.0, 10.0), Vector3(52.0, 0.0, 10.0), 5.4)
	_add_road(Vector3(52.0, 0.0, 10.0), Vector3(52.0, 0.0, 68.0), 5.0)

	# Dirty shoulders and plow banks make roads sit in the snow.
	for z_value: float in [-34.4, -25.6]:
		_box(Vector3(200.0, 0.20, 1.05), slush_mat, Vector3(0.0, 0.08, z_value), self)
	for x_value: float in [-45.7, -38.3]:
		_box(Vector3(1.0, 0.20, 166.0), slush_mat, Vector3(x_value, 0.08, -5.0), self)

func _add_road(a: Vector3, b: Vector3, width: float) -> void:
	var d: Vector3 = b - a
	var length: float = d.length()
	var road: MeshInstance3D = _box(Vector3(length, 0.07, width), asphalt_mat, (a + b) * 0.5 + Vector3(0.0, 0.025, 0.0), self)
	road.rotation.y = -atan2(d.z, d.x)
	if width >= 5.4:
		var line_mat: StandardMaterial3D = _mat(Color("b9b8ae"), 0.92)
		var count: int = maxi(1, int(length / 8.0))
		for i: int in range(count):
			var t: float = (float(i) + 0.5) / float(count)
			var p: Vector3 = a.lerp(b, t)
			var dash: MeshInstance3D = _box(Vector3(2.2, 0.018, 0.11), line_mat, p + Vector3(0.0, 0.07, 0.0), self)
			dash.rotation.y = road.rotation.y

func _build_school() -> void:
	var root: Node3D = Node3D.new()
	root.name = "RosviksSkola"
	root.position = Vector3(4.0, 0.0, -1.0)
	add_child(root)

	# Multiple volumes instead of one slab.
	_box(Vector3(27.0, 4.3, 12.5), school_mat, Vector3(1.5, 2.15, 0.0), root)
	_box(Vector3(11.0, 3.2, 10.0), school_mat, Vector3(-15.8, 1.6, 1.3), root)
	_box(Vector3(8.0, 3.5, 7.0), _textured_mat(Color("8f9a9e"), 0.9, 0.0, "horizontal", 96, 0.03), Vector3(16.5, 1.75, -1.8), root)

	# Dark roof/parapet, gutters and snow caps.
	_box(Vector3(28.0, 0.34, 13.4), roof_mat, Vector3(1.5, 4.46, 0.0), root)
	_box(Vector3(12.0, 0.30, 10.8), roof_mat, Vector3(-15.8, 3.36, 1.3), root)
	_box(Vector3(27.6, 0.13, 13.0), packed_snow_mat, Vector3(1.5, 4.68, 0.0), root)
	_box(Vector3(11.6, 0.12, 10.4), packed_snow_mat, Vector3(-15.8, 3.57, 1.3), root)

	# Facade base and windows recessed into darker surrounds.
	_box(Vector3(27.2, 0.9, 0.18), concrete_mat, Vector3(1.5, 0.48, 6.32), root)
	for i: int in range(9):
		var x: float = -9.0 + float(i) * 2.65
		_add_window(root, Vector3(x, 2.25, 6.39), Vector3(1.45, 1.15, 0.12))

	# Main entrance gets depth, side walls and steps.
	_box(Vector3(4.8, 3.0, 2.3), dark_mat, Vector3(-4.6, 1.5, 7.0), root)
	_box(Vector3(3.1, 2.5, 0.15), glass_mat, Vector3(-4.6, 1.3, 8.18), root)
	_box(Vector3(5.8, 0.28, 3.0), roof_mat, Vector3(-4.6, 3.05, 8.2), root)
	_box(Vector3(4.6, 0.16, 2.8), concrete_mat, Vector3(-4.6, 0.08, 9.0), root)
	_box(Vector3(4.1, 0.12, 1.5), concrete_mat, Vector3(-4.6, 0.22, 10.1), root)

	# Roof equipment and downpipes.
	for x_value: float in [-5.5, 2.0, 8.0]:
		_box(Vector3(1.7, 0.72, 1.2), metal_mat, Vector3(x_value, 5.08, -1.1), root)
	for x_value: float in [-11.5, 12.8]:
		_cylinder(0.055, 4.0, metal_mat, Vector3(x_value, 2.0, 6.52), root)

	# Schoolyard: plowed asphalt, fence, racks, benches and play/sport objects.
	_box(Vector3(33.0, 0.08, 20.0), packed_snow_mat, Vector3(-1.0, 0.03, 19.0), root)
	_box(Vector3(13.0, 0.06, 8.0), slush_mat, Vector3(-4.0, 0.055, 11.5), root)
	_add_fence(Vector3(-18.0, 0.0, 13.0), Vector3(15.0, 0.0, 13.0), root)
	_add_fence(Vector3(-18.0, 0.0, 13.0), Vector3(-18.0, 0.0, 31.0), root)
	_add_fence(Vector3(15.0, 0.0, 13.0), Vector3(15.0, 0.0, 31.0), root)
	_add_fence(Vector3(-18.0, 0.0, 31.0), Vector3(15.0, 0.0, 31.0), root)
	for i: int in range(7):
		var rack: MeshInstance3D = _box(Vector3(0.07, 0.55, 1.45), metal_mat, Vector3(-7.0 + float(i) * 0.55, 0.30, 11.7), root)
		rack.rotation.z = 0.18
	_add_bench(Vector3(-11.0, 0.0, 18.0), root)
	_add_bench(Vector3(9.0, 0.0, 18.0), root)
	_add_goal(Vector3(-9.0, 0.0, 26.0), root)
	_add_basket(Vector3(8.0, 0.0, 25.0), root)
	_add_flag(Vector3(11.0, 0.0, 10.3), root)
	_add_dumpster(Vector3(-16.0, 0.0, 9.0), root)
	_label3d("ROSVIKS SKOLA", Vector3(-4.5, 3.9, 8.38), root, 42)

func _build_arena() -> void:
	var root: Node3D = Node3D.new()
	root.name = "NorrbottenStalArena"
	root.position = Vector3(46.0, 0.0, 27.0)
	add_child(root)

	# Main rink hall + separate entrance/service volumes.
	_box(Vector3(39.0, 7.0, 21.0), arena_mat, Vector3(0.0, 3.5, 0.0), root)
	_box(Vector3(10.0, 3.2, 5.0), _textured_mat(Color("48565e"), 0.8, 0.08, "vertical", 96, 0.03), Vector3(-9.0, 1.6, 12.1), root)
	_box(Vector3(9.0, 3.9, 4.2), _textured_mat(Color("515b60"), 0.8, 0.10, "vertical", 96, 0.03), Vector3(9.8, 1.95, 12.0), root)

	_box(Vector3(40.0, 0.38, 22.0), roof_mat, Vector3(0.0, 7.18, 0.0), root)
	_box(Vector3(39.5, 0.14, 21.5), packed_snow_mat, Vector3(0.0, 7.42, 0.0), root)
	_box(Vector3(39.2, 1.1, 0.18), _mat(Color("303c43"), 0.8), Vector3(0.0, 0.58, 10.58), root)

	# Corrugated ribs cast small shadows and stop the facade reading flat.
	for i: int in range(25):
		var x: float = -18.0 + float(i) * 1.5
		_box(Vector3(0.045, 5.2, 0.08), _mat(Color("778185"), 0.72, 0.10), Vector3(x, 3.4, 10.68), root)

	for i: int in range(7):
		var x: float = -15.0 + float(i) * 4.5
		_add_window(root, Vector3(x, 4.8, 10.72), Vector3(2.2, 0.95, 0.12))

	# Entrance / loading dock details.
	_box(Vector3(3.2, 2.55, 0.14), glass_mat, Vector3(-9.0, 1.35, 14.64), root)
	_box(Vector3(5.6, 0.3, 2.6), roof_mat, Vector3(-9.0, 3.28, 14.2), root)
	_box(Vector3(4.8, 3.2, 0.16), dark_mat, Vector3(10.2, 1.65, 14.14), root)
	_box(Vector3(6.0, 0.45, 2.5), concrete_mat, Vector3(10.2, 0.22, 15.0), root)
	_add_dumpster(Vector3(14.0, 0.0, 13.4), root)

	# Roof vents and exhaust stacks.
	for x_value: float in [-12.0, -4.0, 4.0, 12.0]:
		_box(Vector3(1.4, 0.8, 1.1), metal_mat, Vector3(x_value, 7.9, -1.0), root)
		_cylinder(0.22, 1.2, metal_mat, Vector3(x_value + 0.4, 8.3, -0.6), root)

	# Parking area is asphalt/slush, not white void.
	_box(Vector3(52.0, 0.07, 19.0), slush_mat, Vector3(0.0, 0.035, 22.0), root)
	var line_mat: StandardMaterial3D = _mat(Color("b5b5ad"), 0.9)
	for i: int in range(14):
		_box(Vector3(0.10, 0.018, 2.4), line_mat, Vector3(-19.5 + float(i) * 3.0, 0.085, 22.0), root)
	_add_flag(Vector3(-14.0, 0.0, 14.8), root)
	_label3d("NORRBOTTEN STÅL ARENA", Vector3(0.0, 6.0, 10.86), root, 38)

func _build_background() -> void:
	# A few Rosvik-scale homes and utility buildings give the slice a real neighborhood edge.
	_add_house(Vector3(-24.0, 0.0, -8.0), Color("8c5b4d"), 0.05)
	_add_house(Vector3(-8.0, 0.0, -12.0), Color("b7a268"), -0.08)
	_add_house(Vector3(17.0, 0.0, -14.0), Color("657b85"), 0.10)
	_add_house(Vector3(33.0, 0.0, -9.0), Color("8a7965"), -0.06)
	_add_house(Vector3(-59.0, 0.0, 24.0), Color("667d68"), 0.12)
	_add_house(Vector3(-57.0, 0.0, 48.0), Color("a27e62"), -0.10)

	# Tree groups rather than only a distant ring.
	for i: int in range(58):
		var side: int = -1 if i % 2 == 0 else 1
		var x: float = randf_range(58.0, 100.0) * float(side)
		var z: float = randf_range(-70.0, 80.0)
		_add_tree(Vector3(x, 0.0, z))
	for i: int in range(20):
		_add_tree(Vector3(randf_range(-80.0, 80.0), 0.0, randf_range(62.0, 88.0)))

func _build_props() -> void:
	_add_car(Vector3(-7.0, 0.0, -5.0), Color("56646e"), 0.12, true)
	_add_car(Vector3(3.0, 0.0, 13.5), Color("836b59"), 0.03, false)
	_add_car(Vector3(26.0, 0.0, 45.0), Color("6d6259"), -0.30, false)
	_add_car(Vector3(38.0, 0.0, 45.0), Color("445866"), -0.15, false)
	_add_car(Vector3(57.0, 0.0, 47.0), Color("5b6066"), 0.06, false)
	_add_car(Vector3(68.0, 0.0, 46.0), Color("7b5e52"), -0.08, false)

	var lamp_positions: Array[Vector3] = [
		Vector3(-29.0,0.0,-19.0), Vector3(-10.0,0.0,-19.0),
		Vector3(10.0,0.0,-19.0), Vector3(31.0,0.0,-19.0),
		Vector3(49.0,0.0,8.0), Vector3(49.0,0.0,36.0),
		Vector3(49.0,0.0,55.0), Vector3(12.0,0.0,18.0)
	]
	for p: Vector3 in lamp_positions:
		_add_lamp(p)

	# Plow banks and dirty piles form edges around parking lots.
	var bank_positions: Array[Vector3] = [
		Vector3(-16.0,0.0,10.0), Vector3(14.0,0.0,12.0),
		Vector3(23.0,0.0,51.0), Vector3(37.0,0.0,53.0),
		Vector3(55.0,0.0,55.0), Vector3(70.0,0.0,53.0),
		Vector3(-28.0,0.0,20.0), Vector3(-4.0,0.0,31.0)
	]
	for p: Vector3 in bank_positions:
		_add_snowbank(p)

	# Utility cabinets and bollards.
	for p: Vector3 in [Vector3(19.0,0.0,8.0), Vector3(39.0,0.0,10.0), Vector3(63.0,0.0,27.0)]:
		_box(Vector3(0.75, 1.4, 0.45), metal_mat, p + Vector3(0.0, 0.7, 0.0), self)

func _build_player() -> void:
	player = CharacterBody3D.new()
	player.set_script(PLAYER_SCRIPT)
	player.position = Vector3(-4.0, 0.0, 13.0)
	add_child(player)

	camera = Camera3D.new()
	camera.fov = 47.0
	camera.current = true
	camera.near = 0.1
	camera.far = 230.0
	add_child(camera)
	_update_camera(1.0)

func _build_ui() -> void:
	var ui: CanvasLayer = CanvasLayer.new()
	add_child(ui)

	var panel: ColorRect = ColorRect.new()
	panel.position = Vector2(18.0, 18.0)
	panel.size = Vector2(300.0, 116.0)
	panel.color = Color(0.02, 0.032, 0.042, 0.86)
	ui.add_child(panel)

	var label: Label = Label.new()
	label.position = Vector2(16.0, 12.0)
	label.text = "ROSVIK: BLACKOUT\nWORLD PASS 01\n\nTa dig mot ishallen."
	label.add_theme_font_size_override("font_size", 16)
	panel.add_child(label)

	var hint: Label = Label.new()
	hint.position = Vector2(18.0, 852.0)
	hint.text = "WASD / pilar: gå     Shift: spring"
	hint.add_theme_font_size_override("font_size", 14)
	ui.add_child(hint)

func _update_camera(delta: float) -> void:
	if player == null or camera == null:
		return
	var lead: Vector3 = Vector3(player.velocity.x, 0.0, player.velocity.z)
	if lead.length() > 0.15:
		lead = lead.normalized() * 2.0
	var focus: Vector3 = player.global_position + lead + Vector3(0.0, 1.0, 0.0)
	target_camera_pos = focus + Vector3(14.0, 10.5, 14.0)
	camera.global_position = camera.global_position.lerp(target_camera_pos, 1.0 - exp(-5.0 * delta))
	camera.look_at(focus, Vector3.UP)

func _update_world() -> void:
	var dusk: float = clampf((time_of_day - 15.2) / 3.2, 0.0, 1.0)
	for w: MeshInstance3D in warm_windows:
		var material: Material = w.material_override
		if material is StandardMaterial3D:
			var standard: StandardMaterial3D = material as StandardMaterial3D
			standard.emission_enabled = true
			standard.emission = Color("ff9a4a")
			standard.emission_energy_multiplier = lerpf(0.10, 1.35, dusk)
	for lamp: OmniLight3D in lamps:
		lamp.light_energy = lerpf(0.15, 2.0, dusk)

func _mat(color: Color, rough: float = 0.85, metallic: float = 0.0) -> StandardMaterial3D:
	var m: StandardMaterial3D = StandardMaterial3D.new()
	m.albedo_color = color
	m.roughness = rough
	m.metallic = metallic
	return m

func _textured_mat(base: Color, rough: float, metallic: float, pattern: String, size: int, variance: float) -> StandardMaterial3D:
	var m: StandardMaterial3D = _mat(Color.WHITE, rough, metallic)
	var image: Image = Image.create(size, size, false, Image.FORMAT_RGBA8)
	var rng: RandomNumberGenerator = RandomNumberGenerator.new()
	rng.seed = 424242 + pattern.hash()
	for y: int in range(size):
		for x: int in range(size):
			var noise_value: float = rng.randf_range(-variance, variance)
			if pattern == "horizontal" and y % 14 < 2:
				noise_value -= 0.07
			elif pattern == "vertical" and x % 11 < 2:
				noise_value -= 0.08
			elif pattern == "asphalt" and ((x * 13 + y * 7) % 97) < 4:
				noise_value += 0.08
			elif pattern == "snow" and ((x * 5 + y * 11) % 83) < 3:
				noise_value += 0.05
			var c: Color = Color(
				clampf(base.r + noise_value, 0.0, 1.0),
				clampf(base.g + noise_value, 0.0, 1.0),
				clampf(base.b + noise_value, 0.0, 1.0),
				1.0
			)
			image.set_pixel(x, y, c)
	var texture: ImageTexture = ImageTexture.create_from_image(image)
	m.albedo_texture = texture
	m.uv1_scale = Vector3(4.0, 4.0, 4.0)
	return m

func _box(size: Vector3, mat: Material, pos: Vector3, parent: Node) -> MeshInstance3D:
	var node: MeshInstance3D = MeshInstance3D.new()
	var mesh: BoxMesh = BoxMesh.new()
	mesh.size = size
	node.mesh = mesh
	node.material_override = mat
	node.position = pos
	node.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_ON
	parent.add_child(node)
	return node

func _cylinder(radius: float, height: float, mat: Material, pos: Vector3, parent: Node) -> MeshInstance3D:
	var node: MeshInstance3D = MeshInstance3D.new()
	var mesh: CylinderMesh = CylinderMesh.new()
	mesh.top_radius = radius
	mesh.bottom_radius = radius
	mesh.height = height
	node.mesh = mesh
	node.material_override = mat
	node.position = pos
	node.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_ON
	parent.add_child(node)
	return node

func _cone(radius: float, height: float, mat: Material, pos: Vector3, parent: Node) -> MeshInstance3D:
	var node: MeshInstance3D = MeshInstance3D.new()
	var mesh: CylinderMesh = CylinderMesh.new()
	mesh.top_radius = 0.0
	mesh.bottom_radius = radius
	mesh.height = height
	node.mesh = mesh
	node.material_override = mat
	node.position = pos
	node.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_ON
	parent.add_child(node)
	return node

func _add_window(parent: Node, pos: Vector3, size: Vector3) -> void:
	_box(Vector3(size.x + 0.24, size.y + 0.24, 0.10), dark_mat, pos + Vector3(0.0, 0.0, -0.04), parent)
	var window_mat: StandardMaterial3D = glass_mat.duplicate() as StandardMaterial3D
	var window: MeshInstance3D = _box(size, window_mat, pos + Vector3(0.0, 0.0, 0.03), parent)
	warm_windows.append(window)
	_box(Vector3(size.x + 0.32, 0.08, 0.20), concrete_mat, pos + Vector3(0.0, -size.y * 0.5 - 0.10, 0.02), parent)

func _add_car(pos: Vector3, color: Color, yaw: float, open_door: bool) -> void:
	var root: Node3D = Node3D.new()
	root.position = pos
	root.rotation.y = yaw
	add_child(root)
	var body_mat: StandardMaterial3D = _mat(color, 0.66, 0.18)
	_box(Vector3(4.45, 0.55, 1.82), body_mat, Vector3(0.0, 0.50, 0.0), root)
	_box(Vector3(2.25, 0.62, 1.58), body_mat, Vector3(-0.20, 1.08, 0.0), root)
	_box(Vector3(1.70, 0.43, 1.61), glass_mat, Vector3(-0.20, 1.18, 0.0), root)
	_box(Vector3(0.12, 0.45, 1.64), dark_mat, Vector3(0.72, 1.17, 0.0), root)
	for x_value: float in [-1.42, 1.42]:
		for z_value: float in [-0.92, 0.92]:
			var wheel: MeshInstance3D = _cylinder(0.34, 0.24, _mat(Color("17191b"), 1.0), Vector3(x_value, 0.36, z_value), root)
			wheel.rotation.x = PI / 2.0
	_box(Vector3(1.9, 0.07, 1.2), packed_snow_mat, Vector3(-0.20, 1.47, 0.0), root)
	if open_door:
		var door: MeshInstance3D = _box(Vector3(1.12, 0.92, 0.08), body_mat, Vector3(-0.42, 1.04, 1.13), root)
		door.rotation.y = 0.62

func _add_house(pos: Vector3, color: Color, yaw: float) -> void:
	var root: Node3D = Node3D.new()
	root.position = pos
	root.rotation.y = yaw
	add_child(root)
	var wall: StandardMaterial3D = _textured_mat(color, 0.94, 0.0, "horizontal", 64, 0.035)
	_box(Vector3(10.0, 3.0, 7.0), wall, Vector3(0.0, 1.5, 0.0), root)
	_box(Vector3(10.6, 0.35, 7.6), roof_mat, Vector3(0.0, 3.18, 0.0), root)
	_box(Vector3(10.2, 0.12, 7.2), packed_snow_mat, Vector3(0.0, 3.42, 0.0), root)
	_add_window(root, Vector3(-2.7, 1.7, 3.55), Vector3(1.4, 1.1, 0.10))
	_add_window(root, Vector3(1.0, 1.7, 3.55), Vector3(1.4, 1.1, 0.10))
	_box(Vector3(1.1, 2.1, 0.12), dark_mat, Vector3(3.4, 1.05, 3.56), root)

func _add_tree(pos: Vector3) -> void:
	var root: Node3D = Node3D.new()
	root.position = pos
	root.scale = Vector3.ONE * randf_range(0.82, 1.22)
	add_child(root)
	_cylinder(0.14, 1.8, _mat(Color("594234"), 1.0), Vector3(0.0, 0.9, 0.0), root)
	var pine: StandardMaterial3D = _mat(Color("24433d"), 0.97)
	_cone(1.15, 2.0, pine, Vector3(0.0, 2.1, 0.0), root)
	_cone(0.90, 1.7, pine, Vector3(0.0, 3.0, 0.0), root)
	_cone(0.66, 1.35, pine, Vector3(0.0, 3.8, 0.0), root)
	_box(Vector3(1.2, 0.09, 1.2), packed_snow_mat, Vector3(0.0, 2.95, 0.0), root)

func _add_lamp(pos: Vector3) -> void:
	var root: Node3D = Node3D.new()
	root.position = pos
	add_child(root)
	_cylinder(0.055, 4.4, dark_mat, Vector3(0.0, 2.2, 0.0), root)
	_box(Vector3(0.76, 0.09, 0.12), dark_mat, Vector3(0.32, 4.26, 0.0), root)
	var bulb_mat: StandardMaterial3D = _mat(Color("f2bb67"), 0.4)
	bulb_mat.emission_enabled = true
	bulb_mat.emission = Color("ffb85f")
	bulb_mat.emission_energy_multiplier = 1.2
	_box(Vector3(0.22, 0.10, 0.16), bulb_mat, Vector3(0.63, 4.20, 0.0), root)
	var light: OmniLight3D = OmniLight3D.new()
	light.position = Vector3(0.60, 4.05, 0.0)
	light.light_color = Color("ffc071")
	light.omni_range = 9.0
	light.light_energy = 0.15
	light.shadow_enabled = false
	root.add_child(light)
	lamps.append(light)

func _add_snowbank(pos: Vector3) -> void:
	var bank: MeshInstance3D = _box(Vector3(randf_range(4.0, 8.0), randf_range(0.5, 0.9), randf_range(1.4, 2.5)), packed_snow_mat, pos + Vector3(0.0, 0.28, 0.0), self)
	bank.rotation.y = randf_range(-0.22, 0.22)
	var grit: MeshInstance3D = _box(Vector3(randf_range(2.0, 4.0), 0.06, randf_range(0.6, 1.1)), slush_mat, pos + Vector3(0.0, 0.04, randf_range(-0.4, 0.4)), self)
	grit.rotation.y = bank.rotation.y

func _add_dumpster(pos: Vector3, parent: Node) -> void:
	var green: StandardMaterial3D = _mat(Color("3f5148"), 0.84, 0.08)
	_box(Vector3(1.6, 1.0, 0.9), green, pos + Vector3(0.0, 0.5, 0.0), parent)
	var lid: MeshInstance3D = _box(Vector3(1.7, 0.10, 0.95), dark_mat, pos + Vector3(0.0, 1.03, 0.0), parent)
	lid.rotation.x = -0.08

func _add_fence(a: Vector3, b: Vector3, parent: Node) -> void:
	var d: Vector3 = b - a
	var length: float = d.length()
	var yaw: float = -atan2(d.z, d.x)
	for y_value: float in [0.52, 0.92]:
		var rail: MeshInstance3D = _box(Vector3(length, 0.05, 0.05), dark_mat, (a + b) * 0.5 + Vector3(0.0, y_value, 0.0), parent)
		rail.rotation.y = yaw
	var count: int = maxi(2, int(length / 1.8))
	for i: int in range(count + 1):
		var p: Vector3 = a.lerp(b, float(i) / float(count))
		_box(Vector3(0.06, 1.18, 0.06), dark_mat, p + Vector3(0.0, 0.59, 0.0), parent)

func _add_bench(pos: Vector3, parent: Node) -> void:
	_box(Vector3(2.0, 0.12, 0.50), wood_mat, pos + Vector3(0.0, 0.58, 0.0), parent)
	_box(Vector3(2.0, 0.12, 0.22), wood_mat, pos + Vector3(0.0, 1.0, -0.22), parent)
	for x_value: float in [-0.7, 0.7]:
		_box(Vector3(0.10, 0.58, 0.10), metal_mat, pos + Vector3(x_value, 0.3, 0.0), parent)

func _add_goal(pos: Vector3, parent: Node) -> void:
	var white: StandardMaterial3D = _mat(Color("d8dddf"), 0.82)
	_box(Vector3(2.4, 0.08, 0.08), white, pos + Vector3(0.0, 1.25, 0.0), parent)
	for x_value: float in [-1.2, 1.2]:
		_box(Vector3(0.08, 1.3, 0.08), white, pos + Vector3(x_value, 0.65, 0.0), parent)

func _add_basket(pos: Vector3, parent: Node) -> void:
	_box(Vector3(0.08, 3.1, 0.08), dark_mat, pos + Vector3(0.0, 1.55, 0.0), parent)
	_box(Vector3(1.05, 0.70, 0.07), _mat(Color("d9dee0"), 0.84), pos + Vector3(0.0, 2.7, 0.0), parent)
	var rim: MeshInstance3D = _cylinder(0.22, 0.04, _mat(Color("b95f32"), 0.75), pos + Vector3(0.0, 2.42, -0.18), parent)
	rim.rotation.x = PI / 2.0

func _add_flag(pos: Vector3, parent: Node) -> void:
	_cylinder(0.045, 5.5, metal_mat, pos + Vector3(0.0, 2.75, 0.0), parent)
	_box(Vector3(1.4, 0.62, 0.04), _mat(Color("c99b4c"), 0.88), pos + Vector3(0.72, 4.65, 0.0), parent)

func _label3d(text_value: String, pos: Vector3, parent: Node, font_size_value: int) -> void:
	var label: Label3D = Label3D.new()
	label.text = text_value
	label.font_size = font_size_value
	label.outline_size = 8
	label.modulate = Color("e5ecef")
	label.outline_modulate = Color("1b252b")
	label.position = pos
	parent.add_child(label)
