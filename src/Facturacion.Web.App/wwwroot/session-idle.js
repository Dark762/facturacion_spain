(() => {
  "use strict";
  const idleMs = 15 * 60 * 1000;
  const warningMs = 60 * 1000;
  const heartbeatMs = 60 * 1000;
  let lastActivity = Date.now();
  let lastHeartbeat = 0;
  let ending = false;
  let warningOpen = false;
  const warning = () => document.getElementById("session-idle-warning");
  const csrf = () => document.querySelector('#session-security-token input[name="__RequestVerificationToken"]')?.value;
  const active = () => Boolean(document.getElementById("session-security-token"));
  async function post(path) {
    const token = csrf();
    if (!token) throw new Error("No se encontró el token de seguridad.");
    const data = new URLSearchParams({ __RequestVerificationToken: token });
    return fetch(path, { method: "POST", credentials: "same-origin", headers: { "Content-Type": "application/x-www-form-urlencoded" }, body: data, cache: "no-store" });
  }
  async function end(reason) {
    if (ending) return;
    ending = true;
    try { await post("/session/end"); } catch (_) { /* Cookie may already be expired. */ }
    location.replace("/login?reason=" + encodeURIComponent(reason));
  }
  async function heartbeat() {
    if (ending || !active() || Date.now() - lastHeartbeat < heartbeatMs) return;
    lastHeartbeat = Date.now();
    try {
      const response = await post("/session/heartbeat");
      if (response.status === 401 || response.redirected) { await end("expired"); return; }
      if (!response.ok) lastHeartbeat = 0;
    } catch (_) { lastHeartbeat = 0; }
  }
  function showWarning() {
    const dialog = warning();
    if (!dialog) return;
    if (!warningOpen) {
      warningOpen = true;
      dialog.hidden = false;
      document.getElementById("session-idle-continue")?.focus();
    }
    const remaining = Math.max(0, Math.ceil((idleMs - (Date.now() - lastActivity)) / 1000));
    const counter = document.getElementById("session-idle-seconds");
    if (counter) counter.textContent = String(remaining);
  }
  function hideWarning() {
    warningOpen = false;
    const dialog = warning();
    if (dialog) dialog.hidden = true;
  }
  function activity() {
    if (ending || warningOpen || !active()) return;
    lastActivity = Date.now();
  }
  for (const event of ["pointerdown", "keydown", "touchstart", "scroll"]) {
    document.addEventListener(event, activity, { passive: true });
  }
  document.addEventListener("click", (event) => {
    if (event.target?.closest?.("#session-idle-end")) void end("inactive");
    if (event.target?.closest?.("#session-idle-continue")) {
      lastActivity = Date.now();
      hideWarning();
      lastHeartbeat = 0;
      void heartbeat();
    }
  });
  setInterval(() => {
    if (ending || !active()) return;
    const idle = Date.now() - lastActivity;
    if (idle >= idleMs) { void end("inactive"); return; }
    if (idle >= idleMs - warningMs) { showWarning(); return; }
    if (!warningOpen) void heartbeat();
  }, 1000);
  // The server-side cookie and API token remain authoritative even if timers pause.
  document.addEventListener("visibilitychange", () => {
    if (document.visibilityState !== "visible" || !active() || ending) return;
    if (Date.now() - lastActivity >= idleMs) void end("inactive");
    else if (Date.now() - lastActivity >= idleMs - warningMs) showWarning();
    else void heartbeat();
  });
})();