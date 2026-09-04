extends "res://scripts/main_art_interior_fix.gd"

var _polish_clock: float = 0.0
var _entrance_light: OmniLight3D

func _ready() -> void:
	super._ready()
	_add_school_polish()
	_add_arena_polish()
	_add_exterior_polish()
	print("ROSVIK_POLISH_02_READY")
	print("ROSVIK_INTERIOR_DETAIL_READY")
	print("ROSVIK_ARENA_IDENTITY_READY")

func _process(delta: float) -> void:
	super._process(delta)
	_polish_clock += delta
	if _entrance_light != null:
		_entrance_light.light_energy = 1.45 + sin(_polish_clock * 0.9) * 0.05

func _add_school_polish() -> void:
	var root: Node3D = get_node_or_null("RosviksSkola") as Node3D
	if root == null:
		return

	var rubber_mat: StandardMaterial3D = _textured_mat(Color("30363a"),0.98,0.0,"noise",64,0.035)
	var pale_wall: StandardMaterial3D = _mat(Color("d2d0c6"),0.92)
	var green_mat: StandardMaterial3D = _mat(Color("35634f"),0.78)
	green_mat.emission_enabled = true
	green_mat.emission = Color("4faa73")
	green_mat.emission_energy_multiplier = 0.65
	var red_mat: StandardMaterial3D = _mat(Color("9c2f28"),0.78,0.02)
	var book_blue: StandardMaterial3D = _mat(Color("4b6677"),0.88)
	var book_red: StandardMaterial3D = _mat(Color("8a4a42"),0.88)
	var book_yellow: StandardMaterial3D = _mat(Color("b09055"),0.9)
	var screen_mat: StandardMaterial3D = _mat(Color("25323a"),0.48,0.05)
	screen_mat.emission_enabled = true
	screen_mat.emission = Color("7ea4b8")
	screen_mat.emission_energy_multiplier = 0.28

	# Entrance mat and cleaner transition from snowy exterior to indoor floor.
	_box(Vector3(3.25,0.025,1.65),rubber_mat,Vector3(-4.6,0.095,7.15),root)
	_box(Vector3(2.7,0.022,1.0),rubber_mat,Vector3(-4.6,0.098,5.85),root)

	# Low skirting along the corridor gives the interior walls a finished scale.
	_box(Vector3(0.10,0.16,10.7),dark_mat,Vector3(-5.78,0.15,-0.05),root)
	_box(Vector3(0.10,0.16,1.75),dark_mat,Vector3(-3.42,0.15,0.22),root)
	_box(Vector3(0.10,0.16,3.0),dark_mat,Vector3(-3.42,0.15,-4.65),root)

	# Radiators beneath the interior-facing wall sections.
	for z_value: float in [3.15,0.55,-2.15,-4.45]:
		_box(Vector3(0.12,0.48,1.05),pale_wall,Vector3(-5.68,0.48,z_value),root)
		for rib: int in range(5):
			_box(Vector3(0.035,0.38,0.82),concrete_mat,Vector3(-5.61,0.48,z_value-0.34+float(rib)*0.17),root)

	# Coat rail and hooks next to the foyer.
	_box(Vector3(0.10,0.10,2.25),wood_mat,Vector3(-5.70,1.55,4.15),root)
	for i: int in range(7):
		_cylinder(0.035,0.18,metal_mat,Vector3(-5.58,1.45,3.25+float(i)*0.30),root).rotation.z = PI/2.0

	# Fire extinguisher + green EXIT marker.
	var extinguisher: MeshInstance3D = _cylinder(0.13,0.55,red_mat,Vector3(-3.50,0.62,4.35),root)
	extinguisher.rotation.z = 0.0
	_box(Vector3(0.08,0.13,0.08),dark_mat,Vector3(-3.50,0.96,4.35),root)
	_box(Vector3(0.08,0.42,1.05),green_mat,Vector3(-3.31,2.62,4.70),root)
	_label3d("UT",Vector3(-3.25,2.62,4.70),root,14)

	# Noticeboard is no longer an empty rectangle.
	for i: int in range(5):
		var paper_color: StandardMaterial3D = _mat([book_blue,book_red,book_yellow][i%3].albedo_color.lightened(0.22),0.95)
		_box(Vector3(0.035,0.30,0.22),paper_color,Vector3(-3.23,1.55,2.55+float(i)*0.28),root)

	# Classroom storage wall and shelves.
	_solid_box(Vector3(0.55,2.05,2.15),_mat(Color("7b817d"),0.9),Vector3(2.80,1.03,0.0),root)
	for y_value: float in [0.45,0.95,1.45,1.90]:
		_box(Vector3(0.62,0.06,2.0),dark_mat,Vector3(2.55,y_value,0.0),root)
	for i: int in range(11):
		var bm: Material = book_blue if i%3==0 else (book_red if i%3==1 else book_yellow)
		_box(Vector3(0.12,0.34,0.17),bm,Vector3(2.40,0.68+float(i%3)*0.46,-0.78+float(i%4)*0.38),root)

	# Teacher computer and desktop clutter.
	_box(Vector3(0.10,0.62,0.78),screen_mat,Vector3(2.15,1.18,-1.90),root)
	_box(Vector3(0.30,0.05,0.18),dark_mat,Vector3(2.15,0.84,-1.90),root)
	_box(Vector3(0.36,0.035,0.22),dark_mat,Vector3(1.75,0.85,-1.90),root)
	_box(Vector3(0.18,0.12,0.24),book_red,Vector3(2.55,0.86,-1.90),root)

	# Whiteboard tray + simple clock give human scale.
	_box(Vector3(0.10,0.08,2.50),metal_mat,Vector3(3.18,1.10,-1.85),root)
	var clock_face: StandardMaterial3D = _mat(Color("e9e7dc"),0.82)
	var clock: MeshInstance3D = _cylinder(0.23,0.05,clock_face,Vector3(3.20,2.95,-3.75),root)
	clock.rotation.z = PI/2.0
	_box(Vector3(0.03,0.22,0.03),dark_mat,Vector3(3.16,3.00,-3.75),root)

	# Slightly warmer entrance pool so entering feels intentional.
	_entrance_light = OmniLight3D.new()
	_entrance_light.position = Vector3(-4.6,2.45,6.15)
	_entrance_light.light_color = Color("ffd8a8")
	_entrance_light.light_energy = 1.45
	_entrance_light.omni_range = 5.8
	_entrance_light.shadow_enabled = false
	root.add_child(_entrance_light)

func _add_arena_polish() -> void:
	var root: Node3D = get_node_or_null("NorrbottenStalArena") as Node3D
	if root == null:
		return

	var accent: StandardMaterial3D = _mat(Color("334f62"),0.72,0.06)
	var door_mat: StandardMaterial3D = _textured_mat(Color("3d464b"),0.84,0.10,"vertical",64,0.028)
	var warning_mat: StandardMaterial3D = _mat(Color("c7963e"),0.82)

	# A deliberate facade band makes the hall read as a civic ice arena, not a warehouse.
	_box(Vector3(33.0,0.52,0.13),accent,Vector3(0.0,5.85,10.82),root)
	_box(Vector3(3.2,2.55,0.10),glass_mat,Vector3(-9.0,1.35,14.72),root)
	_box(Vector3(1.42,2.45,0.12),door_mat,Vector3(-10.0,1.28,14.80),root)
	_box(Vector3(1.42,2.45,0.12),door_mat,Vector3(-8.0,1.28,14.80),root)
	_label3d("ENTRÉ",Vector3(-9.0,3.45,14.78),root,22)

	# Service doors, bumpers, warning bollards and outdoor bench.
	for x_value: float in [8.2,11.1,14.0]:
		_box(Vector3(2.25,2.85,0.12),door_mat,Vector3(x_value,1.45,10.78),root)
	for x_value: float in [6.8,8.0,9.2,10.4,11.6,12.8,14.0,15.2]:
		_cylinder(0.07,0.82,warning_mat,Vector3(x_value,0.41,13.0),root)
	_add_bench(Vector3(-4.0,0.0,14.0),root)

	# Hockey identity near the public entrance: stick rack and a small practice goal.
	for i: int in range(5):
		var stick: MeshInstance3D = _box(Vector3(0.05,1.45,0.05),dark_mat,Vector3(-13.1+float(i)*0.22,0.75,13.25),root)
		stick.rotation.z = -0.10+float(i)*0.04
	var white: StandardMaterial3D = _mat(Color("d7dddf"),0.86)
	_box(Vector3(2.2,0.07,0.07),white,Vector3(-14.7,1.10,15.0),root)
	for x_value: float in [-15.8,-13.6]:
		_box(Vector3(0.07,1.15,0.07),white,Vector3(x_value,0.58,15.0),root)

	# Snow-crusted service ventilation gives more believable scale on the side wall.
	for x_value: float in [-14.0,-9.5,-5.0,0.0,5.0,9.5,14.0]:
		_box(Vector3(0.85,0.55,0.28),metal_mat,Vector3(x_value,2.3,-10.60),root)
		_box(Vector3(0.82,0.06,0.30),packed_snow_mat,Vector3(x_value,2.62,-10.60),root)

func _add_exterior_polish() -> void:
	# Public-facing wayfinding and small-scale objects make the slice feel inhabited.
	var blue: StandardMaterial3D = _mat(Color("345c78"),0.76,0.05)
	var sign_root: Node3D = Node3D.new()
	sign_root.name = "SchoolWayfinding"
	sign_root.position = Vector3(-18.5,0.0,9.0)
	add_child(sign_root)
	_cylinder(0.045,2.4,metal_mat,Vector3(0.0,1.2,0.0),sign_root)
	_box(Vector3(2.45,0.62,0.08),blue,Vector3(0.0,2.05,0.0),sign_root)
	_label3d("SKOLA  •  ISHALL",Vector3(0.0,2.05,0.07),sign_root,14)

	# Plowed snow is dirtier right beside the curb instead of uniformly white.
	var dirty_snow: StandardMaterial3D = _textured_mat(Color("929b9e"),0.98,0.0,"snow",64,0.06)
	for p: Vector3 in [Vector3(-18.0,0.08,11.8),Vector3(15.5,0.08,13.0),Vector3(36.0,0.08,42.0),Vector3(57.0,0.08,51.0)]:
		var patch: MeshInstance3D = _box(Vector3(5.0,0.07,1.1),dirty_snow,p,self)
		patch.rotation.y = randf_range(-0.12,0.12)

func _build_ui() -> void:
	var ui: CanvasLayer = CanvasLayer.new()
	add_child(ui)
	var panel: ColorRect = ColorRect.new()
	panel.position = Vector2(18.0,18.0)
	panel.size = Vector2(310.0,108.0)
	panel.color = Color(0.018,0.028,0.035,0.80)
	ui.add_child(panel)
	var label: Label = Label.new()
	label.position = Vector2(14.0,10.0)
	label.text = "ROSVIK: BLACKOUT\nWORLD / INTERIOR POLISH 02\n\nSkola + ishall får mer riktig detaljnivå."
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
		lead = lead.normalized()*1.0
	var p: Vector3 = player.global_position
	var focus: Vector3 = p+lead+Vector3(0.0,1.0,0.0)
	var in_corridor: bool = p.x > -6.15 and p.x < -3.15 and p.z < 8.7 and p.z > -6.45
	var in_classroom: bool = p.x >= -3.15 and p.x < 3.55 and p.z < 1.20 and p.z > -4.95
	if in_classroom:
		focus = p+Vector3(0.25,0.95,-0.10)
		target_camera_pos = focus+Vector3(4.0,10.8,3.2)
		camera.fov = lerp(camera.fov,35.5,1.0-exp(-5.5*delta))
	elif in_corridor:
		focus = p+Vector3(0.0,0.95,-0.15)
		target_camera_pos = focus+Vector3(3.6,11.2,3.0)
		camera.fov = lerp(camera.fov,36.5,1.0-exp(-5.5*delta))
	else:
		target_camera_pos = focus+Vector3(11.4,8.0,11.4)
		camera.fov = lerp(camera.fov,43.0,1.0-exp(-4.0*delta))
	camera.global_position = camera.global_position.lerp(target_camera_pos,1.0-exp(-5.4*delta))
	camera.look_at(focus,Vector3.UP)
