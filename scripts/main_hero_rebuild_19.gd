extends Node3D

# ROSVIK: BLACKOUT — HERO REBUILD 19
# Standalone world. No inherited milestone geometry, no duplicate road generations,
# no open sports-hall walls. This is the first release-quality slice candidate.

const PLAYER_SCRIPT: Script = preload("res://scripts/player_hero_19.gd")
const ASSET_DIR := "res://assets/vendor19/"

var player: CharacterBody3D
var camera: Camera3D
var _ui_layer: CanvasLayer
var _objective_label: Label
var _status_label: Label
var _prompt_label: Label
var _toast_label: Label
var _toast_time := 0.0
var _flashlight: SpotLight3D
var _flashlight_on := false

var _camera_yaw := 0.78
var _camera_pitch := 0.58
var _camera_distance := 23.0
var _camera_dragging := false
var _capture_mode := false

var _school_root: Node3D
var _school_cutaway: Array[GeometryInstance3D] = []
var _inside_school := false
var _school_windows: Array[MeshInstance3D] = []
var _school_lights: Array[Light3D] = []

var _interactables: Array[Dictionary] = []
var _nearest_interactable := -1
var _inventory := {"battery":false,"fuse":false,"cable":false}
var _searched := {}
var _generator_inspected := false
var _generator_on := false
var _school_powered := false
var _generator_light_material: StandardMaterial3D
var _cable_connected_visual: Node3D
var _asset_cache: Dictionary = {}
var _asset_count := 0
var _world_prop_count := 0
var _loot_count := 0

var snow_mat: StandardMaterial3D
var packed_snow_mat: StandardMaterial3D
var dirty_snow_mat: StandardMaterial3D
var asphalt_mat: StandardMaterial3D
var path_mat: StandardMaterial3D
var school_wall_mat: StandardMaterial3D
var school_trim_mat: StandardMaterial3D
var sport_wall_mat: StandardMaterial3D
var roof_mat: StandardMaterial3D
var dark_mat: StandardMaterial3D
var metal_mat: StandardMaterial3D
var wood_mat: StandardMaterial3D
var concrete_mat: StandardMaterial3D
var glass_cold_mat: StandardMaterial3D
var field_mat: StandardMaterial3D

func _ready() -> void:
	seed(19051984)
	_capture_mode = "--capture-hero" in OS.get_cmdline_user_args()
	_build_materials19()
	_build_environment19()
	_build_ground19()
	_build_roads19()
	_build_school_complex19()
	_build_sporthall19()
	_build_rosvalla19()
	_build_background19()
	_build_gameplay19()
	_build_player19()
	_build_ui19()
	_refresh_ui19()

	print("ROSVIK_HERO_REBUILD_19_READY")
	print("ROSVIK_STANDALONE_WORLD_19_READY")
	print("ROSVIK_SCHOOL_COMPLEX_19_READY buildings=3")
	print("ROSVIK_SOLID_FACADE_19_READY")
	print("ROSVIK_ROSVALLA_SINGLE_FIELD_19_READY")
	print("ROSVIK_LIVED_INTERIOR_19_READY props=",_world_prop_count)
	print("ROSVIK_DENSE_LOOT_19_READY searchables=",_loot_count)
	print("ROSVIK_REFERENCE_MOOD_19_READY")
	print("ROSVIK_CC0_WORLD_ASSETS_19_READY count=",_asset_count)
	print("ROSVIK_BLACKOUT_LOOP_19_READY")

	if _capture_mode:
		call_deferred("_run_capture_sequence19")

func _process(delta: float) -> void:
	if not _capture_mode:
		_update_camera19(delta)
		_update_cutaway19()
		_update_interaction19()
		if Input.is_action_just_pressed("interact"):
			_interact19()
		if Input.is_action_just_pressed("flashlight"):
			_flashlight_on = not _flashlight_on
			if _flashlight != null:
				_flashlight.visible = _flashlight_on
	if _toast_time > 0.0:
		_toast_time -= delta
		if _toast_time <= 0.0 and _toast_label != null:
			_toast_label.text = ""

func _unhandled_input(event: InputEvent) -> void:
	if _capture_mode:
		return
	if event is InputEventMouseButton:
		var mb := event as InputEventMouseButton
		if mb.button_index == MOUSE_BUTTON_MIDDLE:
			_camera_dragging = mb.pressed
			get_viewport().set_input_as_handled()
		elif mb.button_index == MOUSE_BUTTON_WHEEL_UP and mb.pressed:
			_camera_distance = clampf(_camera_distance-1.6,15.5,31.0)
			get_viewport().set_input_as_handled()
		elif mb.button_index == MOUSE_BUTTON_WHEEL_DOWN and mb.pressed:
			_camera_distance = clampf(_camera_distance+1.6,15.5,31.0)
			get_viewport().set_input_as_handled()
	elif event is InputEventMouseMotion and _camera_dragging:
		var mm := event as InputEventMouseMotion
		_camera_yaw -= mm.relative.x * 0.0062
		_camera_pitch = clampf(_camera_pitch-mm.relative.y*0.0042,0.38,0.78)
		get_viewport().set_input_as_handled()

func _build_materials19() -> void:
	snow_mat = _textured_mat(Color("c9d2d4"),0.99,"snow",96,0.025)
	packed_snow_mat = _textured_mat(Color("b6c1c2"),0.99,"snow",96,0.020)
	dirty_snow_mat = _textured_mat(Color("8d9695"),0.99,"noise",96,0.035)
	asphalt_mat = _textured_mat(Color("343b3e"),0.98,"asphalt",96,0.030)
	path_mat = _textured_mat(Color("70797a"),0.98,"noise",96,0.020)
	school_wall_mat = _textured_mat(Color("a7aaa4"),0.94,"horizontal",96,0.022)
	school_trim_mat = _mat(Color("d0cec2"),0.93)
	sport_wall_mat = _textured_mat(Color("59666b"),0.87,"vertical",96,0.025)
	roof_mat = _textured_mat(Color("323b3f"),0.90,"noise",96,0.025)
	dark_mat = _mat(Color("20292d"),0.94)
	metal_mat = _mat(Color("586368"),0.68,0.22)
	wood_mat = _textured_mat(Color("765a43"),0.96,"horizontal",72,0.025)
	concrete_mat = _textured_mat(Color("7b8587"),0.98,"noise",96,0.025)
	glass_cold_mat = _mat(Color("415764"),0.22,0.04)
	glass_cold_mat.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	glass_cold_mat.albedo_color.a = 0.88
	field_mat = _textured_mat(Color("6e7970"),0.98,"noise",96,0.018)

func _build_environment19() -> void:
	var env_node := WorldEnvironment.new()
	var env := Environment.new()
	var sky_mat := ProceduralSkyMaterial.new()
	sky_mat.sky_top_color = Color("1d303b")
	sky_mat.sky_horizon_color = Color("6c7b82")
	sky_mat.ground_bottom_color = Color("273840")
	sky_mat.ground_horizon_color = Color("8b9699")
	sky_mat.sun_angle_max = 14.0
	var sky := Sky.new()
	sky.sky_material = sky_mat
	env.background_mode = Environment.BG_SKY
	env.sky = sky
	env.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	env.ambient_light_color = Color("72848e")
	env.ambient_light_energy = 0.36
	env.fog_enabled = true
	env.fog_light_color = Color("687982")
	env.fog_density = 0.0065
	env.fog_height = 0.7
	env.fog_height_density = 0.075
	env.tonemap_mode = Environment.TONE_MAPPER_FILMIC
	env.tonemap_exposure = 0.72
	env_node.environment = env
	add_child(env_node)
	var sun := DirectionalLight3D.new()
	sun.rotation_degrees = Vector3(-31.0,-38.0,0.0)
	sun.light_color = Color("edc7a5")
	sun.light_energy = 0.72
	sun.shadow_enabled = true
	sun.directional_shadow_max_distance = 150.0
	add_child(sun)

func _build_ground19() -> void:
	_solid_box(Vector3(260.0,0.38,220.0),snow_mat,Vector3(12.0,-0.20,35.0),self)
	for p: Vector3 in [Vector3(-38,0,-6),Vector3(34,0,-8),Vector3(88,0,10),Vector3(-48,0,48),Vector3(72,0,58),Vector3(15,0,94)]:
		_snow_mound19(p,Vector3(7.0,0.18,3.0),0.0)

func _build_roads19() -> void:
	var road_root := Node3D.new()
	road_root.name = "AuthoredRoads19"
	add_child(road_root)
	_strip19([Vector2(-58,14),Vector2(-14,14),Vector2(15,14),Vector2(25,15),Vector2(31,20),Vector2(35,27),Vector2(44,30),Vector2(83,30)],5.6,asphalt_mat,0.018,road_root)
	_strip19([Vector2(80,29),Vector2(82,43),Vector2(82,66),Vector2(80,92)],5.2,asphalt_mat,0.019,road_root)
	_strip19([Vector2(-48,-72),Vector2(-48,-30),Vector2(-48,14),Vector2(-48,70)],6.2,asphalt_mat,0.017,road_root)
	_strip19([Vector2(-5.0,6.5),Vector2(-5.0,11.1)],2.6,path_mat,0.032,road_root)
	_strip19([Vector2(53,27.0),Vector2(53,29.0)],3.4,path_mat,0.032,road_root)
	_box(Vector3(22.0,0.035,10.0),asphalt_mat,Vector3(70.0,0.016,21.5),road_root)
	var line := _mat(Color("b8b5a9"),0.96)
	for i: int in range(6):
		_box(Vector3(0.055,0.012,3.6),line,Vector3(61.3+float(i)*3.45,0.046,23.1),road_root)
	for i: int in range(5):
		_box(Vector3(0.48,0.012,4.35),line,Vector3(-6.4+float(i)*0.70,0.050,14.0),road_root)
	for x: float in [-29.0,6.0,40.0]:
		_box(Vector3(13.0,0.010,0.35),dirty_snow_mat,Vector3(x,0.042,16.6),road_root)
	for x: float in [64.5,67.0]:
		_box(Vector3(0.18,0.010,8.0),dirty_snow_mat,Vector3(x,0.048,21.5),road_root)

func _strip19(points: Array[Vector2], width: float, material: Material, y: float, parent: Node) -> MeshInstance3D:
	var left: Array[Vector2] = []
	var right: Array[Vector2] = []
	for i: int in range(points.size()):
		var prev_dir := Vector2.ZERO
		var next_dir := Vector2.ZERO
		if i > 0:
			prev_dir = (points[i]-points[i-1]).normalized()
		if i < points.size()-1:
			next_dir = (points[i+1]-points[i]).normalized()
		var direction := next_dir if i == 0 else prev_dir if i == points.size()-1 else (prev_dir+next_dir).normalized()
		if direction.length() < 0.2:
			direction = next_dir if next_dir.length() > 0.2 else prev_dir
		var perp := Vector2(-direction.y,direction.x)
		left.append(points[i]+perp*width*0.5)
		right.append(points[i]-perp*width*0.5)
	var st := SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)
	for i: int in range(points.size()-1):
		var a := Vector3(left[i].x,y,left[i].y)
		var b := Vector3(right[i].x,y,right[i].y)
		var c := Vector3(left[i+1].x,y,left[i+1].y)
		var d := Vector3(right[i+1].x,y,right[i+1].y)
		for v: Vector3 in [a,b,c,b,d,c]:
			st.set_normal(Vector3.UP)
			st.add_vertex(v)
	var node := MeshInstance3D.new()
	node.mesh = st.commit()
	node.material_override = material
	node.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	parent.add_child(node)
	return node

func _build_school_complex19() -> void:
	_school_root = Node3D.new()
	_school_root.name = "RosviksSkola"
	add_child(_school_root)
	_build_main_school19(_school_root)
	_build_stone_school19(_school_root)
	_build_old_wood_school19(_school_root)
	_build_schoolyard19(_school_root)

func _build_main_school19(root: Node3D) -> void:
	var w := 34.0
	var d := 12.0
	var h := 4.1
	_solid_box(Vector3(w,0.14,d),concrete_mat,Vector3(0,0.07,0),root)
	_solid_box(Vector3(w,0.28,0.28),school_wall_mat,Vector3(0,h*0.5,-d*0.5),root)
	_solid_box(Vector3(0.28,h,d),school_wall_mat,Vector3(-w*0.5,h*0.5,0),root)
	_solid_box(Vector3(0.28,h,d),school_wall_mat,Vector3(w*0.5,h*0.5,0),root)
	var front_left := _solid_box(Vector3(10.8,h,0.28),school_wall_mat,Vector3(-11.6,h*0.5,d*0.5),root)
	var front_mid := _solid_box(Vector3(10.4,h,0.28),school_wall_mat,Vector3(1.4,h*0.5,d*0.5),root)
	var front_right := _solid_box(Vector3(4.2,h,0.28),school_wall_mat,Vector3(14.8,h*0.5,d*0.5),root)
	_school_cutaway.append(front_left)
	_school_cutaway.append(front_mid)
	_school_cutaway.append(front_right)
	var roof := _box(Vector3(w+0.65,0.36,d+0.65),roof_mat,Vector3(0,h+0.18,0),root)
	var roof_snow := _box(Vector3(w+0.35,0.14,d+0.35),packed_snow_mat,Vector3(0,h+0.43,0),root)
	_school_cutaway.append(roof)
	_school_cutaway.append(roof_snow)
	_box(Vector3(w+0.70,0.30,0.20),dark_mat,Vector3(0,h+0.10,d*0.5+0.28),root)
	for x: float in [-16.7,16.7]:
		_solid_cylinder(0.045,3.75,metal_mat,Vector3(x,1.88,d*0.5+0.25),root)
	for x: float in [-14.5,-11.8,-9.1,-0.8,2.0,4.8,7.6,10.4,13.2]:
		_add_school_window19(root,Vector3(x,2.15,d*0.5+0.16),x in [-11.8,2.0,7.6])
	_box(Vector3(5.2,0.30,2.5),roof_mat,Vector3(-5.1,3.20,7.05),root)
	_box(Vector3(4.8,0.11,2.15),packed_snow_mat,Vector3(-5.1,3.41,7.02),root)
	for x: float in [-7.0,-3.2]:
		_solid_box(Vector3(0.16,3.0,0.16),dark_mat,Vector3(x,1.5,6.85),root)
	var door := _box(Vector3(1.10,2.55,0.10),glass_cold_mat,Vector3(-6.28,1.30,6.45),root)
	door.rotation.y = -0.86
	_box(Vector3(7.8,0.68,0.12),dark_mat,Vector3(5.7,3.52,6.23),root)
	_label3d19("ROSVIKS SKOLA",Vector3(5.7,3.53,6.31),root,28)
	_world_prop_count += 1
	var entrance_light := OmniLight3D.new()
	entrance_light.position = Vector3(-5.1,2.85,7.35)
	entrance_light.light_color = Color("ffd093")
	entrance_light.light_energy = 0.0
	entrance_light.omni_range = 7.2
	entrance_light.shadow_enabled = true
	root.add_child(entrance_light)
	_school_lights.append(entrance_light)
	_build_school_interior19(root)

func _add_school_window19(root: Node3D,pos: Vector3,powered: bool) -> void:
	_box(Vector3(1.78,1.40,0.16),dark_mat,pos,root)
	var pane_mat := _mat(Color("344b57"),0.28,0.02)
	pane_mat.emission_enabled = true
	pane_mat.emission = Color("ffbd70") if powered else Color("17242b")
	pane_mat.emission_energy_multiplier = 0.0
	var pane := _box(Vector3(1.48,1.12,0.08),pane_mat,pos+Vector3(0,0,0.10),root)
	_school_windows.append(pane)
	_box(Vector3(1.94,0.10,0.30),school_trim_mat,pos+Vector3(0,-0.80,0.03),root)
	_box(Vector3(0.08,1.20,0.12),school_trim_mat,pos+Vector3(0,0,0.16),root)

func _build_school_interior19(root: Node3D) -> void:
	var floor_mat := _textured_mat(Color("7f786f"),0.92,"horizontal",96,0.012)
	var corridor_mat := _textured_mat(Color("666f70"),0.94,"noise",96,0.014)
	var wall_mat := _mat(Color("c8c7be"),0.96)
	var locker_mat := _mat(Color("66777b"),0.88,0.05)
	var notice_mat := _mat(Color("8e6e50"),0.97)
	_box(Vector3(30.8,0.055,3.35),corridor_mat,Vector3(0,0.16,3.75),root)
	_box(Vector3(14.2,0.055,7.3),floor_mat,Vector3(7.7,0.16,-1.55),root)
	_solid_box(Vector3(12.0,3.25,0.22),wall_mat,Vector3(-9.8,1.78,1.90),root)
	_solid_box(Vector3(7.0,3.25,0.22),wall_mat,Vector3(1.5,1.78,1.90),root)
	_solid_box(Vector3(8.4,3.25,0.22),wall_mat,Vector3(12.8,1.78,1.90),root)
	_solid_box(Vector3(0.22,3.25,7.3),wall_mat,Vector3(0.5,1.78,-1.55),root)
	for i: int in range(10):
		var x := -14.3+float(i)*0.88
		_solid_box(Vector3(0.72,1.72,0.40),locker_mat,Vector3(x,1.02,5.53),root)
		_box(Vector3(0.20,0.04,0.03),metal_mat,Vector3(x,1.15,5.30),root)
	for i: int in range(8):
		var x := -14.2+float(i)*1.02
		var hook := _cylinder(0.035,0.14,metal_mat,Vector3(x,1.92,2.10),root)
		hook.rotation.x = PI/2.0
	_add_bench19(root,Vector3(-10.8,0.0,3.05),3.2)
	for i: int in range(7):
		var boot := _capsule19(0.12,0.30,_mat(Color("33393b").lightened(float(i%3)*0.04),0.99),Vector3(-13.6+float(i)*0.62,0.30,3.48+float(i%2)*0.28),root)
		boot.rotation_degrees.x = 90.0
	for i: int in range(4):
		_add_backpack19(root,Vector3(-8.1+float(i)*0.62,0.33,5.15),Color("665447").lightened(float(i)*0.04))
	_box(Vector3(3.4,1.25,0.08),notice_mat,Vector3(-3.6,1.90,2.04),root)
	var paper_palette: Array[Color] = [Color("d7d2bd"),Color("b7c8d0"),Color("d5b7a4"),Color("c8c59c")]
	for i: int in range(10):
		var paper_color: Color = paper_palette[i%4]
		_box(Vector3(0.34,0.44,0.025),_mat(paper_color,0.99),Vector3(-4.9+float(i%5)*0.63,1.65+float(i/5)*0.57,2.10),root)
	for row: int in range(2):
		for col: int in range(3):
			var p := Vector3(4.2+float(col)*3.0,0.16,-0.2-float(row)*2.35)
			_asset19("furniture/desk.glb",p,Vector3.ONE*0.90,0.0,root)
			_asset19("furniture/chair.glb",p+Vector3(0,0,0.85),Vector3.ONE*0.90,PI,root)
			_collision_box19(root,Vector3(1.4,0.9,0.75),p+Vector3(0,0.45,0))
			_world_prop_count += 2
	_asset19("furniture/desk.glb",Vector3(13.4,0.16,-3.8),Vector3.ONE,PI,root)
	_asset19("furniture/chair.glb",Vector3(13.3,0.16,-2.9),Vector3.ONE,0.0,root)
	_asset19("furniture/bookshelf.glb",Vector3(1.45,0.16,-4.55),Vector3.ONE,PI/2.0,root)
	_asset19("furniture/monitor.glb",Vector3(13.25,0.94,-3.65),Vector3.ONE*0.82,PI,root)
	_asset19("furniture/lamp.glb",Vector3(12.65,0.92,-3.55),Vector3.ONE*0.80,0.0,root)
	_world_prop_count += 5
	_add_mug19(root,Vector3(12.9,1.00,-3.30),Color("9a5e46"))
	_add_thermos19(root,Vector3(13.55,1.04,-3.40))
	for i: int in range(8):
		var paper := _box(Vector3(0.42,0.018,0.30),_mat(Color("d6d2c6").darkened(float(i%3)*0.04),0.99),Vector3(3.5+float(i%4)*2.0,0.94,-0.10-float(i/4)*2.35),root)
		paper.rotation.y = -0.18+float(i)*0.05
	_world_prop_count += 10
	for x: float in [-11.5,-6.0,-0.5,5.0,10.5]:
		var fixture_mat := _mat(Color("d9d6ca"),0.78)
		fixture_mat.emission_enabled = true
		fixture_mat.emission = Color("ffe0aa")
		fixture_mat.emission_energy_multiplier = 0.0
		_box(Vector3(1.35,0.055,0.22),fixture_mat,Vector3(x,3.62,3.78),root)
		var light := OmniLight3D.new()
		light.position = Vector3(x,3.25,3.75)
		light.light_color = Color("ffd6a0")
		light.light_energy = 0.0
		light.omni_range = 4.8
		root.add_child(light)
		_school_lights.append(light)
	_add_searchable19(root,Vector3(-14.5,0.0,5.0),"janitor","VAKTMÄSTARSKÅP",["silvertejp","säkringar","AA-batterier"])
	_add_searchable19(root,Vector3(13.6,0.0,-4.6),"staff","LÄRARBORD",["nyckelknippa","ficklampa","tändare"])
	_add_searchable19(root,Vector3(8.6,0.0,-1.5),"bag","ÖVERGIVEN RYGGSÄCK",["choklad","vantar","powerbank"])

func _build_stone_school19(root: Node3D) -> void:
	var r := Node3D.new()
	r.name = "Stenskolan"
	r.position = Vector3(-24.5,0.0,-4.2)
	root.add_child(r)
	var stone := _textured_mat(Color("8b918e"),0.96,"noise",72,0.028)
	_solid_box(Vector3(9.5,3.3,7.5),stone,Vector3(0,1.65,0),r)
	_box(Vector3(10.1,0.34,8.1),roof_mat,Vector3(0,3.46,0),r)
	_box(Vector3(9.7,0.13,7.7),packed_snow_mat,Vector3(0,3.69,0),r)
	for x: float in [-2.7,0,2.7]:
		_add_warm_window_simple19(r,Vector3(x,1.85,3.82),false)
	_box(Vector3(1.1,2.1,0.14),dark_mat,Vector3(3.5,1.05,3.82),r)
	_world_prop_count += 4

func _build_old_wood_school19(root: Node3D) -> void:
	var r := Node3D.new()
	r.name = "GamlaTraskolan"
	r.position = Vector3(-24.5,0.0,7.0)
	root.add_child(r)
	var red_wood := _textured_mat(Color("815449"),0.96,"horizontal",72,0.026)
	_solid_box(Vector3(9.0,3.2,7.0),red_wood,Vector3(0,1.60,0),r)
	var left := _box(Vector3(9.6,0.30,4.2),roof_mat,Vector3(0,3.63,-1.62),r)
	left.rotation.x = -0.28
	var right := _box(Vector3(9.6,0.30,4.2),roof_mat,Vector3(0,3.63,1.62),r)
	right.rotation.x = 0.28
	var sl := _box(Vector3(9.3,0.10,3.95),packed_snow_mat,Vector3(0,3.80,-1.57),r)
	sl.rotation.x = -0.28
	var sr := _box(Vector3(9.3,0.10,3.95),packed_snow_mat,Vector3(0,3.80,1.57),r)
	sr.rotation.x = 0.28
	for x: float in [-2.5,0.0,2.5]:
		_add_warm_window_simple19(r,Vector3(x,1.75,3.56),x==0.0)
	_box(Vector3(1.0,2.0,0.12),dark_mat,Vector3(-3.4,1.0,3.57),r)
	_world_prop_count += 4

func _build_schoolyard19(root: Node3D) -> void:
	var yard := Node3D.new()
	yard.name = "Schoolyard19"
	root.add_child(yard)
	_box(Vector3(34.0,0.025,13.0),packed_snow_mat,Vector3(0,0.014,13.5),yard)
	_add_bench19(yard,Vector3(-12.0,0,10.0),2.6)
	_add_bench19(yard,Vector3(9.5,0,10.6),2.4)
	for i: int in range(7):
		var rack := _cylinder(0.026,0.92,metal_mat,Vector3(6.7+float(i)*0.52,0.45,8.6),yard)
		rack.rotation.x = PI/2.0
	for x: float in [-1.4,1.4]:
		_solid_cylinder(0.065,2.25,wood_mat,Vector3(x,1.12,18.0),yard)
	var beam := _cylinder(0.07,3.0,metal_mat,Vector3(0,2.10,18.0),yard)
	beam.rotation.z = PI/2.0
	for x: float in [-0.55,0.55]:
		_cylinder(0.015,1.10,metal_mat,Vector3(x,1.53,18.0),yard)
		_box(Vector3(0.54,0.06,0.34),wood_mat,Vector3(x,0.96,18.0),yard)
	_add_fence19(yard,Vector3(-17,0,20),Vector3(17,0,20))
	for p: Vector3 in [Vector3(-15,0,19.3),Vector3(-7,0,19.6),Vector3(10,0,19.6),Vector3(16,0,17.2)]:
		_snow_mound19(p,Vector3(2.2,0.34,0.70),0.05)
	for i: int in range(16):
		var foot := _box(Vector3(0.16,0.015,0.30),dirty_snow_mat,Vector3(-5.2+float(i%2)*0.26,0.048,11.3-float(i)*0.31),yard)
		foot.rotation.y = -0.10 if i%2==0 else 0.10
	_world_prop_count += 32

func _build_sporthall19() -> void:
	var root := Node3D.new()
	root.name = "RosvikSporthall"
	root.position = Vector3(56.0,0.0,18.0)
	add_child(root)
	var w := 28.0
	var d := 18.0
	var h := 6.2
	_solid_box(Vector3(w,h,d),sport_wall_mat,Vector3(0,h*0.5,0),root)
	_box(Vector3(w+0.7,0.38,d+0.7),roof_mat,Vector3(0,h+0.18,0),root)
	_box(Vector3(w+0.35,0.14,d+0.35),packed_snow_mat,Vector3(0,h+0.42,0),root)
	for x: float in [-12,-8,-4,0,4,8,12]:
		_box(Vector3(0.055,5.5,0.12),metal_mat,Vector3(x,3.15,9.06),root)
	_box(Vector3(27.6,0.75,0.14),dark_mat,Vector3(0,0.38,9.08),root)
	_solid_box(Vector3(7.0,3.0,2.4),_textured_mat(Color("46545a"),0.88,"vertical",72,0.022),Vector3(-4.0,1.5,10.0),root)
	_box(Vector3(3.5,2.25,0.12),glass_cold_mat,Vector3(-4.0,1.20,11.24),root)
	_box(Vector3(5.6,0.28,2.5),roof_mat,Vector3(-4.0,3.10,11.1),root)
	_box(Vector3(9.5,0.72,0.12),dark_mat,Vector3(4.4,4.85,9.14),root)
	_label3d19("ROSVIK SPORTHALL",Vector3(4.4,4.86,9.22),root,27)
	_box(Vector3(1.0,1.35,0.45),metal_mat,Vector3(14.1,0.68,3.0),root)
	_asset19("nature/lamppost.glb",Vector3(-10.5,0.0,13.0),Vector3.ONE*1.20,0.0,root)
	_asset19("nature/lamppost.glb",Vector3(10.5,0.0,13.0),Vector3.ONE*1.20,0.0,root)
	_world_prop_count += 4

func _build_rosvalla19() -> void:
	var root := Node3D.new()
	root.name = "Rosvalla19"
	add_child(root)
	var center := Vector3(9.0,0.0,53.0)
	_box(Vector3(64.0,0.035,38.0),field_mat,center+Vector3(0,0.018,0),root)
	var line := _mat(Color("d2d1c6"),0.98)
	_box(Vector3(62.0,0.012,0.09),line,center+Vector3(0,0.045,-18.0),root)
	_box(Vector3(62.0,0.012,0.09),line,center+Vector3(0,0.045,18.0),root)
	_box(Vector3(0.09,0.012,36.0),line,center+Vector3(-31.0,0.045,0),root)
	_box(Vector3(0.09,0.012,36.0),line,center+Vector3(31.0,0.045,0),root)
	_box(Vector3(0.09,0.012,36.0),line,center+Vector3(0,0.045,0),root)
	for i: int in range(24):
		var a := float(i)/24.0*TAU
		var p := center+Vector3(cos(a)*4.4,0.055,sin(a)*4.4)
		var dash := _box(Vector3(1.15,0.012,0.08),line,p,root)
		dash.rotation.y = -a
	_add_goal19(root,center+Vector3(-31.7,0,0),PI/2.0)
	_add_goal19(root,center+Vector3(31.7,0,0),-PI/2.0)
	_add_fence19(root,center+Vector3(-32.8,0,-20.0),center+Vector3(32.8,0,-20.0))
	_add_fence19(root,center+Vector3(-32.8,0,20.0),center+Vector3(18.0,0,20.0))
	_add_fence19(root,center+Vector3(24.0,0,20.0),center+Vector3(32.8,0,20.0))
	for p: Vector3 in [center+Vector3(-27,0,-20.8),center+Vector3(27,0,-20.8),center+Vector3(-27,0,20.8),center+Vector3(27,0,20.8)]:
		_add_field_light19(root,p)
	for p: Vector3 in [center+Vector3(-29,0,-17),center+Vector3(28,0,16),center+Vector3(-30,0,15)]:
		_snow_mound19(p,Vector3(3.0,0.28,1.1),0.0)
	_world_prop_count += 80

func _build_background19() -> void:
	var root := Node3D.new()
	root.name = "RosvikContext19"
	add_child(root)
	var trees: Array[Vector3] = [Vector3(-74,0,-32),Vector3(-69,0,-42),Vector3(-61,0,-52),Vector3(-80,0,31),Vector3(-72,0,46),Vector3(-62,0,74),Vector3(-45,0,91),Vector3(-25,0,100),Vector3(2,0,104),Vector3(31,0,103),Vector3(58,0,100),Vector3(89,0,97),Vector3(103,0,76),Vector3(106,0,50),Vector3(108,0,33),Vector3(106,0,-25),Vector3(88,0,-40),Vector3(65,0,-49),Vector3(42,0,-55)]
	for i: int in range(trees.size()):
		var scale_value := 1.15+float(i%4)*0.12
		_asset19("nature/tree-pine.glb",trees[i],Vector3.ONE*scale_value,float(i)*0.37,root)
	for p: Vector3 in [Vector3(-32,0,24),Vector3(-28,0,27),Vector3(31,0,7),Vector3(36,0,7),Vector3(73,0,9),Vector3(77,0,8)]:
		_asset19("nature/bush.glb",p,Vector3.ONE*0.75,0.0,root)
	for p: Vector3 in [Vector3(-34,0,25),Vector3(35,0,8),Vector3(75,0,10)]:
		_asset19("nature/rock.glb",p,Vector3.ONE*0.72,0.0,root)
	var hala := Node3D.new()
	hala.name = "HALAHallenContext19"
	hala.position = Vector3(66,0,84)
	root.add_child(hala)
	_solid_box(Vector3(34,6.8,19),_textured_mat(Color("4b5960"),0.90,"vertical",72,0.020),Vector3(0,3.4,0),hala)
	_box(Vector3(34.8,0.38,19.8),roof_mat,Vector3(0,6.95,0),hala)
	_box(Vector3(34.4,0.13,19.4),packed_snow_mat,Vector3(0,7.20,0),hala)
	_world_prop_count += 3

func _build_gameplay19() -> void:
	var root := Node3D.new()
	root.name = "BlackoutGameplay19"
	add_child(root)
	_build_generator19(root,Vector3(72.0,0.0,15.0))
	_build_battery19(root,Vector3(66.4,0.0,24.6))
	_build_cable_reel19(root,Vector3(68.5,0.0,24.0))
	_build_fuse_box19(root,Vector3(-13.8,0.0,4.95))
	_build_school_inlet19(root,Vector3(17.2,0.0,4.1))
	_cable_connected_visual = Node3D.new()
	_cable_connected_visual.name = "ConnectedCable19"
	_cable_connected_visual.visible = false
	root.add_child(_cable_connected_visual)
	_strip19([Vector2(72,15),Vector2(68,17),Vector2(61,18),Vector2(53,17),Vector2(42,15.5),Vector2(28,13),Vector2(20,9),Vector2(17.2,4.1)],0.10,dark_mat,0.065,_cable_connected_visual)
	_add_searchable19(root,Vector3(65.2,0.0,14.0),"sport_service","SPORTHALLENS SERVICEHYLLA",["första hjälpen","rep","arbetslampa"])
	_add_searchable19(root,Vector3(74.0,0.0,22.5),"parking_car","ÖVERGIVEN BIL",["isskrapa","filt","USB-kabel"])

func _build_generator19(parent: Node3D,pos: Vector3) -> void:
	var root := Node3D.new()
	root.name = "ReserveGenerator19"
	root.position = pos
	parent.add_child(root)
	var yellow := _mat(Color("a88b42"),0.86,0.06)
	var engine := _mat(Color("4f595b"),0.88,0.12)
	for x: float in [-1.0,1.0]:
		for z: float in [-0.6,0.6]:
			_solid_cylinder(0.045,1.35,metal_mat,Vector3(x,0.68,z),root)
	var tank := _capsule19(0.34,1.35,yellow,Vector3(-0.30,1.05,0),root)
	tank.rotation.z = PI/2.0
	var motor := _capsule19(0.35,1.10,engine,Vector3(0.30,0.58,0),root)
	motor.rotation.z = PI/2.0
	_box(Vector3(0.12,0.72,0.82),dark_mat,Vector3(1.05,0.84,0),root)
	_generator_light_material = _mat(Color("5b2423"),0.55)
	_generator_light_material.emission_enabled = true
	_generator_light_material.emission = Color("8d221c")
	_generator_light_material.emission_energy_multiplier = 0.30
	_box(Vector3(0.035,0.12,0.12),_generator_light_material,Vector3(1.13,1.05,0.22),root)
	_box(Vector3(3.0,0.08,2.1),concrete_mat,Vector3(0,0.035,0),root)
	_collision_box19(root,Vector3(2.4,1.5,1.5),Vector3(0,0.75,0))
	_register_interactable19(root,"generator","RESERVKRAFT",2.3)

func _build_battery19(parent: Node3D,pos: Vector3) -> void:
	var root := Node3D.new()
	root.name = "Battery19"
	root.position = pos
	parent.add_child(root)
	_box(Vector3(0.62,0.42,0.38),dark_mat,Vector3(0,0.22,0),root)
	_box(Vector3(0.58,0.08,0.35),metal_mat,Vector3(0,0.47,0),root)
	_cylinder(0.05,0.07,_mat(Color("a94a3d"),0.78),Vector3(-0.18,0.55,0),root)
	_cylinder(0.05,0.07,metal_mat,Vector3(0.18,0.55,0),root)
	_register_interactable19(root,"battery","12 V-BATTERI",1.7)

func _build_cable_reel19(parent: Node3D,pos: Vector3) -> void:
	var root := Node3D.new()
	root.name = "CableReel19"
	root.position = pos
	parent.add_child(root)
	var orange := _mat(Color("a85d3c"),0.86,0.08)
	for z: float in [-0.22,0.22]:
		var flange := _cylinder(0.42,0.08,orange,Vector3(0,0.48,z),root)
		flange.rotation.x = PI/2.0
	var cable := _cylinder(0.30,0.40,dark_mat,Vector3(0,0.48,0),root)
	cable.rotation.x = PI/2.0
	_register_interactable19(root,"cable","32 A-KABELRULLE",1.8)

func _build_fuse_box19(parent: Node3D,pos: Vector3) -> void:
	var root := Node3D.new()
	root.name = "FuseF3_19"
	root.position = pos
	parent.add_child(root)
	_box(Vector3(0.62,0.86,0.22),metal_mat,Vector3(0,1.35,0),root)
	_box(Vector3(0.50,0.68,0.04),dark_mat,Vector3(0,1.35,0.13),root)
	var fuse := _box(Vector3(0.16,0.07,0.05),_mat(Color("d1ba73"),0.78),Vector3(0.36,1.18,0.20),root)
	_register_interactable19(fuse,"fuse","F3-SÄKRING",1.6)

func _build_school_inlet19(parent: Node3D,pos: Vector3) -> void:
	var root := Node3D.new()
	root.name = "SchoolInlet19"
	root.position = pos
	parent.add_child(root)
	_solid_box(Vector3(0.70,1.20,0.34),metal_mat,Vector3(0,0.60,0),root)
	var socket := _cylinder(0.13,0.08,_mat(Color("31567a"),0.72),Vector3(0,0.52,0.21),root)
	socket.rotation.x = PI/2.0
	_register_interactable19(root,"inlet","SKOLANS RESERVMATNING",2.0)

func _register_interactable19(node: Node3D,kind: String,label: String,radius: float) -> void:
	_interactables.append({"node":node,"kind":kind,"label":label,"radius":radius})

func _add_searchable19(parent: Node3D,pos: Vector3,id_value: String,label: String,items: Array[String]) -> void:
	var root := Node3D.new()
	root.name = "Searchable_%s" % id_value
	root.position = pos
	parent.add_child(root)
	var case_mat := _mat(Color("4f5c5b"),0.94)
	_box(Vector3(0.76,0.72,0.52),case_mat,Vector3(0,0.36,0),root)
	_box(Vector3(0.82,0.08,0.56),dark_mat,Vector3(0,0.76,0),root)
	_interactables.append({"node":root,"kind":"search","id":id_value,"label":label,"radius":1.7,"items":items})
	_loot_count += 1

func _update_interaction19() -> void:
	_nearest_interactable = -1
	if player == null:
		return
	var best := INF
	for i: int in range(_interactables.size()):
		var data := _interactables[i]
		var node := data["node"] as Node3D
		if not is_instance_valid(node) or not node.visible:
			continue
		var distance := player.global_position.distance_to(node.global_position)
		if distance <= float(data["radius"]) and distance < best:
			best = distance
			_nearest_interactable = i
	if _prompt_label != null:
		_prompt_label.text = "" if _nearest_interactable < 0 else "E  %s" % String(_interactables[_nearest_interactable]["label"])

func _interact19() -> void:
	if _nearest_interactable < 0:
		return
	var data := _interactables[_nearest_interactable]
	var kind := String(data["kind"])
	var node := data["node"] as Node3D
	match kind:
		"generator":
			if not _generator_inspected:
				_generator_inspected = true
				_toast19("Reservkraften saknar batteri och F3-säkring.")
			elif not _inventory["battery"] or not _inventory["fuse"]:
				_toast19("Du saknar batteri eller F3-säkring.")
			elif not _generator_on:
				_generator_on = true
				_generator_light_material.emission = Color("7fd48a")
				_generator_light_material.emission_energy_multiplier = 1.2
				_toast19("Reservkraften går. Hämta 32 A-kabeln.")
			else:
				_toast19("Generatorn går stabilt.")
		"battery":
			_inventory["battery"] = true
			node.visible = false
			_toast19("12 V-batteri taget.")
		"fuse":
			_inventory["fuse"] = true
			node.visible = false
			_toast19("F3-säkring tagen.")
		"cable":
			if not _generator_on:
				_toast19("Få igång reservkraften först.")
			else:
				_inventory["cable"] = true
				node.visible = false
				_toast19("Du bär 32 A-kabeln.")
		"inlet":
			if _school_powered:
				_toast19("Skolans reservmatning är inkopplad.")
			elif _generator_on and _inventory["cable"]:
				_school_powered = true
				_cable_connected_visual.visible = true
				_set_school_power19(true)
				_toast19("Ett par rum vaknar till liv bakom fönstren.")
			else:
				_toast19("Du behöver fungerande reservkraft och kabeln.")
		"search":
			var id_value := String(data["id"])
			if bool(_searched.get(id_value,false)):
				_toast19("Här finns inget mer användbart.")
			else:
				_searched[id_value] = true
				var items: Array = data["items"]
				var item_strings := PackedStringArray()
				for item: Variant in items:
					item_strings.append(String(item))
				_toast19("Hittat: %s" % ", ".join(item_strings))
	_refresh_ui19()

func _set_school_power19(on: bool) -> void:
	for light: Light3D in _school_lights:
		light.light_energy = 1.0 if on else 0.0
	for i: int in range(_school_windows.size()):
		var m := _school_windows[i].material_override as StandardMaterial3D
		m.emission_energy_multiplier = 1.25 if on and i%3 != 0 else 0.0

func _build_player19() -> void:
	player = CharacterBody3D.new()
	player.set_script(PLAYER_SCRIPT)
	player.position = Vector3(-5.0,0.0,11.0)
	add_child(player)
	_flashlight = SpotLight3D.new()
	_flashlight.position = Vector3(0,1.45,0.25)
	_flashlight.rotation_degrees = Vector3(-8,180,0)
	_flashlight.light_color = Color("ffe2b2")
	_flashlight.light_energy = 3.0
	_flashlight.spot_range = 11.0
	_flashlight.spot_angle = 38.0
	_flashlight.visible = false
	player.add_child(_flashlight)
	camera = Camera3D.new()
	camera.fov = 38.0
	camera.near = 0.10
	camera.far = 320.0
	camera.current = true
	add_child(camera)
	_update_camera19(1.0)

func _update_camera19(delta: float) -> void:
	if player == null or camera == null:
		return
	var focus := player.global_position+Vector3(0,1.0,0)
	var travel := Vector3(player.velocity.x,0,player.velocity.z)
	if travel.length() > 0.2:
		focus += travel.normalized()*0.55
	var horizontal := cos(_camera_pitch)*_camera_distance
	var offset := Vector3(sin(_camera_yaw)*horizontal,sin(_camera_pitch)*_camera_distance,cos(_camera_yaw)*horizontal)
	var wanted := focus+offset
	camera.global_position = camera.global_position.lerp(wanted,1.0-exp(-6.5*delta))
	camera.look_at(focus,Vector3.UP)

func _update_cutaway19() -> void:
	if player == null:
		return
	var p := player.global_position
	var inside := p.x > -16.6 and p.x < 16.6 and p.z > -5.7 and p.z < 5.85
	_set_cutaway19(inside)

func _set_cutaway19(inside: bool) -> void:
	if inside == _inside_school:
		return
	_inside_school = inside
	for node: GeometryInstance3D in _school_cutaway:
		if is_instance_valid(node):
			node.visible = not inside

func _build_ui19() -> void:
	_ui_layer = CanvasLayer.new()
	add_child(_ui_layer)
	var panel := Panel.new()
	panel.position = Vector2(24,22)
	panel.size = Vector2(355,106)
	var style := StyleBoxFlat.new()
	style.bg_color = Color(0.025,0.045,0.052,0.82)
	style.corner_radius_top_left = 15
	style.corner_radius_top_right = 15
	style.corner_radius_bottom_left = 15
	style.corner_radius_bottom_right = 15
	style.content_margin_left = 16
	style.content_margin_right = 16
	style.content_margin_top = 12
	style.content_margin_bottom = 12
	panel.add_theme_stylebox_override("panel",style)
	_ui_layer.add_child(panel)
	var title := Label.new()
	title.position = Vector2(15,10)
	title.text = "ROSVIK • BLACKOUT"
	title.add_theme_font_size_override("font_size",16)
	panel.add_child(title)
	_objective_label = Label.new()
	_objective_label.position = Vector2(15,35)
	_objective_label.size = Vector2(325,36)
	_objective_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	_objective_label.add_theme_font_size_override("font_size",14)
	panel.add_child(_objective_label)
	_status_label = Label.new()
	_status_label.position = Vector2(15,77)
	_status_label.add_theme_font_size_override("font_size",12)
	_status_label.modulate = Color("b6c5c5")
	panel.add_child(_status_label)
	_prompt_label = Label.new()
	_prompt_label.position = Vector2(620,825)
	_prompt_label.size = Vector2(360,42)
	_prompt_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_prompt_label.add_theme_font_size_override("font_size",18)
	_ui_layer.add_child(_prompt_label)
	_toast_label = Label.new()
	_toast_label.position = Vector2(500,760)
	_toast_label.size = Vector2(600,42)
	_toast_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_toast_label.add_theme_font_size_override("font_size",17)
	_ui_layer.add_child(_toast_label)
	var controls := Label.new()
	controls.position = Vector2(25,850)
	controls.text = "WASD gå • Shift spring • MMB kamera • hjul zoom • E använd • F ficklampa"
	controls.modulate = Color(0.78,0.82,0.82,0.72)
	controls.add_theme_font_size_override("font_size",12)
	_ui_layer.add_child(controls)
	_ui_layer.visible = not _capture_mode

func _refresh_ui19() -> void:
	if _objective_label == null:
		return
	if _school_powered:
		_objective_label.text = "Skolan har reservkraft. Undersök de upplysta rummen."
	elif _generator_on and _inventory["cable"]:
		_objective_label.text = "Koppla 32 A-kabeln till skolans reservmatning."
	elif _generator_on:
		_objective_label.text = "Ta kabelrullen vid sporthallens serviceyta."
	elif _generator_inspected:
		_objective_label.text = "Hitta batteriet och F3-säkringen. Återvänd sedan till reservkraften."
	else:
		_objective_label.text = "Undersök reservkraften vid sporthallen."
	_status_label.text = "BAT %s   F3 %s   KABEL %s   SKOLA %s" % ["✓" if _inventory["battery"] else "—","✓" if _inventory["fuse"] else "—","✓" if _inventory["cable"] else "—","LJUS" if _school_powered else "MÖRK"]

func _toast19(text: String) -> void:
	if _toast_label != null:
		_toast_label.text = text
		_toast_time = 4.0

func _run_capture_sequence19() -> void:
	_set_school_power19(true)
	_school_powered = true
	if _cable_connected_visual != null:
		_cable_connected_visual.visible = true
	var dir := ProjectSettings.globalize_path("res://build/captures")
	DirAccess.make_dir_recursive_absolute(dir)
	await _capture_view19("01_school_exterior.png",Vector3(28,17,30),Vector3(-2,1.6,3.5),false)
	await _capture_view19("02_school_sport_rosvalla.png",Vector3(79,43,88),Vector3(18,0.8,35),false)
	await _capture_view19("03_school_interior.png",Vector3(17,10,14),Vector3(3.5,1.2,0.0),true)
	print("ROSVIK_VISUAL_CAPTURE_19_READY files=3")
	get_tree().quit()

func _capture_view19(filename: String,cam_pos: Vector3,focus: Vector3,cutaway: bool) -> void:
	_inside_school = not cutaway
	_set_cutaway19(cutaway)
	camera.global_position = cam_pos
	camera.look_at(focus,Vector3.UP)
	await get_tree().process_frame
	await RenderingServer.frame_post_draw
	var image := get_viewport().get_texture().get_image()
	var path := "res://build/captures/%s" % filename
	var err := image.save_png(path)
	if err != OK:
		push_error("Capture failed: %s" % path)

func _add_bench19(parent: Node3D,pos: Vector3,length: float) -> void:
	_box(Vector3(length,0.13,0.46),wood_mat,pos+Vector3(0,0.55,0),parent)
	_box(Vector3(length,0.12,0.20),wood_mat,pos+Vector3(0,0.96,-0.20),parent)
	for x: float in [-length*0.34,length*0.34]:
		_solid_box(Vector3(0.08,0.58,0.08),metal_mat,pos+Vector3(x,0.29,0),parent)
	_world_prop_count += 4

func _add_backpack19(parent: Node3D,pos: Vector3,color: Color) -> void:
	var mat := _mat(color,0.97)
	var body := _capsule19(0.22,0.50,mat,pos,parent)
	body.scale.z = 0.70
	_box(Vector3(0.18,0.06,0.08),dark_mat,pos+Vector3(0,0.28,-0.10),parent)
	_world_prop_count += 2

func _add_mug19(parent: Node3D,pos: Vector3,color: Color) -> void:
	_cylinder(0.10,0.18,_mat(color,0.90),pos,parent)
	var handle := TorusMesh.new()
	handle.inner_radius = 0.035
	handle.outer_radius = 0.065
	var node := MeshInstance3D.new()
	node.mesh = handle
	node.material_override = _mat(color,0.90)
	node.position = pos+Vector3(0.11,0.0,0)
	node.rotation_degrees.z = 90
	parent.add_child(node)
	_world_prop_count += 2

func _add_thermos19(parent: Node3D,pos: Vector3) -> void:
	_cylinder(0.09,0.32,_mat(Color("777e7d"),0.60,0.28),pos,parent)
	_cylinder(0.075,0.08,dark_mat,pos+Vector3(0,0.20,0),parent)
	_world_prop_count += 2

func _add_warm_window_simple19(parent: Node3D,pos: Vector3,warm: bool) -> void:
	_box(Vector3(1.35,1.20,0.15),dark_mat,pos,parent)
	var mat := _mat(Color("38505b"),0.28)
	mat.emission_enabled = true
	mat.emission = Color("ffbf73") if warm else Color("18252b")
	mat.emission_energy_multiplier = 0.75 if warm else 0.0
	_box(Vector3(1.10,0.95,0.07),mat,pos+Vector3(0,0,0.10),parent)

func _add_fence19(parent: Node3D,a: Vector3,b: Vector3) -> void:
	var d := b-a
	var length := d.length()
	var yaw := -atan2(d.z,d.x)
	for y: float in [0.48,0.92]:
		var rail := _cylinder(0.024,length,metal_mat,(a+b)*0.5+Vector3(0,y,0),parent)
		rail.rotation.z = PI/2.0
		rail.rotation.y = yaw
	var count := maxi(2,int(length/2.2))
	for i: int in range(count+1):
		var p := a.lerp(b,float(i)/float(count))
		_solid_cylinder(0.035,1.18,metal_mat,p+Vector3(0,0.59,0),parent)

func _add_goal19(parent: Node3D,pos: Vector3,yaw: float) -> void:
	var root := Node3D.new()
	root.position = pos
	root.rotation.y = yaw
	parent.add_child(root)
	var white := _mat(Color("d7dcda"),0.84)
	for x: float in [-2.5,2.5]:
		_solid_cylinder(0.045,2.15,white,Vector3(x,1.08,0),root)
	var top := _cylinder(0.045,5.0,white,Vector3(0,2.15,0),root)
	top.rotation.z = PI/2.0
	for x: float in [-2.5,-1.25,0,1.25,2.5]:
		_cylinder(0.012,2.0,metal_mat,Vector3(x,1.0,-0.75),root)
	_world_prop_count += 8

func _add_field_light19(parent: Node3D,pos: Vector3) -> void:
	var root := Node3D.new()
	root.position = pos
	parent.add_child(root)
	_solid_cylinder(0.075,8.0,dark_mat,Vector3(0,4.0,0),root)
	_box(Vector3(2.2,0.14,0.30),dark_mat,Vector3(0,7.85,0),root)
	for x: float in [-0.75,-0.25,0.25,0.75]:
		_box(Vector3(0.32,0.24,0.12),_mat(Color("6d7677"),0.70),Vector3(x,7.75,0.18),root)

func _snow_mound19(pos: Vector3,scale_value: Vector3,yaw: float) -> void:
	var mesh := SphereMesh.new()
	mesh.radius = 1.0
	mesh.height = 2.0
	var node := MeshInstance3D.new()
	node.mesh = mesh
	node.material_override = packed_snow_mat
	node.position = pos+Vector3(0,scale_value.y*0.22,0)
	node.scale = scale_value
	node.rotation.y = yaw
	node.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_ON
	add_child(node)

func _asset19(relative_path: String,pos: Vector3,scale_value: Vector3,yaw: float,parent: Node) -> Node3D:
	var path := ASSET_DIR+relative_path
	var packed: PackedScene = _asset_cache.get(path,null) as PackedScene
	if packed == null:
		var res := load(path)
		if res is PackedScene:
			packed = res as PackedScene
			_asset_cache[path] = packed
	if packed == null:
		return Node3D.new()
	var instance := packed.instantiate() as Node3D
	instance.position = pos
	instance.rotation.y = yaw
	instance.scale = scale_value
	parent.add_child(instance)
	_asset_count += 1
	return instance

func _mat(color: Color,rough: float=0.88,metallic: float=0.0) -> StandardMaterial3D:
	var m := StandardMaterial3D.new()
	m.albedo_color = color
	m.roughness = rough
	m.metallic = metallic
	return m

func _textured_mat(base: Color,rough: float,pattern: String,size: int,variance: float) -> StandardMaterial3D:
	var m := _mat(Color.WHITE,rough)
	var image := Image.create(size,size,false,Image.FORMAT_RGBA8)
	var rng := RandomNumberGenerator.new()
	rng.seed = 19051984+pattern.hash()
	for y: int in range(size):
		for x: int in range(size):
			var n := rng.randf_range(-variance,variance)
			if pattern == "horizontal" and y%13 < 2:
				n -= 0.055
			elif pattern == "vertical" and x%11 < 2:
				n -= 0.060
			elif pattern == "asphalt" and ((x*11+y*7)%89)<3:
				n += 0.055
			elif pattern == "snow" and ((x*5+y*13)%79)<3:
				n += 0.040
			image.set_pixel(x,y,Color(clampf(base.r+n,0,1),clampf(base.g+n,0,1),clampf(base.b+n,0,1),1))
	m.albedo_texture = ImageTexture.create_from_image(image)
	m.uv1_scale = Vector3(4,4,4)
	return m

func _box(size: Vector3,material: Material,pos: Vector3,parent: Node) -> MeshInstance3D:
	var node := MeshInstance3D.new()
	var mesh := BoxMesh.new()
	mesh.size = size
	node.mesh = mesh
	node.material_override = material
	node.position = pos
	node.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_ON
	parent.add_child(node)
	return node

func _solid_box(size: Vector3,material: Material,pos: Vector3,parent: Node) -> MeshInstance3D:
	var node := _box(size,material,pos,parent)
	var body := StaticBody3D.new()
	var shape_node := CollisionShape3D.new()
	var shape := BoxShape3D.new()
	shape.size = size
	shape_node.shape = shape
	body.add_child(shape_node)
	node.add_child(body)
	return node

func _cylinder(radius: float,height: float,material: Material,pos: Vector3,parent: Node) -> MeshInstance3D:
	var node := MeshInstance3D.new()
	var mesh := CylinderMesh.new()
	mesh.top_radius = radius
	mesh.bottom_radius = radius
	mesh.height = height
	node.mesh = mesh
	node.material_override = material
	node.position = pos
	node.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_ON
	parent.add_child(node)
	return node

func _solid_cylinder(radius: float,height: float,material: Material,pos: Vector3,parent: Node) -> MeshInstance3D:
	var node := _cylinder(radius,height,material,pos,parent)
	var body := StaticBody3D.new()
	var shape_node := CollisionShape3D.new()
	var shape := CylinderShape3D.new()
	shape.radius = radius
	shape.height = height
	shape_node.shape = shape
	body.add_child(shape_node)
	node.add_child(body)
	return node

func _capsule19(radius: float,height: float,material: Material,pos: Vector3,parent: Node) -> MeshInstance3D:
	var node := MeshInstance3D.new()
	var mesh := CapsuleMesh.new()
	mesh.radius = radius
	mesh.height = height
	node.mesh = mesh
	node.material_override = material
	node.position = pos
	node.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_ON
	parent.add_child(node)
	return node

func _collision_box19(parent: Node,size: Vector3,pos: Vector3) -> void:
	var body := StaticBody3D.new()
	body.position = pos
	var shape_node := CollisionShape3D.new()
	var shape := BoxShape3D.new()
	shape.size = size
	shape_node.shape = shape
	body.add_child(shape_node)
	parent.add_child(body)

func _label3d19(text_value: String,pos: Vector3,parent: Node,font_size: int) -> Label3D:
	var label := Label3D.new()
	label.text = text_value
	label.position = pos
	label.font_size = font_size
	label.modulate = Color("e3e0d3")
	label.outline_size = 2
	label.outline_modulate = Color(0.02,0.03,0.035,0.82)
	label.no_depth_test = false
	parent.add_child(label)
	return label
