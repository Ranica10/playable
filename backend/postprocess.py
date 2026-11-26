import json
from pathlib import Path

import pandas as pd

backend_dir = Path(__file__).resolve().parent

predictions_csv_path = backend_dir / "data" / "predictions.csv"
output_json_path = backend_dir / "data" / "output.json"

pixels_per_unit = 100

# Load the predictions CSV file
df = pd.read_csv(predictions_csv_path)

# Build the level JSON
level = {
    "schemaVersion": 1,
    "levelName": "Predictions",
    "terrain": [],
    "objects": [],
}

### Terrain Processing

terrain = df[df["label"] == "terrain"].copy()

# Choose the widest terrain rectangle as the main floor
widths = terrain["x2"] - terrain["x1"]
floor = terrain.loc[widths.idxmax()]

origin_x = float(floor["x1"])
origin_y = float(floor["y1"])

# Convert terrain rectangles into occupied grid cells -> use a set to avoid duplicates
terrain_cells = set()

for _, row in terrain.iterrows():
    xmin = float(row["x1"])
    ymin = float(row["y1"])
    xmax = float(row["x2"])
    ymax = float(row["y2"])

    # Convert pixel edges into units and round to the nearest integer grid cell
    left = round((xmin - origin_x) / pixels_per_unit)
    right = round((xmax - origin_x) / pixels_per_unit)

    top = round((origin_y - ymin) / pixels_per_unit)
    bottom = round((origin_y - ymax) / pixels_per_unit)

    # Keep thin platforms at least one cell wide and tall
    right = max(right, left + 1)
    bottom = min(bottom, top - 1)

    # Fill the rectangle with cells
    for x in range(left, right):
        for y in range(bottom + 1, top + 1):
            terrain_cells.add((x, y))

for x, y in sorted(terrain_cells):
    level["terrain"].append({"x": x, "y": y})
    

### Object Processing

objects = df[df["label"] != "terrain"].copy()

# map object types to their heights in units (for snapping to platforms)
object_heights = {
    "player": 1.2,
    "spikes": 0.6,
    "exit": 1.5,
}

for _, row in objects.iterrows():
    xmin = float(row["x1"])
    ymin = float(row["y1"])
    xmax = float(row["x2"])
    ymax = float(row["y2"])

    cx = (xmin + xmax) / 2
    cy = (ymin + ymax) / 2

    object_type = row["label"]

    # Convert the centre, including the terrain's half-cell alignment
    obj_x = (cx - origin_x) / pixels_per_unit - 0.5
    obj_y = (origin_y - cy) / pixels_per_unit + 0.5

    # Align grounded objects to a nearby platform
    if object_type in object_heights:
        # Find platforms underneath the object's horizontal centre
        candidates = terrain[
            (terrain["x1"] <= cx)
            & (terrain["x2"] >= cx)
        ]

        # Only snap if there are candidate platforms
        if not candidates.empty:
            # Compare the object's feet/base with platform top edges
            distances = (candidates["y1"] - ymax).abs()
            platform = candidates.loc[distances.idxmin()]

            # Only snap if the base is within half a unit of the platform
            if abs(float(platform["y1"]) - ymax) <= pixels_per_unit * 0.5:
                platform_top = round(
                    (origin_y - float(platform["y1"])) / pixels_per_unit
                )

                surface_y = platform_top + 0.5
                obj_y = surface_y + object_heights[object_type] / 2

                # add some small clearance so the player starts above the collider
                if object_type == "player":
                    obj_y += 0.05

    level["objects"].append({
        "type": object_type,
        "x": obj_x,
        "y": obj_y,
    })


### Save the level JSON

# Save the level JSON to a file
with output_json_path.open("w", encoding="utf-8") as f:
    json.dump(level, f, indent=4)