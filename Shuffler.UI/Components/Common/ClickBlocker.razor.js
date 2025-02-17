const blockers = new Set();

export function initClickBlocker(element, dotNetRef, contentElement) {
  console.log("initClickBlocker", element, contentElement);
  if (!element || blockers.has(element)) return;
  blockers.add({ element, dotNetRef, contentElement });
}

export function cleanupClickBlocker(element) {
  blockers.delete([...blockers].find((b) => b.element === element));
}

document.addEventListener("click", handleClick, true);
document.addEventListener("contextmenu", handleClick, true);

function handleClick(e) {
  for (const { element, dotNetRef, contentElement } of blockers) {
    const contained = contentElement?.contains(e.target);
    if (contained === false) {
      console.log("Handling click", e);
      dotNetRef.invokeMethodAsync("HandleClick", {
        type: e.type,
        clientX: e.clientX,
        clientY: e.clientY,
      });
    }
  }
}
