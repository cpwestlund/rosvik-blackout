extends CharacterBody3D

@export var walk_speed: float = 3.45
@export var run_speed: float = 5.85
@export var acceleration: float = 62.0
@export var braking: float = 72.0
@export var turn_speed: float = 12.5

var _visual: Node3D = Node3D.new()
var _left_arm: Node3D = Node3D.new()
var _right_arm: Node3D = Node3D.new()
var _left_leg: Node3D = Node3D.new()
var _right_leg: Node3D = Node3D.new()
var _phase: float = 0.0
var _asset_visual: Node3D
var _asset_animation: AnimationPlayer
var _using_asset_character: bool = false

func _ready() -> void:
	name = "Player"
	_build_visual()
	_try_cc0_character()
	var shape := CapsuleShape3D.new()
	shape.radius = 0.32
	shape.height = 1.72
	var collision := CollisionShape3D.new()
	collision.shape = shape
	collision.position.y = 0.86
	add_child(collision)
	print("ROSVIK_CONTROL_REWRITE_18_READY")
	if _using_asset_character:
		print("ROSVIK_CC0_CHARACTER_18_READY")

func _physics_process(delta: float) -> void:
	# Deliberately boring controls: the camera is never consulted. W/A/S/D always
	# map to the same world axes. No cached direction, no screen projection and no
	# hidden remapping can make left become right after orbiting the camera.
	var input_vec := Input.get_vector("move_left","move_right","move_forward","move_back")
	var target_speed := run_speed if Input.is_action_pressed("sprint") else walk_speed
	var desired := Vector3(input_vec.x,0.0,input_vec.y) * target_speed
	var response := braking if input_vec == Vector2.ZERO else acceleration
	velocity.x = move_toward(velocity.x,desired.x,response*delta)
	velocity.z = move_toward(velocity.z,desired.z,response*delta)

	var travel := Vector3(velocity.x,0.0,velocity.z)
	if travel.length() > 0.14:
		var target_yaw := atan2(travel.x,travel.z)
		rotation.y = lerp_angle(rotation.y,target_yaw,1.0-exp(-turn_speed*delta))

	if not is_on_floor():
		velocity.y -= 18.0*delta
	else:
		velocity.y = minf(velocity.y,0.0)
	move_and_slide()
	_animate(delta)

func _try_cc0_character() -> void:
	var path := "res://assets/vendor/kenney_character.glb"
	if not ResourceLoader.exists(path):
		return
	var packed := load(path) as PackedScene
	if packed == null:
		return
	var instance := packed.instantiate()
	if not instance is Node3D:
		instance.queue_free()
		return
	_asset_visual = instance as Node3D
	_asset_visual.name = "KenneyCharacter18"
	_asset_visual.scale = Vector3.ONE * 1.50
	_asset_visual.position = Vector3(0.0,0.02,0.0)
	add_child(_asset_visual)
	_asset_animation = _find_animation_player(_asset_visual)
	_visual.visible = false
	_using_asset_character = true
	_play_asset_animation("idle",1.0)

func _find_animation_player(node: Node) -> AnimationPlayer:
	if node is AnimationPlayer:
		return node as AnimationPlayer
	for child: Node in node.get_children():
		var found := _find_animation_player(child)
		if found != null:
			return found
	return null

func _play_asset_animation(needle: String,speed_scale: float) -> void:
	if _asset_animation == null:
		return
	var wanted := ""
	for raw_name: StringName in _asset_animation.get_animation_list():
		var n := String(raw_name)
		if needle.to_lower() in n.to_lower():
			wanted = n
			break
	if wanted == "":
		return
	_asset_animation.speed_scale = speed_scale
	if _asset_animation.current_animation != wanted:
		_asset_animation.play(wanted,0.16)

func _animate(delta: float) -> void:
	var speed := Vector2(velocity.x,velocity.z).length()
	var moving := speed > 0.15
	if _using_asset_character:
		if moving:
			_play_asset_animation("walk",clampf(speed/walk_speed,0.80,1.65))
		else:
			_play_asset_animation("idle",1.0)
		return

	_phase += delta*(8.0 if speed < 4.0 else 11.0)
	var swing := sin(_phase)*clampf(speed/walk_speed,0.0,1.0)
	var amp := 0.62 if speed < 4.0 else 0.9
	if moving:
		_left_arm.rotation.x = lerp(_left_arm.rotation.x,swing*amp,0.22)
		_right_arm.rotation.x = lerp(_right_arm.rotation.x,-swing*amp,0.22)
		_left_leg.rotation.x = lerp(_left_leg.rotation.x,-swing*amp,0.22)
		_right_leg.rotation.x = lerp(_right_leg.rotation.x,swing*amp,0.22)
		_visual.position.y = abs(sin(_phase))*0.035
	else:
		_left_arm.rotation.x = lerp(_left_arm.rotation.x,0.05+sin(_phase*0.25)*0.02,0.08)
		_right_arm.rotation.x = lerp(_right_arm.rotation.x,-0.05-sin(_phase*0.25)*0.02,0.08)
		_left_leg.rotation.x = lerp(_left_leg.rotation.x,0.0,0.1)
		_right_leg.rotation.x = lerp(_right_leg.rotation.x,0.0,0.1)
		_visual.position.y = sin(_phase*0.35)*0.006

func _mat(color: Color,rough: float = 0.82) -> StandardMaterial3D:
	var m := StandardMaterial3D.new()
	m.albedo_color = color
	m.roughness = rough
	return m

func _mesh_box(size: Vector3,color: Color,parent: Node3D,pos: Vector3 = Vector3.ZERO) -> MeshInstance3D:
	var n := MeshInstance3D.new()
	var mesh := BoxMesh.new()
	mesh.size = size
	n.mesh = mesh
	n.material_override = _mat(color)
	n.position = pos
	n.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_ON
	parent.add_child(n)
	return n

func _mesh_capsule(radius: float,height: float,color: Color,parent: Node3D,pos: Vector3 = Vector3.ZERO) -> MeshInstance3D:
	var n := MeshInstance3D.new()
	var mesh := CapsuleMesh.new()
	mesh.radius = radius
	mesh.height = height
	n.mesh = mesh
	n.material_override = _mat(color)
	n.position = pos
	n.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_ON
	parent.add_child(n)
	return n

func _limb(parent: Node3D,x: float,y: float,color: Color,length: float,boot: bool = false) -> Node3D:
	var pivot := Node3D.new()
	pivot.position = Vector3(x,y,0.0)
	parent.add_child(pivot)
	var radius := 0.105 if boot else 0.09
	_mesh_capsule(radius,length,color,pivot,Vector3(0.0,-length*0.5,0.0))
	if boot:
		_mesh_box(Vector3(0.2,0.13,0.34),Color("23272b"),pivot,Vector3(0.0,-length+0.02,0.08))
	return pivot

func _build_visual() -> void:
	add_child(_visual)
	var coat := Color("a84b3c")
	var pants := Color("344751")
	var skin := Color("d6aa89")
	var dark := Color("263139")
	_mesh_box(Vector3(0.62,0.86,0.38),coat,_visual,Vector3(0.0,1.25,0.0))
	_mesh_box(Vector3(0.45,0.56,0.18),Color("506557"),_visual,Vector3(0.0,1.25,-0.28))
	_mesh_capsule(0.23,0.46,skin,_visual,Vector3(0.0,1.93,0.0))
	var hat := _mesh_capsule(0.235,0.17,dark,_visual,Vector3(0.0,2.12,0.0))
	hat.scale.y = 0.65
	_left_arm = _limb(_visual,-0.4,1.58,coat,0.7)
	_right_arm = _limb(_visual,0.4,1.58,coat,0.7)
	_left_leg = _limb(_visual,-0.17,0.9,pants,0.78,true)
	_right_leg = _limb(_visual,0.17,0.9,pants,0.78,true)
