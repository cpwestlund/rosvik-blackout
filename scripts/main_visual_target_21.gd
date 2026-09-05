extends "res://scripts/main_visual_vertical_slice_20.gd"

# ROSVIK VISUAL TARGET 21
# This pass deliberately treats the school frontage + entrance + first interior
# rooms as a release-quality hero location, not as a map blockout.

var _hero21 := Node3D.new()

func _ready() -> void:
	super._ready()
	_hero21.name = "VisualTarget21"
	add_child(_hero21)
	_upgrade_environment21()
	_upgrade_school_facade21()
	_upgrade_school_entry21()
	_upgrade_schoolyard21()
	_upgrade_interior21()
	_upgrade_background_context21()
	_upgrade_rosvalla21()
	print("ROSVIK_VISUAL_TARGET_21_READY")
	print("ROSVIK_HERO_SCHOOL_21_READY")
	print("ROSVIK_COZY_BLACKOUT_21_READY")
	print("ROSVIK_INTERIOR_DIORAMA_21_READY")
	print("ROSVIK_WORLD_COHESION_21_READY")

func _upgrade_environment21() -> void:
	# Stronger late-winter-afternoon separation: cold environment, warm human light.
	var env_node: WorldEnvironment = null
	for child: Node in get_children():
		if child is WorldEnvironment:
			env_node = child
			break
	if env_node != null and env_node.environment != null:
		var env := env_node.environment
		env.ambient_light_color = Color("586b76")
		env.ambient_light_energy = 0.27
		env.fog_light_color = Color("5e7078")
		env.fog_density = 0.008
		env.tonemap_exposure = 0.64
		# Subtle glow around warm emissive windows if the renderer supports it.
		if "glow_enabled" in env:
			env.glow_enabled = true
			env.glow_intensity = 0.58
	# Add soft blue fill to keep snow readable in shadows.
	var fill := DirectionalLight3D.new()
	fill.rotation_degrees = Vector3(-64,132,0)
	fill.light_color = Color("89a9ba")
	fill.light_energy = 0.16
	fill.shadow_enabled = false
	_hero21.add_child(fill)

func _upgrade_school_facade21() -> void:
	if _school_root == null:
		return
	var facade := Node3D.new()
	facade.name = "HeroFacade21"
	_school_root.add_child(facade)
	var timber := _textured_mat(Color("858983"),0.95,"horizontal",128,0.030)
	var trim := _mat(Color("d2d0c4"),0.86)
	var metal_dark := _mat(Color("263239"),0.62,0.22)
	var rusty := _mat(Color("6d5141"),0.82,0.10)
	# Bottom plinth gives the long facade weight and contact with the ground.
	_box(Vector3(33.6,0.50,0.20),_textured_mat(Color("6a716f"),0.98,"noise",96,0.028),Vector3(0,0.38,6.13),facade)
	# Slight timber banding breaks the monolithic slab feeling.
	for y: float in [0.92,1.55,2.18,2.81,3.44]:
		_box(Vector3(33.4,0.028,0.035),timber,Vector3(0,y,6.22),facade)
	# Gutter + downpipes, including elbows near ground.
	_cylinder(0.055,33.8,metal_dark,Vector3(0,4.16,6.34),facade).rotation_degrees.z = 90
	for x: float in [-15.8,15.8]:
		_cylinder(0.047,3.65,metal_dark,Vector3(x,2.18,6.34),facade)
		var elbow := _cylinder(0.047,0.48,metal_dark,Vector3(x+(0.15 if x<0 else -0.15),0.42,6.34),facade)
		elbow.rotation_degrees.z = 56
	# Real facade sign slab, not floating label.
	_box(Vector3(5.9,0.58,0.12),metal_dark,Vector3(6.0,3.57,6.31),facade)
	_label3d19("ROSVIKS SKOLA",Vector3(6.0,3.57,6.39),facade,30)
	# Mechanical/service details make the wall feel occupied.
	_box(Vector3(0.72,0.98,0.32),_mat(Color("586263"),0.78,0.18),Vector3(14.0,0.73,6.36),facade)
	for i: int in range(4):
		_box(Vector3(0.08,0.58,0.05),dark_mat,Vector3(13.78+float(i)*0.15,0.74,6.56),facade)
	_box(Vector3(0.58,0.22,0.18),rusty,Vector3(-14.6,1.02,6.38),facade)

func _upgrade_school_entry21() -> void:
	if _school_root == null:
		return
	var entry := Node3D.new()
	entry.name = "HeroEntrance21"
	_school_root.add_child(entry)
	var dark := _mat(Color("202c32"),0.68,0.16)
	var steel := _mat(Color("667278"),0.52,0.28)
	var warm_glass := _mat(Color(0.48,0.36,0.24,0.48),0.22,0.02)
	warm_glass.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	warm_glass.emission_enabled = true
	warm_glass.emission = Color("ffb86e")
	warm_glass.emission_energy_multiplier = 1.35
	# Deeper vestibule canopy and side glazing.
	_box(Vector3(5.7,0.24,2.95),dark,Vector3(-5.0,3.10,7.34),entry)
	_box(Vector3(5.45,0.09,2.66),packed_snow_mat,Vector3(-5.0,3.27,7.30),entry)
	for x: float in [-7.55,-2.45]:
		_solid_box(Vector3(0.16,3.0,0.18),steel,Vector3(x,1.50,7.05),entry)
		_box(Vector3(1.00,2.35,0.07),warm_glass,Vector3(x+(0.50 if x<0 else -0.50),1.45,7.05),entry)
	# Warm vestibule ceiling light spilling onto snow.
	var l := OmniLight3D.new()
	l.position = Vector3(-5.0,2.55,7.60)
	l.light_color = Color("ffc078")
	l.light_energy = 2.35
	l.omni_range = 6.5
	l.shadow_enabled = true
	entry.add_child(l)
	# Rubber mat, salt bucket, footprints and a cable crossing the threshold.
	_box(Vector3(2.0,0.025,1.05),_textured_mat(Color("303536"),0.99,"noise",64,0.08),Vector3(-5.0,0.07,6.72),entry)
	_add_bin20(entry,Vector3(-7.75,0,7.55),Color("465149"))
	_add_footpath20(entry,Vector3(-5.0,0,7.1),Vector3(-5.2,0,13.2),14)
	_add_cable21(entry,[Vector3(-2.2,0.10,11.0),Vector3(-3.8,0.09,9.2),Vector3(-5.0,0.08,7.2),Vector3(-5.0,0.10,5.4)])

func _upgrade_schoolyard21() -> void:
	var yard := Node3D.new()
	yard.name = "HeroSchoolyard21"
	_school_root.add_child(yard)
	# Dense but grounded everyday clutter along the facade.
	for spec: Dictionary in [
		{"p":Vector3(-13.4,0,8.25),"c":Color("3d4944")},
		{"p":Vector3(12.7,0,8.15),"c":Color("48514b")},
		{"p":Vector3(14.0,0,8.20),"c":Color("39453f")}
	]:
		_add_bin20(yard,spec["p"],spec["c"])
	# Pallets, firewood and bags by service end.
	for i: int in range(3):
		_add_pallet21(yard,Vector3(14.8+float(i)*0.72,0.02,7.35+float(i%2)*0.48),0.02*float(i))
	for i: int in range(5):
		_add_garbage_bag21(yard,Vector3(12.2+float(i)*0.32,0.22,7.25+float(i%2)*0.28),0.17+0.02*float(i))
	_add_woodpile21(yard,Vector3(-13.3,0.0,7.6))
	# Benches become little scenes.
	_add_thermos19(yard,Vector3(9.35,0.88,10.55))
	_add_mug19(yard,Vector3(9.75,0.82,10.50),Color("b26f4e"))
	for i: int in range(3):
		_add_mitten20(yard,Vector3(8.8+float(i)*0.22,0.11,10.0+float(i%2)*0.18),Color("77554e"),float(i)*0.5)
	# Snow edges are irregular and narrow, not giant white pills.
	for x: float in [-15.5,-11.5,-7.5,2.5,6.5,10.5,14.5]:
		_snow_mound21(yard,Vector3(x,0.0,7.25),Vector3(1.8,0.18,0.38),0.08*sin(x))
	# Street-side snowbank interrupted by crossings.
	for x: float in [-14,-10,-1,4,9,14]:
		_snow_mound21(yard,Vector3(x,0.0,16.15),Vector3(2.2,0.22,0.48),0.03*x)

func _upgrade_interior21() -> void:
	# Add visual stories inside without changing the readable low-partition layout.
	if _school_root == null:
		return
	var interior := Node3D.new()
	interior.name = "InteriorStory21"
	_school_root.add_child(interior)
	# Corridor clutter.
	_add_notice_cluster21(interior,Vector3(-1.9,1.62,2.08))
	_add_fire_extinguisher21(interior,Vector3(-0.2,0.72,2.08))
	_add_cleaning_cart21(interior,Vector3(-13.6,0.0,4.25))
	# Classroom life: pencil cups, notebooks, lunchbox, scattered chair.
	for p: Vector3 in [Vector3(4.2,0.97,-0.2),Vector3(7.2,0.97,-2.55),Vector3(10.2,0.97,-0.2)]:
		_add_pencilcup21(interior,p)
		_add_notebook21(interior,p+Vector3(0.36,0.02,0.05),0.2)
	_add_lunchbox21(interior,Vector3(6.75,0.28,-4.2))
	_asset19("furniture/chair.glb",Vector3(11.6,0.16,-4.0),Vector3.ONE*0.9,-0.55,interior)
	# Reading corner gets soft rug + low storage.
	_box(Vector3(4.8,0.028,3.0),_mat(Color("79685e"),0.96),Vector3(-7.6,0.19,-2.35),interior)
	_box(Vector3(2.8,0.60,0.55),_mat(Color("8a765b"),0.90),Vector3(-11.2,0.48,-4.5),interior)
	for i: int in range(7):
		_box(Vector3(0.20,0.42+0.05*float(i%3),0.36),_mat([Color("7b5845"),Color("5d6f78"),Color("8e7b4d")][i%3],0.92),Vector3(-12.25+float(i)*0.34,0.95,-4.18),interior)
	# Warm local light pockets – not a uniformly lit room.
	for p: Vector3 in [Vector3(-10.0,2.25,-1.5),Vector3(5.7,2.30,-1.4),Vector3(12.0,2.15,-3.2)]:
		var l := OmniLight3D.new()
		l.position = p
		l.light_color = Color("ffc67d")
		l.light_energy = 0.0
		l.omni_range = 4.8
		l.shadow_enabled = true
		interior.add_child(l)
		_school_lights.append(l)

func _upgrade_background_context21() -> void:
	# A few believable warm/cold silhouettes around the hero area. They are context,
	# never substitutes for Rosvik-specific hero buildings.
	var context := Node3D.new()
	context.name = "BackgroundContext21"
	add_child(context)
	for spec: Dictionary in [
		{"p":Vector3(-37,0,-19),"r":0.08,"s":1.7},
		{"p":Vector3(-38,0,33),"r":-0.12,"s":1.6},
		{"p":Vector3(42,0,-20),"r":0.04,"s":1.55}
	]:
		_asset19("buildings/cottage.glb",spec["p"],Vector3.ONE*float(spec["s"]),float(spec["r"]),context)
	# Vegetation clusters break the endless flat snow plane.
	for p: Vector3 in [Vector3(-32,0,20),Vector3(-35,0,24),Vector3(29,0,-13),Vector3(33,0,-16),Vector3(38,0,5),Vector3(-29,0,-12)]:
		_asset19("nature/tree-pine.glb",p,Vector3.ONE*(1.35+0.12*sin(p.x)),0.0,context)

func _upgrade_rosvalla21() -> void:
	var field := get_node_or_null("Rosvalla19") as Node3D
	if field == null:
		return
	# Sideline microdetail and modest snow ridges around fencing.
	for p: Vector3 in [Vector3(-19,0,-21),Vector3(28,0,-21),Vector3(-19,0,21),Vector3(26,0,21)]:
		_snow_mound21(field,Vector3(9,0,53)+p,Vector3(2.8,0.22,0.55),0.0)
	_add_pallet21(field,Vector3(29.5,0.03,31.3),0.08)
	_add_pallet21(field,Vector3(30.3,0.03,31.2),-0.05)
	_add_bin20(field,Vector3(31.0,0,31.0),Color("3e4944"))

func _add_pallet21(parent: Node3D,pos: Vector3,yaw: float) -> void:
	var root := Node3D.new()
	root.position = pos
	root.rotation.y = yaw
	parent.add_child(root)
	for z: float in [-0.30,0.0,0.30]:
		_box(Vector3(1.1,0.07,0.16),wood_mat,Vector3(0,0.14,z),root)
	for x: float in [-0.42,0.0,0.42]:
		_box(Vector3(0.12,0.12,0.82),wood_mat,Vector3(x,0.07,0),root)

func _add_garbage_bag21(parent: Node3D,pos: Vector3,scale_value: float) -> void:
	var m := _capsule19(scale_value,scale_value*1.8,_mat(Color("222829"),0.98),pos,parent)
	m.scale = Vector3(0.86,1.0,0.78)
	var tie := _capsule19(scale_value*0.22,scale_value*0.32,_mat(Color("1a2021"),0.98),pos+Vector3(0,scale_value*1.15,0),parent)
	tie.scale = Vector3(0.8,0.7,0.8)

func _add_woodpile21(parent: Node3D,pos: Vector3) -> void:
	var root := Node3D.new()
	root.position = pos
	parent.add_child(root)
	for row: int in range(3):
		for col: int in range(6):
			var log := _cylinder(0.105,0.82,_mat(Color("6b4a35").lightened(0.03*float((row+col)%3)),0.96),Vector3(float(col)*0.23,0.18+float(row)*0.19,0),root)
			log.rotation_degrees.z = 90

func _snow_mound21(parent: Node3D,pos: Vector3,scale_value: Vector3,yaw: float) -> void:
	for i: int in range(4):
		var mesh := SphereMesh.new()
		mesh.radius = 1.0
		mesh.height = 2.0
		mesh.radial_segments = 20
		mesh.rings = 10
		var n := MeshInstance3D.new()
		n.mesh = mesh
		n.material_override = packed_snow_mat
		n.position = pos+Vector3((float(i)-1.5)*scale_value.x*0.34,0.02,0)
		n.scale = Vector3(scale_value.x*0.40,scale_value.y*(0.76+0.08*float(i%2)),scale_value.z*(0.72+0.06*float(i)))
		n.rotation.y = yaw+0.05*float(i-2)
		parent.add_child(n)

func _add_cable21(parent: Node3D,points: Array[Vector3]) -> void:
	var mat := _mat(Color("191d1f"),0.72,0.18)
	for i: int in range(points.size()-1):
		_add_rod20(parent,points[i],points[i+1],0.035,mat)

func _add_notice_cluster21(parent: Node3D,pos: Vector3) -> void:
	var board := _box(Vector3(2.6,1.2,0.06),_mat(Color("765b42"),0.97),pos,parent)
	for i: int in range(8):
		var col: Color = [Color("ded8c8"),Color("a9bfca"),Color("d8b1a5"),Color("c9c58e")][i%4]
		_box(Vector3(0.34,0.42,0.022),_mat(col,0.99),pos+Vector3(-0.9+float(i%4)*0.58,0.26-float(i/4)*0.52,0.04),parent)

func _add_fire_extinguisher21(parent: Node3D,pos: Vector3) -> void:
	var red := _mat(Color("a84235"),0.68,0.12)
	_cylinder(0.14,0.56,red,pos,parent)
	_box(Vector3(0.22,0.14,0.10),dark_mat,pos+Vector3(0,0.35,0),parent)
	_add_rod20(parent,pos+Vector3(0.08,0.31,0),pos+Vector3(0.28,0.55,0.02),0.022,dark_mat)

func _add_cleaning_cart21(parent: Node3D,pos: Vector3) -> void:
	var base := _mat(Color("4c5c5f"),0.90)
	_box(Vector3(0.85,0.55,0.48),base,pos+Vector3(0,0.35,0),parent)
	for x: float in [-0.30,0.30]:
		for z: float in [-0.15,0.15]:
			var wheel := _cylinder(0.08,0.05,dark_mat,pos+Vector3(x,0.08,z),parent)
			wheel.rotation_degrees.z = 90
	_add_rod20(parent,pos+Vector3(-0.32,0.55,0),pos+Vector3(-0.32,1.10,0),0.022,metal_mat)

func _add_pencilcup21(parent: Node3D,pos: Vector3) -> void:
	_cylinder(0.08,0.20,_mat(Color("6e7b76"),0.88),pos,parent)
	for i: int in range(4):
		var pencil := _cylinder(0.008,0.28,_mat([Color("d17c46"),Color("5f7890"),Color("d3b44c")][i%3],0.80),pos+Vector3(-0.03+0.02*float(i),0.18,0),parent)
		pencil.rotation_degrees.z = -5+4*i

func _add_notebook21(parent: Node3D,pos: Vector3,yaw: float) -> void:
	var n := _box(Vector3(0.35,0.025,0.48),_mat(Color("9f604b"),0.95),pos,parent)
	n.rotation.y = yaw
	_box(Vector3(0.30,0.012,0.42),_mat(Color("d9d3c6"),0.99),pos+Vector3(0,0.019,0),parent).rotation.y = yaw

func _add_lunchbox21(parent: Node3D,pos: Vector3) -> void:
	_box(Vector3(0.48,0.26,0.34),_mat(Color("8b4e45"),0.92),pos,parent)
	_add_rod20(parent,pos+Vector3(-0.13,0.16,0),pos+Vector3(0.13,0.16,0),0.018,dark_mat)

func _run_capture_sequence19() -> void:
	_set_school_power19(true)
	_school_powered = true
	if _cable_connected_visual != null:
		_cable_connected_visual.visible = true
	var dir := ProjectSettings.globalize_path("res://build/captures")
	DirAccess.make_dir_recursive_absolute(dir)
	camera.projection = Camera3D.PROJECTION_ORTHOGONAL
	camera.size = 13.6
	await _capture_view19("01_school_exterior.png",Vector3(18.5,13.2,21.0),Vector3(-2.0,1.35,6.6),false)
	camera.size = 24.0
	await _capture_view19("02_school_sport_rosvalla.png",Vector3(52,31,66),Vector3(15,0.8,36),false)
	camera.size = 9.0
	await _capture_view19("03_school_interior.png",Vector3(9.8,8.2,11.2),Vector3(4.2,1.0,-0.6),true)
	print("ROSVIK_VISUAL_CAPTURE_19_READY files=3")
	get_tree().quit()
