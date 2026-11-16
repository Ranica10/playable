from pathlib import Path
import cv2 as cv
from matplotlib import pyplot as plt

project_root = Path(__file__).resolve().parent.parent
image_path = project_root / "samples" / "drawings" / "player.png"

image = cv.imread(str(image_path))

gray_image = cv.cvtColor(image, cv.COLOR_BGR2GRAY)

canny = cv.Canny(gray_image, 100, 200)

plt.imshow(canny, cmap="gray")
plt.axis("off")
plt.show()