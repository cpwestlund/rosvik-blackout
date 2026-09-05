extends "res://scripts/main_visual_identity_world_16.gd"

# ROSVIK AUTHENTIC SLICE 17
# Reference-led pass around Rosviks skola. Public Pitea material confirms the
# school campus is a real multi-building place and that sporthall/ice facilities
# sit nearby; this pass therefore stops treating the school as a generic level
# shell and concentrates detail, lived-in evidence and winter atmosphere here.
# The style target is the agreed cozy isometric reference: cold quiet exterior,
# protected warm pockets, soft silhouettes and dense human-scale storytelling.

var _slice17_root: Node3D
var _slice17_school: Node3D
var _slice17_power_lights: Array[OmniLight3D] = []
var _slice17_warm_materials: Array[StandardMaterial3D] = []
var _slice17_detail_count: int = 0
var _slice17_loot_nodes: int = 0

func _ready() -> void:
	super._ready()
	_slice17_root = Node3D.new()
	_slice17_root.name = "AuthenticRosvikSlice17"
	add_child(_slice17_root)
	_slice17_school = get_node_or_null("RosviksSkola") as Node3D
	if _slice17_school != null:
		_rework_school_facade17()
		_build_lived_school17()
		_build_schoolyard_story17()
	_build_winter_depth17()
	_build_civic_transition17()
	_enrich_loot17()
	_hard_cleanup17()
	_apply_power_state17()
	print("ROSVIK_AUTHENTIC_SLICE_17_READY")
	print("ROSVIK_REFERENCE_LED_SCHOOL_17_READY")
	print("ROSVIK_LIVED_INTERIORS_17_READY details=",_slice17_detail_count)
	print("ROSVIK_DENSE_LOOT_17_READY nodes=",_slice17_loot_nodes)
	print("ROSVIK_COLD_WARM_CONTRAST_17_READY")
	print("ROSVIK_WORLD_LOCKED_CONTROLS_17_READY")

func _process(delta: float) -> void:
	super._process(delta)
	_apply_power_state17()

# -------------------------------------------------------------------------
# SCHOOL EXTERIOR: RHYTHM, DEPTH, ENTRANCE, WEATHERING
# -------------------------------------------------------------------------
func _rework_school_facade17() -> void:
	var school := _slice17_school
	var pale := _textured_mat(Color("a9aca5"),0.96,0.0,"horizontal",96,0.018)
	var aged := _textured_mat(Color("858b87"),0.98,0.0,"noise",72,0.025)
	var dark := _mat(Color("30383b"),0.86,0.10)
	var timber := _textured_mat(Color("765f4a"),0.96,0.0,"vertical",72,0.020)
	var glass_dark := _mat(Color("314750"),0.26,0.05)

	# A low horizontal 60/70s facade rhythm, laid over the structural shell rather
	# than inventing a completely different building footprint.
	for x: float in [-10.45,-8.60,1.35,3.45,5.55,7.65,9.75,11.55]:
		_box(Vector3(0.10,3.10,0.13),aged,Vector3(x,1.72,6.56),school)
		_slice17_detail_count += 1
	for x: float in [-9.55,-7.70,2.35,4.45,6.55,8.65,10.65]:
		_box(Vector3(1.28,1.22,0.09),glass_dark,Vector3(x,2.05,6.62),school)
		_box(Vector3(1.46,0.08,0.14),dark,Vector3(x,1.41,6.65),school)
		_box(Vector3(1.46,0.08,0.14),dark,Vector3(x,2.69,6.65),school)
		_slice17_detail_count += 3

	# Make the real entrance read as a destination: deep canopy, timber soffit,
	# side glazing and a small warm pool when reserve power is restored.
	_solid_box(Vector3(4.75,0.20,3.00),dark,Vector3(-4.45,3.02,7.68),school)
	_box(Vector3(4.35,0.07,2.65),timber,Vector3(-4.45,2.88,7.72),school)
	for x: float in [-6.28,-2.62]:
		_solid_box(Vector3(0.16,2.70,0.16),dark,Vector3(x,1.35,8.68),school)
	_box(Vector3(1.05,2.15,0.08),glass_dark,Vector3(-6.10,1.43,6.64),school)
	_box(Vector3(1.05,2.15,0.08),glass_dark,Vector3(-2.80,1.43,6.64),school)
	var entry_light := _make_power_light17(Vector3(-4.45,2.35,8.35),Color("f5c98f"),1.6,7.0)
	school.add_child(entry_light)

	# Roof edge, gutter and downpipes give the broad building a readable silhouette.
	var gutter := _mat(Color("414b4e"),0.80,0.16)
	var roof_lip := _textured_mat(Color("3b4244"),0.90,0.05,"noise",64,0.018)
	_box(Vector3(25.75,0.24,0.32),roof_lip,Vector3(0.5,4.26,6.58),school)
	var horizontal := _cylinder(0.055,25.30,gutter,Vector3(0.5,3.96,6.77),school)
	horizontal.rotation.z = PI/2.0
	for x: float in [-11.55,12.45]:
		_cylinder(0.050,3.45,gutter,Vector3(x,1.90,6.78),school)
		_slice17_detail_count += 1

	# Uneven snow lip, deliberately soft rather than a perfect white rectangle.
	for x: float in [-9.8,-5.7,-1.6,2.6,6.8,10.5]:
		var mound_scale := Vector3(randf_range(1.45,2.25),randf_range(0.16,0.26),0.45)
		_soft_snow_mound17(school,Vector3(x,4.35,6.40),mound_scale,0.0)

# -------------------------------------------------------------------------
# LIVED-IN INTERIOR: THE SCHOOL WAS A PLACE, NOT A LOOT CONTAINER
# -------------------------------------------------------------------------
func _build_lived_school17() -> void:
	var school := _slice17_school
	var coat_colors: Array[Color] = [Color("9d4b3c"),Color("435c6a"),Color("69784e"),Color("b27b42"),Color("6e536c")]
	var hook_mat := _mat(Color("4c5658"),0.70,0.25)
	var paper := _mat(Color("d9d1ba"),0.98)
	var pencil := _mat(Color("b16c43"),0.90)
	var warm_wood := _textured_mat(Color("896849"),0.95,0.0,"horizontal",64,0.018)

	# Corridor cloakroom: hooks, coats, backpacks and boots. This is the first
	# thing a player should read through a cutaway - children used this place.
	for i: int in range(9):
		var x := -10.5 + float(i)*1.10
		_cylinder(0.025,0.24,hook_mat,Vector3(x,1.86,2.36),school).rotation.x = PI/2.0
		if i != 3 and i != 7:
			_add_hanging_coat17(school,Vector3(x,1.28,2.20),coat_colors[i % coat_colors.size()],i % 3)
		if i % 2 == 0:
			_add_backpack17(school,Vector3(x+0.18,0.48,2.18),coat_colors[(i+2) % coat_colors.size()])
		_slice17_detail_count += 2
	for x: float in [-9.65,-6.9,-4.7]:
		_add_boot_pair17(school,Vector3(x,0.12,2.12))

	# Classroom 1: half-finished work left on tables and drawings on the wall.
	for i: int in range(6):
		var px := -10.0 + float(i % 3)*1.65
		var pz := -4.45 + float(i / 3)*1.55
		_box(Vector3(0.42,0.018,0.30),paper,Vector3(px,0.86,pz),school)
		var pen := _cylinder(0.012,0.26,pencil,Vector3(px+0.18,0.89,pz+0.08),school)
		pen.rotation.z = PI/2.0
		_slice17_detail_count += 2
	for i: int in range(7):
		var art_color := coat_colors[i % coat_colors.size()].lightened(0.18)
		_box(Vector3(0.52,0.40,0.025),_mat(art_color,0.94),Vector3(-11.84,1.30+float(i%2)*0.50,-5.25+float(i/2)*0.72),school)
		_slice17_detail_count += 1

	# Expedition/staff area: mug, folders, paper piles, radio and old desk lamp.
	_add_mug17(school,Vector3(0.55,0.87,5.00),Color("d9d1bf"))
	for j: int in range(3):
		_box(Vector3(0.48,0.025,0.34),paper,Vector3(1.05,0.87+float(j)*0.028,4.86),school)
	_add_desk_lamp17(school,Vector3(1.34,0.87,5.18))
	_add_small_radio17(school,Vector3(10.30,0.88,4.92))
	_add_mug17(school,Vector3(10.75,0.87,5.12),Color("6f7f76"))
	_box(Vector3(1.10,0.08,0.65),warm_wood,Vector3(8.82,0.12,3.34),school)
	_slice17_detail_count += 8

	# Foyer: a forgotten bench bag and a child's glove under it.
	_add_backpack17(school,Vector3(-5.25,0.44,4.80),Color("a64e3e"))
	_add_glove17(school,Vector3(-5.95,0.14,4.55),Color("354d63"))

	# A few ceiling pools only activate with the restored school feed. Keeping most
	# of the building dark is more atmospheric than lighting every fixture.
	for p: Vector3 in [Vector3(-4.5,2.78,4.55),Vector3(-7.7,2.82,-2.7),Vector3(0.5,2.82,1.5)]:
		var light := _make_power_light17(p,Color("ffdca7"),1.25,5.5)
		school.add_child(light)

func _add_hanging_coat17(parent: Node3D,pos: Vector3,color: Color,variant: int) -> void:
	var root := Node3D.new()
	root.position = pos
	parent.add_child(root)
	var cloth := _mat(color,0.98)
	var torso := _capsule_local(0.22,0.72,cloth,Vector3.ZERO,root)
	torso.scale = Vector3(1.0,1.0,0.56)
	for x: float in [-0.25,0.25]:
		var arm := _capsule_local(0.075,0.58,cloth,Vector3(x,-0.02,0.0),root)
		arm.rotation.z = (0.30 if x < 0.0 else -0.30) + float(variant)*0.03
	_slice17_detail_count += 3

func _add_backpack17(parent: Node3D,pos: Vector3,color: Color) -> Node3D:
	var root := Node3D.new()
	root.position = pos
	parent.add_child(root)
	var body := _capsule_local(0.28,0.55,_mat(color,0.96),Vector3.ZERO,root)
	body.scale = Vector3(0.85,1.0,0.50)
	var strap_mat := _mat(Color("30383a"),0.90)
	for x: float in [-0.18,0.18]:
		var strap := _cylinder(0.018,0.44,strap_mat,Vector3(x,0.02,-0.15),root)
		strap.rotation.z = 0.10 if x < 0.0 else -0.10
	_slice17_detail_count += 3
	return root

func _add_boot_pair17(parent: Node3D,pos: Vector3) -> void:
	var boot_mat := _mat(Color("272e31"),0.99)
	for x: float in [-0.15,0.15]:
		var boot := _capsule_local(0.105,0.32,boot_mat,pos+Vector3(x,0.10,0.0),parent)
		boot.scale = Vector3(0.88,1.0,1.24)
		boot.rotation.x = PI/2.0
	_slice17_detail_count += 2

func _add_mug17(parent: Node3D,pos: Vector3,color: Color) -> void:
	var mug_mat := _mat(color,0.72)
	_cylinder(0.085,0.15,mug_mat,pos,parent)
	var handle := _cylinder(0.018,0.18,mug_mat,pos+Vector3(0.10,0.03,0.0),parent)
	handle.rotation.x = PI/2.0

func _add_desk_lamp17(parent: Node3D,pos: Vector3) -> void:
	var metal := _mat(Color("424d50"),0.62,0.28)
	_cylinder(0.12,0.035,metal,pos,parent)
	var stem := _cylinder(0.022,0.42,metal,pos+Vector3(0.0,0.20,0.0),parent)
	stem.rotation.z = -0.30
	var shade := _capsule_local(0.12,0.18,_mat(Color("aa8b55"),0.78),pos+Vector3(0.10,0.39,0.0),parent)
	shade.scale = Vector3(1.2,0.55,1.2)

func _add_small_radio17(parent: Node3D,pos: Vector3) -> void:
	var radio := _mat(Color("353d3f"),0.78,0.10)
	var speaker := _mat(Color("161b1d"),0.98)
	_box(Vector3(0.42,0.24,0.18),radio,pos,parent)
	_cylinder(0.07,0.025,speaker,pos+Vector3(-0.10,0.0,0.105),parent).rotation.x = PI/2.0
	_cylinder(0.025,0.035,_mat(Color("aa9a79"),0.58,0.20),pos+Vector3(0.12,0.05,0.105),parent).rotation.x = PI/2.0

func _add_glove17(parent: Node3D,pos: Vector3,color: Color) -> void:
	var cloth := _mat(color,0.98)
	var palm := _capsule_local(0.08,0.22,cloth,pos,parent)
	palm.rotation.z = PI/2.0
	for i: int in range(3):
		var finger := _capsule_local(0.018,0.11,cloth,pos+Vector3(0.08,float(i-1)*0.035,0.0),parent)
		finger.rotation.z = PI/2.0

# -------------------------------------------------------------------------
# SCHOOLYARD: EVIDENCE OF ORDINARY LIFE INTERRUPTED BY BLACKOUT
# -------------------------------------------------------------------------
func _build_schoolyard_story17() -> void:
	var school := _slice17_school
	var metal := _mat(Color("4a5558"),0.66,0.26)
	var red := _mat(Color("9a493d"),0.88)
	var wood := _textured_mat(Color("786149"),0.97,0.0,"horizontal",64,0.020)

	# Bike rack just inside the yard, not in the carriageway.
	var rack_root := Node3D.new()
	rack_root.position = Vector3(7.6,0.0,17.1)
	school.add_child(rack_root)
	for i: int in range(5):
		var x := -1.4+float(i)*0.70
		var hoop := _cylinder(0.035,1.02,metal,Vector3(x,0.48,0.0),rack_root)
		hoop.rotation.x = PI/2.0
		_slice17_detail_count += 1
	# One abandoned bicycle silhouette.
	_add_bicycle17(rack_root,Vector3(-0.72,0.0,0.30),red)

	# Snow shovel and grit bin against the fence/yard, deliberately grounded.
	_add_snow_shovel17(school,Vector3(13.55,0.0,17.7),-0.18)
	var grit := Node3D.new()
	grit.position = Vector3(12.4,0.0,18.2)
	school.add_child(grit)
	var grit_body := _capsule_local(0.34,0.58,_mat(Color("557069"),0.92),Vector3(0.0,0.30,0.0),grit)
	grit_body.scale = Vector3(1.15,0.82,0.95)
	_box(Vector3(0.70,0.08,0.58),_mat(Color("354341"),0.88),Vector3(0.0,0.61,0.0),grit)
	_slice17_detail_count += 3

	# Footsteps from the crossing toward the entrance - small paired impressions,
	# not text arrows. They visually guide the player without a floating sign.
	var footprint_mat := _mat(Color("7f898b"),0.99)
	for i: int in range(13):
		var t := float(i)/12.0
		var x := lerpf(-4.3,-4.48,t) + (0.14 if i % 2 == 0 else -0.14)
		var z := lerpf(13.0,7.25,t)
		var foot := _cylinder(0.075,0.018,footprint_mat,Vector3(x,0.115,z),school)
		foot.scale = Vector3(0.72,1.0,1.45)
		_slice17_detail_count += 1

	# A low kick-sled and a forgotten mitten near the bench.
	_add_kicksled17(school,Vector3(-11.0,0.0,19.45),wood)
	_add_glove17(school,Vector3(-10.25,0.11,18.55),Color("8f5147"))

func _add_bicycle17(parent: Node3D,pos: Vector3,frame_mat: StandardMaterial3D) -> void:
	var root := Node3D.new()
	root.position = pos
	root.rotation.y = 0.15
	parent.add_child(root)
	var tire := _mat(Color("1e2426"),0.99)
	for x: float in [-0.48,0.48]:
		var wheel := _cylinder(0.31,0.035,tire,Vector3(x,0.33,0.0),root)
		wheel.rotation.z = PI/2.0
	var bar1 := _cylinder(0.025,0.78,frame_mat,Vector3(0.0,0.50,0.0),root)
	bar1.rotation.z = PI/2.0
	var bar2 := _cylinder(0.025,0.64,frame_mat,Vector3(-0.10,0.52,0.0),root)
	bar2.rotation.z = 0.72
	var bar3 := _cylinder(0.025,0.70,frame_mat,Vector3(0.13,0.52,0.0),root)
	bar3.rotation.z = -0.72
	_cylinder(0.022,0.52,frame_mat,Vector3(0.34,0.68,0.0),root).rotation.z = -0.28
	_slice17_detail_count += 7

func _add_snow_shovel17(parent: Node3D,pos: Vector3,yaw: float) -> void:
	var root := Node3D.new()
	root.position = pos
	root.rotation.y = yaw
	parent.add_child(root)
	var handle := _cylinder(0.028,1.45,_mat(Color("8c6d4c"),0.96),Vector3(0.0,0.78,0.0),root)
	handle.rotation.z = 0.10
	var blade := _box(Vector3(0.52,0.08,0.42),_mat(Color("b36b42"),0.84),Vector3(-0.07,0.12,0.0),root)
	blade.rotation.z = 0.10
	_slice17_detail_count += 2

func _add_kicksled17(parent: Node3D,pos: Vector3,wood_mat: StandardMaterial3D) -> void:
	var root := Node3D.new()
	root.position = pos
	root.rotation.y = 0.35
	parent.add_child(root)
	var metal := _mat(Color("4b5557"),0.72,0.22)
	for x: float in [-0.24,0.24]:
		var runner := _cylinder(0.022,1.22,metal,Vector3(x,0.08,0.0),root)
		runner.rotation.x = PI/2.0
		_cylinder(0.025,0.84,metal,Vector3(x,0.47,-0.45),root)
	_box(Vector3(0.58,0.08,0.48),wood_mat,Vector3(0.0,0.36,0.12),root)
	_slice17_detail_count += 5

# -------------------------------------------------------------------------
# WINTER DEPTH: TREES, DRIFTS AND QUIET EDGES
# -------------------------------------------------------------------------
func _build_winter_depth17() -> void:
	for data: Dictionary in [
		{"p":Vector3(-22.0,0.0,-2.5),"s":1.15},
		{"p":Vector3(20.8,0.0,3.5),"s":1.05},
		{"p":Vector3(-18.5,0.0,29.0),"s":0.90},
		{"p":Vector3(17.8,0.0,28.2),"s":1.00},
		{"p":Vector3(33.0,0.0,47.8),"s":1.20}
	]:
		_add_snowy_pine17(data["p"],float(data["s"]))

	# Rounded plough ridges outside the road edge create framing and remove the
	# sterile flat-map feel without blocking movement.
	for p: Vector3 in [Vector3(-28.0,0.0,17.2),Vector3(21.8,0.0,17.4),Vector3(31.8,0.0,31.0),Vector3(70.0,0.0,31.0)]:
		_soft_snow_mound17(_slice17_root,p,Vector3(randf_range(2.4,3.8),randf_range(0.32,0.48),randf_range(0.75,1.10)),randf_range(-0.14,0.14))

func _add_snowy_pine17(pos: Vector3,scale_value: float) -> void:
	var root := Node3D.new()
	root.position = pos
	root.scale = Vector3.ONE*scale_value
	_slice17_root.add_child(root)
	var trunk := _mat(Color("4c3a2e"),0.98)
	var needles := _mat(Color("263f39"),0.99)
	var snow := _mat(Color("cbd5d8"),0.99)
	_cylinder(0.12,3.15,trunk,Vector3(0.0,1.58,0.0),root)
	for layer: int in range(4):
		var y := 1.0+float(layer)*0.68
		var radius := 1.15-float(layer)*0.20
		var crown := MeshInstance3D.new()
		var sphere := SphereMesh.new()
		sphere.radius = radius
		sphere.height = radius*1.35
		sphere.radial_segments = 16
		sphere.rings = 8
		crown.mesh = sphere
		crown.material_override = needles
		crown.position = Vector3(0.0,y,0.0)
		crown.scale = Vector3(1.0,0.58,1.0)
		root.add_child(crown)
		var cap := MeshInstance3D.new()
		var snow_sphere := SphereMesh.new()
		snow_sphere.radius = radius*0.82
		snow_sphere.height = radius*0.40
		snow_sphere.radial_segments = 16
		snow_sphere.rings = 6
		cap.mesh = snow_sphere
		cap.material_override = snow
		cap.position = Vector3(0.0,y+radius*0.28,0.0)
		cap.scale = Vector3(1.0,0.34,1.0)
		root.add_child(cap)
	_slice17_detail_count += 9

func _soft_snow_mound17(parent: Node3D,pos: Vector3,scale_value: Vector3,yaw: float) -> void:
	var mesh_instance := MeshInstance3D.new()
	var sphere := SphereMesh.new()
	sphere.radius = 1.0
	sphere.height = 2.0
	sphere.radial_segments = 24
	sphere.rings = 12
	mesh_instance.mesh = sphere
	mesh_instance.material_override = packed_snow_mat
	mesh_instance.position = pos+Vector3(0.0,-0.48+scale_value.y*0.50,0.0)
	mesh_instance.scale = scale_value
	mesh_instance.rotation.y = yaw
	mesh_instance.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_ON
	parent.add_child(mesh_instance)
	_slice17_detail_count += 1

# -------------------------------------------------------------------------
# BETWEEN SCHOOL AND SPORT: VISUAL CONTINUITY, NO FLOATING WAYFINDING
# -------------------------------------------------------------------------
func _build_civic_transition17() -> void:
	var metal := _mat(Color("4b5659"),0.68,0.26)
	var bench_wood := _textured_mat(Color("725d49"),0.97,0.0,"horizontal",64,0.018)
	# Small waiting/rest spot beside the pedestrian flow, away from the roadway.
	var root := Node3D.new()
	root.position = Vector3(27.8,0.0,18.7)
	_slice17_root.add_child(root)
	for z: float in [-0.54,0.54]:
		_solid_box(Vector3(2.35,0.12,0.26),bench_wood,Vector3(0.0,0.52,z),root)
	for x: float in [-0.92,0.92]:
		_cylinder(0.04,0.52,metal,Vector3(x,0.28,0.0),root)
	_soft_snow_mound17(root,Vector3(1.45,0.0,0.45),Vector3(0.70,0.14,0.48),0.0)
	_add_glove17(root,Vector3(-0.55,0.62,-0.48),Color("526b78"))
	_slice17_detail_count += 6

# -------------------------------------------------------------------------
# DENSE, PLACE-BASED LOOT: SEARCH THE STORY, NOT RANDOM CRATES
# -------------------------------------------------------------------------
func _enrich_loot17() -> void:
	_item_weights15["radio"] = 0.45
	_item_weights15["pannlampa"] = 0.16
	_item_weights15["forlangningskabel"] = 1.05
	_item_weights15["kaffe"] = 0.50

	if _slice17_school != null:
		var forgotten_bag := _slice17_school.get_node_or_null("ForgottenSchoolBag17") as Node3D
		if forgotten_bag == null:
			forgotten_bag = Node3D.new()
			forgotten_bag.name = "ForgottenSchoolBag17"
			forgotten_bag.position = Vector3(-5.25,0.0,4.80)
			_slice17_school.add_child(forgotten_bag)
		_register_searchable11(forgotten_bag,"GLÖMD RYGGSÄCK",["energibar","aa","vantar"],1.6)
		_slice17_loot_nodes += 1

		_add_search_cabinet11(_slice17_school,Vector3(11.55,0.0,4.86),PI,"VAKTMÄSTARENS SKÅP",["verktyg","silvertejp","forlangningskabel","pannlampa"])
		_slice17_loot_nodes += 1
		_add_search_cabinet11(_slice17_school,Vector3(2.20,0.0,5.68),PI,"PERSONALSKÅP",["kaffe","termos","radio","tandstickor"])
		_slice17_loot_nodes += 1

	# One believable service cache at sporthall edge; no magical chest in the road.
	if _gameplay_root != null:
		_add_search_cabinet11(_gameplay_root,Vector3(68.0,0.0,29.15),PI/2.0,"ELFÖRRÅD",["aa","silvertejp","forlangningskabel","verktyg"])
		_slice17_loot_nodes += 1

func _loot_name11(key: String) -> String:
	match key:
		"radio": return "batteriradio"
		"pannlampa": return "pannlampa"
		"forlangningskabel": return "förlängningskabel"
		"kaffe": return "kaffepaket"
	return super._loot_name11(key)

# -------------------------------------------------------------------------
# HARD CLEANUP: IF IT LOOKS LIKE EDITOR GUIDANCE, IT DOES NOT BELONG IN WORLD
# -------------------------------------------------------------------------
func _hard_cleanup17() -> void:
	var labels: Array[Label3D] = []
	_collect_label3d16(self,labels)
	for label: Label3D in labels:
		var t := label.text.strip_edges().to_upper()
		if t in ["BOSTÄDER","SPORTHALL ->","SPORTHALL →","HALA ->","HALA →"]:
			label.visible = false
		# Old giant building-name labels are replaced by facade-mounted lettering.
		if t == "NORRBOTTEN STÅL ARENA":
			label.visible = false

# -------------------------------------------------------------------------
# POWER: WARMTH IS A REWARD FOR RESTORING ELECTRICITY
# -------------------------------------------------------------------------
func _make_power_light17(pos: Vector3,color: Color,energy: float,range_value: float) -> OmniLight3D:
	var light := OmniLight3D.new()
	light.position = pos
	light.light_color = color
	light.light_energy = energy
	light.omni_range = range_value
	light.shadow_enabled = true
	light.visible = false
	_slice17_power_lights.append(light)
	return light

func _apply_power_state17() -> void:
	var powered := _school_powered
	for light: OmniLight3D in _slice17_power_lights:
		if is_instance_valid(light):
			light.visible = powered
