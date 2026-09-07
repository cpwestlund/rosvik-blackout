extends SceneTree
const Loot = preload("res://winter/scripts/loot.gd")
const SaveGame = preload("res://winter/scripts/save_game.gd")

func _initialize() -> void:
	var loot = Loot.new()
	assert(loot.catalog.size() == 56)
	var legacy = {"pack": [], "containers": {"van": [], "refuge": []}}
	assert(loot.restore(legacy))
	assert(loot.containers.van.is_empty() and not loot.containers.house_kitchen.is_empty())
	loot.containers.house_kitchen.clear()
	var migrated = loot.snapshot()
	var restored = Loot.new()
	assert(restored.restore(migrated) and restored.containers.house_kitchen.is_empty())
	var broken = migrated.duplicate(true)
	broken.containers.erase("house_tools")
	assert(not restored.restore(broken) and restored.snapshot() == migrated)
	loot.reset()
	var original = loot.snapshot()
	assert(loot.transfer("van", 0, true) == "")
	assert(loot.pack.size() == 1 and loot.pack[0].id == "work_gloves")
	assert(loot.transfer("van", 0, false) == "")
	assert(loot.pack.is_empty())
	var gloves = 0
	for item: Dictionary in loot.containers.van:
		if item.id == "work_gloves": gloves += item.quantity
	assert(gloves == 1)
	loot.containers.van = [loot.stack("lawn_mower", 1, 80)]
	assert(loot.transfer("van", 0, true) != "")
	assert(loot.containers.van[0].quantity == 1 and loot.pack.is_empty())
	loot.pack = [loot.stack("socket_set", 4, 80)]
	loot.containers.van = [loot.stack("socket_set", 1, 80)]
	assert(loot.transfer("van", 0, true) == "För tungt för ryggsäcken.")
	loot.pack = [loot.stack("blanket", 4, 80)]
	loot.containers.van = [loot.stack("usb_cable", 1, 80)]
	assert(loot.transfer("van", 0, true) == "Det får inte plats i ryggsäcken.")
	assert(loot.restore(original))
	assert(loot.transfer("van", 0, true) == "")
	var state = {"version": 1, "stage": 4, "position": [-24.0, 0.0, -24.0], "loot": loot.snapshot()}
	var path = "user://winter_loot_automated_test.json"
	assert(SaveGame.write(state, path))
	var loaded = SaveGame.read_file(path)
	var second = Loot.new()
	assert(second.restore(loaded.loot))
	assert(second.pack[0].id == "work_gloves")
	assert(second.containers.van.size() == original.containers.van.size() - 1)
	assert(loaded.stage == 4)
	var before = second.snapshot()
	assert(not second.restore({"pack": [{"id": "invented"}], "containers": {}}))
	assert(second.snapshot() == before)
	assert(SaveGame.write(state, path))
	var file = FileAccess.open(path, FileAccess.WRITE)
	file.store_string("{truncated")
	file.close()
	assert(SaveGame.read_file(path).is_empty())
	assert(SaveGame.read_file(path + ".bak").stage == 4)
	for suffix: String in ["", ".bak", ".tmp"]:
		DirAccess.remove_absolute(ProjectSettings.globalize_path(path + suffix))
	print("WINTER_LOOT_OK transfers=true capacity=true save_roundtrip=true corrupt_backup=true")
	quit()
