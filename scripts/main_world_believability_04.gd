extends "res://scripts/main_architectural_cohesion_03.gd"

var _interactive_doors: Array[Dictionary] = []
var _interaction_prompt: Label
var _nearest_door_index: int = -1
var _mood_clock: float = 0.0
var _flicker_light: OmniLight3D

func _ready() -> void:
	super._ready()
	_add_apocalypse_dressing()
	print("ROSVIK_WORLD_BELIEVABILITY_04_READY")
	print("ROSVIK_INTERACTIVE_DOORS_READY doors=", _interactive_doors.size())
	print("ROSVIK_APOCALYPSE_MOOD_READY")
	print("ROSVIK_ART_REPLACEMENT_READY")

func _process(delta: float) -> void:
	super._process(delta)
	_mood_clock += delta
	_update_interactive_doors(delta)
	_update_flicker()

# -------------------------------------------------------------------------
# VISUAL MOOD
# -------------------------------------------------------------------------
func _build_environment() -> void:
	var env_node: WorldEnvironment = WorldEnvironment.new()
	var env: Environment = Environment.new()
	var sky_material: ProceduralSkyMaterial = ProceduralSkyMaterial.new()
	sky_material.sky_top_color = Color("182832")
	sky_material.sky_horizon_color = Color("52666f")
	sky_material.ground_bottom_color = Color("29383e")
	sky_material.ground_horizon_color = Color("79868a")
	sky_material.sun_angle_max = 12.0
	var sky: Sky = Sky.new()
	sky.sky_material = sky_material
	env.background_mode = Environment.BG_SKY
	env.sky = sky
	env.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	env.ambient_light_color = Color("70838d")
	env.ambient_light_energy = 0.27
	env.fog_enabled = true
	env.fog_light_color = Color("53656d")
	env.fog_density = 0.0090
	env.fog_height = 0.18
	env.fog_height_density = 0.085
	env.tonemap_mode = Environment.TONE_MAPPER_FILMIC
	env.tonemap_exposure = 0.50
	env_node.environment = env
	add_child(env_node)

	var sun: DirectionalLight3D = DirectionalLight3D.new()
	sun.rotation_degrees = Vector3(-34.0,-39.0,0.0)
	sun.light_color = Color("e8b58e")
	sun.light_energy = 0.58
	sun.shadow_enabled = true
	sun.directional_shadow_max_distance = 120.0
	add_child(sun)

# -------------------------------------------------------------------------
# REAL DOORS
# -------------------------------------------------------------------------
func _doorway_x(root: Node3D, center_x: float, z: float, width: float, title: String, leaf_mat: Material, frame_mat: Material, room_index: int, double_door: bool = false) -> void:
	# The opening, frame, leaf and collision all share the same measurements.
	_box(Vector3(0.08,2.35,0.20),frame_mat,Vector3(center_x-width*0.5,1.18,z),root)
	_box(Vector3(0.08,2.35,0.20),frame_mat,Vector3(center_x+width*0.5,1.18,z),root)
	_box(Vector3(width+0.08,0.10,0.20),frame_mat,Vector3(center_x,2.31,z),root)
	if title != "":
		_label3d(title,Vector3(center_x,2.60,z+0.03),root,11)

	# Wide untitled portals (foyer -> corridor) are intentionally open circulation.
	if title == "" and width > 2.0 and not double_door:
		return

	var hinges: Array[Node3D] = []
	var open_angles: Array[float] = []
	var is_glass: bool = title == "ENTRÉ" or double_door
	if double_door:
		var leaf_width: float = width * 0.48
		var left_hinge: Node3D = Node3D.new()
		left_hinge.name = "DoorHinge_L_" + title
		left_hinge.position = Vector3(center_x-width*0.5,0.0,z)
		root.add_child(left_hinge)
		_make_door_leaf(left_hinge,leaf_width,1.0,leaf_mat,frame_mat,is_glass)
		hinges.append(left_hinge)
		open_angles.append(-1.28)

		var right_hinge: Node3D = Node3D.new()
		right_hinge.name = "DoorHinge_R_" + title
		right_hinge.position = Vector3(center_x+width*0.5,0.0,z)
		root.add_child(right_hinge)
		_make_door_leaf(right_hinge,leaf_width,-1.0,leaf_mat,frame_mat,is_glass)
		hinges.append(right_hinge)
		open_angles.append(1.28)
	else:
		var hinge: Node3D = Node3D.new()
		hinge.name = "DoorHinge_" + title
		hinge.position = Vector3(center_x-width*0.5,0.0,z)
		root.add_child(hinge)
		_make_door_leaf(hinge,width*0.94,1.0,leaf_mat,frame_mat,is_glass)
		hinges.append(hinge)
		# Classroom doors open into the classrooms; northern rooms open away from corridor.
		open_angles.append(1.34 if z < 1.0 else -1.34)

	_interactive_doors.append({
		"root": root,
		"point": Vector3(center_x,1.0,z),
		"hinges": hinges,
		"open_angles": open_angles,
		"open": false,
		"title": title if title != "" else "DÖRR"
	})

func _make_door_leaf(hinge: Node3D, width: float, direction: float, leaf_mat: Material, frame_mat: Material, is_glass: bool) -> void:
	var height: float = 2.18
	var center: Vector3 = Vector3(direction*width*0.5,height*0.5,0.0)
	if is_glass:
		# Narrow metal perimeter + a large glass pane gives the entrance a less blocky silhouette.
		_box(Vector3(width,0.11,0.085),frame_mat,Vector3(center.x,0.10,0.0),hinge)
		_box(Vector3(width,0.11,0.085),frame_mat,Vector3(center.x,height-0.10,0.0),hinge)
		_box(Vector3(0.09,height,0.085),frame_mat,Vector3(direction*0.045,height*0.5,0.0),hinge)
		_box(Vector3(0.09,height,0.085),frame_mat,Vector3(direction*(width-0.045),height*0.5,0.0),hinge)
		_box(Vector3(width-0.16,height-0.24,0.055),leaf_mat,Vector3(center.x,height*0.5,0.0),hinge)
	else:
		_box(Vector3(width,height,0.085),leaf_mat,center,hinge)
		# Inset panels keep classroom doors from looking like plain slabs.
		var inset: StandardMaterial3D = _mat(Color("4f443b"),0.91)
		_box(Vector3(width*0.70,0.58,0.025),inset,Vector3(center.x,0.53,0.058),hinge)
		_box(Vector3(width*0.70,0.88,0.025),inset,Vector3(center.x,1.45,0.058),hinge)
		var pane_mat: StandardMaterial3D = glass_mat.duplicate() as StandardMaterial3D
		_box(Vector3(width*0.42,0.36,0.030),pane_mat,Vector3(center.x,1.82,0.060),hinge)

	# Collision follows the moving hinge, so a closed door is actually solid.
	var body: StaticBody3D = StaticBody3D.new()
	body.name = "DoorCollision"
	body.position = center
	var collision: CollisionShape3D = CollisionShape3D.new()
	var shape: BoxShape3D = BoxShape3D.new()
	shape.size = Vector3(width,height,0.13)
	collision.shape = shape
	body.add_child(collision)
	hinge.add_child(body)

	# Handle + escutcheon.
	var handle_x: float = direction*(width-0.15)
	_box(Vector3(0.055,0.18,0.035),metal_mat,Vector3(handle_x,1.02,0.075),hinge)
	var handle: MeshInstance3D = _cylinder(0.025,0.20,metal_mat,Vector3(handle_x,1.02,0.18),hinge)
	handle.rotation.x = PI/2.0

func _update_interactive_doors(delta: float) -> void:
	if player == null:
		return
	_nearest_door_index = -1
	var nearest_distance: float = 999.0
	for i: int in range(_interactive_doors.size()):
		var door: Dictionary = _interactive_doors[i]
		var door_root: Node3D = door["root"] as Node3D
		if door_root == null:
			continue
		var world_point: Vector3 = door_root.to_global(door["point"])
		var d: float = player.global_position.distance_to(world_point)
		if d < 2.15 and d < nearest_distance:
			nearest_distance = d
			_nearest_door_index = i

		var hinges: Array = door["hinges"]
		var angles: Array = door["open_angles"]
		for h: int in range(hinges.size()):
			var hinge: Node3D = hinges[h] as Node3D
			if hinge == null:
				continue
			var target: float = float(angles[h]) if door["open"] else 0.0
			hinge.rotation.y = move_toward(hinge.rotation.y,target,2.65*delta)

	if _interaction_prompt != null:
		if _nearest_door_index >= 0:
			var near_door: Dictionary = _interactive_doors[_nearest_door_index]
			var verb: String = "STÄNG" if near_door["open"] else "ÖPPNA"
			_interaction_prompt.text = "E  •  " + verb + " " + String(near_door["title"])
			_interaction_prompt.visible = true
		else:
			_interaction_prompt.visible = false

	if _nearest_door_index >= 0 and Input.is_action_just_pressed("interact"):
		_interactive_doors[_nearest_door_index]["open"] = not _interactive_doors[_nearest_door_index]["open"]

# -------------------------------------------------------------------------
# LESS BLOCKY PROPS / APOCALYPSE DRESSING
# -------------------------------------------------------------------------
func _add_car(pos: Vector3, color: Color, yaw: float, open_door: bool) -> void:
	# A more shaped stylised car: rounded lower body, sloped bonnet/trunk, real lamps and wheels.
	var root: Node3D = Node3D.new()
	root.position = pos
	root.rotation.y = yaw
	add_child(root)
	var body_mat: StandardMaterial3D = _mat(color,0.70,0.16)
	var tyre_mat: StandardMaterial3D = _mat(Color("171a1c"),1.0)
	var lamp_mat: StandardMaterial3D = _mat(Color("e1d8ad"),0.45)
	lamp_mat.emission_enabled = true
	lamp_mat.emission = Color("e7d39a")
	lamp_mat.emission_energy_multiplier = 0.18

	var lower: MeshInstance3D = _capsule_local(0.56,3.85,body_mat,Vector3(0.0,0.68,0.0),root)
	lower.rotation.z = PI/2.0
	lower.scale.z = 1.48
	var cabin: MeshInstance3D = _capsule_local(0.50,1.95,body_mat,Vector3(-0.25,1.18,0.0),root)
	cabin.rotation.z = PI/2.0
	cabin.scale.z = 1.30
	_box(Vector3(1.50,0.46,1.48),glass_mat,Vector3(-0.25,1.30,0.0),root)
	var bonnet: MeshInstance3D = _box(Vector3(1.30,0.20,1.60),body_mat,Vector3(1.45,0.87,0.0),root)
	bonnet.rotation.z = -0.10
	var trunk: MeshInstance3D = _box(Vector3(0.78,0.22,1.58),body_mat,Vector3(-1.65,0.88,0.0),root)
	trunk.rotation.z = 0.08
	for xw: float in [-1.38,1.36]:
		for zw: float in [-0.84,0.84]:
			var wheel: MeshInstance3D = _cylinder(0.34,0.25,tyre_mat,Vector3(xw,0.40,zw),root)
			wheel.rotation.x = PI/2.0
	for zlamp: float in [-0.55,0.55]:
		_sphere_local(0.10,lamp_mat,Vector3(2.00,0.82,zlamp),root,Vector3(0.42,0.55,0.80))
	_box(Vector3(1.85,0.055,1.15),packed_snow_mat,Vector3(-0.25,1.69,0.0),root)
	_collision_box(root,Vector3(4.35,1.50,1.80),Vector3(0.0,0.75,0.0))
	if open_door:
		var door_hinge: Node3D = Node3D.new()
		door_hinge.position = Vector3(-0.55,0.72,0.90)
		door_hinge.rotation.y = -0.62
		root.add_child(door_hinge)
		_box(Vector3(1.05,0.82,0.08),body_mat,Vector3(0.48,0.42,0.0),door_hinge)

func _capsule_local(radius: float, height: float, mat: Material, pos: Vector3, parent: Node) -> MeshInstance3D:
	var node: MeshInstance3D = MeshInstance3D.new()
	var mesh: CapsuleMesh = CapsuleMesh.new()
	mesh.radius = radius
	mesh.height = height
	node.mesh = mesh
	node.material_override = mat
	node.position = pos
	node.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_ON
	parent.add_child(node)
	return node

func _sphere_local(radius: float, mat: Material, pos: Vector3, parent: Node, scale_value: Vector3 = Vector3.ONE) -> MeshInstance3D:
	var node: MeshInstance3D = MeshInstance3D.new()
	var mesh: SphereMesh = SphereMesh.new()
	mesh.radius = radius
	mesh.height = radius*2.0
	node.mesh = mesh
	node.material_override = mat
	node.position = pos
	node.scale = scale_value
	node.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_ON
	parent.add_child(node)
	return node

func _add_apocalypse_dressing() -> void:
	var school: Node3D = get_node_or_null("RosviksSkola") as Node3D
	if school != null:
		# Kill most normal lighting. One tube flickers; emergency lights remain warm/red.
		var school_lights: Array[OmniLight3D] = []
		_collect_omni_lights(school,school_lights)
		for i: int in range(school_lights.size()):
			if i % 4 == 0:
				school_lights[i].light_energy *= 0.48
			else:
				school_lights[i].light_energy *= 0.12
		if school_lights.size() > 2:
			_flicker_light = school_lights[2]

		var emergency: OmniLight3D = OmniLight3D.new()
		emergency.position = Vector3(-4.45,2.35,4.75)
		emergency.light_color = Color("d65f43")
		emergency.light_energy = 0.62
		emergency.omni_range = 4.0
		school.add_child(emergency)

		# Loose papers and a fallen chair create an interrupted, recently-used space.
		var paper_mat: StandardMaterial3D = _mat(Color("c9c5b8"),0.98)
		for i: int in range(12):
			var px: float = -9.0 + float((i*17)%90)/10.0
			var pz: float = 0.75 + float((i*11)%17)/10.0
			var paper: MeshInstance3D = _box(Vector3(0.28+float(i%3)*0.05,0.008,0.20),paper_mat,Vector3(px,0.095,pz),school)
			paper.rotation.y = float(i)*0.73
		_add_toppled_school_chair(school,Vector3(10.7,0.10,1.15))

		# Round trash bags and a mop bucket reduce the all-box visual language.
		var bag_mat: StandardMaterial3D = _mat(Color("242a2d"),0.99)
		_sphere_local(0.32,bag_mat,Vector3(11.7,0.31,5.35),school,Vector3(0.92,1.20,0.82))
		_sphere_local(0.25,bag_mat,Vector3(11.15,0.24,5.45),school,Vector3(1.05,1.05,0.90))
		var bucket_mat: StandardMaterial3D = _mat(Color("8b3e32"),0.84,0.05)
		_cylinder(0.25,0.42,bucket_mat,Vector3(5.55,0.22,2.20),school)
		var mop: MeshInstance3D = _cylinder(0.025,1.55,wood_mat,Vector3(5.65,0.95,2.22),school)
		mop.rotation.z = 0.25

		# Handwritten-feeling emergency information on the corridor wall.
		var warning_mat: StandardMaterial3D = _mat(Color("a87135"),0.90)
		_box(Vector3(1.45,0.72,0.035),warning_mat,Vector3(-11.78,1.60,0.55),school)
		_label3d("STRÖM BORTA\n16:08",Vector3(-11.74,1.60,0.57),school,13)

	# Exterior: abandoned maintenance scene, bent light and cones.
	var cone_mat: StandardMaterial3D = _mat(Color("b85e2d"),0.88)
	for p: Vector3 in [Vector3(-8.5,0.0,11.2),Vector3(-7.4,0.0,11.9),Vector3(38.0,0.0,20.0)]:
		_cone(0.18,0.55,cone_mat,p+Vector3(0.0,0.275,0.0),self)
	var broken_lamp: Node3D = Node3D.new()
	broken_lamp.position = Vector3(18.0,0.0,13.0)
	broken_lamp.rotation.z = 0.34
	add_child(broken_lamp)
	_cylinder(0.06,3.8,dark_mat,Vector3(0.0,1.9,0.0),broken_lamp)
	_box(Vector3(0.62,0.08,0.12),dark_mat,Vector3(0.28,3.65,0.0),broken_lamp)

func _collect_omni_lights(node: Node, output: Array[OmniLight3D]) -> void:
	for child: Node in node.get_children():
		if child is OmniLight3D:
			output.append(child as OmniLight3D)
		_collect_omni_lights(child,output)

func _add_toppled_school_chair(parent: Node3D, pos: Vector3) -> void:
	var root: Node3D = Node3D.new()
	root.position = pos
	root.rotation = Vector3(0.15,0.55,1.10)
	parent.add_child(root)
	var seat_mat: StandardMaterial3D = _mat(Color("59666c"),0.91)
	_box(Vector3(0.48,0.08,0.45),seat_mat,Vector3(0.0,0.47,0.0),root)
	_box(Vector3(0.48,0.52,0.06),seat_mat,Vector3(0.0,0.73,-0.21),root)
	for x: float in [-0.18,0.18]:
		for z: float in [-0.16,0.16]:
			var leg: MeshInstance3D = _cylinder(0.025,0.50,metal_mat,Vector3(x,0.25,z),root)
			leg.rotation.z = 0.04 if x > 0.0 else -0.04

func _update_flicker() -> void:
	if _flicker_light == null:
		return
	var phase: float = fmod(_mood_clock,5.4)
	if phase < 0.08 or (phase > 0.17 and phase < 0.24) or (phase > 3.65 and phase < 3.72):
		_flicker_light.light_energy = 0.04
	elif phase < 0.34:
		_flicker_light.light_energy = 0.82
	else:
		_flicker_light.light_energy = 0.34 + sin(_mood_clock*9.0)*0.03

func _build_ui() -> void:
	super._build_ui()
	_interaction_prompt = Label.new()
	_interaction_prompt.position = Vector2(560.0,790.0)
	_interaction_prompt.size = Vector2(480.0,52.0)
	_interaction_prompt.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_interaction_prompt.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	_interaction_prompt.add_theme_font_size_override("font_size",18)
	_interaction_prompt.add_theme_color_override("font_color",Color("f0eee3"))
	_interaction_prompt.add_theme_color_override("font_shadow_color",Color(0.0,0.0,0.0,0.85))
	_interaction_prompt.add_theme_constant_override("shadow_offset_x",2)
	_interaction_prompt.add_theme_constant_override("shadow_offset_y",2)
	_interaction_prompt.visible = false
	var ui_layer: CanvasLayer = get_child(get_child_count()-1) as CanvasLayer
	if ui_layer != null:
		ui_layer.add_child(_interaction_prompt)
