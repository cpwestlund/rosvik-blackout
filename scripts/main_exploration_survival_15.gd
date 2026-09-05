extends "res://scripts/main_control_world_presentation_14.gd"

# Exploration + Survival Foundation 15
# No enemies. Rosvik itself is the pressure: cold, hunger, thirst, darkness and
# useful mundane loot. The existing blackout loop now matters because a powered
# school is the first reliable warm refuge.

var _survival15_root: Node3D
var _survival15_layer: CanvasLayer
var _survival15_panel: ColorRect
var _warmth15_bar: ProgressBar
var _hunger15_bar: ProgressBar
var _thirst15_bar: ProgressBar
var _survival15_status: Label
var _warmth15: float = 78.0
var _hunger15: float = 86.0
var _thirst15: float = 82.0
var _equipped_gloves15: bool = false
var _max_weight15: float = 12.0
var _item_weights15: Dictionary = {
	"ficklampa":0.35,"aa":0.12,"silvertejp":0.25,"konserv":0.55,"tandstickor":0.05,
	"vatten":0.65,"energibar":0.10,"vantar":0.20,"verktyg":1.20,"bensin":2.80,
	"termos":0.70,"filt":1.10,"ved":1.60
}

func _ready() -> void:
	super._ready()
	_loot_capacity11 = 99 # weight is the real limiter in this milestone.
	_survival15_root = Node3D.new()
	_survival15_root.name = "ExplorationSurvival15"
	add_child(_survival15_root)
	_build_survival_searchables15()
	_build_survival_ui15()
	_refresh_loot11_ui()
	_refresh_survival15_ui()
	print("ROSVIK_EXPLORATION_SURVIVAL_15_READY")
	print("ROSVIK_WEIGHT_INVENTORY_15_READY")
	print("ROSVIK_WARMTH_SYSTEM_15_READY")
	print("ROSVIK_CONSUMABLES_15_READY")
	print("ROSVIK_WARM_REFUGE_15_READY")

func _process(delta: float) -> void:
	super._process(delta)
	_update_survival15(delta)
	if Input.is_action_just_pressed("consume_food"):
		_consume_food15()
	if Input.is_action_just_pressed("consume_drink"):
		_consume_drink15()
	if Input.is_action_just_pressed("equip_warmth"):
		_toggle_gloves15()

# -------------------------------------------------------------------------
# SURVIVAL SIMULATION - GENTLE FOUNDATION, NOT A PUNISHING TIMER
# -------------------------------------------------------------------------
func _update_survival15(delta: float) -> void:
	if player == null:
		return
	var warm_refuge: bool = _is_warm_refuge15()
	if warm_refuge:
		_warmth15 = minf(100.0,_warmth15+1.15*delta)
	else:
		var cold_rate: float = 0.085
		if _equipped_gloves15:
			cold_rate *= 0.78
		_warmth15 = maxf(0.0,_warmth15-cold_rate*delta)
	_hunger15 = maxf(0.0,_hunger15-0.010*delta)
	_thirst15 = maxf(0.0,_thirst15-0.017*delta)

	# Low warmth affects speed a little, but never wrestles control away from player.
	if _warmth15 < 25.0:
		player.set("walk_speed",3.0)
		player.set("run_speed",4.8)
	else:
		player.set("walk_speed",3.35)
		player.set("run_speed",5.65)
	_refresh_survival15_ui()

func _is_warm_refuge15() -> bool:
	if not _school_powered or player == null:
		return false
	var p := player.global_position
	return p.x > -11.85 and p.x < 12.85 and p.z > -6.10 and p.z < 6.35

# -------------------------------------------------------------------------
# WEIGHT-BASED LOOT: OVERRIDES THE OLD SLOT-ONLY SEARCH BEHAVIOUR
# -------------------------------------------------------------------------
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
		var key := String(raw)
		var weight := _item_weight15(key)
		if _inventory_weight15()+weight > _max_weight15+0.001:
			remaining.append(key)
			continue
		_add_loot11(key)
		found_names.append(_loot_name11(key))
	_gameplay_interactables[index]["loot"] = remaining
	_gameplay_interactables[index]["searched"] = remaining.is_empty()
	if found_names.is_empty():
		_show_toast("För tungt. Lämna eller använd något först.")
	else:
		_show_toast("Hittade: "+", ".join(found_names)+".")
	_refresh_loot11_ui()

func _inventory_weight15() -> float:
	var total: float = 0.0
	for key: Variant in _loot11.keys():
		total += _item_weight15(String(key))*float(int(_loot11[key]))
	return total

func _item_weight15(key: String) -> float:
	return float(_item_weights15.get(key,0.25))

func _loot_name11(key: String) -> String:
	match key:
		"vatten": return "vattenflaska"
		"energibar": return "energibar"
		"vantar": return "vintervantar"
		"verktyg": return "verktygssats"
		"bensin": return "bensindunk"
		"termos": return "termos"
		"filt": return "yllefilt"
		"ved": return "vedträ"
	return super._loot_name11(key)

func _refresh_loot11_ui() -> void:
	if _loot11_label == null:
		return
	var parts: Array[String] = []
	for key: Variant in _loot11.keys():
		var count := int(_loot11[key])
		if count > 0:
			parts.append(_loot_name11(String(key))+" ×"+str(count))
	var items: String = "Tom" if parts.is_empty() else "\n".join(parts)
	var lamp_state: String = "PÅ" if _flashlight_on11 else "AV"
	var gloves_state: String = "på" if _equipped_gloves15 else "—"
	_loot11_label.text = "%.1f / %.1f kg\n%s\nFicklampa: %s   Vantar: %s" % [_inventory_weight15(),_max_weight15,items,lamp_state,gloves_state]

# -------------------------------------------------------------------------
# USE ITEMS
# -------------------------------------------------------------------------
func _consume_food15() -> void:
	var key := "energibar" if int(_loot11.get("energibar",0)) > 0 else "konserv"
	if int(_loot11.get(key,0)) <= 0:
		_show_toast("Ingen mat i ryggsäcken.")
		return
	_remove_loot15(key,1)
	_hunger15 = minf(100.0,_hunger15+(24.0 if key == "energibar" else 42.0))
	_show_toast("Du äter "+_loot_name11(key)+".")
	_refresh_loot11_ui()

func _consume_drink15() -> void:
	var key := "termos" if int(_loot11.get("termos",0)) > 0 else "vatten"
	if int(_loot11.get(key,0)) <= 0:
		_show_toast("Inget att dricka.")
		return
	_remove_loot15(key,1)
	_thirst15 = minf(100.0,_thirst15+(32.0 if key == "termos" else 40.0))
	if key == "termos":
		_warmth15 = minf(100.0,_warmth15+8.0)
	_show_toast("Du dricker "+_loot_name11(key)+".")
	_refresh_loot11_ui()

func _toggle_gloves15() -> void:
	if int(_loot11.get("vantar",0)) <= 0:
		_show_toast("Du har inga vintervantar.")
		return
	_equipped_gloves15 = not _equipped_gloves15
	_show_toast("Vintervantar "+("på." if _equipped_gloves15 else "av."))
	_refresh_loot11_ui()

func _remove_loot15(key: String,count: int) -> void:
	var left := maxi(0,int(_loot11.get(key,0))-count)
	_loot11[key] = left

# -------------------------------------------------------------------------
# MORE REASONS TO SEARCH THE WORLD
# -------------------------------------------------------------------------
func _build_survival_searchables15() -> void:
	# Residential yards: each household has a different mundane survival profile.
	_add_search_crate15(Vector3(-95.45,0.0,-5.35),"VERKTYGSLÅDA",["verktyg","bensin","vantar"],Color("596660"))
	_add_search_crate15(Vector3(-95.35,0.0,13.25),"ALTANLÅDA",["vatten","energibar","filt"],Color("6c5948"))
	_add_search_crate15(Vector3(-95.50,0.0,30.15),"VEDBOD",["ved","tandstickor","termos"],Color("705b46"))

	# Sporthall service edge: vending/service leftovers after the blackout.
	_add_search_crate15(Vector3(77.2,0.0,30.4),"KIOSKFÖRRÅD",["vatten","energibar","aa"],Color("4e6067"))

	# One emergency cupboard inside the powered-school refuge.
	var school: Node3D = get_node_or_null("RosviksSkola") as Node3D
	if school != null:
		_add_search_cabinet11(school,Vector3(11.55,0.0,3.20),PI/2.0,"NÖDSKÅP",["filt","vatten","energibar"])

func _add_search_crate15(pos: Vector3,title: String,loot: Array,color: Color) -> void:
	var root := Node3D.new()
	root.name = "SurvivalCrate15_"+title
	root.position = pos
	_survival15_root.add_child(root)
	var body_m := _textured_mat(color,0.94,0.0,"horizontal",64,0.025)
	var trim_m := _mat(Color("2d3437"),0.84,0.12)
	_solid_box(Vector3(1.05,0.58,0.68),body_m,Vector3(0.0,0.29,0.0),root)
	_box(Vector3(1.10,0.09,0.72),body_m,Vector3(0.0,0.63,0.0),root)
	_box(Vector3(0.34,0.08,0.04),trim_m,Vector3(0.0,0.42,0.36),root)
	_register_searchable11(root,title,loot,1.8)

# -------------------------------------------------------------------------
# SURVIVAL UI
# -------------------------------------------------------------------------
func _build_survival_ui15() -> void:
	_survival15_layer = CanvasLayer.new()
	_survival15_layer.layer = 23
	add_child(_survival15_layer)
	_survival15_panel = ColorRect.new()
	_survival15_panel.position = Vector2(20.0,664.0)
	_survival15_panel.size = Vector2(332.0,188.0)
	_survival15_panel.color = Color(0.016,0.024,0.029,0.86)
	_survival15_layer.add_child(_survival15_panel)
	var title := Label.new()
	title.position = Vector2(14.0,10.0)
	title.text = "ÖVERLEVNAD"
	title.add_theme_font_size_override("font_size",14)
	title.add_theme_color_override("font_color",Color("e8dfc8"))
	_survival15_panel.add_child(title)
	_warmth15_bar = _make_survival_bar15("VÄRME",38.0)
	_hunger15_bar = _make_survival_bar15("MÄTTNAD",72.0)
	_thirst15_bar = _make_survival_bar15("VÄTSKA",106.0)
	_survival15_status = Label.new()
	_survival15_status.position = Vector2(14.0,137.0)
	_survival15_status.size = Vector2(304.0,42.0)
	_survival15_status.add_theme_font_size_override("font_size",11)
	_survival15_status.add_theme_color_override("font_color",Color("bdc8c3"))
	_survival15_panel.add_child(_survival15_status)

func _make_survival_bar15(title_text: String,y: float) -> ProgressBar:
	var label := Label.new()
	label.position = Vector2(14.0,y-6.0)
	label.text = title_text
	label.add_theme_font_size_override("font_size",10)
	_survival15_panel.add_child(label)
	var bar := ProgressBar.new()
	bar.position = Vector2(82.0,y)
	bar.size = Vector2(230.0,12.0)
	bar.min_value = 0.0
	bar.max_value = 100.0
	bar.show_percentage = false
	_survival15_panel.add_child(bar)
	return bar

func _refresh_survival15_ui() -> void:
	if _warmth15_bar == null:
		return
	_warmth15_bar.value = _warmth15
	_hunger15_bar.value = _hunger15
	_thirst15_bar.value = _thirst15
	var place := "VARMT SKYDD" if _is_warm_refuge15() else "UTOMHUS • -14°C"
	var gear := "vantar" if _equipped_gloves15 else "inga vantar"
	_survival15_status.text = "%s • %s\n1: ät   2: drick   G: vantar" % [place,gear]

func _current_objective() -> String:
	if not _school_powered:
		return super._current_objective()
	if _warmth15 < 42.0:
		return "Du börjar bli kall. Skolan har reservkraft och är ett varmt skydd."
	return "Utforska Rosvik: hitta mat, vatten, kläder och verktyg. Reservkraften gör skolan till din första trygga bas."
