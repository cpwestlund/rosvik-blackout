extends "res://scripts/main_gameplay_foundation_10.gd"

# World Expansion + Interaction Depth 11
# First residential edge beyond the civic cluster, a real collision pass for
# decorative objects, searchable everyday loot and a usable flashlight.

var _expansion11_root: Node3D
var _collision11_root: Node3D
var _residential11_root: Node3D
var _loot11_label: Label
var _zone11_label: Label
var _flashlight11: SpotLight3D
var _flashlight_on11: bool = false
var _loot11: Dictionary = {}
var _loot_capacity11: int = 8
var _residential_specs11: Array[Dictionary] = []
var _residential_roads11: Array[Dictionary] = []

func _enter_tree() -> void:
	super._enter_tree()
	_residential_specs11 = [
		{"pos":Vector3(-101.5,0.0,-10.0),"color":Color("8d6b58"),"variant":0},
		{"pos":Vector3(-101.0,0.0,8.0),"color":Color("6f8187"),"variant":1},
		{"pos":Vector3(-101.8,0.0,25.0),"color":Color("8b775e"),"variant":2}
	]
	for spec: Dictionary in _residential_specs11:
		var p: Vector3 = spec["pos"]
		_world_structure_zones.append(Rect2(Vector2(p.x-5.4,p.z-4.3),Vector2(10.8,8.6)))

func _ready() -> void:
	super._ready()
	_inventory["ficklampa"] = false
	_build_expansion11()
	_build_collision_pass11()
	_build_searchables11()
	_build_exploration_ui11()
	_validate_expansion11()
	_refresh_gameplay_ui()
	_refresh_loot11_ui()
	print("ROSVIK_WORLD_EXPANSION_11_READY houses=",_residential_specs11.size())
	print("ROSVIK_PROP_COLLISION_11_READY")
	print("ROSVIK_SEARCHABLE_LOOT_11_READY")
	print("ROSVIK_FLASHLIGHT_11_READY")
	print("ROSVIK_INTERACTION_DEPTH_11_READY")

func _process(delta: float) -> void:
	super._process(delta)
	if Input.is_action_just_pressed("flashlight"):
		_toggle_flashlight11()
	_update_zone11_label()

func _build_expansion11() -> void:
	_expansion11_root = Node3D.new()
	_expansion11_root.name = "WorldExpansion11"
	add_child(_expansion11_root)
	_residential11_root = Node3D.new()
	_residential11_root.name = "ResidentialEdge11"
	_expansion11_root.add_child(_residential11_root)
	_build_residential_road11()
	for spec: Dictionary in _residential_specs11:
		_build_house11(spec["pos"],spec["color"],int(spec["variant"]))
	var sign_root: Node3D = Node3D.new()
	sign_root.position = Vector3(-106.4,0.0,-22.0)
	_residential11_root.add_child(sign_root)
	var sign_mat: StandardMaterial3D = _mat(Color("345a70"),0.80,0.05)
	_solid_box(Vector3(0.07,1.75,0.07),metal_mat,Vector3(-0.92,0.88,0.0),sign_root)
	_solid_box(Vector3(0.07,1.75,0.07),metal_mat,Vector3(0.92,0.88,0.0),sign_root)
	_box(Vector3(2.15,0.55,0.08),sign_mat,Vector3(0.0,1.48,0.0),sign_root)
	_label3d("BOSTÄDER",Vector3(0.0,1.49,0.055),sign_root,13)

func _build_residential_road11() -> void:
	_residential_roads11 = [
		{"a":Vector2(-105.0,-30.0),"b":Vector2(-109.0,-30.0),"w":6.0},
		{"a":Vector2(-112.0,-27.0),"b":Vector2(-112.0,32.0),"w":6.0}
	]
	_road09_piece(Vector2(-105.0,-30.0),Vector2(-109.0,-30.0),6.0,true)
	_road09_patch(Vector3(-112.0,0.0,-30.0),6.0)
	_road09_piece(Vector2(-112.0,-27.0),Vector2(-112.0,32.0),6.0,true)
	for z_value: float in [-10.0,8.0,25.0]:
		_box(Vector3(5.2,0.045,3.15),asphalt_mat,Vector3(-106.5,0.052,z_value),_residential11_root)
	var path_mat: StandardMaterial3D = _textured_mat(Color("70787a"),0.98,0.0,"noise",96,0.018)
	_box(Vector3(1.15,0.035,57.0),path_mat,Vector3(-108.25,0.080,2.5),_residential11_root)

func _build_house11(pos: Vector3, color: Color, variant: int) -> void:
	var root: Node3D = Node3D.new()
	root.position = pos
	root.name = "ResidentialHouse11_%d" % variant
	_residential11_root.add_child(root)
	var wall: StandardMaterial3D = _textured_mat(color,0.94,0.0,"horizontal",72,0.026)
	var trim: StandardMaterial3D = _mat(Color("d5d2c6"),0.92)
	var roof_m: StandardMaterial3D = _textured_mat(Color("333b3f"),0.91,0.03,"noise",72,0.024)
	var porch_m: StandardMaterial3D = _textured_mat(Color("756352"),0.96,0.0,"horizontal",64,0.022)
	_solid_box(Vector3(9.4,3.05,7.0),wall,Vector3(0.0,1.525,0.0),root)
	var roof_l: MeshInstance3D = _box(Vector3(10.0,0.28,4.15),roof_m,Vector3(0.0,3.43,-1.76),root)
	roof_l.rotation.x = -0.24
	var roof_r: MeshInstance3D = _box(Vector3(10.0,0.28,4.15),roof_m,Vector3(0.0,3.43,1.76),root)
	roof_r.rotation.x = 0.24
	var snow_l: MeshInstance3D = _box(Vector3(9.76,0.07,3.95),packed_snow_mat,Vector3(0.0,3.60,-1.70),root)
	snow_l.rotation.x = -0.24
	var snow_r: MeshInstance3D = _box(Vector3(9.76,0.07,3.95),packed_snow_mat,Vector3(0.0,3.60,1.70),root)
	snow_r.rotation.x = 0.24
	_box(Vector3(0.12,2.08,1.04),dark_mat,Vector3(-4.76,1.06,1.55),root)
	_box(Vector3(0.13,0.12,1.20),trim,Vector3(-4.80,2.12,1.55),root)
	for z_w: float in [-1.55,0.05]:
		_box(Vector3(0.12,1.16,1.42),dark_mat,Vector3(-4.78,1.68,z_w),root)
		_box(Vector3(0.135,0.96,1.22),glass_mat,Vector3(-4.84,1.68,z_w),root)
	_solid_box(Vector3(1.45,0.16,2.0),porch_m,Vector3(-5.38,0.09,1.55),root)
	_solid_box(Vector3(0.58,0.14,1.55),porch_m,Vector3(-6.20,0.07,1.55),root)
	var lamp_mat: StandardMaterial3D = _mat(Color("e0c292"),0.50)
	lamp_mat.emission_enabled = true
	lamp_mat.emission = Color("c88b54")
	lamp_mat.emission_energy_multiplier = 0.35
	_box(Vector3(0.10,0.24,0.18),lamp_mat,Vector3(-4.92,2.36,2.35),root)
	if variant != 1:
		_solid_box(Vector3(3.35,2.35,5.2),wall,Vector3(2.95,1.175,-0.45),root)
		_box(Vector3(3.05,1.92,0.10),_mat(Color("555f63"),0.90),Vector3(2.95,0.98,-3.08),root)
		_box(Vector3(3.7,0.24,5.6),roof_m,Vector3(2.95,2.48,-0.45),root)
		_box(Vector3(3.45,0.07,5.35),packed_snow_mat,Vector3(2.95,2.64,-0.45),root)
	_add_house_detail11(root,variant)

func _add_house_detail11(root: Node3D, variant: int) -> void:
	var wood_m: StandardMaterial3D = _textured_mat(Color("785b43"),0.98,0.0,"horizontal",64,0.026)
	var bin_m_a: StandardMaterial3D = _mat(Color("3f5550"),0.94)
	var bin_m_b: StandardMaterial3D = _mat(Color("3b4e4a"),0.94)
	_solid_box(Vector3(0.08,1.05,0.08),metal_mat,Vector3(-6.60,0.53,-2.35),root)
	_solid_box(Vector3(0.42,0.32,0.28),_mat(Color("526a73"),0.84),Vector3(-6.60,1.05,-2.35),root)
	for i: int in range(2):
		var x: float = 4.05-float(i)*0.72
		var bin_mat: Material = bin_m_a if i == 0 else bin_m_b
		_solid_box(Vector3(0.58,0.88,0.56),bin_mat,Vector3(x,0.44,3.76),root)
		_box(Vector3(0.64,0.10,0.60),dark_mat,Vector3(x,0.93,3.76),root)
	for row: int in range(3):
		for col: int in range(4):
			var log: MeshInstance3D = _cylinder(0.12,0.78,wood_m,Vector3(2.25+float(col)*0.28,0.16+float(row)*0.22,3.70),root)
			log.rotation.z = PI/2.0
	_collision_box(root,Vector3(1.45,0.80,0.75),Vector3(2.67,0.40,3.70))
	if variant == 2:
		var sled_m: StandardMaterial3D = _mat(Color("8e3f34"),0.86)
		var rail_a: MeshInstance3D = _cylinder(0.025,1.05,metal_mat,Vector3(-2.3,0.12,3.95),root)
		rail_a.rotation.z = PI/2.0
		var rail_b: MeshInstance3D = _cylinder(0.025,1.05,metal_mat,Vector3(-2.3,0.12,4.25),root)
		rail_b.rotation.z = PI/2.0
		_box(Vector3(0.95,0.07,0.48),sled_m,Vector3(-2.3,0.24,4.10),root)

func _build_collision_pass11() -> void:
	_collision11_root = Node3D.new()
	_collision11_root.name = "PropCollisionPass11"
	add_child(_collision11_root)
	_add_static_collision11("SchoolTrash",Vector3(10.85,0.0,7.02),Vector3(1.05,0.78,0.95),0.0)
	_add_static_collision11("SchoolPallet",Vector3(8.60,0.0,7.05),Vector3(1.48,0.28,1.15),0.0)
	_add_static_collision11("SchoolUtility",Vector3(12.55,0.0,4.25),Vector3(0.76,1.32,0.42),PI/2.0)
	_add_static_collision11("SportTrash",Vector3(39.65,0.0,33.45),Vector3(1.02,0.76,0.82),0.0)
	_add_static_collision11("SportBins",Vector3(42.60,0.0,33.35),Vector3(1.55,1.20,0.78),0.0)
	_add_static_collision11("SportPallet",Vector3(45.0,0.0,33.55),Vector3(1.45,0.28,1.10),PI/2.0)
	_add_static_collision11("HalaPallet",Vector3(39.0,0.0,80.05),Vector3(1.45,0.28,1.10),0.0)
	_add_static_collision11("HalaBoxes",Vector3(40.7,0.0,80.10),Vector3(0.95,1.55,0.78),0.0)
	_add_static_collision11("HalaTrash",Vector3(43.25,0.0,80.0),Vector3(0.90,0.75,0.78),0.0)
	_add_static_collision11("BenchWest",Vector3(-11.0,0.0,18.0),Vector3(2.15,0.70,0.72),0.0)
	_add_static_collision11("BenchEast",Vector3(9.0,0.0,18.0),Vector3(2.15,0.70,0.72),0.0)
	_add_static_collision11("GeneratorCrate",Vector3(67.25,0.0,26.85),Vector3(0.90,0.50,0.62),0.0)

func _add_static_collision11(name_value: String, pos: Vector3, size: Vector3, yaw: float) -> void:
	var anchor: Node3D = Node3D.new()
	anchor.name = name_value
	anchor.position = pos
	anchor.rotation.y = yaw
	_collision11_root.add_child(anchor)
	_collision_box(anchor,size,Vector3(0.0,size.y*0.5,0.0))

func _build_searchables11() -> void:
	var school: Node3D = get_node_or_null("RosviksSkola") as Node3D
	if school != null:
		_add_search_cabinet11(school,Vector3(10.9,0.0,2.38),PI,"STÄDSKÅP",["silvertejp","tandstickor"])
	_add_search_cabinet11(_gameplay_root,Vector3(47.0,0.0,33.25),0.0,"SERVICEFÖRRÅD",["aa","konserv"])
	_add_search_cabinet11(_residential11_root,Vector3(-106.2,0.0,8.6),PI/2.0,"GARAGESKÅP",["ficklampa","aa"])
	_add_car(Vector3(-110.0,0.0,24.4),Color("665d54"),PI/2.0,true)
	var boot: Node3D = Node3D.new()
	boot.name = "AbandonedCarBoot11"
	boot.position = Vector3(-109.5,0.0,24.4)
	_expansion11_root.add_child(boot)
	_register_searchable11(boot,"BAGAGELUCKA",["konserv","tandstickor"],2.0)

func _add_search_cabinet11(parent: Node3D, pos: Vector3, yaw: float, title: String, loot: Array) -> void:
	var root: Node3D = Node3D.new()
	root.name = "Searchable_"+title
	root.position = pos
	root.rotation.y = yaw
	parent.add_child(root)
	var cabinet: StandardMaterial3D = _mat(Color("5d686b"),0.84,0.14)
	var face: StandardMaterial3D = _mat(Color("343b3e"),0.92)
	_solid_box(Vector3(0.82,1.42,0.40),cabinet,Vector3(0.0,0.71,0.0),root)
	_box(Vector3(0.66,1.20,0.025),face,Vector3(0.0,0.73,0.215),root)
	_box(Vector3(0.10,0.05,0.035),metal_mat,Vector3(0.27,0.73,0.235),root)
	_register_searchable11(root,title,loot,1.85)

func _register_searchable11(node: Node3D, title: String, loot: Array, radius: float) -> void:
	_gameplay_interactables.append({"node":node,"kind":"search11","title":title,"radius":radius,"active":true,"loot":loot.duplicate(),"searched":false})

func _prompt_for_interactable(entry: Dictionary) -> String:
	if String(entry["kind"]) == "search11":
		if bool(entry.get("searched",false)):
			return "E  •  TOMT " + String(entry["title"])
		return "E  •  SÖK " + String(entry["title"])
	return super._prompt_for_interactable(entry)

func _activate_gameplay_interactable(index: int) -> void:
	if index >= 0 and index < _gameplay_interactables.size() and String(_gameplay_interactables[index]["kind"]) == "search11":
		_search_container11(index)
		_refresh_gameplay_ui()
		_refresh_loot11_ui()
		return
	super._activate_gameplay_interactable(index)

func _search_container11(index: int) -> void:
	var entry: Dictionary = _gameplay_interactables[index]
	var contents: Array = entry.get("loot",[])
	if contents.is_empty():
		_gameplay_interactables[index]["searched"] = true
		_show_toast("Tomt.")
		return
	var remaining: Array[String] = []
	var found_names: Array[String] = []
	for raw: Variant in contents:
		var key: String = String(raw)
		if _loot_total11() >= _loot_capacity11:
			remaining.append(key)
			continue
		_add_loot11(key)
		found_names.append(_loot_name11(key))
	_gameplay_interactables[index]["loot"] = remaining
	_gameplay_interactables[index]["searched"] = remaining.is_empty()
	if found_names.is_empty():
		_show_toast("Ryggsäcken är full.")
	else:
		_show_toast("Hittade: " + ", ".join(found_names) + ".")

func _add_loot11(key: String) -> void:
	_loot11[key] = int(_loot11.get(key,0))+1
	if key == "ficklampa" and not bool(_inventory.get("ficklampa",false)):
		_inventory["ficklampa"] = true
		_build_player_flashlight11()

func _loot_total11() -> int:
	var total: int = 0
	for key: Variant in _loot11.keys():
		total += int(_loot11[key])
	return total

func _loot_name11(key: String) -> String:
	match key:
		"ficklampa": return "ficklampa"
		"aa": return "AA-batterier"
		"silvertejp": return "silvertejp"
		"konserv": return "konserv"
		"tandstickor": return "tändstickor"
	return key

func _build_player_flashlight11() -> void:
	if player == null or _flashlight11 != null:
		return
	_flashlight11 = SpotLight3D.new()
	_flashlight11.name = "PlayerFlashlight11"
	_flashlight11.position = Vector3(0.0,1.34,0.24)
	_flashlight11.rotation.y = PI
	_flashlight11.light_color = Color("f2e2bd")
	_flashlight11.light_energy = 3.2
	_flashlight11.spot_range = 15.0
	_flashlight11.spot_angle = 31.0
	_flashlight11.shadow_enabled = true
	_flashlight11.visible = false
	player.add_child(_flashlight11)

func _toggle_flashlight11() -> void:
	if not bool(_inventory.get("ficklampa",false)):
		_show_toast("Du har ingen ficklampa.")
		return
	if _flashlight11 == null:
		_build_player_flashlight11()
	_flashlight_on11 = not _flashlight_on11
	if _flashlight11 != null:
		_flashlight11.visible = _flashlight_on11
	_show_toast("Ficklampa " + ("på." if _flashlight_on11 else "av."))
	_refresh_loot11_ui()

func _build_exploration_ui11() -> void:
	var ui: CanvasLayer = CanvasLayer.new()
	ui.layer = 21
	add_child(ui)
	var panel: ColorRect = ColorRect.new()
	panel.position = Vector2(1215.0,690.0)
	panel.size = Vector2(355.0,165.0)
	panel.color = Color(0.018,0.026,0.031,0.84)
	ui.add_child(panel)
	var title: Label = Label.new()
	title.position = Vector2(15.0,10.0)
	title.text = "RYGGSÄCK / UTFORSKNING"
	title.add_theme_font_size_override("font_size",14)
	title.add_theme_color_override("font_color",Color("e5dfc9"))
	panel.add_child(title)
	_loot11_label = Label.new()
	_loot11_label.position = Vector2(15.0,39.0)
	_loot11_label.size = Vector2(325.0,90.0)
	_loot11_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	_loot11_label.add_theme_font_size_override("font_size",12)
	panel.add_child(_loot11_label)
	var hint: Label = Label.new()
	hint.position = Vector2(15.0,132.0)
	hint.text = "E: interagera   F: ficklampa"
	hint.add_theme_font_size_override("font_size",11)
	hint.add_theme_color_override("font_color",Color("aebdb8"))
	panel.add_child(hint)
	_zone11_label = Label.new()
	_zone11_label.position = Vector2(24.0,156.0)
	_zone11_label.size = Vector2(400.0,28.0)
	_zone11_label.add_theme_font_size_override("font_size",12)
	_zone11_label.add_theme_color_override("font_color",Color("c7d0cc"))
	ui.add_child(_zone11_label)

func _refresh_loot11_ui() -> void:
	if _loot11_label == null:
		return
	var parts: Array[String] = []
	for key: Variant in _loot11.keys():
		var count: int = int(_loot11[key])
		if count > 0:
			parts.append(_loot_name11(String(key)) + " ×" + str(count))
	var items: String = "Tom" if parts.is_empty() else "\n".join(parts)
	var lamp_state: String = "PÅ" if _flashlight_on11 else "AV"
	_loot11_label.text = "%d/%d platser\n%s\nFicklampa: %s" % [_loot_total11(),_loot_capacity11,items,lamp_state]

func _update_zone11_label() -> void:
	if _zone11_label == null or player == null:
		return
	var p: Vector3 = player.global_position
	if p.x < -94.0 and p.z > -27.0:
		_zone11_label.text = "ROSVIK • BOSTADSKANT / första utbyggnaden"
	elif p.z > 70.0:
		_zone11_label.text = "ROSVIK • HALA / idrottsområde"
	elif p.x > 30.0:
		_zone11_label.text = "ROSVIK • sporthall / Rosvalla"
	else:
		_zone11_label.text = "ROSVIK • skola / centrumkant"

func _current_objective() -> String:
	if not _school_powered:
		return super._current_objective()
	if not bool(_inventory.get("ficklampa",false)):
		return "Reservkraften är igång. Utforska vidare mot bostadsgatan och sök användbar utrustning."
	return "Utforska Rosvik. Sök förråd, bilar och serviceutrymmen efter sådant som kan behövas senare."

func _validate_expansion11() -> void:
	var road_errors: int = 0
	for edge: Dictionary in _residential_roads11:
		var a: Vector2 = edge["a"]
		var b: Vector2 = edge["b"]
		var w: float = float(edge["w"])
		if _segment_hits_building(a,b,w*0.5):
			road_errors += 1
	if road_errors > 0:
		push_error("World Expansion 11 failed: %d residential road segments intersect buildings" % road_errors)
