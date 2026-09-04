extends CharacterBody3D

@export var walk_speed: float = 3.2
@export var run_speed: float = 5.4
@export var acceleration: float = 9.0
@export var turn_speed: float = 12.0

var _visual: Node3D = Node3D.new()
var _left_arm: Node3D = Node3D.new()
var _right_arm: Node3D = Node3D.new()
var _left_leg: Node3D = Node3D.new()
var _right_leg: Node3D = Node3D.new()
var _phase: float = 0.0

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
	var dir: Vector3 = Vector3(input_vec.x, 0.0, input_vec.y)
	var target_speed: float = run_speed if Input.is_action_pressed("sprint") else walk_speed
	if dir.length() > 0.01:
		dir = dir.normalized()
		velocity.x = move_toward(velocity.x, dir.x * target_speed, acceleration * delta)
		velocity.z = move_toward(velocity.z, dir.z * target_speed, acceleration * delta)
		var target_yaw: float = atan2(dir.x, dir.z)
		rotation.y = lerp_angle(rotation.y, target_yaw, 1.0 - exp(-turn_speed * delta))
	else:
		velocity.x = move_toward(velocity.x, 0.0, acceleration * delta)
		velocity.z = move_toward(velocity.z, 0.0, acceleration * delta)
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
