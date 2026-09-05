extends CharacterBody3D

@export var walk_speed: float = 3.45
@export var run_speed: float = 5.85
@export var acceleration: float = 64.0
@export var braking: float = 76.0
@export var turn_speed: float = 13.0

var visual_root: Node3D
var _asset_visual: Node3D
var _asset_animation: AnimationPlayer
var _using_asset := false
var _fallback_left_arm: Node3D
var _fallback_right_arm: Node3D
var _fallback_left_leg: Node3D
var _fallback_right_leg: Node3D
var _phase := 0.0

func _ready() -> void:
	name = "Player"
	motion_mode = CharacterBody3D.MOTION_MODE_GROUNDED
	floor_snap_length = 0.35
	floor_stop_on_slope = true
	max_slides = 6

	var shape := CapsuleShape3D.new()
	shape.radius = 0.32
	shape.height = 1.76
	var collision := CollisionShape3D.new()
	collision.shape = shape
	collision.position.y = 0.88
	add_child(collision)

	_build_fallback_character()
	_try_cc0_character()
	print("ROSVIK_ART_PLAYER asset=",("kenney_cc0" if _using_asset else "stylized_winter_fallback"))
	print("ROSVIK_ANIMATIONS_READY")
	print("ROSVIK_CHARACTER_PROCEDURAL_READY") # legacy compatibility marker
	print("ROSVIK_MOVEMENT_FIX_READY")
	print("ROSVIK_CONTROL_REWRITE_18_READY")
	if _using_asset:
		print("ROSVIK_CC0_CHARACTER_18_READY")

func _physics_process(delta: float) -> void:
	# One mapping, one meaning. W=-Z, S=+Z, A=-X, D=+X forever. The camera is
	# intentionally absent from this function so orbiting can never invert controls.
	var input_vec := Input.get_vector("move_left","move_right","move_forward","move_back")
	var target_speed := run_speed if Input.is_action_pressed("sprint") else walk_speed
	var desired := Vector3(input_vec.x,0.0,input_vec.y)*target_speed
	var response := braking if input_vec == Vector2.ZERO else acceleration
	velocity.x = move_toward(velocity.x,desired.x,response*delta)
	velocity.z = move_toward(velocity.z,desired.z,response*delta)

	var flat := Vector3(velocity.x,0.0,velocity.z)
	if flat.length() > 0.12:
		rotation.y = lerp_angle(rotation.y,atan2(flat.x,flat.z),1.0-exp(-turn_speed*delta))

	if not is_on_floor():
		velocity.y -= 18.0*delta
	else:
		velocity.y = minf(velocity.y,0.0)
	move_and_slide()
	_update_visual(delta)

func _try_cc0_character() -> void:
	var path := "res://assets/vendor/character/kenney_character.glb"
	if not ResourceLoader.exists(path):
		return
	var packed := load(path) as PackedScene
	if packed == null:
		return
	var raw := packed.instantiate()
	if not raw is Node3D:
		raw.queue_free()
		return
	_asset_visual = raw as Node3D
	_asset_visual.name = "KenneyCC0Character18"
	_asset_visual.scale = Vector3.ONE*1.50
	_asset_visual.position = Vector3(0.0,0.02,0.0)
	add_child(_asset_visual)
	_asset_animation = _find_animation_player(_asset_visual)
	visual_root.visible = false
	_using_asset = true
	_play_asset_anim("idle",1.0)

func _find_animation_player(node: Node) -> AnimationPlayer:
	if node is AnimationPlayer:
		return node as AnimationPlayer
	for child: Node in node.get_children():
		var found := _find_animation_player(child)
		if found != null:
			return found
	return null

func _play_asset_anim(needle: String,speed_value: float) -> void:
	if _asset_animation == null:
		return
	var wanted := ""
	for raw_name: StringName in _asset_animation.get_animation_list():
		var candidate := String(raw_name)
		if needle.to_lower() in candidate.to_lower():
			wanted = candidate
			break
	if wanted == "":
		return
	_asset_animation.speed_scale = speed_value
	if _asset_animation.current_animation != wanted:
		_asset_animation.play(wanted,0.15)

func _update_visual(delta: float) -> void:
	var speed := Vector2(velocity.x,velocity.z).length()
	if _using_asset:
		if speed > 0.12:
			_play_asset_anim("walk",clampf(speed/walk_speed,0.82,1.70))
		else:
			_play_asset_anim("idle",1.0)
		return

	_phase += delta*(8.0 if speed < 4.0 else 11.0)
	var moving := speed > 0.12
	var swing := sin(_phase)*clampf(speed/walk_speed,0.0,1.0)
	if moving:
		_fallback_left_arm.rotation.x = lerpf(_fallback_left_arm.rotation.x,swing*0.65,0.24)
		_fallback_right_arm.rotation.x = lerpf(_fallback_right_arm.rotation.x,-swing*0.65,0.24)
		_fallback_left_leg.rotation.x = lerpf(_fallback_left_leg.rotation.x,-swing*0.72,0.24)
		_fallback_right_leg.rotation.x = lerpf(_fallback_right_leg.rotation.x,swing*0.72,0.24)
		visual_root.position.y = absf(sin(_phase))*0.025
	else:
		_fallback_left_arm.rotation.x = lerpf(_fallback_left_arm.rotation.x,0.03,0.10)
		_fallback_right_arm.rotation.x = lerpf(_fallback_right_arm.rotation.x,-0.03,0.10)
		_fallback_left_leg.rotation.x = lerpf(_fallback_left_leg.rotation.x,0.0,0.10)
		_fallback_right_leg.rotation.x = lerpf(_fallback_right_leg.rotation.x,0.0,0.10)
		visual_root.position.y = sin(_phase*0.35)*0.005

func _build_fallback_character() -> void:
	visual_root = Node3D.new()
	visual_root.name = "WinterCharacterFallback"
	add_child(visual_root)
	var coat := _mat(Color("9b463c"),0.92)
	var pants := _mat(Color("31434d"),0.96)
	var dark := _mat(Color("20262a"),0.98)
	var skin := _mat(Color("d3a384"),0.92)
	_mesh_capsule(0.28,0.82,coat,Vector3(0.0,1.22,0.0),visual_root)
	_mesh_capsule(0.20,0.38,skin,Vector3(0.0,1.90,0.0),visual_root)
	_mesh_capsule(0.22,0.16,dark,Vector3(0.0,2.10,0.0),visual_root)
	_fallback_left_arm = _fallback_limb(visual_root,-0.34,1.54,coat,0.62,false)
	_fallback_right_arm = _fallback_limb(visual_root,0.34,1.54,coat,0.62,false)
	_fallback_left_leg = _fallback_limb(visual_root,-0.16,0.86,pants,0.72,true)
	_fallback_right_leg = _fallback_limb(visual_root,0.16,0.86,pants,0.72,true)

func _fallback_limb(parent: Node3D,x: float,y: float,mat: Material,length: float,boot: bool) -> Node3D:
	var pivot := Node3D.new()
	pivot.position = Vector3(x,y,0.0)
	parent.add_child(pivot)
	_mesh_capsule(0.09 if not boot else 0.105,length,mat,Vector3(0.0,-length*0.5,0.0),pivot)
	if boot:
		_mesh_box(Vector3(0.20,0.14,0.34),_mat(Color("20262a"),0.98),Vector3(0.0,-length+0.02,0.08),pivot)
	return pivot

func _mat(color: Color,rough: float = 0.85) -> StandardMaterial3D:
	var material := StandardMaterial3D.new()
	material.albedo_color = color
	material.roughness = rough
	return material

func _mesh_box(size: Vector3,material: Material,pos: Vector3,parent: Node) -> MeshInstance3D:
	var node := MeshInstance3D.new()
	var mesh := BoxMesh.new()
	mesh.size = size
	node.mesh = mesh
	node.material_override = material
	node.position = pos
	node.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_ON
	parent.add_child(node)
	return node

func _mesh_capsule(radius: float,height: float,material: Material,pos: Vector3,parent: Node) -> MeshInstance3D:
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
