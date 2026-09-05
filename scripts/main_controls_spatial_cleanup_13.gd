extends "res://scripts/main_camera_residential_life_12.gd"

# Controls + Spatial Cleanup 13
# - WASD is camera-relative after orbiting
# - camera lead and occlusion use hysteresis to stop direction-change shimmer
# - inherited roadside props are rebuilt in safe parking/service zones
# - schoolyard fence, flag and dumpster are pulled fully clear of the carriageway
# - old milestone HUD layers are removed from presentation

var _camera13_lead: Vector3 = Vector3.ZERO
var _occ13_state: Dictionary = {}

func _ready() -> void:
	super._ready()
	_repair_school_street_intrusions13()
	_cleanup_legacy_hud13()
	print("ROSVIK_CAMERA_RELATIVE_MOVEMENT_13_READY")
	print("ROSVIK_DIRECTION_STABILITY_13_READY")
	print("ROSVIK_ROAD_PROP_CLEARANCE_13_READY")
	print("ROSVIK_PRESENTATION_CLEANUP_13_READY")

# -------------------------------------------------------------------------
# REBUILD THE OLD GENERIC PROP PASS WITH ROAD-SAFE LOCATIONS
# -------------------------------------------------------------------------
func _build_props() -> void:
	# Cars now live in authored parking/forecourt areas instead of arbitrary points.
	_add_car(Vector3(18.2,0.0,21.8),Color("59676d"),0.02,true)
	_add_car(Vector3(49.0,0.0,44.2),Color("6d6259"),0.0,false)
	_add_car(Vector3(57.0,0.0,44.2),Color("465a65"),0.0,false)
	_add_car(Vector3(64.5,0.0,44.2),Color("735e54"),0.0,false)
	_add_car(Vector3(48.0,0.0,85.2),Color("5b6066"),0.0,false)
	_add_car(Vector3(59.0,0.0,85.2),Color("765a50"),0.0,false)

	# Lighting sits outside carriageways. Fewer, better placed poles read more clearly.
	var lamp_positions: Array[Vector3] = [
		Vector3(-29.0,0.0,-24.0),Vector3(-8.0,0.0,-24.0),Vector3(18.0,0.0,-24.0),
		Vector3(-34.0,0.0,-52.0),Vector3(-34.0,0.0,1.0),Vector3(-34.0,0.0,30.0),
		Vector3(24.0,0.0,15.0),Vector3(35.0,0.0,31.8),Vector3(71.5,0.0,31.8),
		Vector3(74.0,0.0,58.0),Vector3(74.0,0.0,78.0)
	]
	for p: Vector3 in lamp_positions:
		_add_lamp(p)

	# Ploughed snow is kept well outside road rectangles and crossings.
	for p: Vector3 in [
		Vector3(-25.0,0.0,17.0),Vector3(27.0,0.0,17.0),
		Vector3(37.0,0.0,50.5),Vector3(68.0,0.0,50.5),
		Vector3(38.0,0.0,92.5),Vector3(72.0,0.0,92.5),
		Vector3(-48.0,0.0,18.0)
	]:
		_add_snowbank(p)

# -------------------------------------------------------------------------
# CAMERA: SMOOTH LEAD + OCCLUSION HYSTERESIS
# -------------------------------------------------------------------------
func _update_camera(delta: float) -> void:
	if player == null or camera == null:
		return
	var travel := Vector3(player.velocity.x,0.0,player.velocity.z)
	var wanted_lead := Vector3.ZERO
	if travel.length() > 0.20:
		wanted_lead = travel.normalized() * minf(1.25,travel.length()*0.22)
	_camera13_lead = _camera13_lead.lerp(wanted_lead,1.0-exp(-3.1*delta))

	var focus := player.global_position + _camera13_lead + Vector3(0.0,1.15,0.0)
	var horizontal := cos(_camera12_pitch) * _camera12_distance
	var offset := Vector3(cos(_camera12_yaw)*horizontal,sin(_camera12_pitch)*_camera12_distance,sin(_camera12_yaw)*horizontal)
	target_camera_pos = focus + offset
	camera.global_position = camera.global_position.lerp(target_camera_pos,1.0-exp(-5.0*delta))
	camera.look_at(focus,Vector3.UP)

func _update_occlusion12() -> void:
	if player == null or camera == null:
		return
	var cam2 := Vector2(camera.global_position.x,camera.global_position.z)
	var player2 := Vector2(player.global_position.x,player.global_position.z)
	var total_distance := cam2.distance_to(player2)
	for mesh: GeometryInstance3D in _camera12_occluders:
		if not is_instance_valid(mesh):
			continue
		var id := mesh.get_instance_id()
		var p := Vector2(mesh.global_position.x,mesh.global_position.z)
		var near_line := _distance_to_segment12(p,cam2,player2)
		var between := p.distance_to(cam2) < total_distance and p.distance_to(player2) < 17.0
		var faded: bool = bool(_occ13_state.get(id,false))
		if faded:
			# Keep it faded until it is clearly out of the sight corridor.
			faded = between and near_line < 6.4
		else:
			# Enter only through a tighter threshold. The gap is intentional hysteresis.
			faded = between and near_line < 4.4
		_occ13_state[id] = faded
		var target_transparency := 0.58 if faded else 0.0
		mesh.transparency = lerpf(mesh.transparency,target_transparency,0.10)

# -------------------------------------------------------------------------
# SCHOOL STREET REPAIR
# -------------------------------------------------------------------------
func _repair_school_street_intrusions13() -> void:
	var school: Node3D = get_node_or_null("RosviksSkola") as Node3D
	if school == null:
		return
	var moved: int = 0
	for child: Node in school.get_children():
		if not child is Node3D:
			continue
		var n := child as Node3D
		var p := n.position
		# Front fence was only 30 cm outside the road edge. Pull the whole front line
		# into the schoolyard, including its collision bodies and gate posts.
		if absf(p.z-13.0) < 0.16 and p.x > -19.0 and p.x < 16.0:
			n.position.z += 1.35
			moved += 1
			continue
		# Flag pole/flag were authored at z=10.3: literally in the school road.
		if absf(p.z-10.3) < 0.45 and p.x > 10.3 and p.x < 13.2:
			n.position.z += 5.4
			moved += 1
			continue
		# Dumpster and lid were also in the carriageway at z=9.0.
		if absf(p.z-9.0) < 0.55 and p.x < -14.8 and p.x > -17.3:
			n.position.z += 7.2
			moved += 1
	print("ROSVIK_SCHOOL_STREET_REPAIR_13 moved=",moved)

# -------------------------------------------------------------------------
# PRESENTATION CLEANUP: ONE CURRENT HUD, NOT THE HISTORY OF EVERY MILESTONE
# -------------------------------------------------------------------------
func _cleanup_legacy_hud13() -> void:
	for child: Node in get_children():
		if child is CanvasLayer:
			_cleanup_canvas_node13(child)

func _cleanup_canvas_node13(node: Node) -> void:
	for child: Node in node.get_children():
		if child is Label:
			var label := child as Label
			var t := label.text
			if t.begins_with("WASD / pilar"):
				label.visible = false
			elif "ARCHITECTURAL COHESION" in t or "WORLD INTEGRITY" in t or "SPATIAL SANITY" in t or "PHYSICAL ROSVIK" in t or "NATIVE GODOT BUILD" in t:
				label.visible = false
		if child is ColorRect:
			var panel := child as ColorRect
			if _panel_is_legacy13(panel):
				panel.visible = false
		_cleanup_canvas_node13(child)

func _panel_is_legacy13(panel: Node) -> bool:
	for descendant: Node in panel.get_children():
		if descendant is Label:
			var t := (descendant as Label).text
			if "ARCHITECTURAL COHESION" in t or "WORLD INTEGRITY" in t or "SPATIAL SANITY" in t or "PHYSICAL ROSVIK" in t or "NATIVE GODOT BUILD" in t:
				return true
	return false
