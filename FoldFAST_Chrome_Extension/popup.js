const BRIDGE_URL = "http://127.0.0.1:16888";
let currentTheme = "dark";
let currentLang = "es";

function applyPopupSettings(theme, lang) {
  currentTheme = theme || "dark";
  currentLang = lang || "es";
  document.body.className = currentTheme === "light" ? "light" : "";

  const btnTheme = document.getElementById("btnTheme");
  if (btnTheme) btnTheme.textContent = currentTheme === "light" ? "☀️" : "🌙";

  const btnLang = document.getElementById("btnLang");
  if (btnLang) btnLang.textContent = currentLang.toUpperCase();
}

async function syncFromDesktop() {
  const dot = document.getElementById("statusDot");
  try {
    const r = await fetch(`${BRIDGE_URL}/ping`);
    if (r.ok) {
      const data = await r.json();
      dot.className = "dot on";
      if (data) {
        applyPopupSettings(data.theme || currentTheme, data.lang || currentLang);
        chrome.storage.local.set({ theme: currentTheme, lang: currentLang });
      }
      return;
    }
  } catch (e) {}
  dot.className = "dot";
}

document.addEventListener("DOMContentLoaded", async () => {
  const stored = await chrome.storage.local.get(["theme", "lang", "autoIntercept"]);
  applyPopupSettings(stored.theme || "dark", stored.lang || "es");
  syncFromDesktop();

  document.querySelectorAll(".opt-row").forEach(row => {
    row.addEventListener("click", async () => {
      const [tab] = await chrome.tabs.query({ active: true, currentWindow: true });
      if (!tab) return;
      const [mType, fmt, qual] = row.getAttribute("data-val").split("|");
      chrome.runtime.sendMessage({
        action: "download_media",
        url: tab.url,
        pageUrl: tab.url,
        title: tab.title,
        mediaType: mType,
        formatExt: fmt,
        quality: qual
      }, () => window.close());
    });
  });

  document.getElementById("btnLang").addEventListener("click", async () => {
    const nextLang = currentLang === "es" ? "en" : "es";
    applyPopupSettings(currentTheme, nextLang);
    chrome.storage.local.set({ lang: nextLang });
    try {
      await fetch(`${BRIDGE_URL}/lang`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ lang: nextLang })
      });
    } catch (e) {}
  });

  document.getElementById("btnTheme").addEventListener("click", async () => {
    const nextTheme = currentTheme === "dark" ? "light" : "dark";
    applyPopupSettings(nextTheme, currentLang);
    chrome.storage.local.set({ theme: nextTheme });
    try {
      await fetch(`${BRIDGE_URL}/theme`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ theme: nextTheme })
      });
    } catch (e) {}
  });

  document.getElementById("btnFolder").addEventListener("click", async () => {
    try {
      await fetch(`${BRIDGE_URL}/open_folder`, { method: "POST" });
      window.close();
    } catch (e) {}
  });

  const toggleBtn = document.getElementById("btnToggleAuto");
  let autoOn = stored.autoIntercept !== false;
  toggleBtn.className = autoOn ? "bar-item active" : "bar-item";
  toggleBtn.addEventListener("click", () => {
    autoOn = !autoOn;
    chrome.storage.local.set({ autoIntercept: autoOn });
    toggleBtn.className = autoOn ? "bar-item active" : "bar-item";
  });
});
