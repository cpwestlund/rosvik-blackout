extends "res://scripts/main_exploration_survival_15.gd"

# Visual Identity + World Cleanup 16
# This milestone deliberately pauses feature sprawl and moves the whole game
# toward the agreed visual target: softer winter forms, colder exterior / warmer
# refuge contrast, cleaner diegetic presentation and less prototype HUD.
# Player movement itself is now world-locked in player.gd and never reads camera.

var _visual16_root: Node3D
var _hud16_layer: CanvasLayer
var _inventory16_panel: Panel
var _status16_panel: Panel
var _soft_form_count16: int = 0
var _cleanup_count16: int = 0

func _ready() -> void:
	super._ready()
	_visual16_root = Node3D.new()
	_visual16_root.name = "VisualIdentityWorld16"
	add_child(_visual16_root)
	_tune_environment16()
	_cleanup_floating_world16()
	_build_soft_winter_forms16()
	_upgrade_residential_silhouettes16()
	_rebuild_visual_hud16()
	_refresh_loot11_ui()
	_refresh_survival15_ui()
	print("ROSVIK_WORLD_AXIS_WASD_16_READY")
	print("ROSVIK_VISUAL_IDENTITY_16_READY")
	print("ROSVIK_ROUNDED_HUD_16_READY")
	print("ROSVIK_SOFT_WORLD_FORMS_16_READY count=",_soft_form_count16)
	print("ROSVIK_FLOATING_CLEANUP_16_READY count=",_cleanup_count16)
	print("ROSVIK_RESIDENTIAL_SILHOUETTE_16_READY")

# -------------------------------------------------------------------------
# ATMOSPHERE: COLD NORRBOTTEN EXTERIOR, WARM LIGHT MEANS SOMETHING
# -------------------------------------------------------------------------
func _tune_environment16() -> void:
	var env_nodes: Array[WorldEnvironment] = []
	_collect_world_environments16(self,env_nodes)
	for env_node: WorldEnvironment in env_nodes:
		if env_node.environment == null:
			continue
		var env: Environment = env_node.environment
		env.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
		env.ambient_light_color = Color("667983")
		env.ambient_light_energy = 0.24
		env.fog_enabled = true
		env.fog_light_color = Color("536873")
		env.fog_density = 0.0105
		env.fog_height = 0.20
		env.fog_height_density = 0.095
		env.tonemap_mode = Environment.TONE_MAPPER_FILMIC
		env.tonemap_exposure = 0.52
		if env.sky != null and env.sky.sky_material is ProceduralSkyMaterial:
			var sky_mat := env.sky.sky_material as ProceduralSkyMaterial
			sky_mat.sky_top_color = Color("172832")
			sky_mat.sky_horizon_color = Color("60727a")
			sky_mat.ground_bottom_color = Color("26363d")
			sky_mat.ground_horizon_color = Color("849095")
			sky_mat.sun_angle_max = 10.0

	var suns: Array[DirectionalLight3D] = []
	_collect_directional_lights16(self,suns)
	for sun: DirectionalLight3D in suns:
		sun.light_color = Color("efbb92")
		sun.light_energy = 0.54
		sun.shadow_enabled = true

func _collect_world_environments16(node: Node,out: Array[WorldEnvironment]) -> void:
	for child: Node in node.get_children():
		if child is WorldEnvironment:
			out.append(child as WorldEnvironment)
		_collect_world_environments16(child,out)

func _collect_directional_lights16(node: Node,out: Array[DirectionalLight3D]) -> void:
	for child: Node in node.get_children():
		if child is DirectionalLight3D:
			out.append(child as DirectionalLight3D)
		_collect_directional_lights16(child,out)

# -------------------------------------------------------------------------
# FLOATING / ORPHAN CLEANUP
# -------------------------------------------------------------------------
func _cleanup_floating_world16() -> void:
	# Kill the old freestanding prototype sign cluster decisively. It has survived
	# earlier passes because pole, board and text are separate siblings.
	var coherence: Node = get_node_or_null("WorldCoherence09")
	if coherence != null:
		for child: Node in coherence.get_children():
			if child is Node3D:
				var n := child as Node3D
				var p := n.global_position
				if Vector2(p.x-27.0,p.z-32.05).length() < 4.2:
					_hide_and_disable16(n)

	# No level-editor wayfinding text in empty space. Building identity and labels
	# that are physically attached to electrical/door objects are kept.
	var labels: Array[Label3D] = []
	_collect_label3d16(self,labels)
	for label: Label3D in labels:
		var text_value := label.text.strip_edges()
		if text_value == "BOSTÄDER" or "→" in text_value or "SPORTHALL  " in text_value:
			label.visible = false
			_cleanup_count16 += 1

	# Front-school legacy fence rails were authored as independent siblings. Remove
	# any remaining low, long, thin rail before the clean z=15.35 fence from pass 14.
	var school: Node3D = get_node_or_null("RosviksSkola") as Node3D
	if school != null:
		_cleanup_old_school_rails16(school)

func _collect_label3d16(node: Node,out: Array[Label3D]) -> void:
	for child: Node in node.get_children():
		if child is Label3D:
			out.append(child as Label3D)
		_collect_label3d16(child,out)

func _hide_and_disable16(node: Node3D) -> void:
	if node is GeometryInstance3D:
		(node as GeometryInstance3D).visible = false
		_cleanup_count16 += 1
	if node is CollisionObject3D:
		var body := node as CollisionObject3D
		body.collision_layer = 0
		body.collision_mask = 0
	for child: Node in node.get_children():
		if child is Node3D:
			_hide_and_disable16(child as Node3D)

func _cleanup_old_school_rails16(node: Node) -> void:
	for child: Node in node.get_children():
		if child is MeshInstance3D:
			var mesh := child as MeshInstance3D
			var p := mesh.global_position
			if p.z > 9.5 and p.z < 15.0 and p.y > 0.35 and p.y < 1.45:
				var s := mesh.get_aabb().size
				var rail_like := (s.x > 1.25 and s.y < 0.18 and s.z < 0.22) or (s.z > 1.25 and s.y < 0.18 and s.x < 0.22)
				if rail_like:
					mesh.visible = false
					_cleanup_count16 += 1
		elif child is StaticBody3D:
			var body := child as StaticBody3D
			var bp := body.global_position
			if bp.z > 9.5 and bp.z < 15.0 and bp.y < 1.5:
				for shape_node: Node in body.get_children():
					if shape_node is CollisionShape3D:
						var cs := shape_node as CollisionShape3D
						if cs.shape is BoxShape3D:
							var bs := (cs.shape as BoxShape3D).size
							var rail_collision := (bs.x > 1.25 and bs.y < 1.3 and bs.z < 0.25) or (bs.z > 1.25 and bs.y < 1.3 and bs.x < 0.25)
							if rail_collision:
								body.collision_layer = 0
								body.collision_mask = 0
		_cleanup_old_school_rails16(child)

# -------------------------------------------------------------------------
# SOFTER WORLD FORMS: CURVED SNOW, BUSHES, ACCUMULATION
# -------------------------------------------------------------------------
func _build_soft_winter_forms16() -> void:
	# School / civic edges.
	_soft_snow_mound16(Vector3(-20.0,0.0,16.4),Vector3(3.2,0.48,1.15),0.08)
	_soft_snow_mound16(Vector3(18.2,0.0,16.8),Vector3(2.7,0.42,1.05),-0.10)
	_soft_snow_mound16(Vector3(35.0,0.0,50.6),Vector3(3.6,0.56,1.20),0.05)
	_soft_snow_mound16(Vector3(68.5,0.0,50.8),Vector3(3.0,0.50,1.10),-0.06)
	_soft_snow_mound16(Vector3(37.5,0.0,92.2),Vector3(3.4,0.58,1.30),0.10)

	# Residential street: windblown accumulations and shrubs make the edge feel
	# inhabited rather than three boxes placed beside asphalt.
	for p: Vector3 in [Vector3(-95.0,0.0,-2.4),Vector3(-95.4,0.0,17.0),Vector3(-95.2,0.0,34.2),Vector3(-107.4,0.0,-18.0),Vector3(-107.0,0.0,31.5)]:
		_soft_snow_mound16(p,Vector3(2.15,0.38,0.88),randf_range(-0.16,0.16))
	for p: Vector3 in [Vector3(-94.8,0.0,-13.6),Vector3(-95.1,0.0,4.2),Vector3(-95.0,0.0,21.2),Vector3(-105.8,0.0,31.8)]:
		_winter_bush16(p,0.9)

func _soft_snow_mound16(pos: Vector3,scale_value: Vector3,yaw: float) -> void:
	var root := Node3D.new()
	root.position = pos
	root.rotation.y = yaw
	_visual16_root.add_child(root)
	var mesh_instance := MeshInstance3D.new()
	var sphere := SphereMesh.new()
	sphere.radius = 1.0
	sphere.height = 2.0
	sphere.radial_segments = 24
	sphere.rings = 12
	mesh_instance.mesh = sphere
	mesh_instance.material_override = packed_snow_mat
	mesh_instance.scale = scale_value
	mesh_instance.position.y = -0.28 + scale_value.y*0.42
	mesh_instance.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_ON
	root.add_child(mesh_instance)
	_soft_form_count16 += 1

func _winter_bush16(pos: Vector3,scale_value: float) -> void:
	var root := Node3D.new()
	root.position = pos
	root.scale = Vector3.ONE*scale_value
	_visual16_root.add_child(root)
	var twig := _mat(Color("4b4039"),0.98)
	var dark_green := _mat(Color("30433e"),0.97)
	for i: int in range(5):
		var a := -0.75+float(i)*0.37
		var stem := _cylinder(0.025,1.05,twig,Vector3(a*0.28,0.50,absf(a)*0.12),root)
		stem.rotation.z = a*0.34
	for p: Vector3 in [Vector3(-0.32,0.55,0.05),Vector3(0.0,0.72,-0.08),Vector3(0.34,0.52,0.08)]:
		var leaf := MeshInstance3D.new()
		var sphere := SphereMesh.new()
		sphere.radius = 0.38
		sphere.height = 0.76
		sphere.radial_segments = 16
		sphere.rings = 8
		leaf.mesh = sphere
		leaf.material_override = dark_green
		leaf.position = p
		leaf.scale = Vector3(1.0,0.72,0.82)
		root.add_child(leaf)
	_soft_snow_mound16(pos+Vector3(0.0,0.0,0.08),Vector3(0.72,0.17,0.55),0.0)
	_soft_form_count16 += 1

# -------------------------------------------------------------------------
# RESIDENTIAL SILHOUETTES: GIVE THE VILLAS GABLES, EAVES AND SMALL DETAILS
# -------------------------------------------------------------------------
func _upgrade_residential_silhouettes16() -> void:
	if _residential11_root == null:
		return
	for spec: Dictionary in _residential_specs11:
		var variant := int(spec["variant"])
		var root := _residential11_root.get_node_or_null("ResidentialHouse11_%d" % variant) as Node3D
		if root == null:
			continue
		var wall_color: Color = spec["color"]
		var wall_m := _textured_mat(wall_color,0.94,0.0,"horizontal",72,0.020)
		wall_m.cull_mode = BaseMaterial3D.CULL_DISABLED
		_add_gable16(root,-4.71,wall_m)
		_add_gable16(root,4.71,wall_m)

		# Gutter lines and downpipe break the giant-box silhouette.
		var gutter_m := _mat(Color("424c50"),0.80,0.16)
		for z_side: float in [-3.58,3.58]:
			var gutter := _cylinder(0.045,9.85,gutter_m,Vector3(0.0,3.27,z_side),root)
			gutter.rotation.z = PI/2.0
		_cylinder(0.042,2.72,gutter_m,Vector3(-4.53,1.53,-3.55),root)

		# A real little canopy/porch zone, not just a slab on the ground.
		var canopy_m := _textured_mat(Color("3d4548"),0.90,0.04,"noise",64,0.018)
		var canopy := _box(Vector3(1.95,0.14,2.45),canopy_m,Vector3(-5.24,2.54,1.55),root)
		canopy.rotation.z = -0.05
		_soft_snow_mound16(root.global_position+Vector3(-5.28,0.0,1.55),Vector3(1.05,0.16,1.25),0.0)

		# Chimney and warm porch pool make each home read at distance.
		var chimney_m := _textured_mat(Color("665b52"),0.96,0.0,"noise",48,0.025)
		_solid_box(Vector3(0.58,1.55,0.58),chimney_m,Vector3(2.0,3.88,-0.70),root)
		_box(Vector3(0.72,0.12,0.72),_mat(Color("30373a"),0.86),Vector3(2.0,4.68,-0.70),root)
		var porch_light := OmniLight3D.new()
		porch_light.position = Vector3(-5.05,2.16,2.38)
		porch_light.light_color = Color("efc28b")
		porch_light.light_energy = 0.22 if variant != 1 else 0.10
		porch_light.omni_range = 3.4
		porch_light.shadow_enabled = false
		root.add_child(porch_light)

func _add_gable16(parent: Node3D,x_pos: float,mat: Material) -> void:
	var arrays: Array = []
	arrays.resize(Mesh.ARRAY_MAX)
	arrays[Mesh.ARRAY_VERTEX] = PackedVector3Array([
		Vector3(x_pos,3.02,-3.48),Vector3(x_pos,3.02,3.48),Vector3(x_pos,4.20,0.0)
	])
	var normal := Vector3(-1.0,0.0,0.0) if x_pos < 0.0 else Vector3(1.0,0.0,0.0)
	arrays[Mesh.ARRAY_NORMAL] = PackedVector3Array([normal,normal,normal])
	var mesh := ArrayMesh.new()
	mesh.add_surface_from_arrays(Mesh.PRIMITIVE_TRIANGLES,arrays)
	var instance := MeshInstance3D.new()
	instance.mesh = mesh
	instance.material_override = mat
	instance.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_ON
	parent.add_child(instance)

# -------------------------------------------------------------------------
# HUD: ROUNDED, QUIETER, LESS DEBUG-PANEL
# -------------------------------------------------------------------------
func _rebuild_visual_hud16() -> void:
	if _survival15_panel != null:
		_survival15_panel.visible = false
	if _loot11_label != null and _loot11_label.get_parent() is CanvasItem:
		(_loot11_label.get_parent() as CanvasItem).visible = false

	_hud16_layer = CanvasLayer.new()
	_hud16_layer.layer = 32
	add_child(_hud16_layer)

	_status16_panel = Panel.new()
	_status16_panel.position = Vector2(22.0,704.0)
	_status16_panel.size = Vector2(350.0,158.0)
	_status16_panel.add_theme_stylebox_override("panel",_rounded_panel16(Color(0.025,0.038,0.044,0.90),18))
	_hud16_layer.add_child(_status16_panel)

	var title := Label.new()
	title.position = Vector2(18.0,12.0)
	title.text = "ROSVIK • STATUS"
	title.add_theme_font_size_override("font_size",13)
	title.add_theme_color_override("font_color",Color("e8e0cf"))
	_status16_panel.add_child(title)
	_warmth15_bar = _pill_bar16(_status16_panel,"VÄRME",43.0,Color("c78961"))
	_hunger15_bar = _pill_bar16(_status16_panel,"MÄTTNAD",72.0,Color("9aa178"))
	_thirst15_bar = _pill_bar16(_status16_panel,"VÄTSKA",101.0,Color("759ba8"))
	_survival15_status = Label.new()
	_survival15_status.position = Vector2(18.0,126.0)
	_survival15_status.size = Vector2(315.0,26.0)
	_survival15_status.add_theme_font_size_override("font_size",10)
	_survival15_status.add_theme_color_override("font_color",Color("b8c5c3"))
	_status16_panel.add_child(_survival15_status)

	_inventory16_panel = Panel.new()
	_inventory16_panel.position = Vector2(1230.0,678.0)
	_inventory16_panel.size = Vector2(346.0,184.0)
	_inventory16_panel.add_theme_stylebox_override("panel",_rounded_panel16(Color(0.025,0.036,0.040,0.90),18))
	_hud16_layer.add_child(_inventory16_panel)
	var inv_title := Label.new()
	inv_title.position = Vector2(18.0,12.0)
	inv_title.text = "RYGGSÄCK"
	inv_title.add_theme_font_size_override("font_size",13)
	inv_title.add_theme_color_override("font_color",Color("e8e0cf"))
	_inventory16_panel.add_child(inv_title)
	_loot11_label = Label.new()
	_loot11_label.position = Vector2(18.0,39.0)
	_loot11_label.size = Vector2(310.0,120.0)
	_loot11_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	_loot11_label.add_theme_font_size_override("font_size",11)
	_loot11_label.add_theme_color_override("font_color",Color("c5cfca"))
	_inventory16_panel.add_child(_loot11_label)
	var inv_hint := Label.new()
	inv_hint.position = Vector2(18.0,158.0)
	inv_hint.text = "E interagera  •  F ficklampa"
	inv_hint.add_theme_font_size_override("font_size",10)
	inv_hint.add_theme_color_override("font_color",Color("829692"))
	_inventory16_panel.add_child(inv_hint)

func _rounded_panel16(color: Color,radius: int) -> StyleBoxFlat:
	var style := StyleBoxFlat.new()
	style.bg_color = color
	style.corner_radius_top_left = radius
	style.corner_radius_top_right = radius
	style.corner_radius_bottom_left = radius
	style.corner_radius_bottom_right = radius
	style.border_width_left = 1
	style.border_width_top = 1
	style.border_width_right = 1
	style.border_width_bottom = 1
	style.border_color = Color(0.35,0.45,0.46,0.24)
	return style

func _pill_bar16(parent: Control,title_text: String,y: float,fill_color: Color) -> ProgressBar:
	var label := Label.new()
	label.position = Vector2(18.0,y-7.0)
	label.size = Vector2(70.0,20.0)
	label.text = title_text
	label.add_theme_font_size_override("font_size",9)
	label.add_theme_color_override("font_color",Color("aebbb7"))
	parent.add_child(label)
	var bar := ProgressBar.new()
	bar.position = Vector2(88.0,y)
	bar.size = Vector2(238.0,11.0)
	bar.min_value = 0.0
	bar.max_value = 100.0
	bar.show_percentage = false
	var bg := StyleBoxFlat.new()
	bg.bg_color = Color(0.09,0.12,0.13,0.92)
	for corner: String in ["corner_radius_top_left","corner_radius_top_right","corner_radius_bottom_left","corner_radius_bottom_right"]:
		bg.set(corner,6)
	var fill := StyleBoxFlat.new()
	fill.bg_color = fill_color
	for corner: String in ["corner_radius_top_left","corner_radius_top_right","corner_radius_bottom_left","corner_radius_bottom_right"]:
		fill.set(corner,6)
	bar.add_theme_stylebox_override("background",bg)
	bar.add_theme_stylebox_override("fill",fill)
	parent.add_child(bar)
	return bar
