extends RefCounted

# Placement study from the user's aerial overview, not a measured survey.
# Coordinates share the existing OSM projection. All surfaces remain walkable.
static func build(world: Node3D) -> void:
	var g = world.g
	var root = world.static_world
	var packed = g.mat("8c9fa9", 0, 0.24)
	var yard = g.mat("98aab2", 0, 0.32)
	var undisturbed = g.mat("b0bec6", 0, 0.12)
	var line = g.mat("b8c4c6", 0, 0.1)
	var wood = g.mat("79644c", 2, 0.25)
	var patches = [
		# Schoolyard between the older school and the main school.
		[[-42,-101],[11,-101],[17,-84],[16,-52],[-18,-50],[-43,-61]],
		# Cleared apron in front of both halls, connected to southern Skolgränd.
		[[-24,61],[64,61],[69,78],[56,87],[23,87],[3,89],[-25,85]],
		# Access strip between sports hall and ice arena.
		[[12,-6],[24,-6],[25,66],[12,66]],
		# School approach, narrower than the entire courtyard.
		[[-34,-46],[-18,-46],[-18,-16],[-26,-14],[-35,-23]]
	]
	for index: int in range(patches.size()):
		soft_patch(world, polygon(patches[index]), 0.008, "98aab2" if index == 0 else "8c9fa9")
	# Snow-covered grass and planting islands establish the spaces around paths.
	for points: Array in [
		[[-47,-43],[-38,-43],[-36,-26],[-42,-17],[-54,-20]],
		[[-58,-9],[-41,-9],[-39,1],[-48,7],[-62,5]],
		[[-65,22],[-42,20],[-37,44],[-50,54],[-72,48]],
		[[-39,-92],[-31,-94],[-28,-82],[-36,-75]],
		[[1,-91],[9,-90],[12,-64],[5,-60]],
		[[70,-50],[100,-48],[107,6],[78,7]]
	]: soft_patch(world, polygon(points), 0.013, "b0bec6")
	# Parking bay traces show through snow; leave the mission's van/work route open.
	for x: float in [-17,-14,-11,-8,-5,-2,1]:
		g.box(root, Vector3(x, 0.023, 82), Vector3(0.08, 0.007, 3.4), line)
	for x: float in [35,38,41,44,47,50,53,56]:
		g.box(root, Vector3(x, 0.023, 80), Vector3(0.08, 0.007, 4.3), line)
	world._car(Vector3(-12.5,0,81), PI, false)
	world._car(Vector3(48.5,0,80), PI, false)
	# Low ploughed perimeter, with the entrances left open.
	for segment: Array in [
		[[-23,85],[-3,88]],[[31,87],[56,86]],[[64,65],[68,78]],
		[[-43,-98],[-43,-72]],[[0,-101],[10,-99]]
	]:
		world._drift(v3(segment[0]), v3(segment[1]), 0.9, 0.25)
	# Tree groups inferred from visible crowns; retain clear sightlines to entrances.
	for p: Array in [
		[-40,-97],[-35,-91],[-36,-78],[-40,-62],[-28,-54],[-3,-55],[9,-62],
		[-44,-34],[-49,-24],[-49,-2],[-57,26],[-57,43],[-44,49],
		[77,-43],[81,-26],[85,-7],[82,15],[81,43],[78,70],[-19,97],[1,99]
	]:
		var point = Vector2(p[0],p[1])
		if not world._occupied(point, 2.4): world._birch(v3(p))
	# Rosvalla: an open snow-covered field south of the halls, no cleared asphalt.
	g.slab(root, polygon([[-15,105],[88,105],[88,178],[-15,178]]), 0.006, undisturbed)
	for z: float in [109,174]:
		for x: float in [29,35]:
			g.rod(root, Vector3(x,0,z), Vector3(x,2.4,z), 0.045, world.metal)
		g.rod(root, Vector3(29,2.4,z), Vector3(35,2.4,z), 0.045, world.metal)
	# A compact schoolyard play area; exact apparatus is an interpretation.
	for x: float in [-25,-19]:
		g.rod(root, Vector3(x,0,-82), Vector3(x,2.5,-83), 0.07, wood)
		g.rod(root, Vector3(x,0,-84), Vector3(x,2.5,-83), 0.07, wood)
	g.rod(root, Vector3(-25,2.5,-83), Vector3(-19,2.5,-83), 0.08, wood)
	for x: float in [-23.5,-20.5]:
		for dx: float in [-0.24,0.24]:
			g.rod(root, Vector3(x+dx,2.4,-83),Vector3(x+dx,0.5,-83),0.012,world.metal)
		g.box(root,Vector3(x,0.5,-83),Vector3(0.6,0.07,0.3),world.metal)
	world._bench(Vector3(-33,0,-67))
	world._bench(Vector3(6,0,-94))
	print("WINTER_GROUNDS_READY patches=4 contextual_parking=true field=true")

static func polygon(values: Array) -> PackedVector2Array:
	var result = PackedVector2Array()
	for p: Array in values: result.append(Vector2(p[0],p[1]))
	return result

static func v3(p: Array) -> Vector3:
	return Vector3(p[0],0,p[1])

static func soft_patch(world: Node3D, points: PackedVector2Array, height: float, color: String) -> void:
	for step: int in range(3,0,-1):
		var outlines = Geometry2D.offset_polygon(points, step * 0.55)
		var blend = Color(color).lerp(Color("a5b6c3"), float(step) / 4.0).to_html(false)
		for outline: PackedVector2Array in outlines:
			world.g.slab(world.static_world, outline, height - step * 0.0005, world.g.mat(blend,0,0.18))
	world.g.slab(world.static_world, points, height, world.g.mat(color,0,0.24))
