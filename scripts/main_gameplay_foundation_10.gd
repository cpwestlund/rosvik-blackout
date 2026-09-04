extends "res://scripts/main_road_topology_09.gd"

# Presentation + Gameplay Foundation 10
# First complete non-combat gameplay loop: inspect the reserve generator, recover
# a fuse and battery, start it, carry a 32 A cable reel, and restore a small amount
# of power to Rosviks skola. The visual world remains the priority, so every
# mechanic has a physical object and a visible reaction in the scene.

var _gameplay_root: Node3D
var _gameplay_interactables: Array[Dictionary] = []
var _nearest_gameplay_index: int = -1
var _inventory: Dictionary = {"f3": false}
var _carry_kind: String = ""
var _carry_proxy: Node3D
var _generator_inspected: bool = false
var _battery_installed: bool = false
var _generator_on: bool = false
var _school_powered: bool = false
var _objective_label: Label
var _status_label: Label
var _toast_label: Label
var _toast_time: float = 0.0
var _generator_indicator: StandardMaterial3D
var _battery_socket_visual: Node3D
var _school_power_lights: Array[OmniLight3D] = []
var _sport_power_lights: Array[OmniLight3D] = []
var _powered_window_panels: Array[MeshInstance3D] = []

func _ready() -> void:
	super._ready()
	_build_gameplay_world()
	_build_gameplay_ui()
	_refresh_gameplay_ui()
	print("ROSVIK_GAMEPLAY_FOUNDATION_10_READY")
	print("ROSVIK_PICKUP_SYSTEM_READY interactables=",_gameplay_interactables.size())
	print("ROSVIK_CARRY_SYSTEM_READY")
	print("ROSVIK_BLACKOUT_LOOP_READY")
	print("ROSVIK_POWER_REACTION_READY")

func _process(delta: float) -> void:
	super._process(delta)
	_update_gameplay_interaction()
	_update_toast(delta)

# -------------------------------------------------------------------------
# PHYSICAL GAMEPLAY OBJECTS
# -------------------------------------------------------------------------
func _build_gameplay_world() -> void:
	_gameplay_root = Node3D.new()
	_gameplay_root.name = "GameplayFoundation10"
	add_child(_gameplay_root)

	_build_reserve_generator(Vector3(69.0,0.0,26.4))
	_build_loose_battery(Vector3(24.8,0.0,20.4))
	_build_cable_reel(Vector3(68.3,0.0,32.6))
	_build_school_fuse_box()
	_build_school_power_inlet(Vector3(13.24,0.0,4.15))
	_build_power_reaction_lights()
	_add_foundation_dressing()

func _build_reserve_generator(pos: Vector3) -> void:
	var root: Node3D = Node3D.new()
	root.name = "ReserveGenerator"
	root.position = pos
	_gameplay_root.add_child(root)

	var frame_mat: StandardMaterial3D = _mat(Color("343d40"),0.76,0.16)
	var engine_mat: StandardMaterial3D = _mat(Color("50595a"),0.84,0.12)
	var yellow_mat: StandardMaterial3D = _mat(Color("a78a3f"),0.82,0.08)
	var dark: StandardMaterial3D = _mat(Color("202629"),0.96)
	var exhaust_mat: StandardMaterial3D = _mat(Color("596062"),0.76,0.22)

	# Tubular-ish frame, fuel tank, engine and control end. More silhouette than one box.
	for x: float in [-1.05,1.05]:
		for z: float in [-0.62,0.62]:
			_cylinder(0.045,1.38,frame_mat,Vector3(x,0.70,z),root)
	for z: float in [-0.62,0.62]:
		var top_rail: MeshInstance3D = _cylinder(0.045,2.10,frame_mat,Vector3(0.0,1.36,z),root)
		top_rail.rotation.z = PI/2.0
		var lower_rail: MeshInstance3D = _cylinder(0.045,2.10,frame_mat,Vector3(0.0,0.18,z),root)
		lower_rail.rotation.z = PI/2.0
	for x: float in [-1.05,1.05]:
		var side_rail: MeshInstance3D = _cylinder(0.045,1.24,frame_mat,Vector3(x,1.36,0.0),root)
		side_rail.rotation.x = PI/2.0

	var tank: MeshInstance3D = _capsule_local(0.34,1.28,yellow_mat,Vector3(-0.36,1.08,0.0),root)
	tank.rotation.z = PI/2.0
	tank.scale.z = 1.42
	var engine: MeshInstance3D = _capsule_local(0.35,1.12,engine_mat,Vector3(0.28,0.63,0.0),root)
	engine.rotation.z = PI/2.0
	engine.scale.z = 1.28
	_cylinder(0.24,0.30,dark,Vector3(0.75,0.62,0.0),root).rotation.z = PI/2.0
	_cylinder(0.055,0.72,exhaust_mat,Vector3(0.64,1.13,-0.32),root)
	var muffler: MeshInstance3D = _capsule_local(0.09,0.38,exhaust_mat,Vector3(0.64,1.47,-0.32),root)
	muffler.rotation.z = 0.12

	# Control panel with an indicator that visibly changes when the generator starts.
	_box(Vector3(0.10,0.72,0.84),dark,Vector3(1.10,0.88,0.0),root)
	var panel_face: StandardMaterial3D = _mat(Color("667174"),0.74,0.18)
	_box(Vector3(0.04,0.56,0.66),panel_face,Vector3(1.16,0.90,0.0),root)
	_generator_indicator = _mat(Color("672b28"),0.46)
	_generator_indicator.emission_enabled = true
	_generator_indicator.emission = Color("6c1715")
	_generator_indicator.emission_energy_multiplier = 0.35
	_box(Vector3(0.035,0.11,0.11),_generator_indicator,Vector3(1.19,1.08,0.22),root)
	for z: float in [-0.18,0.0,0.18]:
		_cylinder(0.045,0.05,frame_mat,Vector3(1.20,0.78,z),root).rotation.z = PI/2.0

	# Empty battery cradle is physical and gets filled when the player installs it.
	_box(Vector3(0.72,0.08,0.52),dark,Vector3(-0.48,0.28,0.0),root)
	_battery_socket_visual = Node3D.new()
	_battery_socket_visual.position = Vector3(-0.48,0.31,0.0)
	_battery_socket_visual.visible = false
	root.add_child(_battery_socket_visual)
	_make_battery_mesh(_battery_socket_visual,Vector3.ZERO,0.72)

	_collision_box(root,Vector3(2.45,1.55,1.55),Vector3(0.0,0.78,0.0))
	_register_gameplay_interactable(root,"generator","RESERVKRAFT",2.35)

	# Concrete pad and wall-side utility clutter anchor it to the world.
	_box(Vector3(3.25,0.08,2.30),concrete_mat,pos+Vector3(0.0,0.035,0.0),_gameplay_root)
	_add_trash_bag_cluster(pos+Vector3(1.85,0.0,-0.62),PI/2.0,2)

func _build_loose_battery(pos: Vector3) -> void:
	var root: Node3D = Node3D.new()
	root.name = "LooseBattery12V"
	root.position = pos
	_gameplay_root.add_child(root)
	_make_battery_mesh(root,Vector3.ZERO,1.0)
	_register_gameplay_interactable(root,"battery","12 V AGM-BATTERI",1.85)

func _make_battery_mesh(parent: Node3D, pos: Vector3, scale_value: float) -> void:
	var root: Node3D = Node3D.new()
	root.position = pos
	root.scale = Vector3.ONE*scale_value
	parent.add_child(root)
	var case_mat: StandardMaterial3D = _mat(Color("252b2e"),0.90)
	var top_mat: StandardMaterial3D = _mat(Color("373f42"),0.82,0.10)
	var red_mat: StandardMaterial3D = _mat(Color("a74538"),0.76,0.12)
	var metal: StandardMaterial3D = _mat(Color("9b9f9b"),0.44,0.54)
	_box(Vector3(0.62,0.42,0.38),case_mat,Vector3(0.0,0.23,0.0),root)
	_box(Vector3(0.58,0.08,0.36),top_mat,Vector3(0.0,0.48,0.0),root)
	_cylinder(0.055,0.07,red_mat,Vector3(-0.19,0.56,0.0),root)
	_cylinder(0.055,0.07,metal,Vector3(0.19,0.56,0.0),root)
	var handle: MeshInstance3D = _box(Vector3(0.32,0.035,0.05),top_mat,Vector3(0.0,0.66,0.0),root)
	_box(Vector3(0.04,0.20,0.05),top_mat,Vector3(-0.15,0.58,0.0),root)
	_box(Vector3(0.04,0.20,0.05),top_mat,Vector3(0.15,0.58,0.0),root)

func _build_cable_reel(pos: Vector3) -> void:
	var root: Node3D = Node3D.new()
	root.name = "CableReel32A"
	root.position = pos
	root.rotation.y = -0.18
	_gameplay_root.add_child(root)
	_make_cable_reel_mesh(root,Vector3.ZERO,1.0)
	_register_gameplay_interactable(root,"cable","32 A KABELRULLE",1.95)

func _make_cable_reel_mesh(parent: Node3D, pos: Vector3, scale_value: float) -> void:
	var root: Node3D = Node3D.new()
	root.position = pos
	root.scale = Vector3.ONE*scale_value
	parent.add_child(root)
	var reel_mat: StandardMaterial3D = _mat(Color("a64f35"),0.82,0.08)
	var cable_mat: StandardMaterial3D = _mat(Color("171d1f"),0.98)
	var hub_mat: StandardMaterial3D = _mat(Color("5a6264"),0.70,0.18)
	for z: float in [-0.22,0.22]:
		var flange: MeshInstance3D = _cylinder(0.42,0.07,reel_mat,Vector3(0.0,0.48,z),root)
		flange.rotation.x = PI/2.0
	var hub: MeshInstance3D = _cylinder(0.18,0.48,hub_mat,Vector3(0.0,0.48,0.0),root)
	hub.rotation.x = PI/2.0
	var cable: MeshInstance3D = _cylinder(0.30,0.40,cable_mat,Vector3(0.0,0.48,0.0),root)
	cable.rotation.x = PI/2.0
	for x: float in [-0.38,0.38]:
		_cylinder(0.035,0.90,hub_mat,Vector3(x,0.45,0.0),root)
	var handle: MeshInstance3D = _cylinder(0.035,0.78,hub_mat,Vector3(0.0,0.92,0.0),root)
	handle.rotation.z = PI/2.0
	_box(Vector3(0.18,0.10,0.10),reel_mat,Vector3(0.44,0.92,0.0),root)

func _build_school_fuse_box() -> void:
	var school: Node3D = get_node_or_null("RosviksSkola") as Node3D
	if school == null:
		return
	var root: Node3D = Node3D.new()
	root.name = "FuseBoxF3"
	root.position = Vector3(2.10,0.0,4.70)
	school.add_child(root)
	var steel: StandardMaterial3D = _mat(Color("667174"),0.78,0.18)
	var inside: StandardMaterial3D = _mat(Color("282f32"),0.92)
	var fuse_mat: StandardMaterial3D = _mat(Color("a6a08b"),0.72,0.12)
	_solid_box(Vector3(0.62,0.86,0.22),steel,Vector3(0.0,1.35,0.0),root)
	_box(Vector3(0.50,0.68,0.03),inside,Vector3(0.0,1.35,0.125),root)
	for i: int in range(5):
		var y: float = 1.12+float(i)*0.115
		_box(Vector3(0.30,0.06,0.035),fuse_mat,Vector3(-0.05,y,0.15),root)
	# F3 is pulled out and clearly readable as the one loose fuse.
	var loose: Node3D = Node3D.new()
	loose.name = "LooseF3"
	loose.position = Vector3(0.42,1.18,0.25)
	root.add_child(loose)
	_box(Vector3(0.16,0.07,0.05),_mat(Color("d3bd72"),0.72,0.10),Vector3.ZERO,loose)
	_label3d("F3",Vector3(0.0,0.13,0.02),loose,9)
	_register_gameplay_interactable(loose,"fuse","F3-SÄKRING",1.65)

func _build_school_power_inlet(pos: Vector3) -> void:
	var root: Node3D = Node3D.new()
	root.name = "SchoolPowerInlet"
	root.position = pos
	root.rotation.y = -PI/2.0
	_gameplay_root.add_child(root)
	var cabinet: StandardMaterial3D = _mat(Color("596568"),0.78,0.18)
	var dark: StandardMaterial3D = _mat(Color("22292c"),0.92)
	var blue: StandardMaterial3D = _mat(Color("31587a"),0.68,0.12)
	_solid_box(Vector3(0.72,1.25,0.32),cabinet,Vector3(0.0,0.63,0.0),root)
	_box(Vector3(0.55,0.94,0.035),dark,Vector3(0.0,0.65,0.18),root)
	var socket: MeshInstance3D = _cylinder(0.13,0.08,blue,Vector3(0.0,0.54,0.23),root)
	socket.rotation.x = PI/2.0
	for angle: float in [0.0,PI*0.5,PI,PI*1.5]:
		var px: float = cos(angle)*0.06
		var py: float = sin(angle)*0.06
		_cylinder(0.012,0.04,metal_mat,Vector3(px,0.54+py,0.29),root).rotation.x = PI/2.0
	_label3d("32 A",Vector3(0.0,1.12,0.20),root,11)
	_register_gameplay_interactable(root,"panel","SKOLANS MATNING",2.0)

func _build_power_reaction_lights() -> void:
	# Sporthall exterior/service lights react as soon as the generator itself starts.
	for p: Vector3 in [Vector3(39.0,3.2,33.0),Vector3(52.0,3.2,33.0),Vector3(64.0,3.2,33.0)]:
		var light: OmniLight3D = OmniLight3D.new()
		light.position = p
		light.light_color = Color("ffd9a3")
		light.light_energy = 0.0
		light.omni_range = 6.8
		_gameplay_root.add_child(light)
		_sport_power_lights.append(light)

	# School windows and entrance warm up only after the cable is connected.
	for p: Vector3 in [Vector3(-4.45,2.0,7.2),Vector3(1.5,2.0,6.9),Vector3(8.0,2.0,6.9)]:
		var light: OmniLight3D = OmniLight3D.new()
		light.position = p
		light.light_color = Color("ffd4a0")
		light.light_energy = 0.0
		light.omni_range = 6.0
		_gameplay_root.add_child(light)
		_school_power_lights.append(light)

	var warm_mat: StandardMaterial3D = _mat(Color("d7a16e"),0.72)
	warm_mat.emission_enabled = true
	warm_mat.emission = Color("f1a868")
	warm_mat.emission_energy_multiplier = 1.10
	for x: float in [-10.0,-8.1,1.8,4.3,6.8,9.3,11.6]:
		var panel: MeshInstance3D = _box(Vector3(1.12,0.82,0.025),warm_mat,Vector3(x,2.12,6.31),_gameplay_root)
		panel.visible = false
		_powered_window_panels.append(panel)

func _add_foundation_dressing() -> void:
	# Additional power/service cues make the first mechanic feel native to the world.
	var conduit_mat: StandardMaterial3D = _mat(Color("4d5659"),0.88,0.08)
	for z: float in [3.3,4.7,5.7]:
		var pipe: MeshInstance3D = _cylinder(0.028,1.25,conduit_mat,Vector3(13.32,1.0,z),_gameplay_root)
		pipe.rotation.x = PI/2.0
	var warning_mat: StandardMaterial3D = _mat(Color("c3a344"),0.80)
	_box(Vector3(0.40,0.28,0.025),warning_mat,Vector3(13.35,1.38,4.15),_gameplay_root)
	# A small tool crate beside the generator; visual only for now.
	var crate_mat: StandardMaterial3D = _textured_mat(Color("765a43"),0.96,0.0,"horizontal",64,0.02)
	_box(Vector3(0.85,0.42,0.55),crate_mat,Vector3(67.25,0.22,26.85),_gameplay_root)
	_box(Vector3(0.78,0.04,0.48),dark_mat,Vector3(67.25,0.46,26.85),_gameplay_root)

func _register_gameplay_interactable(node: Node3D, kind: String, title: String, radius: float) -> void:
	_gameplay_interactables.append({"node":node,"kind":kind,"title":title,"radius":radius,"active":true})

# -------------------------------------------------------------------------
# INTERACTION + STATE
# -------------------------------------------------------------------------
func _update_gameplay_interaction() -> void:
	if player == null:
		return
	_nearest_gameplay_index = -1
	var nearest_distance: float = 999.0
	for i: int in range(_gameplay_interactables.size()):
		var entry: Dictionary = _gameplay_interactables[i]
		if not bool(entry.get("active",true)):
			continue
		var node: Node3D = entry["node"] as Node3D
		if node == null or not is_instance_valid(node):
			continue
		var d: float = player.global_position.distance_to(node.global_position)
		if d < float(entry["radius"]) and d < nearest_distance:
			nearest_distance = d
			_nearest_gameplay_index = i

	if _nearest_gameplay_index >= 0:
		var near: Dictionary = _gameplay_interactables[_nearest_gameplay_index]
		if _interaction_prompt != null:
			_interaction_prompt.text = _prompt_for_interactable(near)
			_interaction_prompt.visible = true
		if Input.is_action_just_pressed("interact"):
			_activate_gameplay_interactable(_nearest_gameplay_index)

func _prompt_for_interactable(entry: Dictionary) -> String:
	var kind: String = String(entry["kind"])
	if kind == "generator":
		if not _generator_inspected:
			return "E  •  UNDERSÖK RESERVKRAFT"
		if _carry_kind == "battery" and not _battery_installed:
			return "E  •  MONTERA BATTERI"
		if _battery_installed and bool(_inventory["f3"]) and not _generator_on:
			return "E  •  MONTERA F3 OCH STARTA"
		return "E  •  KONTROLLERA RESERVKRAFT"
	if kind == "battery":
		return "E  •  TA 12 V-BATTERI"
	if kind == "fuse":
		return "E  •  TA F3-SÄKRING"
	if kind == "cable":
		return "E  •  TA 32 A-KABELRULLE"
	if kind == "panel":
		return "E  •  KONTROLLERA SKOLANS MATNING"
	return "E  •  UNDERSÖK " + String(entry["title"])

func _activate_gameplay_interactable(index: int) -> void:
	if index < 0 or index >= _gameplay_interactables.size():
		return
	var entry: Dictionary = _gameplay_interactables[index]
	var kind: String = String(entry["kind"])
	match kind:
		"battery":
			_pickup_bulky(index,"battery")
		"cable":
			_pickup_bulky(index,"cable")
		"fuse":
			_inventory["f3"] = true
			_deactivate_interactable(index,true)
			_show_toast("F3-säkring i fickan.")
		"generator":
			_interact_generator()
		"panel":
			_interact_school_panel()
	_refresh_gameplay_ui()

func _pickup_bulky(index: int, kind: String) -> void:
	if _carry_kind != "":
		_show_toast("Händerna är fulla. Du bär redan " + _carry_display_name() + ".")
		return
	_carry_kind = kind
	_deactivate_interactable(index,true)
	_update_carry_proxy()
	_show_toast("Du bär " + _carry_display_name() + ".")

func _deactivate_interactable(index: int, hide_node: bool) -> void:
	_gameplay_interactables[index]["active"] = false
	var node: Node3D = _gameplay_interactables[index]["node"] as Node3D
	if node != null and is_instance_valid(node) and hide_node:
		node.visible = false

func _interact_generator() -> void:
	if not _generator_inspected:
		_generator_inspected = true
		_show_toast("Dieseln ser okej ut. Batteri saknas och F3 är urplockad.")
		return
	if _generator_on:
		_show_toast("Reservkraften går. Skolan är fortfarande inte ansluten.")
		return
	if _carry_kind == "battery" and not _battery_installed:
		_battery_installed = true
		_carry_kind = ""
		_update_carry_proxy()
		if _battery_socket_visual != null:
			_battery_socket_visual.visible = true
		_show_toast("12 V-batteriet är monterat.")
		return
	if not _battery_installed:
		_show_toast("Batteriet saknas. Det låg ett vid skolparkeringen.")
		return
	if not bool(_inventory["f3"]):
		_show_toast("F3-säkringen saknas. Kontrollera expeditionen.")
		return
	_inventory["f3"] = false
	_generator_on = true
	_start_generator_reaction()
	_show_toast("Reservkraften startar. Sporthallens serviceljus vaknar.")

func _interact_school_panel() -> void:
	if _school_powered:
		_show_toast("Skolan får reservkraft.")
		return
	if not _generator_on:
		_show_toast("Ingen spänning framme. Reservkraften måste startas först.")
		return
	if _carry_kind != "cable":
		_show_toast("En 32 A-kabel behövs mellan reservkraft och skolans matning.")
		return
	_carry_kind = ""
	_update_carry_proxy()
	_school_powered = true
	_complete_school_power_reaction()
	_show_toast("Reservkraft inkopplad. En del av skolan får ljus och värme.")

func _start_generator_reaction() -> void:
	if _generator_indicator != null:
		_generator_indicator.albedo_color = Color("477b4d")
		_generator_indicator.emission = Color("54a65e")
		_generator_indicator.emission_energy_multiplier = 1.8
	for light: OmniLight3D in _sport_power_lights:
		light.light_energy = 0.82

func _complete_school_power_reaction() -> void:
	for light: OmniLight3D in _school_power_lights:
		light.light_energy = 0.92
	for panel: MeshInstance3D in _powered_window_panels:
		panel.visible = true
	# Visible cable laid along the safe outer edge, deliberately above snow/asphalt.
	var cable_mat: StandardMaterial3D = _mat(Color("161c1e"),0.98)
	_add_ground_cable(Vector2(13.7,4.2),Vector2(18.0,8.2),cable_mat)
	_add_ground_cable(Vector2(18.0,8.2),Vector2(28.5,11.0),cable_mat)

func _add_ground_cable(a: Vector2, b: Vector2, mat: Material) -> void:
	var d: Vector2 = b-a
	var length: float = d.length()
	if length < 0.1:
		return
	var mid: Vector2 = (a+b)*0.5
	var cable: MeshInstance3D = _cylinder(0.035,length,mat,Vector3(mid.x,0.13,mid.y),_gameplay_root)
	cable.rotation.z = PI/2.0
	cable.rotation.y = -atan2(d.y,d.x)

# -------------------------------------------------------------------------
# CARRY PRESENTATION
# -------------------------------------------------------------------------
func _update_carry_proxy() -> void:
	if _carry_proxy != null and is_instance_valid(_carry_proxy):
		_carry_proxy.queue_free()
	_carry_proxy = null
	if player == null or _carry_kind == "":
		return
	_carry_proxy = Node3D.new()
	_carry_proxy.name = "CarryProxy"
	_carry_proxy.position = Vector3(0.46,0.92,0.43)
	player.add_child(_carry_proxy)
	if _carry_kind == "battery":
		_make_battery_mesh(_carry_proxy,Vector3.ZERO,0.78)
	else:
		_make_cable_reel_mesh(_carry_proxy,Vector3.ZERO,0.70)

func _carry_display_name() -> String:
	if _carry_kind == "battery":
		return "12 V-batteriet"
	if _carry_kind == "cable":
		return "32 A-kabelrullen"
	return "ingenting"

# -------------------------------------------------------------------------
# UI / OBJECTIVE
# -------------------------------------------------------------------------
func _build_gameplay_ui() -> void:
	var ui: CanvasLayer = CanvasLayer.new()
	ui.layer = 20
	add_child(ui)

	var panel: ColorRect = ColorRect.new()
	panel.position = Vector2(1215.0,78.0)
	panel.size = Vector2(355.0,190.0)
	panel.color = Color(0.018,0.026,0.031,0.86)
	ui.add_child(panel)

	var title: Label = Label.new()
	title.position = Vector2(16.0,12.0)
	title.text = "BLACKOUT / UPPDRAG"
	title.add_theme_font_size_override("font_size",15)
	title.add_theme_color_override("font_color",Color("e5dfc9"))
	panel.add_child(title)

	_objective_label = Label.new()
	_objective_label.position = Vector2(16.0,42.0)
	_objective_label.size = Vector2(323.0,76.0)
	_objective_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	_objective_label.add_theme_font_size_override("font_size",14)
	panel.add_child(_objective_label)

	_status_label = Label.new()
	_status_label.position = Vector2(16.0,120.0)
	_status_label.size = Vector2(323.0,58.0)
	_status_label.add_theme_font_size_override("font_size",12)
	_status_label.add_theme_color_override("font_color",Color("b9c7c2"))
	panel.add_child(_status_label)

	_toast_label = Label.new()
	_toast_label.position = Vector2(500.0,70.0)
	_toast_label.size = Vector2(600.0,48.0)
	_toast_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_toast_label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	_toast_label.add_theme_font_size_override("font_size",15)
	_toast_label.add_theme_color_override("font_color",Color("f1ead7"))
	_toast_label.add_theme_color_override("font_shadow_color",Color(0,0,0,0.92))
	_toast_label.add_theme_constant_override("shadow_offset_x",2)
	_toast_label.add_theme_constant_override("shadow_offset_y",2)
	_toast_label.visible = false
	ui.add_child(_toast_label)

func _refresh_gameplay_ui() -> void:
	if _objective_label == null or _status_label == null:
		return
	_objective_label.text = _current_objective()
	var fuse_text: String = "F3: ✓" if bool(_inventory["f3"]) else "F3: —"
	var hands_text: String = "HÄNDER: " + ("tomma" if _carry_kind == "" else ("12 V-batteri" if _carry_kind == "battery" else "32 A-kabel"))
	var gen_text: String = "RESERVKRAFT: " + ("ON" if _generator_on else "OFF")
	var school_text: String = "SKOLA: " + ("RESERVKRAFT" if _school_powered else "MÖRK")
	_status_label.text = fuse_text + "     " + hands_text + "\n" + gen_text + "     " + school_text

func _current_objective() -> String:
	if _school_powered:
		return "KLART: En del av Rosviks skola har fått reservkraft. Utforska området."
	if _generator_on:
		if _carry_kind == "cable":
			return "Bär 32 A-kabeln till skolans externa matning."
		return "Hämta 32 A-kabelrullen vid sporthallens servicesida."
	if not _generator_inspected:
		return "Undersök reservkraften vid sporthallen."
	if not _battery_installed:
		if _carry_kind == "battery":
			return "Bär 12 V-batteriet till reservkraften."
		return "Hitta 12 V-batteriet vid skolparkeringen."
	if not bool(_inventory["f3"]):
		return "Hitta F3-säkringen i skolans expedition."
	return "Gå tillbaka till reservkraften och starta den."

func _show_toast(text: String) -> void:
	if _toast_label == null:
		return
	_toast_label.text = text
	_toast_label.visible = true
	_toast_time = 3.6

func _update_toast(delta: float) -> void:
	if _toast_time <= 0.0:
		return
	_toast_time -= delta
	if _toast_time <= 0.0 and _toast_label != null:
		_toast_label.visible = false
