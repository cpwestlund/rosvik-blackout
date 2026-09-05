extends Node3D

const Loot = preload("res://winter/scripts/loot.gd")
const InventoryUI = preload("res://winter/scripts/inventory_ui.gd")
const SaveGame = preload("res://winter/scripts/save_game.gd")
var loot = Loot.new()
var inventory = InventoryUI.new()
var save_ready = false
var autosave_clock = 0.0

const Geometry = preload("res://winter/scripts/geometry.gd")
const Player = preload("res://winter/scripts/player.gd")
const Soundscape = preload("res://winter/scripts/soundscape.gd")
var g: = Geometry.new()
var rng: = RandomNumberGenerator.new()
var player: CharacterBody3D
var camera: = Camera3D.new()
var audio: Node
var static_world: = Node3D.new()
var powered_root: = Node3D.new()
var refuge_windows: Array[MeshInstance3D] = []
var footprints: Array[Node3D] = []
var building_polygons: Array[PackedVector2Array] = []
var roads: Array[Dictionary] = []
var snow: Material
var drift_material: ShaderMaterial
var metal: Material
var glass: Material
var stage: = 0
var hold_time: = 0.0
var zoom: = 19.0
var camera_target: = Vector3.ZERO
var time: = 0.0
var battery: Node3D
var generator_lid: = Node3D.new()
var generator_lamp: OmniLight3D
var hint: = Label.new()
var objective: = Label.new()
var progress: = ProgressBar.new()
var toast: = Label.new()
var pause_panel: = PanelContainer.new()
var hud: = CanvasLayer.new()
var paused: = false
var hide_hud: = false
var toast_time: = 0.0
var completed: = false
var screenshot_frame: = 0
const BATTERY_POS: = Vector3(7.0, 0.0, 72.0)
const GENERATOR_POS: = Vector3(17.0, 0.0, 64.0)
const SWITCH_POS: = Vector3(-20.3, 0.0, 49.0)
const REFUGE_POS: = Vector3(-24.0, 0.0, -24.0)

func _ready() -> void :
	rng.seed = 650426
	snow = g.mat("a5b6c3", 0, 0.1)
	drift_material = snow.duplicate() as ShaderMaterial
	var drift_shader: = Shader.new()
	drift_shader.code = preload("res://winter/shaders/surface.gdshader").code.replace("render_mode diffuse_burley;", "render_mode diffuse_burley, cull_disabled;")
	drift_material.shader = drift_shader
	metal = g.mat("465057", 2)
	glass = g.plain("263e49")
	add_child(static_world)
	static_world.name = "AuthoredRosvik"
	add_child(powered_root)
	powered_root.name = "RefugeLighting"
	_environment()
	_terrain()
	_build_map()
	_yard()
	_refuge_details()
	_generator()
	_vegetation()
	_weather()
	player = CharacterBody3D.new()
	player.set_script(Player)
	player.position = Vector3(-23.5, 0.1, -22.0)
	add_child(player)
	player.step.connect(_footstep)
	add_child(camera)
	camera.projection = Camera3D.PROJECTION_ORTHOGONAL
	camera.size = zoom
	camera.near = 0.1
	camera.far = 500.0
	camera.current = true
	camera_target = player.position + Vector3(0, 0, -4)
	_update_camera(1.0)
	audio = Node.new()
	audio.set_script(Soundscape)
	add_child(audio)
	_ui()
	set_stage(0)
	_setup_inventory()
	_restore_save()
	save_ready = true
	_merge_boxes()
	print("WINTER_SLICE_READY buildings=", building_polygons.size(), " roads=", roads.size())
	if "--smoke-test" in OS.get_cmdline_user_args(): call_deferred("_smoke_test")
	if "--walk-test" in OS.get_cmdline_user_args(): call_deferred("_walk_test")
	if "--inventory-test" in OS.get_cmdline_user_args(): call_deferred("_inventory_test")

func _environment() -> void :
	var env: = Environment.new()
	env.background_mode = Environment.BG_COLOR
	env.background_color = Color("283e50")
	env.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	env.ambient_light_color = Color("849db9")
	env.ambient_light_energy = 0.28
	env.tonemap_mode = Environment.TONE_MAPPER_FILMIC
	env.fog_enabled = true
	env.fog_light_color = Color("3f596d")
	env.fog_density = 0.0025
	env.ssao_enabled = true
	env.ssao_radius = 1.5
	env.ssao_intensity = 2.0
	env.glow_enabled = true
	env.glow_intensity = 0.6
	var world_env: = WorldEnvironment.new()
	world_env.environment = env
	add_child(world_env)
	var sun: = DirectionalLight3D.new()
	sun.rotation_degrees = Vector3(-24, -38, 0)
	sun.light_color = Color("c6d5e5")
	sun.light_energy = 0.3
	sun.shadow_enabled = true
	sun.directional_shadow_max_distance = 160.0
	sun.shadow_blur = 2.5
	add_child(sun)

func _terrain() -> void :
	g.box(static_world, Vector3(0, -0.3, 0), Vector3(500, 0.6, 500), snow, true)

	for i: int in range(95):
		var pos: = Vector3(rng.randf_range(-180, 180), -0.35, rng.randf_range(-190, 170))
		if pos.x > -90 and pos.x < 95 and pos.z > -145 and pos.z < 100: continue
		g.ellipsoid(static_world, pos, Vector3(rng.randf_range(5, 12), rng.randf_range(0.5, 1.3), rng.randf_range(4, 9)), snow)

	for p: Vector3 in [Vector3(-180, 3, 0), Vector3(180, 3, 0)]:
		var wall: = g.box(static_world, p, Vector3(1, 6, 380), snow, true)
		wall.visible = false
	for p: Vector3 in [Vector3(0, 3, -190), Vector3(0, 3, 190)]:
		var wall: = g.box(static_world, p, Vector3(360, 6, 1), snow, true)
		wall.visible = false

func _build_map() -> void :
	var data: Dictionary = JSON.parse_string(FileAccess.get_file_as_string("res://winter/data/rosvik.json"))
	for feature: Dictionary in data.features:
		var points: = PackedVector2Array()
		for p: Array in feature.points: points.append(Vector2(p[0], p[1]))
		var tags: Dictionary = feature.tags
		if tags.has("highway"):
			var width: = 5.8
			if tags.highway in ["path", "footway", "cycleway"]: width = 2.0
			if tags.highway == "service": width = 3.5
			roads.append({"points": points, "width": width})
			_road(points, width)
		elif tags.has("building"):
			if points[0].is_equal_approx(points[-1]): points.remove_at(points.size() - 1)
			building_polygons.append(points)
			_building(feature.id, points, tags)

func _road(points: PackedVector2Array, width: float) -> void :
	var roadmat: = g.mat("7c8a90", 0, 0.45)
	var trackmat: = g.mat("4f616b", 3, 0.45)
	for i: int in range(points.size() - 1):
		var a: = Vector3(points[i].x, 0.012, points[i].y)
		var b: = Vector3(points[i + 1].x, 0.012, points[i + 1].y)
		var dir: = (b - a).normalized()
		var side: = Vector3( - dir.z, 0, dir.x)
		var road: = g.box(static_world, (a + b) * 0.5, Vector3(width, 0.025, a.distance_to(b) + 0.1), roadmat)
		road.rotation.y = atan2(dir.x, dir.z)
		for offset: float in ([-1.65, -0.65, 0.65, 1.65] if width > 3.0 else []):
			if absf(offset) > width * 0.4: continue
			var track: = g.box(static_world, (a + b) * 0.5 + side * offset + Vector3.UP * 0.022, Vector3(0.3, 0.012, a.distance_to(b)), trackmat)
			track.rotation.y = road.rotation.y
		for sign_value: float in [-1.0, 1.0]:
			_drift(a + side * (width * 0.5 + 0.35) * sign_value, b + side * (width * 0.5 + 0.35) * sign_value, 0.85, 0.2)

func _bank(pos: Vector3, size: Vector3, angle: float = 0.0) -> void :
	var bank: = g.ellipsoid(static_world, pos + Vector3(0, -0.12, 0), size, snow)
	bank.rotation.y = angle + rng.randf_range(-0.16, 0.16)
	bank.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF

func _drift(a: Vector3, b: Vector3, width: float, height: float) -> void :
	var dir: = (b - a).normalized()
	var side: = Vector3( - dir.z, 0, dir.x)
	var count: = maxi(2, int(a.distance_to(b) / 0.7))
	var surface: = SurfaceTool.new()
	surface.begin(Mesh.PRIMITIVE_TRIANGLES)
	for i: int in range(count):
		for j: int in range(6):
			var vertices: Array[Vector3] = []
			for ij: Vector2i in [Vector2i(i, j), Vector2i(i, j + 1), Vector2i(i + 1, j), Vector2i(i, j + 1), Vector2i(i + 1, j + 1), Vector2i(i + 1, j)]:
				var t: = float(ij.x) / float(count)
				var across: = float(ij.y) / 6.0
				var variation: = 0.85 + 0.12 * sin(t * 23.0) + 0.08 * sin(t * 71.0)
				var p: = a.lerp(b, t) + side * ((across - 0.5) * 2.0 * width * variation)
				p.y = 0.018 + height * sin(across * PI) * variation
				vertices.append(p)
			for v: Vector3 in vertices:
				surface.set_normal(Vector3.UP)
				surface.add_vertex(v)
	var n: = g.mesh(static_world, surface.commit(), drift_material, Vector3.ZERO)
	n.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF

func _refuge_details() -> void :

	var wood: = g.mat("77614a", 2, 0.25)
	var cloth: = g.mat("746458", 0, 0.3)
	var root: = Node3D.new()
	static_world.add_child(root)
	root.position = Vector3(-18.4, 0, -31.5)
	root.rotation.y = - PI / 2
	for i: int in range(6):
		g.box(root, Vector3(float(i) * 0.16 - 0.4, 0.14, 0), Vector3(0.12, 0.09, 0.85), wood)
	for x: float in [-0.3, 0.3]: g.box(root, Vector3(x, 0.055, 0), Vector3(0.12, 0.11, 0.75), wood)
	for p: Vector3 in [Vector3(-0.23, 0.44, 0), Vector3(0.26, 0.39, 0.03), Vector3(0.08, 0.8, 0)]:
		g.box(root, p, Vector3(0.44, 0.43, 0.56), g.mat("8d795b"))
		g.box(root, p + Vector3(0, 0.01, 0.286), Vector3(0.06, 0.4, 0.008), g.mat("b29b72"))
	g.box(root, Vector3(-0.22, 0.69, 0.01), Vector3(0.53, 0.04, 0.6), cloth)
	g.box(root, Vector3(-0.22, 0.59, 0.32), Vector3(0.53, 0.23, 0.025), cloth)

	var table: = Vector3(-20.5, 0, -19.5)
	g.box(static_world, table + Vector3.UP * 0.77, Vector3(0.7, 0.07, 1.7), wood)
	for z: float in [-0.63, 0.63]:
		g.rod(static_world, table + Vector3(-0.25, 0, z), table + Vector3(0.25, 0.75, z), 0.025, metal)
		g.rod(static_world, table + Vector3(0.25, 0, z), table + Vector3(-0.25, 0.75, z), 0.025, metal)
	g.rod(static_world, table + Vector3(0, 0.8, -0.35), table + Vector3(0, 1.14, -0.35), 0.085, g.mat("7b8989", 3))
	g.rod(static_world, table + Vector3(0.1, 0.8, 0.05), table + Vector3(0.1, 0.91, 0.05), 0.047, g.mat("ddd0af"))
	for z: float in [-0.6, 0.35]:
		g.box(static_world, table + Vector3(0.8, 0.28, z), Vector3(0.28, 0.5, 0.36), g.mat("b5b7aa"), true)
		g.rod(static_world, table + Vector3(0.8, 0.52, z), table + Vector3(0.8, 0.57, z), 0.04, g.mat("446877"))

	var board: = Node3D.new()
	static_world.add_child(board)
	board.position = Vector3(-17.65, 1.75, -28.2)
	board.rotation.y = - PI / 2
	g.box(board, Vector3.ZERO, Vector3(0.85, 0.68, 0.08), wood)
	g.box(board, Vector3(0, 0, 0.045), Vector3(0.72, 0.57, 0.014), g.mat("bcb59c"))
	g.label(board, "VÄRMEPUNKT\nVATTEN · FILTAR", Vector3(0, 0.03, 0.06), 12)
	g.rod(static_world, Vector3(-18.2, 0.05, -29.3), Vector3(-17.75, 1.45, -29.3), 0.025, wood)
	g.box(static_world, Vector3(-18.2, 0.15, -29.3), Vector3(0.09, 0.32, 0.4), g.mat("516068"))

	g.box(static_world, table + Vector3(0, 0.89, 0.6), Vector3(0.14, 0.2, 0.14), g.plain("d9a55d", 1.0))
	g.lamp(static_world, table + Vector3(0, 1.07, 0.6), 1.1, 4.0)

	for i: int in range(36):
		var t: = float(i) / 35.0
		var p: = Vector3(-29.5, 0.028, -23.6).lerp(Vector3(-19.1, 0.028, -26.0), t)
		p.z += 0.14 if i % 2 == 0 else -0.14
		var mark: = g.ellipsoid(static_world, p, Vector3(0.08, 0.007, 0.15), g.mat("7c929f"))
		mark.rotation.y = 1.4
		mark.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF

func _building(id: String, points: PackedVector2Array, _tags: Dictionary) -> void :
	var root: = Node3D.new()
	root.name = "OSM_" + id
	static_world.add_child(root)
	var school: = id == "163199458"
	var arena: = id == "163199454"
	var stone: = id == "163199461"
	var height: = 4.0 if school else 7.0 if arena else 7.4 if stone else 3.4
	var facade: = g.mat("b58b47") if school or stone else g.mat("6a756c", 2) if arena else g.mat("8c5343", 2)
	var brick: = g.mat("985c42", 1, 0.32)
	var frame: = g.mat("d0d1c6")
	for i: int in range(points.size()):
		var a: = Vector3(points[i].x, 0, points[i].y)
		var b: = Vector3(points[(i + 1) % points.size()].x, 0, points[(i + 1) % points.size()].y)
		var length: = a.distance_to(b)
		var dir: = (b - a).normalized()
		var outward: = Vector3( - dir.z, 0, dir.x)
		var mid: = (a + b) * 0.5

		if Geometry2D.is_point_in_polygon(Vector2(mid.x + outward.x, mid.z + outward.z), points): outward = - outward
		var angle: = atan2(dir.x, dir.z)
		var wallmat: Material = facade
		if school and mid.z > -5.0: wallmat = g.mat("396575", 2)
		var wall: = g.box(root, mid + Vector3.UP * height * 0.5, Vector3(0.32, height, length), wallmat, true)
		wall.rotation.y = angle
		var base: = g.box(root, mid + outward * 0.025 + Vector3.UP * 0.52, Vector3(0.36, 1.04, length), brick if school else g.mat("747977"))
		base.rotation.y = angle
		var edge: = g.box(root, mid + Vector3.UP * (height + 0.08), Vector3(0.7, 0.2, length + 0.3), metal)
		edge.rotation.y = angle
		var snow_edge: = g.box(root, mid + Vector3.UP * (height + 0.24), Vector3(0.68, 0.13, length + 0.3), snow)
		snow_edge.rotation.y = angle
		if not arena:
			for k: int in range(int(length / 3.2)):
				var p: = a + dir * (1.7 + float(k) * 3.2)
				if p.distance_to(b) < 1.2: continue
				if school and outward.x < -0.9 and absf(p.z + 26.0) < 1.7: continue
				var emergency: = school and outward.x < -0.9 and absf(p.z + 23.3) < 1.3
				var window: = _window(root, p + outward * 0.21 + Vector3.UP * 2.0, outward, frame, emergency)
				if school and outward.x < -0.9 and not emergency and (p.z < -16.0 or p.z > 36.0):
					refuge_windows.append(window)
					g.lamp(powered_root, p + outward * 1.0 + Vector3.UP * 2.0, 1.2, 5.5)
				if stone: _window(root, p + outward * 0.21 + Vector3.UP * 5.0, outward, frame, false)
		_drift(a + outward * 0.25, b + outward * 0.25, 0.55, 0.16)

		g.rod(root, a + outward * 0.3 + Vector3.UP * 0.2, a + outward * 0.3 + Vector3.UP * height, 0.055, metal)

	g.slab(root, points, height + 0.16, snow)
	if arena:
		_pitched_roof(root, Vector3(43, 7.1, 49), Vector2(37, 76), 2.6)
		_arena_front(root)
	elif stone:
		_pitched_roof(root, Vector3(-9, 7.45, -113), Vector2(41, 18), 4.2, g.mat("934c3d", 2))
	elif school:
		_school_entrance(root)

		for p: Vector3 in [Vector3(0, 4.4, -29), Vector3(20, 4.4, -33), Vector3(-3, 4.4, 18)]:
			g.box(root, p, Vector3(1.1, 0.7, 0.8), metal)
			g.box(root, p + Vector3.UP * 0.4, Vector3(1.3, 0.1, 1.0), snow)

func _window(parent: Node3D, pos: Vector3, normal: Vector3, frame: Material, warm: bool) -> MeshInstance3D:
	var root: = Node3D.new()
	parent.add_child(root)
	root.position = pos
	root.rotation.y = atan2(normal.x, normal.z)
	g.box(root, Vector3.ZERO, Vector3(2.0, 1.3, 0.08), frame)
	var pane: = g.box(root, Vector3(0, 0, 0.047), Vector3(1.82, 1.1, 0.035), g.plain("c7955e", 0.6) if warm else glass)
	g.box(root, Vector3(0, 0, 0.076), Vector3(0.055, 1.13, 0.035), frame)
	g.box(root, Vector3(0, 0.14, 0.077), Vector3(1.82, 0.035, 0.035), frame)
	g.box(root, Vector3(0, -0.69, 0.07), Vector3(2.16, 0.07, 0.26), metal)
	g.box(root, Vector3(0, -0.63, 0.08), Vector3(2.1, 0.06, 0.24), snow)
	if warm:
		g.lamp(root, Vector3(0, 0, 0.8), 0.85, 5.5)

		for x: float in [-0.72, 0.72]:
			g.box(root, Vector3(x, 0, 0.083), Vector3(0.21, 1.03, 0.014), g.plain("9a815f", 0.2))

	return pane

func _pitched_roof(parent: Node3D, pos: Vector3, size: Vector2, rise: float, roofmat: Material = null) -> void :
	var mat: Material = snow if roofmat == null else roofmat
	for side: float in [-1.0, 1.0]:
		var roof: = g.box(parent, pos + Vector3(side * size.x * 0.25, rise * 0.5, 0), Vector3(sqrt(pow(size.x * 0.5, 2) + rise * rise), 0.22, size.y), mat)
		roof.rotation.z = - side * atan2(rise, size.x * 0.5)
		if roofmat != null:
			var snowcap: = g.box(parent, roof.position + Vector3.UP * 0.15, Vector3(roof.mesh.size.x * 0.88, 0.07, size.y * 0.94), snow)
			snowcap.rotation.z = roof.rotation.z

	g.box(parent, pos + Vector3.UP * (rise + 0.1), Vector3(0.32, 0.12, size.y), snow)

func _school_entrance(parent: Node3D) -> void :
	var entry: = Node3D.new()
	parent.add_child(entry)
	entry.position = Vector3(-17.55, 0, -26)
	entry.rotation.y = - PI / 2
	g.box(entry, Vector3(0, 1.2, 0.1), Vector3(1.4, 2.4, 0.15), g.mat("dbd7c8"))
	g.box(entry, Vector3(0, 1.52, 0.2), Vector3(1.15, 1.3, 0.04), glass)
	g.box(entry, Vector3(0.43, 1.0, 0.25), Vector3(0.04, 0.3, 0.08), metal)
	g.box(entry, Vector3(0, 2.83, 0.8), Vector3(3.8, 0.17, 2.0), metal)
	g.box(entry, Vector3(0, 2.96, 0.8), Vector3(3.8, 0.13, 2.0), snow)
	for x: float in [-1.65, 1.65]: g.rod(entry, Vector3(x, 0, 1.6), Vector3(x, 2.8, 1.6), 0.055, metal)
	g.box(entry, Vector3(0, 3.38, 0.18), Vector3(4.3, 0.43, 0.08), g.mat("324c53"))
	g.label(entry, "ROSVIKS SKOLA", Vector3(0, 3.38, 0.24), 30)
	g.box(entry, Vector3(0.8, 1.9, 0.22), Vector3(0.22, 0.32, 0.05), g.plain("d5ccaa"))

	g.box(entry, Vector3(-1.0, 0.36, 0.8), Vector3(0.18, 0.3, 0.18), g.plain("ffc17a", 2.0))
	g.lamp(entry, Vector3(-1.0, 0.65, 0.8), 1.8, 5.0)

	powered_root.visible = false

func _arena_front(parent: Node3D) -> void :
	var root: = Node3D.new()
	parent.add_child(root)
	root.position = Vector3(24.65, 0, 65)
	root.rotation.y = - PI / 2
	g.box(root, Vector3(0, 1.8, 0.2), Vector3(3.2, 3.6, 0.15), g.mat("394842", 2))
	for x: float in [-0.78, 0.78]:
		g.box(root, Vector3(x, 2.15, 0.3), Vector3(1.4, 1.55, 0.04), glass)
	g.box(root, Vector3(0, 4.5, 0.2), Vector3(7.6, 1.1, 0.12), g.mat("dee0d7"))
	var title: = g.label(root, "HALA HALLEN", Vector3(0, 4.5, 0.29), 64)
	title.modulate = Color("345040")
	g.box(root, Vector3(0, 3.95, 1.1), Vector3(5.5, 0.18, 2.4), metal)
	g.box(root, Vector3(0, 4.08, 1.1), Vector3(5.5, 0.12, 2.4), snow)

func _yard() -> void :

	_road(PackedVector2Array([Vector2(-30, -34), Vector2(-30, 63), Vector2(17, 70)]), 3.0)
	_road(PackedVector2Array([Vector2(-66, 4), Vector2(-30, 4)]), 3.0)
	for p: Vector3 in [Vector3(-28, 0, -33), Vector3(-28, 0, 19), Vector3(-26, 0, 49)]: _bench(p)
	for z: float in [-11, 13, 36, 62]:
		g.rod(static_world, Vector3(-39, 0, z), Vector3(-39, 5.8, z), 0.065, metal)
		g.box(static_world, Vector3(-38.8, 5.85, z), Vector3(0.55, 0.12, 0.32), metal)
		g.box(static_world, Vector3(-38.8, 5.94, z), Vector3(0.55, 0.05, 0.32), snow)

	for i: int in range(7):
		var x: = -43.0 - float(i) * 0.6
		g.rod(static_world, Vector3(x, 0, -36), Vector3(x, 0.75, -36), 0.025, metal)
		g.rod(static_world, Vector3(x, 0.75, -36), Vector3(x, 0.75, -35.2), 0.025, metal)
		g.rod(static_world, Vector3(x, 0.75, -35.2), Vector3(x, 0, -35.2), 0.025, metal)
	_car(Vector3(7, 0, 76), 0.15, true)
	_car(Vector3(-49, 0, 48), 0.22, false)
	_car(Vector3(-52, 0, -49), -0.1, false)
	for x: float in [-49, -46]:
		g.box(static_world, Vector3(x, 0.65, -19), Vector3(0.65, 1.3, 0.72), g.mat("344f48"), true)
		g.box(static_world, Vector3(x, 1.34, -19), Vector3(0.73, 0.09, 0.8), snow)

	g.rod(static_world, Vector3(-46, 0, -4), Vector3(-46, 0.7, -4), 0.09, metal)
	var beam: = g.box(static_world, Vector3(-46, 0.7, -4), Vector3(4.0, 0.12, 0.2), g.mat("9a5040"))
	beam.rotation.z = 0.12
	for x: float in [-47.7, -44.3]: g.box(static_world, Vector3(x, 0.78 + (x + 46) * 0.12, -4), Vector3(0.45, 0.08, 0.38), g.mat("32464b"))

	for z: int in range(68, 98, 3):
		g.rod(static_world, Vector3(-84, 0, z), Vector3(-84, 1.2, z), 0.04, metal)
	for y: float in [0.45, 0.95]: g.rod(static_world, Vector3(-84, y, 68), Vector3(-84, y, 95), 0.018, metal)

func _bench(pos: Vector3) -> void :
	var wood: = g.mat("76614c", 2, 0.3)
	for i: int in range(3): g.box(static_world, pos + Vector3(0, 0.48, float(i) * 0.16), Vector3(1.8, 0.07, 0.13), wood)
	for x: float in [-0.65, 0.65]:
		g.rod(static_world, pos + Vector3(x, 0, 0.05), pos + Vector3(x, 0.49, 0.05), 0.035, metal)
		g.rod(static_world, pos + Vector3(x, 0, 0.3), pos + Vector3(x, 0.9, 0.3), 0.035, metal)
	g.box(static_world, pos + Vector3(0, 0.82, 0.31), Vector3(1.8, 0.2, 0.06), wood)
	g.box(static_world, pos + Vector3(0, 0.54, 0.17), Vector3(1.74, 0.04, 0.39), snow)

func _car(pos: Vector3, angle: float, van: bool) -> void :
	var root: = Node3D.new()
	static_world.add_child(root)
	root.position = pos
	root.rotation.y = angle
	var paint: = g.mat("b8b9b0", 3) if van else g.mat("4b606a", 3)
	g.box(root, Vector3(0, 0.66, 0), Vector3(1.83, 0.58, 4.4), paint, true)
	g.box(root, Vector3(0, 1.24, -0.35), Vector3(1.73, 0.85, 2.55), paint)
	g.box(root, Vector3(0, 1.29, 0.96), Vector3(1.55, 0.62, 0.04), glass)
	for x: float in [-0.88, 0.88]:
		g.box(root, Vector3(x, 1.3, 0.25), Vector3(0.025, 0.55, 0.92), glass)
		g.box(root, Vector3(x, 1.29, -0.86), Vector3(0.025, 0.55, 0.97), glass if not van else paint)
		for z: float in [-1.42, 1.42]:
			g.rod(root, Vector3(x - 0.13, 0.36, z), Vector3(x + 0.13, 0.36, z), 0.34, g.mat("25292b"))
	g.ellipsoid(root, Vector3(0, 1.72, -0.34), Vector3(0.86, 0.16, 1.28), snow)
	g.ellipsoid(root, Vector3(0, 0.99, 1.66), Vector3(0.84, 0.1, 0.46), snow)
	for x: float in [-0.63, 0.63]:
		g.box(root, Vector3(x, 0.73, 2.22), Vector3(0.4, 0.2, 0.025), g.plain("bec5bc"))
		g.box(root, Vector3(x, 0.7, -2.22), Vector3(0.2, 0.27, 0.025), g.plain("6b3330"))
	g.box(root, Vector3(0, 0.48, 2.24), Vector3(0.46, 0.1, 0.02), g.plain("d6d7cd"))
	if van: g.label(root, "SERVICE", Vector3(0, 1.25, -1.66), 30).rotation.y = PI

func _generator() -> void :
	var root: = Node3D.new()
	add_child(root)
	root.position = GENERATOR_POS
	var ochre: = g.mat("a88943", 2)
	g.box(root, Vector3(0, 0.46, 0), Vector3(1.1, 0.68, 0.76), ochre, true)
	g.box(root, Vector3(0, 0.17, 0), Vector3(1.35, 0.16, 0.95), metal)
	for x: float in [-0.48, 0.48]:
		for z: float in [-0.3, 0.3]: g.rod(root, Vector3(x, 0, z), Vector3(x, 0.24, z), 0.075, metal)
	for i: int in range(8): g.box(root, Vector3(-0.557, 0.36 + float(i) * 0.045, 0), Vector3(0.014, 0.02, 0.42), g.mat("252d30"))
	root.add_child(generator_lid)
	generator_lid.position = Vector3(0, 0.84, -0.38)
	g.box(generator_lid, Vector3(0, 0, 0.38), Vector3(1.12, 0.06, 0.78), ochre)
	g.rod(root, Vector3(0.4, 0.6, -0.24), Vector3(0.4, 1.16, -0.24), 0.045, metal)
	g.box(root, Vector3(0, 0.5, 0.395), Vector3(0.36, 0.25, 0.025), g.mat("283c3e"))
	g.label(root, "RESERVKRAFT", Vector3(0, 0.69, 0.4), 12)
	generator_lamp = g.lamp(root, Vector3(0, 1.3, 0), 0.0, 4.0)
	battery = Node3D.new()
	add_child(battery)
	battery.position = BATTERY_POS + Vector3.UP * 0.4
	g.box(battery, Vector3.ZERO, Vector3(0.4, 0.28, 0.26), g.mat("272e30"))
	g.box(battery, Vector3(0, 0.15, 0), Vector3(0.43, 0.03, 0.29), g.mat("202428"))
	for x: float in [-0.13, 0.13]: g.box(battery, Vector3(x, 0.18, 0), Vector3(0.04, 0.04, 0.04), metal)

	g.box(static_world, BATTERY_POS + Vector3.UP * 0.14, Vector3(0.65, 0.28, 0.55), g.mat("776348", 2), true)
	var cable_points: Array[Vector3] = [GENERATOR_POS + Vector3(0, 0.04, 0.4), Vector3(17, 0.05, 67), Vector3(10, 0.05, 67), Vector3(-21, 0.05, 64), Vector3(-22, 0.05, 49), SWITCH_POS + Vector3(0, 0.9, 0)]
	for i: int in range(cable_points.size() - 1): g.rod(static_world, cable_points[i], cable_points[i + 1], 0.025, g.mat("272a2b"))
	g.box(static_world, SWITCH_POS + Vector3.UP * 1.25, Vector3(0.25, 0.9, 0.58), metal)
	var s: = g.label(static_world, "RESERV\nMATNING", SWITCH_POS + Vector3(-0.14, 1.33, 0), 14)
	s.rotation.y = - PI / 2
	g.box(static_world, SWITCH_POS + Vector3(-0.15, 1.12, 0), Vector3(0.05, 0.16, 0.06), g.mat("9c4a38"))

func _vegetation() -> void :
	for i: int in range(155):
		var p: = Vector2(rng.randf_range(-160, 135), rng.randf_range(-165, 155))
		if p.x > -85 and p.x < 75 and p.y > -140 and p.y < 105: continue
		if _occupied(p, 4.0): continue
		_tree(Vector3(p.x, 0, p.y), rng.randf_range(5.5, 10.0))
	for p: Vector3 in [Vector3(-45, 0, -28), Vector3(-49, 0, 8), Vector3(-44, 0, 42), Vector3(-35, 0, -55), Vector3(-34, 0, -91)]: _birch(p)
	for i: int in range(130):
		var p: = Vector2(rng.randf_range(-105, 100), rng.randf_range(-145, 130))
		if _occupied(p, 2.0) or (p.x > -57 and p.x < 20 and p.y > -50 and p.y < 80): continue
		for j: int in range(3):
			g.rod(static_world, Vector3(p.x, 0, p.y), Vector3(p.x + rng.randf_range(-0.35, 0.35), rng.randf_range(0.25, 0.65), p.y + rng.randf_range(-0.3, 0.3)), 0.014, g.mat("706b58"), 0.003)

func _occupied(p: Vector2, margin: float) -> bool:
	for poly: PackedVector2Array in building_polygons:
		if Geometry2D.is_point_in_polygon(p, poly): return true
		for i: int in range(poly.size()):
			if Geometry2D.get_closest_point_to_segment(p, poly[i], poly[(i + 1) % poly.size()]).distance_to(p) < margin: return true
	for road: Dictionary in roads:
		var pts: PackedVector2Array = road.points
		for i: int in range(pts.size() - 1):
			if Geometry2D.get_closest_point_to_segment(p, pts[i], pts[i + 1]).distance_to(p) < road.width * 0.5 + margin: return true
	return false

func _tree(pos: Vector3, height: float) -> void :
	var bark: = g.mat("655b4c", 2)
	var needles: = g.mat("354b48", 0, 0.4)
	g.rod(static_world, pos, pos + Vector3.UP * height, 0.15, bark, 0.025)
	for tier: int in range(7):
		var y: = height * (0.22 + float(tier) * 0.105)
		var radius: = height * 0.28 * (1.0 - float(tier) / 8.0)
		for arm: int in range(5):
			var angle: = float(arm) * TAU / 5.0 + float(tier) * 0.77
			var dir: = Vector3(cos(angle), 0, sin(angle))
			var start: = pos + Vector3.UP * y
			var end: = start + dir * radius - Vector3.UP * 0.35
			g.rod(static_world, start, end, 0.032, bark, 0.009)
			var cluster: = g.ellipsoid(static_world, start + dir * radius * 0.58 - Vector3.UP * 0.12, Vector3(radius * 0.65, 0.18, radius * 0.3), needles)
			cluster.rotation.y = - angle
			var cap: = g.ellipsoid(static_world, cluster.position + Vector3.UP * 0.14, Vector3(radius * 0.59, 0.12, radius * 0.27), snow)
			cap.rotation.y = - angle

func _birch(pos: Vector3) -> void :
	var bark: = g.mat("a5aaa5", 2, 0.5)
	g.rod(static_world, pos, pos + Vector3(0.14, 7.0, 0.04), 0.14, bark, 0.045)
	for i: int in range(12):
		var y: = 2.7 + float(i) * 0.32
		var a: = float(i) * 2.4
		var start: = pos + Vector3(0, y, 0)
		var end: = start + Vector3(cos(a) * 1.6, 1.0, sin(a) * 1.6) * (1.0 - float(i) * 0.04)
		g.rod(static_world, start, end, 0.035, bark, 0.006)
		for j: int in range(3):
			var t: = float(j + 1) / 4.0
			var twig: = start.lerp(end, t)
			g.rod(static_world, twig, twig + Vector3(cos(a + 1.2) * 0.6, 0.7, sin(a + 1.2) * 0.6), 0.009, bark, 0.002)

func _weather() -> void :
	var p: = CPUParticles3D.new()
	add_child(p)
	p.position = Vector3(-20, 12, 20)
	p.amount = 800
	p.lifetime = 14.0
	p.preprocess = 14.0
	p.emission_shape = CPUParticles3D.EMISSION_SHAPE_BOX
	p.emission_box_extents = Vector3(85, 10, 110)
	p.direction = Vector3(0.4, -1, 0.18)
	p.spread = 15
	p.gravity = Vector3(0.1, -0.12, 0)
	p.initial_velocity_min = 0.65
	p.initial_velocity_max = 1.1
	p.scale_amount_min = 0.025
	p.scale_amount_max = 0.055
	var flake: = SphereMesh.new()
	flake.radius = 1.0
	flake.height = 2.0
	flake.radial_segments = 4
	flake.rings = 2
	flake.material = g.plain("d7e1e5")
	p.mesh = flake

func _ui() -> void :
	add_child(hud)
	var vignette: = ColorRect.new()
	vignette.size = Vector2(1600, 900)
	vignette.mouse_filter = Control.MOUSE_FILTER_IGNORE
	var vignette_mat: = ShaderMaterial.new()
	vignette_mat.shader = preload("res://winter/shaders/vignette.gdshader")
	vignette.material = vignette_mat
	hud.add_child(vignette)
	var top: = MarginContainer.new()
	top.position = Vector2(38, 28)
	hud.add_child(top)
	var stack: = VBoxContainer.new()
	stack.add_theme_constant_override("separation", 7)
	top.add_child(stack)
	var title: = Label.new()
	title.text = "ROSVIK / BLACKOUT"
	title.add_theme_font_size_override("font_size", 20)
	title.modulate = Color("e8e7df")
	stack.add_child(title)
	var subtitle: = Label.new()
	subtitle.text = "DAG 03   ·   SKOLGRÄND   ·   −18 °C"
	subtitle.add_theme_font_size_override("font_size", 12)
	subtitle.modulate = Color("b6c7cd")
	stack.add_child(subtitle)
	stack.add_child(objective)
	objective.add_theme_font_size_override("font_size", 17)
	objective.add_theme_color_override("font_shadow_color", Color("18242c"))
	objective.add_theme_constant_override("shadow_offset_x", 1)
	objective.add_theme_constant_override("shadow_offset_y", 2)
	hint.mouse_filter = Control.MOUSE_FILTER_IGNORE
	hint.position = Vector2(400, 806)
	hint.size = Vector2(800, 35)
	hint.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	hint.add_theme_font_size_override("font_size", 19)
	hud.add_child(hint)
	progress.position = Vector2(660, 850)
	progress.size = Vector2(280, 5)
	progress.show_percentage = false
	progress.max_value = 1.0
	hud.add_child(progress)
	toast.position = Vector2(350, 740)
	toast.size = Vector2(900, 40)
	toast.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	toast.add_theme_font_size_override("font_size", 17)
	hud.add_child(toast)
	var controls: = Label.new()
	controls.text = "WASD  Gå     SHIFT  Spring     E  Arbeta     I  Ryggsäck/sök     F  Lampa     HJUL  Zoom     ESC  Paus"
	controls.position = Vector2(38, 865)
	controls.add_theme_font_size_override("font_size", 12)
	controls.modulate = Color("c3ced0")
	hud.add_child(controls)
	var credit: = Label.new()
	credit.text = "Kartunderlag © OpenStreetMap contributors · ODbL"
	credit.position = Vector2(1200, 865)
	credit.add_theme_font_size_override("font_size", 10)
	credit.modulate = Color("b5bfc1")
	hud.add_child(credit)
	hud.add_child(pause_panel)
	pause_panel.position = Vector2(560, 300)
	pause_panel.size = Vector2(480, 250)
	var box: = VBoxContainer.new()
	box.add_theme_constant_override("separation", 18)
	pause_panel.add_child(box)
	var label: = Label.new()
	label.text = "EN STUND I ROSVIK"
	label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	box.add_child(label)
	for entry: Array in [["Fortsätt", _toggle_pause], ["Börja om", _confirm_restart], ["Avsluta", _quit]]:
		var button: = Button.new()
		button.text = entry[0]
		button.pressed.connect(entry[1])
		box.add_child(button)
	pause_panel.visible = false
	_message("Skolan är kall. Servicebilen vid ishallen har ett reservbatteri.", 8.0)

func _input(event: InputEvent) -> void :
	if event is InputEventKey and event.pressed and not event.echo:
		if event.keycode == KEY_I:
			_toggle_inventory()
			return
		if event.keycode == KEY_ESCAPE:
			if inventory.visible: _toggle_inventory()
			else: _toggle_pause()
		if event.keycode == KEY_F2 and not paused:
			hide_hud = not hide_hud
			hud.visible = not hide_hud
	if event is InputEventMouseButton and event.pressed and not paused:
		if event.button_index == MOUSE_BUTTON_WHEEL_UP: zoom = maxf(18.0, zoom - 2.0)
		if event.button_index == MOUSE_BUTTON_WHEEL_DOWN: zoom = minf(55.0, zoom + 2.0)

func _process(delta: float) -> void :
	if player == null: return
	if not paused:
		time += delta
		_update_camera(delta)
		_interaction(delta)
		autosave_clock += delta
		if autosave_clock >= 30.0:
			autosave_clock = 0.0
			_save_progress()
		toast_time -= delta
		toast.visible = toast_time > 0.0
	if "--capture" in OS.get_cmdline_user_args():
		screenshot_frame += 1
		if screenshot_frame == 30:
			get_viewport().get_texture().get_image().save_png("/tmp/rosvik-winter.png")
			get_tree().quit()

func _update_camera(delta: float) -> void :
	var desired: = player.position + Vector3(0, 0.8, -3.0)
	camera_target = camera_target.lerp(desired, 1.0 - exp( - delta * 4.5))
	camera.position = camera_target + Vector3(-38, 36, 38)
	camera.look_at(camera_target)
	camera.size = lerpf(camera.size, zoom, 1.0 - exp( - delta * 9.0))

func _interaction(delta: float) -> void :
	var target: = BATTERY_POS if stage == 0 else GENERATOR_POS if stage < 3 else SWITCH_POS if stage == 3 else REFUGE_POS
	var distance: = Vector2(player.position.x - target.x, player.position.z - target.z).length()
	var verb: = "Ta upp batteriet" if stage == 0 else "Montera batteriet" if stage == 1 else "Starta reservkraften" if stage == 2 else "Slå på reservmatningen" if stage == 3 else "Stanna vid värmepunkten"
	if stage == 5:
		hint.text = "Värmen är tillbaka. Ta en stund och utforska Rosvik."
		if _nearest_container() != "": hint.text += "   ·   I  Sök förrådet"
		progress.visible = false
		return
	var near: = distance < 2.5
	hint.text = "Håll E  ·  " + verb if near else "%s  ·  %d m" % [verb, int(distance)]
	if _nearest_container() != "": hint.text += "   ·   I  Sök förrådet"
	player.working = near and Input.is_action_pressed("interact")
	if player.working:
		hold_time += delta
		player.model.rotation.y = lerp_angle(player.model.rotation.y, atan2(target.x - player.position.x, target.z - player.position.z), delta * 8.0)
	else: hold_time = 0.0
	var duration: = 0.55 if stage == 0 else 2.4 if stage == 1 else 2.8 if stage == 2 else 1.3
	progress.visible = hold_time > 0.0
	progress.value = hold_time / duration
	if hold_time >= duration: set_stage(stage + 1)

func set_stage(value: int) -> void :
	stage = value
	hold_time = 0.0
	player.working = false
	player.carrying = stage == 1
	battery.visible = stage == 0
	var messages: = ["Hämta reservbatteriet vid servicebilen.", "Bär batteriet till generatorn vid ishallen.", "Batteriet är monterat. Starta generatorn.", "Reservkraften går. Slå på matningen på skolans vägg.", "Fönstren lyser. Gå till skolans entré.", "Värmepunkten är igång."]
	objective.text = messages[stage]
	if stage == 1:
		create_tween().tween_property(generator_lid, "rotation:x", -1.1, 0.7)
		_message("Batteriet väger. Du rör dig långsammare medan du bär det.")
	if stage == 2: create_tween().tween_property(generator_lid, "rotation:x", 0.0, 0.6)
	if stage >= 3:
		audio.start_engine()
		generator_lamp.light_energy = 1.5
		_message("Motorn tar. Kabeln ligger redan framdragen längs skolans gavel.")
	if stage >= 4:
		powered_root.visible = true
		for pane: MeshInstance3D in refuge_windows: pane.material_override = g.plain("c7955e", 0.6)
	if stage == 4: _message("Ett fönster i taget. Det finns värme att komma tillbaka till.", 7.0)
	if stage == 5:
		completed = true
		_message("Första natten med reservkraft. Rosvik håller ihop.", 10.0)

	if save_ready: _save_progress()

func _message(text: String, duration: float = 5.0) -> void :
	toast.text = text
	toast_time = duration

func _footstep(pos: Vector3, side: float) -> void :
	if audio != null: audio.footstep()
	var offset: Vector3 = player.model.basis.x * side * 0.12
	var mark: = g.ellipsoid(self, pos + offset + Vector3.UP * 0.025, Vector3(0.085, 0.008, 0.16), g.mat("8eabb9"))
	mark.rotation.y = player.model.rotation.y
	mark.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	footprints.append(mark)
	if footprints.size() > 160: footprints.pop_front().queue_free()

func _toggle_pause() -> void :
	paused = not paused
	player.paused = paused
	pause_panel.visible = paused
	audio.wind.stream_paused = paused
	audio.machine.stream_paused = paused

func _confirm_restart() -> void:
	var dialog = ConfirmationDialog.new()
	dialog.dialog_text = "Börja om från första uppdraget? Din sparade omgång ersätts."
	dialog.confirmed.connect(_restart)
	dialog.canceled.connect(dialog.queue_free)
	hud.add_child(dialog)
	dialog.popup_centered()

func _restart() -> void :
	for suffix: String in ["", ".bak", ".tmp"]:
		DirAccess.remove_absolute(ProjectSettings.globalize_path(SaveGame.SAVE_PATH + suffix))
	save_ready = false
	get_tree().reload_current_scene()

func _quit() -> void :
	_save_progress()
	get_tree().quit()

func _merge_boxes() -> void :


	var groups: Dictionary = {}
	for node: Node in static_world.find_children("*", "MeshInstance3D", true, false):
		var n: = node as MeshInstance3D
		if not n.visible or refuge_windows.has(n): continue
		var key: = str(n.material_override.get_instance_id())
		var primitive: Mesh
		var local_scale: = Vector3.ONE
		if n.mesh is BoxMesh:
			key += "box"
			primitive = BoxMesh.new()
			primitive.size = Vector3.ONE
			local_scale = n.mesh.size
		elif n.mesh is SphereMesh:
			key += "sphere" + str(n.cast_shadow)
			primitive = n.mesh
		elif n.mesh is CylinderMesh:
			var ratio: float = n.mesh.top_radius / n.mesh.bottom_radius
			key += "rod" + str(snappedf(ratio, 0.05))
			primitive = CylinderMesh.new()
			primitive.height = 1.0
			primitive.bottom_radius = 1.0
			primitive.top_radius = snappedf(ratio, 0.05)
			primitive.radial_segments = 8
			local_scale = Vector3(n.mesh.bottom_radius, n.mesh.height, n.mesh.bottom_radius)
		else: continue
		if not groups.has(key): groups[key] = {"mat": n.material_override, "mesh": primitive, "transforms": [], "shadow": n.cast_shadow}
		var transform: = n.global_transform
		transform.basis = transform.basis * Basis.from_scale(local_scale)
		groups[key].transforms.append(transform)
		for child: Node in n.get_children(): child.reparent(static_world, true)
		n.queue_free()
	for group: Dictionary in groups.values():
		var multimesh: = MultiMesh.new()
		multimesh.transform_format = MultiMesh.TRANSFORM_3D
		multimesh.mesh = group.mesh
		multimesh.instance_count = group.transforms.size()
		for i: int in range(group.transforms.size()): multimesh.set_instance_transform(i, group.transforms[i])
		var instance: = MultiMeshInstance3D.new()
		instance.multimesh = multimesh
		instance.material_override = group.mat
		instance.cast_shadow = group.shadow
		add_child(instance)
	print("WINTER_STATIC_BATCHES ", groups.size())

func _smoke_test() -> void :

	assert (building_polygons.size() >= 5)
	assert (roads.size() >= 5)
	for p: Vector3 in [BATTERY_POS, GENERATOR_POS, SWITCH_POS, REFUGE_POS, player.position]:
		for polygon: PackedVector2Array in building_polygons:
			assert ( not Geometry2D.is_point_in_polygon(Vector2(p.x, p.z), polygon), "Interaction placed inside a building")
	for s: int in range(1, 6):
		set_stage(s)
		assert (player.carrying == (s == 1))
	assert (completed and powered_root.visible and not battery.visible)
	print("WINTER_SMOKE_OK progression=5 mapped_landmarks=", building_polygons.size())
	get_tree().quit()

func _walk_to(point: Vector3) -> bool:
	var ticks: = 0
	while Vector2(player.position.x - point.x, player.position.z - point.z).length() > 1.65:
		var d: = (point - player.position)
		d.y = 0.0
		d = d.normalized()
		var horizontal: = (d.x + d.z) * 0.7071
		var vertical: = ( - d.x + d.z) * 0.7071
		Input.action_press("move_right", maxf(horizontal, 0.0))
		Input.action_press("move_left", maxf( - horizontal, 0.0))
		Input.action_press("move_back", maxf(vertical, 0.0))
		Input.action_press("move_forward", maxf( - vertical, 0.0))
		await get_tree().physics_frame
		ticks += 1
		if ticks > 4800:
			push_error("WALK_BLOCKED at " + str(player.position) + " target " + str(point))
			get_tree().quit(1)
			return false
	for action: String in ["move_left", "move_right", "move_forward", "move_back"]: Input.action_release(action)
	for i: int in range(12): await get_tree().physics_frame
	return true

func _hold_interact(expected_stage: int) -> bool:
	Input.action_press("interact")
	for i: int in range(240):
		await get_tree().physics_frame
		if stage == expected_stage: break
	Input.action_release("interact")
	if stage != expected_stage:
		push_error("INTERACTION_FAILED expected " + str(expected_stage) + " got " + str(stage))
		get_tree().quit(1)
		return false
	return true

func _walk_test() -> void :
	for point: Vector3 in [Vector3(-30, 0, 63), BATTERY_POS]:
		if not await _walk_to(point): return
	if not await _hold_interact(1): return
	if not await _walk_to(GENERATOR_POS): return
	if not await _hold_interact(2): return
	if not await _hold_interact(3): return
	for point: Vector3 in [Vector3(17, 0, 68), Vector3(-24, 0, 65), Vector3(-23, 0, 49), SWITCH_POS]:
		if not await _walk_to(point): return
	if not await _hold_interact(4): return
	if not await _walk_to(REFUGE_POS): return
	if not await _hold_interact(5): return
	print("WINTER_WALK_OK actual_movement=true all_interactions=true")
	get_tree().quit()

func _setup_inventory() -> void:
	hud.add_child(inventory)
	inventory.closed.connect(_toggle_inventory)
	inventory.transferred.connect(_save_progress)
	get_tree().auto_accept_quit = false

func _nearest_container() -> String:
	if player.position.distance_to(BATTERY_POS) < 3.0: return "van"
	if player.position.distance_to(REFUGE_POS) < 3.0: return "refuge"
	return ""

func _toggle_inventory() -> void:
	if paused and not inventory.visible: return
	if inventory.visible:
		inventory.hide()
		paused = false
		player.paused = false
	else:
		var id = _nearest_container()
		paused = true
		player.paused = true
		player.working = false
		hold_time = 0.0
		inventory.open_box(loot, id, "SERVICEBILENS FÖRVARING" if id == "van" else "FÖRRÅD VID VÄRMEPUNKTEN")

func _is_test() -> bool:
	for arg: String in OS.get_cmdline_user_args():
		if arg.ends_with("-test") or arg == "--capture": return true
	return false

func _save_progress(path: String = SaveGame.SAVE_PATH) -> void:
	if not save_ready or (_is_test() and path == SaveGame.SAVE_PATH): return
	var p = player.position
	var data = {"version": 1, "stage": stage, "position": [p.x, p.y, p.z], "loot": loot.snapshot()}
	if not SaveGame.write(data, path): _message("Kunde inte spara. Kontrollera ledigt utrymme.", 8.0)

func _restore_save(source: String = SaveGame.SAVE_PATH) -> void:
	if _is_test() and source == SaveGame.SAVE_PATH: return
	for path: String in [source, source + ".bak"]:
		var data: Dictionary = SaveGame.read_file(path)
		if data.is_empty(): continue
		var point = Vector2(data.position[0], data.position[2])
		var blocked = false
		for polygon: PackedVector2Array in building_polygons:
			if Geometry2D.is_point_in_polygon(point, polygon): blocked = true
		if blocked or not loot.restore(data.loot): continue
		set_stage(int(data.stage))
		player.position = Vector3(data.position[0], data.position[1], data.position[2])
		camera_target = player.position + Vector3(0, 0.8, -3)
		_update_camera(1.0)
		_message("Din sparade omgång har laddats.")
		return
	if FileAccess.file_exists(source):
		_message("Sparningen kunde inte läsas. En ny omgång har startats.", 10.0)

func _notification(what: int) -> void:
	if what == NOTIFICATION_WM_CLOSE_REQUEST: _quit()

func _inventory_test() -> void:
	player.position = BATTERY_POS
	_toggle_inventory()
	assert(paused and player.paused and inventory.visible)
	assert(inventory.container_id == "van")
	inventory.box_list.select(0)
	inventory._transfer(true)
	assert(loot.pack.size() == 1)
	assert(inventory.pack_list.item_count == 1)
	_toggle_inventory()
	assert(not paused and not player.paused and not inventory.visible)
	player.position = Vector3(-30, 0, 63)
	_toggle_inventory()
	assert(inventory.container_id == "" and inventory.take.disabled and inventory.put.disabled)
	_toggle_inventory()
	set_stage(4)
	assert(audio.machine.playing and powered_root.visible and not player.carrying)
	var path = "user://winter_world_automated_test.json"
	player.position = REFUGE_POS
	_save_progress(path)
	loot.pack.clear()
	player.position = BATTERY_POS
	_restore_save(path)
	assert(loot.pack.size() == 1 and player.position == REFUGE_POS and stage == 4)
	for suffix: String in ["", ".bak", ".tmp"]:
		DirAccess.remove_absolute(ProjectSettings.globalize_path(path + suffix))
	print("WINTER_INVENTORY_OK ui_transfer=true pause=true range=true restored_power=true world_save=true")
	get_tree().quit()
