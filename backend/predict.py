from pathlib import Path

import torch
from torchvision.io import decode_image
from torchvision.models.detection import fasterrcnn_resnet50_fpn
from torchvision.models.detection.faster_rcnn import FastRCNNPredictor
from torchvision.utils import draw_bounding_boxes
from matplotlib import pyplot as plt

backend_dir = Path(__file__).resolve().parent

# path to the trained model weights
model_path = backend_dir / "models" / "model_epoch_30.pth"
# path to the image we want to predict
image_path = backend_dir / "data" / "test" / "drawing_101.png"

class_ids = {
    "player": 1,
    "collectible": 2,
    "spikes": 3,
    "exit": 4,
    "terrain": 5,
}
id_to_name = {value: key for key, value in class_ids.items()}

device = torch.device("cuda" if torch.cuda.is_available() else "cpu")

# recreate the same architecture used during training
model = fasterrcnn_resnet50_fpn(
    weights=None,
    weights_backbone=None,
)

in_features = model.roi_heads.box_predictor.cls_score.in_features
model.roi_heads.box_predictor = FastRCNNPredictor(
    in_features, len(class_ids) + 1
)

# Load the models trained weights
state_dict = torch.load(
    model_path,
    map_location="cpu",
    weights_only=True,
)
model.load_state_dict(state_dict)
model.to(device)
model.eval()

# load the original image to show the predictions on top of it
image = decode_image(str(image_path), mode="RGB")

# preprocess the image to match the input format expected by the model  
input_image = image.to(device=device, dtype=torch.float32) / 255.0

# Make a prediction using the model
with torch.inference_mode():
    prediction = model([input_image])[0]

# Show only predictions above the chosen confidence threshold: >=50%
threshold = 0.5
keep = prediction["scores"] >= threshold

boxes = prediction["boxes"][keep].cpu()
labels = prediction["labels"][keep].cpu()
scores = prediction["scores"][keep].cpu()

display_labels = [
    f"{id_to_name[int(label)]}: {float(score):.2f}"
    for label, score in zip(labels, scores)
]

for name, box in zip(display_labels, boxes.tolist()):
    print(name, box)

print(f"Found {len(boxes)} objects above confidence {threshold}")

preview = draw_bounding_boxes(
    image,
    boxes,
    labels=display_labels,
    colors="red",
    width=3,
)

plt.figure(figsize=(14, 8))
plt.imshow(preview.permute(1, 2, 0))
plt.axis("off")
plt.show()