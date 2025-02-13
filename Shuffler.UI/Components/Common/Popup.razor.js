export function positionContent(content, marker, x, y) {
  console.log("positionContent called with:", { content, marker, x, y });

  if (!content) {
    console.log("Content element is null, returning");
    return;
  }

  // Set initial position
  const parent = content.parentElement;
  console.log("Parent element:", parent);
  parent.style.position = "absolute";

  // Wait a frame to ensure content is rendered
  requestAnimationFrame(() => {
    const parentRect = parent.getBoundingClientRect();
    const viewportWidth = window.innerWidth;
    const viewportHeight = window.innerHeight;
    const gap = 6; // Gap between parent and popup

    console.log("Initial measurements:", {
      parentRect,
      viewportWidth,
      viewportHeight,
      gap,
    });

    // Get reference point (either from cursor or marker)
    let referencePoint;
    if (x != null && y != null) {
      referencePoint = { left: x, right: x, top: y, bottom: y };
      console.log(
        "Using cursor coordinates as reference point:",
        referencePoint
      );
    } else if (marker) {
      referencePoint = marker.parentElement.getBoundingClientRect();
      console.log("Using marker element as reference point:", referencePoint);
    } else {
      console.log("No reference point available, returning");
      return;
    }

    // Try positions in order: right-up, left-up, right-down, left-down
    const positions = [
      // Right-up
      {
        left: referencePoint.right,
        top: referencePoint.top - parentRect.height - gap,
        origin: "bottom left",
      },
      // Left-up
      {
        left: referencePoint.right - parentRect.width,
        top: referencePoint.top - parentRect.height - gap,
        origin: "bottom right",
      },
      // Right-down
      {
        left: referencePoint.right,
        top: referencePoint.bottom + gap,
        origin: "top left",
      },
      // Left-down
      {
        left: referencePoint.right - parentRect.width,
        top: referencePoint.bottom + gap,
        origin: "top right",
      },
    ];

    console.log("Calculated possible positions:", positions);

    // Find the first position that fits in the viewport
    let position = positions.find((pos) => {
      const fits =
        pos.left >= 0 &&
        pos.left + parentRect.width <= viewportWidth &&
        pos.top >= 0 &&
        pos.top + parentRect.height <= viewportHeight;

      console.log("Testing position:", pos, "Fits:", fits);
      return fits;
    });

    // If no position fits perfectly, use the first position and clamp to viewport
    if (!position) {
      console.log("No perfect fit found, using first position");
      position = positions[0];
    }

    console.log("Selected position:", position);

    // Set transform origin before any other styles
    content.style.transformOrigin = position.origin;
    console.log("Set transform origin:", position.origin);

    // Clamp to viewport bounds
    const left = Math.max(
      0,
      Math.min(position.left, viewportWidth - parentRect.width)
    );
    const top = Math.max(
      0,
      Math.min(position.top, viewportHeight - parentRect.height)
    );

    console.log("Final clamped position:", { left, top });

    parent.style.left = `${left}px`;
    parent.style.top = `${top}px`;
  });
}
