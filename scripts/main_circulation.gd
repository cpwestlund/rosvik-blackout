extends "res://scripts/main_physical.gd"

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
	print("ROSVIK_CIRCULATION_READY solids=", solid_count)

func _build_school() -> void:
	var root: Node3D = Node3D.new()
	root.name = "RosviksSkola"
	root.position = Vector3.ZERO
	add_child(root)

	_solid_box(Vector3(27.0,4.3,12.5), school_mat, Vector3(1.5,2.15,0.0), root)
	_solid_box(Vector3(11.0,3.2,10.0), school_mat, Vector3(-15.8,1.6,1.3), root)
	_solid_box(Vector3(8.0,3.5,7.0), _textured_mat(Color("8f9a9e"),0.9,0.0,"horizontal",96,0.03), Vector3(16.5,1.75,-1.8), root)
	_box(Vector3(28.0,0.34,13.4), roof_mat, Vector3(1.5,4.46,0.0), root)
	_box(Vector3(12.0,0.30,10.8), roof_mat, Vector3(-15.8,3.36,1.3), root)
	_box(Vector3(27.6,0.13,13.0), packed_snow_mat, Vector3(1.5,4.68,0.0), root)
	_box(Vector3(11.6,0.12,10.4), packed_snow_mat, Vector3(-15.8,3.57,1.3), root)
	_box(Vector3(27.2,0.9,0.18), concrete_mat, Vector3(1.5,0.48,6.32), root)
	for i: int in range(9):
		_add_window(root, Vector3(-9.0+float(i)*2.65,2.25,6.39), Vector3(1.45,1.15,0.12))

	# Main entrance volume.
	_solid_box(Vector3(4.8,3.0,2.3), dark_mat, Vector3(-4.6,1.5,7.0), root)
	_box(Vector3(3.1,2.5,0.15), glass_mat, Vector3(-4.6,1.3,8.18), root)
	_box(Vector3(5.8,0.28,3.0), roof_mat, Vector3(-4.6,3.05,8.2), root)
	_solid_box(Vector3(4.6,0.16,2.8), concrete_mat, Vector3(-4.6,0.08,9.0), root)
	_solid_box(Vector3(4.1,0.12,1.5), concrete_mat, Vector3(-4.6,0.22,10.1), root)
	for x_value: float in [-5.5,2.0,8.0]:
		_box(Vector3(1.7,0.72,1.2), metal_mat, Vector3(x_value,5.08,-1.1), root)
	for x_value: float in [-11.5,12.8]:
		_solid_cylinder(0.055,4.0,metal_mat,Vector3(x_value,2.0,6.52),root)

	# Yard surfaces. The dark/packed path deliberately continues through the gate.
	_box(Vector3(33.0,0.08,20.0), packed_snow_mat, Vector3(-1.0,0.03,19.0), root)
	_box(Vector3(13.0,0.06,8.0), slush_mat, Vector3(-4.0,0.055,11.5), root)
	_box(Vector3(4.2,0.065,12.5), slush_mat, Vector3(-4.6,0.058,16.2), root)
	_box(Vector3(4.2,0.065,8.0), slush_mat, Vector3(15.7,0.058,21.0), root)

	# Front fence has a 4 m pedestrian gate aligned to the main entrance.
	_add_fence(Vector3(-18.0,0.0,13.0),Vector3(-6.7,0.0,13.0),root)
	_add_fence(Vector3(-2.5,0.0,13.0),Vector3(15.0,0.0,13.0),root)
	_gate_posts(Vector3(-6.7,0.0,13.0),Vector3(-2.5,0.0,13.0),root)

	# West side stays closed. East side gets a second escape/circulation gate.
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
	_label3d("ROSVIKS SKOLA",Vector3(-4.5,3.9,8.38),root,42)

func _build_player() -> void:
	player = CharacterBody3D.new()
	player.set_script(PLAYER_SCRIPT)
	# Spawn on the entrance approach, outside collision volumes and aligned with the gate.
	player.position = Vector3(-4.6,0.0,11.0)
	add_child(player)
	camera = Camera3D.new()
	camera.fov = 47.0
	camera.current = true
	camera.near = 0.1
	camera.far = 250.0
	add_child(camera)
	_update_camera(1.0)

func _build_ui() -> void:
	var ui: CanvasLayer = CanvasLayer.new()
	add_child(ui)
	var panel: ColorRect = ColorRect.new()
	panel.position = Vector2(18.0,18.0)
	panel.size = Vector2(335.0,126.0)
	panel.color = Color(0.02,0.032,0.042,0.86)
	ui.add_child(panel)
	var label: Label = Label.new()
	label.position = Vector2(16.0,12.0)
	label.text = "ROSVIK: BLACKOUT\nCIRCULATION PASS 01\n\nGångvägar och grindöppningar är nu spelbara."
	label.add_theme_font_size_override("font_size",16)
	panel.add_child(label)
	var hint: Label = Label.new()
	hint.position = Vector2(18.0,852.0)
	hint.text = "WASD / pilar: gå     Shift: spring"
	hint.add_theme_font_size_override("font_size",14)
	ui.add_child(hint)

func _gate_posts(a: Vector3, b: Vector3, parent: Node) -> void:
	# Stronger terminal posts visually explain that the missing fence segment is intentional.
	_solid_box(Vector3(0.12,1.45,0.12), dark_mat, a+Vector3(0.0,0.725,0.0), parent)
	_solid_box(Vector3(0.12,1.45,0.12), dark_mat, b+Vector3(0.0,0.725,0.0), parent)
	var mid: Vector3 = (a+b)*0.5
	var sign_pos: Vector3 = mid+Vector3(0.0,1.65,0.0)
	var sign_mat: StandardMaterial3D = _mat(Color("6d7b82"),0.82)
	_box(Vector3(1.15,0.28,0.06),sign_mat,sign_pos,parent)
