extends "res://scripts/main_controls_spatial_cleanup_13.gd"

# Control Feel + World Presentation 14
# The player script now owns crisp screen-space movement. This layer focuses on
# presentation: no floating direction boards, attached building identity, and a
# rebuilt school-front fence instead of inherited orphan rails/posts.

var _presentation14_root: Node3D

func _ready() -> void:
	super._ready()
	_presentation14_root = Node3D.new()
	_presentation14_root.name = "WorldPresentation14"
	add_child(_presentation14_root)
	_remove_floating_direction_signs14()
	_repair_school_front_fence14()
	_replace_building_identity14()
	print("ROSVIK_CONTROL_FEEL_14_READY")
	print("ROSVIK_DIEGETIC_SIGNAGE_14_READY")
	print("ROSVIK_FLOATING_SIGN_CLEANUP_14_READY")
	print("ROSVIK_FRONT_FENCE_REBUILD_14_READY")

# -------------------------------------------------------------------------
# REMOVE WAYFINDING THAT READS LIKE DEBUG TEXT IN THE WORLD
# -------------------------------------------------------------------------
func _remove_floating_direction_signs14() -> void:
	# The old blue SPORTHALL -> sign was authored directly into WorldCoherence09.
	# Hide the pole/board/label cluster around that known location and rely on the
	# actual sporthall facade instead.
	var coherence: Node = get_node_or_null("WorldCoherence09")
	if coherence != null:
		for child: Node in coherence.get_children():
			if child is Node3D:
				var n := child as Node3D
				var p := n.global_position
				if Vector2(p.x-27.0,p.z-32.05).length() < 2.8 and p.y > 0.15:
					_hide_and_disable14(n)

	# Residential BOSTÄDER sign is useful as prototype navigation, but visually it
	# feels like a floating level marker. Remove the whole little sign root.
	var labels: Array[Label3D] = []
	_collect_labels14(self,labels)
	for label: Label3D in labels:
		var t: String = label.text.strip_edges()
		if t == "BOSTÄDER" or "SPORTHALL" in t and "→" in t:
			var parent := label.get_parent()
			if parent is Node3D:
				_hide_and_disable14(parent as Node3D)

func _hide_and_disable14(node: Node3D) -> void:
	if node is GeometryInstance3D:
		(node as GeometryInstance3D).visible = false
	if node is CollisionObject3D:
		var body := node as CollisionObject3D
		body.collision_layer = 0
		body.collision_mask = 0
	for child: Node in node.get_children():
		if child is Node3D:
			_hide_and_disable14(child as Node3D)

func _collect_labels14(node: Node, out: Array[Label3D]) -> void:
	for child: Node in node.get_children():
		if child is Label3D:
			out.append(child as Label3D)
		_collect_labels14(child,out)

# -------------------------------------------------------------------------
# SCHOOL FRONT: DELETE THE PARTIALLY MOVED LEGACY FENCE AND REBUILD ONE LINE
# -------------------------------------------------------------------------
func _repair_school_front_fence14() -> void:
	var school: Node3D = get_node_or_null("RosviksSkola") as Node3D
	if school == null:
		return
	# Pass 13 moved individual direct children of the old z=13 fence. Because rails,
	# posts and collision bodies were separate nodes, a few could visually detach.
	# Remove every low direct child in that narrow band, then author one clean fence.
	for child: Node in school.get_children():
		if not child is Node3D:
			continue
		var n := child as Node3D
		var p := n.position
		if p.z > 14.05 and p.z < 14.75 and p.x > -19.2 and p.x < 16.2 and p.y < 1.45:
			_hide_and_disable14(n)

	var fence_root := Node3D.new()
	fence_root.name = "SchoolFrontFence14"
	school.add_child(fence_root)
	_add_fence(Vector3(-18.0,0.0,15.35),Vector3(-7.2,0.0,15.35),fence_root)
	_add_fence(Vector3(-1.7,0.0,15.35),Vector3(15.0,0.0,15.35),fence_root)
	_gate_posts(Vector3(-7.2,0.0,15.35),Vector3(-1.7,0.0,15.35),fence_root)

# -------------------------------------------------------------------------
# BUILDING-ATTACHED IDENTITY: LETTERS LIVE ON FACADES, NOT IN EMPTY SPACE
# -------------------------------------------------------------------------
func _replace_building_identity14() -> void:
	var labels: Array[Label3D] = []
	_collect_labels14(self,labels)
	for label: Label3D in labels:
		var t := label.text.strip_edges()
		if t == "ROSVIKS SKOLA" or t == "NORRBOTTEN STÅL ARENA":
			label.visible = false

	var school: Node3D = get_node_or_null("RosviksSkola") as Node3D
	if school != null:
		_box(Vector3(7.2,0.62,0.09),_mat(Color("252d31"),0.84),Vector3(5.0,3.58,6.49),school)
		_facade_letters14(school,"ROSVIKS SKOLA",Vector3(2.15,3.58,6.56),0.48,[2,8],10)
		_add_sign_glow14(school,Vector3(5.0,3.25,7.05),Color("e4c89c"),0.28,5.5)

	var sport: Node3D = get_node_or_null("NorrbottenStalArena") as Node3D
	if sport != null:
		_box(Vector3(9.6,0.70,0.10),_mat(Color("263137"),0.82),Vector3(0.0,5.86,10.72),sport)
		_facade_letters14(sport,"ROSVIKS SPORTHALL",Vector3(-4.12,5.86,10.80),0.50,[5,12],12)
		_add_sign_glow14(sport,Vector3(0.0,5.45,11.12),Color("b9d1d7"),0.20,6.5)

func _facade_letters14(parent: Node3D,text_value: String,start: Vector3,spacing: float,dim_indices: Array[int],hanging_index: int) -> void:
	var cursor: float = 0.0
	var visible_index: int = 0
	for i: int in range(text_value.length()):
		var ch: String = text_value.substr(i,1)
		if ch == " ":
			cursor += spacing*0.72
			continue
		var letter := Label3D.new()
		letter.text = ch
		letter.font_size = 72
		letter.outline_size = 8
		letter.pixel_size = 0.0062
		letter.modulate = Color("e8dfcb") if not dim_indices.has(visible_index) else Color("5b5d59")
		letter.outline_modulate = Color("151b1e")
		letter.position = start+Vector3(cursor,0.0,0.0)
		if visible_index == hanging_index:
			letter.position.y -= 0.12
			letter.rotation.z = -0.12
		parent.add_child(letter)
		cursor += spacing
		visible_index += 1

func _add_sign_glow14(parent: Node3D,pos: Vector3,color: Color,energy: float,range_value: float) -> void:
	var light := OmniLight3D.new()
	light.position = pos
	light.light_color = color
	light.light_energy = energy
	light.omni_range = range_value
	light.shadow_enabled = false
	parent.add_child(light)
