extends CharacterBody3D

# ROSVIK: BLACKOUT — Hero Rebuild 19 player
# Movement is screen-relative: W is always visually up/forward on screen, S down/back, A left, D right.

@export var walk_speed: float = 3.25
@export var run_speed: float = 5.45
@export var acceleration: float = 24.0
@export var braking: float = 34.0
@export var turn_speed: float = 12.0

var _visual := Node3D.new()
var _hips := Node3D.new()
var _torso := Node3D.new()
var _head := Node3D.new()
var _left_arm := Node3D.new()
var _right_arm := Node3D.new()
var _left_leg := Node3D.new()
var _right_leg := Node3D.new()
var _phase := 0.0

func _ready() -> void:
	name = "Player"
	motion_mode = CharacterBody3D.MOTION_MODE_GROUNDED
	floor_snap_length = 0.38
	floor_stop_on_slope = true
	max_slides = 6
	var shape := CapsuleShape3D.new()
	shape.radius = 0.31
	shape.height = 1.72
	var collision := CollisionShape3D.new()
	collision.shape = shape
	collision.position.y = 0.86
	add_child(collision)
	_build_winter_person()
	print("ROSVIK_WORLD_LOCKED_CONTROLS_19_READY")
	print("ROSVIK_SCREEN_RELATIVE_CONTROLS_20_READY")
	print("ROSVIK_WINTER_PLAYER_19_READY")

func _physics_process(delta: float) -> void:
	# Natural top-down controls: keys map to the screen, not arbitrary world axes.
	# Camera rotation therefore NEVER makes A become visual-right or W become sideways.
	var x_axis := Input.get_action_strength("move_right") - Input.get_action_strength("move_left")
	var y_axis := Input.get_action_strength("move_forward") - Input.get_action_strength("move_back")
	var requested := Vector3.ZERO
	var cam := get_viewport().get_camera_3d()
	if cam != null:
		var screen_right := cam.global_transform.basis.x
		screen_right.y = 0.0
		screen_right = screen_right.normalized()
		var screen_up := -cam.global_transform.basis.z
		screen_up.y = 0.0
		screen_up = screen_up.normalized()
		requested = screen_right * x_axis + screen_up * y_axis
	else:
		requested = Vector3(x_axis,0.0,-y_axis)
	if requested.length() > 1.0:
		requested = requested.normalized()

	var speed := run_speed if Input.is_action_pressed("sprint") else walk_speed
	var target := requested * speed
	var reversing := false
	var current_horizontal := Vector3(velocity.x,0.0,velocity.z)
	if requested.length() > 0.05 and current_horizontal.length() > 0.10:
		reversing = current_horizontal.normalized().dot(requested) < 0.25
	var accel := braking if requested == Vector3.ZERO else (42.0 if reversing else acceleration)
	velocity.x = move_toward(velocity.x,target.x,accel*delta)
	velocity.z = move_toward(velocity.z,target.z,accel*delta)

	var horizontal := Vector3(velocity.x,0.0,velocity.z)
	if horizontal.length() > 0.14:
		var target_yaw := atan2(horizontal.x,horizontal.z)
		rotation.y = lerp_angle(rotation.y,target_yaw,1.0-exp(-turn_speed*delta))

	if not is_on_floor():
		velocity.y -= 18.0*delta
	move_and_slide()
	_animate(delta)

func _build_winter_person() -> void:
	_visual.name = "WinterPerson19"
	add_child(_visual)

	var coat := _mat(Color("8f493f"), 0.94)
	var coat_dark := _mat(Color("63372f"), 0.96)
	var pants := _mat(Color("34434a"), 0.98)
	var boots := _mat(Color("20262a"), 0.99)
	var skin := _mat(Color("cda082"), 0.94)
	var hat := _mat(Color("39474e"), 0.96)
	var scarf := _mat(Color("b46e43"), 0.94)
	var gloves := _mat(Color("262c2f"), 0.99)
	var pack := _mat(Color("4a5c4b"), 0.97)

	_hips.position = Vector3(0, 0.84, 0)
	_visual.add_child(_hips)
	_mesh_capsule(0.23, 0.34, pants, Vector3(0,0.08,0), _hips)

	_torso.position = Vector3(0, 0.18, 0)
	_hips.add_child(_torso)
	var coat_body := _mesh_capsule(0.34, 0.82, coat, Vector3(0,0.42,0), _torso)
	coat_body.scale = Vector3(1.0,1.0,0.72)
	_mesh_box(Vector3(0.56,0.14,0.34), coat_dark, Vector3(0,0.10,0), _torso)
	_mesh_box(Vector3(0.40,0.50,0.16), pack, Vector3(0,0.44,-0.29), _torso)
	_mesh_capsule(0.26,0.10,scarf,Vector3(0,0.82,0),_torso).scale = Vector3(1.0,0.7,0.82)

	_head.position = Vector3(0, 1.01, 0)
	_torso.add_child(_head)
	var face := _mesh_capsule(0.19,0.34,skin,Vector3(0,0,0),_head)
	face.scale.z = 0.90
	_mesh_capsule(0.205,0.15,hat,Vector3(0,0.18,0),_head).scale.y = 0.72
	_mesh_box(Vector3(0.22,0.045,0.11),hat,Vector3(0,0.13,0.17),_head)

	_left_arm = _limb(_torso,-0.36,0.72,coat,gloves)
	_right_arm = _limb(_torso,0.36,0.72,coat,gloves)
	_left_leg = _leg(_hips,-0.15,pants,boots)
	_right_leg = _leg(_hips,0.15,pants,boots)

func _limb(parent: Node3D, x: float, y: float, sleeve: Material, glove: Material) -> Node3D:
	var pivot := Node3D.new()
	pivot.position = Vector3(x,y,0)
	parent.add_child(pivot)
	_mesh_capsule(0.085,0.56,sleeve,Vector3(0,-0.27,0),pivot)
	_mesh_capsule(0.09,0.17,glove,Vector3(0,-0.61,0),pivot)
	return pivot

func _leg(parent: Node3D, x: float, pants: Material, boots: Material) -> Node3D:
	var pivot := Node3D.new()
	pivot.position = Vector3(x,0,0)
	parent.add_child(pivot)
	_mesh_capsule(0.105,0.68,pants,Vector3(0,-0.34,0),pivot)
	_mesh_box(Vector3(0.20,0.15,0.34),boots,Vector3(0,-0.73,0.09),pivot)
	return pivot

func _animate(delta: float) -> void:
	var speed := Vector2(velocity.x,velocity.z).length()
	var moving := speed > 0.12
	_phase += delta * (8.4 if moving else 1.8)
	if not moving:
		_visual.position.y = lerp(_visual.position.y, sin(_phase)*0.006, 0.08)
		_left_arm.rotation.x = lerp_angle(_left_arm.rotation.x,0.04,0.10)
		_right_arm.rotation.x = lerp_angle(_right_arm.rotation.x,-0.04,0.10)
		_left_leg.rotation.x = lerp_angle(_left_leg.rotation.x,0.0,0.10)
		_right_leg.rotation.x = lerp_angle(_right_leg.rotation.x,0.0,0.10)
		return
	var swing := sin(_phase)
	var stride := clampf(speed/run_speed,0.45,1.0)*0.70
	_left_leg.rotation.x = lerp_angle(_left_leg.rotation.x,-swing*stride,0.24)
	_right_leg.rotation.x = lerp_angle(_right_leg.rotation.x,swing*stride,0.24)
	_left_arm.rotation.x = lerp_angle(_left_arm.rotation.x,swing*stride*0.72,0.22)
	_right_arm.rotation.x = lerp_angle(_right_arm.rotation.x,-swing*stride*0.72,0.22)
	_visual.position.y = -abs(sin(_phase))*0.025
	_torso.rotation.x = lerp_angle(_torso.rotation.x,0.045,0.16)
	_torso.rotation.y = lerp_angle(_torso.rotation.y,-swing*0.035,0.16)
	_head.rotation.y = lerp_angle(_head.rotation.y,swing*0.02,0.14)

func _mat(color: Color, roughness: float) -> StandardMaterial3D:
	var m := StandardMaterial3D.new()
	m.albedo_color = color
	m.roughness = roughness
	return m

func _mesh_box(size: Vector3, material: Material, pos: Vector3, parent: Node) -> MeshInstance3D:
	var node := MeshInstance3D.new()
	var mesh := BoxMesh.new()
	mesh.size = size
	node.mesh = mesh
	node.material_override = material
	node.position = pos
	node.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_ON
	parent.add_child(node)
	return node

func _mesh_capsule(radius: float, height: float, material: Material, pos: Vector3, parent: Node) -> MeshInstance3D:
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
