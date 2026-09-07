extends RefCounted

const SURFACE = preload("res://winter/shaders/surface.gdshader")
var materials: Dictionary = {}

func mat(color: String, kind: int = 0, grain: float = 0.18) -> ShaderMaterial:
	var key: = color + str(kind) + str(grain)
	if materials.has(key): return materials[key]
	var m: = ShaderMaterial.new()
	m.shader = SURFACE
	m.set_shader_parameter("base_color", Color(color))
	m.set_shader_parameter("surface_kind", kind)
	m.set_shader_parameter("grain", grain)
	materials[key] = m
	return m

func plain(color: String, emission: float = 0.0) -> StandardMaterial3D:
	var key: = color + "e" + str(emission)
	if materials.has(key): return materials[key]
	var m: = StandardMaterial3D.new()
	m.albedo_color = Color(color)
	m.roughness = 0.8
	if emission > 0.0:
		m.emission_enabled = true
		m.emission = Color(color)
		m.emission_energy_multiplier = emission
	materials[key] = m
	return m

func mesh(parent: Node3D, resource: Mesh, material: Material, pos: Vector3) -> MeshInstance3D:
	var n: = MeshInstance3D.new()
	n.mesh = resource
	n.material_override = material
	n.position = pos
	parent.add_child(n)
	return n

func box(parent: Node3D, pos: Vector3, size: Vector3, material: Material, solid: bool = false) -> MeshInstance3D:
	var m: = BoxMesh.new()
	m.size = size
	var n: = mesh(parent, m, material, pos)
	if solid:
		var body: = StaticBody3D.new()
		var shape: = CollisionShape3D.new()
		var resource: = BoxShape3D.new()
		resource.size = size
		shape.shape = resource
		n.add_child(body)
		body.add_child(shape)
	return n

func ellipsoid(parent: Node3D, pos: Vector3, size: Vector3, material: Material) -> MeshInstance3D:
	var m: = SphereMesh.new()
	m.radius = 1.0
	m.height = 2.0
	m.radial_segments = 12
	m.rings = 8
	var n: = mesh(parent, m, material, pos)
	n.scale = size
	return n

func rod(parent: Node3D, a: Vector3, b: Vector3, radius: float, material: Material, tip: float = -1.0) -> MeshInstance3D:
	var m: = CylinderMesh.new()
	m.bottom_radius = radius
	m.top_radius = radius if tip < 0.0 else tip
	m.height = a.distance_to(b)
	m.radial_segments = 8
	var n: = mesh(parent, m, material, (a + b) * 0.5)
	var direction: = (b - a).normalized()
	n.quaternion = Quaternion(Vector3.UP, direction)
	return n

func label(parent: Node3D, text: String, pos: Vector3, font_size: int = 40) -> Label3D:
	var n: = Label3D.new()
	n.text = text
	n.font_size = font_size
	n.pixel_size = 0.008
	n.outline_size = 0
	n.modulate = Color("e3e1d7")
	n.position = pos
	parent.add_child(n)
	return n

func lamp(parent: Node3D, pos: Vector3, energy: float, radius: float) -> OmniLight3D:
	var light: = OmniLight3D.new()
	light.position = pos
	light.light_color = Color("ffbf78")
	light.light_energy = energy
	light.omni_range = radius
	light.omni_attenuation = 1.5
	parent.add_child(light)
	return light

func slab(parent: Node3D, points: PackedVector2Array, height: float, material: Material) -> MeshInstance3D:
	var indices: = Geometry2D.triangulate_polygon(points)
	var st: = SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)
	for i: int in range(0, indices.size(), 3):
		for j: int in [0, 1, 2]:
			var p: = points[indices[i + j]]
			st.set_normal(Vector3.UP)
			st.add_vertex(Vector3(p.x, height, p.y))
	var n: = mesh(parent, st.commit(), material, Vector3.ZERO)
	n.material_override = material
	return n
