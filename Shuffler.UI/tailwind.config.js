/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ["./**/*.{razor,html,cshtml}", "!./Components/Styles.razor"],
  theme: {
    fontFamily: {
      sans: "var(--font-sans)",
    },
    borderRadius: {
      lg: "var(--radius)",
      md: "calc(var(--radius) - 2px)",
      sm: "calc(var(--radius) - 4px)",
      full: "9999px",
    },
    extend: {
      gridTemplateColumns: {
        "auto-fit": "repeat(auto-fill, minmax(0, 1fr))",
      },
      colors: {
        background: generateColorShades("--color-background"),
        foreground: generateColorShades("--color-foreground"),
        disabled: generateColorShades("--color-disabled"),
        "muted-foreground": generateColorShades("--color-muted-foreground"),
        primary: generateColorShades("--color-primary"),
        border: generateColorShades("--color-border"),
        destructive: generateColorShades("--color-destructive"),
        success: generateColorShades("--color-success"),
        warning: generateColorShades("--color-warning"),
        error: generateColorShades("--color-error"),
        gradient: {
          1: "var(--color-gradient-1)",
          2: "var(--color-gradient-2)",
        },
      },
    },
  },
  plugins: [],
};

function generateColorShades(baseColorVar) {
  const withOpacity = (color) => {
    return `color-mix(in oklab, ${color} calc(<alpha-value> * 100%), transparent)`;
  };

  const shades = {
    DEFAULT: withOpacity(`var(${baseColorVar})`),
    hover: withOpacity(`color-mix(in srgb, white 20%, var(${baseColorVar}))`),
  };

  for (let i = 1; i <= 9; i++) {
    const shade = i * 100;
    let mixColor;
    let percentage;

    const minPercentage = 10;
    const maxPercentage = 90;

    if (i <= 4) {
      mixColor = "white";
      percentage = 1 - (i - 1) / 3;
    } else if (i === 5) {
      shades[shade] = withOpacity(`var(${baseColorVar})`);
      continue;
    } else {
      mixColor = "black";
      percentage = 1 - (9 - i) / 3;
    }

    percentage = minPercentage + percentage * (maxPercentage - minPercentage);
    shades[shade] = withOpacity(
      `color-mix(in srgb, ${mixColor} ${percentage}%, var(${baseColorVar}))`
    );
  }

  return shades;
}
