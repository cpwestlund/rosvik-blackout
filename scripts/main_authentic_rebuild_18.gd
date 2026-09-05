extends "res://scripts/main_authentic_slice_17.gd"

# AUTHENTIC REBUILD 18
# A hard visual reset of the school/sports slice. The working gameplay stays,
# but legacy roads, duplicated yard dressing and the incorrect one-field Rosvalla
# are suppressed and replaced by one coherent authored layer.

var _rebuild18_root: Node3D
var _roads18_root: Node3D
var _yard18_root: Node3D
var _rosvalla18_root: Node3D
var _assets18_root: Node3D
var _camera18_dragging := false
var _asset_count18 := 0
var _grounded_count18 := 0

func _ready() -> void:
	super._ready()
	_rebuild18_root = Node3D.new()
	_rebuild18_root.name = "AuthenticRebuild18"
	add_child(_rebuild18_root)
	_hard_reset_visual_layers18()
	_purge_old_rosvalla18(self)
	_purge_schoolyard18()
	_cleanup_duplicate_world_text18()
	_build_clean_roads18()
	_build_grounded_schoolyard18()
	_build_five_pitch_rosvalla18()
	_build_cc0_dressing18()
	_rebuild_building_signage18()
	_tune_slice_mood18()
	_collect_occluders12()
	print("ROSVIK_AUTHENTIC_REBUILD_18_READY")
	print("ROSVIK_NATURAL_CAMERA_18_READY")
	print("ROSVIK_CLEAN_ROADS_18_READY")
	print("ROSVIK_ROSVALLA_FIVE_PITCH_18_READY")
	print("ROSVIK_GROUNDED_WORLD_18_READY count=",_grounded_count18)
	print("ROSVIK_CC0_ASSETS_18_READY count=",_asset_count18)

# -------------------------------------------------------------------------
# CAMERA: MOUSE DRAG IS NO LONGER MIRRORED
# -------------------------------------------------------------------------
func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventMouseButton:
		var mb := event as InputEventMouseButton
		if mb.button_index == MOUSE_BUTTON_MIDDLE:
			_camera18_dragging = mb.pressed
			get_viewport().set_input_as_handled()
		elif mb.button_index == MOUSE_BUTTON_WHEEL_UP and mb.pressed:
			_camera12_distance = clampf(_camera12_distance-1.8,16.0,33.0)
			get_viewport().set_input_as_handled()
		elif mb.button_index == MOUSE_BUTTON_WHEEL_DOWN and mb.pressed:
			_camera12_distance = clampf(_camera12_distance+1.8,16.0,33.0)
			get_viewport().set_input_as_handled()
	elif event is InputEventMouseMotion and _camera18_dragging:
		var mm := event as InputEventMouseMotion
		# Pass 12 used the opposite yaw sign. This makes a horizontal drag follow
		# the hand rather than feeling like the world is mirrored.
		_camera12_yaw += mm.relative.x*0.0068
		_camera12_pitch = clampf(_camera12_pitch-mm.relative.y*0.0042,0.40,0.80)
		get_viewport().set_input_as_handled()

func _update_camera(delta: float) -> void:
	if player == null or camera == null:
		return
	var focus := player.global_position+Vector3(0.0,1.12,0.0)
	var travel := Vector3(player.velocity.x,0.0,player.velocity.z)
	if travel.length() > 0.2:
		focus += travel.normalized()*0.38
	var horizontal := cos(_camera12_pitch)*_camera12_distance
	var offset := Vector3(cos(_camera12_yaw)*horizontal,sin(_camera12_pitch)*_camera12_distance,sin(_camera12_yaw)*horizontal)
	target_camera_pos = focus+offset
	camera.global_position = camera.global_position.lerp(target_camera_pos,1.0-exp(-7.4*delta))
	camera.fov = lerpf(camera.fov,42.5,1.0-exp(-5.0*delta))
	camera.look_at(focus,Vector3.UP)

# -------------------------------------------------------------------------
# HARD RESET: STOP RENDERING THREE GENERATIONS OF THE SAME PLACE
# -------------------------------------------------------------------------
func _hard_reset_visual_layers18() -> void:
	for path: String in ["RoadTopology09","WorldCoherence09","VisualIdentityWorld16","AuthenticRosvikSlice17","PropCollisionPass11"]:
		var legacy := get_node_or_null(path)
		if legacy != null:
			_disable_tree18(legacy)

	# The generic prop pass creates unnamed car/lamp roots directly below the world.
	# Remove only those inside the rebuilt civic slice; residential content remains.
	for child: Node in get_children():
		if not child is Node3D:
			continue
		var n := child as Node3D
		if n.name in ["RosviksSkola","RosvikSporthall","HALAHallen","WorldExpansion11","ResidentialLife12","AuthenticRebuild18"]:
			continue
		var p := n.global_position
		if p.x > -30.0 and p.x < 88.0 and p.z > 5.0 and p.z < 95.0:
			if n.name == "Node3D" or n is MeshInstance3D or n is StaticBody3D:
				_disable_tree18(n)

func _disable_tree18(node: Node) -> void:
	if node is GeometryInstance3D:
		(node as GeometryInstance3D).visible = false
	if node is Label3D:
		(node as Label3D).visible = false
	if node is CollisionObject3D:
		var body := node as CollisionObject3D
		body.collision_layer = 0
		body.collision_mask = 0
	for child: Node in node.get_children():
		_disable_tree18(child)

# -------------------------------------------------------------------------
# SCHOOLYARD RESET: ONE GROUNDED SET OF PROPS / ONE FENCE SYSTEM
# -------------------------------------------------------------------------
func _purge_schoolyard18() -> void:
	var school := get_node_or_null("RosviksSkola") as Node3D
	if school == null:
		return
	_purge_schoolyard_node18(school,school)

func _purge_schoolyard_node18(node: Node,school: Node3D) -> void:
	for child: Node in node.get_children():
		if child is Node3D:
			var n := child as Node3D
			var p := n.global_position
			if p.x > -20.5 and p.x < 16.5 and p.z > 12.2 and p.z < 32.2 and p.y < 4.2:
				_disable_tree18(n)
				continue
		_purge_schoolyard_node18(child,school)

func _build_grounded_schoolyard18() -> void:
	var school := get_node_or_null("RosviksSkola") as Node3D
	if school == null:
		return
	_yard18_root = Node3D.new()
	_yard18_root.name = "Schoolyard18"
	school.add_child(_yard18_root)

	# Ground sits almost flush with the snow base; no floating rectangular podium.
	_box(Vector3(33.0,0.025,17.0),packed_snow_mat,Vector3(-1.0,0.018,22.3),_yard18_root)
	var fence_mat := _mat(Color("424d50"),0.74,0.18)
	_fence18(Vector3(-18.0,0.0,14.5),Vector3(-7.2,0.0,14.5),fence_mat)
	_fence18(Vector3(-1.8,0.0,14.5),Vector3(15.0,0.0,14.5),fence_mat)
	_fence18(Vector3(-18.0,0.0,14.5),Vector3(-18.0,0.0,30.5),fence_mat)
	_fence18(Vector3(15.0,0.0,14.5),Vector3(15.0,0.0,30.5),fence_mat)
	_fence18(Vector3(-18.0,0.0,30.5),Vector3(15.0,0.0,30.5),fence_mat)
	for x: float in [-7.2,-1.8]:
		_solid_cylinder(0.055,1.45,fence_mat,Vector3(x,0.725,14.5),_yard18_root)

	_add_bike_rack18(Vector3(7.5,0.0,17.2))
	_add_playground18(Vector3(5.2,0.0,25.5))
	_add_grit_bin18(Vector3(12.7,0.0,18.0))
	_add_snow_shovel18(Vector3(13.6,0.0,17.5))
	_add_bench(Vector3(-10.5,0.0,19.2),_yard18_root)
	_grounded_count18 += 1

	# Plough ridges live against the fence, not in the carriageway.
	for p: Vector3 in [Vector3(-16.0,0.0,29.6),Vector3(-7.0,0.0,30.0),Vector3(8.0,0.0,30.0),Vector3(14.2,0.0,26.5)]:
		_soft_snow_mound17(_yard18_root,p,Vector3(2.2,0.34,0.75),randf_range(-0.10,0.10))

func _fence18(a: Vector3,b: Vector3,mat: Material) -> void:
	var d := b-a
	var length := d.length()
	var mid := (a+b)*0.5
	var yaw := -atan2(d.z,d.x)
	for y: float in [0.48,0.94]:
		var rail := _cylinder(0.026,length,mat,mid+Vector3(0.0,y,0.0),_yard18_root)
		rail.rotation.z = PI/2.0
		rail.rotation.y = yaw
	var posts := maxi(2,int(length/2.1))
	for i: int in range(posts+1):
		var p := a.lerp(b,float(i)/float(posts))
		_solid_cylinder(0.035,1.25,mat,p+Vector3(0.0,0.625,0.0),_yard18_root)
	_grounded_count18 += posts+3

func _add_bike_rack18(pos: Vector3) -> void:
	var root := Node3D.new()
	root.position = pos
	_yard18_root.add_child(root)
	var metal := _mat(Color("657174"),0.65,0.26)
	for i: int in range(6):
		var x := -1.4+float(i)*0.56
		var post := _cylinder(0.028,0.85,metal,Vector3(x,0.42,0.0),root)
		post.rotation.x = PI/2.0
	_grounded_count18 += 6

func _add_playground18(pos: Vector3) -> void:
	var root := Node3D.new()
	root.position = pos
	_yard18_root.add_child(root)
	var timber := _textured_mat(Color("715843"),0.96,0.0,"vertical",64,0.018)
	var metal := _mat(Color("566266"),0.72,0.22)
	for x: float in [-1.35,1.35]:
		var leg_a := _cylinder(0.065,2.25,timber,Vector3(x,1.06,-0.55),root)
		leg_a.rotation.z = 0.24 if x < 0.0 else -0.24
		var leg_b := _cylinder(0.065,2.25,timber,Vector3(x,1.06,0.55),root)
		leg_b.rotation.z = 0.24 if x < 0.0 else -0.24
	var top := _cylinder(0.07,3.0,metal,Vector3(0.0,2.08,0.0),root)
	top.rotation.z = PI/2.0
	for x: float in [-0.55,0.55]:
		for z: float in [-0.22,0.22]:
			_cylinder(0.014,1.15,metal,Vector3(x,1.47,z),root)
		_box(Vector3(0.58,0.06,0.34),_mat(Color("805747"),0.90),Vector3(x,0.91,0.0),root)
	_grounded_count18 += 11

func _add_grit_bin18(pos: Vector3) -> void:
	var root := Node3D.new()
	root.position = pos
	_yard18_root.add_child(root)
	var body := _capsule_local(0.36,0.60,_mat(Color("526b63"),0.92),Vector3(0.0,0.31,0.0),root)
	body.scale = Vector3(1.15,0.82,0.95)
	_box(Vector3(0.72,0.08,0.60),_mat(Color("33413e"),0.88),Vector3(0.0,0.62,0.0),root)
	_grounded_count18 += 2

func _add_snow_shovel18(pos: Vector3) -> void:
	var root := Node3D.new()
	root.position = pos
	root.rotation.z = -0.10
	_yard18_root.add_child(root)
	_cylinder(0.027,1.45,_mat(Color("8b6948"),0.96),Vector3(0.0,0.75,0.0),root)
	_box(Vector3(0.48,0.08,0.38),_mat(Color("a55d3c"),0.84),Vector3(0.0,0.10,0.0),root)
	_grounded_count18 += 2

# -------------------------------------------------------------------------
# ROADS: ONE CONTINUOUS STRIP INSTEAD OF STACKED ASPHALT PLATES
# -------------------------------------------------------------------------
func _build_clean_roads18() -> void:
	_roads18_root = Node3D.new()
	_roads18_root.name = "CleanRoadNetwork18"
	_rebuild18_root.add_child(_roads18_root)
	var road_mat := _textured_mat(Color("353b3e"),0.98,0.0,"asphalt",96,0.028)
	road_mat.cull_mode = BaseMaterial3D.CULL_DISABLED

	# Main north/south approach and the local school-sport-HALA route.
	_road_strip18([Vector2(-42,-88),Vector2(-42,78)],6.0,road_mat,0.045)
	_road_strip18([Vector2(-100,-30),Vector2(105,-30)],7.0,road_mat,0.043)
	_road_strip18([
		Vector2(-39,10),Vector2(22,10),Vector2(26,10.5),Vector2(29,12.5),
		Vector2(31,16),Vector2(31,29),Vector2(32,32),Vector2(35,35),
		Vector2(39,36),Vector2(73,36),Vector2(77,37),Vector2(79,40),
		Vector2(80,44),Vector2(80,81)
	],5.15,road_mat,0.047)

	# Parking is flat and sparse; the snow edge supplies the shape, not raised curbs.
	_parking18(Vector3(21.0,0.0,21.0),Vector2(12.0,9.0),4)
	_parking18(Vector3(54.0,0.0,44.0),Vector2(28.0,9.5),7)
	_parking18(Vector3(58.0,0.0,84.0),Vector2(28.0,10.0),7)
	_road_strip18([Vector2(27,17),Vector2(27,21)],3.4,road_mat,0.047)
	_road_strip18([Vector2(54,38.5),Vector2(54,44)],3.8,road_mat,0.047)
	_road_strip18([Vector2(70,79),Vector2(70,84)],3.8,road_mat,0.047)

	# One readable crossing at school. No floating wayfinding arrows.
	var stripe_mat := _mat(Color("c3c4ba"),0.96)
	for i: int in range(5):
		var x := -5.55+float(i)*0.55
		_box(Vector3(0.28,0.012,3.4),stripe_mat,Vector3(x,0.070,10.0),_roads18_root)

func _road_strip18(points: Array[Vector2],width: float,mat: Material,y: float) -> void:
	if points.size() < 2:
		return
	var offsets: Array[Vector2] = []
	var half := width*0.5
	for i: int in range(points.size()):
		var prev_dir: Vector2
		var next_dir: Vector2
		if i == 0:
			prev_dir = (points[1]-points[0]).normalized()
		else:
			prev_dir = (points[i]-points[i-1]).normalized()
		if i == points.size()-1:
			next_dir = prev_dir
		else:
			next_dir = (points[i+1]-points[i]).normalized()
		var tangent := prev_dir+next_dir
		if tangent.length() < 0.01:
			tangent = next_dir
		tangent = tangent.normalized()
		var normal := Vector2(-tangent.y,tangent.x)
		var next_normal := Vector2(-next_dir.y,next_dir.x)
		var denom := maxf(0.55,absf(normal.dot(next_normal)))
		var miter := minf(half/denom,half*1.65)
		offsets.append(normal*miter)

	var st := SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLE_STRIP)
	var distance := 0.0
	for i: int in range(points.size()):
		if i > 0:
			distance += points[i].distance_to(points[i-1])
		var left := points[i]+offsets[i]
		var right := points[i]-offsets[i]
		st.set_normal(Vector3.UP)
		st.set_uv(Vector2(distance/5.0,0.0))
		st.add_vertex(Vector3(left.x,y,left.y))
		st.set_normal(Vector3.UP)
		st.set_uv(Vector2(distance/5.0,1.0))
		st.add_vertex(Vector3(right.x,y,right.y))
	var mesh := st.commit()
	if mesh == null:
		return
	var instance := MeshInstance3D.new()
	instance.mesh = mesh
	instance.material_override = mat
	instance.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_ON
	_roads18_root.add_child(instance)

func _parking18(pos: Vector3,size: Vector2,spaces: int) -> void:
	var parking_mat := _textured_mat(Color("42484a"),0.98,0.0,"asphalt",96,0.024)
	_box(Vector3(size.x,0.025,size.y),parking_mat,pos+Vector3(0.0,0.035,0.0),_roads18_root)
	var line := _mat(Color("aaada6"),0.96)
	var spacing := (size.x-1.2)/float(spaces)
	for i: int in range(spaces+1):
		var x := -size.x*0.5+0.6+float(i)*spacing
		_box(Vector3(0.035,0.010,size.y*0.34),line,pos+Vector3(x,0.055,size.y*0.20),_roads18_root)
	for x: float in [-size.x*0.5-0.7,size.x*0.5+0.7]:
		_soft_snow_mound17(_roads18_root,pos+Vector3(x,0.0,0.0),Vector3(0.75,0.24,size.y*0.43),0.0)

# -------------------------------------------------------------------------
# ROSVALLA: OFFICIAL CLUB INFORMATION SAYS FIVE PITCHES, SO SHOW FIVE PITCHES
# -------------------------------------------------------------------------
func _purge_old_rosvalla18(node: Node) -> void:
	for child: Node in node.get_children():
		if child is Node3D:
			var n := child as Node3D
			if n.name in ["HALAHallen","RosvikSporthall","RosviksSkola","AuthenticRebuild18"]:
				continue
			var p := n.global_position
			if p.x > -5.0 and p.x < 40.0 and p.z > 38.0 and p.z < 73.0 and p.y < 3.0:
				_disable_tree18(n)
				continue
		_purge_old_rosvalla18(child)

func _build_five_pitch_rosvalla18() -> void:
	_rosvalla18_root = Node3D.new()
	_rosvalla18_root.name = "RosvallaFivePitch18"
	_rebuild18_root.add_child(_rosvalla18_root)
	_build_pitch18(Vector3(7.0,0.0,47.0),Vector2(18.0,11.0),"7v7")
	_build_pitch18(Vector3(27.0,0.0,47.0),Vector2(18.0,11.0),"7v7")
	_build_pitch18(Vector3(8.0,0.0,60.5),Vector2(14.0,8.5),"5v5")
	_build_pitch18(Vector3(24.0,0.0,60.5),Vector2(14.0,8.5),"5v5")
	_build_pitch18(Vector3(17.0,0.0,69.0),Vector2(9.0,5.8),"3v3")
	_build_rosvalla_edge18()

func _build_pitch18(center: Vector3,size: Vector2,kind: String) -> void:
	var root := Node3D.new()
	root.position = center
	root.name = "Pitch18_"+kind
	_rosvalla18_root.add_child(root)
	var turf := _textured_mat(Color("61736a"),0.98,0.0,"noise",96,0.016)
	var snowed := _mat(Color("c0cbd0"),0.99)
	var line := _mat(Color("c8cbc4"),0.98)
	_box(Vector3(size.x,0.022,size.y),turf,Vector3(0.0,0.020,0.0),root)
	# Four quiet perimeter lines, partially swallowed by winter.
	_box(Vector3(size.x-0.45,0.008,0.045),line,Vector3(0.0,0.040,-size.y*0.5+0.18),root)
	_box(Vector3(size.x-0.45,0.008,0.045),line,Vector3(0.0,0.040,size.y*0.5-0.18),root)
	_box(Vector3(0.045,0.008,size.y-0.35),line,Vector3(-size.x*0.5+0.18,0.040,0.0),root)
	_box(Vector3(0.045,0.008,size.y-0.35),line,Vector3(size.x*0.5-0.18,0.040,0.0),root)
	_box(Vector3(0.045,0.008,size.y-0.6),line,Vector3(0.0,0.040,0.0),root)
	_goal18(root,Vector3(-size.x*0.5+0.25,0.0,0.0),PI/2.0,kind)
	_goal18(root,Vector3(size.x*0.5-0.25,0.0,0.0),PI/2.0,kind)
	# Uneven snow patches stop the pitches reading as pristine summer rectangles.
	for off: Vector3 in [Vector3(-size.x*0.22,0.0,-size.y*0.18),Vector3(size.x*0.24,0.0,size.y*0.21)]:
		var patch := MeshInstance3D.new()
		var sphere := SphereMesh.new()
		sphere.radius = 1.0
		sphere.height = 2.0
		sphere.radial_segments = 20
		sphere.rings = 8
		patch.mesh = sphere
		patch.material_override = snowed
		patch.position = off+Vector3(0.0,-0.13,0.0)
		patch.scale = Vector3(size.x*0.18,0.15,size.y*0.18)
		root.add_child(patch)
	_grounded_count18 += 10

func _goal18(parent: Node3D,pos: Vector3,yaw: float,kind: String) -> void:
	var root := Node3D.new()
	root.position = pos
	root.rotation.y = yaw
	parent.add_child(root)
	var white := _mat(Color("d2d7d6"),0.86)
	var width := 2.2 if kind == "7v7" else (1.8 if kind == "5v5" else 1.35)
	var height := 1.25 if kind == "7v7" else (1.05 if kind == "5v5" else 0.82)
	_box(Vector3(width,0.055,0.055),white,Vector3(0.0,height,0.0),root)
	for x: float in [-width*0.5,width*0.5]:
		_box(Vector3(0.055,height,0.055),white,Vector3(x,height*0.5,0.0),root)

func _build_rosvalla_edge18() -> void:
	var metal := _mat(Color("4c595c"),0.72,0.20)
	# Low edge fence on the road side only, leaving the complex visually open.
	for z: float in [40.2,72.3]:
		for x: float in [-1.0,7.0,15.0,23.0,31.0,35.0]:
			_solid_cylinder(0.035,1.05,metal,Vector3(x,0.525,z),_rosvalla18_root)
	for p: Vector3 in [Vector3(-1.0,0.0,43.0),Vector3(35.0,0.0,43.0),Vector3(-1.0,0.0,68.0)]:
		var pole := _solid_cylinder(0.06,5.2,metal,p+Vector3(0.0,2.6,0.0),_rosvalla18_root)
		var lamp := OmniLight3D.new()
		lamp.position = p+Vector3(0.0,5.0,0.0)
		lamp.light_color = Color("d7e0d7")
		lamp.light_energy = 0.12
		lamp.omni_range = 8.0
		_rebuild18_root.add_child(lamp)

# -------------------------------------------------------------------------
# READY-MADE CC0 ART: GENERIC ASSETS, ROSVIK-SPECIFIC LAYOUT
# -------------------------------------------------------------------------
func _build_cc0_dressing18() -> void:
	_assets18_root = Node3D.new()
	_assets18_root.name = "CC0Dressing18"
	_rebuild18_root.add_child(_assets18_root)
	var pine_path := "res://assets/vendor/tree_pine.glb"
	for data: Dictionary in [
		{"p":Vector3(-24,0,-3),"s":3.0,"r":0.2}, {"p":Vector3(22,0,2),"s":2.7,"r":-0.3},
		{"p":Vector3(-22,0,31),"s":3.2,"r":0.5}, {"p":Vector3(20,0,32),"s":2.8,"r":0.0},
		{"p":Vector3(36,0,75),"s":3.0,"r":0.3}, {"p":Vector3(77,0,53),"s":3.3,"r":-0.2},
		{"p":Vector3(76,0,73),"s":2.8,"r":0.7}, {"p":Vector3(34,0,37),"s":2.5,"r":0.1}
	]:
		_spawn_asset18(pine_path,data["p"],Vector3.ONE*float(data["s"]),float(data["r"]))

	var rock_path := "res://assets/vendor/rock_large.glb"
	for p: Vector3 in [Vector3(-21.5,0,16.5),Vector3(25.5,0,18.0),Vector3(35.5,0,72.8),Vector3(73.5,0,48.5)]:
		_spawn_asset18(rock_path,p,Vector3.ONE*0.85,randf_range(-PI,PI))

	# One real model does more for the scene than another hand-built box car.
	var truck := _spawn_asset18("res://assets/vendor/vehicle-truck-green.glb",Vector3(61.0,0.0,44.0),Vector3.ONE*1.05,0.03)
	if truck != null:
		_collision_box(_assets18_root,Vector3(3.6,1.45,1.75),Vector3(61.0,0.73,44.0),0.03)
		_grounded_count18 += 1

func _spawn_asset18(path: String,pos: Vector3,scale_value: Vector3,yaw: float) -> Node3D:
	if not ResourceLoader.exists(path):
		return null
	var packed := load(path) as PackedScene
	if packed == null:
		return null
	var raw := packed.instantiate()
	if not raw is Node3D:
		raw.queue_free()
		return null
	var instance := raw as Node3D
	instance.position = pos
	instance.scale = scale_value
	instance.rotation.y = yaw
	_assets18_root.add_child(instance)
	_asset_count18 += 1
	return instance

# -------------------------------------------------------------------------
# ONE BUILDING NAME PER BUILDING, MOUNTED ON ITS FACADE
# -------------------------------------------------------------------------
func _cleanup_duplicate_world_text18() -> void:
	var labels: Array[Label3D] = []
	_collect_label3d16(self,labels)
	for label: Label3D in labels:
		var t := label.text.strip_edges().to_upper()
		if t in ["ROSVIKS SKOLA","ROSVIK SPORTHALL","HALA HALLEN","ROSVIK HOCKEY","A-HALL","ROSVALLA","NORRBOTTEN STÅL ARENA"] or "→" in t:
			label.visible = false

func _rebuild_building_signage18() -> void:
	var school := get_node_or_null("RosviksSkola") as Node3D
	var sport := get_node_or_null("RosvikSporthall") as Node3D
	var hala := get_node_or_null("HALAHallen") as Node3D
	if school != null:
		_facade_sign18(school,"ROSVIKS SKOLA",Vector3(3.6,3.55,6.72),Vector2(7.0,0.72),Color("d8c79f"))
	if sport != null:
		_facade_sign18(sport,"ROSVIK SPORTHALL",Vector3(2.2,5.05,9.24),Vector2(8.8,0.70),Color("c6d1cd"))
	if hala != null:
		_facade_sign18(hala,"HALA HALLEN",Vector3(0.0,5.92,10.90),Vector2(7.0,0.70),Color("b8cacc"))

func _facade_sign18(parent: Node3D,text_value: String,pos: Vector3,size: Vector2,color: Color) -> void:
	_box(Vector3(size.x,size.y,0.08),_mat(Color("273034"),0.82,0.10),pos,parent)
	var label := Label3D.new()
	label.text = text_value
	label.position = pos+Vector3(0.0,0.0,0.055)
	label.font_size = 56
	label.pixel_size = 0.010
	label.modulate = color
	label.outline_size = 7
	label.outline_modulate = Color("171c1e")
	parent.add_child(label)

# -------------------------------------------------------------------------
# MOOD: THE SLICE SHOULD READ COLD OUTSIDE / HUMAN INSIDE AT FIRST GLANCE
# -------------------------------------------------------------------------
func _tune_slice_mood18() -> void:
	var envs: Array[WorldEnvironment] = []
	_collect_world_environments16(self,envs)
	for world_env: WorldEnvironment in envs:
		if world_env.environment == null:
			continue
		var env := world_env.environment
		env.background_mode = Environment.BG_COLOR
		env.background_color = Color("263943")
		env.ambient_light_color = Color("657b86")
		env.ambient_light_energy = 0.30
		env.fog_light_color = Color("536972")
		env.fog_density = 0.0085
		env.tonemap_exposure = 0.58
