"""Optimize a user-provided rigged GLB for the 720p WebGL build.

Run with:
blender.exe --background --python Tools/optimize_piquero.py -- <source.glb> <output.glb> [target_triangles]
"""

from __future__ import annotations

import json
import sys
from pathlib import Path

import bpy


DEFAULT_TARGET_TRIANGLES = 24_000
MAX_TEXTURE_SIZE = 1_024


def triangle_count(mesh_object: bpy.types.Object) -> int:
    mesh = mesh_object.data
    mesh.calc_loop_triangles()
    return len(mesh.loop_triangles)


def disable_emission(material: bpy.types.Material) -> None:
    if not material.use_nodes or material.node_tree is None:
        return

    for node in material.node_tree.nodes:
        if node.type != "BSDF_PRINCIPLED":
            continue

        emission_color = node.inputs.get("Emission Color") or node.inputs.get("Emission")
        emission_strength = node.inputs.get("Emission Strength")
        if emission_color is not None:
            for link in list(emission_color.links):
                material.node_tree.links.remove(link)
            emission_color.default_value = (0.0, 0.0, 0.0, 1.0)
        if emission_strength is not None:
            emission_strength.default_value = 0.0


def main() -> None:
    args = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    if len(args) not in (2, 3):
        raise SystemExit("Expected: <source.glb> <output.glb> [target_triangles]")

    source = Path(args[0]).resolve()
    output = Path(args[1]).resolve()
    target_triangles = int(args[2]) if len(args) == 3 else DEFAULT_TARGET_TRIANGLES
    if target_triangles <= 0:
        raise SystemExit("target_triangles must be greater than zero")
    output.parent.mkdir(parents=True, exist_ok=True)

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(source))

    mesh_objects = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    if not mesh_objects:
        raise RuntimeError("The GLB contains no mesh objects.")

    triangles_before = sum(triangle_count(obj) for obj in mesh_objects)
    ratio = min(1.0, target_triangles / max(1, triangles_before))

    for mesh_object in mesh_objects:
        bpy.context.view_layer.objects.active = mesh_object
        mesh_object.select_set(True)

        modifier = mesh_object.modifiers.new(name="WebGL_Decimate", type="DECIMATE")
        modifier.decimate_type = "COLLAPSE"
        modifier.ratio = ratio
        modifier.use_collapse_triangulate = True

        try:
            bpy.ops.object.modifier_move_to_index(modifier=modifier.name, index=0)
        except (AttributeError, RuntimeError):
            while mesh_object.modifiers.find(modifier.name) > 0:
                bpy.ops.object.modifier_move_up(modifier=modifier.name)

        bpy.ops.object.modifier_apply(modifier=modifier.name)
        mesh_object.select_set(False)

    for material in bpy.data.materials:
        disable_emission(material)

    resized_images: list[dict[str, object]] = []
    for image in bpy.data.images:
        width, height = image.size
        largest = max(width, height)
        if largest <= MAX_TEXTURE_SIZE or width <= 0 or height <= 0:
            continue

        scale = MAX_TEXTURE_SIZE / largest
        target_width = max(1, round(width * scale))
        target_height = max(1, round(height * scale))
        image.scale(target_width, target_height)
        resized_images.append(
            {
                "name": image.name,
                "from": [width, height],
                "to": [target_width, target_height],
            }
        )

    bpy.ops.export_scene.gltf(
        filepath=str(output),
        export_format="GLB",
        export_animations=True,
        export_skins=True,
        export_morph=True,
        export_lights=False,
        export_cameras=False,
    )

    triangles_after = sum(triangle_count(obj) for obj in mesh_objects)
    armatures = [obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE"]
    bone_count = sum(len(obj.data.bones) for obj in armatures)
    result = {
        "source": str(source),
        "output": str(output),
        "triangles_before": triangles_before,
        "triangles_after": triangles_after,
        "target_triangles": target_triangles,
        "ratio": ratio,
        "mesh_objects": len(mesh_objects),
        "armatures": len(armatures),
        "bones": bone_count,
        "resized_images": resized_images,
        "output_bytes": output.stat().st_size,
    }
    print("NIDO_OPTIMIZE_RESULT=" + json.dumps(result, ensure_ascii=False))


if __name__ == "__main__":
    main()
