extends "res://scripts/main_visual_foundation_01.gd"

var _school_cutaway: Array[Node3D] = []
var _inside_school: bool = false
var _sporthall_root: Node3D
var _icehall_root: Node3D

func _ready() -> void:
	super._ready()
	_add_rosvalla_context()
	print("ROSVIK_ACCURACY_02_READY")
	print("ROSVIK_MULTI_CLASSROOM_READY")
	print("ROSVIK_SNOW_PASSABLE_READY")
	print("ROSVIK_SPORTHALL_READY")
	print("ROSVIK_ICEHALL_SEPARATED_READY")
	print("ROSVIK_DYNAMIC_CUTAWAY_READY")

func _process(delta: float) -> void:
	super._process(delta)
	_update_school_cutaway()

# Snow banks are visual terrain dressing in this milestone. The old implementation
# used solid collision boxes, which made harmless drifts behave like concrete walls.
func _add_snowbank(pos: Vector3) -> void:
	var size: Vector3 = Vector3(randf_range(4.0,7.5),randf_range(0.30,0.60),randf_range(1.4,2.3))
	var bank: MeshInstance3D = _box(size,packed_snow_mat,pos+Vector3(0.0,size.y*0.5,0.0),self)
	bank.rotation.y = randf_range(-0.18,0.18)
	var dirty: StandardMaterial3D = _textured_mat(Color("929b9f"),0.99,0.0,"snow",64,0.045)
	var edge: MeshInstance3D = _box(Vector3(size.x*0.72,0.04,size.z*0.45),dirty,pos+Vector3(0.0,0.04,size.z*0.22),self)
	edge.rotation.y = bank.rotation.y

func _build_school() -> void:
	var root: Node3D = Node3D.new()
	root.name = "RosviksSkola"
	add_child(root)

	var interior_wall: StandardMaterial3D = _textured_mat(Color("c8c9c3"),0.95,0.0,"horizontal",96,0.012)
	var interior_floor: StandardMaterial3D = _textured_mat(Color("6b7375"),0.93,0.0,"noise",96,0.018)
	var classroom_floor: StandardMaterial3D = _textured_mat(Color("8b8275"),0.93,0.0,"horizontal",96,0.012)
	var locker_mat: StandardMaterial3D = _mat(Color("65777e"),0.86,0.08)
	var desk_mat: StandardMaterial3D = _mat(Color("997554"),0.92)
	var chair_mat: StandardMaterial3D = _mat(Color("465159"),0.90)
	var board_mat: StandardMaterial3D = _mat(Color("e2e3dc"),0.80)
	var rubber_mat: StandardMaterial3D = _textured_mat(Color("30363a"),0.98,0.0,"noise",64,0.025)
	var pale_mat: StandardMaterial3D = _mat(Color("d3d4ce"),0.93)

	# --- Exterior shell / human scale ---------------------------------------
	# Perimeter is now built around an actual floor plan instead of hiding one
	# demo room inside large overlapping boxes.
	_solid_box(Vector3(0.34,4.10,12.8),school_mat,Vector3(-12.0,2.05,0.0),root)
	_solid_box(Vector3(0.34,4.10,12.8),school_mat,Vector3(13.0,2.05,0.0),root)
	_solid_box(Vector3(25.3,4.10,0.34),school_mat,Vector3(0.5,2.05,-6.25),root)
	# Front wall is segmented around the entrance and windows; its visual meshes
	# are hidden while indoors, collisions remain active.
	var front_left: MeshInstance3D = _solid_box(Vector3(6.0,4.10,0.34),school_mat,Vector3(-8.85,2.05,6.25),root)
	var front_mid: MeshInstance3D = _solid_box(Vector3(5.8,4.10,0.34),school_mat,Vector3(-0.20,2.05,6.25),root)
	var front_right: MeshInstance3D = _solid_box(Vector3(9.7,4.10,0.34),school_mat,Vector3(8.0,2.05,6.25),root)
	_school_cutaway.append(front_left)
	_school_cutaway.append(front_mid)
	_school_cutaway.append(front_right)

	# Lower side wings keep the public-building silhouette.
	_solid_box(Vector3(8.0,3.15,8.8),school_mat,Vector3(-16.2,1.575,0.8),root)
	_solid_box(Vector3(6.6,3.45,6.6),_textured_mat(Color("8f999d"),0.91,0.0,"horizontal",96,0.025),Vector3(17.0,1.725,-2.0),root)

	# Full roof outdoors; hidden in one shot when the player is inside.
	var roof_main: MeshInstance3D = _box(Vector3(25.8,0.34,13.2),roof_mat,Vector3(0.5,4.28,0.0),root)
	var roof_snow: MeshInstance3D = _box(Vector3(25.4,0.12,12.8),packed_snow_mat,Vector3(0.5,4.51,0.0),root)
	_school_cutaway.append(roof_main)
	_school_cutaway.append(roof_snow)
	_box(Vector3(8.5,0.28,9.2),roof_mat,Vector3(-16.2,3.27,0.8),root)
	_box(Vector3(8.2,0.11,8.9),packed_snow_mat,Vector3(-16.2,3.48,0.8),root)

	# Facade base and windows.
	_box(Vector3(25.2,0.82,0.16),concrete_mat,Vector3(0.5,0.43,6.43),root)
	for x_value: float in [-10.2,-8.0,-0.2,2.3,4.8,7.3,9.8,12.0]:
		_add_window(root,Vector3(x_value,2.20,6.45),Vector3(1.35,1.05,0.11))
	_label3d("ROSVIKS SKOLA",Vector3(6.0,3.82,6.57),root,30)

	# Entrance - no raised collision threshold.
	_solid_box(Vector3(0.36,3.05,2.2),dark_mat,Vector3(-6.05,1.525,7.02),root)
	_solid_box(Vector3(0.36,3.05,2.2),dark_mat,Vector3(-2.85,1.525,7.02),root)
	_box(Vector3(4.3,0.24,2.6),roof_mat,Vector3(-4.45,3.08,8.0),root)
	var dl: MeshInstance3D = _box(Vector3(0.92,2.35,0.10),glass_mat,Vector3(-5.10,1.23,8.35),root)
	dl.rotation.y = -0.72
	var dr: MeshInstance3D = _box(Vector3(0.92,2.35,0.10),glass_mat,Vector3(-3.80,1.23,8.35),root)
	dr.rotation.y = 0.72
	_box(Vector3(4.0,0.02,2.6),concrete_mat,Vector3(-4.45,0.012,8.9),root)
	_label3d("ENTRÉ",Vector3(-4.45,2.70,8.43),root,16)

	# --- Floor plan ----------------------------------------------------------
	# Foyer flows into a 2.4 m east-west main corridor. Three classrooms sit on
	# the south side, with expedition/resource space north of the corridor.
	_box(Vector3(3.2,0.05,4.6),interior_floor,Vector3(-4.45,0.05,4.0),root)
	_box(Vector3(17.8,0.05,2.35),interior_floor,Vector3(3.0,0.05,1.55),root)
	_box(Vector3(5.4,0.05,4.55),classroom_floor,Vector3(-2.5,0.05,-2.65),root)
	_box(Vector3(5.4,0.05,4.55),classroom_floor,Vector3(3.25,0.05,-2.65),root)
	_box(Vector3(5.4,0.05,4.55),classroom_floor,Vector3(9.0,0.05,-2.65),root)
	_box(Vector3(5.1,0.05,3.2),_textured_mat(Color("7b756d"),0.93,0.0,"horizontal",96,0.010),Vector3(0.1,0.05,4.45),root)
	_box(Vector3(3.0,0.02,1.20),rubber_mat,Vector3(-4.45,0.085,5.75),root)

	# Foyer / corridor walls with actual door gaps.
	_solid_box(Vector3(0.28,3.45,7.8),interior_wall,Vector3(-6.0,1.725,2.15),root)
	_solid_box(Vector3(0.28,3.45,1.3),interior_wall,Vector3(-2.85,1.725,5.45),root)
	# North wall of corridor, split around expedition door.
	_solid_box(Vector3(3.3,3.45,0.28),interior_wall,Vector3(-3.9,1.725,2.80),root)
	_solid_box(Vector3(9.3,3.45,0.28),interior_wall,Vector3(7.65,1.725,2.80),root)
	# South wall of corridor: four solid pieces create three classroom door gaps.
	_solid_box(Vector3(1.3,3.45,0.28),interior_wall,Vector3(-5.2,1.725,0.28),root)
	_solid_box(Vector3(2.7,3.45,0.28),interior_wall,Vector3(-0.05,1.725,0.28),root)
	_solid_box(Vector3(2.7,3.45,0.28),interior_wall,Vector3(5.70,1.725,0.28),root)
	_solid_box(Vector3(1.4,3.45,0.28),interior_wall,Vector3(11.95,1.725,0.28),root)

	# Classroom separation walls. Doorways are from the corridor, so these can be whole walls.
	_solid_box(Vector3(0.28,3.45,4.55),interior_wall,Vector3(0.25,1.725,-2.65),root)
	_solid_box(Vector3(0.28,3.45,4.55),interior_wall,Vector3(6.0,1.725,-2.65),root)

	# Expedition/resource room north of corridor.
	_solid_box(Vector3(0.28,3.45,3.15),interior_wall,Vector3(-2.55,1.725,4.42),root)
	_solid_box(Vector3(5.1,3.45,0.28),interior_wall,Vector3(0.0,1.725,6.0),root)
	_solid_box(Vector3(5.1,3.45,0.28),interior_wall,Vector3(0.0,1.725,2.92),root)

	# Corridor: lockers and bench against walls, never in the path.
	for i: int in range(7):
		var x_l: float = 0.1 + float(i)*0.55
		_solid_box(Vector3(0.48,1.72,0.36),locker_mat,Vector3(x_l,0.86,2.52),root)
		_box(Vector3(0.25,0.05,0.03),metal_mat,Vector3(x_l,0.98,2.31),root)
	_solid_box(Vector3(1.65,0.11,0.42),desk_mat,Vector3(-4.95,0.48,3.55),root)
	for x_leg: float in [-5.45,-4.45]:
		_solid_box(Vector3(0.07,0.46,0.07),chair_mat,Vector3(x_leg,0.24,3.55),root)
	var notice: StandardMaterial3D = _mat(Color("927554"),0.94)
	_box(Vector3(0.06,0.95,1.40),notice,Vector3(-5.86,1.55,4.20),root)
	for i: int in range(5):
		_box(Vector3(0.025,0.20,0.18),_mat(Color("dad7cb").darkened(float(i)*0.025),0.96),Vector3(-5.82,1.32+float(i%2)*0.28,3.72+float(i/2)*0.37),root)
	# Wall radiator and extinguisher.
	for x_rad: float in [6.8,9.0]:
		_box(Vector3(1.05,0.44,0.12),pale_mat,Vector3(x_rad,0.47,2.62),root)
	var extinguisher: MeshInstance3D = _cylinder(0.11,0.48,_mat(Color("a23830"),0.82),Vector3(-5.78,0.58,3.1),root)
	extinguisher.rotation.z = PI/2.0

	# Expedition furniture is deliberately sparse and wall anchored.
	_solid_box(Vector3(1.45,0.10,0.65),desk_mat,Vector3(-0.9,0.75,4.55),root)
	_solid_box(Vector3(0.50,1.85,1.55),_mat(Color("7b817d"),0.92),Vector3(2.20,0.93,4.45),root)
	_box(Vector3(0.07,0.80,1.45),glass_mat,Vector3(-2.38,1.52,4.45),root)
	_label3d("EXPEDITION",Vector3(-2.30,2.25,4.45),root,14)

	# Three different classrooms so the school no longer feels like one demo room.
	_build_classroom(root,Vector3(-2.5,0.0,-2.65),"KLASSRUM 1",Color("997554"),0)
	_build_classroom(root,Vector3(3.25,0.0,-2.65),"KLASSRUM 2",Color("8d765b"),1)
	_build_classroom(root,Vector3(9.0,0.0,-2.65),"KLASSRUM 3",Color("91704f"),2)

	# Lighting: small fixtures along circulation and one pair per classroom.
	var fixture_mat: StandardMaterial3D = _mat(Color("e7e8df"),0.58)
	fixture_mat.emission_enabled = true
	fixture_mat.emission = Color("ffefc5")
	fixture_mat.emission_energy_multiplier = 0.78
	for x_light: float in [-4.3,-1.0,2.5,6.0,9.5]:
		_box(Vector3(0.78,0.045,0.16),fixture_mat,Vector3(x_light,3.30,1.55),root)
		var hall_light: OmniLight3D = OmniLight3D.new()
		hall_light.position = Vector3(x_light,3.0,1.55)
		hall_light.light_color = Color("ffe7ba")
		hall_light.light_energy = 0.50
		hall_light.omni_range = 3.5
		root.add_child(hall_light)
	for room_x: float in [-2.5,3.25,9.0]:
		for room_z: float in [-1.65,-3.65]:
			_box(Vector3(0.88,0.045,0.16),fixture_mat,Vector3(room_x,3.30,room_z),root)
			var room_light: OmniLight3D = OmniLight3D.new()
			room_light.position = Vector3(room_x,2.95,room_z)
			room_light.light_color = Color("ffe7ba")
			room_light.light_energy = 0.58
			room_light.omni_range = 3.6
			root.add_child(room_light)

	# Yard - two real gates, passable snow and compacted pedestrian paths.
	_box(Vector3(34.0,0.07,20.0),packed_snow_mat,Vector3(-1.0,0.03,19.0),root)
	_box(Vector3(13.0,0.05,8.0),slush_mat,Vector3(-4.0,0.05,11.5),root)
	_box(Vector3(4.0,0.05,12.0),slush_mat,Vector3(-4.45,0.05,16.0),root)
	_add_fence(Vector3(-18.0,0.0,13.0),Vector3(-6.7,0.0,13.0),root)
	_add_fence(Vector3(-2.3,0.0,13.0),Vector3(15.0,0.0,13.0),root)
	_gate_posts(Vector3(-6.7,0.0,13.0),Vector3(-2.3,0.0,13.0),root)
	_add_fence(Vector3(-18.0,0.0,13.0),Vector3(-18.0,0.0,31.0),root)
	_add_fence(Vector3(15.0,0.0,13.0),Vector3(15.0,0.0,18.8),root)
	_add_fence(Vector3(15.0,0.0,23.1),Vector3(15.0,0.0,31.0),root)
	_gate_posts(Vector3(15.0,0.0,18.8),Vector3(15.0,0.0,23.1),root)
	_add_fence(Vector3(-18.0,0.0,31.0),Vector3(15.0,0.0,31.0),root)
	for i: int in range(6):
		var rack: MeshInstance3D = _box(Vector3(0.06,0.50,1.20),metal_mat,Vector3(-7.2+float(i)*0.50,0.27,11.8),root)
		rack.rotation.z = 0.17
	_add_bench(Vector3(-11.0,0.0,18.0),root)
	_add_goal(Vector3(-9.0,0.0,26.0),root)
	_add_basket(Vector3(8.0,0.0,25.0),root)
	_add_flag(Vector3(11.0,0.0,10.3),root)
	_add_dumpster(Vector3(-16.0,0.0,9.0),root)

func _build_classroom(root: Node3D, center: Vector3, title: String, wood_color: Color, variant: int) -> void:
	var desk_mat_local: StandardMaterial3D = _mat(wood_color,0.92)
	var chair_mat_local: StandardMaterial3D = _mat(Color("465159"),0.90)
	var board_mat_local: StandardMaterial3D = _mat(Color("e4e5de"),0.80)
	# Board on outer south wall.
	_box(Vector3(2.25,1.15,0.07),board_mat_local,center+Vector3(0.0,1.85,-2.18),root)
	_box(Vector3(2.20,0.06,0.12),metal_mat,center+Vector3(0.0,1.15,-2.12),root)
	_label3d(title,center+Vector3(-2.35,2.62,2.15),root,12)
	# Teacher desk shifted sideways to keep central sightline open.
	_solid_box(Vector3(1.30,0.10,0.60),desk_mat_local,center+Vector3(1.70,0.74,-1.55),root)
	for x_leg: float in [1.25,2.15]:
		_solid_box(Vector3(0.06,0.65,0.06),metal_mat,center+Vector3(x_leg,0.34,-1.55),root)
	# Six student desks, chairs visual-only so navigation remains smooth.
	for row: int in range(2):
		for col: int in range(3):
			var px: float = -1.75+float(col)*1.35
			var pz: float = 0.65-float(row)*1.35
			_solid_box(Vector3(0.92,0.07,0.50),desk_mat_local,center+Vector3(px,0.68,pz),root)
			for leg_x: float in [-0.34,0.34]:
				_solid_box(Vector3(0.05,0.60,0.05),metal_mat,center+Vector3(px+leg_x,0.32,pz),root)
			_box(Vector3(0.40,0.07,0.38),chair_mat_local,center+Vector3(px,0.41,pz+0.48),root)
			_box(Vector3(0.40,0.38,0.06),chair_mat_local,center+Vector3(px,0.62,pz+0.64),root)
	# Different rear-wall identity per room.
	if variant == 0:
		_solid_box(Vector3(0.42,1.70,1.40),_mat(Color("78817d"),0.92),center+Vector3(-2.35,0.85,-1.05),root)
	elif variant == 1:
		_box(Vector3(1.8,0.95,0.07),_mat(Color("927554"),0.94),center+Vector3(-1.0,1.60,2.15),root)
		for i: int in range(4):
			_box(Vector3(0.20,0.26,0.03),_mat(Color("d5d1c5").darkened(float(i)*0.035),0.96),center+Vector3(-1.55+float(i)*0.38,1.58,2.10),root)
	else:
		_solid_box(Vector3(1.45,0.72,0.42),desk_mat_local,center+Vector3(-1.45,0.38,-1.60),root)
		_box(Vector3(0.62,0.38,0.05),_mat(Color("536d79"),0.72),center+Vector3(-1.45,1.10,-2.16),root)

func _build_arena() -> void:
	# The building immediately by the school is Rosvik Sporthall, not the ice rink.
	_sporthall_root = Node3D.new()
	_sporthall_root.name = "RosvikSporthall"
	_sporthall_root.position = Vector3(52.0,0.0,19.0)
	add_child(_sporthall_root)
	var hall_wall: StandardMaterial3D = _textured_mat(Color("68747a"),0.82,0.06,"vertical",96,0.025)
	var hall_dark: StandardMaterial3D = _textured_mat(Color("465158"),0.84,0.08,"vertical",64,0.022)
	var gym_floor: StandardMaterial3D = _textured_mat(Color("a77b4f"),0.78,0.02,"horizontal",96,0.010)
	var court_line: StandardMaterial3D = _mat(Color("e7e1d2"),0.88)
	# Shell with an open public entrance and a cutaway side exposing the gym floor.
	_solid_box(Vector3(0.34,6.2,18.0),hall_wall,Vector3(-14.0,3.1,0.0),_sporthall_root)
	_solid_box(Vector3(0.34,6.2,18.0),hall_wall,Vector3(14.0,3.1,0.0),_sporthall_root)
	_solid_box(Vector3(28.3,6.2,0.34),hall_wall,Vector3(0.0,3.1,-9.0),_sporthall_root)
	_solid_box(Vector3(8.8,6.2,0.34),hall_wall,Vector3(-9.7,3.1,9.0),_sporthall_root)
	_solid_box(Vector3(12.0,6.2,0.34),hall_wall,Vector3(8.0,3.1,9.0),_sporthall_root)
	_box(Vector3(28.6,0.36,18.4),roof_mat,Vector3(0.0,6.35,0.0),_sporthall_root)
	_box(Vector3(28.2,0.12,18.0),packed_snow_mat,Vector3(0.0,6.58,0.0),_sporthall_root)
	_box(Vector3(26.4,0.06,15.8),gym_floor,Vector3(0.0,0.05,0.0),_sporthall_root)
	# Court markings.
	_box(Vector3(22.0,0.018,0.08),court_line,Vector3(0.0,0.09,0.0),_sporthall_root)
	for x_line: float in [-10.5,10.5]:
		_box(Vector3(0.08,0.018,12.0),court_line,Vector3(x_line,0.09,0.0),_sporthall_root)
	# Basketball and handball cues from the real sporthall's supported equipment.
	_add_basket(Vector3(-11.0,0.0,0.0),_sporthall_root)
	_add_basket(Vector3(11.0,0.0,0.0),_sporthall_root)
	_add_goal(Vector3(-11.3,0.0,5.5),_sporthall_root)
	_add_goal(Vector3(11.3,0.0,-5.5),_sporthall_root)
	# Entrance lobby block and doors.
	_solid_box(Vector3(7.0,3.2,4.2),hall_dark,Vector3(-2.0,1.6,11.0),_sporthall_root)
	_box(Vector3(4.0,2.45,0.10),glass_mat,Vector3(-2.0,1.28,13.15),_sporthall_root)
	_box(Vector3(5.2,0.26,2.6),roof_mat,Vector3(-2.0,3.28,13.0),_sporthall_root)
	_label3d("ROSVIK SPORTHALL",Vector3(2.0,5.20,9.18),_sporthall_root,28)
	_label3d("ENTRÉ",Vector3(-2.0,3.45,13.25),_sporthall_root,16)

	# HALA Hallen / former Kristallen sits farther south, separated from the sporthall.
	_icehall_root = Node3D.new()
	_icehall_root.name = "HALAHallen"
	_icehall_root.position = Vector3(55.0,0.0,63.0)
	add_child(_icehall_root)
	var ice_wall: StandardMaterial3D = _textured_mat(Color("56636a"),0.80,0.08,"vertical",96,0.028)
	_solid_box(Vector3(38.0,7.0,21.0),ice_wall,Vector3(0.0,3.5,0.0),_icehall_root)
	_box(Vector3(39.0,0.38,22.0),roof_mat,Vector3(0.0,7.18,0.0),_icehall_root)
	_box(Vector3(38.5,0.14,21.5),packed_snow_mat,Vector3(0.0,7.42,0.0),_icehall_root)
	_box(Vector3(34.0,0.48,0.14),_mat(Color("314b5a"),0.76,0.06),Vector3(0.0,5.80,10.75),_icehall_root)
	for i: int in range(6):
		_add_window(_icehall_root,Vector3(-13.0+float(i)*5.0,4.65,10.72),Vector3(2.0,0.88,0.12))
	_box(Vector3(5.6,0.28,2.8),roof_mat,Vector3(-8.5,3.25,14.0),_icehall_root)
	_box(Vector3(3.2,2.45,0.10),glass_mat,Vector3(-8.5,1.30,14.68),_icehall_root)
	_label3d("HALA HALLEN",Vector3(0.0,5.95,10.88),_icehall_root,32)
	_label3d("ROSVIK HOCKEY",Vector3(11.0,3.55,10.88),_icehall_root,16)
	_box(Vector3(48.0,0.06,18.0),slush_mat,Vector3(0.0,0.05,20.0),_icehall_root)
	for x_value: float in [-12.0,-4.0,4.0,12.0]:
		_box(Vector3(1.3,0.75,1.0),metal_mat,Vector3(x_value,7.90,-1.0),_icehall_root)

func _add_arena_identity() -> void:
	# Identity is now split between the municipal sporthall and the separate ice rink.
	if _sporthall_root != null:
		var blue: StandardMaterial3D = _mat(Color("365d76"),0.78,0.05)
		_box(Vector3(6.0,0.45,0.10),blue,Vector3(5.0,4.45,9.18),_sporthall_root)
		_label3d("A-HALL",Vector3(5.0,4.45,9.26),_sporthall_root,14)
	if _icehall_root != null:
		for i: int in range(4):
			var stick: MeshInstance3D = _box(Vector3(0.045,1.30,0.045),dark_mat,Vector3(-13.0+float(i)*0.20,0.68,13.0),_icehall_root)
			stick.rotation.z = -0.10+float(i)*0.04

func _add_rosvalla_context() -> void:
	# Rosvalla sits in the school/sports cluster. Keep this as a low-detail public
	# context field for now; it helps the cluster read correctly without inventing homes.
	var turf: StandardMaterial3D = _textured_mat(Color("6f8076"),0.96,0.0,"noise",96,0.018)
	var field: MeshInstance3D = _box(Vector3(42.0,0.035,24.0),turf,Vector3(18.0,0.04,52.0),self)
	field.material_override = turf
	var line_mat: StandardMaterial3D = _mat(Color("d7d8cf"),0.95)
	_box(Vector3(38.0,0.012,0.08),line_mat,Vector3(18.0,0.07,40.5),self)
	_box(Vector3(38.0,0.012,0.08),line_mat,Vector3(18.0,0.07,63.5),self)
	_box(Vector3(0.08,0.012,23.0),line_mat,Vector3(-1.0,0.07,52.0),self)
	_box(Vector3(0.08,0.012,23.0),line_mat,Vector3(37.0,0.07,52.0),self)
	_add_goal(Vector3(-0.5,0.0,52.0),self)
	_add_goal(Vector3(36.5,0.0,52.0),self)
	_label3d("ROSVALLA",Vector3(18.0,0.22,40.0),self,18)

func _update_school_cutaway() -> void:
	if player == null:
		return
	var p: Vector3 = player.global_position
	var now_inside: bool = p.x > -11.7 and p.x < 12.8 and p.z > -5.95 and p.z < 6.15
	if now_inside == _inside_school:
		return
	_inside_school = now_inside
	for node: Node3D in _school_cutaway:
		if is_instance_valid(node):
			node.visible = not _inside_school

func _build_ui() -> void:
	var ui: CanvasLayer = CanvasLayer.new()
	add_child(ui)
	var panel: ColorRect = ColorRect.new()
	panel.position = Vector2(18.0,18.0)
	panel.size = Vector2(345.0,118.0)
	panel.color = Color(0.015,0.025,0.032,0.82)
	ui.add_child(panel)
	var label: Label = Label.new()
	label.position = Vector2(14.0,10.0)
	label.text = "ROSVIK: BLACKOUT\nROSVIK ACCURACY / INTERIOR 02\n\n3 klassrum • sporthall • HALA Hallen • passbar snö"
	label.add_theme_font_size_override("font_size",14)
	panel.add_child(label)
	var hint: Label = Label.new()
	hint.position = Vector2(18.0,852.0)
	hint.text = "WASD / pilar: gå     Shift: spring"
	hint.add_theme_font_size_override("font_size",13)
	ui.add_child(hint)

func _update_camera(delta: float) -> void:
	if player == null or camera == null:
		return
	var p: Vector3 = player.global_position
	var move_lead: Vector3 = Vector3(player.velocity.x,0.0,player.velocity.z)
	if move_lead.length() > 0.15:
		move_lead = move_lead.normalized()
	var in_school: bool = p.x > -11.7 and p.x < 12.8 and p.z > -5.95 and p.z < 6.15
	var in_foyer: bool = p.x > -6.1 and p.x < -2.8 and p.z > 2.7 and p.z < 6.15
	var in_corridor: bool = p.x > -6.1 and p.x < 12.8 and p.z > 0.25 and p.z <= 2.75
	var in_classrooms: bool = p.x > -5.3 and p.x < 12.0 and p.z > -5.0 and p.z <= 0.25
	var focus: Vector3 = p+Vector3(0.0,0.95,0.0)
	if in_classrooms:
		focus += move_lead*0.28+Vector3(0.20,0.0,-0.10)
		target_camera_pos = focus+Vector3(6.4,7.4,6.8)
		camera.fov = lerp(camera.fov,42.0,1.0-exp(-5.0*delta))
	elif in_corridor:
		focus += move_lead*0.30
		target_camera_pos = focus+Vector3(6.8,7.5,6.4)
		camera.fov = lerp(camera.fov,42.5,1.0-exp(-5.0*delta))
	elif in_foyer:
		focus += move_lead*0.35
		target_camera_pos = focus+Vector3(6.2,7.2,6.8)
		camera.fov = lerp(camera.fov,42.5,1.0-exp(-5.0*delta))
	elif in_school:
		target_camera_pos = focus+Vector3(6.5,7.4,6.5)
		camera.fov = lerp(camera.fov,42.0,1.0-exp(-5.0*delta))
	else:
		focus += move_lead*1.35
		target_camera_pos = focus+Vector3(11.2,7.7,11.2)
		camera.fov = lerp(camera.fov,43.0,1.0-exp(-4.0*delta))
	camera.global_position = camera.global_position.lerp(target_camera_pos,1.0-exp(-5.0*delta))
	camera.look_at(focus,Vector3.UP)
