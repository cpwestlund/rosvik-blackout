extends Node3D

const PLAYER_SCRIPT: Script = preload("res://scripts/player.gd")

var player: CharacterBody3D
var camera: Camera3D
var target_camera_pos: Vector3 = Vector3.ZERO

var snow_mat: StandardMaterial3D
var road_mat: StandardMaterial3D
var dark_mat: StandardMaterial3D
var school_mat: StandardMaterial3D
var arena_mat: StandardMaterial3D
var glass_mat: StandardMaterial3D
var metal_mat: StandardMaterial3D
var wood_mat: StandardMaterial3D

var warm_windows: Array[MeshInstance3D] = []
var lamps: Array[OmniLight3D] = []
var time_of_day: float = 16.55

func _ready() -> void:
	_build_materials()
	_build_environment()
	_build_ground()
	_build_roads()
	_build_school()
	_build_arena()
	_build_props()
	_build_player()
	_build_ui()
	print("ROSVIK_RUNTIME_READY")

func _process(delta: float) -> void:
	time_of_day = fmod(time_of_day + delta * 0.002, 24.0)
	_update_camera(delta)
	_update_world()

func _build_materials() -> void:
	snow_mat = _mat(Color("dce5ea"), 0.98)
	road_mat = _mat(Color("343b40"), 0.95)
	dark_mat = _mat(Color("20282e"), 0.9)
	school_mat = _mat(Color("aeb8bc"), 0.9)
	arena_mat = _mat(Color("626d74"), 0.78, 0.12)
	glass_mat = _mat(Color("6f8798"), 0.22, 0.04)
	metal_mat = _mat(Color("59666e"), 0.62, 0.3)
	wood_mat = _mat(Color("77543d"), 0.94)

func _build_environment() -> void:
	var env_node: WorldEnvironment = WorldEnvironment.new()
	var env: Environment = Environment.new()
	env.background_mode = Environment.BG_COLOR
	env.background_color = Color("5f707c")
	env.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	env.ambient_light_color = Color("9badb9")
	env.ambient_light_energy = 0.72
	env.fog_enabled = true
	env.fog_light_color = Color("788690")
	env.fog_density = 0.006
	env.fog_height = 0.5
	env.fog_height_density = 0.08
	env.tonemap_mode = Environment.TONE_MAPPER_FILMIC
	env_node.environment = env
	add_child(env_node)

	var sun: DirectionalLight3D = DirectionalLight3D.new()
	sun.rotation_degrees = Vector3(-48.0, -34.0, 0.0)
	sun.light_color = Color("ffd1ac")
	sun.light_energy = 1.55
	sun.shadow_enabled = true
	sun.directional_shadow_max_distance = 110.0
	add_child(sun)

func _build_ground() -> void:
	_box(Vector3(220.0, 0.35, 180.0), snow_mat, Vector3(0.0, -0.18, 5.0), self)
	var patch_mat: StandardMaterial3D = _mat(Color("e6edf1"), 0.99)
	var patches: Array[Vector3] = [
		Vector3(-42.0, 0.02, -18.0),
		Vector3(35.0, 0.02, 25.0),
		Vector3(-5.0, 0.02, 48.0),
		Vector3(55.0, 0.02, -28.0)
	]
	for p: Vector3 in patches:
		var patch: MeshInstance3D = _box(Vector3(28.0, 0.05, 18.0), patch_mat, p, self)
		patch.rotation.y = randf_range(-0.16, 0.16)

func _build_roads() -> void:
	_add_road(Vector3(-90.0, 0.0, -27.0), Vector3(92.0, 0.0, -27.0), 7.2)
	_add_road(Vector3(-38.0, 0.0, -80.0), Vector3(-38.0, 0.0, 72.0), 6.2)
	_add_road(Vector3(-38.0, 0.0, 12.0), Vector3(48.0, 0.0, 12.0), 5.4)
	_add_road(Vector3(48.0, 0.0, 12.0), Vector3(48.0, 0.0, 62.0), 5.0)

	var shoulder_mat: StandardMaterial3D = _mat(Color("e8eef2"), 0.99)
	_box(Vector3(182.0, 0.3, 1.35), shoulder_mat, Vector3(1.0, 0.12, -31.2), self)
	_box(Vector3(182.0, 0.3, 1.35), shoulder_mat, Vector3(1.0, 0.12, -22.8), self)
	_box(Vector3(1.3, 0.3, 152.0), shoulder_mat, Vector3(-42.0, 0.12, -4.0), self)
	_box(Vector3(1.3, 0.3, 152.0), shoulder_mat, Vector3(-34.0, 0.12, -4.0), self)

func _add_road(a: Vector3, b: Vector3, width: float) -> void:
	var d: Vector3 = b - a
	var length: float = d.length()
	var road: MeshInstance3D = _box(Vector3(length, 0.08, width), road_mat, (a + b) * 0.5 + Vector3(0.0, 0.02, 0.0), self)
	road.rotation.y = -atan2(d.z, d.x)
	if width >= 5.5:
		var count: int = maxi(1, int(length / 7.0))
		var line_mat: StandardMaterial3D = _mat(Color("e7e9e9"), 0.85)
		for i: int in range(count):
			var t: float = (float(i) + 0.5) / float(count)
			var p: Vector3 = a.lerp(b, t)
			var dash: MeshInstance3D = _box(Vector3(2.4, 0.015, 0.13), line_mat, p + Vector3(0.0, 0.075, 0.0), self)
			dash.rotation.y = road.rotation.y

func _build_school() -> void:
	var root: Node3D = Node3D.new()
	root.name = "RosviksSkola"
	root.position = Vector3(6.0, 0.0, 1.0)
	add_child(root)

	_box(Vector3(34.0, 4.5, 14.0), school_mat, Vector3(0.0, 2.25, 0.0), root)
	_box(Vector3(35.5, 0.22, 15.5), snow_mat, Vector3(0.0, 4.65, 0.0), root)
	_box(Vector3(34.2, 1.0, 0.16), _mat(Color("58666e"), 0.86), Vector3(0.0, 0.55, 7.08), root)

	for i: int in range(12):
		var x: float = -15.2 + float(i) * 2.75
		var window_mat: StandardMaterial3D = glass_mat.duplicate() as StandardMaterial3D
		var w: MeshInstance3D = _box(Vector3(1.5, 1.2, 0.12), window_mat, Vector3(x, 2.2, 7.12), root)
		warm_windows.append(w)
		_box(Vector3(0.055, 1.2, 0.13), dark_mat, Vector3(x, 2.2, 7.19), root)

	_box(Vector3(3.2, 2.45, 0.16), glass_mat, Vector3(-5.8, 1.25, 7.18), root)
	_box(Vector3(5.0, 0.28, 2.3), dark_mat, Vector3(-5.8, 2.8, 8.0), root)
	_box(Vector3(7.2, 0.08, 9.0), _mat(Color("c6cfd4"), 0.92), Vector3(-5.8, 0.05, 11.0), root)

	_add_fence(Vector3(-14.0, 0.0, 13.0), Vector3(14.0, 0.0, 13.0), root)
	_add_fence(Vector3(-14.0, 0.0, 13.0), Vector3(-14.0, 0.0, 30.0), root)
	_add_fence(Vector3(14.0, 0.0, 13.0), Vector3(14.0, 0.0, 30.0), root)
	_add_fence(Vector3(-14.0, 0.0, 30.0), Vector3(14.0, 0.0, 30.0), root)

	for i: int in range(7):
		var rack: MeshInstance3D = _box(Vector3(0.08, 0.55, 1.5), metal_mat, Vector3(-3.0 + float(i) * 0.55, 0.28, 10.4), root)
		rack.rotation.z = 0.16

	_add_bench(Vector3(-9.0, 0.0, 18.0), root)
	_add_bench(Vector3(9.0, 0.0, 18.0), root)
	_add_goal(Vector3(-7.0, 0.0, 24.0), root)
	_add_basket(Vector3(7.0, 0.0, 23.0), root)
	_add_flag(Vector3(12.0, 0.0, 10.0), root)
	_label3d("ROSVIKS SKOLA", Vector3(-5.8, 3.9, 7.3), root)

func _build_arena() -> void:
	var root: Node3D = Node3D.new()
	root.name = "NorrbottenStalArena"
	root.position = Vector3(47.0, 0.0, 28.0)
	add_child(root)

	_box(Vector3(40.0, 7.2, 22.0), arena_mat, Vector3(0.0, 3.6, 0.0), root)
	_box(Vector3(41.5, 0.22, 23.5), snow_mat, Vector3(0.0, 7.35, 0.0), root)
	_box(Vector3(40.2, 1.15, 0.18), _mat(Color("2c3a43"), 0.78), Vector3(0.0, 0.6, 11.12), root)

	for i: int in range(8):
		var x: float = -16.0 + float(i) * 4.2
		var window_mat: StandardMaterial3D = glass_mat.duplicate() as StandardMaterial3D
		var w: MeshInstance3D = _box(Vector3(2.0, 0.95, 0.12), window_mat, Vector3(x, 4.65, 11.2), root)
		warm_windows.append(w)

	_box(Vector3(3.4, 2.7, 0.16), glass_mat, Vector3(-8.0, 1.4, 11.2), root)
	_box(Vector3(5.2, 0.3, 2.4), dark_mat, Vector3(-8.0, 2.9, 12.1), root)
	_box(Vector3(5.2, 3.7, 0.18), dark_mat, Vector3(6.0, 1.9, 11.2), root)
	_box(Vector3(11.0, 1.15, 0.16), _mat(Color("1c2830"), 0.7), Vector3(0.0, 5.9, 11.22), root)
	_label3d("NORRBOTTEN STÅL ARENA", Vector3(0.0, 5.92, 11.36), root)

	_box(Vector3(50.0, 0.08, 18.0), _mat(Color("c7cfd3"), 0.94), Vector3(0.0, 0.04, 19.0), root)
	var parking_line_mat: StandardMaterial3D = _mat(Color("f0f2f2"), 0.82)
	for i: int in range(12):
		_box(Vector3(0.11, 0.02, 2.5), parking_line_mat, Vector3(-16.0 + float(i) * 3.0, 0.09, 20.0), root)

	for x: float in [-12.0, -4.0, 4.0, 12.0]:
		_box(Vector3(1.4, 0.8, 1.1), metal_mat, Vector3(x, 7.95, -1.0), root)
	_add_flag(Vector3(-14.0, 0.0, 14.5), root)

func _build_props() -> void:
	_add_car(Vector3(-5.0, 0.0, -6.0), Color("5e6871"), 0.15, true)
	_add_car(Vector3(30.0, 0.0, 44.0), Color("6c6259"), -0.35, false)
	_add_car(Vector3(61.0, 0.0, 51.0), Color("405563"), -1.45, false)
	_add_car(Vector3(-18.0, 0.0, 16.0), Color("d29843"), 0.4, false)

	var lamp_positions: Array[Vector3] = [
		Vector3(-26.0, 0.0, -18.0),
		Vector3(-8.0, 0.0, -18.0),
		Vector3(13.0, 0.0, -18.0),
		Vector3(35.0, 0.0, -18.0),
		Vector3(47.0, 0.0, 7.0),
		Vector3(47.0, 0.0, 48.0)
	]
	for p: Vector3 in lamp_positions:
		_add_lamp(p)

	var bank_positions: Array[Vector3] = [
		Vector3(-15.0, 0.0, 11.0),
		Vector3(16.0, 0.0, 13.0),
		Vector3(26.0, 0.0, 47.0),
		Vector3(66.0, 0.0, 48.0),
		Vector3(48.0, 0.0, 57.0)
	]
	var bank_mat: StandardMaterial3D = _mat(Color("e7eef2"), 0.99)
	for p: Vector3 in bank_positions:
		var bank: MeshInstance3D = _box(Vector3(randf_range(4.0, 8.0), randf_range(0.4, 0.75), randf_range(1.3, 2.2)), bank_mat, p + Vector3(0.0, 0.22, 0.0), self)
		bank.rotation.y = randf_range(-0.18, 0.18)

	for i: int in range(42):
		var angle: float = randf_range(-0.4, 2.9)
		var radius: float = randf_range(68.0, 96.0)
		_add_tree(Vector3(cos(angle) * radius, 0.0, sin(angle) * radius + 4.0))

func _build_player() -> void:
	player = CharacterBody3D.new()
	player.set_script(PLAYER_SCRIPT)
	player.position = Vector3(-8.0, 0.0, 14.0)
	add_child(player)

	camera = Camera3D.new()
	camera.fov = 42.0
	camera.current = true
	camera.near = 0.1
	camera.far = 250.0
	add_child(camera)
	_update_camera(1.0)

func _build_ui() -> void:
	var ui: CanvasLayer = CanvasLayer.new()
	add_child(ui)

	var panel: ColorRect = ColorRect.new()
	panel.position = Vector2(20.0, 20.0)
	panel.size = Vector2(370.0, 150.0)
	panel.color = Color(0.025, 0.04, 0.055, 0.9)
	ui.add_child(panel)

	var label: Label = Label.new()
	label.position = Vector2(18.0, 14.0)
	label.text = "ROSVIK: BLACKOUT\nNATIVE GODOT BUILD\n\nMÅL\nTa dig mot ishallen och utforska området."
	label.add_theme_font_size_override("font_size", 18)
	panel.add_child(label)

	var hint: Label = Label.new()
	hint.position = Vector2(20.0, 850.0)
	hint.text = "WASD / pilar: gå     Shift: spring"
	hint.add_theme_font_size_override("font_size", 16)
	ui.add_child(hint)

func _update_camera(delta: float) -> void:
	if player == null or camera == null:
		return
	var velocity_dir: Vector3 = Vector3(player.velocity.x, 0.0, player.velocity.z)
	if velocity_dir.length() > 0.1:
		velocity_dir = velocity_dir.normalized() * 2.6
	var focus: Vector3 = player.global_position + velocity_dir + Vector3(0.0, 1.05, 0.0)
	target_camera_pos = focus + Vector3(18.0, 14.0, 18.0)
	camera.global_position = camera.global_position.lerp(target_camera_pos, 1.0 - exp(-4.5 * delta))
	camera.look_at(focus, Vector3.UP)

func _update_world() -> void:
	var dusk: float = clampf((time_of_day - 15.0) / 4.0, 0.0, 1.0)
	for w: MeshInstance3D in warm_windows:
		var material: Material = w.material_override
		if material is StandardMaterial3D:
			var standard: StandardMaterial3D = material as StandardMaterial3D
			standard.emission_enabled = true
			standard.emission = Color("ff9b4b")
			standard.emission_energy_multiplier = lerpf(0.0, 1.25, dusk)
	for lamp: OmniLight3D in lamps:
		lamp.light_energy = lerpf(0.0, 2.2, dusk)

func _mat(color: Color, rough: float = 0.85, metallic: float = 0.0) -> StandardMaterial3D:
	var m: StandardMaterial3D = StandardMaterial3D.new()
	m.albedo_color = color
	m.roughness = rough
	m.metallic = metallic
	return m

func _box(size: Vector3, mat: Material, pos: Vector3, parent: Node) -> MeshInstance3D:
	var n: MeshInstance3D = MeshInstance3D.new()
	var mesh: BoxMesh = BoxMesh.new()
	mesh.size = size
	n.mesh = mesh
	n.material_override = mat
	n.position = pos
	n.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_ON
	parent.add_child(n)
	return n

func _cylinder(radius: float, height: float, mat: Material, pos: Vector3, parent: Node) -> MeshInstance3D:
	var n: MeshInstance3D = MeshInstance3D.new()
	var mesh: CylinderMesh = CylinderMesh.new()
	mesh.top_radius = radius
	mesh.bottom_radius = radius
	mesh.height = height
	n.mesh = mesh
	n.material_override = mat
	n.position = pos
	n.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_ON
	parent.add_child(n)
	return n

func _cone(radius: float, height: float, mat: Material, pos: Vector3, parent: Node) -> MeshInstance3D:
	var n: MeshInstance3D = MeshInstance3D.new()
	var mesh: CylinderMesh = CylinderMesh.new()
	mesh.top_radius = 0.0
	mesh.bottom_radius = radius
	mesh.height = height
	n.mesh = mesh
	n.material_override = mat
	n.position = pos
	n.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_ON
	parent.add_child(n)
	return n

func _add_car(pos: Vector3, color: Color, yaw: float, open_door: bool) -> void:
	var root: Node3D = Node3D.new()
	root.position = pos
	root.rotation.y = yaw
	add_child(root)
	var body_mat: StandardMaterial3D = _mat(color, 0.7, 0.16)
	_box(Vector3(4.55, 0.58, 1.82), body_mat, Vector3(0.0, 0.52, 0.0), root)
	_box(Vector3(2.35, 0.62, 1.62), body_mat, Vector3(-0.25, 1.1, 0.0), root)
	_box(Vector3(1.55, 0.42, 1.64), glass_mat, Vector3(-0.25, 1.18, 0.0), root)
	for x: float in [-1.45, 1.45]:
		for z: float in [-0.92, 0.92]:
			var wheel: MeshInstance3D = _cylinder(0.34, 0.24, _mat(Color("171a1c"), 1.0), Vector3(x, 0.37, z), root)
			wheel.rotation.x = PI / 2.0
	_box(Vector3(2.0, 0.08, 1.3), snow_mat, Vector3(-0.25, 1.48, 0.0), root)
	if open_door:
		var door: MeshInstance3D = _box(Vector3(1.15, 0.95, 0.08), body_mat, Vector3(-0.4, 1.05, 1.15), root)
		door.rotation.y = 0.65

func _add_tree(pos: Vector3) -> void:
	var root: Node3D = Node3D.new()
	root.position = pos
	add_child(root)
	_cylinder(0.14, 1.8, _mat(Color("604838"), 1.0), Vector3(0.0, 0.9, 0.0), root)
	var pine_mat: StandardMaterial3D = _mat(Color("294942"), 0.96)
	_cone(1.15, 2.0, pine_mat, Vector3(0.0, 2.15, 0.0), root)
	_cone(0.9, 1.7, pine_mat, Vector3(0.0, 3.0, 0.0), root)
	_cone(0.65, 1.35, pine_mat, Vector3(0.0, 3.85, 0.0), root)

func _add_lamp(pos: Vector3) -> void:
	var root: Node3D = Node3D.new()
	root.position = pos
	add_child(root)
	_cylinder(0.055, 4.2, dark_mat, Vector3(0.0, 2.1, 0.0), root)
	_box(Vector3(0.72, 0.08, 0.11), dark_mat, Vector3(0.3, 4.05, 0.0), root)
	var light: OmniLight3D = OmniLight3D.new()
	light.position = Vector3(0.58, 3.95, 0.0)
	light.light_color = Color("ffc071")
	light.omni_range = 8.0
	light.light_energy = 0.0
	light.shadow_enabled = false
	root.add_child(light)
	lamps.append(light)

func _add_fence(a: Vector3, b: Vector3, parent: Node) -> void:
	var d: Vector3 = b - a
	var length: float = d.length()
	var yaw: float = -atan2(d.z, d.x)
	for y: float in [0.55, 0.95]:
		var rail: MeshInstance3D = _box(Vector3(length, 0.06, 0.06), dark_mat, (a + b) * 0.5 + Vector3(0.0, y, 0.0), parent)
		rail.rotation.y = yaw
	var count: int = maxi(2, int(length / 2.0))
	for i: int in range(count + 1):
		var p: Vector3 = a.lerp(b, float(i) / float(count))
		_box(Vector3(0.07, 1.25, 0.07), dark_mat, p + Vector3(0.0, 0.625, 0.0), parent)

func _add_bench(pos: Vector3, parent: Node) -> void:
	_box(Vector3(2.0, 0.12, 0.48), wood_mat, pos + Vector3(0.0, 0.58, 0.0), parent)
	_box(Vector3(2.0, 0.12, 0.22), wood_mat, pos + Vector3(0.0, 1.0, -0.22), parent)
	for x: float in [-0.7, 0.7]:
		_box(Vector3(0.1, 0.58, 0.1), metal_mat, pos + Vector3(x, 0.3, 0.0), parent)

func _add_goal(pos: Vector3, parent: Node) -> void:
	var white_mat: StandardMaterial3D = _mat(Color("e9edef"), 0.8)
	_box(Vector3(2.4, 0.08, 0.08), white_mat, pos + Vector3(0.0, 1.25, 0.0), parent)
	for x: float in [-1.2, 1.2]:
		_box(Vector3(0.08, 1.3, 0.08), white_mat, pos + Vector3(x, 0.65, 0.0), parent)

func _add_basket(pos: Vector3, parent: Node) -> void:
	_box(Vector3(0.08, 3.1, 0.08), dark_mat, pos + Vector3(0.0, 1.55, 0.0), parent)
	_box(Vector3(1.05, 0.7, 0.07), _mat(Color("e8ecee"), 0.84), pos + Vector3(0.0, 2.7, 0.0), parent)
	var rim: MeshInstance3D = _cylinder(0.22, 0.04, _mat(Color("d5652f"), 0.75), pos + Vector3(0.0, 2.42, -0.18), parent)
	rim.rotation.x = PI / 2.0

func _add_flag(pos: Vector3, parent: Node) -> void:
	_cylinder(0.045, 5.5, metal_mat, pos + Vector3(0.0, 2.75, 0.0), parent)
	_box(Vector3(1.4, 0.65, 0.04), _mat(Color("d7a148"), 0.88), pos + Vector3(0.72, 4.65, 0.0), parent)

func _label3d(text_value: String, pos: Vector3, parent: Node) -> void:
	var label: Label3D = Label3D.new()
	label.text = text_value
	label.font_size = 64
	label.outline_size = 10
	label.modulate = Color("eef3f5")
	label.outline_modulate = Color("17232b")
	label.position = pos
	parent.add_child(label)
