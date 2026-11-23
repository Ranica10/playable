import csv
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


### Post processing

# Discard a lower-confidence terrain box when it lies almost entirely inside a higher-confidence terrain box

# tolerance for how much a box can be inside another box and still be considered "inside"
margin = 15  # pixels

for i in range(len(boxes)):
    if labels[i] != class_ids["terrain"]:
        continue

    # check all other terrain boxes
    for j in range(len(boxes)):
        # only compare against a lower-confidence terrain box
        if (i == j
                or labels[j] != class_ids["terrain"]
                or scores[j] >= scores[i]):
            continue

        # check if box j is almost entirely inside box i
        inside = (
            boxes[j][0] >= boxes[i][0] - margin
            and boxes[j][1] >= boxes[i][1] - margin
            and boxes[j][2] <= boxes[i][2] + margin
            and boxes[j][3] <= boxes[i][3] + margin
        )

        # discard box j by setting its score to 0 if inside the other terrain box
        if inside:
            scores[j] = 0.0


# Discard a collectible box fully inside a confidently detected exit
for i in range(len(boxes)):
    # check all exit boxes
    if labels[i] == class_ids["exit"]:
        for j in range(len(boxes)):
            # compare box j with box i
            if i != j and labels[j] == class_ids["collectible"]:
                # check if box j is almost entirely inside box i
                if (
                    boxes[j][0] >= boxes[i][0]
                    and boxes[j][1] >= boxes[i][1]
                    and boxes[j][2] <= boxes[i][2]
                    and boxes[j][3] <= boxes[i][3]
                ):
                    # discard box j by setting its score to 0
                    scores[j] = 0.0


# Keep only boxes with scores above the threshold after post-processing
keep = scores >= threshold

boxes = boxes[keep]
labels = labels[keep]
scores = scores[keep]


### Save the predictions to a CSV file to use for level generation 
predictions_csv_path = backend_dir / "data" / "predictions.csv"

with open(predictions_csv_path, "w", newline="") as f:
    writer = csv.writer(f)
    writer.writerow(["label", "x1", "y1", "x2", "y2", "id", "score"])

    id_to_name = {v: k for k, v in class_ids.items()}
    for box, id, score in zip(boxes.tolist(), labels.tolist(), scores.tolist()):
        writer.writerow([id_to_name[id], box[0], box[1], box[2], box[3], id, score])


# display_labels = [
#     f"{id_to_name[int(label)]}: {float(score):.2f}"
#     for label, score in zip(labels, scores)
# ]

# for name, box in zip(display_labels, boxes.tolist()):
#     print(name, box)

# print(f"Found {len(boxes)} objects above confidence {threshold}")

# preview = draw_bounding_boxes(
#     image,
#     boxes,
#     labels=display_labels,
#     colors="red",
#     width=3,
# )

# plt.figure(figsize=(14, 8))
# plt.imshow(preview.permute(1, 2, 0))
# plt.axis("off")
# plt.show()