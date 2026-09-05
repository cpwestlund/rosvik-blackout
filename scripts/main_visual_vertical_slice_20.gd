extends "res://scripts/main_reference_art_19c.gd"

# ROSVIK VISUAL VERTICAL SLICE 20
# Goal: stop treating the hero zone as a blockout. Controls are screen-relative,
# the school has a real usable entrance, Rosvalla reads as one winter football
# ground, and the immediate area receives dense grounded everyday detail.

var _detail_root20: Node3D

func _ready() -> void:
	super._ready()
	_detail_root20 = Node3D.new()
	_detail_root20.name = "RosvikVerticalSlice20"
	add_child(_detail_root20)
	_remove_roof_scallops20()
	_fix_school_entrance20()
	_add_schoolyard_life20()
	_add_road_detail20()
	_add_winter_microdetail20()
	_add_rosvalla_detail20()
	print("ROSVIK_VISUAL_VERTICAL_SLICE_20_READY")
	print("ROSVIK_USABLE_SCHOOL_ENTRANCE_20_READY")
	print("ROSVIK_ART_DENSITY_20_READY")
	print("ROSVIK_NATURAL_CAMERA_CONTROLS_20_READY")

# Camera orbit is independent from movement. Dragging right rotates the view
# in the same direction instead of feeling mirrored.
func _unhandled_input(event: InputEvent) -> void:
	if _capture_mode:
		return
	if event is InputEventMouseButton:
		var mb := event as InputEventMouseButton
		if mb.button_index == MOUSE_BUTTON_MIDDLE:
			_camera_dragging = mb.pressed
			get_viewport().set_input_as_handled()
		elif mb.button_index == MOUSE_BUTTON_WHEEL_UP and mb.pressed:
			_ortho_size19b = clampf(_ortho_size19b-0.9,10.5,25.0)
			get_viewport().set_input_as_handled()
		elif mb.button_index == MOUSE_BUTTON_WHEEL_DOWN and mb.pressed:
			_ortho_size19b = clampf(_ortho_size19b+0.9,10.5,25.0)
			get_viewport().set_input_as_handled()
	elif event is InputEventMouseMotion and _camera_dragging:
		var mm := event as InputEventMouseMotion
		_camera_yaw += mm.relative.x*0.0054
		_camera_pitch = clampf(_camera_pitch-mm.relative.y*0.0038,0.44,0.74)
		get_viewport().set_input_as_handled()

func _remove_roof_scallops20() -> void:
	if _art_root19b == null:
		return
	for child: Node in _art_root19b.get_children():
		if child is MeshInstance3D:
			var m := child as MeshInstance3D
			if m.mesh is SphereMesh and m.position.y > 4.2 and m.position.z > 5.8:
				m.visible = false

func _fix_school_entrance20() -> void:
	if _school_root == null:
		return
	# The old visual door was sitting partly in the wall rather than in the
	# actual 2.4 m wall opening. Remove only that leaf.
	_hide_bad_door20(_school_root)
	var entrance := Node3D.new()
	entrance.name = "UsableEntrance20"
	_school_root.add_child(entrance)
	var frame := _mat(Color("27363d"),0.82,0.10)
	var glass := _mat(Color(0.20,0.33,0.38,0.52),0.22,0.02)
	glass.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	var warm := _mat(Color("d8b17b"),0.84)

	# True opening is centred around x=-5.0. Both leaves are visibly propped open.
	for x: float in [-6.05,-3.95]:
		_solid_box(Vector3(0.12,2.85,0.16),frame,Vector3(x,1.43,6.10),entrance)
	_box(Vector3(2.25,0.14,0.16),frame,Vector3(-5.0,2.84,6.10),entrance)
	var left := _box(Vector3(0.92,2.45,0.08),glass,Vector3(-5.82,1.27,6.30),entrance)
	left.rotation.y = 0.82
	var right := _box(Vector3(0.92,2.45,0.08),glass,Vector3(-4.18,1.27,6.30),entrance)
	right.rotation.y = -0.82
	_box(Vector3(2.15,0.045,1.35),path_mat,Vector3(-5.0,0.055,6.45),entrance)
	_box(Vector3(1.85,0.025,0.34),warm,Vector3(-5.0,0.075,5.86),entrance)
	# Strong visual invitation into the building.
	for x: float in [-5.72,-4.28]:
		var l := OmniLight3D.new()
		l.position = Vector3(x,2.28,6.40)
		l.light_color = Color("ffc47d")
		l.light_energy = 0.0
		l.omni_range = 4.0
		l.shadow_enabled = true
		entrance.add_child(l)
		_school_lights.append(l)

func _hide_bad_door20(node: Node) -> void:
	for child: Node in node.get_children():
		if child is MeshInstance3D:
			var m := child as MeshInstance3D
			var p := m.global_position
			if absf(p.x+6.28) < 0.22 and absf(p.z-6.45) < 0.22 and p.y > 0.8 and p.y < 1.8:
				m.visible = false
		_hide_bad_door20(child)

func _add_schoolyard_life20() -> void:
	if _school_root == null:
		return
	var root := Node3D.new()
	root.name = "SchoolyardLife20"
	_school_root.add_child(root)
	# Two grounded bicycles: proper wheels and frames, not floating icons.
	_add_bicycle20(root,Vector3(7.2,0.0,8.9),0.10,Color("365a68"))
	_add_bicycle20(root,Vector3(8.0,0.0,9.0),-0.05,Color("8b4b41"))
	_add_bicycle20(root,Vector3(9.0,0.0,9.1),0.08,Color("4f5c4d"))
	# Everyday traces near the school entrance.
	_add_snow_shovel20(root,Vector3(-7.35,0.0,7.55),0.18)
	_add_snow_shovel20(root,Vector3(13.9,0.0,8.05),-0.28)
	_add_bin20(root,Vector3(-8.1,0.0,7.75),Color("334c43"))
	_add_bin20(root,Vector3(12.8,0.0,8.15),Color("4c5651"))
	_add_kicksled19b(_school_root.to_global(Vector3(-11.7,0,9.25)))
	# Small lost-child details near the path.
	_add_mitten20(root,Vector3(-4.25,0.10,9.15),Color("a24e42"),0.35)
	_add_mitten20(root,Vector3(-3.82,0.10,9.38),Color("a24e42"),-0.15)
	_add_ball20(root,Vector3(4.6,0.19,14.8),Color("b46b3f"))

func _add_road_detail20() -> void:
	var root := Node3D.new()
	root.name = "RoadMicrodetail20"
	add_child(root)
	var curb := _mat(Color("737b79"),0.97)
	var drain := _mat(Color("30393b"),0.70,0.18)
	# Curbs and drainage give the school frontage an authored edge.
	for z: float in [11.15,16.75]:
		_box(Vector3(31.0,0.12,0.16),curb,Vector3(1.0,0.08,z),root)
	for x: float in [-10.5,-1.0,9.0,18.0]:
		for i: int in range(5):
			_box(Vector3(0.11,0.018,0.54),drain,Vector3(x+float(i)*0.15,0.075,11.38),root)
	# Wheel tracks are broken, imperfect and partly filled with snow.
	for lane: float in [-0.62,0.62]:
		for i: int in range(14):
			if i in [3,8,11]:
				continue
			var seg := _box(Vector3(1.35,0.012,0.11),dirty_snow_mat,Vector3(-14.0+float(i)*2.65,0.075,14.0+lane),root)
			seg.rotation.y = 0.01*sin(float(i)*1.7)

func _add_winter_microdetail20() -> void:
	var root := Node3D.new()
	root.name = "WinterMicrodetail20"
	add_child(root)
	# Small irregular snow accumulations replace giant smooth 'soap bars'.
	for spec: Dictionary in [
		{"p":Vector3(-15.4,0,8.1),"s":Vector3(1.6,0.18,0.42),"r":0.1},
		{"p":Vector3(-9.2,0,7.0),"s":Vector3(1.1,0.15,0.35),"r":-0.12},
		{"p":Vector3(10.7,0,7.1),"s":Vector3(1.4,0.17,0.38),"r":0.08},
		{"p":Vector3(15.5,0,7.2),"s":Vector3(1.0,0.14,0.32),"r":-0.18},
		{"p":Vector3(35.0,0,29.0),"s":Vector3(1.9,0.20,0.52),"r":0.09},
		{"p":Vector3(80.6,0,29.5),"s":Vector3(2.2,0.22,0.54),"r":-0.08}
	]:
		_snow_mound20(root,spec["p"],spec["s"],float(spec["r"]))
	# Footprints from entrance toward road and schoolyard.
	_add_footpath20(root,Vector3(-5.0,0.0,7.3),Vector3(-5.4,0.0,14.2),17)
	_add_footpath20(root,Vector3(-4.0,0.0,9.0),Vector3(7.5,0.0,11.2),22)

func _build_rosvalla19() -> void:
	var root := Node3D.new()
	root.name = "Rosvalla19"
	add_child(root)
	var center := Vector3(9.0,0.0,53.0)
	_box(Vector3(64.0,0.032,38.0),field_mat,center+Vector3(0,0.016,0),root)

	# One real field under patchy windblown snow. No giant rectangular overlays.
	for spec: Dictionary in [
		{"p":Vector3(-22,0,-13),"s":Vector3(4.5,0.075,1.25),"r":0.12},
		{"p":Vector3(12,0,-14),"s":Vector3(3.8,0.065,1.05),"r":-0.08},
		{"p":Vector3(23,0,-6),"s":Vector3(3.3,0.060,1.10),"r":0.18},
		{"p":Vector3(-13,0,-3),"s":Vector3(4.8,0.065,1.00),"r":-0.14},
		{"p":Vector3(18,0,4),"s":Vector3(4.0,0.060,1.25),"r":0.10},
		{"p":Vector3(-24,0,9),"s":Vector3(3.7,0.065,1.10),"r":0.20},
		{"p":Vector3(1,0,14),"s":Vector3(5.0,0.070,1.10),"r":-0.10},
		{"p":Vector3(25,0,15),"s":Vector3(3.1,0.060,0.95),"r":0.16}
	]:
		_snow_mound20(root,center+spec["p"],spec["s"],float(spec["r"]))

	var line := _mat(Color("c5c9c2"),0.98)
	# Broken touchlines/sidelines peeking through snow.
	for x: float in [-24.0,-12.0,0.0,12.0,24.0]:
		_box(Vector3(7.1,0.012,0.070),line,center+Vector3(x,0.060,-17.55),root)
		_box(Vector3(7.1,0.012,0.070),line,center+Vector3(x,0.060,17.55),root)
	for z: float in [-13.0,-5.0,5.0,13.0]:
		_box(Vector3(0.070,0.012,5.0),line,center+Vector3(-30.55,0.060,z),root)
		_box(Vector3(0.070,0.012,5.0),line,center+Vector3(30.55,0.060,z),root)
	for z: float in [-14.0,-7.0,0.0,7.0,14.0]:
		_box(Vector3(0.070,0.012,3.5),line,center+Vector3(0,0.060,z),root)

	_add_goal19(root,center+Vector3(-31.7,0,0),PI/2.0)
	_add_goal19(root,center+Vector3(31.7,0,0),-PI/2.0)
	_add_fence19(root,center+Vector3(-32.8,0,-20.0),center+Vector3(32.8,0,-20.0))
	_add_fence19(root,center+Vector3(-32.8,0,20.0),center+Vector3(18.0,0,20.0))
	_add_fence19(root,center+Vector3(24.0,0,20.0),center+Vector3(32.8,0,20.0))
	for p: Vector3 in [center+Vector3(-27,0,-20.8),center+Vector3(27,0,-20.8),center+Vector3(-27,0,20.8),center+Vector3(27,0,20.8)]:
		_add_field_light19(root,p)
	# Both dugouts are on the north touchline and open toward the pitch.
	_add_dugout19b(root,center+Vector3(-8,0,-21.2),PI)
	_add_dugout19b(root,center+Vector3(8,0,-21.2),PI)
	for p: Vector3 in [center+Vector3(-29,0,-17),center+Vector3(28,0,16),center+Vector3(-30,0,15),center+Vector3(21,0,-18)]:
		_snow_mound20(root,p,Vector3(2.8,0.20,0.72),0.0)
	_world_prop_count += 94

func _add_rosvalla_detail20() -> void:
	var field := get_node_or_null("Rosvalla19") as Node3D
	if field == null:
		return
	var center := Vector3(9.0,0.0,53.0)
	_add_bin20(field,center+Vector3(-18,0,-21.0),Color("3d4b46"))
	_add_bin20(field,center+Vector3(18,0,-21.0),Color("3d4b46"))
	_add_ball20(field,center+Vector3(6.0,0.19,5.0),Color("d6d0ba"))
	_add_ball20(field,center+Vector3(-13.0,0.19,-9.0),Color("c3b7a0"))

func _add_bicycle20(parent: Node3D,pos: Vector3,yaw: float,color: Color) -> void:
	var root := Node3D.new()
	root.position = pos
	root.rotation.y = yaw
	parent.add_child(root)
	var rubber := _mat(Color("1d2528"),0.98)
	var frame := _mat(color,0.70,0.12)
	for x: float in [-0.52,0.52]:
		var tor := TorusMesh.new()
		tor.inner_radius = 0.25
		tor.outer_radius = 0.31
		var wheel := MeshInstance3D.new()
		wheel.mesh = tor
		wheel.material_override = rubber
		wheel.position = Vector3(x,0.31,0)
		wheel.rotation_degrees.y = 90
		root.add_child(wheel)
	var parts: Array[Array] = [
		[Vector3(-0.52,0.31,0),Vector3(0.0,0.72,0)],
		[Vector3(0.52,0.31,0),Vector3(0.0,0.72,0)],
		[Vector3(-0.52,0.31,0),Vector3(0.18,0.31,0)],
		[Vector3(0.18,0.31,0),Vector3(0.0,0.72,0)]
	]
	for pair: Array in parts:
		_add_rod20(root,pair[0],pair[1],0.025,frame)
	_add_rod20(root,Vector3(0.0,0.72,0),Vector3(0.10,0.94,0),0.025,frame)
	_add_rod20(root,Vector3(0.10,0.94,-0.22),Vector3(0.10,0.94,0.22),0.022,frame)

func _add_rod20(parent: Node3D,a: Vector3,b: Vector3,radius: float,mat: Material) -> void:
	var mid := (a+b)*0.5
	var d := b-a
	var n := _cylinder(radius,d.length(),mat,mid,parent)
	n.quaternion = Quaternion(Vector3.UP,d.normalized())

func _add_snow_shovel20(parent: Node3D,pos: Vector3,yaw: float) -> void:
	var root := Node3D.new()
	root.position = pos
	root.rotation.y = yaw
	root.rotation.z = -0.22
	parent.add_child(root)
	_cylinder(0.025,1.55,wood_mat,Vector3(0,0.78,0),root)
	_box(Vector3(0.52,0.10,0.38),_mat(Color("a5573e"),0.90),Vector3(0,0.08,0),root)

func _add_bin20(parent: Node3D,pos: Vector3,color: Color) -> void:
	var root := Node3D.new()
	root.position = pos
	parent.add_child(root)
	var mat := _mat(color,0.94)
	_solid_box(Vector3(0.62,0.82,0.54),mat,Vector3(0,0.41,0),root)
	var lid := _box(Vector3(0.70,0.09,0.60),dark_mat,Vector3(0,0.86,0),root)
	lid.rotation.x = -0.08
	for x: float in [-0.22,0.22]:
		var w := _cylinder(0.09,0.06,dark_mat,Vector3(x,0.12,-0.30),root)
		w.rotation.x = PI/2.0

func _add_mitten20(parent: Node3D,pos: Vector3,color: Color,yaw: float) -> void:
	var m := _capsule19(0.12,0.25,_mat(color,0.98),pos,parent)
	m.rotation.y = yaw
	m.scale = Vector3(0.8,0.55,1.0)

func _add_ball20(parent: Node3D,pos: Vector3,color: Color) -> void:
	var mesh := SphereMesh.new()
	mesh.radius = 0.18
	mesh.height = 0.36
	mesh.radial_segments = 16
	mesh.rings = 8
	var n := MeshInstance3D.new()
	n.mesh = mesh
	n.material_override = _mat(color,0.92)
	n.position = pos
	parent.add_child(n)

func _snow_mound20(parent: Node3D,pos: Vector3,scale_value: Vector3,yaw: float) -> void:
	# Three overlapping low spheres create an irregular drift silhouette.
	for i: int in range(3):
		var mesh := SphereMesh.new()
		mesh.radius = 1.0
		mesh.height = 2.0
		mesh.radial_segments = 20
		mesh.rings = 10
		var n := MeshInstance3D.new()
		n.mesh = mesh
		n.material_override = packed_snow_mat
		n.position = pos+Vector3((float(i)-1.0)*scale_value.x*0.48,scale_value.y*0.08,0)
		n.scale = Vector3(scale_value.x*0.52,scale_value.y,scale_value.z*(0.90+0.08*float(i)))
		n.rotation.y = yaw+float(i-1)*0.06
		parent.add_child(n)

func _add_footpath20(parent: Node3D,a: Vector3,b: Vector3,count: int) -> void:
	for i: int in range(count):
		var t := float(i)/float(maxi(1,count-1))
		var base := a.lerp(b,t)
		var side := -0.15 if i%2==0 else 0.15
		var direction := (b-a).normalized()
		var right := Vector3(-direction.z,0,direction.x)
		var p := base+right*side
		var foot := _box(Vector3(0.15,0.012,0.30),dirty_snow_mat,p+Vector3(0,0.058,0),parent)
		foot.rotation.y = atan2(direction.x,direction.z)+(-0.08 if i%2==0 else 0.08)

func _run_capture_sequence19() -> void:
	_set_school_power19(true)
	_school_powered = true
	if _cable_connected_visual != null:
		_cable_connected_visual.visible = true
	var dir := ProjectSettings.globalize_path("res://build/captures")
	DirAccess.make_dir_recursive_absolute(dir)
	camera.projection = Camera3D.PROJECTION_ORTHOGONAL
	camera.size = 15.5
	await _capture_view19("01_school_exterior.png",Vector3(23,16,25),Vector3(-1,1.4,6.0),false)
	camera.size = 27.0
	await _capture_view19("02_school_sport_rosvalla.png",Vector3(58,35,72),Vector3(17,0.7,38),false)
	camera.size = 10.0
	await _capture_view19("03_school_interior.png",Vector3(11.5,9.5,13.0),Vector3(5.3,1.1,-0.5),true)
	print("ROSVIK_VISUAL_CAPTURE_19_READY files=3")
	get_tree().quit()
