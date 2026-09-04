extends "res://scripts/main_world_believability_04.gd"

var _coherence_root: Node3D
var _road_marking_mat: StandardMaterial3D
var _sidewalk_mat: StandardMaterial3D
var _reflector_mat: StandardMaterial3D

func _ready() -> void:
	super._ready()
	_add_world_coherence_details()
	print("ROSVIK_WORLD_COHERENCE_05_READY")
	print("ROSVIK_ROAD_NETWORK_READY")
	print("ROSVIK_FLICKER_CLEANUP_READY")
	print("ROSVIK_EXTERIOR_LINKAGE_READY")

# -------------------------------------------------------------------------
# COMPLETE EXTERIOR CIRCULATION
# -------------------------------------------------------------------------
func _build_roads() -> void:
	# Rebuild the road layer as one coherent local network. Road surface, shoulder
	# and markings use deliberately separated heights so coplanar surfaces do not
	# shimmer against each other.
	_road_marking_mat = _mat(Color("c7c4b5"),0.94)
	_sidewalk_mat = _textured_mat(Color("7b8384"),0.98,0.0,"noise",96,0.018)
	_reflector_mat = _mat(Color("d7d3c0"),0.76)

	# Main through-roads retained from the old blockout.
	_road_segment(Vector3(-105.0,0.0,-30.0),Vector3(115.0,0.0,-30.0),7.2,true)
	_road_segment(Vector3(-42.0,0.0,-92.0),Vector3(-42.0,0.0,82.0),6.0,true)

	# School / civic spine.
	_road_segment(Vector3(-42.0,0.0,10.0),Vector3(33.0,0.0,10.0),5.4,true)
	_road_segment(Vector3(33.0,0.0,10.0),Vector3(51.0,0.0,17.0),5.2,true)
	_road_segment(Vector3(51.0,0.0,17.0),Vector3(66.0,0.0,17.0),5.2,true)

	# Rosvalla / sporthall access and connection onwards to HALA Hallen.
	_road_segment(Vector3(33.0,0.0,10.0),Vector3(33.0,0.0,35.0),4.6,false)
	_road_segment(Vector3(33.0,0.0,35.0),Vector3(66.0,0.0,35.0),4.8,false)
	_road_segment(Vector3(66.0,0.0,10.0),Vector3(66.0,0.0,80.0),5.0,true)
	_road_segment(Vector3(66.0,0.0,63.0),Vector3(51.0,0.0,63.0),4.6,false)
	_road_segment(Vector3(51.0,0.0,63.0),Vector3(51.0,0.0,80.0),4.6,false)

	# Parking / turning areas are one layer above snow, one layer below road paint.
	_parking_patch(Vector3(50.0,0.0,39.5),Vector3(27.0,0.055,10.5),0.0,8)
	_parking_patch(Vector3(55.0,0.0,82.0),Vector3(31.0,0.055,12.0),0.0,10)
	_parking_patch(Vector3(21.0,0.0,18.0),Vector3(10.5,0.055,14.0),0.0,4)

	# Pedestrian links: school gate -> street, school -> sporthall and sporthall -> Rosvalla.
	_walk_strip(Vector3(-4.45,0.0,8.8),Vector3(-4.45,0.0,13.0),2.2)
	_walk_strip(Vector3(15.0,0.0,21.0),Vector3(31.0,0.0,21.0),1.8)
	_walk_strip(Vector3(31.0,0.0,21.0),Vector3(45.0,0.0,31.0),1.8)
	_walk_strip(Vector3(45.0,0.0,31.0),Vector3(50.0,0.0,32.0),1.8)
	_walk_strip(Vector3(53.0,0.0,35.0),Vector3(53.0,0.0,45.0),1.6)

	# Small crossings communicate where the player is expected to cross roads.
	_crossing(Vector3(-4.45,0.0,10.0),Vector3(1.0,0.0,0.0),5.4)
	_crossing(Vector3(33.0,0.0,21.0),Vector3(0.0,0.0,1.0),4.6)
	_crossing(Vector3(66.0,0.0,63.0),Vector3(1.0,0.0,0.0),5.0)

func _road_segment(a: Vector3, b: Vector3, width: float, center_line: bool) -> void:
	var d: Vector3 = b-a
	var length: float = d.length()
	if length < 0.1:
		return
	var yaw: float = -atan2(d.z,d.x)
	var mid: Vector3 = (a+b)*0.5

	# Dirty ploughed shoulder sits slightly below the asphalt.
	var shoulder: MeshInstance3D = _box(Vector3(length+0.5,0.040,width+1.30),slush_mat,mid+Vector3(0.0,0.020,0.0),self)
	shoulder.rotation.y = yaw
	var road: MeshInstance3D = _box(Vector3(length,0.060,width),asphalt_mat,mid+Vector3(0.0,0.052,0.0),self)
	road.rotation.y = yaw

	if center_line and width >= 5.0:
		var count: int = maxi(1,int(length/7.5))
		for i: int in range(count):
			var t: float = (float(i)+0.5)/float(count)
			var p: Vector3 = a.lerp(b,t)+Vector3(0.0,0.088,0.0)
			var dash: MeshInstance3D = _box(Vector3(2.0,0.012,0.10),_road_marking_mat,p,self)
			dash.rotation.y = yaw

	# Sparse edge reflectors make long roads legible in the darker world.
	var reflector_count: int = int(length/13.0)
	if reflector_count > 0:
		var perpendicular: Vector3 = Vector3(-d.z,0.0,d.x).normalized()
		for i: int in range(reflector_count):
			var t: float = (float(i)+0.5)/float(reflector_count)
			var base: Vector3 = a.lerp(b,t)
			for side: float in [-1.0,1.0]:
				var rp: Vector3 = base+perpendicular*(width*0.5+0.55)*side
				_box(Vector3(0.055,0.72,0.055),dark_mat,rp+Vector3(0.0,0.36,0.0),self)
				_box(Vector3(0.075,0.12,0.075),_reflector_mat,rp+Vector3(0.0,0.63,0.0),self)

func _walk_strip(a: Vector3, b: Vector3, width: float) -> void:
	var d: Vector3 = b-a
	var length: float = d.length()
	if length < 0.1:
		return
	var path: MeshInstance3D = _box(Vector3(length,0.042,width),_sidewalk_mat,(a+b)*0.5+Vector3(0.0,0.076,0.0),self)
	path.rotation.y = -atan2(d.z,d.x)

func _parking_patch(pos: Vector3, size: Vector3, yaw: float, spaces: int) -> void:
	var patch: MeshInstance3D = _box(size,asphalt_mat,pos+Vector3(0.0,0.050,0.0),self)
	patch.rotation.y = yaw
	if spaces <= 0:
		return
	var spacing: float = (size.x-2.0)/float(spaces)
	for i: int in range(spaces+1):
		var x: float = -size.x*0.5+1.0+float(i)*spacing
		var line: MeshInstance3D = _box(Vector3(0.075,0.012,size.z*0.34),_road_marking_mat,pos+Vector3(x,0.089,size.z*0.23),self)
		line.rotation.y = yaw

func _crossing(center: Vector3, along: Vector3, road_width: float) -> void:
	var a: Vector3 = along.normalized()
	var perpendicular: Vector3 = Vector3(-a.z,0.0,a.x)
	for i: int in range(5):
		var offset: float = -1.2+float(i)*0.60
		var p: Vector3 = center+a*offset+Vector3(0.0,0.093,0.0)
		var stripe: MeshInstance3D = _box(Vector3(0.34,0.012,road_width*0.70),_road_marking_mat,p,self)
		stripe.rotation.y = -atan2(perpendicular.z,perpendicular.x)

# -------------------------------------------------------------------------
# SUBTLER ELECTRICAL FLICKER
# -------------------------------------------------------------------------
func _update_flicker() -> void:
	if _flicker_light == null:
		return
	# Previous pass intentionally exaggerated the blackout tube. Keep the cue, but
	# make it a short irregular dip rather than an obvious constant strobe.
	var phase: float = fmod(_mood_clock,8.7)
	if phase < 0.045 or (phase > 5.82 and phase < 5.88):
		_flicker_light.light_energy = 0.035
	elif phase < 0.12:
		_flicker_light.light_energy = 0.12
	else:
		_flicker_light.light_energy = 0.22

# -------------------------------------------------------------------------
# EXTERIOR COHERENCE / ROSVIK CONTEXT
# -------------------------------------------------------------------------
func _add_world_coherence_details() -> void:
	_coherence_root = Node3D.new()
	_coherence_root.name = "WorldCoherence05"
	add_child(_coherence_root)

	# A municipal-style direction sign at the civic junction.
	var sign_blue: StandardMaterial3D = _mat(Color("315e79"),0.78,0.05)
	_solid_cylinder(0.045,2.25,metal_mat,Vector3(27.5,1.125,12.3),_coherence_root)
	_box(Vector3(2.9,0.52,0.08),sign_blue,Vector3(27.5,2.0,12.3),_coherence_root)
	_label3d("SPORTHALL  →",Vector3(27.5,2.0,12.38),_coherence_root,13)

	# Bus-stop-like shelter / waiting point near the school road. It reads as
	# everyday civic infrastructure without pretending to reproduce a private site.
	var shelter: Node3D = Node3D.new()
	shelter.position = Vector3(-19.5,0.0,8.2)
	_coherence_root.add_child(shelter)
	_solid_box(Vector3(0.08,2.15,0.08),metal_mat,Vector3(-1.15,1.075,-0.65),shelter)
	_solid_box(Vector3(0.08,2.15,0.08),metal_mat,Vector3(1.15,1.075,-0.65),shelter)
	_solid_box(Vector3(0.08,2.15,0.08),metal_mat,Vector3(-1.15,1.075,0.65),shelter)
	_solid_box(Vector3(0.08,2.15,0.08),metal_mat,Vector3(1.15,1.075,0.65),shelter)
	_box(Vector3(2.6,0.10,1.65),roof_mat,Vector3(0.0,2.18,0.0),shelter)
	_box(Vector3(2.35,1.65,0.055),glass_mat,Vector3(0.0,1.05,-0.68),shelter)
	_box(Vector3(1.65,0.10,0.38),wood_mat,Vector3(0.0,0.47,0.20),shelter)

	# A few parked/abandoned cars along the now-connected civic spine.
	_add_car(Vector3(23.0,0.0,17.0),Color("6e7c83"),0.08,false)
	_add_car(Vector3(44.0,0.0,39.0),Color("755f55"),-0.05,true)
	_add_car(Vector3(61.0,0.0,81.0),Color("4e6670"),PI,false)

	# Ploughed banks are visual only and placed along parking edges rather than in
	# pedestrian circulation. This keeps the world wintery without concrete-snow.
	for p: Vector3 in [Vector3(37.0,0.0,45.0),Vector3(63.0,0.0,45.0),Vector3(38.0,0.0,88.0),Vector3(71.0,0.0,88.0)]:
		_add_snowbank(p)

	# Tire marks on the sporthall / icehall approaches, clearly lifted above the
	# asphalt to avoid decal flicker.
	var track_mat: StandardMaterial3D = _textured_mat(Color("252b2e"),0.99,0.0,"asphalt",64,0.025)
	for xoff: float in [-0.70,0.70]:
		var track_a: MeshInstance3D = _box(Vector3(0.14,0.010,20.0),track_mat,Vector3(66.0+xoff,0.096,51.0),_coherence_root)
		track_a.rotation.y = 0.0
		var track_b: MeshInstance3D = _box(Vector3(0.14,0.010,13.0),track_mat,Vector3(51.0+xoff,0.096,72.5),_coherence_root)
		track_b.rotation.y = 0.0
