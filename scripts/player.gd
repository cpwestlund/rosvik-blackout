extends CharacterBody3D

@export var walk_speed: float = 3.2
@export var run_speed: float = 5.4
@export var acceleration: float = 10.5
@export var turn_speed: float = 10.0

var _visual: Node3D = Node3D.new()
var _left_arm: Node3D = Node3D.new()
var _right_arm: Node3D = Node3D.new()
var _left_leg: Node3D = Node3D.new()
var _right_leg: Node3D = Node3D.new()
var _phase: float = 0.0
var _smoothed_dir: Vector3 = Vector3.ZERO

func _ready() -> void:
	name = "Player"
	_build_visual()
	var shape: CapsuleShape3D = CapsuleShape3D.new()
	shape.radius = 0.32
	shape.height = 1.72
	var collision: CollisionShape3D = CollisionShape3D.new()
	collision.shape = shape
	collision.position.y = 0.86
	add_child(collision)

func _physics_process(delta: float) -> void:
	var input_vec: Vector2 = Input.get_vector("move_left", "move_right", "move_forward", "move_back")
	var target_dir: Vector3 = Vector3.ZERO
	var active_camera: Camera3D = get_viewport().get_camera_3d()
	if input_vec.length() > 0.01:
		if active_camera != null:
			var cam_forward: Vector3 = -active_camera.global_transform.basis.z
			cam_forward.y = 0.0
			cam_forward = cam_forward.normalized()
			var cam_right: Vector3 = active_camera.global_transform.basis.x
			cam_right.y = 0.0
			cam_right = cam_right.normalized()
			target_dir = cam_right * input_vec.x + cam_forward * -input_vec.y
		else:
			target_dir = Vector3(input_vec.x,0.0,input_vec.y)
		if target_dir.length() > 0.01:
			target_dir = target_dir.normalized()

	# Smooth the requested world direction so reversing or changing WASD axes does
	# not flip facing back and forth for a few frames.
	var response: float = 1.0 - exp(-13.0 * delta)
	_smoothed_dir = _smoothed_dir.lerp(target_dir,response)
	if target_dir == Vector3.ZERO and _smoothed_dir.length() < 0.025:
		_smoothed_dir = Vector3.ZERO
	elif _smoothed_dir.length() > 1.0:
		_smoothed_dir = _smoothed_dir.normalized()

	var target_speed: float = run_speed if Input.is_action_pressed("sprint") else walk_speed
	var desired_velocity: Vector3 = _smoothed_dir * target_speed
	velocity.x = move_toward(velocity.x, desired_velocity.x, acceleration * delta)
	velocity.z = move_toward(velocity.z, desired_velocity.z, acceleration * delta)

	# Face actual travel rather than raw input. This removes the visible twitch when
	# the player is still decelerating from the previous direction.
	var travel: Vector3 = Vector3(velocity.x,0.0,velocity.z)
	if travel.length() > 0.20:
		var target_yaw: float = atan2(travel.x,travel.z)
		rotation.y = lerp_angle(rotation.y,target_yaw,1.0-exp(-turn_speed*delta))

	if not is_on_floor():
		velocity.y -= 18.0 * delta
	move_and_slide()
	_animate(delta)

func _animate(delta: float) -> void:
	var speed: float = Vector2(velocity.x, velocity.z).length()
	var moving: bool = speed > 0.15
	_phase += delta * (8.0 if speed < 4.0 else 11.0)
	var swing: float = sin(_phase) * clampf(speed / walk_speed, 0.0, 1.0)
	var amp: float = 0.62 if speed < 4.0 else 0.9
	if moving:
		_left_arm.rotation.x = lerp(_left_arm.rotation.x, swing * amp, 0.22)
		_right_arm.rotation.x = lerp(_right_arm.rotation.x, -swing * amp, 0.22)
		_left_leg.rotation.x = lerp(_left_leg.rotation.x, -swing * amp, 0.22)
		_right_leg.rotation.x = lerp(_right_leg.rotation.x, swing * amp, 0.22)
		_visual.position.y = abs(sin(_phase)) * 0.035
	else:
		_left_arm.rotation.x = lerp(_left_arm.rotation.x, 0.05 + sin(_phase * 0.25) * 0.02, 0.08)
		_right_arm.rotation.x = lerp(_right_arm.rotation.x, -0.05 - sin(_phase * 0.25) * 0.02, 0.08)
		_left_leg.rotation.x = lerp(_left_leg.rotation.x, 0.0, 0.1)
		_right_leg.rotation.x = lerp(_right_leg.rotation.x, 0.0, 0.1)
		_visual.position.y = sin(_phase * 0.35) * 0.006

func _mat(color: Color, rough: float = 0.82) -> StandardMaterial3D:
	var m: StandardMaterial3D = StandardMaterial3D.new()
	m.albedo_color = color
	m.roughness = rough
	return m

func _mesh_box(size: Vector3, color: Color, parent: Node3D, pos: Vector3 = Vector3.ZERO) -> MeshInstance3D:
	var n: MeshInstance3D = MeshInstance3D.new()
	var mesh: BoxMesh = BoxMesh.new()
	mesh.size = size
	n.mesh = mesh
	n.material_override = _mat(color)
	n.position = pos
	n.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_ON
	parent.add_child(n)
	return n

func _mesh_capsule(radius: float, height: float, color: Color, parent: Node3D, pos: Vector3 = Vector3.ZERO) -> MeshInstance3D:
	var n: MeshInstance3D = MeshInstance3D.new()
	var mesh: CapsuleMesh = CapsuleMesh.new()
	mesh.radius = radius
	mesh.height = height
	n.mesh = mesh
	n.material_override = _mat(color)
	n.position = pos
	n.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_ON
	parent.add_child(n)
	return n

func _limb(parent: Node3D, x: float, y: float, color: Color, length: float, boot: bool = false) -> Node3D:
	var pivot: Node3D = Node3D.new()
	pivot.position = Vector3(x, y, 0.0)
	parent.add_child(pivot)
	var radius: float = 0.105 if boot else 0.09
	_mesh_capsule(radius, length, color, pivot, Vector3(0.0, -length * 0.5, 0.0))
	if boot:
		_mesh_box(Vector3(0.2, 0.13, 0.34), Color("23272b"), pivot, Vector3(0.0, -length + 0.02, 0.08))
	return pivot

func _build_visual() -> void:
	add_child(_visual)
	var coat: Color = Color("a84b3c")
	var pants: Color = Color("344751")
	var skin: Color = Color("d6aa89")
	var dark: Color = Color("263139")
	_mesh_box(Vector3(0.62, 0.86, 0.38), coat, _visual, Vector3(0.0, 1.25, 0.0))
	_mesh_box(Vector3(0.45, 0.56, 0.18), Color("506557"), _visual, Vector3(0.0, 1.25, -0.28))
	_mesh_capsule(0.23, 0.46, skin, _visual, Vector3(0.0, 1.93, 0.0))
	var hat: MeshInstance3D = _mesh_capsule(0.235, 0.17, dark, _visual, Vector3(0.0, 2.12, 0.0))
	hat.scale.y = 0.65
	_left_arm = _limb(_visual, -0.4, 1.58, coat, 0.7)
	_right_arm = _limb(_visual, 0.4, 1.58, coat, 0.7)
	_left_leg = _limb(_visual, -0.17, 0.9, pants, 0.78, true)
	_right_leg = _limb(_visual, 0.17, 0.9, pants, 0.78, true)
