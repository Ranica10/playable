from pathlib import Path

backend_dir = Path(__file__).resolve().parent

import torch
from torchvision import tv_tensors
from torchvision.io import decode_image
from torchvision.utils import draw_bounding_boxes
from matplotlib import pyplot as plt

def show_bounding_boxes(image_path, box_coordinates, labels, class_ids):
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

# show_bounding_boxes(image_path, box_coordinates, labels, class_ids)


import os
from skimage import io
import numpy as np
import pandas as pd
from torch.utils.data import Dataset

csv_path = backend_dir / "data" / "dataset.csv"

class_ids = {
    "player": 1,
    "collectible": 2,
    "spikes": 3,
    "exit": 4,
    "terrain": 5,
}

df = pd.read_csv(csv_path, usecols=["image_name", "bbox_x", "bbox_y", "bbox_width", "bbox_height", "label_name"])
data = df[["bbox_x", "bbox_y", "bbox_width", "bbox_height"]]
labels = df["label_name"].map(class_ids).values

print(data.head())
print(labels[:5])
