extends Node3D

const PLAYER_SCRIPT := preload("res://scripts/player.gd")

var player: CharacterBody3D
var camera: Camera3D
var target_camera_pos := Vector3.ZERO
var road_mat: StandardMaterial3D
var snow_mat: StandardMaterial3D
var dark_mat: StandardMaterial3D
var school_mat: StandardMaterial3D
var arena_mat: StandardMaterial3D
var glass_mat: StandardMaterial3D
var metal_mat: StandardMaterial3D
var wood_mat: StandardMaterial3D
var warm_mat: StandardMaterial3D
var time_of_day := 16.55
var warm_windows: Array[MeshInstance3D] = []
var lamps: Array[OmniLight3D] = []

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

func _process(delta: float) -> void:
	time_of_day = fmod(time_of_day + delta * 0.002, 24.0)
	_update_camera(delta)
	_update_world(delta)

func _build_materials() -> void:
	snow_mat = _mat(Color("dbe4e9"), 0.98)
	road_mat = _mat(Color("41484d"), 0.96)
	dark_mat = _mat(Color("20282e"), 0.9)
	school_mat = _mat(Color("aeb7bb"), 0.9)
	arena_mat = _mat(Color("687177"), 0.76, 0.12)
	glass_mat = _mat(Color("6f8798"), 0.2, 0.05)
	metal_mat = _mat(Color("5b666d"), 0.62, 0.35)
	wood_mat = _mat(Color("79543d"), 0.95)
	warm_mat = _mat(Color("f0b067"), 0.35)
	warm_mat.emission_enabled = true
	warm_mat.emission = Color("ff8f3b")
	warm_mat.emission_energy_multiplier = 0.0

func _build_environment() -> void:
	var env := WorldEnvironment.new()
	var e := Environment.new()
	e.background_mode = Environment.BG_COLOR
	e.background_color = Color("657582")
	e.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	e.ambient_light_color = Color("9fb3c0")
	e.ambient_light_energy = 0.65
	e.fog_enabled = true
	e.fog_light_color = Color("7e8c96")
	e.fog_density = 0.009
	e.fog_height = 1.5
	e.fog_height_density = 0.18
	e.tonemap_mode = Environment.TONE_MAPPER_FILMIC
	env.environment = e
	add_child(env)
	var sun := DirectionalLight3D.new()
	sun.rotation_degrees = Vector3(-48, -35, 0)
	sun.light_color = Color("ffd2ae")
	sun.light_energy = 1.45
	sun.shadow_enabled = true
	sun.directional_shadow_max_distance = 120
	add_child(sun)

func _build_ground() -> void:
	_box(Vector3(220, 0.35, 180), snow_mat, Vector3(0, -0.18, 5), self)
	# Slight uneven snow plates break up the 'blank white plane' look.
	for p in [Vector3(-42,0.02,-18), Vector3(35,0.02,25), Vector3(-5,0.02,48), Vector3(55,0.02,-28)]:
		var patch := _box(Vector3(26,0.06,16), _mat(Color("e4ebef"),0.99), p, self)
		patch.rotation.y = randf_range(-0.18,0.18)

func _build_roads() -> void:
	_add_road(Vector3(-90,0,-27), Vector3(92,0,-27), 7.2)
	_add_road(Vector3(-38,0,-80), Vector3(-38,0,72), 6.2)
	_add_road(Vector3(-38,0,12), Vector3(48,0,12), 5.4)
	_add_road(Vector3(48,0,12), Vector3(48,0,62), 5.0)
	# Ditches and snow shoulders.
	for z in [-31.2,-22.8]:
		_box(Vector3(182,0.32,1.35), _mat(Color("e6edf1"),0.99), Vector3(1,0.12,z), self)
	for x in [-42.0,-34.0]:
		_box(Vector3(1.3,0.30,152), _mat(Color("e7eef2"),0.99), Vector3(x,0.12,-4), self)

func _add_road(a: Vector3, b: Vector3, width: float) -> void:
	var d := b - a
	var length := d.length()
	var road := _box(Vector3(length,0.08,width), road_mat, (a+b)*0.5 + Vector3(0,0.02,0), self)
	road.rotation.y = -atan2(d.z,d.x)
	if width >= 5.5:
		var count := int(length / 6.0)
		for i in range(count):
			var t := (float(i)+0.5)/float(count)
			var p := a.lerp(b,t)
			var dash := _box(Vector3(2.3,0.015,0.13), _mat(Color("e7e9e9"),0.85), p+Vector3(0,0.075,0), self)
			dash.rotation.y = road.rotation.y

func _build_school() -> void:
	var root := Node3D.new()
	root.name = "RosviksSkola"
	root.position = Vector3(6,0,1)
	add_child(root)
	_box(Vector3(34,4.5,14), school_mat, Vector3(0,2.25,0), root)
	_box(Vector3(36,0.5,16), _mat(Color("4e5b63"),0.88), Vector3(0,4.75,0), root)
	# Roof snow cap with mechanical equipment.
	_box(Vector3(35.5,0.20,15.5), snow_mat, Vector3(0,5.08,0), root)
	for x in [-6.0,0.0,6.0]:
		_box(Vector3(2.3,0.8,1.5), metal_mat, Vector3(x,5.55,-1.2), root)
	# Front facade with actual rhythm.
	for i in range(12):
		var x := -15.4 + i*2.75
		var w := _box(Vector3(1.55,1.25,0.12), glass_mat.duplicate(), Vector3(x,2.2,7.08), root)
		warm_windows.append(w)
		_box(Vector3(0.06,1.25,0.14), dark_mat, Vector3(x,2.2,7.16), root)
	# Main entrance and canopy.
	_box(Vector3(3.1,2.45,0.18), glass_mat, Vector3(-5.8,1.25,7.16), root)
	_box(Vector3(5.0,0.28,2.2), dark_mat, Vector3(-5.8,2.8,8.0), root)
	_box(Vector3(7.2,0.08,9.0), _mat(Color("c6cfd4"),0.92), Vector3(-5.8,0.05,11.0), root)
	# Schoolyard fence, bike racks, benches and court objects.
	_add_fence(Vector3(-14,0,13), Vector3(14,0,13), root)
	_add_fence(Vector3(-14,0,13), Vector3(-14,0,30), root)
	_add_fence(Vector3(14,0,13), Vector3(14,0,30), root)
	_add_fence(Vector3(-14,0,30), Vector3(14,0,30), root)
	for i in range(7):
		var rack := _box(Vector3(0.08,0.55,1.5), metal_mat, Vector3(-3+i*0.55,0.28,10.4), root)
		rack.rotation.z = 0.16
	_add_bench(Vector3(-9,0,18),root)
	_add_bench(Vector3(9,0,18),root)
	_add_goal(Vector3(-7,0,24),root)
	_add_basket(Vector3(7,0,23),root)
	_add_flag(Vector3(12,0,10),root)
	_label3d("ROSVIKS SKOLA", Vector3(-5.8,3.9,7.25), root)

func _build_arena() -> void:
	var root := Node3D.new()
	root.name = "NorrbottenStalArena"
	root.position = Vector3(47,0,28)
	add_child(root)
	_box(Vector3(40,7.2,22), arena_mat, Vector3(0,3.6,0), root)
	_box(Vector3(42,0.32,24), _mat(Color("596166"),0.82), Vector3(0,7.3,0), root)
	_box(Vector3(41.5,0.18,23.5), snow_mat, Vector3(0,7.55,0), root)
	# Lower dark band + corrugated ribs.
	_box(Vector3(40.2,1.15,0.18), _mat(Color("2c3a43"),0.78), Vector3(0,0.6,11.12), root)
	for x in range(-19,20):
		if x % 2 == 0:
			_box(Vector3(0.04,5.5,0.10), _mat(Color("7b8489"),0.72,0.12), Vector3(x,3.3,11.18), root)
	# Rink/office window strip.
	for i in range(8):
		var x := -16.0 + i*4.2
		var w := _box(Vector3(2.0,0.95,0.12), glass_mat.duplicate(), Vector3(x,4.65,11.2), root)
		warm_windows.append(w)
	# Entrance, canopy, loading door, sign.
	_box(Vector3(3.4,2.7,0.16), glass_mat, Vector3(-8.0,1.4,11.2), root)
	_box(Vector3(5.2,0.3,2.4), dark_mat, Vector3(-8.0,2.9,12.1), root)
	_box(Vector3(5.2,3.7,0.18), dark_mat, Vector3(6.0,1.9,11.2), root)
	_box(Vector3(11,1.15,0.16), _mat(Color("1c2830"),0.7), Vector3(0,5.9,11.22), root)
	_label3d("NORRBOTTEN STÅL ARENA", Vector3(0,5.92,11.36), root)
	# Parking / forecourt.
	_box(Vector3(50,0.08,18), _mat(Color("c7cfd3"),0.94), Vector3(0,0.04,19), root)
	for i in range(12):
		_box(Vector3(0.11,0.02,2.5), _mat(Color("f0f2f2"),0.82), Vector3(-16+i*3.0,0.09,20), root)
	for x in [-12.0,-4.0,4.0,12.0]:
		_box(Vector3(1.4,0.8,1.1), metal_mat, Vector3(x,7.95,-1.0), root)
	_add_flag(Vector3(-14,0,14.5),root)

func _build_props() -> void:
	# Vehicles are correctly scaled to the player: roughly 4.5m long.
	_add_car(Vector3(-5,0,-6), Color("5e6871"), 0.15, true)
	_add_car(Vector3(30,0,44), Color("6c6259"), -0.35, false)
	_add_car(Vector3(61,0,51), Color("405563"), -1.45, false)
	_add_car(Vector3(-18,0,16), Color("d29843"), 0.4, false)
	# Streetlights and lit cones.
	for p in [Vector3(-26,0,-18),Vector3(-8,0,-18),Vector3(13,0,-18),Vector3(35,0,-18),Vector3(47,0,7),Vector3(47,0,48)]:
		_add_lamp(p)
	# Snow banks and grit patches.
	for p in [Vector3(-15,0,11),Vector3(16,0,13),Vector3(26,0,47),Vector3(66,0,48),Vector3(48,0,57)]:
		var bank := _box(Vector3(randf_range(4,8),randf_range(0.4,0.75),randf_range(1.3,2.2)), _mat(Color("e6edf1"),0.99), p+Vector3(0,0.22,0), self)
		bank.rotation.y = randf_range(-0.18,0.18)
	# Forest edge.
	for i in range(46):
		var angle := randf_range(-0.4,2.9)
		var radius := randf_range(68.0,96.0)
		_add_tree(Vector3(cos(angle)*radius,0,sin(angle)*radius+4))

func _build_player() -> void:
	player = CharacterBody3D.new()
	player.set_script(PLAYER_SCRIPT)
	player.position = Vector3(-8,0,14)
	add_child(player)
	camera = Camera3D.new()
	camera.fov = 42.0
	camera.current = true
	add_child(camera)
	_update_camera(1.0)

func _build_ui() -> void:
	var ui := CanvasLayer.new()
	add_child(ui)
	var panel := ColorRect.new()
	panel.position = Vector2(20,20)
	panel.size = Vector2(360,142)
	panel.color = Color(0.025,0.04,0.055,0.88)
	ui.add_child(panel)
	var label := Label.new()
	label.position = Vector2(18,14)
	label.text = "ROSVIK: BLACKOUT\nNÄTET DOG 02:17\n\nMÅL\nTa dig till reservkraften vid ishallen."
	label.add_theme_font_size_override("font_size",18)
	panel.add_child(label)
	var hint := Label.new()
	hint.position = Vector2(20,850)
	hint.text = "WASD / pilar  gå     Shift  spring"
	hint.add_theme_font_size_override("font_size",16)
	ui.add_child(hint)

func _update_camera(delta: float) -> void:
	if player == null or camera == null:
		return
	var velocity_dir := Vector3(player.velocity.x,0,player.velocity.z)
	if velocity_dir.length() > 0.1:
		velocity_dir = velocity_dir.normalized() * 2.6
	var focus := player.global_position + velocity_dir + Vector3(0,1.1,0)
	target_camera_pos = focus + Vector3(18,15,18)
	camera.global_position = camera.global_position.lerp(target_camera_pos, 1.0-exp(-4.5*delta))
	camera.look_at(focus, Vector3.UP)

func _update_world(_delta: float) -> void:
	var dusk := clamp((time_of_day-15.0)/4.0,0.0,1.0)
	for w in warm_windows:
		if w.material_override is StandardMaterial3D:
			var m := w.material_override as StandardMaterial3D
			m.emission_enabled = true
			m.emission = Color("ff9b4b")
			m.emission_energy_multiplier = lerp(0.0,1.25,dusk)
	for l in lamps:
		l.light_energy = lerp(0.0,2.2,dusk)

func _mat(color: Color, rough := 0.85, metallic := 0.0) -> StandardMaterial3D:
	var m := StandardMaterial3D.new()
	m.albedo_color = color
	m.roughness = rough
	m.metallic = metallic
	return m

func _box(size: Vector3, mat: Material, pos: Vector3, parent: Node) -> MeshInstance3D:
	var n := MeshInstance3D.new()
	var mesh := BoxMesh.new()
	mesh.size = size
	n.mesh = mesh
	n.material_override = mat
	n.position = pos
	n.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_ON
	parent.add_child(n)
	return n

func _cylinder(radius: float, height: float, mat: Material, pos: Vector3, parent: Node) -> MeshInstance3D:
	var n := MeshInstance3D.new()
	var mesh := CylinderMesh.new()
	mesh.top_radius = radius
	mesh.bottom_radius = radius
	mesh.height = height
	n.mesh = mesh
	n.material_override = mat
	n.position = pos
	n.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_ON
	parent.add_child(n)
	return n

func _add_car(pos: Vector3, color: Color, yaw: float, open_door: bool) -> void:
	var root := Node3D.new()
	root.position = pos
	root.rotation.y = yaw
	add_child(root)
	var body_mat := _mat(color,0.7,0.16)
	_box(Vector3(4.55,0.58,1.82),body_mat,Vector3(0,0.52,0),root)
	_box(Vector3(2.35,0.62,1.62),body_mat,Vector3(-0.25,1.1,0),root)
	_box(Vector3(1.55,0.42,1.64),glass_mat,Vector3(-0.25,1.18,0),root)
	for x in [-1.45,1.45]:
		for z in [-0.92,0.92]:
			var wheel := _cylinder(0.34,0.24,_mat(Color("171a1c"),1.0),Vector3(x,0.37,z),root)
			wheel.rotation.x = PI/2
	_box(Vector3(2.0,0.08,1.3),snow_mat,Vector3(-0.25,1.48,0),root)
	if open_door:
		var d := _box(Vector3(1.15,0.95,0.08),body_mat,Vector3(-0.4,1.05,1.15),root)
		d.rotation.y = 0.65

func _add_tree(pos: Vector3) -> void:
	var root := Node3D.new()
	root.position = pos
	add_child(root)
	_cylinder(0.14,1.8,_mat(Color("604838"),1.0),Vector3(0,0.9,0),root)
	for data in [[1.15,2.15,2.0],[0.9,3.0,1.7],[0.65,3.85,1.35]]:
		var mesh := ConeMesh.new()
		mesh.bottom_radius = data[0]
		mesh.top_radius = 0.0
		mesh.height = data[2]
		var n := MeshInstance3D.new()
		n.mesh = mesh
		n.material_override = _mat(Color("294942"),0.96)
		n.position.y = data[1]
		n.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_ON
		root.add_child(n)

func _add_lamp(pos: Vector3) -> void:
	var root := Node3D.new()
	root.position = pos
	add_child(root)
	_cylinder(0.055,4.2,dark_mat,Vector3(0,2.1,0),root)
	_box(Vector3(0.72,0.08,0.11),dark_mat,Vector3(0.3,4.05,0),root)
	var light := OmniLight3D.new()
	light.position = Vector3(0.58,3.95,0)
	light.light_color = Color("ffc071")
	light.omni_range = 8.0
	light.light_energy = 0.0
	light.shadow_enabled = false
	root.add_child(light)
	lamps.append(light)

func _add_fence(a: Vector3,b: Vector3,parent: Node) -> void:
	var d := b-a
	var length := d.length()
	var yaw := -atan2(d.z,d.x)
	for y in [0.55,0.95]:
		var rail := _box(Vector3(length,0.06,0.06),dark_mat,(a+b)*0.5+Vector3(0,y,0),parent)
		rail.rotation.y = yaw
	var count := max(2,int(length/2.0))
	for i in range(count+1):
		var p := a.lerp(b,float(i)/float(count))
		_box(Vector3(0.07,1.25,0.07),dark_mat,p+Vector3(0,0.625,0),parent)

func _add_bench(pos: Vector3,parent: Node) -> void:
	_box(Vector3(2.0,0.12,0.48),wood_mat,pos+Vector3(0,0.58,0),parent)
	_box(Vector3(2.0,0.12,0.22),wood_mat,pos+Vector3(0,1.0,-0.22),parent)
	for x in [-0.7,0.7]:
		_box(Vector3(0.1,0.58,0.1),metal_mat,pos+Vector3(x,0.3,0),parent)

func _add_goal(pos: Vector3,parent: Node) -> void:
	_box(Vector3(2.4,0.08,0.08),_mat(Color("e9edef"),0.8),pos+Vector3(0,1.25,0),parent)
	for x in [-1.2,1.2]:
		_box(Vector3(0.08,1.3,0.08),_mat(Color("e9edef"),0.8),pos+Vector3(x,0.65,0),parent)

func _add_basket(pos: Vector3,parent: Node) -> void:
	_box(Vector3(0.08,3.1,0.08),dark_mat,pos+Vector3(0,1.55,0),parent)
	_box(Vector3(1.05,0.7,0.07),_mat(Color("e8ecee"),0.84),pos+Vector3(0,2.7,0),parent)
	var rim := _cylinder(0.22,0.04,_mat(Color("d5652f"),0.75),pos+Vector3(0,2.42,-0.18),parent)
	rim.rotation.x = PI/2

func _add_flag(pos: Vector3,parent: Node) -> void:
	_cylinder(0.045,5.5,metal_mat,pos+Vector3(0,2.75,0),parent)
	_box(Vector3(1.4,0.65,0.04),_mat(Color("d7a148"),0.88),pos+Vector3(0.72,4.65,0),parent)

func _label3d(text: String,pos: Vector3,parent: Node) -> void:
	var label := Label3D.new()
	label.text = text
	label.font_size = 64
	label.outline_size = 10
	label.modulate = Color("eef3f5")
	label.outline_modulate = Color("17232b")
	label.position = pos
	label.billboard = BaseMaterial3D.BILLBOARD_ENABLED
	parent.add_child(label)
