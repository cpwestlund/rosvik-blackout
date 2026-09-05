extends "res://scripts/main_reference_art_19b.gd"

# HERO 19C — POLISH / SCALE PASS
# Tightens the playable camera, removes the striped-football-field look and
# reframes the interior so it reads as a room rather than a technical cutaway.

var _ortho_size19c := 15.8

func _ready() -> void:
	super._ready()
	_ortho_size19b = _ortho_size19c
	if camera != null:
		camera.size = _ortho_size19c
	print("ROSVIK_POLISH_PASS_19C_READY")
	print("ROSVIK_INTIMATE_CAMERA_19C_READY")
	print("ROSVIK_PATCHY_ROSVALLA_19C_READY")

# -----------------------------------------------------------------------------
# ROSVALLA — PATCHY WINTER FIELD, NO GIANT PARALLEL STRIPES
# -----------------------------------------------------------------------------
func _build_rosvalla19() -> void:
	var root := Node3D.new()
	root.name = "Rosvalla19"
	add_child(root)
	var center := Vector3(9.0,0.0,53.0)
	_box(Vector3(64.0,0.032,38.0),field_mat,center+Vector3(0,0.016,0),root)

	# Irregular wind-packed snow patches. They overlap softly but never create
	# full-width parallel bands across the pitch.
	var snow_patch := _mat(Color("9eafb6"),0.995)
	var patches: Array[Dictionary] = [
		{"p":Vector3(-19,0,-13),"s":Vector3(12.0,0.055,4.0),"r":0.11},
		{"p":Vector3(8,0,-15),"s":Vector3(9.0,0.050,3.0),"r":-0.08},
		{"p":Vector3(21,0,-8),"s":Vector3(11.0,0.050,3.5),"r":0.16},
		{"p":Vector3(-10,0,-4),"s":Vector3(13.0,0.050,3.0),"r":-0.13},
		{"p":Vector3(18,0,2),"s":Vector3(10.0,0.050,4.1),"r":0.08},
		{"p":Vector3(-22,0,7),"s":Vector3(9.0,0.050,3.2),"r":0.18},
		{"p":Vector3(1,0,12),"s":Vector3(14.0,0.050,3.4),"r":-0.09},
		{"p":Vector3(24,0,15),"s":Vector3(8.0,0.050,2.8),"r":0.14}
	]
	for spec: Dictionary in patches:
		var patch := _box(spec["s"],snow_patch,center+spec["p"]+Vector3(0,0.045,0),root)
		patch.rotation.y = float(spec["r"])

	# Faint interrupted football markings remain visible through snow.
	var line := _mat(Color("c7cbc3"),0.98)
	for x: float in [-24.0,-12.0,0.0,12.0,24.0]:
		_box(Vector3(7.2,0.012,0.075),line,center+Vector3(x,0.062,-17.55),root)
		_box(Vector3(7.2,0.012,0.075),line,center+Vector3(x,0.062,17.55),root)
	for z: float in [-13.0,-5.0,5.0,13.0]:
		_box(Vector3(0.075,0.012,5.0),line,center+Vector3(-30.55,0.062,z),root)
		_box(Vector3(0.075,0.012,5.0),line,center+Vector3(30.55,0.062,z),root)
	# Broken centre line + centre-circle fragments.
	for z: float in [-14.0,-7.0,0.0,7.0,14.0]:
		_box(Vector3(0.075,0.012,3.5),line,center+Vector3(0,0.062,z),root)
	for i: int in range(16):
		if i % 3 == 0:
			continue
		var a := float(i)/16.0*TAU
		var p := center+Vector3(cos(a)*4.3,0.064,sin(a)*4.3)
		var dash := _box(Vector3(1.0,0.012,0.075),line,p,root)
		dash.rotation.y = -a

	_add_goal19(root,center+Vector3(-31.7,0,0),PI/2.0)
	_add_goal19(root,center+Vector3(31.7,0,0),-PI/2.0)
	_add_fence19(root,center+Vector3(-32.8,0,-20.0),center+Vector3(32.8,0,-20.0))
	_add_fence19(root,center+Vector3(-32.8,0,20.0),center+Vector3(18.0,0,20.0))
	_add_fence19(root,center+Vector3(24.0,0,20.0),center+Vector3(32.8,0,20.0))
	for p: Vector3 in [center+Vector3(-27,0,-20.8),center+Vector3(27,0,-20.8),center+Vector3(-27,0,20.8),center+Vector3(27,0,20.8)]:
		_add_field_light19(root,p)
	_add_dugout19b(root,center+Vector3(-8,0,-21.2),0.0)
	_add_dugout19b(root,center+Vector3(8,0,-21.2),0.0)
	for p: Vector3 in [center+Vector3(-29,0,-17),center+Vector3(28,0,16),center+Vector3(-30,0,15),center+Vector3(21,0,-18)]:
		_snow_mound19(p,Vector3(3.4,0.31,1.05),0.0)
	_world_prop_count += 92

# -----------------------------------------------------------------------------
# CAMERA — CLOSER BY DEFAULT, STRONGER INTERIOR ZOOM
# -----------------------------------------------------------------------------
func _update_camera19(delta: float) -> void:
	if player == null or camera == null:
		return
	camera.projection = Camera3D.PROJECTION_ORTHOGONAL
	var p := player.global_position
	var inside := p.x > -16.6 and p.x < 16.6 and p.z > -5.7 and p.z < 5.85
	var wanted_size := minf(_ortho_size19b,10.8) if inside else _ortho_size19b
	camera.size = lerpf(camera.size,wanted_size,1.0-exp(-5.5*delta))
	var focus := p+Vector3(0,1.0,0)
	var travel := Vector3(player.velocity.x,0,player.velocity.z)
	if travel.length() > 0.2:
		focus += travel.normalized()*0.34
	var horizontal := cos(_camera_pitch)*_camera_distance
	var offset := Vector3(sin(_camera_yaw)*horizontal,sin(_camera_pitch)*_camera_distance,cos(_camera_yaw)*horizontal)
	var wanted := focus+offset
	camera.global_position = camera.global_position.lerp(wanted,1.0-exp(-7.5*delta))
	camera.look_at(focus,Vector3.UP)

# -----------------------------------------------------------------------------
# ACCEPTANCE CAPTURES — JUDGE WHAT THE PLAYER WILL ACTUALLY SEE
# -----------------------------------------------------------------------------
func _run_capture_sequence19() -> void:
	_set_school_power19(true)
	_school_powered = true
	if _cable_connected_visual != null:
		_cable_connected_visual.visible = true
	var dir := ProjectSettings.globalize_path("res://build/captures")
	DirAccess.make_dir_recursive_absolute(dir)
	camera.projection = Camera3D.PROJECTION_ORTHOGONAL

	camera.size = 17.0
	await _capture_view19("01_school_exterior.png",Vector3(24,17,26),Vector3(-1,1.45,5.0),false)

	camera.size = 30.0
	await _capture_view19("02_school_sport_rosvalla.png",Vector3(63,39,75),Vector3(18,0.7,38),false)

	# Tight classroom/corridor view. The near facade and roof are removed by
	# the same cutaway used during actual play.
	camera.size = 10.4
	await _capture_view19("03_school_interior.png",Vector3(12.5,10.5,14.0),Vector3(5.8,1.15,-0.8),true)

	print("ROSVIK_VISUAL_CAPTURE_19_READY files=3")
	get_tree().quit()
