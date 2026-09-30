(() => {
  if (!("serviceWorker" in navigator)) return;
  navigator.serviceWorker.register("/service-worker.js").catch(error => console.warn("PWA service worker kaydedilemedi:", error));

  let installPrompt = null;
  const button = document.getElementById("pwa-install-button");
  if (!button) return;

  const isIos = /iphone|ipad|ipod/i.test(navigator.userAgent) && !window.MSStream;
  const isStandalone = window.matchMedia("(display-mode: standalone)").matches || window.navigator.standalone === true;
  if (isStandalone) return;

  window.addEventListener("beforeinstallprompt", event => {
    event.preventDefault();
    installPrompt = event;
    button.classList.remove("d-none");
  });

  if (isIos) button.classList.remove("d-none");

  button.addEventListener("click", async () => {
    if (!installPrompt) {
      if (isIos) alert("Safari'de Paylaş düğmesine, ardından 'Ana Ekrana Ekle' seçeneğine dokunun.");
      return;
    }
    installPrompt.prompt();
    await installPrompt.userChoice;
    installPrompt = null;
    button.classList.add("d-none");
  });

  window.addEventListener("appinstalled", () => button.classList.add("d-none"));
})();
