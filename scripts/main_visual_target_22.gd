extends "res://scripts/main_visual_target_21.gd"

# ROSVIK VISUAL TARGET 22 — SERIOUS HERO-SLICE TEST
# This is the decision build: same Godot foundation, but the school hero area
# now uses authored third-party models and a hand-composed classroom/corridor
# instead of relying on procedural boxes for the primary visual read.

func _ready() -> void:
	super._ready()
	print("ROSVIK_VISUAL_TARGET_22_READY")
	print("ROSVIK_AUTHORED_ASSET_PIPELINE_22_READY")
	print("ROSVIK_HERO_CLASSROOM_22_READY")
	print("ROSVIK_HERO_YARD_22_READY")

func _real_asset22(file_name: String,pos: Vector3,uniform_scale: float,yaw: float,centre: Vector3,parent: Node) -> Node3D:
	var path := ASSET_DIR+"realistic/"+file_name
	var res := load(path)
	if not (res is PackedScene):
		return Node3D.new()
	var inst := (res as PackedScene).instantiate() as Node3D
	inst.scale = Vector3.ONE*uniform_scale
	inst.rotation.y = yaw
	inst.position = pos-centre*uniform_scale
	parent.add_child(inst)
	return inst

# ---------------------------------------------------------------------------
# INTERIOR: replace the procedural desk rows and locker boxes with real models.
# ---------------------------------------------------------------------------
func _build_school_interior19(root: Node3D) -> void:
	var floor_mat := _textured_mat(Color("625f59"),0.91,"horizontal",128,0.018)
	var corridor_mat := _textured_mat(Color("49575a"),0.94,"noise",128,0.022)
	var wall_mat := _mat(Color("b5b5ad"),0.96)
	var notice_mat := _mat(Color("72563f"),0.96)

	# Readable diorama floors.
	_box(Vector3(30.8,0.055,3.20),corridor_mat,Vector3(0,0.16,3.72),root)
	_box(Vector3(14.8,0.055,7.10),floor_mat,Vector3(7.8,0.16,-1.55),root)
	_box(Vector3(11.8,0.055,7.10),floor_mat,Vector3(-8.4,0.16,-1.55),root)

	# Low cutaway partitions. The camera sees the room instead of staring at walls.
	_solid_box(Vector3(11.6,0.86,0.18),wall_mat,Vector3(-9.9,0.58,1.92),root)
	_solid_box(Vector3(7.0,0.86,0.18),wall_mat,Vector3(1.5,0.58,1.92),root)
	_solid_box(Vector3(8.2,0.86,0.18),wall_mat,Vector3(12.7,0.58,1.92),root)
	_solid_box(Vector3(0.18,0.86,7.1),wall_mat,Vector3(0.4,0.58,-1.55),root)

	# Real locker banks along the corridor.
	var locker_centre := Vector3(22.0150,-115.0920,79.8665)
	for x: float in [-13.4,-11.35,-9.30,-7.25]:
		_real_asset22("lockers.glb",Vector3(x,0.16,5.18),0.00904,PI,locker_centre,root)

	# Bench and floor clutter create a lived school entrance.
	_real_asset22("city_bench.glb",Vector3(-10.2,0.16,3.05),0.01186,PI/2.0,Vector3(0,0,-1.1806),root)
	for i: int in range(5):
		var boot := _capsule19(0.11,0.26,_mat(Color("293234").lightened(float(i%3)*0.035),0.98),Vector3(-12.5+float(i)*0.55,0.28,3.65+float(i%2)*0.18),root)
		boot.rotation_degrees.x = 90
	for i: int in range(4):
		_add_backpack19(root,Vector3(-9.0+float(i)*0.62,0.30,4.70),[Color("52685f"),Color("6e4f49"),Color("455d72"),Color("76634c")][i])

	# Notice board.
	_box(Vector3(3.2,1.10,0.07),notice_mat,Vector3(-3.5,1.55,2.02),root)
	for i: int in range(10):
		var paper_color: Color = [Color("ddd8c8"),Color("aec5d0"),Color("d8b3a5"),Color("cbc994")][i%4]
		_box(Vector3(0.34,0.42,0.022),_mat(paper_color,0.99),Vector3(-4.62+float(i%5)*0.56,1.35+float(i/5)*0.48,2.08),root)

	# Real school desks. Six carefully spaced stations create scale and depth.
	var desk_centre := Vector3(-25.9953,0.0512,-26.8323)
	var desk_yaw_fix := deg_to_rad(136.9088)
	for row: int in range(2):
		for col: int in range(3):
			var p := Vector3(4.0+float(col)*3.25,0.16,-0.15-float(row)*2.45)
			_real_asset22("school_desk.glb",p,0.007,desk_yaw_fix,desk_centre,root)
			_collision_box19(root,Vector3(1.30,0.85,0.95),p+Vector3(0,0.42,0))
			_world_prop_count += 1

	# Teacher corner retains authored props from existing asset library.
	_asset19("furniture/desk.glb",Vector3(13.4,0.16,-3.8),Vector3.ONE,PI,root)
	_asset19("furniture/chair.glb",Vector3(13.3,0.16,-2.9),Vector3.ONE,0.0,root)
	_asset19("furniture/bookshelf.glb",Vector3(1.45,0.16,-4.55),Vector3.ONE,PI/2.0,root)
	_asset19("furniture/monitor.glb",Vector3(13.25,0.94,-3.65),Vector3.ONE*0.82,PI,root)
	_asset19("furniture/lamp.glb",Vector3(12.65,0.92,-3.55),Vector3.ONE*0.80,0.0,root)
	_add_mug19(root,Vector3(12.9,1.00,-3.30),Color("9a5e46"))
	_add_thermos19(root,Vector3(13.55,1.04,-3.40))

	# Cleaning cart is now a real model, placed where a caretaker would actually leave it.
	_real_asset22("cleaning_cart.glb",Vector3(-13.6,0.16,4.15),0.16,PI/2.0,Vector3(0,0.000065,-0.831794),root)

	# Reading corner.
	_box(Vector3(4.8,0.028,3.0),_mat(Color("765f55"),0.96),Vector3(-7.2,0.19,-2.35),root)
	_asset19("furniture/bookshelf.glb",Vector3(-12.5,0.16,-4.50),Vector3.ONE*0.96,PI/2.0,root)
	_asset19("furniture/bookshelf.glb",Vector3(-10.4,0.16,-4.50),Vector3.ONE*0.96,PI/2.0,root)
	_real_asset22("city_bench.glb",Vector3(-7.0,0.16,-2.30),0.01186,0.0,Vector3(0,0,-1.1806),root)

	# Layered desk clutter.
	for i: int in range(10):
		var p := Vector3(3.6+float(i%5)*1.75,0.96,-0.35-float(i/5)*2.45)
		_add_notebook21(root,p,(-0.22+float(i)*0.045))
		if i%2==0:
			_add_pencilcup21(root,p+Vector3(0.28,0.05,0.12))
	_add_lunchbox21(root,Vector3(7.3,0.29,-4.30))

	# Local warm pools; blackout leaves most fixtures dead.
	for p: Vector3 in [Vector3(-10.5,2.35,3.5),Vector3(5.3,2.45,-1.4),Vector3(11.2,2.45,-1.4)]:
		var light := OmniLight3D.new()
		light.position = p
		light.light_color = Color("ffc37b")
		light.light_energy = 0.0
		light.omni_range = 4.5
		light.shadow_enabled = true
		root.add_child(light)
		_school_lights.append(light)

	_add_searchable19(root,Vector3(-14.5,0.0,5.0),"janitor","VAKTMÄSTARSKÅP",["silvertejp","säkringar","AA-batterier"])
	_add_searchable19(root,Vector3(13.6,0.0,-4.6),"staff","LÄRARBORD",["nyckelknippa","ficklampa","tändare"])
	_add_searchable19(root,Vector3(8.6,0.0,-1.5),"bag","ÖVERGIVEN RYGGSÄCK",["choklad","vantar","powerbank"])

# ---------------------------------------------------------------------------
# EXTERIOR YARD: use authored benches/bins and compose fewer, better stories.
# ---------------------------------------------------------------------------
func _upgrade_schoolyard21() -> void:
	if _school_root == null:
		return
	var yard := Node3D.new()
	yard.name = "HeroSchoolyard22"
	_school_root.add_child(yard)

	var bin_centre := Vector3(0.0,0.0028,0.0)
	for p: Vector3 in [Vector3(-8.0,0.16,7.75),Vector3(12.7,0.16,8.15),Vector3(14.0,0.16,8.15)]:
		_real_asset22("garbage_bin.glb",p,1.0,0.0,bin_centre,yard)

	var bench_centre := Vector3(0.0,0.0,-1.1806)
	_real_asset22("city_bench.glb",Vector3(-12.1,0.16,10.3),0.01186,PI/2.0,bench_centre,yard)
	_real_asset22("city_bench.glb",Vector3(9.4,0.16,10.5),0.01186,PI/2.0,bench_centre,yard)

	# Bikes, kick-sled, shovel, grit, wood and garbage are concentrated along real use zones.
	_add_bicycle20(yard,Vector3(7.25,0.0,8.95),0.08,Color("365a68"))
	_add_bicycle20(yard,Vector3(8.05,0.0,9.05),-0.04,Color("8b4b41"))
	_add_bicycle20(yard,Vector3(8.95,0.0,9.12),0.07,Color("4f5c4d"))
	_add_kicksled19b(_school_root.to_global(Vector3(-11.7,0,9.25)))
	_add_snow_shovel20(yard,Vector3(-7.3,0.0,7.55),0.16)
	_add_woodpile21(yard,Vector3(-13.7,0.0,7.45))
	for i: int in range(4):
		_add_garbage_bag21(yard,Vector3(12.0+float(i)*0.30,0.22,7.15+float(i%2)*0.24),0.17+0.015*float(i))

	# Human traces.
	_add_thermos19(yard,Vector3(9.28,0.88,10.42))
	_add_mug19(yard,Vector3(9.64,0.82,10.40),Color("b26f4e"))
	_add_mitten20(yard,Vector3(-4.25,0.10,9.15),Color("a24e42"),0.35)
	_add_mitten20(yard,Vector3(-3.82,0.10,9.38),Color("a24e42"),-0.15)
	_add_ball20(yard,Vector3(4.6,0.19,14.8),Color("b46b3f"))
	_add_footpath20(yard,Vector3(-5.0,0.0,7.2),Vector3(-5.3,0.0,14.0),17)

	# Narrow, broken snowbanks hug objects and edges.
	for x: float in [-15.0,-11.0,-7.5,2.5,6.5,10.5,14.5]:
		_snow_mound21(yard,Vector3(x,0.0,7.18),Vector3(1.55,0.14,0.30),0.06*sin(x))

func _upgrade_interior21() -> void:
	# The authored interior is already built in _build_school_interior19.
	# Add only story props, not another layer of furniture.
	if _school_root == null:
		return
	var root := Node3D.new()
	root.name = "InteriorStory22"
	_school_root.add_child(root)
	_add_fire_extinguisher21(root,Vector3(-0.2,0.72,2.08))
	_add_notice_cluster21(root,Vector3(-1.9,1.62,2.08))
	# One abandoned backpack and a dropped notebook sell sudden departure.
	_add_backpack19(root,Vector3(6.9,0.30,-4.35),Color("526178"))
	_add_notebook21(root,Vector3(7.65,0.19,-4.05),-0.42)

func _run_capture_sequence19() -> void:
	_set_school_power19(true)
	_school_powered = true
	if _cable_connected_visual != null:
		_cable_connected_visual.visible = true
	var dir := ProjectSettings.globalize_path("res://build/captures")
	DirAccess.make_dir_recursive_absolute(dir)
	camera.projection = Camera3D.PROJECTION_ORTHOGONAL
	camera.size = 12.8
	await _capture_view19("01_school_exterior.png",Vector3(17.5,12.4,19.5),Vector3(-2.4,1.25,6.7),false)
	camera.size = 9.0
	await _capture_view19("02_school_interior.png",Vector3(9.2,7.6,10.7),Vector3(4.3,1.0,-0.6),true)
	camera.size = 18.5
	await _capture_view19("03_school_context.png",Vector3(31,20,38),Vector3(-1.5,0.9,11.0),false)
	print("ROSVIK_VISUAL_CAPTURE_19_READY files=3")
	get_tree().quit()
