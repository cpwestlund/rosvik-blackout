extends RefCounted
const ATLAS = preload("res://winter/assets/loot_atlas.webp")
const IDS = ["work_gloves","duct_tape","screwdriver","pliers","usb_charger","usb_cable","water","crispbread","jumper_cables","headlamp","aa_battery","tow_rope","oil","socket_set","blanket","matches","thermos","first_aid"]
var cache: Dictionary = {}
func icon(id: String) -> Texture2D:
	if cache.has(id): return cache[id]
	var index = IDS.find(id)
	if index < 0: return null
	var result = AtlasTexture.new()
	result.atlas = ATLAS
	var cell = Vector2(ATLAS.get_width() / 6.0, ATLAS.get_height() / 3.0)
	result.region = Rect2(Vector2(index % 6, index / 6) * cell, cell)
	result.filter_clip = true
	cache[id] = result
	return result
