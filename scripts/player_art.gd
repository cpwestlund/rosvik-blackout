extends CharacterBody3D

@export var walk_speed: float = 3.0
@export var run_speed: float = 5.2
@export var acceleration: float = 10.0
@export var turn_speed: float = 11.0

var visual_root: Node3D
var hips: Node3D
var torso: Node3D
var head: Node3D
var left_upper_arm: Node3D
var right_upper_arm: Node3D
var left_lower_arm: Node3D
var right_lower_arm: Node3D
var left_upper_leg: Node3D
var right_upper_leg: Node3D
var left_lower_leg: Node3D
var right_lower_leg: Node3D
var _phase: float = 0.0
var _last_speed: float = 0.0

func _ready() -> void:
	name = "Player"
	motion_mode = CharacterBody3D.MOTION_MODE_GROUNDED
	floor_snap_length = 0.35
	floor_stop_on_slope = true
	max_slides = 6

	var shape: CapsuleShape3D = CapsuleShape3D.new()
	shape.radius = 0.32
	shape.height = 1.76
	var collision: CollisionShape3D = CollisionShape3D.new()
	collision.shape = shape
	collision.position.y = 0.88
	add_child(collision)

	_build_stylized_winter_character()
	print("ROSVIK_ART_PLAYER asset=stylized_winter procedural_rig=true")
	print("ROSVIK_ANIMATIONS_READY")
	print("ROSVIK_CHARACTER_PROCEDURAL_READY")
	print("ROSVIK_MOVEMENT_FIX_READY")

func _physics_process(delta: float) -> void:
	var input_vec: Vector2 = Input.get_vector("move_left", "move_right", "move_forward", "move_back")
	var dir: Vector3 = Vector3(input_vec.x, 0.0, input_vec.y)
	var sprinting: bool = Input.is_action_pressed("sprint")
	var target_speed: float = run_speed if sprinting else walk_speed

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
	_update_character_animation(delta, sprinting)

func _build_stylized_winter_character() -> void:
	visual_root = Node3D.new()
	visual_root.name = "StylizedWinterCharacter"
	add_child(visual_root)

	var coat_mat: StandardMaterial3D = _mat(Color("9b463c"), 0.9)
	var coat_dark: StandardMaterial3D = _mat(Color("79362f"), 0.94)
	var pants_mat: StandardMaterial3D = _mat(Color("31434d"), 0.95)
	var boot_mat: StandardMaterial3D = _mat(Color("20262a"), 0.98)
	var skin_mat: StandardMaterial3D = _mat(Color("d3a384"), 0.92)
	var hat_mat: StandardMaterial3D = _mat(Color("394b55"), 0.92)
	var glove_mat: StandardMaterial3D = _mat(Color("242a2e"), 0.98)
	var pack_mat: StandardMaterial3D = _mat(Color("465c4e"), 0.94)
	var scarf_mat: StandardMaterial3D = _mat(Color("bc6d42"), 0.9)

	hips = Node3D.new()
	hips.name = "Hips"
	hips.position = Vector3(0.0, 0.86, 0.0)
	visual_root.add_child(hips)
	_mesh_box(Vector3(0.48, 0.25, 0.31), pants_mat, Vector3(0.0, 0.11, 0.0), hips)

	torso = Node3D.new()
	torso.name = "Torso"
	torso.position = Vector3(0.0, 0.20, 0.0)
	hips.add_child(torso)
	_mesh_box(Vector3(0.62, 0.75, 0.38), coat_mat, Vector3(0.0, 0.40, 0.0), torso)
	_mesh_box(Vector3(0.52, 0.18, 0.40), coat_dark, Vector3(0.0, 0.12, 0.0), torso)
	# Backpack is parented to torso, so it follows the body instead of floating on the head.
	_mesh_box(Vector3(0.42, 0.54, 0.18), pack_mat, Vector3(0.0, 0.43, -0.29), torso)
	_mesh_box(Vector3(0.35, 0.10, 0.34), scarf_mat, Vector3(0.0, 0.84, 0.0), torso)

	head = Node3D.new()
	head.name = "Head"
	head.position = Vector3(0.0, 1.02, 0.0)
	torso.add_child(head)
	_mesh_capsule(0.20, 0.38, skin_mat, Vector3(0.0, 0.0, 0.0), head)
	_mesh_capsule(0.22, 0.15, hat_mat, Vector3(0.0, 0.21, 0.0), head)
	_mesh_box(Vector3(0.28, 0.05, 0.10), hat_mat, Vector3(0.0, 0.13, 0.18), head)

	left_upper_arm = _build_arm(torso, -0.39, 0.72, coat_mat, glove_mat, true)
	right_upper_arm = _build_arm(torso, 0.39, 0.72, coat_mat, glove_mat, false)
	left_upper_leg = _build_leg(hips, -0.16, 0.0, pants_mat, boot_mat)
	right_upper_leg = _build_leg(hips, 0.16, 0.0, pants_mat, boot_mat)

func _build_arm(parent: Node3D, x: float, y: float, sleeve_mat: Material, glove_mat: Material, is_left: bool) -> Node3D:
	var upper: Node3D = Node3D.new()
	upper.position = Vector3(x, y, 0.0)
	parent.add_child(upper)
	_mesh_capsule(0.085, 0.37, sleeve_mat, Vector3(0.0, -0.18, 0.0), upper)

	var lower: Node3D = Node3D.new()
	lower.position = Vector3(0.0, -0.36, 0.0)
	upper.add_child(lower)
	_mesh_capsule(0.075, 0.36, sleeve_mat, Vector3(0.0, -0.17, 0.0), lower)
	_mesh_capsule(0.085, 0.17, glove_mat, Vector3(0.0, -0.39, 0.0), lower)

	if is_left:
		left_lower_arm = lower
	else:
		right_lower_arm = lower
	return upper

func _build_leg(parent: Node3D, x: float, y: float, pants_mat: Material, boot_mat: Material) -> Node3D:
	var upper: Node3D = Node3D.new()
	upper.position = Vector3(x, y, 0.0)
	parent.add_child(upper)
	_mesh_capsule(0.105, 0.46, pants_mat, Vector3(0.0, -0.23, 0.0), upper)

	var lower: Node3D = Node3D.new()
	lower.position = Vector3(0.0, -0.45, 0.0)
	upper.add_child(lower)
	_mesh_capsule(0.095, 0.43, pants_mat, Vector3(0.0, -0.21, 0.0), lower)
	_mesh_box(Vector3(0.20, 0.14, 0.36), boot_mat, Vector3(0.0, -0.44, 0.10), lower)

	if left_lower_leg == null:
		left_lower_leg = lower
	else:
		right_lower_leg = lower
	return upper

func _update_character_animation(delta: float, sprinting: bool) -> void:
	if visual_root == null:
		return
	var speed: float = Vector2(velocity.x, velocity.z).length()
	var normalized_speed: float = clamp(speed / run_speed, 0.0, 1.0)
	var moving: bool = speed > 0.12
	var cadence: float = 2.0
	if moving:
		cadence = 7.2 if not sprinting else 10.6
	_phase += delta * cadence

	if not moving:
		var breathe: float = sin(_phase) * 0.012
		visual_root.position.y = lerp(visual_root.position.y, breathe, 0.10)
		hips.rotation.y = lerp_angle(hips.rotation.y, 0.0, 0.10)
		torso.rotation.x = lerp_angle(torso.rotation.x, 0.0, 0.10)
		torso.rotation.y = lerp_angle(torso.rotation.y, breathe * 0.8, 0.08)
		head.rotation.y = lerp_angle(head.rotation.y, -breathe * 0.7, 0.08)
		_set_idle_limbs()
		_last_speed = speed
		return

	var cycle: float = sin(_phase)
	var opposite: float = sin(_phase + PI)
	var stride: float = lerp(0.48, 0.78, normalized_speed)
	var arm_stride: float = stride * 0.72
	var knee_amount: float = lerp(0.42, 0.68, normalized_speed)
	var bob_amount: float = lerp(0.018, 0.032, normalized_speed)
	var lean: float = lerp(0.035, 0.10, normalized_speed)

	left_upper_leg.rotation.x = lerp_angle(left_upper_leg.rotation.x, -cycle * stride, 0.24)
	right_upper_leg.rotation.x = lerp_angle(right_upper_leg.rotation.x, -opposite * stride, 0.24)
	left_lower_leg.rotation.x = lerp_angle(left_lower_leg.rotation.x, max(0.0, cycle) * knee_amount, 0.24)
	right_lower_leg.rotation.x = lerp_angle(right_lower_leg.rotation.x, max(0.0, opposite) * knee_amount, 0.24)

	left_upper_arm.rotation.x = lerp_angle(left_upper_arm.rotation.x, opposite * arm_stride, 0.22)
	right_upper_arm.rotation.x = lerp_angle(right_upper_arm.rotation.x, cycle * arm_stride, 0.22)
	left_lower_arm.rotation.x = lerp_angle(left_lower_arm.rotation.x, -0.12 + max(0.0, -opposite) * 0.18, 0.18)
	right_lower_arm.rotation.x = lerp_angle(right_lower_arm.rotation.x, -0.12 + max(0.0, -cycle) * 0.18, 0.18)

	var bob: float = abs(sin(_phase)) * bob_amount
	visual_root.position.y = lerp(visual_root.position.y, -bob, 0.28)
	hips.rotation.y = lerp_angle(hips.rotation.y, cycle * 0.045, 0.16)
	torso.rotation.x = lerp_angle(torso.rotation.x, lean, 0.18)
	torso.rotation.y = lerp_angle(torso.rotation.y, -cycle * 0.055, 0.16)
	head.rotation.y = lerp_angle(head.rotation.y, cycle * 0.025, 0.14)
	visual_root.rotation.z = lerp_angle(visual_root.rotation.z, -velocity.x * 0.005, 0.12)
	_last_speed = speed

func _set_idle_limbs() -> void:
	left_upper_leg.rotation.x = lerp_angle(left_upper_leg.rotation.x, 0.0, 0.12)
	right_upper_leg.rotation.x = lerp_angle(right_upper_leg.rotation.x, 0.0, 0.12)
	left_lower_leg.rotation.x = lerp_angle(left_lower_leg.rotation.x, 0.0, 0.12)
	right_lower_leg.rotation.x = lerp_angle(right_lower_leg.rotation.x, 0.0, 0.12)
	left_upper_arm.rotation.x = lerp_angle(left_upper_arm.rotation.x, 0.035, 0.10)
	right_upper_arm.rotation.x = lerp_angle(right_upper_arm.rotation.x, -0.035, 0.10)
	left_lower_arm.rotation.x = lerp_angle(left_lower_arm.rotation.x, -0.08, 0.10)
	right_lower_arm.rotation.x = lerp_angle(right_lower_arm.rotation.x, -0.08, 0.10)

func _mat(color: Color, rough: float = 0.85) -> StandardMaterial3D:
	var material: StandardMaterial3D = StandardMaterial3D.new()
	material.albedo_color = color
	material.roughness = rough
	return material

func _mesh_box(size: Vector3, material: Material, pos: Vector3, parent: Node) -> MeshInstance3D:
	var node: MeshInstance3D = MeshInstance3D.new()
	var mesh: BoxMesh = BoxMesh.new()
	mesh.size = size
	node.mesh = mesh
	node.material_override = material
	node.position = pos
	node.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_ON
	parent.add_child(node)
	return node

func _mesh_capsule(radius: float, height: float, material: Material, pos: Vector3, parent: Node) -> MeshInstance3D:
	var node: MeshInstance3D = MeshInstance3D.new()
	var mesh: CapsuleMesh = CapsuleMesh.new()
	mesh.radius = radius
	mesh.height = height
	node.mesh = mesh
	node.material_override = material
	node.position = pos
	node.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_ON
	parent.add_child(node)
	return node
