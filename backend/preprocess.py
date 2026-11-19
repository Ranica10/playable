from pathlib import Path

backend_dir = Path(__file__).resolve().parent

import xml.etree.ElementTree as ET

annotation_path = (
    backend_dir / "data" / "annotations" / "drawing_001.xml"
)

image_path = backend_dir / "data" / "images" / "drawing_001.png"

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


import torch
from torchvision import tv_tensors
from torchvision.io import decode_image
from torchvision.utils import draw_bounding_boxes
from matplotlib import pyplot as plt

img = decode_image(str(image_path), mode="RGB")

# Create a BoundingBoxes object from the box coordinates and the image size.
boxes = tv_tensors.BoundingBoxes(
    torch.tensor(box_coordinates, dtype=torch.float32).reshape(-1, 4),
    format="XYXY",
    canvas_size=img.shape[-2:],
)

# convert the numeric labels back into their names
id_to_name = {value: key for key, value in class_ids.items()}
label_names = [id_to_name[label] for label in labels]

# draw the bounding boxes on the image
preview = draw_bounding_boxes(
    img,
    boxes,
    labels=label_names,
    colors="red",
    width=3,
)

plt.figure(figsize=(14, 8))
plt.imshow(preview.permute(1, 2, 0)) # permute to convert from (C, H, W) to (H, W, C) for matplotlib
plt.axis("off")
plt.show()