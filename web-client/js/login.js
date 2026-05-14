(function () {
  const msg = document.getElementById("msg");
  const btn = document.getElementById("btn-login");

  function setBusy(busy) {
    btn.disabled = busy;
    btn.textContent = busy ? "Вход…" : "Войти";
  }

  function showError(text) {
    msg.className = "msg error";
    msg.textContent = text || "Ошибка входа.";
  }

  async function doLogin() {
    msg.className = "msg";
    msg.textContent = "";

    const login = document.getElementById("login").value.trim();
    const password = document.getElementById("password").value.trim();

    if (!login || !password) {
      showError("Введите логин и пароль.");
      return;
    }

    setBusy(true);
    try {
      const res = await fetch(`${CRM_API_BASE.replace(/\/$/, "")}/auth/login`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Accept: "application/json",
        },
        body: JSON.stringify({ Login: login, Password: password }),
      });

      const text = await res.text();
      let data = null;
      if (text) {
        try {
          data = JSON.parse(text);
        } catch {
          data = text;
        }
      }

      if (!res.ok) {
        const detail =
          typeof data === "string"
            ? data
            : data?.title || data?.detail || text || res.statusText;
        showError(detail || `Ошибка ${res.status}`);
        return;
      }

      const user = parseAuthUser(data);
      if (!user) {
        showError("Не удалось разобрать ответ сервера. Попробуйте ещё раз.");
        return;
      }

      setStoredUser(user);

      if (isDirectorRole(user.position)) {
        window.location.href = "director.html";
        return;
      }
      if (isManagerRole(user.position)) {
        window.location.href = "manager.html";
        return;
      }
      if (isWarehouseRole(user.position)) {
        window.location.href = "warehouse.html";
        return;
      }

      clearStoredUser();
    } catch (e) {
      showError("Не удаётся связаться с сервером. Попробуйте позже.");
    } finally {
      setBusy(false);
    }
  }

  btn.addEventListener("click", doLogin);
  document.getElementById("password").addEventListener("keydown", (e) => {
    if (e.key === "Enter") doLogin();
  });
  document.getElementById("login").addEventListener("keydown", (e) => {
    if (e.key === "Enter") doLogin();
  });
})();
