extends PanelContainer

signal closed
signal transferred
var loot: RefCounted
var container_id = ""
var pack_list = ItemList.new()
var box_list = ItemList.new()
var load_label = Label.new()
var description = Label.new()
var box_label = Label.new()
var take = Button.new()
var put = Button.new()

func _ready() -> void:
	position = Vector2(290, 180)
	size = Vector2(1020, 520)
	var style = StyleBoxFlat.new()
	style.bg_color = Color("172832")
	style.border_color = Color("657775")
	style.set_border_width_all(1)
	style.set_content_margin_all(24)
	add_theme_stylebox_override("panel", style)
	var column = VBoxContainer.new()
	column.add_theme_constant_override("separation", 12)
	add_child(column)
	var title = Label.new()
	title.text = "DET DU BÄR MED DIG"
	title.add_theme_font_size_override("font_size", 22)
	column.add_child(title)
	column.add_child(load_label)
	var row = HBoxContainer.new()
	row.add_theme_constant_override("separation", 20)
	column.add_child(row)
	for side: int in range(2):
		var part = VBoxContainer.new()
		part.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		row.add_child(part)
		var label = Label.new() if side == 0 else box_label
		if side == 0: label.text = "RYGGSÄCK"
		part.add_child(label)
		var list = pack_list if side == 0 else box_list
		list.custom_minimum_size = Vector2(460, 250)
		list.add_theme_font_size_override("font_size", 15)
		part.add_child(list)
		var button = put if side == 0 else take
		button.text = "Lägg här · en i taget" if side == 0 else "Ta · en i taget"
		part.add_child(button)
		button.pressed.connect(_transfer.bind(side == 1))
		list.item_selected.connect(_describe.bind(side == 0))
	description.custom_minimum_size = Vector2(960, 62)
	description.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	column.add_child(description)
	var close = Button.new()
	close.text = "Tillbaka · I / Esc"
	close.pressed.connect(func(): closed.emit())
	column.add_child(close)
	hide()

func open_box(model: RefCounted, id: String, title: String) -> void:
	loot = model
	container_id = id
	box_label.text = title if id != "" else "INGEN BEHÅLLARE INOM RÄCKHÅLL"
	description.text = "Välj ett föremål för att läsa mer. Fynden ligger kvar där du lämnar dem."
	_refresh()
	show()

func _refresh() -> void:
	var load: Vector2 = loot.totals(loot.pack)
	load_label.text = "%.2f / 18 kg     ·     %.1f / 28 liter" % [load.x, load.y]
	for list: ItemList in [pack_list, box_list]: list.clear()
	_fill(pack_list, loot.pack)
	if container_id != "": _fill(box_list, loot.containers[container_id])
	put.disabled = container_id == "" or loot.pack.is_empty()
	take.disabled = container_id == "" or loot.containers.get(container_id, []).is_empty()

func _fill(list: ItemList, items: Array) -> void:
	for item: Dictionary in items:
		list.add_item("%s ×%d   ·   %d%% skick" % [loot.catalog[item.id].name, item.quantity, item.condition])

func _describe(index: int, from_pack: bool) -> void:
	var source: Array = loot.pack if from_pack else loot.containers[container_id]
	var definition: Dictionary = loot.catalog[source[index].id]
	description.text = "%s\n%.3f kg · %.2f liter per styck" % [definition.description, definition.kg, definition.litres]

func _transfer(taking: bool) -> void:
	var selected = (box_list if taking else pack_list).get_selected_items()
	if selected.is_empty():
		description.text = "Välj först ett föremål i listan."
		return
	var index: int = selected[0]
	var result: String = loot.transfer(container_id, index, taking)
	_refresh()
	description.text = result if result != "" else "Föremålet flyttades."
	var list = box_list if taking else pack_list
	if list.item_count > 0: list.select(mini(index, list.item_count - 1))
	if result == "": transferred.emit()
