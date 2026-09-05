extends "res://scripts/main_world_expansion_interaction_11.gd"

# Camera + Residential Life 12
# Director goal: never lose the player behind a house, and make the first
# residential edge feel inhabited rather than like three boxes in snow.

var _camera12_yaw: float = PI / 4.0
var _camera12_pitch: float = 0.58
var _camera12_distance: float = 25.0
var _camera12_dragging: bool = false
var _camera12_last_mouse: Vector2 = Vector2.ZERO
var _camera12_occluders: Array[GeometryInstance3D] = []
var _camera12_original_transparency: Dictionary = {}
var _life12_root: Node3D
var _life12_hint: Label

func _ready() -> void:
	super._ready()
	_build_residential_life12()
	_build_camera_hint12()
	_collect_occluders12()
	print("ROSVIK_CAMERA_ORBIT_12_READY")
	print("ROSVIK_CAMERA_OCCLUSION_12_READY")
	print("ROSVIK_RESIDENTIAL_LIFE_12_READY")
	print("ROSVIK_YARD_STORYTELLING_12_READY")

func _process(delta: float) -> void:
	super._process(delta)
	_update_occlusion12()

func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventMouseButton:
		var mb := event as InputEventMouseButton
		if mb.button_index == MOUSE_BUTTON_MIDDLE:
			_camera12_dragging = mb.pressed
			_camera12_last_mouse = mb.position
			get_viewport().set_input_as_handled()
		elif mb.button_index == MOUSE_BUTTON_WHEEL_UP and mb.pressed:
			_camera12_distance = clampf(_camera12_distance - 2.0,16.0,34.0)
			get_viewport().set_input_as_handled()
		elif mb.button_index == MOUSE_BUTTON_WHEEL_DOWN and mb.pressed:
			_camera12_distance = clampf(_camera12_distance + 2.0,16.0,34.0)
			get_viewport().set_input_as_handled()
	elif event is InputEventMouseMotion and _camera12_dragging:
		var mm := event as InputEventMouseMotion
		_camera12_yaw -= mm.relative.x * 0.0075
		_camera12_pitch = clampf(_camera12_pitch + mm.relative.y * 0.0045,0.38,0.82)
		_camera12_last_mouse = mm.position
		get_viewport().set_input_as_handled()

func _update_camera(delta: float) -> void:
	if player == null or camera == null:
		return
	var velocity_dir := Vector3(player.velocity.x,0.0,player.velocity.z)
	if velocity_dir.length() > 0.1:
		velocity_dir = velocity_dir.normalized() * 2.0
	var focus := player.global_position + velocity_dir + Vector3(0.0,1.15,0.0)
	var horizontal := cos(_camera12_pitch) * _camera12_distance
	var offset := Vector3(cos(_camera12_yaw)*horizontal,sin(_camera12_pitch)*_camera12_distance,sin(_camera12_yaw)*horizontal)
	target_camera_pos = focus + offset
	camera.global_position = camera.global_position.lerp(target_camera_pos,1.0-exp(-5.2*delta))
	camera.look_at(focus,Vector3.UP)

func _collect_occluders12() -> void:
	_camera12_occluders.clear()
	_camera12_original_transparency.clear()
	_collect_occluders_from12(self)

func _collect_occluders_from12(node: Node) -> void:
	for child: Node in node.get_children():
		if child is MeshInstance3D:
			var mesh := child as MeshInstance3D
			# Large wall/roof pieces only. Props remain fully visible.
			var aabb := mesh.get_aabb()
			if aabb.size.x > 3.2 and aabb.size.y > 1.8 or aabb.size.z > 3.2 and aabb.size.y > 1.8:
				_camera12_occluders.append(mesh)
				_camera12_original_transparency[mesh.get_instance_id()] = mesh.transparency
		_collect_occluders_from12(child)

func _update_occlusion12() -> void:
	if player == null or camera == null:
		return
	var cam2 := Vector2(camera.global_position.x,camera.global_position.z)
	var player2 := Vector2(player.global_position.x,player.global_position.z)
	for mesh: GeometryInstance3D in _camera12_occluders:
		if not is_instance_valid(mesh):
			continue
		var p := Vector2(mesh.global_position.x,mesh.global_position.z)
		var near_line := _distance_to_segment12(p,cam2,player2)
		var between := p.distance_to(cam2) < cam2.distance_to(player2) and p.distance_to(player2) < 16.0
		var target_alpha := 0.68 if between and near_line < 5.0 else 0.0
		mesh.transparency = lerpf(mesh.transparency,target_alpha,0.18)

func _distance_to_segment12(p: Vector2,a: Vector2,b: Vector2) -> float:
	var ab := b-a
	var denom := ab.length_squared()
	if denom < 0.0001:
		return p.distance_to(a)
	var t := clampf((p-a).dot(ab)/denom,0.0,1.0)
	return p.distance_to(a+ab*t)

func _build_camera_hint12() -> void:
	var ui := CanvasLayer.new()
	ui.layer = 22
	add_child(ui)
	_life12_hint = Label.new()
	_life12_hint.position = Vector2(20.0,820.0)
	_life12_hint.text = "Mushjul: zoom   Håll mushjul + dra: rotera kamera"
	_life12_hint.add_theme_font_size_override("font_size",12)
	_life12_hint.add_theme_color_override("font_color",Color("d5ddd9"))
	ui.add_child(_life12_hint)

func _build_residential_life12() -> void:
	_life12_root = Node3D.new()
	_life12_root.name = "ResidentialLife12"
	add_child(_life12_root)
	# Three distinct households: workshop, family yard, snowmobile/wood yard.
	_build_yard12(Vector3(-101.5,0.0,-10.0),0)
	_build_yard12(Vector3(-101.0,0.0,8.0),1)
	_build_yard12(Vector3(-101.8,0.0,25.0),2)
	# A few trees and bushes frame the street instead of leaving an empty snow plane.
	for p: Vector3 in [Vector3(-94.0,0.0,-18.0),Vector3(-92.0,0.0,17.0),Vector3(-94.5,0.0,33.0),Vector3(-118.0,0.0,-17.0),Vector3(-119.0,0.0,17.0),Vector3(-118.5,0.0,31.0)]:
		_add_tree(p)
	for p: Vector3 in [Vector3(-96.0,0.0,-5.0),Vector3(-96.0,0.0,12.0),Vector3(-96.2,0.0,29.0)]:
		_bush12(p)

func _build_yard12(origin: Vector3,variant: int) -> void:
	var root := Node3D.new()
	root.position = origin
	root.name = "YardStory12_%d" % variant
	_life12_root.add_child(root)
	var timber := _textured_mat(Color("73543d"),0.98,0.0,"horizontal",64,0.02)
	var muted_red := _mat(Color("7b493e"),0.9)
	var muted_blue := _mat(Color("52646a"),0.9)
	# driveway edge, mailbox and bins
	_solid_box(Vector3(0.10,1.05,0.10),metal_mat,Vector3(-6.7,0.52,-2.0),root)
	_solid_box(Vector3(0.55,0.34,0.34),muted_blue,Vector3(-6.7,1.04,-2.0),root)
	for i: int in range(2):
		_solid_box(Vector3(0.62,0.88,0.58),_mat(Color("354b45"),0.95),Vector3(4.2-float(i)*0.75,0.44,3.95),root)
	# fence/hedge fragments leave real gates instead of enclosing player.
	for x: float in [-3.5,-1.8,0.0,1.8,3.5]:
		_solid_box(Vector3(1.35,0.62,0.35),_mat(Color("465a46"),0.98),Vector3(x,0.31,-4.6),root)
	# snow pushed to edges; visual only, deliberately no collision.
	for x: float in [-3.4,0.0,3.4]:
		var drift := _box(Vector3(2.2,0.42,0.9),packed_snow_mat,Vector3(x,0.18,4.75),root)
		drift.rotation.y = 0.12*float(x)
	if variant == 0:
		# workshop household
		_solid_box(Vector3(2.4,0.18,1.2),timber,Vector3(2.2,0.12,3.25),root)
		for i: int in range(4):
			_box(Vector3(0.42,0.35,0.55),_mat(Color("65584a"),0.98),Vector3(1.3+float(i)*0.55,0.32,3.2),root)
		_tool_rack12(root,Vector3(4.35,0.0,-2.2))
	elif variant == 1:
		# family yard
		_solid_box(Vector3(1.6,0.12,0.48),muted_red,Vector3(-1.5,0.35,3.4),root)
		_solid_box(Vector3(0.10,1.1,0.10),metal_mat,Vector3(-2.1,0.55,3.4),root)
		_solid_box(Vector3(0.10,1.1,0.10),metal_mat,Vector3(-0.9,0.55,3.4),root)
		# child's sled and forgotten ball
		_box(Vector3(1.0,0.08,0.42),muted_red,Vector3(1.2,0.12,3.7),root)
		_cylinder(0.23,0.22,_mat(Color("b06b3d"),0.85),Vector3(2.2,0.23,3.4),root)
	else:
		# northern utility yard: wood, trailer and covered snowmobile silhouette
		for row: int in range(3):
			for col: int in range(5):
				var log := _cylinder(0.11,0.75,timber,Vector3(2.0+float(col)*0.27,0.15+float(row)*0.21,3.5),root)
				log.rotation.z = PI/2.0
		_collision_box(root,Vector3(1.7,0.75,0.8),Vector3(2.55,0.38,3.5))
		_build_snowmobile12(root,Vector3(-1.8,0.0,3.25))

func _tool_rack12(parent: Node3D,pos: Vector3) -> void:
	_solid_box(Vector3(0.12,1.5,1.6),_mat(Color("4d5554"),0.95),pos+Vector3(0.0,0.75,0.0),parent)
	for z: float in [-0.55,0.0,0.55]:
		_box(Vector3(0.16,0.9,0.08),_mat(Color("9a7044"),0.9),pos+Vector3(-0.12,0.85,z),parent)

func _build_snowmobile12(parent: Node3D,pos: Vector3) -> void:
	var cover := _mat(Color("48575b"),0.95)
	_solid_box(Vector3(2.25,0.55,0.9),cover,pos+Vector3(0.0,0.52,0.0),parent)
	var nose := _box(Vector3(0.8,0.48,0.82),cover,pos+Vector3(-1.05,0.46,0.0),parent)
	nose.rotation.z = -0.22
	for z: float in [-0.48,0.48]:
		var ski := _box(Vector3(1.35,0.06,0.08),metal_mat,pos+Vector3(-1.0,0.08,z),parent)
		ski.rotation.y = 0.06

func _bush12(pos: Vector3) -> void:
	var root := Node3D.new()
	root.position = pos
	_life12_root.add_child(root)
	var m := _mat(Color("425347"),0.98)
	for off: Vector3 in [Vector3(-0.35,0.35,0.0),Vector3(0.25,0.42,0.18),Vector3(0.0,0.32,-0.32)]:
		var mesh := SphereMesh.new()
		mesh.radius = 0.55
		mesh.height = 0.85
		var n := MeshInstance3D.new()
		n.mesh = mesh
		n.material_override = m
		n.position = off
		root.add_child(n)
