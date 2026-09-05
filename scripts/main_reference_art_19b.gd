extends "res://scripts/main_hero_rebuild_19.gd"

# HERO 19B — REFERENCE ART PASS
# The reference image is treated as the composition/finish target: tighter
# isometric framing, cold blue winter, warm human islands, softer snow shapes,
# dense lived-in details and no exposed demo geometry.

var _front_cutaway19b: Array[GeometryInstance3D] = []
var _window_spill19b: Array[OmniLight3D] = []
var _ortho_size19b := 18.5
var _art_root19b: Node3D

func _ready() -> void:
	super._ready()
	_register_complete_front_cutaway19b()
	_decorate_school_exterior19b()
	_decorate_sporthall19b()
	_decorate_village_context19b()
	if camera != null:
		camera.projection = Camera3D.PROJECTION_ORTHOGONAL
		camera.size = _ortho_size19b
		camera.fov = 36.0
	_camera_distance = 27.0
	_camera_pitch = 0.62
	_camera_yaw = 0.80
	print("ROSVIK_REFERENCE_ART_PASS_19_READY")
	print("ROSVIK_ORTHO_DIORAMA_19_READY")
	print("ROSVIK_COZY_APOCALYPSE_19_READY")
	print("ROSVIK_COMPLETE_CUTAWAY_19_READY count=",_front_cutaway19b.size())

# -----------------------------------------------------------------------------
# ART DIRECTION
# -----------------------------------------------------------------------------
func _build_materials19() -> void:
	snow_mat = _textured_mat(Color("aebfc8"),0.995,"snow",96,0.026)
	packed_snow_mat = _textured_mat(Color("9eafb6"),0.995,"snow",96,0.022)
	dirty_snow_mat = _textured_mat(Color("6f7d81"),0.995,"noise",96,0.034)
	asphalt_mat = _textured_mat(Color("222d33"),0.985,"asphalt",96,0.027)
	path_mat = _textured_mat(Color("59676b"),0.985,"noise",96,0.020)
	school_wall_mat = _textured_mat(Color("939b98"),0.95,"horizontal",96,0.021)
	school_trim_mat = _mat(Color("cbc7b9"),0.94)
	sport_wall_mat = _textured_mat(Color("485b64"),0.90,"vertical",96,0.024)
	roof_mat = _textured_mat(Color("27343a"),0.92,"noise",96,0.023)
	dark_mat = _mat(Color("172229"),0.95)
	metal_mat = _mat(Color("4c5b61"),0.72,0.20)
	wood_mat = _textured_mat(Color("70523d"),0.97,"horizontal",72,0.025)
	concrete_mat = _textured_mat(Color("69787b"),0.985,"noise",96,0.023)
	glass_cold_mat = _mat(Color("263e49"),0.24,0.03)
	glass_cold_mat.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	glass_cold_mat.albedo_color.a = 0.90
	# The football surface is winter-muted and partly snow covered rather than a
	# giant grey game-board rectangle.
	field_mat = _textured_mat(Color("63736c"),0.985,"noise",96,0.017)

func _build_environment19() -> void:
	var env_node := WorldEnvironment.new()
	var env := Environment.new()
	var sky_mat := ProceduralSkyMaterial.new()
	sky_mat.sky_top_color = Color("102733")
	sky_mat.sky_horizon_color = Color("435e69")
	sky_mat.ground_bottom_color = Color("172b34")
	sky_mat.ground_horizon_color = Color("60747b")
	sky_mat.sun_angle_max = 11.0
	var sky := Sky.new()
	sky.sky_material = sky_mat
	env.background_mode = Environment.BG_SKY
	env.sky = sky
	env.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	env.ambient_light_color = Color("526d7b")
	env.ambient_light_energy = 0.29
	env.fog_enabled = true
	env.fog_light_color = Color("4d6671")
	env.fog_density = 0.0085
	env.fog_height = 0.8
	env.fog_height_density = 0.065
	env.tonemap_mode = Environment.TONE_MAPPER_FILMIC
	env.tonemap_exposure = 0.74
	env_node.environment = env
	add_child(env_node)
	var sun := DirectionalLight3D.new()
	sun.rotation_degrees = Vector3(-24.0,-42.0,0.0)
	sun.light_color = Color("e7b98f")
	sun.light_energy = 0.54
	sun.shadow_enabled = true
	sun.directional_shadow_max_distance = 165.0
	add_child(sun)

func _build_ground19() -> void:
	super._build_ground19()
	# Broad soft variation makes the snow feel sculpted instead of a flat editor plane.
	for spec: Dictionary in [
		{"p":Vector3(-24,0,27),"s":Vector3(9.0,0.14,4.0),"r":0.16},
		{"p":Vector3(37,0,13),"s":Vector3(7.0,0.12,3.0),"r":-0.10},
		{"p":Vector3(54,0,41),"s":Vector3(10.0,0.12,3.6),"r":0.08},
		{"p":Vector3(-25,0,75),"s":Vector3(12.0,0.13,4.3),"r":-0.12},
		{"p":Vector3(88,0,56),"s":Vector3(10.0,0.13,4.0),"r":0.10}
	]:
		_snow_mound19(spec["p"],spec["s"],float(spec["r"]))

func _build_roads19() -> void:
	super._build_roads19()
	var dressing := Node3D.new()
	dressing.name = "RoadWinterBlend19B"
	add_child(dressing)
	# Wheel grooves and dirty plough seams soften transitions between asphalt and snow.
	for x: float in [-0.62,0.62]:
		_box(Vector3(0.16,0.012,41.0),dirty_snow_mat,Vector3(67.0+x,0.052,14.5),dressing).rotation.y = PI/2.0
	for p: Vector3 in [Vector3(-18,0,16.9),Vector3(10,0,16.9),Vector3(36,0,30.0),Vector3(63,0,32.7),Vector3(82.8,0,47),Vector3(82.7,0,71)]:
		_snow_mound19(p,Vector3(3.3,0.26,0.75),0.0)

# -----------------------------------------------------------------------------
# SCHOOL — FINISH THE EXTERIOR AND MAKE CUTAWAY ACTUALLY READ AS CUTAWAY
# -----------------------------------------------------------------------------
func _register_complete_front_cutaway19b() -> void:
	if _school_root == null:
		return
	_collect_front_geometry19b(_school_root)
	for node: GeometryInstance3D in _front_cutaway19b:
		if not _school_cutaway.has(node):
			_school_cutaway.append(node)

func _collect_front_geometry19b(node: Node) -> void:
	for child: Node in node.get_children():
		if child is GeometryInstance3D:
			var geo := child as GeometryInstance3D
			var p := geo.global_position
			if p.x > -18.0 and p.x < 18.0 and p.z > 5.78 and p.z < 8.6 and p.y > 0.15:
				_front_cutaway19b.append(geo)
		_collect_front_geometry19b(child)

func _decorate_school_exterior19b() -> void:
	if _school_root == null:
		return
	_art_root19b = Node3D.new()
	_art_root19b.name = "ReferenceArt19B"
	_school_root.add_child(_art_root19b)
	var blue := _mat(Color("334f5e"),0.88,0.05)
	var warm_wood := _textured_mat(Color("8a6246"),0.96,"horizontal",64,0.018)
	var snow_edge := _mat(Color("c0ccd0"),0.995)

	# Deep facade bands / window bays remove the single-box silhouette.
	for x: float in [-15.7,-8.0,-2.3,5.5,13.4]:
		_box(Vector3(0.13,3.25,0.26),school_trim_mat,Vector3(x,1.83,6.39),_art_root19b)
	# A blue school identity band echoing the real-world municipal public-building language.
	_box(Vector3(10.6,0.32,0.10),blue,Vector3(7.0,3.95,6.42),_art_root19b)
	# Roof snow is handled as a continuous layer; repeated scallop blobs were removed in the vertical-slice art pass.
	# Entrance timber / tactile detail.
	for x: float in [-6.8,-5.95,-5.10,-4.25,-3.4]:
		_box(Vector3(0.09,2.38,0.12),warm_wood,Vector3(x,1.43,6.52),_art_root19b)

	# Warm window spill paints the snow when the generator wakes the school.
	for x: float in [-11.8,2.0,7.6]:
		var spill := OmniLight3D.new()
		spill.position = Vector3(x,1.55,7.45)
		spill.light_color = Color("ffb867")
		spill.light_energy = 0.0
		spill.omni_range = 5.4
		spill.shadow_enabled = true
		_school_root.add_child(spill)
		_window_spill19b.append(spill)
		_school_lights.append(spill)

	# Grounded everyday objects: one shovel, one grit bin, a kick-sled and a small wood pile.
	_add_grit_bin19b(Vector3(13.9,0,8.2))
	_add_shovel19b(Vector3(14.8,0,8.0))
	_add_kicksled19b(Vector3(-12.5,0,9.0))
	_add_wood_stack19b(Vector3(-15.0,0,8.3))
	# Snow against the foundation and entrance edges.
	for p: Vector3 in [Vector3(-15,0,6.55),Vector3(-10,0,6.62),Vector3(11,0,6.62),Vector3(15.5,0,6.55)]:
		_snow_mound19(p,Vector3(1.9,0.20,0.42),0.0)

func _set_school_power19(on: bool) -> void:
	for light: Light3D in _school_lights:
		light.light_energy = 1.75 if on else 0.0
	for i: int in range(_school_windows.size()):
		var m := _school_windows[i].material_override as StandardMaterial3D
		m.emission_energy_multiplier = 2.25 if on and i%3 != 0 else 0.0
	for spill: OmniLight3D in _window_spill19b:
		spill.light_energy = 2.1 if on else 0.0

# -----------------------------------------------------------------------------
# SPORT HALL — SOLID BUT MORE LIKE A FINISHED PUBLIC BUILDING
# -----------------------------------------------------------------------------
func _decorate_sporthall19b() -> void:
	var root := get_node_or_null("RosvikSporthall") as Node3D
	if root == null:
		return
	var band := _mat(Color("2f5265"),0.86,0.06)
	var pale := _mat(Color("9aa5a4"),0.94)
	_box(Vector3(22.0,0.40,0.10),band,Vector3(2.0,5.42,9.22),root)
	# Ventilation / service details are attached to the building, never floating in front.
	for x: float in [-8.0,0.0,8.0]:
		_box(Vector3(1.45,0.65,0.22),pale,Vector3(x,4.15,9.20),root)
	# Snow curls into the wall base and service side.
	for p: Vector3 in [Vector3(-11.5,0,9.7),Vector3(4.5,0,9.7),Vector3(11.5,0,9.7),Vector3(13.9,0,5.8)]:
		var global_p := root.to_global(p)
		_snow_mound19(global_p,Vector3(2.2,0.24,0.55),0.0)
	# A sheltered bench and a single bin create human scale at the entrance.
	_add_bench19(root,Vector3(-8.7,0,11.3),2.2)
	_add_grit_bin19b(root.to_global(Vector3(-0.5,0,11.6)))

# -----------------------------------------------------------------------------
# ROSVALLA — WINTER FOOTBALL GROUND, ONE PLACE, NOT A BOARD OF MINI PITCHES
# -----------------------------------------------------------------------------
func _build_rosvalla19() -> void:
	var root := Node3D.new()
	root.name = "Rosvalla19"
	add_child(root)
	var center := Vector3(9.0,0.0,53.0)
	# Grass survives in subtle muted bands beneath packed snow.
	_box(Vector3(64.0,0.032,38.0),field_mat,center+Vector3(0,0.016,0),root)
	for z: float in [-14.0,-7.0,0.0,7.0,14.0]:
		_box(Vector3(61.0,0.010,4.2),packed_snow_mat,center+Vector3(0,0.040,z),root)
	# Only pieces of line remain visible through the snow.
	var line := _mat(Color("c8cbc2"),0.98)
	for x: float in [-24,-12,0,12,24]:
		_box(Vector3(7.0,0.012,0.075),line,center+Vector3(x,0.052,-17.6),root)
		_box(Vector3(7.0,0.012,0.075),line,center+Vector3(x,0.052,17.6),root)
	for z: float in [-13,-5,5,13]:
		_box(Vector3(0.075,0.012,5.0),line,center+Vector3(-30.6,0.052,z),root)
		_box(Vector3(0.075,0.012,5.0),line,center+Vector3(30.6,0.052,z),root)
	_add_goal19(root,center+Vector3(-31.7,0,0),PI/2.0)
	_add_goal19(root,center+Vector3(31.7,0,0),-PI/2.0)
	_add_fence19(root,center+Vector3(-32.8,0,-20.0),center+Vector3(32.8,0,-20.0))
	_add_fence19(root,center+Vector3(-32.8,0,20.0),center+Vector3(18.0,0,20.0))
	_add_fence19(root,center+Vector3(24.0,0,20.0),center+Vector3(32.8,0,20.0))
	for p: Vector3 in [center+Vector3(-27,0,-20.8),center+Vector3(27,0,-20.8),center+Vector3(-27,0,20.8),center+Vector3(27,0,20.8)]:
		_add_field_light19(root,p)
	# Player benches / simple dugout language, positioned on one touchline.
	_add_dugout19b(root,center+Vector3(-8,0,-21.2),0.0)
	_add_dugout19b(root,center+Vector3(8,0,-21.2),0.0)
	# Plough/wind accumulation makes it read as an outdoor winter ground.
	for p: Vector3 in [center+Vector3(-29,0,-17),center+Vector3(28,0,16),center+Vector3(-30,0,15),center+Vector3(21,0,-18)]:
		_snow_mound19(p,Vector3(3.4,0.31,1.05),0.0)
	_world_prop_count += 90

# -----------------------------------------------------------------------------
# VILLAGE CONTEXT — THE HERO ZONE SHOULD NOT FLOAT IN AN EMPTY TEST PLANE
# -----------------------------------------------------------------------------
func _decorate_village_context19b() -> void:
	var root := Node3D.new()
	root.name = "VillageEdge19B"
	add_child(root)
	var homes: Array[Dictionary] = [
		{"p":Vector3(-63,0,-8),"c":Color("7f5548"),"r":0.06,"warm":true},
		{"p":Vector3(-68,0,11),"c":Color("8b7c58"),"r":-0.08,"warm":false},
		{"p":Vector3(-64,0,31),"c":Color("5e7375"),"r":0.05,"warm":true},
		{"p":Vector3(96,0,-5),"c":Color("756356"),"r":0.10,"warm":false},
		{"p":Vector3(101,0,15),"c":Color("6d7865"),"r":-0.08,"warm":true},
		{"p":Vector3(103,0,36),"c":Color("80604f"),"r":0.06,"warm":false}
	]
	for home: Dictionary in homes:
		_add_house19b(root,home["p"],home["c"],float(home["r"]),bool(home["warm"]))
	# A few timber sheds, bins and snow piles break up the village edge.
	for p: Vector3 in [Vector3(-58,0,-1),Vector3(-60,0,20),Vector3(91,0,6),Vector3(96,0,27)]:
		_add_shed19b(root,p)

# -----------------------------------------------------------------------------
# CAMERA — ORTHOGRAPHIC DIORAMA, DIRECT ROTATION, SEPARATE FROM MOVEMENT
# -----------------------------------------------------------------------------
func _unhandled_input(event: InputEvent) -> void:
	if _capture_mode:
		return
	if event is InputEventMouseButton:
		var mb := event as InputEventMouseButton
		if mb.button_index == MOUSE_BUTTON_MIDDLE:
			_camera_dragging = mb.pressed
			get_viewport().set_input_as_handled()
		elif mb.button_index == MOUSE_BUTTON_WHEEL_UP and mb.pressed:
			_ortho_size19b = clampf(_ortho_size19b-1.1,11.5,27.0)
			get_viewport().set_input_as_handled()
		elif mb.button_index == MOUSE_BUTTON_WHEEL_DOWN and mb.pressed:
			_ortho_size19b = clampf(_ortho_size19b+1.1,11.5,27.0)
			get_viewport().set_input_as_handled()
	elif event is InputEventMouseMotion and _camera_dragging:
		var mm := event as InputEventMouseMotion
		_camera_yaw -= mm.relative.x*0.0060
		_camera_pitch = clampf(_camera_pitch-mm.relative.y*0.0040,0.42,0.76)
		get_viewport().set_input_as_handled()

func _update_camera19(delta: float) -> void:
	if player == null or camera == null:
		return
	camera.projection = Camera3D.PROJECTION_ORTHOGONAL
	var p := player.global_position
	var inside := p.x > -16.6 and p.x < 16.6 and p.z > -5.7 and p.z < 5.85
	var wanted_size := minf(_ortho_size19b,14.5) if inside else _ortho_size19b
	camera.size = lerpf(camera.size,wanted_size,1.0-exp(-5.0*delta))
	var focus := p+Vector3(0,1.0,0)
	var travel := Vector3(player.velocity.x,0,player.velocity.z)
	if travel.length() > 0.2:
		focus += travel.normalized()*0.38
	var horizontal := cos(_camera_pitch)*_camera_distance
	var offset := Vector3(sin(_camera_yaw)*horizontal,sin(_camera_pitch)*_camera_distance,cos(_camera_yaw)*horizontal)
	var wanted := focus+offset
	camera.global_position = camera.global_position.lerp(wanted,1.0-exp(-7.0*delta))
	camera.look_at(focus,Vector3.UP)

# -----------------------------------------------------------------------------
# VISUAL ACCEPTANCE CAPTURES — CLOSE ENOUGH TO JUDGE THE ACTUAL ART
# -----------------------------------------------------------------------------
func _run_capture_sequence19() -> void:
	_set_school_power19(true)
	_school_powered = true
	if _cable_connected_visual != null:
		_cable_connected_visual.visible = true
	var dir := ProjectSettings.globalize_path("res://build/captures")
	DirAccess.make_dir_recursive_absolute(dir)
	camera.projection = Camera3D.PROJECTION_ORTHOGONAL
	camera.size = 21.0
	await _capture_view19("01_school_exterior.png",Vector3(25,18,27),Vector3(-1,1.55,5.0),false)
	camera.size = 36.0
	await _capture_view19("02_school_sport_rosvalla.png",Vector3(68,44,82),Vector3(20,0.8,37),false)
	camera.size = 15.0
	await _capture_view19("03_school_interior.png",Vector3(14,12,17),Vector3(5.5,1.0,-0.7),true)
	print("ROSVIK_VISUAL_CAPTURE_19_READY files=3")
	get_tree().quit()

# -----------------------------------------------------------------------------
# SMALL ART HELPERS
# -----------------------------------------------------------------------------
func _sphere19b(radius: float,material: Material,pos: Vector3,parent: Node) -> MeshInstance3D:
	var mesh := SphereMesh.new()
	mesh.radius = radius
	mesh.height = radius*2.0
	var node := MeshInstance3D.new()
	node.mesh = mesh
	node.material_override = material
	node.position = pos
	node.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_ON
	parent.add_child(node)
	return node

func _add_grit_bin19b(global_pos: Vector3) -> void:
	var root := Node3D.new()
	root.position = global_pos
	add_child(root)
	var green := _mat(Color("425b52"),0.96)
	var body := _capsule19(0.37,0.62,green,Vector3(0,0.32,0),root)
	body.scale = Vector3(1.15,0.85,0.95)
	_box(Vector3(0.76,0.09,0.62),dark_mat,Vector3(0,0.64,0),root)

func _add_shovel19b(global_pos: Vector3) -> void:
	var root := Node3D.new()
	root.position = global_pos
	root.rotation.z = -0.22
	add_child(root)
	_cylinder(0.025,1.55,wood_mat,Vector3(0,0.78,0),root)
	_box(Vector3(0.48,0.10,0.38),_mat(Color("a05939"),0.88),Vector3(0,0.09,0),root)

func _add_kicksled19b(global_pos: Vector3) -> void:
	var root := Node3D.new()
	root.position = global_pos
	root.rotation.y = 0.35
	add_child(root)
	var red := _mat(Color("85483e"),0.90)
	for z: float in [-0.27,0.27]:
		var rail := _cylinder(0.022,1.45,metal_mat,Vector3(0,0.10,z),root)
		rail.rotation.z = PI/2.0
	_box(Vector3(0.70,0.08,0.55),red,Vector3(-0.10,0.28,0),root)
	for x: float in [0.2,0.52]:
		_solid_cylinder(0.025,1.18,metal_mat,Vector3(x,0.69,0),root)
	var handle := _cylinder(0.025,0.75,metal_mat,Vector3(0.52,1.25,0),root)
	handle.rotation.x = PI/2.0

func _add_wood_stack19b(global_pos: Vector3) -> void:
	var root := Node3D.new()
	root.position = global_pos
	add_child(root)
	for row: int in range(3):
		for col: int in range(5):
			var log := _cylinder(0.10,0.72,wood_mat,Vector3(float(col)*0.23,float(row)*0.19+0.10,0),root)
			log.rotation.z = PI/2.0

func _add_dugout19b(parent: Node3D,pos: Vector3,yaw: float) -> void:
	var root := Node3D.new()
	root.position = pos
	root.rotation.y = yaw
	parent.add_child(root)
	var frame := _mat(Color("4d5b60"),0.78,0.18)
	var glass := _mat(Color(0.25,0.38,0.43,0.55),0.22)
	glass.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	_box(Vector3(6.0,0.12,1.8),roof_mat,Vector3(0,2.05,0),root)
	_box(Vector3(6.0,1.75,0.08),glass,Vector3(0,1.08,0.85),root)
	for x: float in [-2.8,0,2.8]:
		_solid_box(Vector3(0.08,2.0,0.08),frame,Vector3(x,1.0,0.82),root)
	_add_bench19(root,Vector3(0,0,-0.15),4.8)

func _add_house19b(parent: Node3D,pos: Vector3,color: Color,yaw: float,warm: bool) -> void:
	var root := Node3D.new()
	root.position = pos
	root.rotation.y = yaw
	parent.add_child(root)
	var wall := _textured_mat(color,0.96,"horizontal",64,0.022)
	_solid_box(Vector3(9.2,3.0,6.5),wall,Vector3(0,1.5,0),root)
	var rl := _box(Vector3(9.8,0.28,3.9),roof_mat,Vector3(0,3.42,-1.55),root)
	rl.rotation.x = -0.27
	var rr := _box(Vector3(9.8,0.28,3.9),roof_mat,Vector3(0,3.42,1.55),root)
	rr.rotation.x = 0.27
	var sl := _box(Vector3(9.5,0.10,3.65),packed_snow_mat,Vector3(0,3.59,-1.50),root)
	sl.rotation.x = -0.27
	var sr := _box(Vector3(9.5,0.10,3.65),packed_snow_mat,Vector3(0,3.59,1.50),root)
	sr.rotation.x = 0.27
	for x: float in [-2.4,0.5]:
		_add_warm_window_simple19(root,Vector3(x,1.65,3.31),warm and x > 0.0)
	_box(Vector3(1.0,2.0,0.12),dark_mat,Vector3(3.1,1.0,3.32),root)
	_snow_mound19(root.to_global(Vector3(-2.5,0,3.5)),Vector3(1.8,0.20,0.45),0.0)

func _add_shed19b(parent: Node3D,pos: Vector3) -> void:
	var root := Node3D.new()
	root.position = pos
	parent.add_child(root)
	var wall := _textured_mat(Color("67584a"),0.98,"horizontal",64,0.020)
	_solid_box(Vector3(3.8,2.2,3.2),wall,Vector3(0,1.1,0),root)
	_box(Vector3(4.2,0.25,3.6),roof_mat,Vector3(0,2.32,0),root)
	_box(Vector3(4.0,0.09,3.4),packed_snow_mat,Vector3(0,2.49,0),root)
