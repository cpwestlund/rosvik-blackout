extends Node3D
const Geometry = preload("res://winter/scripts/geometry.gd")
var g = Geometry.new()
var footprint = PackedVector2Array()
var roof = Node3D.new()
var cut_walls: Array[Node3D] = []
var door_pivot = Node3D.new()
var door_shape: CollisionShape3D
var door_center = Vector3.ZERO
var door_open = false
var inside = false
var wood: Material
var plaster: Material

func build(mapped: PackedVector2Array) -> void:
	name = "EnterableHouse_1185295272"
	var center = Vector2.ZERO
	for p: Vector2 in mapped: center += p
	center /= mapped.size()
	position = Vector3(center.x,0,center.y)
	var axis = (mapped[1]-mapped[0]).normalized()
	rotation.y = atan2(axis.x,axis.y)
	for p: Vector2 in mapped:
		var v = to_local(Vector3(p.x,0,p.y))
		footprint.append(Vector2(v.x,v.z))
	wood = g.mat("9b7953",2,0.2)
	plaster = g.mat("b9b4a0",0,0.09)
	var red = g.mat("864b3c",2,0.22)
	var frame = g.mat("d4cbb3")
	# Floor follows the mapped polygon, rather than a new unrelated rectangle.
	g.slab(self,footprint,0.07,wood)
	for z: float in range(-6,7):
		g.box(self,Vector3(0,0.075,z),Vector3(9.0,0.01,0.018),g.mat("70593f"))
	for i: int in range(footprint.size()):
		var a = Vector3(footprint[i].x,0,footprint[i].y)
		var b = Vector3(footprint[(i+1)%footprint.size()].x,0,footprint[(i+1)%footprint.size()].y)
		var dir = (b-a).normalized()
		var outward = Vector3(-dir.z,0,dir.x)
		var mid = (a+b)*0.5
		if Geometry2D.is_point_in_polygon(Vector2(mid.x+outward.x,mid.z+outward.z),footprint): outward = -outward
		var section = Node3D.new()
		add_child(section)
		if outward.x < -0.7 or outward.z > 0.7: cut_walls.append(section)
		# Low base remains visible when the camera-facing upper walls are cut away.
		wall(self,a,b,0.14,0.07,wood,false)
		if outward.x > 0.7:
			door_center = a.lerp(b,0.25)
			var d1 = door_center-dir*0.85
			var d2 = door_center+dir*0.85
			wall(section,a,d1,2.9,1.45,red,true)
			wall(section,d2,b,2.9,1.45,red,true)
			wall(section,d1,d2,0.6,2.6,red,true)
			door_pivot.position = d1+Vector3.UP*0.07
			door_pivot.rotation.y = atan2(dir.x,dir.z)
			add_child(door_pivot)
			var door = g.box(door_pivot,Vector3(0,1.12,0.85),Vector3(0.12,2.24,1.65),g.mat("6c786b",2),true)
			door_shape = door.get_child(0).get_child(0)
			g.box(door_pivot,Vector3(0.08,1.04,1.4),Vector3(0.08,0.07,0.22),g.plain("b5a27c"))
			g.box(self,door_center+outward*0.7+Vector3.UP*0.04,Vector3(1.5,0.08,2.3),g.mat("7c8588"))
		else:
			wall(section,a,b,2.9,1.45,red,true)
		for t: float in [0.18,0.65]:
			var p = a.lerp(b,t)
			if outward.x > 0.7 and p.distance_to(door_center)<1.9: continue
			window(section,p+outward*0.15+Vector3.UP*1.75,outward,frame)
	add_child(roof)
	g.slab(roof,footprint,3.0,g.mat("a5b6c3",0,0.12))
	for i: int in range(footprint.size()):
		var a = Vector3(footprint[i].x,3.0,footprint[i].y)
		var b = Vector3(footprint[(i+1)%footprint.size()].x,3.0,footprint[(i+1)%footprint.size()].y)
		wall(roof,a,b,0.14,0.0,g.mat("525c60"),false)
	g.box(roof,Vector3(-2,3.35,-4),Vector3(0.65,0.7,0.65),g.mat("8a6252",1))
	# Hall at the roadside entrance; kitchen behind it, living room to the left.
	wall(self,Vector3(1.0,0,0.5),Vector3(1.0,0,2.1),1.05,0.525,plaster,true)
	wall(self,Vector3(1.0,0,3.8),Vector3(1.0,0,6.1),1.05,0.525,plaster,true)
	wall(self,Vector3(1.0,0,0.5),Vector3(2.0,0,0.5),1.05,0.525,plaster,true)
	wall(self,Vector3(3.5,0,0.5),Vector3(4.45,0,0.5),1.05,0.525,plaster,true)
	wall(self,Vector3(-4.4,0,0.5),Vector3(-1.2,0,0.5),1.05,0.525,plaster,true)
	furniture()

func wall(parent: Node3D,a: Vector3,b: Vector3,height: float,y: float,material: Material,solid: bool) -> void:
	var n = g.box(parent,(a+b)*0.5+Vector3.UP*y,Vector3(0.18,height,a.distance_to(b)),material,solid)
	n.rotation.y = atan2(b.x-a.x,b.z-a.z)

func window(parent: Node3D,p: Vector3,outward: Vector3,frame: Material) -> void:
	var n = Node3D.new()
	parent.add_child(n)
	n.position=p
	n.rotation.y=atan2(outward.x,outward.z)
	g.box(n,Vector3.ZERO,Vector3(1.35,1.15,0.12),frame)
	g.box(n,Vector3(0,0,0.08),Vector3(1.15,0.95,0.04),g.plain("75664c",0.15))
	g.box(n,Vector3(0,0,0.12),Vector3(0.05,1.0,0.03),frame)

func furniture() -> void:
	# First-pass furnishings establish scale and walkable room layout.
	g.box(self,Vector3(2.8,0.085,4.5),Vector3(2.0,0.025,2.0),g.mat("655f4d"))
	g.box(self,Vector3(3.75,0.48,5.75),Vector3(1.2,0.85,0.45),wood,true)
	for z: float in [-5.2,-4.0,-2.8]:
		g.box(self,Vector3(3.8,0.5,z),Vector3(0.75,0.85,1.12),g.mat("a3aaa1"),true)
		g.box(self,Vector3(3.8,0.96,z),Vector3(0.85,0.08,1.16),wood)
	g.box(self,Vector3(3.8,1.02,-4.0),Vector3(0.5,0.04,0.65),g.plain("657276"))
	g.box(self,Vector3(3.8,1.05,-5.2),Vector3(0.62,0.08,0.75),g.mat("333c40"))
	g.box(self,Vector3(0.0,0.085,-2.4),Vector3(3.2,0.025,3.7),g.mat("6d7569",0,0.25))
	g.box(self,Vector3(0,0.78,-2.4),Vector3(1.7,0.12,1.2),wood,true)
	for x: float in [-0.8,0.8]:
		g.box(self,Vector3(x,0.3,-1.2),Vector3(0.55,0.5,0.55),wood,true)
		g.box(self,Vector3(x,0.83,-0.95),Vector3(0.55,0.5,0.09),wood)
	g.box(self,Vector3(-2.0,0.09,3.5),Vector3(3.1,0.03,3.7),g.mat("967757",0,0.22))
	g.box(self,Vector3(-3.55,0.42,3.4),Vector3(1.0,0.7,2.5),g.mat("626e68"),true)
	g.box(self,Vector3(-4.0,0.92,3.4),Vector3(0.3,0.8,2.5),g.mat("626e68"))
	g.box(self,Vector3(-1.8,0.4,3.4),Vector3(0.85,0.65,1.45),wood,true)
	g.box(self,Vector3(-1.8,0.79,3.4),Vector3(0.17,0.22,0.17),g.plain("d9b17a",0.8))
	var lamp = g.lamp(self,Vector3(-1.8,1.1,3.4),1.3,7.5)
	lamp.shadow_enabled=true
	var kitchen_light = g.lamp(self,Vector3(0,1.4,-2.4),0.7,5.5)
	kitchen_light.shadow_enabled=true

func contains(world_point: Vector3) -> bool:
	var p = to_local(world_point)
	return p.y > -0.5 and p.y < 3.0 and Geometry2D.is_point_in_polygon(Vector2(p.x,p.z),footprint)

func doorway() -> Vector3:
	return to_global(door_center)

func set_open(value: bool) -> void:
	door_open=value
	door_shape.set_deferred("disabled",value)
	# Initial hinge angle follows the mapped wall orientation.
	var edge = footprint[3]-footprint[2]
	door_pivot.rotation.y=atan2(edge.x,edge.y)+(PI*0.5 if value else 0.0)

func update_view(world_point: Vector3) -> bool:
	inside=contains(world_point)
	roof.visible=not inside
	for wall_node: Node3D in cut_walls: wall_node.visible=not inside
	return inside
