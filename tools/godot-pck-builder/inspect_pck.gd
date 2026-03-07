extends SceneTree


func _init() -> void:
	var user_args := OS.get_cmdline_user_args()
	var pck_path := ""

	for index in range(user_args.size()):
		if user_args[index] == "--pck" and index + 1 < user_args.size():
			pck_path = user_args[index + 1]
			break

	if pck_path.is_empty():
		push_error("Missing required --pck argument.")
		quit(2)
		return

	var loaded := ProjectSettings.load_resource_pack(pck_path)
	print("load_resource_pack=", loaded)
	for path in [
		"res://mod_manifest.json",
		"mod_manifest.json",
		"res://manifest.json",
		"manifest.json"
	]:
		print("--- ", path, " ---")
		print("ResourceLoader.exists=", ResourceLoader.exists(path))
		print("FileAccess.file_exists=", FileAccess.file_exists(path))
		if FileAccess.file_exists(path):
			print(FileAccess.get_file_as_string(path))

	quit()
