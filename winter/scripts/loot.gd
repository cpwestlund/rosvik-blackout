extends RefCounted

const CATALOG_PATH = "res://winter/data/items.json"
const MAX_KG = 18.0
const MAX_L = 28.0
var catalog: Dictionary = {}
var containers: Dictionary = {}
var pack: Array = []

func _init() -> void:
	var data = JSON.parse_string(FileAccess.get_file_as_string(CATALOG_PATH))
	for item: Dictionary in data:
		catalog[item.id] = item
	reset()

func reset() -> void:
	pack = []
	containers = {
		"van": [stack("work_gloves", 1, 74), stack("duct_tape", 2, 90), stack("screwdriver", 1, 67), stack("pliers", 1, 80), stack("usb_charger", 1, 43), stack("usb_cable", 2, 65), stack("water", 2, 100), stack("crispbread", 1, 100), stack("jumper_cables", 1, 70), stack("headlamp", 1, 85), stack("aa_battery", 4, 62), stack("tow_rope", 1, 72), stack("oil", 1, 80), stack("socket_set", 1, 74)],
		"refuge": [stack("blanket", 2, 80), stack("matches", 2, 90), stack("thermos", 1, 78), stack("first_aid", 1, 91)]
	}

func stack(id: String, quantity: int, condition: int) -> Dictionary:
	return {"id": id, "quantity": quantity, "condition": condition}

func totals(items: Array) -> Vector2:
	var result = Vector2.ZERO
	for item: Dictionary in items:
		result += Vector2(catalog[item.id].kg, catalog[item.id].litres) * item.quantity
	return result

func transfer(container: String, index: int, take: bool) -> String:
	if not containers.has(container): return "Ingen behållare inom räckhåll."
	var source: Array = containers[container] if take else pack
	var target: Array = pack if take else containers[container]
	if index < 0 or index >= source.size(): return "Välj ett föremål."
	var item: Dictionary = source[index]
	var definition: Dictionary = catalog[item.id]
	if take:
		if not definition.portable: return "Det här kräver annan transport."
		var load = totals(pack) + Vector2(definition.kg, definition.litres)
		if load.x > MAX_KG + 0.0001: return "För tungt för ryggsäcken."
		if load.y > MAX_L + 0.0001: return "Det får inte plats i ryggsäcken."
	var merged = false
	for existing: Dictionary in target:
		if existing.id == item.id and existing.condition == item.condition:
			existing.quantity += 1
			merged = true
			break
	if not merged: target.append(stack(item.id, 1, item.condition))
	item.quantity -= 1
	if item.quantity == 0: source.remove_at(index)
	return ""

func snapshot() -> Dictionary:
	return {"pack": pack.duplicate(true), "containers": containers.duplicate(true)}

func valid_stack_list(value: Variant) -> bool:
	if not value is Array: return false
	for item: Variant in value:
		if not item is Dictionary: return false
		if not catalog.has(item.get("id", "")): return false
		for key: String in ["quantity", "condition"]:
			if not (item.get(key) is int or item.get(key) is float): return false
			if not is_finite(float(item[key])) or int(item[key]) != item[key]: return false
		if item.quantity < 1 or item.quantity > 10000: return false
		if item.condition < 0 or item.condition > 100: return false
	return true

func restore(data: Dictionary) -> bool:
	if not valid_stack_list(data.get("pack")): return false
	var boxes = data.get("containers")
	if not boxes is Dictionary or boxes.size() != containers.size(): return false
	for key: String in containers:
		if not boxes.has(key) or not valid_stack_list(boxes[key]): return false
	for item: Dictionary in data.pack:
		if not catalog[item.id].portable: return false
	var load = totals(data.pack)
	if load.x > MAX_KG + 0.001 or load.y > MAX_L + 0.001: return false
	pack = data.pack.duplicate(true)
	containers = boxes.duplicate(true)
	return true
