// Servicio en segundo plano v6.0 de Fold FAST para Google Chrome: Intercepta CUALQUIER archivo descargado + Videos/Audios
const BRIDGE_URL = "http://127.0.0.1:16888";

const STREAMING_DOMAINS = [
  "youtube.com", "youtu.be", "googlevideo.com", "tiktok.com", "tiktokcdn",
  "facebook.com", "fb.watch", "fb.com", "fbcdn.net", "instagram.com", "cdninstagram.com",
  "twitter.com", "x.com", "twimg.com", "vimeo.com", "soundcloud.com", "twitch.tv", "dailymotion.com", "reddit.com"
];

const tabMediaStreams = new Map();

function isStreamingUrl(u) {
  if (!u) return false;
  const low = u.toLowerCase();
  if (low.startsWith("blob:")) return true;
  return STREAMING_DOMAINS.some(d => low.includes(d));
}

function cleanSocialCdnUrl(rawUrl) {
  if (!rawUrl) return "";
  try {
    const u = new URL(rawUrl);
    if (u.hostname.includes("fbcdn.net") || u.hostname.includes("cdninstagram.com")) {
      u.searchParams.delete("bytestart");
      u.searchParams.delete("byteend");
      return u.toString();
    }
  } catch (e) {}
  return rawUrl;
}

async function syncSettingsFromDesktop() {
  try {
    const r = await fetch(`${BRIDGE_URL}/ping`);
    if (r.ok) {
      const data = await r.json();
      if (data) {
        const updates = {};
        if (data.theme) updates.theme = data.theme;
        if (data.lang) updates.lang = data.lang;
        await chrome.storage.local.set(updates);
      }
    }
  } catch (e) {}
}

setInterval(syncSettingsFromDesktop, 3000);
syncSettingsFromDesktop();

chrome.runtime.onInstalled.addListener(async () => {
  chrome.storage.local.set({ autoIntercept: true, theme: "dark", lang: "es" });

  chrome.contextMenus.removeAll(() => {
    chrome.contextMenus.create({
      id: "foldfast-download-link",
      title: "⚡ Fold FAST: Descargar / Download",
      contexts: ["link", "video", "audio", "image", "page"]
    });
  });

  try {
    const tabs = await chrome.tabs.query({ url: ["http://*/*", "https://*/*"] });
    for (const tab of tabs) {
      if (tab.id) {
        chrome.scripting.executeScript({
          target: { tabId: tab.id, allFrames: true },
          files: ["content.js"]
        }).catch(() => {});
      }
    }
  } catch (e) {}
});

chrome.webRequest.onHeadersReceived.addListener(
  (details) => {
    if (details.tabId < 0 || details.type === "main_frame") return;
    const rawUrl = details.url || "";
    if (!rawUrl.startsWith("http") || rawUrl.includes("127.0.0.1:16888")) return;

    let contentType = "";
    let contentLength = 0;
    for (const h of (details.responseHeaders || [])) {
      const name = (h.name || "").toLowerCase();
      if (name === "content-type") contentType = (h.value || "").toLowerCase();
      if (name === "content-length") contentLength = parseInt(h.value || "0", 10) || 0;
    }

    const lowerUrl = rawUrl.split("?")[0].toLowerCase();
    const isFbOrIgCdn = rawUrl.includes("fbcdn.net") || rawUrl.includes("cdninstagram.com");
    const isVideoContent =
      contentType.startsWith("video/") ||
      lowerUrl.endsWith(".mp4") ||
      lowerUrl.endsWith(".webm") ||
      lowerUrl.endsWith(".m3u8");
    const isAudioContent =
      contentType.startsWith("audio/") ||
      lowerUrl.endsWith(".mp3") ||
      lowerUrl.endsWith(".m4a") ||
      lowerUrl.endsWith(".aac") ||
      lowerUrl.endsWith(".ogg") ||
      lowerUrl.endsWith(".wav") ||
      lowerUrl.endsWith(".flac");

    if (isVideoContent || isAudioContent) {
      const cleanedUrl = cleanSocialCdnUrl(rawUrl);
      const mediaType = isAudioContent ? "audio" : "video";

      if (isFbOrIgCdn || contentLength > 120000 || lowerUrl.endsWith(".m3u8")) {
        if (!cleanedUrl.includes("googlevideo.com/videoplayback")) {
          tabMediaStreams.set(details.tabId, {
            url: cleanedUrl,
            mediaType,
            size: contentLength,
            time: Date.now()
          });
        }
        chrome.tabs.sendMessage(details.tabId, {
          action: "network_media_detected",
          streamUrl: cleanedUrl,
          mediaType
        }).catch(() => {});
      }
    }
  },
  { urls: ["<all_urls>"] },
  ["responseHeaders"]
);

async function sendToFoldFast(payload) {
  try {
    const resp = await fetch(`${BRIDGE_URL}/add`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload)
    });
    if (resp.ok) {
      return true;
    }
  } catch (err) {}
  return false;
}

chrome.contextMenus.onClicked.addListener(async (info, tab) => {
  if (info.menuItemId === "foldfast-download-link") {
    const pageUrl = (tab && tab.url) || info.pageUrl || "";
    const targetUrl = info.linkUrl || info.srcUrl || pageUrl;
    const cached = (tab && tab.id) ? tabMediaStreams.get(tab.id) : null;
    if (!targetUrl) return;
    await sendToFoldFast({
      url: targetUrl,
      pageUrl: pageUrl,
      fallbackStreamUrl: (cached && cached.url) || "",
      title: (tab && tab.title) || "",
      mediaType: info.linkUrl ? "file" : (info.mediaType || "video"),
      formatExt: "",
      quality: info.linkUrl ? "Directo" : "1080",
      referer: pageUrl,
      userAgent: navigator.userAgent
    });
  }
});

// INTERCEPTAR AUTOMÁTICAMENTE CUALQUIER ARCHIVO QUE SE DESCARGUE EN GOOGLE CHROME E INICIARLO EN FOLD FAST
chrome.downloads.onCreated.addListener(async (downloadItem) => {
  const url = downloadItem.finalUrl || downloadItem.url || "";
  if (!url.startsWith("http://") && !url.startsWith("https://")) return;

  const data = await chrome.storage.local.get(["autoIntercept"]);
  if (data.autoIntercept === false) return;

  const connected = await sendToFoldFast({
    url,
    filename: downloadItem.filename || "",
    mediaType: "file",
    formatExt: "",
    quality: "Directo",
    referer: downloadItem.referrer || "",
    userAgent: navigator.userAgent
  });

  if (connected) {
    try {
      chrome.downloads.cancel(downloadItem.id);
      chrome.downloads.erase({ id: downloadItem.id });
    } catch (e) {}
  }
});

chrome.runtime.onMessage.addListener((msg, sender, sendResponse) => {
  if (msg.action === "download_media" || msg.action === "download_url") {
    const tabId = sender && sender.tab && sender.tab.id;
    const tabUrl = msg.pageUrl || (sender && sender.tab && sender.tab.url) || "";
    const cachedStream = tabId ? tabMediaStreams.get(tabId) : null;
    const fallbackUrl = msg.fallbackStreamUrl || (cachedStream ? cachedStream.url : "");

    let finalUrl = msg.url || tabUrl || fallbackUrl;

    const payload = {
      url: finalUrl,
      pageUrl: msg.pageUrl || tabUrl,
      fallbackStreamUrl: fallbackUrl,
      title: msg.title || (sender && sender.tab && sender.tab.title) || "",
      filename: msg.filename || "",
      mediaType: msg.mediaType || "video",
      formatExt: msg.formatExt || (msg.mediaType === "audio" ? "mp3" : "mp4"),
      quality: msg.quality || (msg.mediaType === "audio" ? "320k" : "1080"),
      referer: tabUrl,
      userAgent: navigator.userAgent
    };

    sendToFoldFast(payload).then((ok) => {
      sendResponse({ ok });
    });
    return true;
  }
});
