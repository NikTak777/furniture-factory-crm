(function () {
  const STORAGE_KEY = "furniture_crm_user";

  window.CRM_API_BASE = window.CRM_API_BASE || "http://localhost:5028/api";

  // Первое непустое значение по списку ключей (учёт camelCase ASP.NET для кириллицы)
  function pickFirst(payload, keys) {
    for (let i = 0; i < keys.length; i++) {
      const k = keys[i];
      if (Object.prototype.hasOwnProperty.call(payload, k)) {
        const v = payload[k];
        if (v !== undefined && v !== null) return v;
      }
    }
    return undefined;
  }

  window.parseAuthUser = function (payload) {
    if (!payload || typeof payload !== "object") return null;
    const staffId = pickFirst(payload, [
      "staffId",
      "ID_сотрудника",
      "iD_сотрудника",
      "id_сотрудника",
    ]);
    const fullName =
      pickFirst(payload, [
        "fullName",
        "ФИО",
        "фИО",
        "фио",
      ]) ?? "";
    const position = (
      pickFirst(payload, ["position", "Должность", "должность"]) ?? ""
    ).trim();
    if (staffId === undefined || staffId === null) return null;
    return { staffId, fullName, position };
  };

  window.getStoredUser = function () {
    try {
      const raw = sessionStorage.getItem(STORAGE_KEY);
      if (!raw) return null;
      const data = JSON.parse(raw);
      return parseAuthUser(data);
    } catch {
      return null;
    }
  };

  window.setStoredUser = function (user) {
    sessionStorage.setItem(STORAGE_KEY, JSON.stringify(user));
  };

  window.clearStoredUser = function () {
    sessionStorage.removeItem(STORAGE_KEY);
  };

  window.isManagerRole = function (position) {
    return (position || "").trim() === "Менеджер";
  };

  window.isDirectorRole = function (position) {
    return (position || "").trim() === "Директор";
  };

  window.isWarehouseRole = function (position) {
    return (position || "").trim() === "Кладовщик";
  };

  window.requireManagerPage = function () {
    const user = getStoredUser();
    if (!user) {
      window.location.href = "index.html";
      return null;
    }
    if (!isManagerRole(user.position)) {
      alert(
        "Вход доступен только менеджеру. Попробуйте войти с другой учётной записью."
      );
      window.location.href = "index.html";
      return null;
    }
    return user;
  };

  window.requireDirectorPage = function () {
    const user = getStoredUser();
    if (!user) {
      window.location.href = "index.html";
      return null;
    }
    if (!isDirectorRole(user.position)) {
      alert("Вход доступен только директору. Попробуйте войти с другой учётной записью.");
      window.location.href = "index.html";
      return null;
    }
    return user;
  };

  window.requireWarehousePage = function () {
    const user = getStoredUser();
    if (!user) {
      window.location.href = "index.html";
      return null;
    }
    if (!isWarehouseRole(user.position)) {
      alert("Вход доступен только кладовщику. Попробуйте войти с другой учётной записью.");
      window.location.href = "index.html";
      return null;
    }
    return user;
  };

  window.apiJson = async function (path, options = {}) {
    const url =
      path.startsWith("http") ? path : `${CRM_API_BASE.replace(/\/$/, "")}/${path.replace(/^\//, "")}`;
    const res = await fetch(url, {
      headers: {
        Accept: "application/json",
        ...(options.headers || {}),
      },
      ...options,
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
      const msg =
        typeof data === "string"
          ? data
          : data?.title || data?.detail || res.statusText;
      const err = new Error(msg || `HTTP ${res.status}`);
      err.status = res.status;
      err.body = data;
      throw err;
    }
    return data;
  };

  window.formatRuDate = function (isoOrDate) {
    if (!isoOrDate) return "—";
    const d = new Date(isoOrDate);
    if (Number.isNaN(d.getTime())) return String(isoOrDate);
    return d.toLocaleDateString("ru-RU");
  };

  window.formatMoney = function (n) {
    if (n === undefined || n === null) return "—";
    return `${Number(n).toLocaleString("ru-RU")} ₽`;
  };
})();
