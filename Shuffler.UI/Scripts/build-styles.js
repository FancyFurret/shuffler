const fs = require("fs");
const { exec } = require("child_process");

const tailwindInput = "./Shuffler.UI/wwwroot/css/app.css";
const tailwindOutput = "./Shuffler.UI/wwwroot/css/app.min.css"; // Tailwind's minified output
const razorOutput = "./Shuffler.UI/Components/Styles.razor";

// Start the Tailwind CLI in watch mode
function startTailwindWatch() {
  console.log("Starting Tailwind CLI in watch mode...");
  const command = `cd Shuffler.UI && npx tailwindcss -i wwwroot/css/app.css -o wwwroot/css/app.min.css --minify --watch`;

  const tailwindProcess = exec(command);

  tailwindProcess.stdout.on("data", (data) => {
    console.log(`Tailwind: ${data.toString().trim()}`);
  });

  tailwindProcess.stderr.on("data", (data) => {
    console.error(`Tailwind Error: ${data.toString().trim()}`);
  });

  tailwindProcess.on("close", (code) => {
    console.log(`Tailwind process exited with code ${code}`);
    process.exit(code);
  });

  return tailwindProcess;
}

// Watch the minified output file for changes and generate Razor
function watchForChanges() {
  console.log(`Watching for changes in: ${tailwindOutput}`);
  fs.watch(tailwindOutput, (eventType, filename) => {
    if (eventType === "change") {
      console.log(`Detected change in: ${filename}`);
      generateRazorComponent();
    }
  });
}

// Generate the Razor component
function generateRazorComponent() {
  try {
    const css = fs.readFileSync(tailwindOutput, "utf-8");
    // Escape @ symbols in CSS by doubling them for Razor
    const escapedCss = css.replace(/@/g, "@@");
    const razorContent = `<style>\n${escapedCss}\n</style>`;

    // Only write if content has changed
    let currentContent = "";
    try {
      currentContent = fs.readFileSync(razorOutput, "utf-8");
    } catch (err) {
      // File doesn't exist yet, that's fine
    }

    if (currentContent !== razorContent) {
      fs.writeFileSync(razorOutput, razorContent);
      console.log(`Updated Razor component: ${razorOutput}`);
    } else {
      console.log(
        `No changes detected in CSS, skipping Razor component update`
      );
    }
  } catch (err) {
    console.error(`Failed to generate Razor component: ${err.message}`);
  }
}

// Graceful shutdown
function handleExit(tailwindProcess) {
  console.log("\nStopping Tailwind watch process...");
  tailwindProcess.kill();
  process.exit();
}

// Main process
const tailwindProcess = startTailwindWatch();
watchForChanges();
generateRazorComponent();

// Handle termination signals
process.on("SIGINT", () => handleExit(tailwindProcess));
process.on("SIGTERM", () => handleExit(tailwindProcess));
