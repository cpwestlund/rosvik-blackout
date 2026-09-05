extends RefCounted

const SAVE_PATH = "user://winter_save_v1.json"

static func write(data: Dictionary, path: String = SAVE_PATH) -> bool:
	var file = FileAccess.open(path + ".tmp", FileAccess.WRITE)
	if file == null: return false
	file.store_string(JSON.stringify(data))
	file.flush()
	var error = file.get_error()
	file.close()
	if error != OK: return false
	var full = ProjectSettings.globalize_path(path)
	if FileAccess.file_exists(path):
		if DirAccess.copy_absolute(full, full + ".bak") != OK: return false
	return DirAccess.rename_absolute(full + ".tmp", full) == OK

static func read_file(path: String = SAVE_PATH) -> Dictionary:
	if not FileAccess.file_exists(path): return {}
	var parser = JSON.new()
	if parser.parse(FileAccess.get_file_as_string(path)) != OK: return {}
	var data = parser.data
	if not data is Dictionary or data.get("version") != 1: return {}
	var stage = data.get("stage")
	if not (stage is int or stage is float): return {}
	if not is_finite(float(stage)) or int(stage) != stage or stage < 0 or stage > 5: return {}
	var p = data.get("position")
	if not p is Array or p.size() != 3: return {}
	for v: Variant in p:
		if not (v is int or v is float) or not is_finite(float(v)): return {}
	if absf(p[0]) > 179 or absf(p[2]) > 189 or p[1] < -2 or p[1] > 20: return {}
	if not data.get("loot") is Dictionary: return {}
	return data
