from pathlib import Path

backend_dir = Path(__file__).resolve().parent

import torch
from torchvision import tv_tensors
from torchvision.io import decode_image
from torchvision.utils import draw_bounding_boxes
from matplotlib import pyplot as plt

def show_bounding_boxes(image, target, class_ids, ax):
    # Convert class_ids to a dictionary mapping from id to name
    id_to_name = {value: key for key, value in class_ids.items()}
    label_names = [
        id_to_name[int(label)] for label in target["labels"]
    ]

    # Draw the bounding boxes on the image
    preview = draw_bounding_boxes(
        image.cpu(),
        target["boxes"].cpu(),
        labels=label_names,
        colors="red",
        width=3,
    )

    ax.imshow(preview.permute(1, 2, 0))
    ax.axis("off")

# show_bounding_boxes(image_path, box_coordinates, labels, class_ids)


import os
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

class DrawingsDataset(Dataset):
    """Drawings dataset."""

    def __init__(self, root_dir, csv_path, transform=None):
        """
        Arguments:
            root_dir (string): Directory with all the images.
            csv_path (string): Path to the csv file with annotations.
            transform (callable, optional): Optional transform to be applied
                on a sample.
        """
        self.df = pd.read_csv(csv_path, usecols=["image_name", "bbox_x", "bbox_y", "bbox_width", "bbox_height", "label_name"])

        self.image_names = self.df["image_name"].unique()
        
        self.data = self.df[["bbox_x", "bbox_y"]].copy()
        # convert x, y, width, height -> xmin, ymin, xmax, ymax
        self.data["bbox_xmax"] = self.df["bbox_x"] + self.df["bbox_width"]
        self.data["bbox_ymax"] = self.df["bbox_y"] + self.df["bbox_height"]

        self.labels = self.df["label_name"].map(class_ids).values

        self.root_dir = root_dir
        self.transform = transform

    def __len__(self):
        return len(self.image_names)

    def __getitem__(self, idx):
        if torch.is_tensor(idx):
            idx = idx.item()

        # get the image name + path
        image_name = self.image_names[idx]
        img_path = os.path.join(self.root_dir, image_name)
        # load the image
        image = decode_image(str(img_path), mode="RGB")

        # create a mask to filter the dataframe for the current image
        mask = self.df["image_name"] == image_name

        # get all the annotations for this image
        annotations = self.data[mask]
        # get the labels for this image
        labels = torch.tensor(self.labels[mask], dtype=torch.int64)

        # extract the bounding box coordinates and convert to a numpy array of shape (N, 4)
        box_coordinates = annotations[["bbox_x", "bbox_y", "bbox_xmax", "bbox_ymax"]].values.astype(np.float32)

        # create a BoundingBoxes object from the box coordinates and the image size
        boxes = tv_tensors.BoundingBoxes(
                torch.tensor(box_coordinates, dtype=torch.float32).reshape(-1, 4),
                format="XYXY",
                canvas_size=image.shape[-2:],
        )

        # create a sample dict to hold the image name, bounding boxes, and labels
        sample = {"image_name": image_name, "boxes": boxes, "labels": labels}

        # apply any transformations to the image and sample (if any)
        if self.transform is not None:
            image, sample = self.transform(image, sample)

        # return the image and the sample dict
        return image, sample


### Transforms
from torchvision.transforms import v2

transforms = v2.Compose(
    [
        v2.ToImage(),
        v2.RandomHorizontalFlip(p=1),
        v2.SanitizeBoundingBoxes(),
        v2.ToDtype(torch.float32, scale=True),
    ]
)


dataset = DrawingsDataset(
    root_dir=backend_dir / "data" / "images",
    csv_path=csv_path,
    transform=transforms
)

fig, axes = plt.subplots(1, 4, figsize=(20, 6))

for i, ax in enumerate(axes):
    if i >= len(dataset):
        ax.axis("off")
        continue

    image, target = dataset[i]

    show_bounding_boxes(image, target, class_ids, ax)
    ax.set_title(f"Sample #{i}")

plt.tight_layout()
plt.show()