This Utility is designed to check the layout of Visual Studio package files. It can be used to ensure that the package files are correctly hash for Visual Studio to install and run properly. 

Usage: args [option]

Args:
	--layoutpath : The path to the layout folder containing the package files.
	--catalog : The path to the Visual Stdio packages catalog file.
	--download : Automatically download the package files if the file is not found.
	--packageid : Specify the package ID to check. If not specified, all packages in the catalog will be checked.
	--nodynamicendpoint : The default, the utility will skip checking if the package is a dynamic endpoint. Use this option to check it is matched by catalog file.
	--fix : The utility will re-download and replace the package files if they are not found or do not match the hash.
	--lang : Specify the language to check. If not specified en-us, zh-tw,ja-jp as default.


Note: Visual Studio 2017 havn't offical --verify args for layout, so this utility is used to check the layout files. It tested for Visual Studio 2017, 2019 and 2022 (up to 17.13.0).
	On Visual Studio 2017, some package files needs additional command to finlish the layout, this utility will not check the package files and execute the additional layout command.