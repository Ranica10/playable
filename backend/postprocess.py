import json
from pathlib import Path

import pandas as pd

backend_dir = Path(__file__).resolve().parent

predictions_csv_path = backend_dir / "data" / "predictions.csv"
output_json_path = backend_dir / "data" / "output.json"

pixels_per_unit = 100

# Load the predictions CSV file
df = pd.read_csv(predictions_csv_path)

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


# Build the level JSON
level = {
    "schemaVersion": 1,
    "levelName": "Predictions",
    "terrain": [
        {"x": x, "y": y}
        for x, y in sorted(terrain_cells)
    ],
    "objects": [],
}

# Save the level JSON to a file
with output_json_path.open("w", encoding="utf-8") as f:
    json.dump(level, f, indent=4)