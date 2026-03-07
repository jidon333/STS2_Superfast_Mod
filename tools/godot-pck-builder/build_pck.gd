extends SceneTree


func _init() -> void:
	var user_args := OS.get_cmdline_user_args()
	var spec_path := ""

	for index in range(user_args.size()):
		if user_args[index] == "--spec" and index + 1 < user_args.size():
			spec_path = user_args[index + 1]
			break

	if spec_path.is_empty():
		push_error("Missing required --spec argument.")
		quit(2)
		return

	if not FileAccess.file_exists(spec_path):
		push_error("Spec file was not found: %s" % spec_path)
		quit(3)
		return

	var spec_text := FileAccess.get_file_as_string(spec_path)
	var parsed = JSON.parse_string(spec_text)
	if typeof(parsed) != TYPE_DICTIONARY:
		push_error("Spec JSON must deserialize to a dictionary.")
		quit(4)
		return

	var output_pck = str(parsed.get("outputPckPath", ""))
	if output_pck.is_empty():
		push_error("Spec is missing outputPckPath.")
		quit(5)
		return

	var files = parsed.get("files", [])
	if typeof(files) != TYPE_ARRAY or files.is_empty():
		push_error("Spec must contain at least one file mapping.")
		quit(6)
		return

	var packer := PCKPacker.new()
	var result := packer.pck_start(output_pck)
	if result != OK:
		push_error("pck_start failed: %s" % result)
		quit(result)
		return

	for item in files:
		if typeof(item) != TYPE_DICTIONARY:
			push_error("File mapping entry must be a dictionary.")
			quit(7)
			return

		var target_path = str(item.get("targetPath", ""))
		var source_path = str(item.get("sourcePath", ""))
		if target_path.is_empty() or source_path.is_empty():
			push_error("Each file mapping requires targetPath and sourcePath.")
			quit(8)
			return

		if not FileAccess.file_exists(source_path):
			push_error("Source file was not found: %s" % source_path)
			quit(9)
			return

		result = packer.add_file(target_path, source_path)
		if result != OK:
			push_error("add_file failed for %s -> %s: %s" % [source_path, target_path, result])
			quit(result)
			return

	result = packer.flush(true)
	if result != OK:
		push_error("flush failed: %s" % result)
		quit(result)
		return

	print("PCK built successfully: %s" % output_pck)
	quit()
