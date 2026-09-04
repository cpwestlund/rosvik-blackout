extends CharacterBody3D

@export var walk_speed: float = 3.0
@export var run_speed: float = 5.2
@export var acceleration: float = 10.0
@export var turn_speed: float = 11.0

var visual_root: Node3D
var animation_player: AnimationPlayer
var current_clip: String = ""
var loaded_art_character: bool = false
var animation_set_ready: bool = false
var _fallback_visual: Node3D
var _phase: float = 0.0
var _left_arm: Node3D
var _right_arm: Node3D
var _left_leg: Node3D
var _right_leg: Node3D

func _ready() -> void:
	name = "Player"
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
	print("ROSVIK_ART_PLAYER asset=", loaded_art_character, " animations=", animation_set_ready)
	if loaded_art_character and animation_set_ready:
		print("ROSVIK_ANIMATIONS_READY")

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
	visual_root.rotation.y = PI
	add_child(visual_root)
	animation_player = _find_animation_player(visual_root)
	if animation_player == null:
		animation_player = AnimationPlayer.new()
		animation_player.name = "AnimationPlayer"
		visual_root.add_child(animation_player)
	animation_set_ready = _merge_external_animation_library()
	_add_winter_kit()
	return true

func _merge_external_animation_library() -> bool:
	var anim_res: Resource = load("res://assets/character/universal-animation-library.glb")
	if anim_res == null or not anim_res is PackedScene:
		return false
	var anim_scene: Node = (anim_res as PackedScene).instantiate()
	if anim_scene == null:
		return false
	var source: AnimationPlayer = _find_animation_player(anim_scene)
	if source == null:
		anim_scene.free()
		return false
	var copied: AnimationLibrary = AnimationLibrary.new()
	for library_name: StringName in source.get_animation_library_list():
		var lib: AnimationLibrary = source.get_animation_library(library_name)
		if lib == null:
			continue
		for animation_name: StringName in lib.get_animation_list():
			var clean_name: String = String(animation_name)
			if not copied.has_animation(clean_name):
				var anim: Animation = lib.get_animation(animation_name)
				if anim != null:
					copied.add_animation(clean_name, anim.duplicate(true))
	if animation_player.has_animation_library("ual"):
		animation_player.remove_animation_library("ual")
	animation_player.add_animation_library("ual", copied)
	anim_scene.free()
	var idle_ok: bool = _find_clip(["Idle"]) != ""
	var jog_ok: bool = _find_clip(["Jog_Fwd", "Walk"]) != ""
	var sprint_ok: bool = _find_clip(["Sprint"]) != ""
	return idle_ok and jog_ok and sprint_ok

func _find_animation_player(node: Node) -> AnimationPlayer:
	if node is AnimationPlayer:
		return node as AnimationPlayer
	for child: Node in node.get_children():
		var found: AnimationPlayer = _find_animation_player(child)
		if found != null:
			return found
	return null

func _find_clip(candidates: Array[String]) -> String:
	if animation_player == null:
		return ""
	var names: PackedStringArray = animation_player.get_animation_list()
	for candidate: String in candidates:
		for full_name: String in names:
			if full_name == candidate or full_name.ends_with("/" + candidate) or full_name.ends_with(candidate):
				return full_name
	return ""

func _update_animation(delta: float, sprinting: bool) -> void:
	var speed: float = Vector2(velocity.x, velocity.z).length()
	if loaded_art_character and animation_set_ready and animation_player != null:
		var wanted: String = ""
		if speed < 0.12:
			wanted = _find_clip(["Idle"])
		elif sprinting and speed > 3.8:
			wanted = _find_clip(["Sprint"])
		else:
			wanted = _find_clip(["Jog_Fwd", "Walk"])
		if wanted != "" and wanted != current_clip:
			animation_player.play(wanted, 0.18, 1.0)
			current_clip = wanted
		return
	_animate_fallback(delta)

func _add_winter_kit() -> void:
	var pack_mat: StandardMaterial3D = _mat(Color("40564a"), 0.92)
	var scarf_mat: StandardMaterial3D = _mat(Color("7e342d"), 0.9)
	var backpack: MeshInstance3D = _mesh_box(Vector3(0.44, 0.56, 0.18), pack_mat, Vector3(0.0, 1.18, 0.24))
	backpack.name = "WinterBackpack"
	var scarf: MeshInstance3D = _mesh_box(Vector3(0.34, 0.11, 0.30), scarf_mat, Vector3(0.0, 1.58, 0.0))
	scarf.name = "Scarf"

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
