extends CharacterBody3D

@export var walk_speed: float = 3.0
@export var run_speed: float = 5.2
@export var acceleration: float = 10.0
@export var turn_speed: float = 11.0

var visual_root: Node3D
var skeleton: Skeleton3D
var loaded_art_character: bool = false
var animation_set_ready: bool = false
var _fallback_visual: Node3D
var _phase: float = 0.0
var _left_arm: Node3D
var _right_arm: Node3D
var _left_leg: Node3D
var _right_leg: Node3D
var _bone_left_arm: int = -1
var _bone_right_arm: int = -1
var _bone_left_leg: int = -1
var _bone_right_leg: int = -1
var _bone_left_knee: int = -1
var _bone_right_knee: int = -1

func _ready() -> void:
	name = "Player"
	motion_mode = CharacterBody3D.MOTION_MODE_GROUNDED
	floor_snap_length = 0.35
	floor_stop_on_slope = true
	max_slides = 6
	var shape: CapsuleShape3D = CapsuleShape3D.new()
	shape.radius = 0.32
	shape.height = 1.72
	var collision: CollisionShape3D = CollisionShape3D.new()
	collision.shape = shape
	collision.position.y = 0.86
	add_child(collision)
	loaded_art_character = _build_art_character()
	if not loaded_art_character:
		_build_fallback_visual()
	print("ROSVIK_ART_PLAYER asset=", loaded_art_character, " procedural_rig=", animation_set_ready)
	if loaded_art_character and animation_set_ready:
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
	_update_animation(delta, sprinting)

func _build_art_character() -> bool:
	var character_res: Resource = load("res://assets/character/night-striker.glb")
	if character_res == null or not character_res is PackedScene:
		return false
	visual_root = (character_res as PackedScene).instantiate() as Node3D
	if visual_root == null:
		return false
	visual_root.name = "RiggedCharacter"
	visual_root.rotation.y = 0.0
	add_child(visual_root)
	# The previous build copied animation tracks from another GLB directly onto this
	# skeleton. They loaded, but were not retargeted, which folded the character into
	# a crawling pose. Keep this mesh on its own rest rig and animate matching bones
	# procedurally until a true retarget pipeline is added.
	var imported_player: AnimationPlayer = _find_animation_player(visual_root)
	if imported_player != null:
		imported_player.stop()
	skeleton = _find_skeleton(visual_root)
	if skeleton != null:
		_resolve_locomotion_bones()
		animation_set_ready = true
	else:
		animation_set_ready = false
	# No floating backpack/scarf primitives here. Equipment will return only when it
	# is attached to an actual skeleton bone/socket.
	return true

func _find_animation_player(node: Node) -> AnimationPlayer:
	if node is AnimationPlayer:
		return node as AnimationPlayer
	for child: Node in node.get_children():
		var found: AnimationPlayer = _find_animation_player(child)
		if found != null:
			return found
	return null

func _find_skeleton(node: Node) -> Skeleton3D:
	if node is Skeleton3D:
		return node as Skeleton3D
	for child: Node in node.get_children():
		var found: Skeleton3D = _find_skeleton(child)
		if found != null:
			return found
	return null

func _normal_name(value: String) -> String:
	return value.to_lower().replace("_", "").replace("-", "").replace(".", "").replace(" ", "")

func _find_bone(candidates: Array[String]) -> int:
	if skeleton == null:
		return -1
	for candidate: String in candidates:
		var wanted: String = _normal_name(candidate)
		for i: int in range(skeleton.get_bone_count()):
			var bone_name: String = _normal_name(String(skeleton.get_bone_name(i)))
			if bone_name == wanted or bone_name.contains(wanted):
				return i
	return -1

func _resolve_locomotion_bones() -> void:
	_bone_left_arm = _find_bone(["leftupperarm", "leftarm", "upperarml", "lupperarm"])
	_bone_right_arm = _find_bone(["rightupperarm", "rightarm", "upperarmr", "rupperarm"])
	_bone_left_leg = _find_bone(["leftupleg", "leftupperleg", "leftthigh", "thighl", "upperlegl"])
	_bone_right_leg = _find_bone(["rightupleg", "rightupperleg", "rightthigh", "thighr", "upperlegr"])
	_bone_left_knee = _find_bone(["leftleg", "leftlowerleg", "leftshin", "calfl", "lowerlegl"])
	_bone_right_knee = _find_bone(["rightleg", "rightlowerleg", "rightshin", "calfr", "lowerlegr"])
	print("ROSVIK_BONES armL=", _bone_left_arm, " armR=", _bone_right_arm, " legL=", _bone_left_leg, " legR=", _bone_right_leg)

func _update_animation(delta: float, sprinting: bool) -> void:
	var speed: float = Vector2(velocity.x, velocity.z).length()
	if loaded_art_character and skeleton != null:
		_animate_rig(delta, speed, sprinting)
		return
	_animate_fallback(delta)

func _animate_rig(delta: float, speed: float, sprinting: bool) -> void:
	_phase += delta * (2.1 if speed < 0.12 else (10.2 if sprinting else 7.2))
	skeleton.reset_bone_poses()
	var moving: bool = speed > 0.12
	if not moving:
		visual_root.position.y = lerp(visual_root.position.y, sin(_phase) * 0.006, 0.08)
		visual_root.rotation.x = lerp(visual_root.rotation.x, 0.0, 0.12)
		visual_root.rotation.z = lerp(visual_root.rotation.z, 0.0, 0.12)
		return
	var amount: float = 0.42 if not sprinting else 0.62
	var swing: float = sin(_phase) * amount
	var knee_l: float = max(0.0, -sin(_phase)) * (0.35 if not sprinting else 0.52)
	var knee_r: float = max(0.0, sin(_phase)) * (0.35 if not sprinting else 0.52)
	_set_bone_x(_bone_left_arm, -swing * 0.72)
	_set_bone_x(_bone_right_arm, swing * 0.72)
	_set_bone_x(_bone_left_leg, swing)
	_set_bone_x(_bone_right_leg, -swing)
	_set_bone_x(_bone_left_knee, knee_l)
	_set_bone_x(_bone_right_knee, knee_r)
	visual_root.position.y = abs(sin(_phase)) * (0.018 if not sprinting else 0.028)
	visual_root.rotation.x = lerp(visual_root.rotation.x, 0.04 if not sprinting else 0.085, 0.16)
	visual_root.rotation.z = lerp(visual_root.rotation.z, -velocity.x * 0.008, 0.14)

func _set_bone_x(index: int, angle: float) -> void:
	if skeleton == null or index < 0:
		return
	skeleton.set_bone_pose_rotation(index, Quaternion(Vector3.RIGHT, angle))

func _mat(color: Color, rough: float = 0.85) -> StandardMaterial3D:
	var material: StandardMaterial3D = StandardMaterial3D.new()
	material.albedo_color = color
	material.roughness = rough
	return material

func _mesh_box(size: Vector3, material: Material, pos: Vector3, parent: Node = self) -> MeshInstance3D:
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

func _limb(parent: Node3D, x: float, y: float, material: Material, length: float, boot: bool = false) -> Node3D:
	var pivot: Node3D = Node3D.new()
	pivot.position = Vector3(x, y, 0.0)
	parent.add_child(pivot)
	_mesh_capsule(0.095, length, material, Vector3(0.0, -length * 0.5, 0.0), pivot)
	if boot:
		_mesh_box(Vector3(0.20, 0.13, 0.34), _mat(Color("23272b"), 0.96), Vector3(0.0, -length + 0.02, 0.08), pivot)
	return pivot

func _build_fallback_visual() -> void:
	_fallback_visual = Node3D.new()
	add_child(_fallback_visual)
	var coat: StandardMaterial3D = _mat(Color("93483c"), 0.92)
	var pants: StandardMaterial3D = _mat(Color("344751"), 0.94)
	var skin: StandardMaterial3D = _mat(Color("d6aa89"), 0.9)
	var dark: StandardMaterial3D = _mat(Color("263139"), 0.94)
	_mesh_box(Vector3(0.60, 0.84, 0.36), coat, Vector3(0.0, 1.25, 0.0), _fallback_visual)
	_mesh_capsule(0.22, 0.44, skin, Vector3(0.0, 1.92, 0.0), _fallback_visual)
	_mesh_capsule(0.23, 0.16, dark, Vector3(0.0, 2.10, 0.0), _fallback_visual)
	_left_arm = _limb(_fallback_visual, -0.38, 1.58, coat, 0.70)
	_right_arm = _limb(_fallback_visual, 0.38, 1.58, coat, 0.70)
	_left_leg = _limb(_fallback_visual, -0.17, 0.90, pants, 0.78, true)
	_right_leg = _limb(_fallback_visual, 0.17, 0.90, pants, 0.78, true)

func _animate_fallback(delta: float) -> void:
	if _fallback_visual == null:
		return
	var speed: float = Vector2(velocity.x, velocity.z).length()
	_phase += delta * (7.5 if speed < 4.0 else 10.5)
	var moving: bool = speed > 0.15
	var swing: float = sin(_phase) * (0.55 if speed < 4.0 else 0.82)
	if moving:
		_left_arm.rotation.x = lerp(_left_arm.rotation.x, swing, 0.22)
		_right_arm.rotation.x = lerp(_right_arm.rotation.x, -swing, 0.22)
		_left_leg.rotation.x = lerp(_left_leg.rotation.x, -swing, 0.22)
		_right_leg.rotation.x = lerp(_right_leg.rotation.x, swing, 0.22)
		_fallback_visual.position.y = abs(sin(_phase)) * 0.025
	else:
		_left_arm.rotation.x = lerp(_left_arm.rotation.x, 0.03, 0.08)
		_right_arm.rotation.x = lerp(_right_arm.rotation.x, -0.03, 0.08)
		_left_leg.rotation.x = lerp(_left_leg.rotation.x, 0.0, 0.1)
		_right_leg.rotation.x = lerp(_right_leg.rotation.x, 0.0, 0.1)
		_fallback_visual.position.y = sin(_phase * 0.3) * 0.005
