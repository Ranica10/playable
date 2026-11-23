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

        # Use the final boxes after transformations.
        final_boxes = sample["boxes"]

        sample["image_id"] = int(idx)

        sample["area"] = (
            (final_boxes[:, 2] - final_boxes[:, 0])
            * (final_boxes[:, 3] - final_boxes[:, 1])
        )

        sample["iscrowd"] = torch.zeros(
            len(final_boxes), dtype=torch.int64
        )

        # return the image and the sample dict
        return image, sample


### Transforms
from torchvision.transforms import v2

train_transforms = v2.Compose([
    v2.ToImage(),
    v2.RandomHorizontalFlip(p=0.5),
    v2.SanitizeBoundingBoxes(),
    v2.ToDtype(torch.float32, scale=True),
])

val_transforms = v2.Compose([
    v2.ToImage(),
    v2.SanitizeBoundingBoxes(),
    v2.ToDtype(torch.float32, scale=True),
])

# dataset = DrawingsDataset(
#     root_dir=backend_dir / "data" / "images",
#     csv_path=csv_path,
#     transform=transforms
# )

# fig, axes = plt.subplots(1, 4, figsize=(20, 6))

# for i, ax in enumerate(axes):
#     if i >= len(dataset):
#         ax.axis("off")
#         continue

#     image, target = dataset[i]

#     show_bounding_boxes(image, target, class_ids, ax)
#     ax.set_title(f"Sample #{i} - {target['image_name']}")

# plt.tight_layout()
# plt.show()


from torch.utils.data import DataLoader

# Load training dataset
train_dataset = DrawingsDataset(
    root_dir=backend_dir / "data" / "train",
    csv_path=backend_dir / "data" / "train" / "annotations.csv",
    transform=train_transforms
)

# Load validation dataset
val_dataset = DrawingsDataset(
    root_dir=backend_dir / "data" / "test",
    csv_path=backend_dir / "data" / "test" / "annotations.csv",
    transform=val_transforms
)

# Load dataset with DataLoaders
train_loader = DataLoader(train_dataset, batch_size=2, shuffle=True, collate_fn=lambda x: tuple(zip(*x)))
val_loader = DataLoader(val_dataset, batch_size=1, shuffle=False, collate_fn=lambda x: tuple(zip(*x)))

# Set device to GPU if available, otherwise use CPU
if torch.cuda.is_available():
    device = torch.device("cuda")
else:
    device = torch.device("cpu")

print(f"Using device: {device}")


### Load the model for object detection

import torchvision
from torchvision.models.detection.faster_rcnn import FastRCNNPredictor, FasterRCNN_ResNet50_FPN_Weights

# Load a pre-trained Faster R-CNN model with ResNet50 backbone and FPN
model = torchvision.models.detection.fasterrcnn_resnet50_fpn(
    weights=FasterRCNN_ResNet50_FPN_Weights.DEFAULT
)

# Number of classes in the dataset (+1 for background class)
num_classes = len(class_ids) + 1

# Number of input features for the classifier head
in_features = model.roi_heads.box_predictor.cls_score.in_features

# Replace the pre-trained head with a new one for our dataset
model.roi_heads.box_predictor = FastRCNNPredictor(in_features, num_classes)

# Move the model to the GPU for faster training
model.to(device)

# Get parameters that require gradients (the model's trainable parameters)
params = [p for p in model.parameters() if p.requires_grad]

# Define the optimizer SGD(Stochastic Gradient Descent) 
optimizer = torch.optim.SGD(params, lr=0.005,
                            momentum=0.9, weight_decay=0.0005)

# and a learning rate scheduler
lr_scheduler = torch.optim.lr_scheduler.StepLR(
    optimizer,
    step_size=10,
    gamma=0.1
)


### Training loop

import sys

references_dir = backend_dir / "references"
sys.path.insert(0, str(references_dir))

from references.engine import train_one_epoch, evaluate

models_dir = backend_dir / "models"
models_dir.mkdir(parents=True, exist_ok=True)

# Number of epochs for training
num_epochs = 30

# Loop through each epoch
for epoch in range(num_epochs):
    print(f"\nEpoch {epoch + 1}/{num_epochs}")

    # Train the model for one epoch, printing status every 25 iterations
    train_one_epoch(model, optimizer, train_loader, device, epoch, print_freq=25)  # Using train_loader for training

    # Update the learning rate
    lr_scheduler.step()

    # Evaluate the model on the validation dataset after each epoch
    evaluate(model, val_loader, device=device)  # Using val_loader for evaluation

    # save the model after each epoch
    torch.save(model.state_dict(), models_dir / f"model_epoch_{epoch + 1}.pth")