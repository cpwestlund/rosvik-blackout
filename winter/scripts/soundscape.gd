extends Node

var wind: = AudioStreamPlayer.new()
var machine: = AudioStreamPlayer3D.new()
var steps: Array[AudioStreamPlayer] = []
var step_index: = 0

func _ready() -> void :
	add_child(wind)
	wind.stream = synth("wind", 6.0, true)
	wind.volume_db = -23.0
	wind.play()
	add_child(machine)
	machine.position = Vector3(17, 1, 64)
	machine.stream = synth("engine", 2.0, true)
	machine.unit_size = 6.0
	machine.max_distance = 80.0
	machine.volume_db = -12.0
	for i: int in range(4):
		var s: = AudioStreamPlayer.new()
		add_child(s)
		s.stream = synth("step", 0.25, false)
		s.volume_db = -18.0
		steps.append(s)

func footstep() -> void :
	var s: = steps[step_index % steps.size()]
	s.pitch_scale = randf_range(0.88, 1.14)
	s.play()
	step_index += 1

func start_engine() -> void :
	machine.play()
	machine.pitch_scale = 0.55
	create_tween().tween_property(machine, "pitch_scale", 1.0, 2.2)

func synth(kind: String, seconds: float, looped: bool) -> AudioStreamWAV:
	var rate: = 22050
	var count: = int(seconds * rate)
	var data: = PackedByteArray()
	data.resize(count * 2)
	var rng: = RandomNumberGenerator.new()
	rng.seed = 334
	var low: = 0.0
	for i: int in range(count):
		var t: = float(i) / rate
		var white: = rng.randf_range(-1.0, 1.0)
		low = lerpf(low, white, 0.045)
		var value: = low
		if kind == "wind": value = low * (0.6 + 0.2 * sin(TAU * t / seconds))
		elif kind == "engine": value = sin(TAU * 50.0 * t) * 0.25 + sin(TAU * 100.0 * t) * 0.09 + low * 0.7
		else: value = (white * 0.45 + low) * pow(maxf(0.0, 1.0 - t / seconds), 3.0) * minf(t * 100.0, 1.0)
		data.encode_s16(i * 2, int(clampf(value, -1.0, 1.0) * 30000))
	var stream: = AudioStreamWAV.new()
	stream.format = AudioStreamWAV.FORMAT_16_BITS
	stream.mix_rate = rate
	stream.data = data
	if looped:
		stream.loop_mode = AudioStreamWAV.LOOP_FORWARD
		stream.loop_end = count
	return stream
