from pathlib import Path

backend_dir = Path(__file__).resolve().parent

import xml.etree.ElementTree as ET

annotation_path = (
    backend_dir / "data" / "annotations" / "drawing_001.xml"
)

root = ET.parse(annotation_path).getroot()

box_coordinates = []
labels = []

class_ids = {
    "player": 1,
    "collectible": 2,
    "spikes": 3,
    "exit": 4,
    "terrain": 5,
}

# loop through each object in the XML
for obj in root.findall("object"):
    # extract the class label 
    name = obj.findtext("name")

    # find the bounding box element
    bbox = obj.find("bndbox")
    # extract the bounding box coordinates
    coordinates = [
        float(bbox.findtext("xmin")),
        float(bbox.findtext("ymin")),
        float(bbox.findtext("xmax")),
        float(bbox.findtext("ymax")),
    ]

    box_coordinates.append(coordinates)
    labels.append(class_ids[name])

print("Box Coordinates:", box_coordinates)
print("Labels:", labels)