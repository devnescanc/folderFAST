// Fold FAST v6.0: 1 SOLO ICONO que inicia la descarga inmediatamente al elegir cualquier opción de la lista
(function() {
  ["openidm-floating-bubble", "openidm-floating-bubble-v3", "openidm-minimal-bubble", "openidm-v4-widget", "foldfast-v5-widget", "foldfast-v6-widget"].forEach(id => {
    const old = document.getElementById(id);
    if (old) old.remove();
  });

  if (window.__foldFastInjectedV6) return;
  window.__foldFastInjectedV6 = true;

  const LOGO_DATA_URI = "data:image/png;base64,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";

  let rootWidget = null;
  let iconTrigger = null;
  let menuList = null;
  let badgeLabel = null;
  let activeMediaElement = null;
  let latestNetworkStreamUrl = "";
  let currentTheme = "dark";
  let currentLang = "es";
  let menuOpen = false;

  const OPTIONS = [
    { value: "video|mp4|1080", icon: "🎬", es: "MP4 • 1080p", en: "MP4 • 1080p" },
    { value: "video|mp4|720",  icon: "🎬", es: "MP4 • 720p",  en: "MP4 • 720p"  },
    { value: "video|mp4|480",  icon: "🎬", es: "MP4 • 480p",  en: "MP4 • 480p"  },
    { value: "video|mp4|360",  icon: "🎬", es: "MP4 • 360p",  en: "MP4 • 360p"  },
    { value: "video|mkv|best", icon: "🎬", es: "MKV • Auto",  en: "MKV • Auto"  },
    { value: "audio|mp3|320k", icon: "🎵", es: "MP3 • 320k",  en: "MP3 • 320k"  },
    { value: "audio|mp3|128k", icon: "🎵", es: "MP3 • 128k",  en: "MP3 • 128k"  },
    { value: "audio|m4a|320k", icon: "🎵", es: "M4A • Audio", en: "M4A • Audio" },
    { value: "audio|wav|best", icon: "🎵", es: "WAV • Audio", en: "WAV • Audio" }
  ];

  function findMediaPermalink(mediaEl) {
    const pageHref = location.href;
    if (!mediaEl) return pageHref;

    if (/\/(watch|reel|reels|videos|shorts|status|p)\b/i.test(pageHref)) {
      return pageHref;
    }

    let node = mediaEl;
    for (let depth = 0; depth < 12 && node && node !== document.body; depth++) {
      node = node.parentElement;
      if (!node || !node.querySelectorAll) continue;
      const links = node.querySelectorAll("a[href]");
      for (const a of links) {
        const h = a.getAttribute("href") || "";
        if (/(\/reel\/|\/reels\/|\/videos\/|\/watch\/?\?v=|\/status\/|\/shorts\/|\/p\/)/i.test(h)) {
          try {
            return new URL(h, location.origin).href;
          } catch (e) {}
        }
      }
    }
    return pageHref;
  }

  function isValidMediaElement(el, allowHoveredPaused = false) {
    if (!el || (el.tagName !== "VIDEO" && el.tagName !== "AUDIO")) return false;

    const src = el.currentSrc || el.src || (el.querySelector && el.querySelector("source[src]") && el.querySelector("source[src]").src) || latestNetworkStreamUrl || "";
    const isPlaying = !el.paused && !el.ended && el.readyState >= 1;

    if (!isPlaying && !allowHoveredPaused) return false;

    if (el.tagName === "AUDIO") {
      return Boolean(src) || isPlaying;
    }

    const rect = el.getBoundingClientRect();
    if (rect.width < 140 || rect.height < 140) return false;
    if (rect.bottom < 20 || rect.top > window.innerHeight - 20) return false;

    return true;
  }

  function applyThemeAndLangToWidget() {
    if (!iconTrigger || !menuList) return;
    const isLight = (currentTheme === "light");

    iconTrigger.title = currentLang === "en"
      ? "Fold FAST: Click to pick quality & download"
      : "Fold FAST: Haz clic para elegir calidad y descargar";

    iconTrigger.style.background = isLight ? "rgba(255, 255, 255, 0.94)" : "rgba(24, 24, 28, 0.92)";
    iconTrigger.style.border = isLight ? "1px solid rgba(15, 23, 42, 0.16)" : "1px solid rgba(255, 255, 255, 0.2)";
    iconTrigger.style.boxShadow = isLight ? "0 4px 14px rgba(15, 23, 42, 0.2)" : "0 4px 14px rgba(0, 0, 0, 0.55)";

    menuList.style.background = isLight ? "#ffffff" : "#18181c";
    menuList.style.color = isLight ? "#0f172a" : "#ffffff";
    menuList.style.border = isLight ? "1px solid rgba(15, 23, 42, 0.12)" : "1px solid rgba(255, 255, 255, 0.14)";
    menuList.style.boxShadow = isLight ? "0 10px 25px rgba(15, 23, 42, 0.2)" : "0 10px 25px rgba(0, 0, 0, 0.6)";

    const items = menuList.querySelectorAll(".foldfast-opt-item");
    items.forEach((it, idx) => {
      const opt = OPTIONS[idx];
      if (opt) {
        const labelText = currentLang === "en" ? opt.en : opt.es;
        it.innerHTML = `<span>${opt.icon}</span><span>${labelText}</span>`;
      }
      it.style.color = isLight ? "#0f172a" : "#ffffff";
      it.onmouseenter = () => {
        it.style.background = isLight ? "#f1f5f9" : "#27272f";
      };
      it.onmouseleave = () => {
        it.style.background = "transparent";
      };
    });
  }

  function ensureWidget() {
    if (rootWidget && document.body && document.body.contains(rootWidget)) {
      return rootWidget;
    }
    if (!document.body) return null;

    rootWidget = document.createElement("div");
    rootWidget.id = "foldfast-v6-widget";
    Object.assign(rootWidget.style, {
      position: "fixed",
      zIndex: "2147483647",
      top: "12px",
      right: "12px",
      display: "none",
      fontFamily: "'Segoe UI', system-ui, sans-serif",
      userSelect: "none"
    });

    iconTrigger = document.createElement("div");
    Object.assign(iconTrigger.style, {
      width: "34px",
      height: "34px",
      borderRadius: "10px",
      display: "flex",
      alignItems: "center",
      justifyContent: "center",
      cursor: "pointer",
      position: "relative",
      backdropFilter: "blur(8px)",
      opacity: "0.85",
      transition: "opacity 0.15s ease, transform 0.15s ease"
    });

    const img = document.createElement("img");
    img.src = LOGO_DATA_URI;
    Object.assign(img.style, {
      width: "22px",
      height: "22px",
      objectFit: "contain",
      pointerEvents: "none"
    });
    iconTrigger.appendChild(img);

    badgeLabel = document.createElement("span");
    Object.assign(badgeLabel.style, {
      position: "absolute",
      bottom: "-4px",
      right: "-4px",
      background: "#38e54d",
      color: "#091206",
      fontSize: "9px",
      fontWeight: "800",
      borderRadius: "99px",
      padding: "1px 4px",
      display: "none"
    });
    iconTrigger.appendChild(badgeLabel);

    menuList = document.createElement("div");
    Object.assign(menuList.style, {
      position: "absolute",
      top: "38px",
      right: "0px",
      width: "136px",
      borderRadius: "12px",
      padding: "4px",
      display: "none",
      flexDirection: "column",
      gap: "1px"
    });

    OPTIONS.forEach(opt => {
      const row = document.createElement("div");
      row.className = "foldfast-opt-item";
      Object.assign(row.style, {
        display: "flex",
        alignItems: "center",
        gap: "7px",
        padding: "5px 8px",
        borderRadius: "8px",
        fontSize: "11.5px",
        fontWeight: "600",
        cursor: "pointer",
        whiteSpace: "nowrap"
      });
      row.addEventListener("click", (e) => {
        e.stopPropagation();
        e.preventDefault();
        menuOpen = false;
        menuList.style.display = "none";
        startInstantDownload(opt.value);
      });
      menuList.appendChild(row);
    });

    iconTrigger.addEventListener("mouseenter", () => {
      iconTrigger.style.opacity = "1";
      iconTrigger.style.transform = "scale(1.06)";
    });
    iconTrigger.addEventListener("mouseleave", () => {
      if (!menuOpen) iconTrigger.style.opacity = "0.85";
      iconTrigger.style.transform = "scale(1)";
    });
    iconTrigger.addEventListener("click", (e) => {
      e.stopPropagation();
      e.preventDefault();
      menuOpen = !menuOpen;
      menuList.style.display = menuOpen ? "flex" : "none";
    });

    document.addEventListener("click", () => {
      if (menuOpen) {
        menuOpen = false;
        if (menuList) menuList.style.display = "none";
      }
    });

    rootWidget.appendChild(iconTrigger);
    rootWidget.appendChild(menuList);
    document.body.appendChild(rootWidget);
    applyThemeAndLangToWidget();
    return rootWidget;
  }

  function positionIconOnMedia(el) {
    const w = ensureWidget();
    if (!w) return;

    w.style.display = "block";

    if (el && el.tagName === "VIDEO" && el.getBoundingClientRect) {
      const rect = el.getBoundingClientRect();
      if (rect.width >= 140 && rect.height >= 140 && rect.bottom > 20 && rect.top < window.innerHeight - 20) {
        const topPos = Math.max(10, Math.min(window.innerHeight - 46, rect.top + 10));
        const rightPos = Math.max(10, window.innerWidth - rect.right + 10);
        w.style.top = topPos + "px";
        w.style.right = rightPos + "px";
        w.style.left = "auto";
        return;
      }
    }

    w.style.top = "14px";
    w.style.right = "14px";
    w.style.left = "auto";
  }

  function scanAndPositionActiveMedia() {
    if (activeMediaElement && isValidMediaElement(activeMediaElement, false)) {
      positionIconOnMedia(activeMediaElement);
      return;
    }

    const nodes = document.querySelectorAll("video, audio");
    for (const node of nodes) {
      if (isValidMediaElement(node, false)) {
        activeMediaElement = node;
        positionIconOnMedia(node);
        return;
      }
    }

    if (rootWidget && !menuOpen) {
      rootWidget.style.display = "none";
    }
  }

  function startInstantDownload(presetValue) {
    const [mType, fmt, qual] = presetValue.split("|");
    const rawSrc = (activeMediaElement && (activeMediaElement.currentSrc || activeMediaElement.src)) || "";
    const directSrc = (rawSrc && !rawSrc.startsWith("blob:")) ? rawSrc : "";
    const permalink = findMediaPermalink(activeMediaElement);

    if (badgeLabel) {
      badgeLabel.textContent = "⏳";
      badgeLabel.style.display = "block";
    }

    chrome.runtime.sendMessage(
      {
        action: "download_media",
        url: directSrc || permalink,
        pageUrl: permalink,
        fallbackStreamUrl: latestNetworkStreamUrl || directSrc,
        title: document.title || "foldfast_media",
        mediaType: mType,
        formatExt: fmt,
        quality: qual
      },
      (resp) => {
        if (badgeLabel) {
          badgeLabel.textContent = (resp && resp.ok) ? "✓" : "!";
          setTimeout(() => {
            if (badgeLabel) badgeLabel.style.display = "none";
          }, 2200);
        }
      }
    );
  }

  chrome.storage.local.get(["theme", "lang"], (data) => {
    if (data) {
      if (data.theme) currentTheme = data.theme;
      if (data.lang) currentLang = data.lang;
      applyThemeAndLangToWidget();
    }
  });
  chrome.storage.onChanged.addListener((changes) => {
    if (changes.theme && changes.theme.newValue) currentTheme = changes.theme.newValue;
    if (changes.lang && changes.lang.newValue) currentLang = changes.lang.newValue;
    applyThemeAndLangToWidget();
  });

  ["play", "playing", "timeupdate", "pause", "ended"].forEach(evName => {
    document.addEventListener(
      evName,
      (e) => {
        const target = e.target;
        if (target && (target.tagName === "VIDEO" || target.tagName === "AUDIO")) {
          if (isValidMediaElement(target, false)) {
            activeMediaElement = target;
            positionIconOnMedia(target);
          } else if (evName === "pause" || evName === "ended") {
            scanAndPositionActiveMedia();
          }
        }
      },
      true
    );
  });

  document.addEventListener(
    "mousemove",
    (e) => {
      if (!document.elementsFromPoint) return;
      const els = document.elementsFromPoint(e.clientX, e.clientY);
      for (const el of els) {
        if ((el.tagName === "VIDEO" || el.tagName === "AUDIO") && isValidMediaElement(el, true)) {
          activeMediaElement = el;
          positionIconOnMedia(el);
          break;
        }
      }
    },
    { passive: true }
  );

  window.addEventListener(
    "scroll",
    () => {
      if (activeMediaElement && rootWidget && rootWidget.style.display === "block") {
        if (isValidMediaElement(activeMediaElement, true)) {
          positionIconOnMedia(activeMediaElement);
        } else if (!menuOpen) {
          rootWidget.style.display = "none";
        }
      }
    },
    { passive: true }
  );

  chrome.runtime.onMessage.addListener((msg) => {
    if (msg.action === "network_media_detected") {
      latestNetworkStreamUrl = msg.streamUrl || "";
      scanAndPositionActiveMedia();
    }
  });

  setInterval(scanAndPositionActiveMedia, 1000);
})();
