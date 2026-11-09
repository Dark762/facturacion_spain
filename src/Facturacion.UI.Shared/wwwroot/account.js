(() => {
  const dialog = document.getElementById("logout-confirmation");
  if (!dialog || typeof dialog.showModal !== "function") return;
  const open = () => { if (!dialog.open) dialog.showModal(); };
  document.querySelectorAll("[data-open-logout]").forEach(button =>
    button.addEventListener("click", open));
  if (new URLSearchParams(window.location.search).get("confirmLogout") === "1") {
    open();
    // Evita volver a mostrar la confirmación al actualizar la página.
    window.history.replaceState(null, "", window.location.pathname);
  }
  dialog.addEventListener("click", event => {
    if (event.target === dialog) dialog.close();
  });
})();