extends CharacterBody3D

const Geometry = preload("res://winter/scripts/geometry.gd")
var g: = Geometry.new()
var model: = Node3D.new()
var left_leg: = Node3D.new()
var right_leg: = Node3D.new()
var left_arm: = Node3D.new()
var right_arm: = Node3D.new()
var pack: = Node3D.new()
var carry_model: = Node3D.new()
var torch: = SpotLight3D.new()
var carrying: = false
var working: = false
var paused: = false
var phase: = 0.0
var stride_distance: = 0.0
var breath_clock: = 0.0
signal step(pos: Vector3, side: float)

func _ready() -> void :
	var col: = CollisionShape3D.new()
	var capsule: = CapsuleShape3D.new()
	capsule.radius = 0.28
	capsule.height = 1.78
	col.shape = capsule
	col.position.y = 0.9
	add_child(col)
	add_child(model)
	var coat: = g.mat("5e695d", 0, 0.25)
	var dark: = g.mat("252d32")
	var boot: = g.mat("272524")
	var skin: = g.plain("bd9881")
	g.ellipsoid(model, Vector3(0, 1.12, 0), Vector3(0.29, 0.39, 0.21), coat)
	g.ellipsoid(model, Vector3(0, 0.82, 0), Vector3(0.29, 0.16, 0.21), coat)
	g.ellipsoid(model, Vector3(0, 1.61, 0), Vector3(0.2, 0.24, 0.2), coat)
	g.ellipsoid(model, Vector3(0, 1.61, 0.145), Vector3(0.135, 0.14, 0.075), skin)
	g.ellipsoid(model, Vector3(0, 1.77, 0.04), Vector3(0.18, 0.07, 0.17), dark)
	g.box(model, Vector3(0, 1.36, 0.19), Vector3(0.25, 0.12, 0.075), g.mat("867863"))
	g.box(model, Vector3(0, 1.1, 0.211), Vector3(0.018, 0.4, 0.018), dark)
	for side: float in [-1.0, 1.0]:
		var leg: = left_leg if side < 0 else right_leg
		model.add_child(leg)
		leg.position = Vector3(side * 0.145, 0.85, 0)
		g.rod(leg, Vector3.ZERO, Vector3(0, -0.57, 0), 0.115, dark, 0.085)
		g.ellipsoid(leg, Vector3(0, -0.64, 0.045), Vector3(0.115, 0.15, 0.18), boot)
		var arm: = left_arm if side < 0 else right_arm
		model.add_child(arm)
		arm.position = Vector3(side * 0.3, 1.35, 0)
		g.rod(arm, Vector3.ZERO, Vector3(side * 0.025, -0.43, 0.04), 0.12, coat, 0.08)
		g.ellipsoid(arm, Vector3(side * 0.025, -0.47, 0.05), Vector3(0.075, 0.1, 0.09), dark)
	model.add_child(pack)
	g.ellipsoid(pack, Vector3(0, 1.12, -0.23), Vector3(0.23, 0.3, 0.12), g.mat("6e503b"))
	for x: float in [-0.19, 0.19]:
		g.box(pack, Vector3(x, 1.23, 0.17), Vector3(0.04, 0.35, 0.035), g.mat("433d32"))
	model.add_child(carry_model)
	carry_model.position = Vector3(0, 0.94, 0.44)
	g.box(carry_model, Vector3.ZERO, Vector3(0.38, 0.25, 0.24), g.mat("303537"))
	g.box(carry_model, Vector3(0, 0.14, 0), Vector3(0.41, 0.035, 0.26), g.mat("191e20"))
	for x: float in [-0.13, 0.13]:
		g.box(carry_model, Vector3(x, 0.17, 0), Vector3(0.03, 0.03, 0.04), g.plain("bfbab1"))
	carry_model.visible = false
	model.add_child(torch)
	torch.position = Vector3(0.18, 1.32, 0.22)
	torch.rotation.y = PI
	torch.rotation.x = -0.3
	torch.light_color = Color("f2e4c8")
	torch.light_energy = 2.5
	torch.spot_range = 18.0
	torch.spot_angle = 28.0
	torch.spot_attenuation = 1.4
	torch.shadow_enabled = true
	torch.visible = false
	floor_snap_length = 0.3

func _physics_process(delta: float) -> void :
	if paused: return
	var input: = Input.get_vector("move_left", "move_right", "move_forward", "move_back")

	var direction: = Vector3(input.x * 0.7071 + input.y * -0.7071, 0, input.x * 0.7071 + input.y * 0.7071)
	var speed: = 5.1 if Input.is_action_pressed("sprint") else 3.0
	if carrying: speed = 2.2
	if working: direction = Vector3.ZERO
	velocity.x = move_toward(velocity.x, direction.x * speed, delta * 13.0)
	velocity.z = move_toward(velocity.z, direction.z * speed, delta * 13.0)
	if not is_on_floor(): velocity.y -= 20.0 * delta
	else: velocity.y = -0.1
	move_and_slide()
	var planar_speed: = Vector2(velocity.x, velocity.z).length()
	if planar_speed > 0.1:
		model.rotation.y = lerp_angle(model.rotation.y, atan2(velocity.x, velocity.z), 1.0 - exp(-12.0 * delta))
	phase += planar_speed * delta * 3.6
	var swing: = sin(phase) * minf(planar_speed / 4.5, 0.6)
	left_leg.rotation.x = swing
	right_leg.rotation.x = - swing
	left_arm.rotation.x = -0.85 if carrying else - swing * 0.7
	right_arm.rotation.x = -0.85 if carrying else swing * 0.7
	model.position.y = absf(sin(phase)) * 0.026 * minf(planar_speed, 2.0)
	model.rotation.x = lerpf(model.rotation.x, 0.2 if working else 0.0, delta * 8.0)
	carry_model.visible = carrying
	stride_distance += planar_speed * delta
	if stride_distance > 0.74:
		stride_distance = 0.0
		step.emit(global_position, 1.0 if sin(phase) > 0.0 else -1.0)
	if Input.is_action_just_pressed("flashlight"): torch.visible = not torch.visible
